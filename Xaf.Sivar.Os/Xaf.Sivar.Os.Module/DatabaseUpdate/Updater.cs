using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.EF;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Updating;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.BaseImpl.EF.PermissionPolicy;
using Microsoft.Extensions.DependencyInjection;
using Xaf.Sivar.Os.Module.BusinessObjects;
using Sivar.Os.Shared.Entities;

namespace Xaf.Sivar.Os.Module.DatabaseUpdate
{
    // For more typical usage scenarios, be sure to check out https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.Updating.ModuleUpdater
    public class Updater : ModuleUpdater
    {
        public Updater(IObjectSpace objectSpace, Version currentDBVersion) :
            base(objectSpace, currentDBVersion)
        {
        }
        public override void UpdateDatabaseAfterUpdateSchema()
        {
            base.UpdateDatabaseAfterUpdateSchema();
            //string name = "MyName";
            //EntityObject1 theObject = ObjectSpace.FirstOrDefault<EntityObject1>(u => u.Name == name);
            //if(theObject == null) {
            //    theObject = ObjectSpace.CreateObject<EntityObject1>();
            //    theObject.Name = name;
            //}

            // The code below creates users and roles for testing purposes only.
            // In production code, you can create users and assign roles to them automatically, as described in the following help topic:
            // https://docs.devexpress.com/eXpressAppFramework/119064/data-security-and-safety/security-system/authentication
#if !RELEASE
            // If a role doesn't exist in the database, create this role
            var defaultRole = CreateDefaultRole();
            var adminRole = CreateAdminRole();

            ObjectSpace.CommitChanges(); //This line persists created object(s).

            UserManager userManager = ObjectSpace.ServiceProvider.GetRequiredService<UserManager>();

            // If a user named 'User' doesn't exist in the database, create this user
            if (userManager.FindUserByName<ApplicationUser>(ObjectSpace, "User") == null)
            {
                // Set a password if the standard authentication type is used
                string EmptyPassword = "";
                _ = userManager.CreateUser<ApplicationUser>(ObjectSpace, "User", EmptyPassword, (user) =>
                {
                    // Add the Users role to the user
                    user.Roles.Add(defaultRole);
                });
            }

            // If a user named 'Admin' doesn't exist in the database, create this user
            if (userManager.FindUserByName<ApplicationUser>(ObjectSpace, "Admin") == null)
            {
                // Set a password if the standard authentication type is used
                string EmptyPassword = "";
                _ = userManager.CreateUser<ApplicationUser>(ObjectSpace, "Admin", EmptyPassword, (user) =>
                {
                    // Add the Administrators role to the user
                    user.Roles.Add(adminRole);
                });
            }

            ObjectSpace.CommitChanges(); //This line persists created object(s);
#endif

            // Seed profile types (runs in both DEBUG and RELEASE)
            SeedProfileTypes();

            ObjectSpace.CommitChanges(); //This line persists created object(s);
        }
        public override void UpdateDatabaseBeforeUpdateSchema()
        {
            base.UpdateDatabaseBeforeUpdateSchema();
        }
        PermissionPolicyRole CreateAdminRole()
        {
            PermissionPolicyRole adminRole = ObjectSpace.FirstOrDefault<PermissionPolicyRole>(r => r.Name == "Administrators");
            if (adminRole == null)
            {
                adminRole = ObjectSpace.CreateObject<PermissionPolicyRole>();
                adminRole.Name = "Administrators";
                adminRole.IsAdministrative = true;
            }
            return adminRole;
        }
        PermissionPolicyRole CreateDefaultRole()
        {
            PermissionPolicyRole defaultRole = ObjectSpace.FirstOrDefault<PermissionPolicyRole>(role => role.Name == "Default");
            if (defaultRole == null)
            {
                defaultRole = ObjectSpace.CreateObject<PermissionPolicyRole>();
                defaultRole.Name = "Default";

                defaultRole.AddObjectPermissionFromLambda<ApplicationUser>(SecurityOperations.Read, cm => cm.ID == (Guid)CurrentUserIdOperator.CurrentUserId(), SecurityPermissionState.Allow);
                defaultRole.AddNavigationPermission(@"Application/NavigationItems/Items/Default/Items/MyDetails", SecurityPermissionState.Allow);
                defaultRole.AddMemberPermissionFromLambda<ApplicationUser>(SecurityOperations.Write, "ChangePasswordOnFirstLogon", cm => cm.ID == (Guid)CurrentUserIdOperator.CurrentUserId(), SecurityPermissionState.Allow);
                defaultRole.AddMemberPermissionFromLambda<ApplicationUser>(SecurityOperations.Write, "StoredPassword", cm => cm.ID == (Guid)CurrentUserIdOperator.CurrentUserId(), SecurityPermissionState.Allow);
                defaultRole.AddTypePermissionsRecursively<PermissionPolicyRole>(SecurityOperations.Read, SecurityPermissionState.Deny);
                defaultRole.AddObjectPermission<ModelDifference>(SecurityOperations.ReadWriteAccess, "UserId = ToStr(CurrentUserId())", SecurityPermissionState.Allow);
                defaultRole.AddObjectPermission<ModelDifferenceAspect>(SecurityOperations.ReadWriteAccess, "Owner.UserId = ToStr(CurrentUserId())", SecurityPermissionState.Allow);
                defaultRole.AddTypePermissionsRecursively<ModelDifference>(SecurityOperations.Create, SecurityPermissionState.Allow);
                defaultRole.AddTypePermissionsRecursively<ModelDifferenceAspect>(SecurityOperations.Create, SecurityPermissionState.Allow);
            }
            return defaultRole;
        }

        void SeedProfileTypes()
        {
            var personalProfileId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var businessProfileId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var organizationProfileId = Guid.Parse("33333333-3333-3333-3333-333333333333");
            var now = DateTime.UtcNow;

            // Personal Profile Type
            var personalProfileType = ObjectSpace.FirstOrDefault<ProfileType>(pt => pt.Id == personalProfileId);
            if (personalProfileType == null)
            {
                personalProfileType = ObjectSpace.CreateObject<ProfileType>();
                personalProfileType.Id = personalProfileId;
                personalProfileType.Name = "PersonalProfile";
                personalProfileType.DisplayName = "Personal Profile";
                personalProfileType.Description = "A personal profile for individual users to share their information, interests, and bio.";
                personalProfileType.IsActive = true;
                personalProfileType.SortOrder = 1;
                personalProfileType.FeatureFlags = @"{
                ""AllowsDisplayName"": true,
     ""AllowsBio"": true,
   ""AllowsAvatar"": true,
         ""AllowsLocation"": true,
     ""AllowsBookings"": false,
             ""AllowsProducts"": false,
         ""AllowsContactInfo"": true,
    ""MaxBioLength"": 1000
     }";
                personalProfileType.CreatedAt = now;
                personalProfileType.UpdatedAt = now;
            }

            // Business Profile Type
            var businessProfileType = ObjectSpace.FirstOrDefault<ProfileType>(pt => pt.Id == businessProfileId);
            if (businessProfileType == null)
            {
                businessProfileType = ObjectSpace.CreateObject<ProfileType>();
                businessProfileType.Id = businessProfileId;
                businessProfileType.Name = "BusinessProfile";
                businessProfileType.DisplayName = "Business Profile";
                businessProfileType.Description = "A business profile for companies and professional services.";
                businessProfileType.IsActive = true;
                businessProfileType.SortOrder = 2;
                businessProfileType.FeatureFlags = @"{
       ""AllowsDisplayName"": true,
  ""AllowsBio"": true,
        ""AllowsAvatar"": true,
""AllowsLocation"": true,
     ""AllowsBookings"": true,
   ""AllowsProducts"": true,
       ""AllowsContactInfo"": true,
          ""MaxBioLength"": 2000
            }";
                businessProfileType.CreatedAt = now;
                businessProfileType.UpdatedAt = now;
            }

            // Organization Profile Type
            var organizationProfileType = ObjectSpace.FirstOrDefault<ProfileType>(pt => pt.Id == organizationProfileId);
            if (organizationProfileType == null)
            {
                organizationProfileType = ObjectSpace.CreateObject<ProfileType>();
                organizationProfileType.Id = organizationProfileId;
                organizationProfileType.Name = "OrganizationProfile";
                organizationProfileType.DisplayName = "Organization Profile";
                organizationProfileType.Description = "An organization profile for groups, non-profits, and institutions.";
                organizationProfileType.IsActive = true;
                organizationProfileType.SortOrder = 3;
                organizationProfileType.FeatureFlags = @"{
                ""AllowsDisplayName"": true,
              ""AllowsBio"": true,
                   ""AllowsAvatar"": true,
                      ""AllowsLocation"": true,
                ""AllowsBookings"": false,
                      ""AllowsProducts"": false,
                        ""AllowsContactInfo"": true,
                   ""MaxBioLength"": 2000
            }";
                organizationProfileType.CreatedAt = now;
                organizationProfileType.UpdatedAt = now;
            }
        }
    }
}
