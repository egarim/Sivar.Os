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
        public new DbSet<Xaf.Sivar.Os.Module.BusinessObjects.ApplicationUser> Users { get; set; }
        public DbSet<Xaf.Sivar.Os.Module.BusinessObjects.ApplicationUserLoginInfo> UserLoginsInfo { get; set; }
     public DbSet<FileData> FileData { get; set; }
     public DbSet<ReportDataV2> ReportDataV2 { get; set; }
  public DbSet<KpiDefinition> KpiDefinitions { get; set; }
        public DbSet<KpiInstance> KpiInstances { get; set; }
     public DbSet<KpiHistoryItem> KpiHistoryItems { get; set; }
        public DbSet<KpiScorecard> KpiScorecards { get; set; }
        public DbSet<DashboardData> DashboardData { get; set; }
        public new DbSet<Event> Events { get; set; }
        public DbSet<Analysis> Analysis { get; set; }
        public DbSet<SqlScript> SqlScripts { get; set; }        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Call base to apply Sivar entity configurations
      base.OnModelCreating(modelBuilder);
            
            // XAF-specific configurations
     modelBuilder.UseDeferredDeletion(this);
        modelBuilder.UseOptimisticLock();
       modelBuilder.SetOneToManyAssociationDeleteBehavior(DeleteBehavior.SetNull, DeleteBehavior.Cascade);
     modelBuilder.HasChangeTrackingStrategy(ChangeTrackingStrategy.ChangingAndChangedNotificationsWithOriginalValues);
            modelBuilder.UsePropertyAccessMode(PropertyAccessMode.PreferFieldDuringConstruction);
    
            // Configure table names with Xaf_ prefix for XAF entities only
      
          // Base DevExpress security classes
    modelBuilder.Entity<PermissionPolicyRoleBase>().ToTable("Xaf_PermissionPolicyRoleBase");
       modelBuilder.Entity<PermissionPolicyUser>().ToTable("Xaf_PermissionPolicyUser");
            modelBuilder.Entity<PermissionPolicyRole>().ToTable("Xaf_PermissionPolicyRole");
            modelBuilder.Entity<PermissionPolicyActionPermissionObject>().ToTable("Xaf_PermissionPolicyActionPermissionObject");
            modelBuilder.Entity<PermissionPolicyNavigationPermissionObject>().ToTable("Xaf_PermissionPolicyNavigationPermissionObject");
        modelBuilder.Entity<PermissionPolicyTypePermissionObject>().ToTable("Xaf_PermissionPolicyTypePermissionObject");
   modelBuilder.Entity<PermissionPolicyObjectPermissionsObject>().ToTable("Xaf_PermissionPolicyObjectPermissionsObject");
    modelBuilder.Entity<PermissionPolicyMemberPermissionsObject>().ToTable("Xaf_PermissionPolicyMemberPermissionsObject");

       // Base DevExpress classes
     modelBuilder.Entity<FileData>().ToTable("Xaf_FileData");
       modelBuilder.Entity<Event>().ToTable("Xaf_Event");
  modelBuilder.Entity<Resource>().ToTable("Xaf_Resource");
       
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
            
            // SQL Script management
            modelBuilder.Entity<SqlScript>(b =>
            {
                b.ToTable("Xaf_SqlScripts");
                b.HasIndex(s => s.Name).IsUnique();
                b.HasIndex(s => new { s.BatchName, s.ExecutionOrder });
                b.Property(s => s.Description).IsRequired();
                b.Property(s => s.SqlText).IsRequired();
            });
 
    // XAF-specific configurations
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
