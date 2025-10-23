using DevExpress.ExpressApp.Design;
using DevExpress.ExpressApp.EFCore.DesignTime;
using DevExpress.ExpressApp.EFCore.Updating;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.BaseImpl.EF.Kpi;
using DevExpress.Persistent.BaseImpl.EF.PermissionPolicy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Sivar.Os.Data.Context;

namespace Xaf.Sivar.Os.Module.BusinessObjects
{
    [TypesInfoInitializer(typeof(DbContextTypesInfoInitializer<OsEFCoreDbContext>))]
    public class OsEFCoreDbContext : SivarDbContext
    {
        static OsEFCoreDbContext()
        {
            // Enable legacy timestamp behavior for PostgreSQL
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        }

        public OsEFCoreDbContext(DbContextOptions<OsEFCoreDbContext> options) : base(options)
        {
        }
        //public DbSet<ModuleInfo> ModulesInfo { get; set; }
        public DbSet<ModelDifference> ModelDifferences { get; set; }
        public DbSet<ModelDifferenceAspect> ModelDifferenceAspects { get; set; }
        public DbSet<PermissionPolicyRole> Roles { get; set; }
        public DbSet<Xaf.Sivar.Os.Module.BusinessObjects.ApplicationUser> Users { get; set; }
        public DbSet<Xaf.Sivar.Os.Module.BusinessObjects.ApplicationUserLoginInfo> UserLoginsInfo { get; set; }
        public DbSet<FileData> FileData { get; set; }
        public DbSet<ReportDataV2> ReportDataV2 { get; set; }
        public DbSet<KpiDefinition> KpiDefinitions { get; set; }
        public DbSet<KpiInstance> KpiInstances { get; set; }
        public DbSet<KpiHistoryItem> KpiHistoryItems { get; set; }
        public DbSet<KpiScorecard> KpiScorecards { get; set; }
        public DbSet<DashboardData> DashboardData { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Analysis> Analysis { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.UseDeferredDeletion(this);
            modelBuilder.UseOptimisticLock();
            modelBuilder.SetOneToManyAssociationDeleteBehavior(DeleteBehavior.SetNull, DeleteBehavior.Cascade);
            modelBuilder.HasChangeTrackingStrategy(ChangeTrackingStrategy.ChangingAndChangedNotificationsWithOriginalValues);
            modelBuilder.UsePropertyAccessMode(PropertyAccessMode.PreferFieldDuringConstruction);
            
            // Configure table names with Xaf_ prefix for XAF entities only
            
            // Ultimate base classes
            //modelBuilder.Entity<BaseObject>().ToTable("Xaf_BaseObject");
            
            // Base DevExpress security classes
            modelBuilder.Entity<PermissionPolicyRoleBase>().ToTable("Xaf_PermissionPolicyRoleBase");
            modelBuilder.Entity<PermissionPolicyUser>().ToTable("Xaf_PermissionPolicyUser");
            modelBuilder.Entity<PermissionPolicyRole>().ToTable("Xaf_PermissionPolicyRole");
            modelBuilder.Entity<PermissionPolicyActionPermissionObject>().ToTable("Xaf_PermissionPolicyActionPermissionObject");
            modelBuilder.Entity<PermissionPolicyNavigationPermissionObject>().ToTable("Xaf_PermissionPolicyNavigationPermissionObject");
            modelBuilder.Entity<PermissionPolicyTypePermissionObject>().ToTable("Xaf_PermissionPolicyTypePermissionObject");
            modelBuilder.Entity<PermissionPolicyObjectPermissionsObject>().ToTable("Xaf_PermissionPolicyObjectPermissionsObject");
            modelBuilder.Entity<PermissionPolicyMemberPermissionsObject>().ToTable("Xaf_PermissionPolicyMemberPermissionsObject");

            //StillMissng:
            //PermissionPolicyRolePermissionPolicyUser


            // Audit trail classes (if using Audit Trail module)
            //modelBuilder.Entity<AuditDataItemPersistent>().ToTable("Xaf_AuditDataItemPersistent"); // Not in base namespace
            //modelBuilder.Entity<AuditedObjectWeakReference>().ToTable("Xaf_AuditedObjectWeakReference"); // Not in base namespace

            // Base DevExpress classes
            modelBuilder.Entity<FileData>().ToTable("Xaf_FileData");
            //modelBuilder.Entity<FileAttachment>().ToTable("Xaf_FileAttachment");
            modelBuilder.Entity<Event>().ToTable("Xaf_Event");
            modelBuilder.Entity<Resource>().ToTable("Xaf_Resource");
            //modelBuilder.Entity<EventResource>().ToTable("Xaf_EventResource"); // Not available in EF namespace
            
            // Model customization classes
            modelBuilder.Entity<ModelDifference>().ToTable("Xaf_ModelDifference");
            modelBuilder.Entity<ModelDifferenceAspect>().ToTable("Xaf_ModelDifferenceAspect");
            
            // Application-specific classes derived from DevExpress base classes
            modelBuilder.Entity<Xaf.Sivar.Os.Module.BusinessObjects.ApplicationUser>().ToTable("Xaf_ApplicationUser");
            modelBuilder.Entity<Xaf.Sivar.Os.Module.BusinessObjects.ApplicationUserLoginInfo>().ToTable("Xaf_ApplicationUserLoginInfo");
            
            // Reporting and analytics classes
            modelBuilder.Entity<ReportDataV2>().ToTable("Xaf_ReportDataV2");
            modelBuilder.Entity<DashboardData>().ToTable("Xaf_DashboardData");
            modelBuilder.Entity<Analysis>().ToTable("Xaf_Analysis");
            
            // KPI classes
            modelBuilder.Entity<KpiDefinition>().ToTable("Xaf_KpiDefinition");
            modelBuilder.Entity<KpiInstance>().ToTable("Xaf_KpiInstance");
            modelBuilder.Entity<KpiHistoryItem>().ToTable("Xaf_KpiHistoryItem");
            modelBuilder.Entity<KpiScorecard>().ToTable("Xaf_KpiScorecard");
            //modelBuilder.Entity<KpiInstanceKpiScorecard>().ToTable("Xaf_KpiInstanceKpiScorecard"); // Join table managed by EF
            
            modelBuilder.Entity<Xaf.Sivar.Os.Module.BusinessObjects.ApplicationUserLoginInfo>(b =>
            {
                b.HasIndex(nameof(DevExpress.ExpressApp.Security.ISecurityUserLoginInfo.LoginProviderName), nameof(DevExpress.ExpressApp.Security.ISecurityUserLoginInfo.ProviderUserKey)).IsUnique();
            });
            modelBuilder.Entity<ModelDifference>()
                .HasMany(t => t.Aspects)
                .WithOne(t => t.Owner)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
