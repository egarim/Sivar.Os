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
using Microsoft.Build.Tasks.Deployment.Bootstrapper;
using Microsoft.CodeAnalysis;
using Sivar.Os.Shared.Entities;
using System.ComponentModel;

namespace Xaf.Sivar.Os.Module
{
    // For more typical usage scenarios, be sure to check out https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.ModuleBase.
    /// <summary>
    /// XAF Admin Backend Module - Provides administration UI for all Sivar.Os entities.
    /// This is the admin dashboard for managing users, content, AI chat, and system configuration.
    /// </summary>
    public sealed class OsModule : ModuleBase
    {
        // Navigation Group Constants
        private const string NavGroupUsers = "Users & Profiles";
        private const string NavGroupContent = "Content";
        private const string NavGroupAIChat = "AI Chat";
        private const string NavGroupAIConfig = "AI Configuration";
        private const string NavGroupBusiness = "Business";
        private const string NavGroupBookings = "Bookings";
        private const string NavGroupScheduling = "Scheduling";
        private const string NavGroupWaitingList = "Waiting List";
        private const string NavGroupSearch = "Search & Ranking";
        private const string NavGroupSystem = "System";

        public OsModule()
        {
            //
            // OsModule - Admin Backend for Sivar.Os
            //
            // Entity Registration by Navigation Group:
            //
            // === Users & Profiles ===
            AdditionalExportedTypes.Add(typeof(User));
            AdditionalExportedTypes.Add(typeof(Profile));
            AdditionalExportedTypes.Add(typeof(ProfileType));
            AdditionalExportedTypes.Add(typeof(ProfileFollower));
            AdditionalExportedTypes.Add(typeof(ProfileBookmark));
            AdditionalExportedTypes.Add(typeof(ProfileEmotionSummary));

            // === Content ===
            AdditionalExportedTypes.Add(typeof(Post));
            AdditionalExportedTypes.Add(typeof(PostAttachment));
            AdditionalExportedTypes.Add(typeof(Comment));
            AdditionalExportedTypes.Add(typeof(Reaction));
            AdditionalExportedTypes.Add(typeof(CategoryDefinition));

            // === AI Chat ===
            AdditionalExportedTypes.Add(typeof(Conversation));
            AdditionalExportedTypes.Add(typeof(ChatMessage));
            AdditionalExportedTypes.Add(typeof(SavedResult));
            AdditionalExportedTypes.Add(typeof(ChatTokenUsage));
            AdditionalExportedTypes.Add(typeof(AiModelPricing));

            // === AI Configuration ===
            AdditionalExportedTypes.Add(typeof(ChatBotSettings));
            AdditionalExportedTypes.Add(typeof(AgentCapability));
            AdditionalExportedTypes.Add(typeof(CapabilityParameter));
            AdditionalExportedTypes.Add(typeof(QuickAction));
            AdditionalExportedTypes.Add(typeof(AgentConfiguration));
            AdditionalExportedTypes.Add(typeof(AgentTool));

            // === Business ===
            AdditionalExportedTypes.Add(typeof(BusinessContactInfo));
            AdditionalExportedTypes.Add(typeof(ContactType));
            AdditionalExportedTypes.Add(typeof(AdTransaction));

            // === Bookings (Resource Booking System) ===
            AdditionalExportedTypes.Add(typeof(BookableResource));
            AdditionalExportedTypes.Add(typeof(ResourceService));
            AdditionalExportedTypes.Add(typeof(ResourceAvailability));
            AdditionalExportedTypes.Add(typeof(ResourceException));
            AdditionalExportedTypes.Add(typeof(ResourceBooking));

            // === Scheduling (Events & Calendar) ===
            AdditionalExportedTypes.Add(typeof(ScheduleEvent));
            AdditionalExportedTypes.Add(typeof(EventAttendee));
            AdditionalExportedTypes.Add(typeof(EventReminder));
            AdditionalExportedTypes.Add(typeof(RecurrenceRule));

            // === Waiting List ===
            AdditionalExportedTypes.Add(typeof(WaitingListEntry));
            AdditionalExportedTypes.Add(typeof(PhoneVerification));

            // === Search & Ranking ===
            AdditionalExportedTypes.Add(typeof(SearchResult));
            AdditionalExportedTypes.Add(typeof(UserSearchBehavior));
            AdditionalExportedTypes.Add(typeof(RankingConfiguration));

            // === System ===
            AdditionalExportedTypes.Add(typeof(Activity));
            AdditionalExportedTypes.Add(typeof(Notification));

            // === XAF-specific Business Objects ===
            AdditionalExportedTypes.Add(typeof(Xaf.Sivar.Os.Module.BusinessObjects.SeederLog));
            AdditionalExportedTypes.Add(typeof(Xaf.Sivar.Os.Module.BusinessObjects.ApplicationUser));
            AdditionalExportedTypes.Add(typeof(DevExpress.Persistent.BaseImpl.EF.PermissionPolicy.PermissionPolicyRole));
            AdditionalExportedTypes.Add(typeof(DevExpress.Persistent.BaseImpl.EF.ModelDifference));
            AdditionalExportedTypes.Add(typeof(DevExpress.Persistent.BaseImpl.EF.ModelDifferenceAspect));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.SystemModule.SystemModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.Security.SecurityModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.Chart.ChartModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.CloneObject.CloneObjectModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.ConditionalAppearance.ConditionalAppearanceModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.Dashboards.DashboardsModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.Notifications.NotificationsModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.Office.OfficeModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.PivotGrid.PivotGridModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.ReportsV2.ReportsModuleV2));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.Scheduler.SchedulerModuleBase));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.Validation.ValidationModule));
            DevExpress.ExpressApp.Security.SecurityModule.UsedExportedTypes = DevExpress.Persistent.Base.UsedExportedTypes.Custom;
            AdditionalExportedTypes.Add(typeof(DevExpress.Persistent.BaseImpl.EF.FileData));
            AdditionalExportedTypes.Add(typeof(DevExpress.Persistent.BaseImpl.EF.FileAttachment));
            AdditionalExportedTypes.Add(typeof(DevExpress.Persistent.BaseImpl.EF.Event));
            AdditionalExportedTypes.Add(typeof(DevExpress.Persistent.BaseImpl.EF.Resource));
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
        private void ConfigureTypeWithDefaultClassOptions(ITypesInfo typesInfo, Type type, string? navigationGroup = null, Action<ITypeInfo>? additionalConfig = null)
        {
            var typeInfo = typesInfo.FindTypeInfo(type);
            if (typeInfo != null)
            {
                typeInfo.AddAttribute(new DefaultClassOptionsAttribute());
                typeInfo.AddAttribute(new ModelDefaultAttribute("IsCloneable", "True"));
                
                // Add navigation group if specified
                if (!string.IsNullOrEmpty(navigationGroup))
                {
                    typeInfo.AddAttribute(new NavigationItemAttribute(navigationGroup));
                }
                
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

            // ============================================
            // Navigation Group: Users & Profiles
            // ============================================
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(User), NavGroupUsers);
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(Profile), NavGroupUsers);
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(ProfileType), NavGroupUsers, typeInfo =>
            {
                typeInfo.Members.FirstOrDefault(m => m.Name == nameof(ProfileType.FeatureFlags))?.AddAttribute(new FieldSizeAttribute(FieldSizeAttribute.Unlimited));
            });
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(ProfileFollower), NavGroupUsers);
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(ProfileBookmark), NavGroupUsers);
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(ProfileEmotionSummary), NavGroupUsers);

            // ============================================
            // Navigation Group: Content
            // ============================================
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(Post), NavGroupContent);
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(PostAttachment), NavGroupContent);
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(Comment), NavGroupContent);
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(Reaction), NavGroupContent);
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(CategoryDefinition), NavGroupContent);

            // ============================================
            // Navigation Group: AI Chat
            // ============================================
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(Conversation), NavGroupAIChat, typeInfo =>
            {
                // Format TotalCost as currency with 6 decimal places for micro-costs
                typeInfo.Members.FirstOrDefault(m => m.Name == nameof(Conversation.TotalCost))
                    ?.AddAttribute(new ModelDefaultAttribute("DisplayFormat", "{0:$0.000000}"));
            });
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(ChatMessage), NavGroupAIChat);
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(SavedResult), NavGroupAIChat);
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(ChatTokenUsage), NavGroupAIChat, typeInfo =>
            {
                // Format EstimatedCost as currency with 6 decimal places for micro-costs
                typeInfo.Members.FirstOrDefault(m => m.Name == nameof(ChatTokenUsage.EstimatedCost))
                    ?.AddAttribute(new ModelDefaultAttribute("DisplayFormat", "{0:$0.000000}"));
            });
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(AiModelPricing), NavGroupAIChat, typeInfo =>
            {
                // Format all cost fields as currency with 6 decimal places
                typeInfo.Members.FirstOrDefault(m => m.Name == nameof(AiModelPricing.InputCostPer1M))
                    ?.AddAttribute(new ModelDefaultAttribute("DisplayFormat", "{0:$0.000000}"));
                typeInfo.Members.FirstOrDefault(m => m.Name == nameof(AiModelPricing.OutputCostPer1M))
                    ?.AddAttribute(new ModelDefaultAttribute("DisplayFormat", "{0:$0.000000}"));
                typeInfo.Members.FirstOrDefault(m => m.Name == nameof(AiModelPricing.BatchInputCostPer1M))
                    ?.AddAttribute(new ModelDefaultAttribute("DisplayFormat", "{0:$0.000000}"));
                typeInfo.Members.FirstOrDefault(m => m.Name == nameof(AiModelPricing.BatchOutputCostPer1M))
                    ?.AddAttribute(new ModelDefaultAttribute("DisplayFormat", "{0:$0.000000}"));
            });

            // ============================================
            // Navigation Group: AI Configuration
            // ============================================
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(ChatBotSettings), NavGroupAIConfig);
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(AgentCapability), NavGroupAIConfig);
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(CapabilityParameter), NavGroupAIConfig);
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(QuickAction), NavGroupAIConfig);
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(AgentConfiguration), NavGroupAIConfig);
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(AgentTool), NavGroupAIConfig);

            // ============================================
            // Navigation Group: Business
            // ============================================
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(BusinessContactInfo), NavGroupBusiness);
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(ContactType), NavGroupBusiness);
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(AdTransaction), NavGroupBusiness);

            // ============================================
            // Navigation Group: Bookings (Resource Booking System)
            // ============================================
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(BookableResource), NavGroupBookings);
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(ResourceService), NavGroupBookings);
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(ResourceAvailability), NavGroupBookings);
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(ResourceException), NavGroupBookings);
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(ResourceBooking), NavGroupBookings);

            // ============================================
            // Navigation Group: Scheduling (Events & Calendar)
            // ============================================
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(ScheduleEvent), NavGroupScheduling);
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(EventAttendee), NavGroupScheduling);
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(EventReminder), NavGroupScheduling);
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(RecurrenceRule), NavGroupScheduling);

            // ============================================
            // Navigation Group: Waiting List
            // ============================================
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(WaitingListEntry), NavGroupWaitingList);
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(PhoneVerification), NavGroupWaitingList);

            // ============================================
            // Navigation Group: Search & Ranking
            // ============================================
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(SearchResult), NavGroupSearch);
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(UserSearchBehavior), NavGroupSearch);
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(RankingConfiguration), NavGroupSearch);

            // ============================================
            // Navigation Group: System
            // ============================================
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(Activity), NavGroupSystem);
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(Notification), NavGroupSystem);
            ConfigureTypeWithDefaultClassOptions(typesInfo, typeof(BusinessObjects.SeederLog), NavGroupSystem);
        }
    }
}
