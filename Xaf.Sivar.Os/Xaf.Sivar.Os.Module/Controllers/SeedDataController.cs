using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.Persistent.Base;
using Sivar.Os.Shared.Entities;

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

        private void SyncKeycloakUsersAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            var seederLog = View.CurrentObject as BusinessObjects.SeederLog;
            if (seederLog == null) return;

            try
            {
                seederLog.StartOperation("Sync Keycloak Users");
                seederLog.AppendLog("Starting Keycloak user synchronization...");

                // TODO: Implement Keycloak Admin API integration
                // 1. Get all profiles from database
                // 2. For each profile, create a Keycloak user with email: {handle}@sivar.lat
                // 3. Save the Keycloak user ID to the local User table
                // 4. Update the profile's UserId to link to the new user

                // Placeholder implementation - log what would happen
                var profileCount = GetProfileCount();
                seederLog.AppendLog($"Found {profileCount} profiles to sync");
                
                seederLog.AppendLog("⚠️ Keycloak integration not yet implemented");
                seederLog.AppendLog("Required: Keycloak Admin API credentials and endpoint configuration");
                seederLog.AppendLog("See: https://www.keycloak.org/docs-api/latest/rest-api/index.html");

                seederLog.EndOperation("Sync Keycloak Users", false, "Not yet implemented - requires Keycloak Admin API integration");
                
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
