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
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Xaf.Sivar.Os.Module.DatabaseUpdate
{
    // For more typical usage scenarios, be sure to check out https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.Updating.ModuleUpdater
    public class Updater : ModuleUpdater
    {
        public Updater(IObjectSpace objectSpace, Version currentDBVersion) :
            base(objectSpace, currentDBVersion)
        {
        }
        
        public override void UpdateDatabaseBeforeUpdateSchema()
        {
            base.UpdateDatabaseBeforeUpdateSchema();
            
            // Execute SQL scripts before schema update (if any)
            //ExecuteSqlScriptBatch(SqlScriptBatches.BeforeSchemaUpdate);
        }
        
        public override void UpdateDatabaseAfterUpdateSchema()
        {
            base.UpdateDatabaseAfterUpdateSchema();
            
            // Seed SQL scripts first (before executing them)
            SeedSqlScripts();
            ObjectSpace.CommitChanges();
            
            // Execute SQL scripts after schema update
            // This runs BEFORE seeding data
            ExecuteSqlScriptBatch(SqlScriptBatches.AfterSchemaUpdate);
            
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
        
        /// <summary>
        /// Seeds all SQL scripts (called from UpdateDatabaseAfterUpdateSchema)
        /// </summary>
        private void SeedSqlScripts()
        {
            SeedConvertContentEmbeddingToVectorScript();
            SeedTimescaleDBEnableScript();
            SeedConvertToHypertablesScript();
            SeedRetentionPoliciesScript();
            SeedCompressionPoliciesScript();
            SeedFullTextSearchColumnsScript(); // Phase 3: Full-Text Search
            SeedContinuousAggregatesScript(); // Phase 7: Continuous Aggregates
            SeedSentimentAggregatesScript(); // Phase 8: Sentiment Aggregates
            SeedPostGISLocationSupportScript(); // Phase 9: PostGIS Location Services
        }
        
        /// <summary>
        /// Seeds the ConvertContentEmbeddingToVector SQL script if it doesn't exist
        /// </summary>
        private void SeedConvertContentEmbeddingToVectorScript()
        {
            const string scriptName = "ConvertContentEmbeddingToVector";
            
            // Check if script already exists
            var existingScript = ObjectSpace.GetObjectsQuery<SqlScript>()
                .FirstOrDefault(s => s.Name == scriptName);
            
            if (existingScript != null)
            {
                System.Diagnostics.Debug.WriteLine($"[SQL Scripts] Seed script '{scriptName}' already exists. Skipping.");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"[SQL Scripts] Creating seed script: {scriptName}");
            
            // Create the ConvertContentEmbeddingToVector script
            var script = ObjectSpace.CreateObject<SqlScript>();
            script.Name = scriptName;
            script.Description = "Converts ContentEmbedding column from text to vector(384) and creates HNSW index for similarity search. Required because EF Core 9.0 cannot handle pgvector types.";
            script.ExecutionOrder = 1.0m;
            script.BatchName = SqlScriptBatches.AfterSchemaUpdate;
            script.IsActive = true;
            script.RunOnce = true;
            
            // Embed the SQL script content
            script.SqlText = @"-- =====================================================
-- Script: Convert ContentEmbedding from TEXT to VECTOR(384)
-- Purpose: Convert existing ContentEmbedding column to pgvector type
--          This is REQUIRED because EF Core 9.0 cannot handle pgvector types
-- Date: October 31, 2025
-- =====================================================

-- Step 1: Ensure pgvector extension is installed
CREATE EXTENSION IF NOT EXISTS vector;

-- Step 2: Check current column type (optional - for verification)
SELECT column_name, data_type, udt_name
FROM information_schema.columns
WHERE table_name = 'Sivar_Posts' 
AND column_name = 'ContentEmbedding';

-- Step 3: Check if column exists, if not create it
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_name = 'Sivar_Posts' 
        AND column_name = 'ContentEmbedding'
    ) THEN
        ALTER TABLE ""Sivar_Posts"" 
        ADD COLUMN ""ContentEmbedding"" vector(384);
        RAISE NOTICE 'Column ContentEmbedding created as vector(384)';
    ELSE
        -- Column exists, check if it's already vector type
        IF EXISTS (
            SELECT 1 
            FROM information_schema.columns 
            WHERE table_name = 'Sivar_Posts' 
            AND column_name = 'ContentEmbedding'
            AND udt_name = 'vector'
        ) THEN
            RAISE NOTICE 'Column ContentEmbedding is already vector type';
        ELSE
            -- Convert from text/other type to vector
            ALTER TABLE ""Sivar_Posts"" 
            ALTER COLUMN ""ContentEmbedding"" TYPE vector(384) 
            USING CASE 
                WHEN ""ContentEmbedding"" IS NULL THEN NULL
                WHEN ""ContentEmbedding"" = '' THEN NULL
                ELSE ""ContentEmbedding""::vector
            END;
            RAISE NOTICE 'Column ContentEmbedding converted to vector(384)';
        END IF;
    END IF;
END $$;

-- Step 4: Create HNSW index for fast similarity search
-- Drop index if it exists first
DROP INDEX IF EXISTS ""IX_Posts_ContentEmbedding_Hnsw"";

-- Create new HNSW index with cosine similarity
CREATE INDEX ""IX_Posts_ContentEmbedding_Hnsw"" 
ON ""Sivar_Posts"" 
USING hnsw (""ContentEmbedding"" vector_cosine_ops);

-- Step 5: Verify the change
SELECT column_name, data_type, udt_name
FROM information_schema.columns
WHERE table_name = 'Sivar_Posts' 
AND column_name = 'ContentEmbedding';

-- Step 6: Verify the index
SELECT indexname, indexdef
FROM pg_indexes
WHERE tablename = 'Sivar_Posts'
AND indexname = 'IX_Posts_ContentEmbedding_Hnsw';

-- =====================================================
-- IMPORTANT NOTES:
-- - This script is idempotent (safe to run multiple times)
-- - EF Core 9.0 CANNOT handle pgvector types - column MUST be ignored in PostConfiguration.cs
-- - Updates to ContentEmbedding MUST use raw SQL (see PostRepository.UpdateContentEmbeddingAsync)
-- - The column is ignored by EF Core but exists in the database
-- - HNSW index improves performance for similarity search queries using <=> operator
-- =====================================================";
            
            System.Diagnostics.Debug.WriteLine($"[SQL Scripts] Seed script '{scriptName}' created successfully.");
        }
        
        /// <summary>
        /// Seeds the EnableTimescaleDB SQL script if it doesn't exist
        /// </summary>
        private void SeedTimescaleDBEnableScript()
        {
            const string scriptName = "EnableTimescaleDB";
            
            // Check if script already exists
            var existingScript = ObjectSpace.GetObjectsQuery<SqlScript>()
                .FirstOrDefault(s => s.Name == scriptName);
            
            if (existingScript != null)
            {
                System.Diagnostics.Debug.WriteLine($"[SQL Scripts] Seed script '{scriptName}' already exists. Skipping.");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"[SQL Scripts] Creating seed script: {scriptName}");
            
            var script = ObjectSpace.CreateObject<SqlScript>();
            script.Name = scriptName;
            script.Description = "Enables TimescaleDB extension for time-series optimization";
            script.ExecutionOrder = 2.0m;
            script.BatchName = SqlScriptBatches.AfterSchemaUpdate;
            script.IsActive = true;
            script.RunOnce = true;
            
            // Load SQL from embedded resource or file
            script.SqlText = LoadScriptFromFile("EnableTimescaleDB.sql");
            
            System.Diagnostics.Debug.WriteLine($"[SQL Scripts] Seed script '{scriptName}' created successfully.");
        }
        
        /// <summary>
        /// Seeds the ConvertToHypertables SQL script if it doesn't exist
        /// </summary>
        private void SeedConvertToHypertablesScript()
        {
            const string scriptName = "ConvertToHypertables";
            
            // Check if script already exists
            var existingScript = ObjectSpace.GetObjectsQuery<SqlScript>()
                .FirstOrDefault(s => s.Name == scriptName);
            
            if (existingScript != null)
            {
                System.Diagnostics.Debug.WriteLine($"[SQL Scripts] Seed script '{scriptName}' already exists. Skipping.");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"[SQL Scripts] Creating seed script: {scriptName}");
            
            var script = ObjectSpace.CreateObject<SqlScript>();
            script.Name = scriptName;
            script.Description = "Converts time-series tables to TimescaleDB hypertables";
            script.ExecutionOrder = 3.0m;
            script.BatchName = SqlScriptBatches.AfterSchemaUpdate;
            script.IsActive = true;
            script.RunOnce = true;
            
            script.SqlText = LoadScriptFromFile("ConvertToHypertables.sql");
            
            System.Diagnostics.Debug.WriteLine($"[SQL Scripts] Seed script '{scriptName}' created successfully.");
        }
        
        /// <summary>
        /// Seeds the AddRetentionPolicies SQL script if it doesn't exist
        /// </summary>
        private void SeedRetentionPoliciesScript()
        {
            const string scriptName = "AddRetentionPolicies";
            
            // Check if script already exists
            var existingScript = ObjectSpace.GetObjectsQuery<SqlScript>()
                .FirstOrDefault(s => s.Name == scriptName);
            
            if (existingScript != null)
            {
                System.Diagnostics.Debug.WriteLine($"[SQL Scripts] Seed script '{scriptName}' already exists. Skipping.");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"[SQL Scripts] Creating seed script: {scriptName}");
            
            var script = ObjectSpace.CreateObject<SqlScript>();
            script.Name = scriptName;
            script.Description = "Adds data retention policies to automatically drop old chunks";
            script.ExecutionOrder = 4.0m;
            script.BatchName = SqlScriptBatches.AfterSchemaUpdate;
            script.IsActive = true;
            script.RunOnce = true;
            
            script.SqlText = LoadScriptFromFile("AddRetentionPolicies.sql");
            
            System.Diagnostics.Debug.WriteLine($"[SQL Scripts] Seed script '{scriptName}' created successfully.");
        }
        
        /// <summary>
        /// Seeds the AddCompressionPolicies SQL script if it doesn't exist
        /// </summary>
        private void SeedCompressionPoliciesScript()
        {
            const string scriptName = "AddCompressionPolicies";
            
            // Check if script already exists
            var existingScript = ObjectSpace.GetObjectsQuery<SqlScript>()
                .FirstOrDefault(s => s.Name == scriptName);
            
            if (existingScript != null)
            {
                System.Diagnostics.Debug.WriteLine($"[SQL Scripts] Seed script '{scriptName}' already exists. Skipping.");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"[SQL Scripts] Creating seed script: {scriptName}");
            
            var script = ObjectSpace.CreateObject<SqlScript>();
            script.Name = scriptName;
            script.Description = "Adds compression policies to automatically compress old chunks";
            script.ExecutionOrder = 5.0m;
            script.BatchName = SqlScriptBatches.AfterSchemaUpdate;
            script.IsActive = true;
            script.RunOnce = true;
            
            script.SqlText = LoadScriptFromFile("AddCompressionPolicies.sql");
            
            System.Diagnostics.Debug.WriteLine($"[SQL Scripts] Seed script '{scriptName}' created successfully.");
        }
        
        /// <summary>
        /// Loads SQL script content from file in Scripts directory
        /// </summary>
        /// <param name="fileName">Name of SQL file (e.g., "EnableTimescaleDB.sql")</param>
        /// <returns>SQL script content</returns>
        private string LoadScriptFromFile(string fileName)
        {
            try
            {
                // Get the solution directory (navigate up from the executing assembly)
                var assemblyLocation = Assembly.GetExecutingAssembly().Location;
                var assemblyDir = Path.GetDirectoryName(assemblyLocation);
                
                // Navigate to solution root: bin/Debug/net9.0 -> ../../../
                var solutionRoot = Path.GetFullPath(Path.Combine(assemblyDir!, "..", "..", "..", "..", ".."));
                
                // Path to Scripts directory
                var scriptsDir = Path.Combine(solutionRoot, "Sivar.Os.Data", "Scripts");
                var scriptPath = Path.Combine(scriptsDir, fileName);
                
                if (!File.Exists(scriptPath))
                {
                    throw new FileNotFoundException($"SQL script file not found: {scriptPath}");
                }
                
                var content = File.ReadAllText(scriptPath);
                System.Diagnostics.Debug.WriteLine($"[SQL Scripts] Loaded script from: {scriptPath} ({content.Length} chars)");
                return content;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SQL Scripts] Error loading script {fileName}: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Executes SQL scripts from database by batch name
        /// Scripts are ordered by ExecutionOrder and filtered by IsActive
        /// </summary>
        /// <param name="batchName">Batch name (e.g., "AfterSchemaUpdate", "BeforeSchemaUpdate")</param>
        private void ExecuteSqlScriptBatch(string batchName)
        {
            // Get the DbContext from ObjectSpace
            var efObjectSpace = ObjectSpace as DevExpress.ExpressApp.EFCore.EFCoreObjectSpace;
            if (efObjectSpace == null)
            {
                System.Diagnostics.Debug.WriteLine($"[SQL Scripts] ObjectSpace is not EFCoreObjectSpace. Cannot execute SQL scripts.");
                return;
            }

            var dbContext = efObjectSpace.DbContext;

            // Query scripts from database
            var scripts = ObjectSpace.GetObjectsQuery<SqlScript>()
                .Where(s => s.BatchName == batchName && s.IsActive)
                .OrderBy(s => s.ExecutionOrder)
                .ToList();

            if (!scripts.Any())
            {
                System.Diagnostics.Debug.WriteLine($"[SQL Scripts] No scripts found for batch: {batchName}");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[SQL Scripts] Found {scripts.Count} script(s) for batch: {batchName}");

            foreach (var script in scripts)
            {
                try
                {
                    // Check if script should run only once
                    if (script.RunOnce && script.ExecutionCount > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SQL Scripts] Skipping (already executed): {script.Name}");
                        continue;
                    }

                    System.Diagnostics.Debug.WriteLine($"[SQL Scripts] Executing: {script.Name} (Order: {script.ExecutionOrder})");

                    // Execute the raw SQL
                    dbContext.Database.ExecuteSqlRaw(script.SqlText);

                    // Update execution tracking
                    script.LastExecutedAt = DateTime.UtcNow;
                    script.ExecutionCount++;
                    script.LastExecutionError = null; // Clear previous errors

                    System.Diagnostics.Debug.WriteLine($"[SQL Scripts] Successfully executed: {script.Name}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SQL Scripts] Error executing {script.Name}: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"[SQL Scripts] Stack trace: {ex.StackTrace}");

                    // Update error tracking
                    script.LastExecutionError = $"{ex.Message}\n\nStack Trace:\n{ex.StackTrace}";

                    // Don't throw - continue with other scripts and seeding
                }
            }

            // Save execution tracking changes
            ObjectSpace.CommitChanges();
        }
        
        /// <summary>
        /// Seeds the AddFullTextSearchColumns SQL script if it doesn't exist
        /// Phase 3: PostgreSQL Full-Text Search
        /// </summary>
        private void SeedFullTextSearchColumnsScript()
        {
            const string scriptName = "AddFullTextSearchColumns";
            
            // Check if script already exists
            var existingScript = ObjectSpace.GetObjectsQuery<SqlScript>()
                .FirstOrDefault(s => s.Name == scriptName);
            
            if (existingScript != null)
            {
                System.Diagnostics.Debug.WriteLine($"[SQL Scripts] Seed script '{scriptName}' already exists. Skipping.");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"[SQL Scripts] Creating seed script: {scriptName}");
            
            var script = ObjectSpace.CreateObject<SqlScript>();
            script.Name = scriptName;
            script.Description = "Adds tsvector columns for language-aware and language-agnostic full-text search on Posts table. Creates GIN indexes for fast full-text search queries.";
            script.ExecutionOrder = 6.0m; // After TimescaleDB scripts (2.0-5.0)
            script.BatchName = SqlScriptBatches.AfterSchemaUpdate;
            script.IsActive = true;
            script.RunOnce = true;
            
            // Load SQL script from embedded resource
            script.SqlText = LoadScriptFromFile("AddFullTextSearchColumns.sql");
            
            System.Diagnostics.Debug.WriteLine($"[SQL Scripts] Seed script '{scriptName}' created successfully.");
        }
        
        /// <summary>
        /// Seeds the AddContinuousAggregates SQL script if it doesn't exist
        /// Phase 7: TimescaleDB Continuous Aggregates
        /// </summary>
        private void SeedContinuousAggregatesScript()
        {
            const string scriptName = "AddContinuousAggregates";
            
            // Check if script already exists
            var existingScript = ObjectSpace.GetObjectsQuery<SqlScript>()
                .FirstOrDefault(s => s.Name == scriptName);
            
            if (existingScript != null)
            {
                System.Diagnostics.Debug.WriteLine($"[SQL Scripts] Seed script '{scriptName}' already exists. Skipping.");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"[SQL Scripts] Creating seed script: {scriptName}");
            
            var script = ObjectSpace.CreateObject<SqlScript>();
            script.Name = scriptName;
            script.Description = "Creates TimescaleDB continuous aggregates (materialized views) for real-time analytics: post metrics, activity metrics, user engagement, and post engagement.";
            script.ExecutionOrder = 7.0m; // After full-text search (6.0)
            script.BatchName = SqlScriptBatches.AfterSchemaUpdate;
            script.IsActive = true;
            script.RunOnce = true;
            
            // Load SQL script from file
            script.SqlText = LoadScriptFromFile("AddContinuousAggregates.sql");
            
            System.Diagnostics.Debug.WriteLine($"[SQL Scripts] Seed script '{scriptName}' created successfully.");
        }
        
        /// <summary>
        /// Seeds the AddSentimentAggregates SQL script if it doesn't exist
        /// Phase 8: Sentiment Analysis Continuous Aggregates (City & Country)
        /// </summary>
        private void SeedSentimentAggregatesScript()
        {
            const string scriptName = "AddSentimentAggregates";
            
            // Check if script already exists
            var existingScript = ObjectSpace.GetObjectsQuery<SqlScript>()
                .FirstOrDefault(s => s.Name == scriptName);
            
            if (existingScript != null)
            {
                System.Diagnostics.Debug.WriteLine($"[SQL Scripts] Seed script '{scriptName}' already exists. Skipping.");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"[SQL Scripts] Creating seed script: {scriptName}");
            
            var script = ObjectSpace.CreateObject<SqlScript>();
            script.Name = scriptName;
            script.Description = "Creates sentiment analysis continuous aggregates for city-level and country-level sentiment metrics. Enables location-based sentiment tracking and moderation.";
            script.ExecutionOrder = 8.0m; // After continuous aggregates (7.0)
            script.BatchName = SqlScriptBatches.AfterSchemaUpdate;
            script.IsActive = true;
            script.RunOnce = true;
            
            // Load SQL script from file
            script.SqlText = LoadScriptFromFile("AddSentimentAggregates.sql");
            
            System.Diagnostics.Debug.WriteLine($"[SQL Scripts] Seed script '{scriptName}' created successfully.");
        }
        
        /// <summary>
        /// Seeds the AddPostGISLocationSupport SQL script if it doesn't exist
        /// Phase 9: PostGIS Location Services (Geocoding, Proximity Search)
        /// </summary>
        private void SeedPostGISLocationSupportScript()
        {
            const string scriptName = "AddPostGISLocationSupport";
            
            // Check if script already exists
            var existingScript = ObjectSpace.GetObjectsQuery<SqlScript>()
                .FirstOrDefault(s => s.Name == scriptName);
            
            if (existingScript != null)
            {
                System.Diagnostics.Debug.WriteLine($"[SQL Scripts] Seed script '{scriptName}' already exists. Skipping.");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"[SQL Scripts] Creating seed script: {scriptName}");
            
            var script = ObjectSpace.CreateObject<SqlScript>();
            script.Name = scriptName;
            script.Description = "Adds PostGIS extension and GeoLocation columns to Profiles and Posts for location-based features. Creates spatial indexes, distance calculation functions, and proximity search functions.";
            script.ExecutionOrder = 9.0m; // After sentiment aggregates (8.0)
            script.BatchName = SqlScriptBatches.AfterSchemaUpdate;
            script.IsActive = true;
            script.RunOnce = true;
            
            // Load SQL script from file
            script.SqlText = LoadScriptFromFile("003_AddPostGISLocationSupport.sql");
            
            System.Diagnostics.Debug.WriteLine($"[SQL Scripts] Seed script '{scriptName}' created successfully.");
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
