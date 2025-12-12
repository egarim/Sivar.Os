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

            AdditionalExportedTypes.Add(typeof(BusinessContactInfo));
            AdditionalExportedTypes.Add(typeof(ChatBotSettings));
            AdditionalExportedTypes.Add(typeof(AgentCapability));
            AdditionalExportedTypes.Add(typeof(CapabilityParameter));
            AdditionalExportedTypes.Add(typeof(QuickAction));
            AdditionalExportedTypes.Add(typeof(Activity));
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
        private void ConfigureTypeWithDefaultClassOptions(ITypesInfo typesInfo, Type type, Action<ITypeInfo>? additionalConfig = null)
        {
            var typeInfo = typesInfo.FindTypeInfo(type);
            if (typeInfo != null)
            {
                typeInfo.AddAttribute(new DefaultClassOptionsAttribute());
                typeInfo.AddAttribute(new ModelDefaultAttribute("IsCloneable", "True"));
                additionalConfig?.Invoke(typeInfo);
            }
        }
        public override void CustomizeTypesInfo(ITypesInfo typesInfo)
        {
            base.CustomizeTypesInfo(typesInfo);

            ModelNodesGeneratorSettings.SetIdPrefix(
                typeof(Notification),
                "Notification_SivarOs"
            );

            // Configure DefaultClassOptions for Sivar.Os entities
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(Post));
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(User));
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(Profile));
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(ProfileType), typeInfo =>
            {
                typeInfo.Members.FirstOrDefault(m => m.Name == nameof(ProfileType.FeatureFlags))?.AddAttribute(new FieldSizeAttribute(FieldSizeAttribute.Unlimited));
            });
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(ProfileFollower));
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(PostAttachment));
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(Comment));
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(Reaction));
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(Notification));
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(Conversation));
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(ChatMessage));
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(SavedResult));
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(Activity));
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(BusinessContactInfo));
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(ChatBotSettings));
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(AgentCapability));
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(CapabilityParameter));
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(QuickAction));
        }
    }
}
