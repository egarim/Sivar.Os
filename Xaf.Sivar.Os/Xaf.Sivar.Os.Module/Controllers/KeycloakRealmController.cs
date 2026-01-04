using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.Persistent.Base;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Xaf.Sivar.Os.Module.Configuration;

namespace Xaf.Sivar.Os.Module.Controllers
{
    /// <summary>
    /// Controller for Keycloak realm management.
    /// Actions are numbered for execution order.
    /// </summary>
    public class KeycloakRealmController : ObjectViewController<DetailView, BusinessObjects.KeycloakSetupLog>
    {
        private SimpleAction deleteRealmAction;
        private SimpleAction createRealmAction;
        private SimpleAction createClientsAction;
        private SimpleAction setupUserProfileAction;

        public KeycloakRealmController()
        {
            // 1. Delete Realm
            deleteRealmAction = new SimpleAction(this, "1_DeleteRealm", "KeycloakSetup")
            {
                Caption = "1. Delete Realm",
                ToolTip = "Delete the sivar-os realm (WARNING: Deletes all users!)",
                ImageName = "Action_Delete",
                ConfirmationMessage = "⚠️ This will DELETE the entire sivar-os realm including all users. Continue?"
            };
            deleteRealmAction.Execute += DeleteRealmAction_Execute;

            // 2. Create Realm
            createRealmAction = new SimpleAction(this, "2_CreateRealm", "KeycloakSetup")
            {
                Caption = "2. Create Realm",
                ToolTip = "Create the sivar-os realm with basic settings",
                ImageName = "Action_New"
            };
            createRealmAction.Execute += CreateRealmAction_Execute;

            // 3. Create Clients
            createClientsAction = new SimpleAction(this, "3_CreateClients", "KeycloakSetup")
            {
                Caption = "3. Create Clients",
                ToolTip = "Create sivaros-client and sivaros-server clients",
                ImageName = "BO_Security_Permission_Type"
            };
            createClientsAction.Execute += CreateClientsAction_Execute;

            // 4. Setup User Profile & Mappers
            setupUserProfileAction = new SimpleAction(this, "4_SetupUserProfile", "KeycloakSetup")
            {
                Caption = "4. Setup User Profile",
                ToolTip = "Add waiting_list_status attribute and token mapper",
                ImageName = "BO_User"
            };
            setupUserProfileAction.Execute += SetupUserProfileAction_Execute;
        }

        private KeycloakAdminSettings? GetSettings()
        {
            var options = Application?.ServiceProvider?.GetService<IOptions<KeycloakAdminSettings>>();
            return options?.Value;
        }

        private BusinessObjects.KeycloakSetupLog? GetLog() => View.CurrentObject as BusinessObjects.KeycloakSetupLog;

        private string? GetToken(HttpClient client, KeycloakAdminSettings settings, BusinessObjects.KeycloakSetupLog log)
        {
            try
            {
                var tokenRequest = new Dictionary<string, string>
                {
                    ["grant_type"] = "password",
                    ["client_id"] = settings.ClientId,
                    ["username"] = settings.AdminUsername,
                    ["password"] = settings.AdminPassword
                };
                var content = new FormUrlEncodedContent(tokenRequest);
                var response = client.PostAsync(settings.TokenEndpoint, content).Result;
                if (!response.IsSuccessStatusCode)
                {
                    log.AppendLog($"❌ Auth failed: {response.StatusCode}");
                    return null;
                }
                var json = response.Content.ReadAsStringAsync().Result;
                var doc = JsonDocument.Parse(json);
                return doc.RootElement.GetProperty("access_token").GetString();
            }
            catch (Exception ex)
            {
                log.AppendLog($"❌ Auth error: {ex.Message}");
                return null;
            }
        }

        private void DeleteRealmAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            var log = GetLog();
            var settings = GetSettings();
            if (log == null || settings == null) return;

            log.StartOperation("Delete Realm");
            using var client = new HttpClient();
            var token = GetToken(client, settings, log);
            if (token == null) { ObjectSpace.CommitChanges(); return; }

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var url = $"{settings.BaseUrl}/admin/realms/{settings.Realm}";
            log.AppendLog($"Deleting realm: {settings.Realm}");

            var response = client.DeleteAsync(url).Result;
            if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                log.AppendLog("✅ Realm deleted (or didn't exist)");
                log.EndOperation("Delete Realm", true, "Success");
            }
            else
            {
                log.AppendLog($"❌ Failed: {response.StatusCode}");
                log.EndOperation("Delete Realm", false, response.StatusCode.ToString());
            }
            ObjectSpace.CommitChanges();
            View.Refresh();
        }

        private void CreateRealmAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            var log = GetLog();
            var settings = GetSettings();
            if (log == null || settings == null) return;

            log.StartOperation("Create Realm");
            using var client = new HttpClient();
            var token = GetToken(client, settings, log);
            if (token == null) { ObjectSpace.CommitChanges(); return; }

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var realmPayload = new
            {
                realm = settings.Realm,
                enabled = true,
                registrationAllowed = true,
                registrationEmailAsUsername = true,
                resetPasswordAllowed = true,
                loginWithEmailAllowed = true,
                duplicateEmailsAllowed = false,
                sslRequired = "external",
                accessTokenLifespan = 300,
                ssoSessionIdleTimeout = 1800,
                ssoSessionMaxLifespan = 36000
            };

            var json = JsonSerializer.Serialize(realmPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var url = $"{settings.BaseUrl}/admin/realms";

            log.AppendLog($"Creating realm: {settings.Realm}");
            var response = client.PostAsync(url, content).Result;

            if (response.IsSuccessStatusCode)
            {
                log.AppendLog("✅ Realm created");
                log.EndOperation("Create Realm", true, "Success");
            }
            else
            {
                var error = response.Content.ReadAsStringAsync().Result;
                log.AppendLog($"❌ Failed: {response.StatusCode} - {error}");
                log.EndOperation("Create Realm", false, response.StatusCode.ToString());
            }
            ObjectSpace.CommitChanges();
            View.Refresh();
        }

        private void CreateClientsAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            var log = GetLog();
            var settings = GetSettings();
            if (log == null || settings == null) return;

            log.StartOperation("Create Clients");
            using var client = new HttpClient();
            var token = GetToken(client, settings, log);
            if (token == null) { ObjectSpace.CommitChanges(); return; }

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var baseUrl = $"{settings.BaseUrl}/admin/realms/{settings.Realm}/clients";

            // Create public client for Blazor WASM
            CreateClient(client, baseUrl, log, "sivaros-client", true);
            
            // Create confidential client for server
            CreateClient(client, baseUrl, log, "sivaros-server", false);

            log.EndOperation("Create Clients", true, "Done");
            ObjectSpace.CommitChanges();
            View.Refresh();
        }

        private void CreateClient(HttpClient client, string baseUrl, BusinessObjects.KeycloakSetupLog log, string clientId, bool isPublic)
        {
            var payload = new
            {
                clientId = clientId,
                enabled = true,
                publicClient = isPublic,
                directAccessGrantsEnabled = true,
                standardFlowEnabled = true,
                redirectUris = new[] { "https://localhost:5001/*", "http://localhost:5000/*" },
                webOrigins = new[] { "https://localhost:5001", "http://localhost:5000" },
                attributes = new Dictionary<string, string>
                {
                    ["post.logout.redirect.uris"] = "https://localhost:5001/*"
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = client.PostAsync(baseUrl, content).Result;

            if (response.IsSuccessStatusCode)
                log.AppendLog($"✅ Created client: {clientId}");
            else
                log.AppendLog($"❌ Failed {clientId}: {response.StatusCode}");
        }

        private void SetupUserProfileAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            var log = GetLog();
            var settings = GetSettings();
            if (log == null || settings == null) return;

            log.StartOperation("Setup User Profile");
            using var client = new HttpClient();
            var token = GetToken(client, settings, log);
            if (token == null) { ObjectSpace.CommitChanges(); return; }

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Get current User Profile config
            var profileUrl = $"{settings.BaseUrl}/admin/realms/{settings.Realm}/users/profile";
            var getResponse = client.GetAsync(profileUrl).Result;
            
            if (!getResponse.IsSuccessStatusCode)
            {
                log.AppendLog($"❌ Failed to get user profile: {getResponse.StatusCode}");
                log.EndOperation("Setup User Profile", false, "Failed");
                ObjectSpace.CommitChanges();
                return;
            }

            var profileJson = getResponse.Content.ReadAsStringAsync().Result;
            log.AppendLog("📋 Got current user profile config");

            // Parse and add our attribute
            using var doc = JsonDocument.Parse(profileJson);
            var root = doc.RootElement;
            
            // Build updated profile with waiting_list_status attribute
            var attributes = new List<object>();
            if (root.TryGetProperty("attributes", out var existingAttrs))
            {
                foreach (var attr in existingAttrs.EnumerateArray())
                {
                    var name = attr.GetProperty("name").GetString();
                    if (name != "waiting_list_status")
                        attributes.Add(JsonSerializer.Deserialize<object>(attr.GetRawText())!);
                }
            }

            // Add waiting_list_status attribute
            attributes.Add(new
            {
                name = "waiting_list_status",
                displayName = "Waiting List Status",
                validations = new { },
                permissions = new { view = new[] { "admin", "user" }, edit = new[] { "admin" } },
                multivalued = false
            });

            var updatedProfile = new
            {
                attributes = attributes,
                groups = root.TryGetProperty("groups", out var g) ? JsonSerializer.Deserialize<object>(g.GetRawText()) : null
            };

            var updateJson = JsonSerializer.Serialize(updatedProfile);
            var updateContent = new StringContent(updateJson, Encoding.UTF8, "application/json");
            var updateResponse = client.PutAsync(profileUrl, updateContent).Result;

            if (updateResponse.IsSuccessStatusCode)
                log.AppendLog("✅ Added waiting_list_status attribute");
            else
                log.AppendLog($"❌ Failed to update profile: {updateResponse.StatusCode}");

            log.EndOperation("Setup User Profile", updateResponse.IsSuccessStatusCode, "Done");
            ObjectSpace.CommitChanges();
            View.Refresh();
        }
    }
}
