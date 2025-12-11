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
using Sivar.Os.Shared.DTOs.ValueObjects;
using Sivar.Os.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Text.Json;

namespace Xaf.Sivar.Os.Module.DatabaseUpdate
{
    // For more typical usage scenarios, be sure to check out https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.Updating.ModuleUpdater
    public class Updater : ModuleUpdater
    {
        // Collection to store post embeddings for raw SQL update after EF Core commit
        // Key: Post ID, Value: Embedding string in PostgreSQL vector format "[0.1,0.2,...]"
        private readonly Dictionary<Guid, string> _pendingEmbeddings = new();
        
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
            
            // Run async seeding in a synchronous context
            Task.Run(async () => await UpdateDatabaseAfterUpdateSchemaAsync()).Wait();
        }

        /// <summary>
        /// Async version of UpdateDatabaseAfterUpdateSchema for seeding operations
        /// </summary>
        private async Task UpdateDatabaseAfterUpdateSchemaAsync()
        {
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
            
            // Seed default profiles for users (runs in both DEBUG and RELEASE)
            SeedDefaultProfiles();

            ObjectSpace.CommitChanges(); //This line persists created object(s);
            
            // Seed demo data from DemoData folder (runs in both DEBUG and RELEASE)
            await SeedDemoDataAsync();
            
            ObjectSpace.CommitChanges(); //This line persists demo data
            
            // Apply content embeddings via raw SQL (EF Core ignores ContentEmbedding property)
            // This must be called AFTER CommitChanges so posts exist in the database
            ApplyPendingEmbeddingsViaSql();
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
        /// Applies pending content embeddings via raw SQL.
        /// Required because EF Core ignores the ContentEmbedding property (see PostConfiguration.cs).
        /// This method must be called AFTER ObjectSpace.CommitChanges() to ensure posts exist in database.
        /// </summary>
        private void ApplyPendingEmbeddingsViaSql()
        {
            if (_pendingEmbeddings.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[Updater] No pending embeddings to apply.");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[Updater] 📊 Applying {_pendingEmbeddings.Count} embeddings via raw SQL...");
            System.Diagnostics.Debug.WriteLine($"[Updater] 📊 EXPECTED: 140 embeddings (50 restaurants + 40 entertainment + 10 tourism + 20 government + 20 services)");
            System.Diagnostics.Debug.WriteLine($"[Updater] 📊 ACTUAL: {_pendingEmbeddings.Count} embeddings queued");

            // Get the DbContext from ObjectSpace
            var efObjectSpace = ObjectSpace as DevExpress.ExpressApp.EFCore.EFCoreObjectSpace;
            if (efObjectSpace == null)
            {
                System.Diagnostics.Debug.WriteLine("[Updater] ❌ ObjectSpace is not EFCoreObjectSpace. Cannot apply embeddings.");
                return;
            }

            var dbContext = efObjectSpace.DbContext;
            int successCount = 0;
            int errorCount = 0;

            foreach (var kvp in _pendingEmbeddings)
            {
                try
                {
                    var postId = kvp.Key;
                    var embedding = kvp.Value;
                    
                    // Escape single quotes in embedding string (though vectors shouldn't have them)
                    var safeEmbedding = embedding.Replace("'", "''");
                    
                    // Update the embedding via raw SQL with vector cast
                    var sql = $@"UPDATE ""Sivar_Posts"" 
                                 SET ""ContentEmbedding"" = '{safeEmbedding}'::vector 
                                 WHERE ""Id"" = '{postId}'";
                    
                    dbContext.Database.ExecuteSqlRaw(sql);
                    successCount++;
                }
                catch (Exception ex)
                {
                    errorCount++;
                    System.Diagnostics.Debug.WriteLine($"[Updater] ❌ Error applying embedding for {kvp.Key}: {ex.Message}");
                }
            }

            System.Diagnostics.Debug.WriteLine($"[Updater] ✅ Applied {successCount} embeddings, {errorCount} errors.");
            
            // Clear the pending embeddings
            _pendingEmbeddings.Clear();
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
    ""AllowsBlogging"": true,
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
    ""AllowsBlogging"": true,
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
    ""AllowsBlogging"": true,
                   ""MaxBioLength"": 2000
            }";
                organizationProfileType.CreatedAt = now;
                organizationProfileType.UpdatedAt = now;
            }
        }
        
        /// <summary>
        /// Seeds default profiles for known users from users.txt
        /// Creates a Personal profile for each user if they don't already have one
        /// </summary>
        void SeedDefaultProfiles()
        {
            var now = DateTime.UtcNow;
            var personalProfileTypeId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            
            System.Diagnostics.Debug.WriteLine("[Updater] Starting SeedDefaultProfiles...");
            
            // Get the PersonalProfile type to ensure it exists
            var personalProfileType = ObjectSpace.FirstOrDefault<ProfileType>(pt => pt.Id == personalProfileTypeId);
            if (personalProfileType == null)
            {
                System.Diagnostics.Debug.WriteLine("[Updater] PersonalProfile type not found. Skipping profile seeding.");
                return;
            }
            
            // Define users from users.txt
            var usersToSeed = new[]
            {
                new { Name = "Roberto Guzman", KeycloakId = "20b52564-e505-404a-bd7a-be5916c8e0a4", Handle = "roberto-guzman" },
                new { Name = "Jaime Macias", KeycloakId = "b65fd3b2-e181-4830-8678-fff5f96492b9", Handle = "jaime-macias" },
                new { Name = "Joche Ojeda", KeycloakId = "28b46a88-d191-4c63-8812-1bb8f3332228", Handle = "joche-ojeda" },
                new { Name = "Oscar Ojeda", KeycloakId = "ea06c2da-07f3-4606-aa65-46a67cb0a471", Handle = "oscar-ojeda" }
            };
            
            foreach (var userInfo in usersToSeed)
            {
                try
                {
                    // Find or create user by Keycloak ID
                    var user = ObjectSpace.GetObjectsQuery<User>()
                        .FirstOrDefault(u => u.KeycloakId == userInfo.KeycloakId);
                    
                    if (user == null)
                    {
                        // User doesn't exist, create it
                        System.Diagnostics.Debug.WriteLine($"[Updater] User not found. Creating user: {userInfo.Name} (KeycloakId: {userInfo.KeycloakId})");
                        
                        // Parse name into first and last name
                        var nameParts = userInfo.Name.Split(' ', 2);
                        var firstName = nameParts.Length > 0 ? nameParts[0] : userInfo.Name;
                        var lastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;
                        
                        user = ObjectSpace.CreateObject<User>();
                        user.Id = Guid.NewGuid();
                        user.KeycloakId = userInfo.KeycloakId;
                        user.Email = $"{userInfo.Handle}@sivar.os"; // Generate default email
                        user.FirstName = firstName;
                        user.LastName = lastName;
                        user.Role = UserRole.RegisteredUser;
                        user.IsActive = true;
                        user.PreferredLanguage = "en";
                        user.TimeZone = "UTC";
                        user.CreatedAt = now;
                        user.UpdatedAt = now;
                        
                        // Commit the user first before creating profile
                        ObjectSpace.CommitChanges();
                        System.Diagnostics.Debug.WriteLine($"[Updater] ✅ Created user: {userInfo.Name} (ID: {user.Id})");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[Updater] User found: {userInfo.Name} (ID: {user.Id})");
                    }
                    
                    // Check if user already has a Personal profile
                    var existingProfile = ObjectSpace.GetObjectsQuery<Profile>()
                        .FirstOrDefault(p => p.UserId == user.Id && p.ProfileTypeId == personalProfileTypeId);
                    
                    if (existingProfile != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Updater] User {userInfo.Name} already has a Personal profile. Skipping.");
                        continue;
                    }
                    
                    // Check if handle is already taken
                    var handleExists = ObjectSpace.GetObjectsQuery<Profile>()
                        .Any(p => p.Handle == userInfo.Handle);
                    
                    var finalHandle = userInfo.Handle;
                    if (handleExists)
                    {
                        // Add a suffix to make handle unique
                        finalHandle = $"{userInfo.Handle}-{Guid.NewGuid().ToString().Substring(0, 8)}";
                        System.Diagnostics.Debug.WriteLine($"[Updater] Handle '{userInfo.Handle}' already exists. Using '{finalHandle}' instead.");
                    }
                    
                    // Create default Personal profile
                    var profile = ObjectSpace.CreateObject<Profile>();
                    profile.Id = Guid.NewGuid();
                    profile.UserId = user.Id;
                    profile.ProfileTypeId = personalProfileTypeId;
                    profile.DisplayName = userInfo.Name;
                    profile.Handle = finalHandle;
                    profile.Bio = $"Welcome to {userInfo.Name}'s profile!";
                    profile.Avatar = string.Empty;
                    profile.IsActive = true; // Set as active profile
                    profile.VisibilityLevel = VisibilityLevel.Public;
                    profile.CreatedAt = now;
                    profile.UpdatedAt = now;
                    
                    System.Diagnostics.Debug.WriteLine($"[Updater] ✅ Created Personal profile for {userInfo.Name} (Handle: {finalHandle})");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Updater] ❌ Error creating profile for {userInfo.Name}: {ex.Message}");
                }
            }
            
            System.Diagnostics.Debug.WriteLine("[Updater] Finished SeedDefaultProfiles.");
        }
        
        /// <summary>
        /// Seeds demo data from the DemoData folder JSON files
        /// </summary>
        private async Task SeedDemoDataAsync()
        {
            System.Diagnostics.Debug.WriteLine("[Updater] Starting SeedDemoDataAsync...");
            
            try
            {
                // Find the DemoData folder - go up from XAF module to solution root
                var assemblyLocation = Assembly.GetExecutingAssembly().Location;
                var assemblyDir = Path.GetDirectoryName(assemblyLocation) ?? "";
                
                // Navigate up to find the solution root (where DemoData folder should be)
                var solutionRoot = FindSolutionRoot(assemblyDir);
                if (string.IsNullOrEmpty(solutionRoot))
                {
                    System.Diagnostics.Debug.WriteLine("[Updater] Could not find solution root. Skipping demo data seeding.");
                    return;
                }
                
                var demoDataPath = Path.Combine(solutionRoot, "DemoData");
                if (!Directory.Exists(demoDataPath))
                {
                    System.Diagnostics.Debug.WriteLine($"[Updater] DemoData folder not found at: {demoDataPath}. Skipping demo data seeding.");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"[Updater] DemoData folder found at: {demoDataPath}");
                
                // Seed restaurants
                await SeedRestaurantsAsync(demoDataPath);
                System.Diagnostics.Debug.WriteLine($"[Updater] 📊 Embeddings queued after Restaurants: {_pendingEmbeddings.Count}");
                
                // Seed entertainment
                await SeedEntertainmentAsync(demoDataPath);
                System.Diagnostics.Debug.WriteLine($"[Updater] 📊 Embeddings queued after Entertainment: {_pendingEmbeddings.Count}");
                
                // Seed tourism
                await SeedTourismAsync(demoDataPath);
                System.Diagnostics.Debug.WriteLine($"[Updater] 📊 Embeddings queued after Tourism: {_pendingEmbeddings.Count}");
                
                // Seed government
                await SeedGovernmentAsync(demoDataPath);
                System.Diagnostics.Debug.WriteLine($"[Updater] 📊 Embeddings queued after Government: {_pendingEmbeddings.Count}");
                
                // Seed services
                await SeedServicesAsync(demoDataPath);
                System.Diagnostics.Debug.WriteLine($"[Updater] 📊 Embeddings queued after Services: {_pendingEmbeddings.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Updater] ❌ Error seeding demo data: {ex.Message}");
            }
            
            System.Diagnostics.Debug.WriteLine("[Updater] Finished SeedDemoDataAsync.");
        }
        
        /// <summary>
        /// Finds the solution root directory by looking for Sivar.Os.sln
        /// </summary>
        private string? FindSolutionRoot(string startDir)
        {
            var dir = startDir;
            while (!string.IsNullOrEmpty(dir))
            {
                if (File.Exists(Path.Combine(dir, "Sivar.Os.sln")))
                {
                    return dir;
                }
                var parent = Directory.GetParent(dir);
                dir = parent?.FullName;
            }
            return null;
        }
        
        /// <summary>
        /// Seeds restaurant demo data from DemoData/Restaurants/restaurants.json
        /// </summary>
        private async Task SeedRestaurantsAsync(string demoDataPath)
        {
            var restaurantsJsonPath = Path.Combine(demoDataPath, "Restaurants", "restaurants.json");
            if (!File.Exists(restaurantsJsonPath))
            {
                System.Diagnostics.Debug.WriteLine($"[Updater] Restaurants JSON not found at: {restaurantsJsonPath}. Skipping.");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"[Updater] Loading restaurants from: {restaurantsJsonPath}");
            
            var jsonContent = await File.ReadAllTextAsync(restaurantsJsonPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            var demoData = JsonSerializer.Deserialize<DemoDataFile>(jsonContent, options);
            if (demoData == null)
            {
                System.Diagnostics.Debug.WriteLine("[Updater] Failed to parse restaurants JSON.");
                return;
            }
            
            var now = DateTime.UtcNow;
            var businessProfileTypeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            
            // Get the business profile type
            var businessProfileType = ObjectSpace.FirstOrDefault<ProfileType>(pt => pt.Id == businessProfileTypeId);
            if (businessProfileType == null)
            {
                System.Diagnostics.Debug.WriteLine("[Updater] Business profile type not found. Skipping restaurant seeding.");
                return;
            }
            
            // Create a system user for demo data if not exists
            var systemUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var systemUser = ObjectSpace.FirstOrDefault<User>(u => u.Id == systemUserId);
            if (systemUser == null)
            {
                systemUser = ObjectSpace.CreateObject<User>();
                systemUser.Id = systemUserId;
                systemUser.KeycloakId = "demo-system-user";
                systemUser.Email = "demo@sivar.os";
                systemUser.FirstName = "Demo";
                systemUser.LastName = "System";
                systemUser.Role = UserRole.RegisteredUser;
                systemUser.IsActive = true;
                systemUser.PreferredLanguage = "es";
                systemUser.TimeZone = "America/El_Salvador";
                systemUser.CreatedAt = now;
                systemUser.UpdatedAt = now;
                ObjectSpace.CommitChanges();
                System.Diagnostics.Debug.WriteLine("[Updater] ✅ Created demo system user.");
            }
            
            // Seed profiles
            var profileCount = 0;
            foreach (var profileData in demoData.Profiles ?? new List<DemoProfileData>())
            {
                try
                {
                    var profileId = Guid.Parse(profileData.Id);
                    
                    // Check if profile already exists
                    var existingProfile = ObjectSpace.FirstOrDefault<Profile>(p => p.Id == profileId);
                    if (existingProfile != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Updater] Profile {profileData.DisplayName} already exists. Skipping.");
                        continue;
                    }
                    
                    var profile = ObjectSpace.CreateObject<Profile>();
                    profile.Id = profileId;
                    profile.UserId = systemUserId;
                    profile.ProfileTypeId = businessProfileTypeId;
                    profile.DisplayName = profileData.DisplayName ?? "";
                    profile.Handle = profileData.Handle ?? "";
                    profile.Bio = profileData.Bio ?? "";
                    profile.Avatar = profileData.Avatar ?? "";
                    profile.IsActive = true; // Demo profiles are active for directory listings
                    profile.VisibilityLevel = VisibilityLevel.Public;
                    profile.CreatedAt = now;
                    profile.UpdatedAt = now;
                    
                    profileCount++;
                    System.Diagnostics.Debug.WriteLine($"[Updater] ✅ Created profile: {profileData.DisplayName} ({profileData.Handle})");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Updater] ❌ Error creating profile {profileData.DisplayName}: {ex.Message}");
                }
            }
            
            ObjectSpace.CommitChanges();
            System.Diagnostics.Debug.WriteLine($"[Updater] Created {profileCount} restaurant profiles.");
            
            // Seed posts
            var postCount = 0;
            foreach (var postData in demoData.Posts ?? new List<DemoPostData>())
            {
                try
                {
                    var postId = Guid.Parse(postData.Id);
                    var profileId = Guid.Parse(postData.ProfileId);
                    
                    // Check if post already exists
                    var existingPost = ObjectSpace.FirstOrDefault<Post>(p => p.Id == postId);
                    if (existingPost != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Updater] Post {postData.Title} already exists. Skipping.");
                        continue;
                    }
                    
                    var post = ObjectSpace.CreateObject<Post>();
                    post.Id = postId;
                    post.ProfileId = profileId;
                    post.PostType = (PostType)(postData.PostType ?? 2); // Default to BusinessLocation
                    post.Title = postData.Title ?? "";
                    post.Content = postData.Content ?? "";
                    post.Visibility = VisibilityLevel.Public;
                    post.Language = "es";
                    post.Tags = postData.Tags?.ToArray() ?? Array.Empty<string>();
                    post.CreatedAt = now;
                    post.UpdatedAt = now;
                    
                    // Set location if available
                    if (postData.Location != null)
                    {
                        post.Location = new Location(
                            postData.Location.City ?? "",
                            postData.Location.State ?? "",
                            postData.Location.Country ?? "El Salvador",
                            postData.Location.Latitude,
                            postData.Location.Longitude
                        );
                    }
                    
                    // Set pricing info
                    if (postData.PricingInfo != null)
                    {
                        var pricing = new PricingInformation
                        {
                            Amount = postData.PricingInfo.Amount ?? 0,
                            Currency = Currency.USD,
                            IsNegotiable = postData.PricingInfo.IsNegotiable ?? false,
                            Description = postData.PricingInfo.Description
                        };
                        post.PricingInfo = JsonSerializer.Serialize(pricing);
                    }
                    
                    // Set business metadata
                    if (postData.BusinessMetadata != null)
                    {
                        var metadata = new BusinessLocationMetadata
                        {
                            LocationType = Enum.TryParse<BusinessLocationType>(postData.BusinessMetadata.LocationType, out var locType) 
                                ? locType 
                                : BusinessLocationType.RetailStore,
                            ContactPhone = postData.BusinessMetadata.ContactPhone,
                            ContactEmail = postData.BusinessMetadata.ContactEmail,
                            AcceptsWalkIns = postData.BusinessMetadata.AcceptsWalkIns ?? true,
                            RequiresAppointment = postData.BusinessMetadata.RequiresAppointment ?? false,
                            SpecialInstructions = postData.BusinessMetadata.SpecialInstructions
                        };
                        
                        // Set working hours if available
                        if (postData.BusinessMetadata.WorkingHours != null)
                        {
                            metadata.WorkingHours = new BusinessHours
                            {
                                Monday = ParseDaySchedule(postData.BusinessMetadata.WorkingHours.Monday),
                                Tuesday = ParseDaySchedule(postData.BusinessMetadata.WorkingHours.Tuesday),
                                Wednesday = ParseDaySchedule(postData.BusinessMetadata.WorkingHours.Wednesday),
                                Thursday = ParseDaySchedule(postData.BusinessMetadata.WorkingHours.Thursday),
                                Friday = ParseDaySchedule(postData.BusinessMetadata.WorkingHours.Friday),
                                Saturday = ParseDaySchedule(postData.BusinessMetadata.WorkingHours.Saturday),
                                Sunday = ParseDaySchedule(postData.BusinessMetadata.WorkingHours.Sunday)
                            };
                        }
                        
                        post.BusinessMetadata = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });
                    }
                    
                    // Queue pre-computed content embedding for raw SQL update
                    // (EF Core ignores ContentEmbedding property - see PostConfiguration.cs)
                    if (!string.IsNullOrEmpty(postData.ContentEmbedding))
                    {
                        _pendingEmbeddings[post.Id] = postData.ContentEmbedding;
                        System.Diagnostics.Debug.WriteLine($"[Updater] 📊 Queued embedding for: {postData.Title}");
                    }
                    
                    postCount++;
                    System.Diagnostics.Debug.WriteLine($"[Updater] ✅ Created post: {postData.Title}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Updater] ❌ Error creating post {postData.Title}: {ex.Message}");
                }
            }
            
            ObjectSpace.CommitChanges();
            System.Diagnostics.Debug.WriteLine($"[Updater] ✅ Created {postCount} restaurant posts.");
        }
        
        /// <summary>
        /// Parses a day schedule from JSON data
        /// </summary>
        private DaySchedule ParseDaySchedule(DemoDaySchedule? data)
        {
            if (data == null)
                return new DaySchedule { IsClosed = true };
                
            return new DaySchedule
            {
                IsClosed = data.IsClosed ?? false,
                OpenTime = ParseTimeOnly(data.OpenTime, "09:00"),
                CloseTime = ParseTimeOnly(data.CloseTime, "18:00")
            };
        }
        
        /// <summary>
        /// Parses a string time to TimeOnly
        /// </summary>
        private TimeOnly? ParseTimeOnly(string? timeString, string defaultValue)
        {
            if (string.IsNullOrEmpty(timeString))
                timeString = defaultValue;
                
            if (TimeOnly.TryParse(timeString, out var time))
                return time;
                
            return null;
        }
        
        /// <summary>
        /// Seeds entertainment demo data from DemoData/Entertainment/entertainment.json
        /// </summary>
        private async Task SeedEntertainmentAsync(string demoDataPath)
        {
            var entertainmentJsonPath = Path.Combine(demoDataPath, "Entertainment", "entertainment.json");
            if (!File.Exists(entertainmentJsonPath))
            {
                System.Diagnostics.Debug.WriteLine($"[Updater] Entertainment JSON not found at: {entertainmentJsonPath}. Skipping.");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"[Updater] Loading entertainment from: {entertainmentJsonPath}");
            
            var jsonContent = await File.ReadAllTextAsync(entertainmentJsonPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            var demoData = JsonSerializer.Deserialize<DemoDataFile>(jsonContent, options);
            if (demoData == null)
            {
                System.Diagnostics.Debug.WriteLine("[Updater] Failed to parse entertainment JSON.");
                return;
            }
            
            var now = DateTime.UtcNow;
            var businessProfileTypeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            
            // Get the business profile type
            var businessProfileType = ObjectSpace.FirstOrDefault<ProfileType>(pt => pt.Id == businessProfileTypeId);
            if (businessProfileType == null)
            {
                System.Diagnostics.Debug.WriteLine("[Updater] Business profile type not found. Skipping entertainment seeding.");
                return;
            }
            
            // Ensure system user exists (should already be created by restaurants seeding)
            var systemUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var systemUser = ObjectSpace.FirstOrDefault<User>(u => u.Id == systemUserId);
            if (systemUser == null)
            {
                systemUser = ObjectSpace.CreateObject<User>();
                systemUser.Id = systemUserId;
                systemUser.KeycloakId = "demo-system-user";
                systemUser.Email = "demo@sivar.os";
                systemUser.FirstName = "Demo";
                systemUser.LastName = "System";
                systemUser.Role = UserRole.RegisteredUser;
                systemUser.IsActive = true;
                systemUser.PreferredLanguage = "es";
                systemUser.TimeZone = "America/El_Salvador";
                systemUser.CreatedAt = now;
                systemUser.UpdatedAt = now;
                ObjectSpace.CommitChanges();
                System.Diagnostics.Debug.WriteLine("[Updater] ✅ Created demo system user.");
            }
            
            // Seed profiles
            var profileCount = 0;
            foreach (var profileData in demoData.Profiles ?? new List<DemoProfileData>())
            {
                try
                {
                    var profileId = Guid.Parse(profileData.Id);
                    
                    // Check if profile already exists
                    var existingProfile = ObjectSpace.FirstOrDefault<Profile>(p => p.Id == profileId);
                    if (existingProfile != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Updater] Profile {profileData.DisplayName} already exists. Skipping.");
                        continue;
                    }
                    
                    var profile = ObjectSpace.CreateObject<Profile>();
                    profile.Id = profileId;
                    profile.UserId = systemUserId;
                    profile.ProfileTypeId = businessProfileTypeId;
                    profile.DisplayName = profileData.DisplayName ?? "";
                    profile.Handle = profileData.Handle ?? "";
                    profile.Bio = profileData.Bio ?? "";
                    profile.Avatar = profileData.Avatar ?? "";
                    profile.IsActive = true; // Demo profiles are active for directory listings
                    profile.VisibilityLevel = VisibilityLevel.Public;
                    profile.CreatedAt = now;
                    profile.UpdatedAt = now;
                    
                    profileCount++;
                    System.Diagnostics.Debug.WriteLine($"[Updater] ✅ Created profile: {profileData.DisplayName} ({profileData.Handle})");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Updater] ❌ Error creating profile {profileData.DisplayName}: {ex.Message}");
                }
            }
            
            ObjectSpace.CommitChanges();
            System.Diagnostics.Debug.WriteLine($"[Updater] Created {profileCount} entertainment profiles.");
            
            // Seed posts
            var postCount = 0;
            foreach (var postData in demoData.Posts ?? new List<DemoPostData>())
            {
                try
                {
                    var postId = Guid.Parse(postData.Id);
                    var profileId = Guid.Parse(postData.ProfileId);
                    
                    // Check if post already exists
                    var existingPost = ObjectSpace.FirstOrDefault<Post>(p => p.Id == postId);
                    if (existingPost != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Updater] Post {postData.Title} already exists. Skipping.");
                        continue;
                    }
                    
                    var post = ObjectSpace.CreateObject<Post>();
                    post.Id = postId;
                    post.ProfileId = profileId;
                    post.PostType = (PostType)(postData.PostType ?? 2); // Default to BusinessLocation
                    post.Title = postData.Title ?? "";
                    post.Content = postData.Content ?? "";
                    post.Visibility = VisibilityLevel.Public;
                    post.Language = "es";
                    post.Tags = postData.Tags?.ToArray() ?? Array.Empty<string>();
                    post.CreatedAt = now;
                    post.UpdatedAt = now;
                    
                    // Set location if available (for BusinessLocation posts)
                    if (postData.Location != null)
                    {
                        post.Location = new Location(
                            postData.Location.City ?? "",
                            postData.Location.State ?? "",
                            postData.Location.Country ?? "El Salvador",
                            postData.Location.Latitude,
                            postData.Location.Longitude
                        );
                    }
                    
                    // Set pricing info
                    if (postData.PricingInfo != null)
                    {
                        var pricing = new PricingInformation
                        {
                            Amount = postData.PricingInfo.Amount ?? 0,
                            Currency = Currency.USD,
                            IsNegotiable = postData.PricingInfo.IsNegotiable ?? false,
                            Description = postData.PricingInfo.Description
                        };
                        post.PricingInfo = JsonSerializer.Serialize(pricing);
                    }
                    
                    // Set business metadata (for BusinessLocation posts)
                    if (postData.BusinessMetadata != null)
                    {
                        var metadata = new BusinessLocationMetadata
                        {
                            LocationType = Enum.TryParse<BusinessLocationType>(postData.BusinessMetadata.LocationType, out var locType) 
                                ? locType 
                                : BusinessLocationType.RetailStore,
                            ContactPhone = postData.BusinessMetadata.ContactPhone,
                            ContactEmail = postData.BusinessMetadata.ContactEmail,
                            AcceptsWalkIns = postData.BusinessMetadata.AcceptsWalkIns ?? true,
                            RequiresAppointment = postData.BusinessMetadata.RequiresAppointment ?? false,
                            SpecialInstructions = postData.BusinessMetadata.SpecialInstructions
                        };
                        
                        // Set working hours if available
                        if (postData.BusinessMetadata.WorkingHours != null)
                        {
                            metadata.WorkingHours = new BusinessHours
                            {
                                Monday = ParseDaySchedule(postData.BusinessMetadata.WorkingHours.Monday),
                                Tuesday = ParseDaySchedule(postData.BusinessMetadata.WorkingHours.Tuesday),
                                Wednesday = ParseDaySchedule(postData.BusinessMetadata.WorkingHours.Wednesday),
                                Thursday = ParseDaySchedule(postData.BusinessMetadata.WorkingHours.Thursday),
                                Friday = ParseDaySchedule(postData.BusinessMetadata.WorkingHours.Friday),
                                Saturday = ParseDaySchedule(postData.BusinessMetadata.WorkingHours.Saturday),
                                Sunday = ParseDaySchedule(postData.BusinessMetadata.WorkingHours.Sunday)
                            };
                        }
                        
                        post.BusinessMetadata = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });
                    }
                    
                    // Queue pre-computed content embedding for raw SQL update
                    // (EF Core ignores ContentEmbedding property - see PostConfiguration.cs)
                    if (!string.IsNullOrEmpty(postData.ContentEmbedding))
                    {
                        _pendingEmbeddings[post.Id] = postData.ContentEmbedding;
                        System.Diagnostics.Debug.WriteLine($"[Updater] 📊 Queued embedding for: {postData.Title}");
                    }
                    
                    postCount++;
                    System.Diagnostics.Debug.WriteLine($"[Updater] ✅ Created post: {postData.Title}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Updater] ❌ Error creating post {postData.Title}: {ex.Message}");
                }
            }
            
            ObjectSpace.CommitChanges();
            System.Diagnostics.Debug.WriteLine($"[Updater] ✅ Created {postCount} entertainment posts.");
        }
        
        /// <summary>
        /// Seeds tourism demo data from DemoData/Tourism/tourism.json
        /// </summary>
        private async Task SeedTourismAsync(string demoDataPath)
        {
            var tourismJsonPath = Path.Combine(demoDataPath, "Tourism", "tourism.json");
            if (!File.Exists(tourismJsonPath))
            {
                System.Diagnostics.Debug.WriteLine($"[Updater] Tourism JSON not found at: {tourismJsonPath}. Skipping.");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"[Updater] Loading tourism from: {tourismJsonPath}");
            
            var jsonContent = await File.ReadAllTextAsync(tourismJsonPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            var demoData = JsonSerializer.Deserialize<DemoDataFile>(jsonContent, options);
            if (demoData == null)
            {
                System.Diagnostics.Debug.WriteLine("[Updater] Failed to parse tourism JSON.");
                return;
            }
            
            var now = DateTime.UtcNow;
            var businessProfileTypeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            
            // Get the business profile type
            var businessProfileType = ObjectSpace.FirstOrDefault<ProfileType>(pt => pt.Id == businessProfileTypeId);
            if (businessProfileType == null)
            {
                System.Diagnostics.Debug.WriteLine("[Updater] Business profile type not found. Skipping tourism seeding.");
                return;
            }
            
            // Ensure system user exists
            var systemUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var systemUser = ObjectSpace.FirstOrDefault<User>(u => u.Id == systemUserId);
            if (systemUser == null)
            {
                System.Diagnostics.Debug.WriteLine("[Updater] System user not found. Skipping tourism seeding.");
                return;
            }
            
            // Seed profiles
            var profileCount = 0;
            foreach (var profileData in demoData.Profiles ?? new List<DemoProfileData>())
            {
                try
                {
                    var profileId = Guid.Parse(profileData.Id);
                    
                    // Check if profile already exists
                    var existingProfile = ObjectSpace.FirstOrDefault<Profile>(p => p.Id == profileId);
                    if (existingProfile != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Updater] Profile {profileData.DisplayName} already exists. Skipping.");
                        continue;
                    }
                    
                    var profile = ObjectSpace.CreateObject<Profile>();
                    profile.Id = profileId;
                    profile.UserId = systemUserId;
                    profile.ProfileTypeId = businessProfileTypeId;
                    profile.DisplayName = profileData.DisplayName ?? "";
                    profile.Handle = profileData.Handle ?? "";
                    profile.Bio = profileData.Bio ?? "";
                    profile.Avatar = profileData.Avatar ?? "";
                    profile.IsActive = true; // Demo profiles are active for directory listings
                    profile.VisibilityLevel = VisibilityLevel.Public;
                    profile.CreatedAt = now;
                    profile.UpdatedAt = now;
                    
                    profileCount++;
                    System.Diagnostics.Debug.WriteLine($"[Updater] ✅ Created profile: {profileData.DisplayName} ({profileData.Handle})");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Updater] ❌ Error creating profile {profileData.DisplayName}: {ex.Message}");
                }
            }
            
            ObjectSpace.CommitChanges();
            System.Diagnostics.Debug.WriteLine($"[Updater] Created {profileCount} tourism profiles.");
            
            // Seed posts
            var postCount = 0;
            foreach (var postData in demoData.Posts ?? new List<DemoPostData>())
            {
                try
                {
                    var postId = Guid.Parse(postData.Id);
                    var profileId = Guid.Parse(postData.ProfileId);
                    
                    // Check if post already exists
                    var existingPost = ObjectSpace.FirstOrDefault<Post>(p => p.Id == postId);
                    if (existingPost != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Updater] Post {postData.Title} already exists. Skipping.");
                        continue;
                    }
                    
                    var post = ObjectSpace.CreateObject<Post>();
                    post.Id = postId;
                    post.ProfileId = profileId;
                    post.PostType = (PostType)(postData.PostType ?? 2); // Default to BusinessLocation
                    post.Title = postData.Title ?? "";
                    post.Content = postData.Content ?? "";
                    post.Visibility = VisibilityLevel.Public;
                    post.Language = "es";
                    post.Tags = postData.Tags?.ToArray() ?? Array.Empty<string>();
                    post.CreatedAt = now;
                    post.UpdatedAt = now;
                    
                    // Set location if available
                    if (postData.Location != null)
                    {
                        post.Location = new Location(
                            postData.Location.City ?? "",
                            postData.Location.State ?? "",
                            postData.Location.Country ?? "El Salvador",
                            postData.Location.Latitude,
                            postData.Location.Longitude
                        );
                    }
                    
                    // Set pricing info
                    if (postData.PricingInfo != null)
                    {
                        var pricing = new PricingInformation
                        {
                            Amount = postData.PricingInfo.Amount ?? 0,
                            Currency = Currency.USD,
                            IsNegotiable = postData.PricingInfo.IsNegotiable ?? false,
                            Description = postData.PricingInfo.Description
                        };
                        post.PricingInfo = JsonSerializer.Serialize(pricing);
                    }
                    
                    // Set business metadata
                    if (postData.BusinessMetadata != null)
                    {
                        var metadata = new BusinessLocationMetadata
                        {
                            LocationType = Enum.TryParse<BusinessLocationType>(postData.BusinessMetadata.LocationType, out var locType) 
                                ? locType 
                                : BusinessLocationType.RetailStore,
                            ContactPhone = postData.BusinessMetadata.ContactPhone,
                            ContactEmail = postData.BusinessMetadata.ContactEmail,
                            AcceptsWalkIns = postData.BusinessMetadata.AcceptsWalkIns ?? true,
                            RequiresAppointment = postData.BusinessMetadata.RequiresAppointment ?? false,
                            SpecialInstructions = postData.BusinessMetadata.SpecialInstructions
                        };
                        
                        // Set working hours if available
                        if (postData.BusinessMetadata.WorkingHours != null)
                        {
                            metadata.WorkingHours = new BusinessHours
                            {
                                Monday = ParseDaySchedule(postData.BusinessMetadata.WorkingHours.Monday),
                                Tuesday = ParseDaySchedule(postData.BusinessMetadata.WorkingHours.Tuesday),
                                Wednesday = ParseDaySchedule(postData.BusinessMetadata.WorkingHours.Wednesday),
                                Thursday = ParseDaySchedule(postData.BusinessMetadata.WorkingHours.Thursday),
                                Friday = ParseDaySchedule(postData.BusinessMetadata.WorkingHours.Friday),
                                Saturday = ParseDaySchedule(postData.BusinessMetadata.WorkingHours.Saturday),
                                Sunday = ParseDaySchedule(postData.BusinessMetadata.WorkingHours.Sunday)
                            };
                        }
                        
                        post.BusinessMetadata = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });
                    }
                    
                    // Queue pre-computed content embedding for raw SQL update
                    // (EF Core ignores ContentEmbedding property - see PostConfiguration.cs)
                    if (!string.IsNullOrEmpty(postData.ContentEmbedding))
                    {
                        _pendingEmbeddings[post.Id] = postData.ContentEmbedding;
                        System.Diagnostics.Debug.WriteLine($"[Updater] 📊 Queued embedding for: {postData.Title}");
                    }
                    
                    postCount++;
                    System.Diagnostics.Debug.WriteLine($"[Updater] ✅ Created post: {postData.Title}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Updater] ❌ Error creating post {postData.Title}: {ex.Message}");
                }
            }
            
            ObjectSpace.CommitChanges();
            System.Diagnostics.Debug.WriteLine($"[Updater] ✅ Created {postCount} tourism posts.");
        }
        
        /// <summary>
        /// Seeds government demo data from DemoData/Government/government.json
        /// </summary>
        private async Task SeedGovernmentAsync(string demoDataPath)
        {
            var governmentJsonPath = Path.Combine(demoDataPath, "Government", "government.json");
            if (!File.Exists(governmentJsonPath))
            {
                System.Diagnostics.Debug.WriteLine($"[Updater] Government JSON not found at: {governmentJsonPath}. Skipping.");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"[Updater] Loading government from: {governmentJsonPath}");
            
            var jsonContent = await File.ReadAllTextAsync(governmentJsonPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            var demoData = JsonSerializer.Deserialize<DemoDataFile>(jsonContent, options);
            if (demoData == null)
            {
                System.Diagnostics.Debug.WriteLine("[Updater] Failed to parse government JSON.");
                return;
            }
            
            var now = DateTime.UtcNow;
            var businessProfileTypeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            
            // Get the business profile type
            var businessProfileType = ObjectSpace.FirstOrDefault<ProfileType>(pt => pt.Id == businessProfileTypeId);
            if (businessProfileType == null)
            {
                System.Diagnostics.Debug.WriteLine("[Updater] Business profile type not found. Skipping government seeding.");
                return;
            }
            
            // Ensure system user exists
            var systemUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var systemUser = ObjectSpace.FirstOrDefault<User>(u => u.Id == systemUserId);
            if (systemUser == null)
            {
                System.Diagnostics.Debug.WriteLine("[Updater] System user not found. Skipping government seeding.");
                return;
            }
            
            // Seed profiles
            var profileCount = 0;
            foreach (var profileData in demoData.Profiles ?? new List<DemoProfileData>())
            {
                try
                {
                    var profileId = Guid.Parse(profileData.Id);
                    
                    // Check if profile already exists
                    var existingProfile = ObjectSpace.FirstOrDefault<Profile>(p => p.Id == profileId);
                    if (existingProfile != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Updater] Profile {profileData.DisplayName} already exists. Skipping.");
                        continue;
                    }
                    
                    var profile = ObjectSpace.CreateObject<Profile>();
                    profile.Id = profileId;
                    profile.UserId = systemUserId;
                    profile.ProfileTypeId = businessProfileTypeId;
                    profile.DisplayName = profileData.DisplayName ?? "";
                    profile.Handle = profileData.Handle ?? "";
                    profile.Bio = profileData.Bio ?? "";
                    profile.Avatar = profileData.Avatar ?? "";
                    profile.IsActive = true; // Demo profiles are active for directory listings
                    profile.VisibilityLevel = VisibilityLevel.Public;
                    profile.CreatedAt = now;
                    profile.UpdatedAt = now;
                    
                    profileCount++;
                    System.Diagnostics.Debug.WriteLine($"[Updater] ✅ Created profile: {profileData.DisplayName} ({profileData.Handle})");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Updater] ❌ Error creating profile {profileData.DisplayName}: {ex.Message}");
                }
            }
            
            ObjectSpace.CommitChanges();
            System.Diagnostics.Debug.WriteLine($"[Updater] Created {profileCount} government profiles.");
            
            // Seed posts
            var postCount = 0;
            foreach (var postData in demoData.Posts ?? new List<DemoPostData>())
            {
                try
                {
                    var postId = Guid.Parse(postData.Id);
                    var profileId = Guid.Parse(postData.ProfileId);
                    
                    // Check if post already exists
                    var existingPost = ObjectSpace.FirstOrDefault<Post>(p => p.Id == postId);
                    if (existingPost != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Updater] Post {postData.Title} already exists. Skipping.");
                        continue;
                    }
                    
                    var post = ObjectSpace.CreateObject<Post>();
                    post.Id = postId;
                    post.ProfileId = profileId;
                    post.PostType = (PostType)(postData.PostType ?? 2); // Default to BusinessLocation
                    post.Title = postData.Title ?? "";
                    post.Content = postData.Content ?? "";
                    post.Visibility = VisibilityLevel.Public;
                    post.Language = "es";
                    post.Tags = postData.Tags?.ToArray() ?? Array.Empty<string>();
                    post.CreatedAt = now;
                    post.UpdatedAt = now;
                    
                    // Set location if available (for BusinessLocation posts)
                    if (postData.Location != null)
                    {
                        post.Location = new Location(
                            postData.Location.City ?? "",
                            postData.Location.State ?? "",
                            postData.Location.Country ?? "El Salvador",
                            postData.Location.Latitude,
                            postData.Location.Longitude
                        );
                    }
                    
                    // Set pricing info
                    if (postData.PricingInfo != null)
                    {
                        var pricing = new PricingInformation
                        {
                            Amount = postData.PricingInfo.Amount ?? 0,
                            Currency = Currency.USD,
                            IsNegotiable = postData.PricingInfo.IsNegotiable ?? false,
                            Description = postData.PricingInfo.Description
                        };
                        post.PricingInfo = JsonSerializer.Serialize(pricing);
                    }
                    
                    // Set business metadata (for BusinessLocation posts)
                    if (postData.BusinessMetadata != null)
                    {
                        var metadata = new BusinessLocationMetadata
                        {
                            LocationType = Enum.TryParse<BusinessLocationType>(postData.BusinessMetadata.LocationType, out var locType) 
                                ? locType 
                                : BusinessLocationType.MainOffice,
                            ContactPhone = postData.BusinessMetadata.ContactPhone,
                            ContactEmail = postData.BusinessMetadata.ContactEmail,
                            AcceptsWalkIns = postData.BusinessMetadata.AcceptsWalkIns ?? true,
                            RequiresAppointment = postData.BusinessMetadata.RequiresAppointment ?? false,
                            SpecialInstructions = postData.BusinessMetadata.SpecialInstructions
                        };
                        
                        // Set working hours if available
                        if (postData.BusinessMetadata.WorkingHours != null)
                        {
                            metadata.WorkingHours = new BusinessHours
                            {
                                Monday = ParseDaySchedule(postData.BusinessMetadata.WorkingHours.Monday),
                                Tuesday = ParseDaySchedule(postData.BusinessMetadata.WorkingHours.Tuesday),
                                Wednesday = ParseDaySchedule(postData.BusinessMetadata.WorkingHours.Wednesday),
                                Thursday = ParseDaySchedule(postData.BusinessMetadata.WorkingHours.Thursday),
                                Friday = ParseDaySchedule(postData.BusinessMetadata.WorkingHours.Friday),
                                Saturday = ParseDaySchedule(postData.BusinessMetadata.WorkingHours.Saturday),
                                Sunday = ParseDaySchedule(postData.BusinessMetadata.WorkingHours.Sunday)
                            };
                        }
                        
                        post.BusinessMetadata = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });
                    }
                    
                    // Queue pre-computed content embedding for raw SQL update
                    // (EF Core ignores ContentEmbedding property - see PostConfiguration.cs)
                    if (!string.IsNullOrEmpty(postData.ContentEmbedding))
                    {
                        _pendingEmbeddings[post.Id] = postData.ContentEmbedding;
                        System.Diagnostics.Debug.WriteLine($"[Updater] 📊 Queued embedding for: {postData.Title}");
                    }
                    
                    postCount++;
                    System.Diagnostics.Debug.WriteLine($"[Updater] ✅ Created post: {postData.Title}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Updater] ❌ Error creating post {postData.Title}: {ex.Message}");
                }
            }
            
            ObjectSpace.CommitChanges();
            System.Diagnostics.Debug.WriteLine($"[Updater] ✅ Created {postCount} government posts.");
        }
        
        /// <summary>
        /// Seeds Services category data - banks, utilities, healthcare, professional services, retail
        /// </summary>
        private async Task SeedServicesAsync(string demoDataPath)
        {
            System.Diagnostics.Debug.WriteLine("[Updater] Starting SeedServicesAsync...");
            
            var servicesFile = Path.Combine(demoDataPath, "Services", "services.json");
            if (!File.Exists(servicesFile))
            {
                System.Diagnostics.Debug.WriteLine("[Updater] services.json not found. Skipping.");
                return;
            }
            
            var json = await File.ReadAllTextAsync(servicesFile);
            var demoData = JsonSerializer.Deserialize<DemoDataFile>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            if (demoData == null)
            {
                System.Diagnostics.Debug.WriteLine("[Updater] Failed to deserialize services.json");
                return;
            }
            
            var businessProfileTypeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var seederUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var now = DateTime.UtcNow;
            
            // Seed profiles
            var profileCount = 0;
            foreach (var profileData in demoData.Profiles ?? new List<DemoProfileData>())
            {
                try
                {
                    var profileId = Guid.Parse(profileData.Id);
                    
                    // Check if profile already exists
                    var existingProfile = ObjectSpace.FirstOrDefault<Profile>(p => p.Id == profileId);
                    if (existingProfile != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Updater] Profile {profileData.DisplayName} already exists. Skipping.");
                        continue;
                    }
                    
                    var profile = ObjectSpace.CreateObject<Profile>();
                    profile.Id = profileId;
                    profile.UserId = seederUserId;
                    profile.DisplayName = profileData.DisplayName ?? "";
                    profile.Handle = profileData.Handle ?? "";
                    profile.Bio = profileData.Bio ?? "";
                    profile.Avatar = profileData.Avatar ?? "";
                    profile.ProfileTypeId = businessProfileTypeId;
                    profile.IsActive = true;
                    profile.VisibilityLevel = VisibilityLevel.Public;
                    profile.CreatedAt = now;
                    profile.UpdatedAt = now;
                    
                    profileCount++;
                    System.Diagnostics.Debug.WriteLine($"[Updater] ✅ Created profile: {profileData.DisplayName}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Updater] ❌ Error creating profile {profileData.DisplayName}: {ex.Message}");
                }
            }
            
            ObjectSpace.CommitChanges();
            System.Diagnostics.Debug.WriteLine($"[Updater] Created {profileCount} service profiles.");
            
            // Seed posts
            var postCount = 0;
            foreach (var postData in demoData.Posts ?? new List<DemoPostData>())
            {
                try
                {
                    var postId = Guid.Parse(postData.Id);
                    var profileId = Guid.Parse(postData.ProfileId);
                    
                    // Check if post already exists
                    var existingPost = ObjectSpace.FirstOrDefault<Post>(p => p.Id == postId);
                    if (existingPost != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Updater] Post {postData.Title} already exists. Skipping.");
                        continue;
                    }
                    
                    var post = ObjectSpace.CreateObject<Post>();
                    post.Id = postId;
                    post.ProfileId = profileId;
                    post.PostType = (PostType)(postData.PostType ?? 2); // Default to BusinessLocation
                    post.Title = postData.Title ?? "";
                    post.Content = postData.Content ?? "";
                    post.Visibility = VisibilityLevel.Public;
                    post.Language = "es";
                    post.Tags = postData.Tags?.ToArray() ?? Array.Empty<string>();
                    post.CreatedAt = now;
                    post.UpdatedAt = now;
                    
                    // Handle business location details
                    if (postData.BusinessLocationDetails != null)
                    {
                        var details = postData.BusinessLocationDetails;
                        
                        // Set location from business details
                        post.Location = new Location(
                            details.City ?? "",
                            details.State ?? "",
                            details.Country ?? "El Salvador",
                            details.Latitude ?? 0,
                            details.Longitude ?? 0
                        );
                        
                        // Set business metadata
                        var locationType = (BusinessLocationType)(details.BusinessLocationType ?? 4);
                        var metadata = new BusinessLocationMetadata
                        {
                            LocationType = locationType,
                            ContactPhone = details.PhoneNumber,
                            ContactEmail = details.Email,
                            AcceptsWalkIns = true,
                            RequiresAppointment = false,
                            SpecialInstructions = details.WorkingHours
                        };
                        
                        post.BusinessMetadata = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });
                    }
                    
                    // Queue pre-computed content embedding for raw SQL update
                    // (EF Core ignores ContentEmbedding property - see PostConfiguration.cs)
                    if (!string.IsNullOrEmpty(postData.ContentEmbedding))
                    {
                        _pendingEmbeddings[post.Id] = postData.ContentEmbedding;
                        System.Diagnostics.Debug.WriteLine($"[Updater] 📊 Queued embedding for: {postData.Title}");
                    }
                    
                    postCount++;
                    System.Diagnostics.Debug.WriteLine($"[Updater] ✅ Created post: {postData.Title}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Updater] ❌ Error creating post {postData.Title}: {ex.Message}");
                }
            }
            
            ObjectSpace.CommitChanges();
            System.Diagnostics.Debug.WriteLine($"[Updater] ✅ Created {postCount} service posts.");
        }
    }
    
    #region Demo Data DTOs
    
    /// <summary>
    /// Root structure for demo data JSON files
    /// </summary>
    public class DemoDataFile
    {
        public DemoMetadata? Metadata { get; set; }
        public List<DemoProfileData>? Profiles { get; set; }
        public List<DemoPostData>? Posts { get; set; }
    }
    
    public class DemoMetadata
    {
        public string? Category { get; set; }
        public string? Description { get; set; }
        public string? CreatedAt { get; set; }
        public string? ProfileTypeId { get; set; }
        public int? PostType { get; set; }
    }
    
    public class DemoProfileData
    {
        public string Id { get; set; } = "";
        public string? DisplayName { get; set; }
        public string? Handle { get; set; }
        public string? Bio { get; set; }
        public string? Avatar { get; set; }
    }
    
    public class DemoPostData
    {
        public string Id { get; set; } = "";
        public string ProfileId { get; set; } = "";
        public int? PostType { get; set; }
        public string? Title { get; set; }
        public string? Content { get; set; }
        public List<string>? ImageUrls { get; set; }
        public DemoLocationData? Location { get; set; }
        public List<string>? Tags { get; set; }
        public DemoPricingData? PricingInfo { get; set; }
        public DemoBusinessMetadata? BusinessMetadata { get; set; }
        public DemoBusinessLocationDetails? BusinessLocationDetails { get; set; }
        
        /// <summary>
        /// Pre-computed content embedding as PostgreSQL vector format string.
        /// Format: "[0.123,0.456,0.789,...]" - 384 dimensions for all-MiniLM-L6-v2
        /// </summary>
        public string? ContentEmbedding { get; set; }
    }
    
    public class DemoBusinessLocationDetails
    {
        public int? BusinessLocationType { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? PostalCode { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? WorkingHours { get; set; }
    }
    
    public class DemoLocationData
    {
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
    
    public class DemoPricingData
    {
        public decimal? Amount { get; set; }
        public string? Currency { get; set; }
        public string? Description { get; set; }
        public bool? IsNegotiable { get; set; }
    }
    
    public class DemoBusinessMetadata
    {
        public string? LocationType { get; set; }
        public string? ContactPhone { get; set; }
        public string? ContactEmail { get; set; }
        public bool? AcceptsWalkIns { get; set; }
        public bool? RequiresAppointment { get; set; }
        public DemoWorkingHours? WorkingHours { get; set; }
        public string? SpecialInstructions { get; set; }
    }
    
    public class DemoWorkingHours
    {
        public DemoDaySchedule? Monday { get; set; }
        public DemoDaySchedule? Tuesday { get; set; }
        public DemoDaySchedule? Wednesday { get; set; }
        public DemoDaySchedule? Thursday { get; set; }
        public DemoDaySchedule? Friday { get; set; }
        public DemoDaySchedule? Saturday { get; set; }
        public DemoDaySchedule? Sunday { get; set; }
    }
    
    public class DemoDaySchedule
    {
        public bool? IsClosed { get; set; }
        public string? OpenTime { get; set; }
        public string? CloseTime { get; set; }
    }
    
    #endregion
}