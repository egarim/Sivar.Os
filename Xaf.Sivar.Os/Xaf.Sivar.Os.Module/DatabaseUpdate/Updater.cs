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
            
            // Seed contact type catalog (Phase 1: Contact Actions)
            SeedContactTypes();
            
            ObjectSpace.CommitChanges(); //This line persists contact types
            
            // Seed agent capabilities (Phase 0.6: Relational capabilities)
            SeedAgentCapabilities();
            
            ObjectSpace.CommitChanges(); //This line persists agent capabilities
            
            // Seed chat bot settings (Phase 0.5: Configurable welcome messages)
            SeedChatBotSettings();
            
            ObjectSpace.CommitChanges(); //This line persists chat bot settings
            
            // Seed agent configurations (Phase 10: Multi-Agent Configuration)
            SeedAgentConfigurations();
            
            ObjectSpace.CommitChanges(); //This line persists agent configurations
            
            // Seed ranking configurations (Phase 11: Results Ranking & Personalization)
            SeedRankingConfigurations();
            
            ObjectSpace.CommitChanges(); //This line persists ranking configurations
            
            // Seed default profiles for users (runs in both DEBUG and RELEASE)
            SeedDefaultProfiles();

            ObjectSpace.CommitChanges(); //This line persists created object(s);
            
            // Seed demo data from DemoData folder (runs in both DEBUG and RELEASE)
            await SeedDemoDataAsync();
            
            ObjectSpace.CommitChanges(); //This line persists demo data
            
            // Seed demo contact info for business profiles (Phase 1: Contact Actions)
            SeedDemoContactInfo();
            
            ObjectSpace.CommitChanges(); //This line persists demo contact info
            
            // Seed ad budget for sponsored profiles (Search Ads System)
            SeedAdBudgets();
            
            ObjectSpace.CommitChanges(); //This line persists ad budgets
            
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
        /// Seeds agent capabilities for Phase 0.6: Relational capabilities
        /// Defines what AI functions are available and their parameters
        /// </summary>
        void SeedAgentCapabilities()
        {
            System.Diagnostics.Debug.WriteLine("[Updater] Starting SeedAgentCapabilities...");
            var now = DateTime.UtcNow;
            
            // Check if capabilities already exist
            var existingCapability = ObjectSpace.FirstOrDefault<AgentCapability>(c => c.Key == "search_posts");
            if (existingCapability != null)
            {
                System.Diagnostics.Debug.WriteLine("[Updater] Agent capabilities already exist. Skipping.");
                return;
            }
            
            // ===================================
            // 1. Search Posts Capability
            // ===================================
            var searchPosts = ObjectSpace.CreateObject<AgentCapability>();
            searchPosts.Id = Guid.Parse("b0000001-0001-0001-0001-000000000001");
            searchPosts.Key = "search_posts";
            searchPosts.Name = "Search Posts";
            searchPosts.Description = "Search for posts, businesses, places, and services by keyword or semantic similarity. Returns posts matching the query with content, location, and profile information.";
            searchPosts.FunctionName = "SearchPosts";
            searchPosts.Category = "search";
            searchPosts.Icon = "🔍";
            searchPosts.ExampleQueriesJson = @"[""pizzerías cerca de mí"", ""restaurantes italianos"", ""farmacias abiertas"", ""hoteles en la playa""]";
            searchPosts.UsageInstructions = "Use this function when the user wants to find businesses, places, services, or any general information. Supports both keyword and semantic search.";
            searchPosts.IsEnabled = true;
            searchPosts.SortOrder = 1;
            searchPosts.CreatedAt = now;
            searchPosts.UpdatedAt = now;
            
            // Parameters for SearchPosts
            var searchPostsQuery = ObjectSpace.CreateObject<CapabilityParameter>();
            searchPostsQuery.Id = Guid.Parse("b0000002-0001-0001-0001-000000000001");
            searchPostsQuery.CapabilityId = searchPosts.Id;
            searchPostsQuery.Name = "query";
            searchPostsQuery.DisplayName = "Search Query";
            searchPostsQuery.Description = "The search term or phrase to look for";
            searchPostsQuery.DataType = "string";
            searchPostsQuery.IsRequired = true;
            searchPostsQuery.SortOrder = 1;
            searchPostsQuery.CreatedAt = now;
            searchPostsQuery.UpdatedAt = now;
            
            var searchPostsLat = ObjectSpace.CreateObject<CapabilityParameter>();
            searchPostsLat.Id = Guid.Parse("b0000002-0001-0001-0001-000000000002");
            searchPostsLat.CapabilityId = searchPosts.Id;
            searchPostsLat.Name = "latitude";
            searchPostsLat.DisplayName = "Latitude";
            searchPostsLat.Description = "User's latitude for location-aware search";
            searchPostsLat.DataType = "number";
            searchPostsLat.IsRequired = false;
            searchPostsLat.SortOrder = 2;
            searchPostsLat.CreatedAt = now;
            searchPostsLat.UpdatedAt = now;
            
            var searchPostsLng = ObjectSpace.CreateObject<CapabilityParameter>();
            searchPostsLng.Id = Guid.Parse("b0000002-0001-0001-0001-000000000003");
            searchPostsLng.CapabilityId = searchPosts.Id;
            searchPostsLng.Name = "longitude";
            searchPostsLng.DisplayName = "Longitude";
            searchPostsLng.Description = "User's longitude for location-aware search";
            searchPostsLng.DataType = "number";
            searchPostsLng.IsRequired = false;
            searchPostsLng.SortOrder = 3;
            searchPostsLng.CreatedAt = now;
            searchPostsLng.UpdatedAt = now;
            
            var searchPostsRadius = ObjectSpace.CreateObject<CapabilityParameter>();
            searchPostsRadius.Id = Guid.Parse("b0000002-0001-0001-0001-000000000004");
            searchPostsRadius.CapabilityId = searchPosts.Id;
            searchPostsRadius.Name = "radiusKm";
            searchPostsRadius.DisplayName = "Radius (km)";
            searchPostsRadius.Description = "Search radius in kilometers";
            searchPostsRadius.DataType = "number";
            searchPostsRadius.IsRequired = false;
            searchPostsRadius.DefaultValue = "10";
            searchPostsRadius.SortOrder = 4;
            searchPostsRadius.CreatedAt = now;
            searchPostsRadius.UpdatedAt = now;
            
            System.Diagnostics.Debug.WriteLine("[Updater] ✅ Created SearchPosts capability.");
            
            // ===================================
            // 2. Search Profiles Capability
            // ===================================
            var searchProfiles = ObjectSpace.CreateObject<AgentCapability>();
            searchProfiles.Id = Guid.Parse("b0000001-0001-0001-0001-000000000002");
            searchProfiles.Key = "search_profiles";
            searchProfiles.Name = "Search Profiles";
            searchProfiles.Description = "Search for business profiles, users, and organizations by name or handle. Returns profile details including contact information and location.";
            searchProfiles.FunctionName = "SearchProfiles";
            searchProfiles.Category = "search";
            searchProfiles.Icon = "👤";
            searchProfiles.ExampleQueriesJson = @"[""buscar Pizza Hut"", ""perfil de McDonald's"", ""información de contacto de Pollo Campero""]";
            searchProfiles.UsageInstructions = "Use this function when the user asks specifically for a business profile or wants to find contact information for a specific place.";
            searchProfiles.IsEnabled = true;
            searchProfiles.SortOrder = 2;
            searchProfiles.CreatedAt = now;
            searchProfiles.UpdatedAt = now;
            
            // Parameters for SearchProfiles
            var searchProfilesQuery = ObjectSpace.CreateObject<CapabilityParameter>();
            searchProfilesQuery.Id = Guid.Parse("b0000002-0001-0001-0001-000000000005");
            searchProfilesQuery.CapabilityId = searchProfiles.Id;
            searchProfilesQuery.Name = "query";
            searchProfilesQuery.DisplayName = "Search Query";
            searchProfilesQuery.Description = "The business name or handle to search for";
            searchProfilesQuery.DataType = "string";
            searchProfilesQuery.IsRequired = true;
            searchProfilesQuery.SortOrder = 1;
            searchProfilesQuery.CreatedAt = now;
            searchProfilesQuery.UpdatedAt = now;
            
            System.Diagnostics.Debug.WriteLine("[Updater] ✅ Created SearchProfiles capability.");
            
            // ===================================
            // 3. Get Nearby Places Capability
            // ===================================
            var getNearby = ObjectSpace.CreateObject<AgentCapability>();
            getNearby.Id = Guid.Parse("b0000001-0001-0001-0001-000000000003");
            getNearby.Key = "get_nearby";
            getNearby.Name = "Get Nearby Places";
            getNearby.Description = "Find places near the user's location. Returns businesses and services within a specified radius.";
            getNearby.FunctionName = "GetNearbyPosts";
            getNearby.Category = "location";
            getNearby.Icon = "📍";
            getNearby.ExampleQueriesJson = @"[""qué hay cerca de mí"", ""lugares cercanos"", ""negocios cerca""]";
            getNearby.UsageInstructions = "Use this function when the user asks for places near them without a specific search term. Requires user location.";
            getNearby.IsEnabled = true;
            getNearby.SortOrder = 3;
            getNearby.CreatedAt = now;
            getNearby.UpdatedAt = now;
            
            // Parameters for GetNearby
            var getNearbyLat = ObjectSpace.CreateObject<CapabilityParameter>();
            getNearbyLat.Id = Guid.Parse("b0000002-0001-0001-0001-000000000006");
            getNearbyLat.CapabilityId = getNearby.Id;
            getNearbyLat.Name = "latitude";
            getNearbyLat.DisplayName = "Latitude";
            getNearbyLat.Description = "User's current latitude";
            getNearbyLat.DataType = "number";
            getNearbyLat.IsRequired = true;
            getNearbyLat.SortOrder = 1;
            getNearbyLat.CreatedAt = now;
            getNearbyLat.UpdatedAt = now;
            
            var getNearbyLng = ObjectSpace.CreateObject<CapabilityParameter>();
            getNearbyLng.Id = Guid.Parse("b0000002-0001-0001-0001-000000000007");
            getNearbyLng.CapabilityId = getNearby.Id;
            getNearbyLng.Name = "longitude";
            getNearbyLng.DisplayName = "Longitude";
            getNearbyLng.Description = "User's current longitude";
            getNearbyLng.DataType = "number";
            getNearbyLng.IsRequired = true;
            getNearbyLng.SortOrder = 2;
            getNearbyLng.CreatedAt = now;
            getNearbyLng.UpdatedAt = now;
            
            var getNearbyRadius = ObjectSpace.CreateObject<CapabilityParameter>();
            getNearbyRadius.Id = Guid.Parse("b0000002-0001-0001-0001-000000000008");
            getNearbyRadius.CapabilityId = getNearby.Id;
            getNearbyRadius.Name = "radiusKm";
            getNearbyRadius.DisplayName = "Radius (km)";
            getNearbyRadius.Description = "Search radius in kilometers";
            getNearbyRadius.DataType = "number";
            getNearbyRadius.IsRequired = false;
            getNearbyRadius.DefaultValue = "5";
            getNearbyRadius.SortOrder = 3;
            getNearbyRadius.CreatedAt = now;
            getNearbyRadius.UpdatedAt = now;
            
            System.Diagnostics.Debug.WriteLine("[Updater] ✅ Created GetNearby capability.");
            
            // ===================================
            // 4. Get Government Procedures Capability
            // ===================================
            var getProcedures = ObjectSpace.CreateObject<AgentCapability>();
            getProcedures.Id = Guid.Parse("b0000001-0001-0001-0001-000000000004");
            getProcedures.Key = "get_procedures";
            getProcedures.Name = "Get Government Procedures";
            getProcedures.Description = "Search for government procedures, paperwork requirements, and official processes in El Salvador.";
            getProcedures.FunctionName = "SearchPosts";
            getProcedures.Category = "information";
            getProcedures.Icon = "🏛️";
            getProcedures.ExampleQueriesJson = @"[""cómo sacar pasaporte"", ""requisitos para DUI"", ""trámites de licencia de conducir""]";
            getProcedures.UsageInstructions = "Use SearchPosts with government-related keywords when users ask about official procedures, documents, or paperwork.";
            getProcedures.IsEnabled = true;
            getProcedures.SortOrder = 4;
            getProcedures.CreatedAt = now;
            getProcedures.UpdatedAt = now;
            
            System.Diagnostics.Debug.WriteLine("[Updater] ✅ Created GetProcedures capability.");
            
            // ===================================
            // 5. Get Events Capability
            // ===================================
            var getEvents = ObjectSpace.CreateObject<AgentCapability>();
            getEvents.Id = Guid.Parse("b0000001-0001-0001-0001-000000000005");
            getEvents.Key = "get_events";
            getEvents.Name = "Get Events";
            getEvents.Description = "Search for events, festivals, shows, and activities happening in El Salvador.";
            getEvents.FunctionName = "SearchPosts";
            getEvents.Category = "entertainment";
            getEvents.Icon = "🎉";
            getEvents.ExampleQueriesJson = @"[""eventos este fin de semana"", ""conciertos"", ""festivales"", ""qué hacer hoy""]";
            getEvents.UsageInstructions = "Use SearchPosts with event-related keywords when users ask about things to do, events, or entertainment.";
            getEvents.IsEnabled = true;
            getEvents.SortOrder = 5;
            getEvents.CreatedAt = now;
            getEvents.UpdatedAt = now;
            
            System.Diagnostics.Debug.WriteLine("[Updater] ✅ Created GetEvents capability.");
            
            System.Diagnostics.Debug.WriteLine("[Updater] Finished SeedAgentCapabilities.");
        }
        
        /// <summary>
        /// Seeds default chat bot settings for Phase 0.5: Configurable welcome messages
        /// Creates default settings for Spanish (es) culture
        /// </summary>
        void SeedChatBotSettings()
        {
            System.Diagnostics.Debug.WriteLine("[Updater] Starting SeedChatBotSettings...");
            var now = DateTime.UtcNow;

            // Check if default settings already exist
            var existingSettings = ObjectSpace.FirstOrDefault<ChatBotSettings>(s => s.Key == "default");
            if (existingSettings != null)
            {
                System.Diagnostics.Debug.WriteLine("[Updater] Default ChatBotSettings already exists. Skipping.");
                return;
            }

            // Get capability IDs for linking QuickActions
            var searchPostsCapabilityId = Guid.Parse("b0000001-0001-0001-0001-000000000001");
            var getProceduresCapabilityId = Guid.Parse("b0000001-0001-0001-0001-000000000004");
            var getNearbyCapabilityId = Guid.Parse("b0000001-0001-0001-0001-000000000003");
            var getEventsCapabilityId = Guid.Parse("b0000001-0001-0001-0001-000000000005");

            // Create default settings (Spanish)
            var defaultSettings = ObjectSpace.CreateObject<ChatBotSettings>();
            defaultSettings.Id = Guid.Parse("a0000001-0001-0001-0001-000000000001");
            defaultSettings.Key = "default";
            defaultSettings.Culture = "es";
            defaultSettings.WelcomeMessage = @"¡Hola! Soy tu asistente Sivar AI. Puedo ayudarte a:

🔍 Encontrar negocios y servicios
📝 Buscar lugares y eventos
🏪 Descubrir lo mejor de El Salvador
📋 Guiarte en trámites y papeleos

¡Pregúntame algo como ""pizzerías cerca"" o ""cómo sacar pasaporte""!";
            defaultSettings.HeaderTagline = "Siempre aquí para ayudarte";
            defaultSettings.BotName = "Sivar AI Assistant";
            defaultSettings.QuickActionsJson = @"[""🍕 Buscar comida"", ""🏛️ Trámites"", ""📍 Cerca de mí"", ""🎉 Eventos""]"; // Legacy JSON kept for backward compatibility
            defaultSettings.SystemPrompt = @"Eres Sivar AI, un asistente virtual amigable y conocedor de El Salvador. 
Ayudas a los usuarios a encontrar negocios, servicios, lugares y eventos en El Salvador.
Respondes en español de forma concisa y útil.
Cuando busques información, usa las funciones disponibles para buscar en la base de datos.
Siempre sé cortés y positivo.";
            defaultSettings.ErrorMessage = "Lo siento, ocurrió un error. Por favor intenta de nuevo.";
            defaultSettings.ThinkingMessage = "Pensando...";
            defaultSettings.IsActive = true;
            defaultSettings.Priority = 0;
            defaultSettings.RegionCode = "SV";
            defaultSettings.CreatedAt = now;
            defaultSettings.UpdatedAt = now;

            // Create QuickActions for default Spanish settings
            var qaFoodEs = ObjectSpace.CreateObject<QuickAction>();
            qaFoodEs.Id = Guid.Parse("c0000003-0001-0001-0001-000000000001");
            qaFoodEs.ChatBotSettingsId = defaultSettings.Id;
            qaFoodEs.CapabilityId = searchPostsCapabilityId;
            qaFoodEs.Label = "🍕 Buscar comida";
            qaFoodEs.Icon = "🍕";
            qaFoodEs.Color = "#FF5722";
            qaFoodEs.DefaultQuery = "Buscar restaurantes y comida cerca de mi ubicación";
            qaFoodEs.ContextHint = "El usuario quiere encontrar opciones de comida cercanas";
            qaFoodEs.SortOrder = 1;
            qaFoodEs.IsActive = true;
            qaFoodEs.RequiresLocation = true;
            qaFoodEs.CreatedAt = now;
            qaFoodEs.UpdatedAt = now;
            
            var qaProceduresEs = ObjectSpace.CreateObject<QuickAction>();
            qaProceduresEs.Id = Guid.Parse("c0000003-0001-0001-0001-000000000002");
            qaProceduresEs.ChatBotSettingsId = defaultSettings.Id;
            qaProceduresEs.CapabilityId = getProceduresCapabilityId;
            qaProceduresEs.Label = "🏛️ Trámites";
            qaProceduresEs.Icon = "🏛️";
            qaProceduresEs.Color = "#3F51B5";
            qaProceduresEs.DefaultQuery = "¿Qué trámites gubernamentales puedo realizar?";
            qaProceduresEs.ContextHint = "El usuario quiere información sobre trámites y procedimientos gubernamentales";
            qaProceduresEs.SortOrder = 2;
            qaProceduresEs.IsActive = true;
            qaProceduresEs.RequiresLocation = false;
            qaProceduresEs.CreatedAt = now;
            qaProceduresEs.UpdatedAt = now;
            
            var qaNearbyEs = ObjectSpace.CreateObject<QuickAction>();
            qaNearbyEs.Id = Guid.Parse("c0000003-0001-0001-0001-000000000003");
            qaNearbyEs.ChatBotSettingsId = defaultSettings.Id;
            qaNearbyEs.CapabilityId = getNearbyCapabilityId;
            qaNearbyEs.Label = "📍 Cerca de mí";
            qaNearbyEs.Icon = "📍";
            qaNearbyEs.Color = "#4CAF50";
            qaNearbyEs.DefaultQuery = "¿Qué hay cerca de mi ubicación?";
            qaNearbyEs.ContextHint = "El usuario quiere ver lugares cercanos a su ubicación actual";
            qaNearbyEs.SortOrder = 3;
            qaNearbyEs.IsActive = true;
            qaNearbyEs.RequiresLocation = true;
            qaNearbyEs.CreatedAt = now;
            qaNearbyEs.UpdatedAt = now;
            
            var qaEventsEs = ObjectSpace.CreateObject<QuickAction>();
            qaEventsEs.Id = Guid.Parse("c0000003-0001-0001-0001-000000000004");
            qaEventsEs.ChatBotSettingsId = defaultSettings.Id;
            qaEventsEs.CapabilityId = getEventsCapabilityId;
            qaEventsEs.Label = "🎉 Eventos";
            qaEventsEs.Icon = "🎉";
            qaEventsEs.Color = "#9C27B0";
            qaEventsEs.DefaultQuery = "¿Qué eventos hay este fin de semana?";
            qaEventsEs.ContextHint = "El usuario quiere saber sobre eventos y actividades";
            qaEventsEs.SortOrder = 4;
            qaEventsEs.IsActive = true;
            qaEventsEs.RequiresLocation = false;
            qaEventsEs.CreatedAt = now;
            qaEventsEs.UpdatedAt = now;

            System.Diagnostics.Debug.WriteLine("[Updater] ✅ Created default ChatBotSettings with QuickActions.");

            // Create English settings
            var englishSettings = ObjectSpace.CreateObject<ChatBotSettings>();
            englishSettings.Id = Guid.Parse("a0000001-0001-0001-0001-000000000002");
            englishSettings.Key = "en";
            englishSettings.Culture = "en";
            englishSettings.WelcomeMessage = @"Hello! I'm your Sivar AI assistant. I can help you:

🔍 Find businesses and services
📝 Search for places and events
🏪 Discover the best of El Salvador
📋 Guide you through procedures and paperwork

Ask me something like ""pizza places nearby"" or ""how to get a passport""!";
            englishSettings.HeaderTagline = "Always here to help you explore";
            englishSettings.BotName = "Sivar AI Assistant";
            englishSettings.QuickActionsJson = @"[""🍕 Find food"", ""🏛️ Procedures"", ""📍 Near me"", ""🎉 Events""]"; // Legacy JSON kept for backward compatibility
            englishSettings.SystemPrompt = @"You are Sivar AI, a friendly and knowledgeable virtual assistant for El Salvador.
You help users find businesses, services, places, and events in El Salvador.
Respond concisely and helpfully.
When searching for information, use available functions to search the database.
Always be polite and positive.";
            englishSettings.ErrorMessage = "Sorry, an error occurred. Please try again.";
            englishSettings.ThinkingMessage = "Thinking...";
            englishSettings.IsActive = true;
            englishSettings.Priority = 0;
            englishSettings.CreatedAt = now;
            englishSettings.UpdatedAt = now;

            // Create QuickActions for English settings
            var qaFoodEn = ObjectSpace.CreateObject<QuickAction>();
            qaFoodEn.Id = Guid.Parse("c0000003-0001-0001-0001-000000000005");
            qaFoodEn.ChatBotSettingsId = englishSettings.Id;
            qaFoodEn.CapabilityId = searchPostsCapabilityId;
            qaFoodEn.Label = "🍕 Find food";
            qaFoodEn.Icon = "🍕";
            qaFoodEn.Color = "#FF5722";
            qaFoodEn.DefaultQuery = "Find restaurants and food near my location";
            qaFoodEn.ContextHint = "User wants to find food options nearby";
            qaFoodEn.SortOrder = 1;
            qaFoodEn.IsActive = true;
            qaFoodEn.RequiresLocation = true;
            qaFoodEn.CreatedAt = now;
            qaFoodEn.UpdatedAt = now;
            
            var qaProceduresEn = ObjectSpace.CreateObject<QuickAction>();
            qaProceduresEn.Id = Guid.Parse("c0000003-0001-0001-0001-000000000006");
            qaProceduresEn.ChatBotSettingsId = englishSettings.Id;
            qaProceduresEn.CapabilityId = getProceduresCapabilityId;
            qaProceduresEn.Label = "🏛️ Procedures";
            qaProceduresEn.Icon = "🏛️";
            qaProceduresEn.Color = "#3F51B5";
            qaProceduresEn.DefaultQuery = "What government procedures can I do?";
            qaProceduresEn.ContextHint = "User wants information about government procedures";
            qaProceduresEn.SortOrder = 2;
            qaProceduresEn.IsActive = true;
            qaProceduresEn.RequiresLocation = false;
            qaProceduresEn.CreatedAt = now;
            qaProceduresEn.UpdatedAt = now;
            
            var qaNearbyEn = ObjectSpace.CreateObject<QuickAction>();
            qaNearbyEn.Id = Guid.Parse("c0000003-0001-0001-0001-000000000007");
            qaNearbyEn.ChatBotSettingsId = englishSettings.Id;
            qaNearbyEn.CapabilityId = getNearbyCapabilityId;
            qaNearbyEn.Label = "📍 Near me";
            qaNearbyEn.Icon = "📍";
            qaNearbyEn.Color = "#4CAF50";
            qaNearbyEn.DefaultQuery = "What's near my location?";
            qaNearbyEn.ContextHint = "User wants to see places near their current location";
            qaNearbyEn.SortOrder = 3;
            qaNearbyEn.IsActive = true;
            qaNearbyEn.RequiresLocation = true;
            qaNearbyEn.CreatedAt = now;
            qaNearbyEn.UpdatedAt = now;
            
            var qaEventsEn = ObjectSpace.CreateObject<QuickAction>();
            qaEventsEn.Id = Guid.Parse("c0000003-0001-0001-0001-000000000008");
            qaEventsEn.ChatBotSettingsId = englishSettings.Id;
            qaEventsEn.CapabilityId = getEventsCapabilityId;
            qaEventsEn.Label = "🎉 Events";
            qaEventsEn.Icon = "🎉";
            qaEventsEn.Color = "#9C27B0";
            qaEventsEn.DefaultQuery = "What events are happening this weekend?";
            qaEventsEn.ContextHint = "User wants to know about events and activities";
            qaEventsEn.SortOrder = 4;
            qaEventsEn.IsActive = true;
            qaEventsEn.RequiresLocation = false;
            qaEventsEn.CreatedAt = now;
            qaEventsEn.UpdatedAt = now;

            System.Diagnostics.Debug.WriteLine("[Updater] ✅ Created English ChatBotSettings with QuickActions.");
        }
        
        /// <summary>
        /// Seeds agent configurations for Phase 10: Multi-Agent Configuration
        /// Creates default "sivar-main" agent and all available tools
        /// </summary>
        void SeedAgentConfigurations()
        {
            System.Diagnostics.Debug.WriteLine("[Updater] Starting SeedAgentConfigurations (Phase 10)...");
            var now = DateTime.UtcNow;
            
            // Check if default agent already exists
            var existingAgent = ObjectSpace.GetObjectsQuery<AgentConfiguration>()
                .FirstOrDefault(a => a.AgentKey == "sivar-main");
            
            if (existingAgent != null)
            {
                System.Diagnostics.Debug.WriteLine("[Updater] Agent configuration 'sivar-main' already exists. Skipping.");
                return;
            }
            
            // --- Create Default Agent Configuration ---
            var defaultAgent = ObjectSpace.CreateObject<AgentConfiguration>();
            defaultAgent.AgentKey = "sivar-main";
            defaultAgent.DisplayName = "Sivar Principal";
            defaultAgent.Description = "Agente principal para todas las consultas generales en Sivar.Os";
            defaultAgent.SystemPrompt = @"You are Sivar, a helpful AI assistant for the Sivar.Os social network platform in El Salvador.
You can help users:
- Search for profiles, posts, businesses, and places on the network
- Find nearby businesses and content using GPS location
- Get contact information (phone, email, WhatsApp) for businesses
- Get business hours and open/closed status
- Get directions and location information
- Help with government procedures and requirements (DUI, pasaporte, licencia, etc.)
- Follow and unfollow other users
- Get information about their own profile

IMPORTANT INSTRUCTIONS:
1. Always respond in Spanish when the user writes in Spanish.
2. When users ask for contact info, use GetContactInfo function.
3. When users ask about hours/schedule, use GetBusinessHours function.
4. When users ask for directions/location, use GetDirections function.
5. When users ask about procedures/requirements, use GetProcedureInfo function.
6. When showing links, always use RELATIVE URLs (starting with /) not absolute URLs.
7. Be friendly, helpful, and conversational.";
            defaultAgent.Provider = "ollama";
            defaultAgent.ModelId = "llama3.2:latest";
            defaultAgent.Temperature = 0.7;
            defaultAgent.MaxTokens = 2000;
            defaultAgent.Priority = 100;
            defaultAgent.IsActive = true;
            defaultAgent.Version = 1;
            defaultAgent.AbTestWeight = 100;
            defaultAgent.CreatedAt = now;
            defaultAgent.UpdatedAt = now;
            
            // Set enabled tools as JSON array
            var enabledTools = new List<string>
            {
                "SearchProfiles", "SearchPosts", "GetPostDetails", "FindBusinesses",
                "FollowProfile", "UnfollowProfile", "GetMyProfile",
                "SearchNearbyProfiles", "SearchNearbyPosts", "CalculateDistance",
                "GetAddressFromCoordinates", "GetCoordinatesFromAddress", "SearchNearMe", "GetCurrentLocationStatus",
                "GetContactInfo", "GetBusinessHours", "GetDirections", "GetProcedureInfo"
            };
            defaultAgent.SetEnabledTools(enabledTools);
            
            // Set intent patterns - matches everything as default
            var intentPatterns = new List<string> { ".*" };
            defaultAgent.SetIntentPatterns(intentPatterns);
            
            System.Diagnostics.Debug.WriteLine("[Updater] ✅ Created AgentConfiguration 'sivar-main'");
            
            // --- Create Agent Tools Registry ---
            var toolDefinitions = new[]
            {
                // Search tools
                ("SearchProfiles", "Buscar Perfiles", "Search", "Busca perfiles por nombre, tipo, o palabras clave", 1),
                ("SearchPosts", "Buscar Publicaciones", "Search", "Busca publicaciones por contenido", 2),
                ("GetPostDetails", "Ver Publicación", "Search", "Obtiene detalles completos de una publicación", 3),
                ("FindBusinesses", "Buscar Negocios", "Search", "Busca negocios por categoría y ubicación", 4),
                
                // Profile tools
                ("FollowProfile", "Seguir Perfil", "Profile", "Sigue a un perfil", 10),
                ("UnfollowProfile", "Dejar de Seguir", "Profile", "Deja de seguir a un perfil", 11),
                ("GetMyProfile", "Mi Perfil", "Profile", "Obtiene información del perfil activo", 12),
                
                // Location tools
                ("SearchNearbyProfiles", "Perfiles Cercanos", "Location", "Busca perfiles cerca de una ubicación", 20),
                ("SearchNearbyPosts", "Publicaciones Cercanas", "Location", "Busca publicaciones cerca de una ubicación", 21),
                ("CalculateDistance", "Calcular Distancia", "Location", "Calcula distancia entre dos puntos", 22),
                ("GetAddressFromCoordinates", "Geocodificación Inversa", "Location", "Obtiene dirección desde coordenadas GPS", 23),
                ("GetCoordinatesFromAddress", "Geocodificación", "Location", "Obtiene coordenadas desde una dirección", 24),
                ("SearchNearMe", "Buscar Cerca de Mí", "Location", "Busca contenido cerca del usuario", 25),
                ("GetCurrentLocationStatus", "Estado de Ubicación", "Location", "Verifica el estado del GPS", 26),
                
                // Business tools
                ("GetContactInfo", "Información de Contacto", "Business", "Obtiene teléfono, email, WhatsApp de un negocio", 30),
                ("GetBusinessHours", "Horarios de Atención", "Business", "Obtiene horarios de un negocio", 31),
                ("GetDirections", "Direcciones", "Business", "Obtiene direcciones hacia un negocio", 32),
                
                // Government tools
                ("GetProcedureInfo", "Información de Trámites", "Government", "Obtiene requisitos y pasos para trámites gubernamentales", 40)
            };
            
            foreach (var (functionName, displayName, category, description, sortOrder) in toolDefinitions)
            {
                // Check if tool already exists
                var existingTool = ObjectSpace.GetObjectsQuery<AgentTool>()
                    .FirstOrDefault(t => t.FunctionName == functionName);
                
                if (existingTool != null) continue;
                
                var tool = ObjectSpace.CreateObject<AgentTool>();
                tool.FunctionName = functionName;
                tool.DisplayName = displayName;
                tool.Category = category;
                tool.Description = description;
                tool.SortOrder = sortOrder;
                tool.IsActive = true;
                tool.IsExternalCall = category == "Location" && (functionName.Contains("Address") || functionName == "GetDirections");
                tool.CreatedAt = now;
                tool.UpdatedAt = now;
            }
            
            System.Diagnostics.Debug.WriteLine($"[Updater] ✅ Created {toolDefinitions.Length} AgentTool entries");
        }
        
        /// <summary>
        /// Seeds ranking configurations for Phase 11: Results Ranking & Personalization
        /// Creates default global ranking weights
        /// </summary>
        void SeedRankingConfigurations()
        {
            System.Diagnostics.Debug.WriteLine("[Updater] Starting SeedRankingConfigurations (Phase 11)...");
            var now = DateTime.UtcNow;
            
            // Check if default config already exists
            var existingConfig = ObjectSpace.GetObjectsQuery<RankingConfiguration>()
                .FirstOrDefault(c => c.Category == null && c.AbTestVariant == null);
            
            if (existingConfig != null)
            {
                System.Diagnostics.Debug.WriteLine("[Updater] Default RankingConfiguration already exists. Skipping.");
                return;
            }
            
            // --- Create Default Global Ranking Configuration ---
            var defaultConfig = ObjectSpace.CreateObject<RankingConfiguration>();
            defaultConfig.DisplayName = "Configuración Global";
            defaultConfig.Description = "Pesos de ranking predeterminados para todas las búsquedas";
            defaultConfig.Category = null; // null = global default
            
            // Content relevance weights (sum ~0.50)
            defaultConfig.SemanticWeight = 0.25;
            defaultConfig.FullTextWeight = 0.15;
            defaultConfig.GeoWeight = 0.10;
            
            // Quality signal weights (sum ~0.25)
            defaultConfig.RatingWeight = 0.10;
            defaultConfig.ReviewCountWeight = 0.05;
            defaultConfig.VerifiedWeight = 0.05;
            defaultConfig.RecencyWeight = 0.05;
            
            // Content ranking weight (Elo system)
            defaultConfig.ContentRankWeight = 0.10;
            
            // Personalization weights (sum ~0.10)
            defaultConfig.PersonalizationWeight = 0.05;
            defaultConfig.CategoryPreferenceWeight = 0.05;
            
            // Behavioral weights (start at 0, increase as data grows)
            defaultConfig.ClickPopularityWeight = 0.025;
            defaultConfig.ActionRateWeight = 0.025;
            
            defaultConfig.IsActive = true;
            defaultConfig.Priority = 0;
            defaultConfig.AbTestTrafficPercent = 100;
            defaultConfig.CreatedAt = now;
            defaultConfig.UpdatedAt = now;
            
            System.Diagnostics.Debug.WriteLine("[Updater] ✅ Created default RankingConfiguration");
            
            // --- Create Restaurant-specific Ranking Configuration ---
            var restaurantConfig = ObjectSpace.CreateObject<RankingConfiguration>();
            restaurantConfig.DisplayName = "Restaurantes";
            restaurantConfig.Description = "Pesos de ranking optimizados para búsquedas de restaurantes";
            restaurantConfig.Category = "restaurant";
            
            // Higher geo weight for restaurants (people want nearby)
            restaurantConfig.SemanticWeight = 0.20;
            restaurantConfig.FullTextWeight = 0.10;
            restaurantConfig.GeoWeight = 0.20; // Higher!
            
            // Higher rating weight for restaurants
            restaurantConfig.RatingWeight = 0.15; // Higher!
            restaurantConfig.ReviewCountWeight = 0.10; // Higher!
            restaurantConfig.VerifiedWeight = 0.05;
            restaurantConfig.RecencyWeight = 0.02;
            
            restaurantConfig.ContentRankWeight = 0.08;
            restaurantConfig.PersonalizationWeight = 0.05;
            restaurantConfig.CategoryPreferenceWeight = 0.03;
            restaurantConfig.ClickPopularityWeight = 0.01;
            restaurantConfig.ActionRateWeight = 0.01;
            
            restaurantConfig.IsActive = true;
            restaurantConfig.Priority = 10; // Higher priority than global
            restaurantConfig.AbTestTrafficPercent = 100;
            restaurantConfig.CreatedAt = now;
            restaurantConfig.UpdatedAt = now;
            
            System.Diagnostics.Debug.WriteLine("[Updater] ✅ Created restaurant RankingConfiguration");
        }
        
        /// <summary>
        /// Seeds contact type catalog for Phase 1: Contact Actions
        /// Creates contact types for phone, messaging, social, email, web, location, delivery
        /// </summary>
        void SeedContactTypes()
        {
            System.Diagnostics.Debug.WriteLine("[Updater] Starting SeedContactTypes...");
            var now = DateTime.UtcNow;
            var seededCount = 0;

            var contactTypes = new[]
            {
                // ============================================
                // PHONE / CALLING
                // ============================================
                new {
                    Id = Guid.Parse("c0000001-0000-0000-0000-000000000001"),
                    Key = "phone",
                    DisplayName = "Llamar",
                    Icon = "📞",
                    MudBlazorIcon = "Icons.Material.Filled.Phone",
                    Color = "#4CAF50",
                    UrlTemplate = "tel:+{country_code}{value}",
                    Category = "phone",
                    SortOrder = 1,
                    RegionalPopularity = @"{""SV"": 100, ""US"": 100, ""MX"": 100, ""GT"": 100}",
                    ValidationRegex = @"^\d{8}$",
                    Placeholder = (string?)null,
                    OpenInNewTab = false,
                    MobileOnly = false
                },
                new {
                    Id = Guid.Parse("c0000001-0000-0000-0000-000000000002"),
                    Key = "sms",
                    DisplayName = "SMS",
                    Icon = "💬",
                    MudBlazorIcon = "Icons.Material.Filled.Sms",
                    Color = "#2196F3",
                    UrlTemplate = "sms:+{country_code}{value}?body={message}",
                    Category = "phone",
                    SortOrder = 2,
                    RegionalPopularity = @"{""SV"": 60, ""US"": 80}",
                    ValidationRegex = (string?)null,
                    Placeholder = (string?)null,
                    OpenInNewTab = false,
                    MobileOnly = true
                },

                // ============================================
                // MESSAGING APPS
                // ============================================
                new {
                    Id = Guid.Parse("c0000002-0000-0000-0000-000000000001"),
                    Key = "whatsapp",
                    DisplayName = "WhatsApp",
                    Icon = "💬",
                    MudBlazorIcon = "Icons.Custom.Brands.WhatsApp",
                    Color = "#25D366",
                    UrlTemplate = "https://wa.me/{country_code}{value}?text={message}",
                    Category = "messaging",
                    SortOrder = 1,
                    RegionalPopularity = @"{""SV"": 100, ""MX"": 95, ""ES"": 90, ""BR"": 95, ""GT"": 98, ""HN"": 95, ""US"": 40, ""RU"": 10}",
                    ValidationRegex = @"^\d{8,15}$",
                    Placeholder = "7XXX-XXXX",
                    OpenInNewTab = true,
                    MobileOnly = false
                },
                new {
                    Id = Guid.Parse("c0000002-0000-0000-0000-000000000002"),
                    Key = "telegram",
                    DisplayName = "Telegram",
                    Icon = "✈️",
                    MudBlazorIcon = "Icons.Custom.Brands.Telegram",
                    Color = "#0088CC",
                    UrlTemplate = "https://t.me/{value}",
                    Category = "messaging",
                    SortOrder = 2,
                    RegionalPopularity = @"{""RU"": 100, ""IR"": 90, ""UA"": 85, ""US"": 30, ""SV"": 15}",
                    ValidationRegex = (string?)null,
                    Placeholder = "@username or +phone",
                    OpenInNewTab = true,
                    MobileOnly = false
                },
                new {
                    Id = Guid.Parse("c0000002-0000-0000-0000-000000000003"),
                    Key = "messenger",
                    DisplayName = "Messenger",
                    Icon = "💬",
                    MudBlazorIcon = "Icons.Custom.Brands.Facebook",
                    Color = "#0084FF",
                    UrlTemplate = "https://m.me/{value}",
                    Category = "messaging",
                    SortOrder = 3,
                    RegionalPopularity = @"{""US"": 70, ""SV"": 50, ""MX"": 45}",
                    ValidationRegex = (string?)null,
                    Placeholder = "Facebook username or Page ID",
                    OpenInNewTab = true,
                    MobileOnly = false
                },

                // ============================================
                // EMAIL
                // ============================================
                new {
                    Id = Guid.Parse("c0000003-0000-0000-0000-000000000001"),
                    Key = "email",
                    DisplayName = "Email",
                    Icon = "📧",
                    MudBlazorIcon = "Icons.Material.Filled.Email",
                    Color = "#EA4335",
                    UrlTemplate = "mailto:{value}?subject={subject}&body={message}",
                    Category = "email",
                    SortOrder = 1,
                    RegionalPopularity = @"{""SV"": 80, ""US"": 90, ""MX"": 75}",
                    ValidationRegex = @"^[\w\.-]+@[\w\.-]+\.\w+$",
                    Placeholder = (string?)null,
                    OpenInNewTab = false,
                    MobileOnly = false
                },

                // ============================================
                // SOCIAL MEDIA
                // ============================================
                new {
                    Id = Guid.Parse("c0000004-0000-0000-0000-000000000001"),
                    Key = "facebook",
                    DisplayName = "Facebook",
                    Icon = "📘",
                    MudBlazorIcon = "Icons.Custom.Brands.Facebook",
                    Color = "#1877F2",
                    UrlTemplate = "https://facebook.com/{value}",
                    Category = "social",
                    SortOrder = 1,
                    RegionalPopularity = @"{""SV"": 90, ""US"": 70, ""MX"": 85}",
                    ValidationRegex = (string?)null,
                    Placeholder = (string?)null,
                    OpenInNewTab = true,
                    MobileOnly = false
                },
                new {
                    Id = Guid.Parse("c0000004-0000-0000-0000-000000000002"),
                    Key = "instagram",
                    DisplayName = "Instagram",
                    Icon = "📷",
                    MudBlazorIcon = "Icons.Custom.Brands.Instagram",
                    Color = "#E4405F",
                    UrlTemplate = "https://instagram.com/{value}",
                    Category = "social",
                    SortOrder = 2,
                    RegionalPopularity = @"{""SV"": 85, ""US"": 80, ""MX"": 80}",
                    ValidationRegex = (string?)null,
                    Placeholder = "@username",
                    OpenInNewTab = true,
                    MobileOnly = false
                },
                new {
                    Id = Guid.Parse("c0000004-0000-0000-0000-000000000003"),
                    Key = "tiktok",
                    DisplayName = "TikTok",
                    Icon = "🎵",
                    MudBlazorIcon = "Icons.Custom.Brands.TikTok",
                    Color = "#000000",
                    UrlTemplate = "https://tiktok.com/@{value}",
                    Category = "social",
                    SortOrder = 3,
                    RegionalPopularity = @"{""SV"": 75, ""US"": 85, ""MX"": 80}",
                    ValidationRegex = (string?)null,
                    Placeholder = (string?)null,
                    OpenInNewTab = true,
                    MobileOnly = false
                },

                // ============================================
                // WEB
                // ============================================
                new {
                    Id = Guid.Parse("c0000005-0000-0000-0000-000000000001"),
                    Key = "website",
                    DisplayName = "Sitio Web",
                    Icon = "🌐",
                    MudBlazorIcon = "Icons.Material.Filled.Language",
                    Color = "#607D8B",
                    UrlTemplate = "{value}",
                    Category = "web",
                    SortOrder = 1,
                    RegionalPopularity = @"{""SV"": 70, ""US"": 90, ""MX"": 75}",
                    ValidationRegex = @"^https?://.*",
                    Placeholder = (string?)null,
                    OpenInNewTab = true,
                    MobileOnly = false
                },
                new {
                    Id = Guid.Parse("c0000005-0000-0000-0000-000000000002"),
                    Key = "menu",
                    DisplayName = "Menú",
                    Icon = "🍽️",
                    MudBlazorIcon = "Icons.Material.Filled.MenuBook",
                    Color = "#795548",
                    UrlTemplate = "{value}",
                    Category = "web",
                    SortOrder = 2,
                    RegionalPopularity = @"{""SV"": 80, ""US"": 85}",
                    ValidationRegex = (string?)null,
                    Placeholder = (string?)null,
                    OpenInNewTab = true,
                    MobileOnly = false
                },
                new {
                    Id = Guid.Parse("c0000005-0000-0000-0000-000000000003"),
                    Key = "booking",
                    DisplayName = "Reservar",
                    Icon = "📅",
                    MudBlazorIcon = "Icons.Material.Filled.EventAvailable",
                    Color = "#FF9800",
                    UrlTemplate = "{value}",
                    Category = "web",
                    SortOrder = 3,
                    RegionalPopularity = @"{""SV"": 60, ""US"": 80}",
                    ValidationRegex = (string?)null,
                    Placeholder = (string?)null,
                    OpenInNewTab = true,
                    MobileOnly = false
                },

                // ============================================
                // LOCATION / NAVIGATION
                // ============================================
                new {
                    Id = Guid.Parse("c0000006-0000-0000-0000-000000000001"),
                    Key = "google_maps",
                    DisplayName = "Google Maps",
                    Icon = "📍",
                    MudBlazorIcon = "Icons.Material.Filled.Map",
                    Color = "#4285F4",
                    UrlTemplate = "https://www.google.com/maps/search/?api=1&query={lat},{lng}",
                    Category = "location",
                    SortOrder = 1,
                    RegionalPopularity = @"{""SV"": 90, ""US"": 95, ""MX"": 90}",
                    ValidationRegex = (string?)null,
                    Placeholder = (string?)null,
                    OpenInNewTab = true,
                    MobileOnly = false
                },
                new {
                    Id = Guid.Parse("c0000006-0000-0000-0000-000000000002"),
                    Key = "waze",
                    DisplayName = "Waze",
                    Icon = "🚗",
                    MudBlazorIcon = (string?)null,
                    Color = "#33CCFF",
                    UrlTemplate = "https://waze.com/ul?ll={lat},{lng}&navigate=yes",
                    Category = "location",
                    SortOrder = 2,
                    RegionalPopularity = @"{""SV"": 85, ""US"": 50, ""MX"": 75, ""IL"": 90}",
                    ValidationRegex = (string?)null,
                    Placeholder = (string?)null,
                    OpenInNewTab = true,
                    MobileOnly = true
                },
                new {
                    Id = Guid.Parse("c0000006-0000-0000-0000-000000000003"),
                    Key = "directions",
                    DisplayName = "Cómo llegar",
                    Icon = "🧭",
                    MudBlazorIcon = "Icons.Material.Filled.Directions",
                    Color = "#4285F4",
                    UrlTemplate = "https://www.google.com/maps/dir/?api=1&destination={lat},{lng}",
                    Category = "location",
                    SortOrder = 3,
                    RegionalPopularity = @"{""SV"": 95, ""US"": 90, ""MX"": 90}",
                    ValidationRegex = (string?)null,
                    Placeholder = (string?)null,
                    OpenInNewTab = true,
                    MobileOnly = false
                },

                // ============================================
                // DELIVERY SERVICES (Regional)
                // ============================================
                new {
                    Id = Guid.Parse("c0000007-0000-0000-0000-000000000001"),
                    Key = "uber_eats",
                    DisplayName = "Uber Eats",
                    Icon = "🍔",
                    MudBlazorIcon = (string?)null,
                    Color = "#06C167",
                    UrlTemplate = "https://www.ubereats.com/store/{value}",
                    Category = "delivery",
                    SortOrder = 1,
                    RegionalPopularity = @"{""US"": 90, ""SV"": 75, ""MX"": 85}",
                    ValidationRegex = (string?)null,
                    Placeholder = (string?)null,
                    OpenInNewTab = true,
                    MobileOnly = false
                },
                new {
                    Id = Guid.Parse("c0000007-0000-0000-0000-000000000002"),
                    Key = "hugo",
                    DisplayName = "Hugo",
                    Icon = "🛵",
                    MudBlazorIcon = (string?)null,
                    Color = "#FF6B00",
                    UrlTemplate = "https://hugo.com/sv/store/{value}",
                    Category = "delivery",
                    SortOrder = 2,
                    RegionalPopularity = @"{""SV"": 95, ""GT"": 90, ""HN"": 85, ""NI"": 80}",
                    ValidationRegex = (string?)null,
                    Placeholder = (string?)null,
                    OpenInNewTab = true,
                    MobileOnly = false
                },
                new {
                    Id = Guid.Parse("c0000007-0000-0000-0000-000000000003"),
                    Key = "pedidosya",
                    DisplayName = "PedidosYa",
                    Icon = "🍕",
                    MudBlazorIcon = (string?)null,
                    Color = "#FA0050",
                    UrlTemplate = "https://www.pedidosya.com.sv/restaurantes/{value}",
                    Category = "delivery",
                    SortOrder = 3,
                    RegionalPopularity = @"{""SV"": 85, ""AR"": 95, ""UY"": 95, ""BO"": 80}",
                    ValidationRegex = (string?)null,
                    Placeholder = (string?)null,
                    OpenInNewTab = true,
                    MobileOnly = false
                }
            };

            foreach (var ct in contactTypes)
            {
                // Check if contact type already exists
                var existing = ObjectSpace.FirstOrDefault<ContactType>(x => x.Key == ct.Key);
                if (existing != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[Updater] Contact type '{ct.Key}' already exists. Skipping.");
                    continue;
                }

                var contactType = ObjectSpace.CreateObject<ContactType>();
                contactType.Id = ct.Id;
                contactType.Key = ct.Key;
                contactType.DisplayName = ct.DisplayName;
                contactType.Icon = ct.Icon;
                contactType.MudBlazorIcon = ct.MudBlazorIcon;
                contactType.Color = ct.Color;
                contactType.UrlTemplate = ct.UrlTemplate;
                contactType.Category = ct.Category;
                contactType.SortOrder = ct.SortOrder;
                contactType.RegionalPopularity = ct.RegionalPopularity;
                contactType.ValidationRegex = ct.ValidationRegex;
                contactType.Placeholder = ct.Placeholder;
                contactType.OpenInNewTab = ct.OpenInNewTab;
                contactType.MobileOnly = ct.MobileOnly;
                contactType.IsActive = true;
                contactType.CreatedAt = now;
                contactType.UpdatedAt = now;

                seededCount++;
                System.Diagnostics.Debug.WriteLine($"[Updater] ✓ Created contact type: {ct.Key}");
            }

            System.Diagnostics.Debug.WriteLine($"[Updater] ✅ Seeded {seededCount} contact types.");
        }
        
        /// <summary>
        /// Seeds demo contact information for business profiles (restaurants, entertainment, etc.)
        /// Creates WhatsApp, Phone, and Email contacts for each business profile
        /// </summary>
        void SeedDemoContactInfo()
        {
            System.Diagnostics.Debug.WriteLine("[Updater] Starting SeedDemoContactInfo...");
            var now = DateTime.UtcNow;
            
            // Check if demo contacts already exist
            var existingContact = ObjectSpace.FirstOrDefault<BusinessContactInfo>(c => c.IsDeleted == false);
            if (existingContact != null)
            {
                System.Diagnostics.Debug.WriteLine("[Updater] Demo contact info already exists. Skipping.");
                return;
            }
            
            // Query contact types by Key (in case they were created with different IDs)
            var phoneType = ObjectSpace.FirstOrDefault<ContactType>(x => x.Key == "phone");
            var whatsappType = ObjectSpace.FirstOrDefault<ContactType>(x => x.Key == "whatsapp");
            var emailType = ObjectSpace.FirstOrDefault<ContactType>(x => x.Key == "email");
            var websiteType = ObjectSpace.FirstOrDefault<ContactType>(x => x.Key == "website");
            var facebookType = ObjectSpace.FirstOrDefault<ContactType>(x => x.Key == "facebook");
            var instagramType = ObjectSpace.FirstOrDefault<ContactType>(x => x.Key == "instagram");
            var googleMapsType = ObjectSpace.FirstOrDefault<ContactType>(x => x.Key == "google_maps");
            
            if (phoneType == null || whatsappType == null || emailType == null)
            {
                System.Diagnostics.Debug.WriteLine("[Updater] Required contact types not found. Skipping contact info seeding.");
                return;
            }
            
            var phoneId = phoneType.Id;
            var whatsappId = whatsappType.Id;
            var emailId = emailType.Id;
            var websiteId = websiteType?.Id ?? Guid.Empty;
            var facebookId = facebookType?.Id ?? Guid.Empty;
            var instagramId = instagramType?.Id ?? Guid.Empty;
            var googleMapsId = googleMapsType?.Id ?? Guid.Empty;
            
            // Business profile type ID
            var businessProfileTypeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            
            // Get all business profiles
            var businessProfiles = ObjectSpace.GetObjectsQuery<Profile>()
                .Where(p => !p.IsDeleted && p.ProfileTypeId == businessProfileTypeId)
                .ToList();
            
            System.Diagnostics.Debug.WriteLine($"[Updater] Found {businessProfiles.Count} business profiles to add contacts to.");
            
            if (businessProfiles.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[Updater] No business profiles found. Skipping contact info seeding.");
                return;
            }
            
            var seededCount = 0;
            var random = new Random(42); // Fixed seed for reproducible demo data
            
            foreach (var profile in businessProfiles)
            {
                var contactsAdded = 0;
                
                // Generate a phone number base for this business (7-digit local number)
                var phoneBase = random.Next(2000, 9000).ToString() + "-" + random.Next(1000, 9999).ToString();
                
                // 1. WhatsApp (primary contact for most SV businesses)
                var whatsapp = ObjectSpace.CreateObject<BusinessContactInfo>();
                whatsapp.ProfileId = profile.Id;
                whatsapp.ContactTypeId = whatsappId;
                whatsapp.Value = phoneBase.Replace("-", "");
                whatsapp.Label = "WhatsApp";
                whatsapp.CountryCode = "503";
                whatsapp.SortOrder = 1;
                whatsapp.IsPrimary = true;
                whatsapp.IsActive = true;
                whatsapp.AvailableHours = "{\"weekdays\": \"8:00-22:00\", \"weekends\": \"9:00-21:00\"}";
                whatsapp.CreatedAt = now;
                whatsapp.UpdatedAt = now;
                contactsAdded++;
                
                // 2. Phone
                var phone = ObjectSpace.CreateObject<BusinessContactInfo>();
                phone.ProfileId = profile.Id;
                phone.ContactTypeId = phoneId;
                phone.Value = phoneBase.Replace("-", "");
                phone.Label = "Llamar";
                phone.CountryCode = "503";
                phone.SortOrder = 2;
                phone.IsPrimary = false;
                phone.IsActive = true;
                phone.CreatedAt = now;
                phone.UpdatedAt = now;
                contactsAdded++;
                
                // 3. Email (use profile handle if available)
                var emailHandle = profile.Handle?.ToLowerInvariant().Replace(" ", "") ?? "contacto";
                var email = ObjectSpace.CreateObject<BusinessContactInfo>();
                email.ProfileId = profile.Id;
                email.ContactTypeId = emailId;
                email.Value = $"{emailHandle}@ejemplo.sv";
                email.Label = "Email";
                email.SortOrder = 3;
                email.IsPrimary = false;
                email.IsActive = true;
                email.CreatedAt = now;
                email.UpdatedAt = now;
                contactsAdded++;
                
                // 4. Website (50% of businesses)
                if (websiteId != Guid.Empty && random.NextDouble() > 0.5)
                {
                    var website = ObjectSpace.CreateObject<BusinessContactInfo>();
                    website.ProfileId = profile.Id;
                    website.ContactTypeId = websiteId;
                    website.Value = $"https://www.{emailHandle}.com.sv";
                    website.Label = "Sitio Web";
                    website.SortOrder = 4;
                    website.IsPrimary = false;
                    website.IsActive = true;
                    website.CreatedAt = now;
                    website.UpdatedAt = now;
                    contactsAdded++;
                }
                
                // 5. Instagram (70% of businesses)
                if (instagramId != Guid.Empty && random.NextDouble() > 0.3)
                {
                    var instagram = ObjectSpace.CreateObject<BusinessContactInfo>();
                    instagram.ProfileId = profile.Id;
                    instagram.ContactTypeId = instagramId;
                    instagram.Value = emailHandle;
                    instagram.Label = "Instagram";
                    instagram.SortOrder = 5;
                    instagram.IsPrimary = false;
                    instagram.IsActive = true;
                    instagram.CreatedAt = now;
                    instagram.UpdatedAt = now;
                    contactsAdded++;
                }
                
                // 6. Facebook (60% of businesses)
                if (facebookId != Guid.Empty && random.NextDouble() > 0.4)
                {
                    var facebook = ObjectSpace.CreateObject<BusinessContactInfo>();
                    facebook.ProfileId = profile.Id;
                    facebook.ContactTypeId = facebookId;
                    facebook.Value = emailHandle;
                    facebook.Label = "Facebook";
                    facebook.SortOrder = 6;
                    facebook.IsPrimary = false;
                    facebook.IsActive = true;
                    facebook.CreatedAt = now;
                    facebook.UpdatedAt = now;
                    contactsAdded++;
                }
                
                // 7. Google Maps (80% of businesses - most have a location)
                if (googleMapsId != Guid.Empty && random.NextDouble() > 0.2 && profile.Location != null && profile.Location.Latitude.HasValue && profile.Location.Longitude.HasValue)
                {
                    var maps = ObjectSpace.CreateObject<BusinessContactInfo>();
                    maps.ProfileId = profile.Id;
                    maps.ContactTypeId = googleMapsId;
                    maps.Value = $"{profile.Location.Latitude},{profile.Location.Longitude}"; // lat,lng
                    maps.Label = "Ver en Mapa";
                    maps.SortOrder = 7;
                    maps.IsPrimary = false;
                    maps.IsActive = true;
                    maps.CreatedAt = now;
                    maps.UpdatedAt = now;
                    contactsAdded++;
                }
                
                seededCount++;
                System.Diagnostics.Debug.WriteLine($"[Updater] ✓ Added {contactsAdded} contacts to: {profile.Handle}");
            }
            
            System.Diagnostics.Debug.WriteLine($"[Updater] ✅ Seeded contacts for {seededCount} business profiles.");
        }
        
        /// <summary>
        /// Seeds ad budgets for demo business profiles to enable sponsored results
        /// Gives some profiles ad credits to appear as sponsored in search results
        /// </summary>
        void SeedAdBudgets()
        {
            System.Diagnostics.Debug.WriteLine("[Updater] Starting SeedAdBudgets...");
            var now = DateTime.UtcNow;
            
            // Check if ad budgets already seeded (look for any profile with budget > 0)
            var existingSponsored = ObjectSpace.FirstOrDefault<Profile>(p => p.AdBudget > 0 && !p.IsDeleted);
            if (existingSponsored != null)
            {
                System.Diagnostics.Debug.WriteLine("[Updater] Ad budgets already seeded. Skipping.");
                return;
            }
            
            // Business profile type ID
            var businessProfileTypeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            
            // Get business profiles to give ad budgets
            var businessProfiles = ObjectSpace.GetObjectsQuery<Profile>()
                .Where(p => !p.IsDeleted && p.ProfileTypeId == businessProfileTypeId)
                .ToList();
            
            System.Diagnostics.Debug.WriteLine($"[Updater] Found {businessProfiles.Count} business profiles for ad budget seeding.");
            
            if (businessProfiles.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[Updater] No business profiles found. Skipping ad budget seeding.");
                return;
            }
            
            var random = new Random(123); // Fixed seed for reproducible demo data
            var seededCount = 0;
            
            // Give ~40% of businesses some ad budget
            foreach (var profile in businessProfiles)
            {
                // 40% chance of being a sponsored profile
                if (random.NextDouble() > 0.4)
                    continue;
                
                // Enable sponsored
                profile.SponsoredEnabled = true;
                
                // Random budget between $25 and $200
                profile.AdBudget = (decimal)(random.NextDouble() * 175 + 25);
                profile.AdBudget = Math.Round(profile.AdBudget, 2);
                
                // Max bid per click: $0.10 to $0.50
                profile.MaxBidPerClick = (decimal)(random.NextDouble() * 0.4 + 0.1);
                profile.MaxBidPerClick = Math.Round(profile.MaxBidPerClick, 2);
                
                // Daily limit: $5 to $25
                profile.DailyAdLimit = (decimal)(random.NextDouble() * 20 + 5);
                profile.DailyAdLimit = Math.Round(profile.DailyAdLimit, 2);
                
                // Quality score: 0.5 to 1.0 (new advertisers start mid-range)
                profile.AdQualityScore = random.NextDouble() * 0.5 + 0.5;
                
                // Target radius: 5-25 km for local businesses
                profile.AdTargetRadiusKm = random.Next(5, 26);
                
                // Set target keywords based on category keys
                if (profile.CategoryKeys != null && profile.CategoryKeys.Length > 0)
                {
                    profile.AdTargetKeywords = JsonSerializer.Serialize(profile.CategoryKeys.Take(5).ToList());
                }
                
                seededCount++;
                System.Diagnostics.Debug.WriteLine(
                    $"[Updater] ✓ Ad budget for {profile.Handle}: ${profile.AdBudget:F2} budget, ${profile.MaxBidPerClick:F2}/click, {profile.AdTargetRadiusKm}km radius");
            }
            
            // Also seed a few AdTransaction records as bonus credits
            if (seededCount > 0)
            {
                var sponsoredProfiles = businessProfiles.Where(p => p.SponsoredEnabled).Take(5).ToList();
                foreach (var profile in sponsoredProfiles)
                {
                    var transaction = ObjectSpace.CreateObject<AdTransaction>();
                    transaction.ProfileId = profile.Id;
                    transaction.TransactionType = AdTransactionType.Bonus;
                    transaction.Amount = profile.AdBudget;
                    transaction.BalanceAfter = profile.AdBudget;
                    transaction.Description = "Welcome bonus - Demo ad credits";
                    transaction.Timestamp = DateTimeOffset.UtcNow;
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"[Updater] ✅ Seeded ad budgets for {seededCount} business profiles.");
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
                
                // Seed categories first (required for multilingual search)
                await SeedCategoriesAsync(demoDataPath);
                
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
        /// Seeds category definitions from DemoData/categories.json for multilingual search
        /// </summary>
        private async Task SeedCategoriesAsync(string demoDataPath)
        {
            var categoriesJsonPath = Path.Combine(demoDataPath, "categories.json");
            if (!File.Exists(categoriesJsonPath))
            {
                System.Diagnostics.Debug.WriteLine($"[Updater] Categories JSON not found at: {categoriesJsonPath}. Skipping.");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"[Updater] Loading categories from: {categoriesJsonPath}");
            
            var jsonContent = await File.ReadAllTextAsync(categoriesJsonPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var categoryDataList = JsonSerializer.Deserialize<List<CategoryJsonData>>(jsonContent, options);
            if (categoryDataList == null || categoryDataList.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[Updater] No categories found in JSON file.");
                return;
            }
            
            var now = DateTime.UtcNow;
            var categoryCount = 0;
            var sortOrder = 1;
            
            foreach (var categoryData in categoryDataList)
            {
                try
                {
                    // Check if category already exists by key
                    var existingCategory = ObjectSpace.FirstOrDefault<CategoryDefinition>(c => c.Key == categoryData.Key);
                    if (existingCategory != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Updater] Category '{categoryData.Key}' already exists. Skipping.");
                        continue;
                    }
                    
                    var category = ObjectSpace.CreateObject<CategoryDefinition>();
                    category.Key = categoryData.Key;
                    category.DisplayNameEn = categoryData.DisplayNameEn ?? categoryData.Key;
                    category.DisplayNameEs = categoryData.DisplayNameEs ?? categoryData.Key;
                    category.ParentKey = categoryData.ParentKey;
                    category.Synonyms = categoryData.Synonyms?.ToArray() ?? Array.Empty<string>();
                    category.Description = categoryData.Description;
                    category.IsActive = true;
                    category.SortOrder = sortOrder++;
                    category.CreatedAt = now;
                    category.UpdatedAt = now;
                    
                    categoryCount++;
                    System.Diagnostics.Debug.WriteLine($"[Updater] ✅ Created category: {categoryData.Key} ({categoryData.DisplayNameEn}) with {category.Synonyms.Length} synonyms");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Updater] ❌ Error creating category {categoryData.Key}: {ex.Message}");
                }
            }
            
            ObjectSpace.CommitChanges();
            System.Diagnostics.Debug.WriteLine($"[Updater] Created {categoryCount} category definitions.");
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
                    profile.CategoryKeys = profileData.CategoryKeys?.ToArray() ?? Array.Empty<string>(); // Phase 6: Multilingual search
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
                    post.CategoryKeys = postData.CategoryKeys?.ToArray() ?? Array.Empty<string>(); // Phase 6: Multilingual search
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
                    profile.CategoryKeys = profileData.CategoryKeys?.ToArray() ?? Array.Empty<string>(); // Phase 6: Multilingual search
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
                    post.CategoryKeys = postData.CategoryKeys?.ToArray() ?? Array.Empty<string>(); // Phase 6: Multilingual search
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
                    profile.CategoryKeys = profileData.CategoryKeys?.ToArray() ?? Array.Empty<string>(); // Phase 6: Multilingual search
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
                    post.CategoryKeys = postData.CategoryKeys?.ToArray() ?? Array.Empty<string>(); // Phase 6: Multilingual search
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
            Console.WriteLine("========== [Updater] SeedGovernmentAsync STARTED ==========");
            
            var governmentJsonPath = Path.Combine(demoDataPath, "Government", "government.json");
            if (!File.Exists(governmentJsonPath))
            {
                Console.WriteLine($"[Updater] ❌ Government JSON not found at: {governmentJsonPath}. Skipping.");
                System.Diagnostics.Debug.WriteLine($"[Updater] Government JSON not found at: {governmentJsonPath}. Skipping.");
                return;
            }
            
            Console.WriteLine($"[Updater] Loading government from: {governmentJsonPath}");
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
                Console.WriteLine("[Updater] ❌ Failed to parse government JSON.");
                System.Diagnostics.Debug.WriteLine("[Updater] Failed to parse government JSON.");
                return;
            }
            
            // Log parsed data counts
            var profilesCount = demoData.Profiles?.Count ?? 0;
            var postsCount = demoData.Posts?.Count ?? 0;
            Console.WriteLine($"[Updater] 📋 Parsed government.json: {profilesCount} profiles, {postsCount} posts");
            
            // Log first profile ID to verify correct IDs
            if (demoData.Profiles?.Count > 0)
            {
                Console.WriteLine($"[Updater] 📋 First profile ID: {demoData.Profiles[0].Id}");
            }
            if (demoData.Posts?.Count > 0)
            {
                Console.WriteLine($"[Updater] 📋 First post ID: {demoData.Posts[0].Id}");
            }
            
            var now = DateTime.UtcNow;
            var businessProfileTypeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            
            // Get the business profile type
            var businessProfileType = ObjectSpace.FirstOrDefault<ProfileType>(pt => pt.Id == businessProfileTypeId);
            if (businessProfileType == null)
            {
                Console.WriteLine("[Updater] ❌ Business profile type not found. Skipping government seeding.");
                System.Diagnostics.Debug.WriteLine("[Updater] Business profile type not found. Skipping government seeding.");
                return;
            }
            
            // Ensure system user exists
            var systemUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var systemUser = ObjectSpace.FirstOrDefault<User>(u => u.Id == systemUserId);
            if (systemUser == null)
            {
                Console.WriteLine("[Updater] ❌ System user not found. Skipping government seeding.");
                System.Diagnostics.Debug.WriteLine("[Updater] System user not found. Skipping government seeding.");
                return;
            }
            
            // Seed profiles
            var profileCount = 0;
            var skippedProfiles = 0;
            foreach (var profileData in demoData.Profiles ?? new List<DemoProfileData>())
            {
                try
                {
                    var profileId = Guid.Parse(profileData.Id);
                    
                    // Check if profile already exists
                    var existingProfile = ObjectSpace.FirstOrDefault<Profile>(p => p.Id == profileId);
                    if (existingProfile != null)
                    {
                        Console.WriteLine($"[Updater] ⏭️ Profile {profileData.DisplayName} (ID: {profileId}) already exists. Skipping.");
                        System.Diagnostics.Debug.WriteLine($"[Updater] Profile {profileData.DisplayName} already exists. Skipping.");
                        skippedProfiles++;
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
                    profile.CategoryKeys = profileData.CategoryKeys?.ToArray() ?? Array.Empty<string>(); // Phase 6: Multilingual search
                    profile.IsActive = true; // Demo profiles are active for directory listings
                    profile.VisibilityLevel = VisibilityLevel.Public;
                    profile.CreatedAt = now;
                    profile.UpdatedAt = now;
                    
                    profileCount++;
                    Console.WriteLine($"[Updater] ✅ Created government profile: {profileData.DisplayName} ({profileData.Handle}) ID: {profileId}");
                    System.Diagnostics.Debug.WriteLine($"[Updater] ✅ Created profile: {profileData.DisplayName} ({profileData.Handle})");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Updater] ❌ Error creating profile {profileData.DisplayName}: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"[Updater] ❌ Error creating profile {profileData.DisplayName}: {ex.Message}");
                }
            }
            
            ObjectSpace.CommitChanges();
            Console.WriteLine($"[Updater] 📊 Government profiles: {profileCount} created, {skippedProfiles} skipped");
            System.Diagnostics.Debug.WriteLine($"[Updater] Created {profileCount} government profiles.");
            
            // Seed posts
            var postCount = 0;
            var skippedPosts = 0;
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
                        Console.WriteLine($"[Updater] ⏭️ Gov post {postData.Title} (ID: {postId}) already exists. Skipping.");
                        System.Diagnostics.Debug.WriteLine($"[Updater] Post {postData.Title} already exists. Skipping.");
                        skippedPosts++;
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
                    post.CategoryKeys = postData.CategoryKeys?.ToArray() ?? Array.Empty<string>(); // Phase 6: Multilingual search
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
                    Console.WriteLine($"[Updater] ✅ Created gov post: {postData.Title} (ID: {postId})");
                    System.Diagnostics.Debug.WriteLine($"[Updater] ✅ Created post: {postData.Title}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Updater] ❌ Error creating gov post {postData.Title}: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"[Updater] ❌ Error creating post {postData.Title}: {ex.Message}");
                }
            }
            
            ObjectSpace.CommitChanges();
            Console.WriteLine($"[Updater] 📊 Government posts: {postCount} created, {skippedPosts} skipped");
            Console.WriteLine("========== [Updater] SeedGovernmentAsync FINISHED ==========");
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
                    profile.CategoryKeys = profileData.CategoryKeys?.ToArray() ?? Array.Empty<string>(); // Phase 6: Multilingual search
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
                    post.CategoryKeys = postData.CategoryKeys?.ToArray() ?? Array.Empty<string>(); // Phase 6: Multilingual search
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
        
        /// <summary>
        /// Normalized English category keys for multilingual search (Phase 6).
        /// Examples: ["pizza", "restaurant"], ["bank"], ["government_office", "passport_office"]
        /// </summary>
        public List<string>? CategoryKeys { get; set; }
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
        
        /// <summary>
        /// Normalized English category keys for multilingual search (Phase 6).
        /// Examples: ["pizza", "restaurant"], ["bank"], ["government_office", "passport_office"]
        /// </summary>
        public List<string>? CategoryKeys { get; set; }
        
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
    
    /// <summary>
    /// DTO for categories.json file structure - Phase 6: Multilingual Search
    /// </summary>
    public class CategoryJsonData
    {
        public string Key { get; set; } = "";
        public string? DisplayNameEn { get; set; }
        public string? DisplayNameEs { get; set; }
        public string? ParentKey { get; set; }
        public List<string>? Synonyms { get; set; }
        public string? Description { get; set; }
    }
    
    #endregion
}