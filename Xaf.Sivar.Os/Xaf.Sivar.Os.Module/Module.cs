using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.DomainLogics;
using DevExpress.ExpressApp.Model.NodeGenerators;
using DevExpress.ExpressApp.ReportsV2;
using DevExpress.ExpressApp.Updating;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF.Kpi;
using Microsoft.Build.Tasks.Deployment.Bootstrapper;
using Microsoft.CodeAnalysis;
using Sivar.Os.Shared.Entities;
using System.ComponentModel;

namespace Xaf.Sivar.Os.Module
{
    // For more typical usage scenarios, be sure to check out https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.ModuleBase.
    public sealed class OsModule : ModuleBase
    {
        public OsModule()
        {
            //
            // OsModule
            //


            AdditionalExportedTypes.Add(typeof(Post));
            AdditionalExportedTypes.Add(typeof(User));
            AdditionalExportedTypes.Add(typeof(Profile));
            AdditionalExportedTypes.Add(typeof(ProfileType));
            AdditionalExportedTypes.Add(typeof(ProfileFollower));
            AdditionalExportedTypes.Add(typeof(Post));
            AdditionalExportedTypes.Add(typeof(PostAttachment));
            AdditionalExportedTypes.Add(typeof(Comment));
            AdditionalExportedTypes.Add(typeof(Reaction));
            AdditionalExportedTypes.Add(typeof(Notification));
            AdditionalExportedTypes.Add(typeof(Conversation));
            AdditionalExportedTypes.Add(typeof(ChatMessage));
            AdditionalExportedTypes.Add(typeof(SavedResult));

            AdditionalExportedTypes.Add(typeof(Xaf.Sivar.Os.Module.BusinessObjects.ApplicationUser));
            AdditionalExportedTypes.Add(typeof(DevExpress.Persistent.BaseImpl.EF.PermissionPolicy.PermissionPolicyRole));
            AdditionalExportedTypes.Add(typeof(DevExpress.Persistent.BaseImpl.EF.ModelDifference));
            AdditionalExportedTypes.Add(typeof(DevExpress.Persistent.BaseImpl.EF.ModelDifferenceAspect));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.SystemModule.SystemModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.Security.SecurityModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.Objects.BusinessClassLibraryCustomizationModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.Chart.ChartModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.CloneObject.CloneObjectModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.ConditionalAppearance.ConditionalAppearanceModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.Dashboards.DashboardsModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.Kpi.KpiModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.Notifications.NotificationsModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.Office.OfficeModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.PivotChart.PivotChartModuleBase));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.PivotGrid.PivotGridModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.ReportsV2.ReportsModuleV2));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.Scheduler.SchedulerModuleBase));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.Validation.ValidationModule));
            DevExpress.ExpressApp.Kpi.KpiModule.UsedExportedTypes = DevExpress.Persistent.Base.UsedExportedTypes.Custom;
            DevExpress.ExpressApp.Security.SecurityModule.UsedExportedTypes = DevExpress.Persistent.Base.UsedExportedTypes.Custom;
            AdditionalExportedTypes.Add(typeof(DevExpress.Persistent.BaseImpl.EF.FileData));
            AdditionalExportedTypes.Add(typeof(DevExpress.Persistent.BaseImpl.EF.FileAttachment));
            AdditionalExportedTypes.Add(typeof(DevExpress.Persistent.BaseImpl.EF.Analysis));
            AdditionalExportedTypes.Add(typeof(DevExpress.Persistent.BaseImpl.EF.Event));
            AdditionalExportedTypes.Add(typeof(DevExpress.Persistent.BaseImpl.EF.Resource));
            AdditionalExportedTypes.Add(typeof(BaseKpiObject));
            AdditionalExportedTypes.Add(typeof(KpiDefinition));
            AdditionalExportedTypes.Add(typeof(KpiHistoryItem));
            AdditionalExportedTypes.Add(typeof(KpiInstance));
            AdditionalExportedTypes.Add(typeof(KpiScorecard));
        }
        public override IEnumerable<ModuleUpdater> GetModuleUpdaters(IObjectSpace objectSpace, Version versionFromDB)
        {
            ModuleUpdater updater = new DatabaseUpdate.Updater(objectSpace, versionFromDB);
            return new ModuleUpdater[] { updater };
        }
        public override void Setup(XafApplication application)
        {
            base.Setup(application);
            // Manage various aspects of the application UI and behavior at the module level.
        }
        public override void Setup(ApplicationModulesManager moduleManager)
        {
            base.Setup(moduleManager);
        }
        public override void CustomizeTypesInfo(ITypesInfo typesInfo)
        {
            base.CustomizeTypesInfo(typesInfo);

            ModelNodesGeneratorSettings.SetIdPrefix(
                typeof(Notification),
                "Notification_SivarOs"
            );

            // Configure DefaultClassOptions for Sivar.Os entities
            var postTypeInfo = typesInfo.FindTypeInfo(typeof(Post));
            if (postTypeInfo != null)
            {
                postTypeInfo.AddAttribute(new DefaultClassOptionsAttribute());
            }

            var userTypeInfo = typesInfo.FindTypeInfo(typeof(User));
            if (userTypeInfo != null)
            {
                userTypeInfo.AddAttribute(new DefaultClassOptionsAttribute());
            }

            var profileTypeInfo = typesInfo.FindTypeInfo(typeof(Profile));
            if (profileTypeInfo != null)
            {
                profileTypeInfo.AddAttribute(new DefaultClassOptionsAttribute());
            }

            var profileTypeTypeInfo = typesInfo.FindTypeInfo(typeof(ProfileType));
            if (profileTypeTypeInfo != null)
            {

                profileTypeTypeInfo.AddAttribute(new DefaultClassOptionsAttribute());
                profileTypeTypeInfo.Members.FirstOrDefault(m => m.Name == nameof(ProfileType.FeatureFlags))?.AddAttribute(new FieldSizeAttribute(FieldSizeAttribute.Unlimited));
            }

            var profileFollowerTypeInfo = typesInfo.FindTypeInfo(typeof(ProfileFollower));
            if (profileFollowerTypeInfo != null)
            {
                profileFollowerTypeInfo.AddAttribute(new DefaultClassOptionsAttribute());
            }

            var postAttachmentTypeInfo = typesInfo.FindTypeInfo(typeof(PostAttachment));
            if (postAttachmentTypeInfo != null)
            {
                postAttachmentTypeInfo.AddAttribute(new DefaultClassOptionsAttribute());
            }

            var commentTypeInfo = typesInfo.FindTypeInfo(typeof(Comment));
            if (commentTypeInfo != null)
            {
                commentTypeInfo.AddAttribute(new DefaultClassOptionsAttribute());
            }

            var reactionTypeInfo = typesInfo.FindTypeInfo(typeof(Reaction));
            if (reactionTypeInfo != null)
            {
                reactionTypeInfo.AddAttribute(new DefaultClassOptionsAttribute());
            }

            var notificationTypeInfo = typesInfo.FindTypeInfo(typeof(Notification));
            if (notificationTypeInfo != null)
            {
                notificationTypeInfo.AddAttribute(new DefaultClassOptionsAttribute());
            }

            var conversationTypeInfo = typesInfo.FindTypeInfo(typeof(Conversation));
            if (conversationTypeInfo != null)
            {
                conversationTypeInfo.AddAttribute(new DefaultClassOptionsAttribute());
            }

            var chatMessageTypeInfo = typesInfo.FindTypeInfo(typeof(ChatMessage));
            if (chatMessageTypeInfo != null)
            {
                chatMessageTypeInfo.AddAttribute(new DefaultClassOptionsAttribute());
            }

            var savedResultTypeInfo = typesInfo.FindTypeInfo(typeof(SavedResult));
            if (savedResultTypeInfo != null)
            {
                savedResultTypeInfo.AddAttribute(new DefaultClassOptionsAttribute());
            }
        }
    }
}
