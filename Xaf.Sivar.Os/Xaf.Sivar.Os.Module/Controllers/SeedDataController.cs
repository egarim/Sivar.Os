using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.Persistent.Base;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Sivar.Os.Shared.Entities;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Xaf.Sivar.Os.Module.Configuration;

namespace Xaf.Sivar.Os.Module.Controllers
{
    /// <summary>
    /// Controller for seeding operations.
    /// Provides actions to sync Keycloak users, seed demo profiles, and link profiles to users.
    /// Only active when viewing the SeederLog singleton DetailView.
    /// </summary>
    public class SeedDataController : ObjectViewController<DetailView, BusinessObjects.SeederLog>
    {
        private SimpleAction syncKeycloakUsersAction;
        private SimpleAction seedDemoProfilesAction;
        private SimpleAction linkProfilesToUsersAction;
        private SimpleAction clearLogAction;

        public SeedDataController()
        {
            // Sync Keycloak Users Action
            syncKeycloakUsersAction = new SimpleAction(this, "SyncKeycloakUsers", PredefinedCategory.Tools)
            {
                Caption = "Sync Keycloak Users",
                ToolTip = "Create users in Keycloak for all demo profiles and save their IDs locally",
                ImageName = "BO_User",
                ConfirmationMessage = "This will create users in Keycloak for all demo profiles. Continue?"
            };
            syncKeycloakUsersAction.Execute += SyncKeycloakUsersAction_Execute;

            // Seed Demo Profiles Action
            seedDemoProfilesAction = new SimpleAction(this, "SeedDemoProfiles", PredefinedCategory.Tools)
            {
                Caption = "Seed Demo Profiles",
                ToolTip = "Create profiles from DemoData JSON files",
                ImageName = "BO_Contact",
                ConfirmationMessage = "This will seed demo profiles from JSON files. Continue?"
            };
            seedDemoProfilesAction.Execute += SeedDemoProfilesAction_Execute;

            // Link Profiles to Users Action
            linkProfilesToUsersAction = new SimpleAction(this, "LinkProfilesToUsers", PredefinedCategory.Tools)
            {
                Caption = "Link Profiles to Users",
                ToolTip = "Update Profile.UserId based on handle↔email matching ({handle}@sivar.lat)",
                ImageName = "BO_Transition",
                ConfirmationMessage = "This will link existing profiles to users by matching handle to email. Continue?"
            };
            linkProfilesToUsersAction.Execute += LinkProfilesToUsersAction_Execute;

            // Clear Log Action
            clearLogAction = new SimpleAction(this, "ClearSeederLog", PredefinedCategory.Tools)
            {
                Caption = "Clear Log",
                ToolTip = "Clear all log entries",
                ImageName = "Action_Clear"
            };
            clearLogAction.Execute += ClearLogAction_Execute;
        }

        /// <summary>
        /// Gets the Keycloak Admin settings from DI
        /// </summary>
        private KeycloakAdminSettings? GetKeycloakSettings()
        {
            var serviceProvider = Application?.ServiceProvider;
            if (serviceProvider == null) return null;

            var options = serviceProvider.GetService<IOptions<KeycloakAdminSettings>>();
            return options?.Value;
        }

        private void SyncKeycloakUsersAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            var seederLog = View.CurrentObject as BusinessObjects.SeederLog;
            if (seederLog == null) return;

            try
            {
                seederLog.StartOperation("Sync Keycloak Users");
                seederLog.AppendLog("Starting Keycloak user synchronization...");

                // Get Keycloak settings
                var settings = GetKeycloakSettings();
                if (settings == null)
                {
                    seederLog.AppendLog("❌ ERROR: Could not retrieve KeycloakAdminSettings from DI");
                    seederLog.EndOperation("Sync Keycloak Users", false, "KeycloakAdminSettings not configured");
                    ObjectSpace.CommitChanges();
                    View.Refresh();
                    return;
                }

                // Validate settings
                var validationErrors = settings.Validate();
                if (validationErrors.Count > 0)
                {
                    seederLog.AppendLog("❌ Configuration errors:");
                    foreach (var error in validationErrors)
                    {
                        seederLog.AppendLog($"  - {error}");
                    }
                    seederLog.AppendLog("");
                    seederLog.AppendLog("📝 Please configure KeycloakAdmin in appsettings.json:");
                    seederLog.AppendLog($"  BaseUrl: {settings.BaseUrl}");
                    seederLog.AppendLog($"  Realm: {settings.Realm}");
                    seederLog.AppendLog($"  AdminUsername: {settings.AdminUsername}");
                    seederLog.AppendLog($"  AdminPassword: {(string.IsNullOrEmpty(settings.AdminPassword) ? "(NOT SET)" : "****")}");
                    seederLog.EndOperation("Sync Keycloak Users", false, "Invalid configuration - see log for details");
                    ObjectSpace.CommitChanges();
                    View.Refresh();
                    return;
                }

                seederLog.AppendLog($"📌 Keycloak URL: {settings.BaseUrl}");
                seederLog.AppendLog($"📌 Realm: {settings.Realm}");
                seederLog.AppendLog($"📌 Admin User: {settings.AdminUsername}");

                // Get all profiles
                var profiles = ObjectSpace.GetObjects<Profile>().ToList();
                seederLog.AppendLog($"Found {profiles.Count} profiles to sync");

                // Run async sync operation
                var syncTask = SyncKeycloakUsersAsync(seederLog, settings, profiles);
                syncTask.Wait(); // XAF actions are synchronous, so we wait

                ObjectSpace.CommitChanges();
                View.Refresh();
            }
            catch (Exception ex)
            {
                seederLog.AppendException(ex, "SyncKeycloakUsers");
                seederLog.EndOperation("Sync Keycloak Users", false, $"Error: {ex.Message}");
                ObjectSpace.CommitChanges();
                View.Refresh();
            }
        }

        private async Task SyncKeycloakUsersAsync(BusinessObjects.SeederLog seederLog, KeycloakAdminSettings settings, List<Profile> profiles)
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            // Step 1: Get admin access token
            seederLog.AppendLog("🔐 Authenticating with Keycloak Admin API...");
            string? accessToken;
            try
            {
                accessToken = await GetKeycloakAdminTokenAsync(httpClient, settings);
                seederLog.AppendLog("✅ Authentication successful");
            }
            catch (Exception ex)
            {
                seederLog.AppendLog($"❌ Authentication failed: {ex.Message}");
                seederLog.EndOperation("Sync Keycloak Users", false, $"Authentication failed: {ex.Message}");
                return;
            }

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            int created = 0;
            int skipped = 0;
            int failed = 0;

            // Step 2: Create users for each profile
            foreach (var profile in profiles)
            {
                var email = $"{profile.Handle}@sivar.lat";
                var username = profile.Handle;

                try
                {
                    // Check if user already exists
                    var existsResponse = await httpClient.GetAsync($"{settings.AdminApiUrl}/users?email={Uri.EscapeDataString(email)}");
                    var existsContent = await existsResponse.Content.ReadAsStringAsync();
                    var existingUsers = JsonSerializer.Deserialize<JsonElement[]>(existsContent);

                    if (existingUsers != null && existingUsers.Length > 0)
                    {
                        var existingUserId = existingUsers[0].GetProperty("id").GetString();
                        seederLog.AppendLog($"⏭️ User exists: {email} (ID: {existingUserId})");
                        
                        // Link to local user if needed
                        await EnsureLocalUserLinkedAsync(profile, existingUserId!, email, seederLog);
                        skipped++;
                        continue;
                    }

                    // Parse display name into first/last name
                    var (firstName, lastName) = ParseDisplayName(profile.DisplayName);

                    // Create user in Keycloak
                    var userPayload = new
                    {
                        username = username,
                        email = email,
                        emailVerified = settings.EmailVerified,
                        enabled = settings.EnabledByDefault,
                        firstName = firstName,
                        lastName = lastName,
                        credentials = new[]
                        {
                            new
                            {
                                type = "password",
                                value = settings.DefaultUserPassword,
                                temporary = settings.TemporaryPassword
                            }
                        }
                    };

                    var json = JsonSerializer.Serialize(userPayload);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await httpClient.PostAsync($"{settings.AdminApiUrl}/users", content);

                    if (response.IsSuccessStatusCode)
                    {
                        // Get the created user's ID from Location header
                        var locationHeader = response.Headers.Location?.ToString();
                        var keycloakUserId = locationHeader?.Split('/').Last() ?? Guid.NewGuid().ToString();

                        seederLog.AppendLog($"✅ Created: {email} (ID: {keycloakUserId})");
                        
                        // Link to local user
                        await EnsureLocalUserLinkedAsync(profile, keycloakUserId, email, seederLog);
                        created++;
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        seederLog.AppendLog($"❌ Failed to create {email}: {response.StatusCode} - {errorContent}");
                        failed++;
                    }
                }
                catch (Exception ex)
                {
                    seederLog.AppendLog($"❌ Error processing {email}: {ex.Message}");
                    failed++;
                }
            }

            seederLog.KeycloakUsersSynced += created;
            var summary = $"Created: {created}, Skipped: {skipped}, Failed: {failed}";
            seederLog.EndOperation("Sync Keycloak Users", failed == 0, summary);
        }

        private async Task<string> GetKeycloakAdminTokenAsync(HttpClient httpClient, KeycloakAdminSettings settings)
        {
            var tokenRequest = new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = settings.ClientId,
                ["username"] = settings.AdminUsername,
                ["password"] = settings.AdminPassword
            };

            var content = new FormUrlEncodedContent(tokenRequest);
            var response = await httpClient.PostAsync(settings.TokenEndpoint, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Token request failed: {response.StatusCode} - {error}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseJson);
            return tokenResponse.GetProperty("access_token").GetString() 
                ?? throw new Exception("No access_token in response");
        }

        private Task EnsureLocalUserLinkedAsync(Profile profile, string keycloakUserId, string email, BusinessObjects.SeederLog seederLog)
        {
            // Check if a local User exists with this email
            var existingUser = ObjectSpace.FirstOrDefault<User>(u => u.Email == email);

            // Parse display name into first/last name
            var (firstName, lastName) = ParseDisplayName(profile.DisplayName);

            if (existingUser == null)
            {
                // Create local User record
                existingUser = ObjectSpace.CreateObject<User>();
                existingUser.Id = Guid.NewGuid();
                existingUser.KeycloakId = keycloakUserId;
                existingUser.Email = email;
                existingUser.FirstName = firstName;
                existingUser.LastName = lastName;
                existingUser.CreatedAt = DateTime.UtcNow;
                seederLog.AppendLog($"  📝 Created local User: {email}");
            }
            else if (existingUser.KeycloakId != keycloakUserId)
            {
                existingUser.KeycloakId = keycloakUserId;
                seederLog.AppendLog($"  🔗 Updated KeycloakId for: {email}");
            }

            // Link profile to user if not already linked
            if (profile.UserId != existingUser.Id)
            {
                profile.UserId = existingUser.Id;
                seederLog.AppendLog($"  🔗 Linked profile '{profile.Handle}' to user");
            }

            return Task.CompletedTask;
        }

        private (string firstName, string lastName) ParseDisplayName(string? displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return ("User", "Unknown");

            var parts = displayName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length switch
            {
                0 => ("User", "Unknown"),
                1 => (parts[0], ""),
                _ => (parts[0], parts[1])
            };
        }

        private void SeedDemoProfilesAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            var seederLog = View.CurrentObject as BusinessObjects.SeederLog;
            if (seederLog == null) return;

            try
            {
                seederLog.StartOperation("Seed Demo Profiles");
                seederLog.AppendLog("Starting demo profile seeding...");

                // TODO: Implement demo profile seeding
                // 1. Read JSON files from DemoData folder
                // 2. Create profiles in the database
                // 3. Link profiles to users if users exist

                seederLog.AppendLog("⚠️ Demo profile seeding not yet implemented");
                seederLog.AppendLog("Will read from: DemoData/Restaurants/*.json, DemoData/Services/*.json, etc.");

                seederLog.EndOperation("Seed Demo Profiles", false, "Not yet implemented");
                
                ObjectSpace.CommitChanges();
                View.Refresh();
            }
            catch (Exception ex)
            {
                seederLog.AppendException(ex, "SeedDemoProfiles");
                seederLog.EndOperation("Seed Demo Profiles", false, $"Error: {ex.Message}");
                ObjectSpace.CommitChanges();
                View.Refresh();
            }
        }

        private void LinkProfilesToUsersAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            var seederLog = View.CurrentObject as BusinessObjects.SeederLog;
            if (seederLog == null) return;

            try
            {
                seederLog.StartOperation("Link Profiles to Users");
                seederLog.AppendLog("Starting profile-to-user linking...");

                // Get profiles and users
                var profiles = ObjectSpace.GetObjects<Profile>();
                var users = ObjectSpace.GetObjects<User>();

                var usersDict = users.ToDictionary(u => u.Email?.ToLowerInvariant() ?? "", u => u);
                int linkedCount = 0;
                int skippedCount = 0;

                foreach (var profile in profiles)
                {
                    // Expected email pattern: {handle}@sivar.lat
                    var expectedEmail = $"{profile.Handle}@sivar.lat".ToLowerInvariant();

                    if (usersDict.TryGetValue(expectedEmail, out var matchingUser))
                    {
                        if (profile.UserId != matchingUser.Id)
                        {
                            profile.UserId = matchingUser.Id;
                            linkedCount++;
                            seederLog.AppendLog($"✅ Linked: {profile.Handle} → {matchingUser.Email}");
                        }
                        else
                        {
                            skippedCount++;
                        }
                    }
                    else
                    {
                        seederLog.AppendLog($"⚠️ No user found for: {profile.Handle} (expected: {expectedEmail})");
                    }
                }

                seederLog.ProfilesLinked += linkedCount;
                var summary = $"Linked: {linkedCount}, Already linked: {skippedCount}, Total profiles: {profiles.Count}";
                seederLog.EndOperation("Link Profiles to Users", true, summary);
                
                ObjectSpace.CommitChanges();
                View.Refresh();
            }
            catch (Exception ex)
            {
                seederLog.AppendException(ex, "LinkProfilesToUsers");
                seederLog.EndOperation("Link Profiles to Users", false, $"Error: {ex.Message}");
                ObjectSpace.CommitChanges();
                View.Refresh();
            }
        }

        private void ClearLogAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            var seederLog = View.CurrentObject as BusinessObjects.SeederLog;
            if (seederLog == null) return;

            seederLog.ClearLog();
            seederLog.KeycloakUsersSynced = 0;
            seederLog.ProfilesSeeded = 0;
            seederLog.ProfilesLinked = 0;
            
            ObjectSpace.CommitChanges();
            View.Refresh();
        }

        private int GetProfileCount()
        {
            return ObjectSpace.GetObjectsCount(typeof(Profile), null);
        }
    }
}
