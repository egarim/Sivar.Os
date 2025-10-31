# Phase 6: TimescaleDB Hypertables - Implementation Plan

**Created**: October 31, 2025  
**Branch**: `feature/phase6-timescaledb-hypertables`  
**Complexity**: ⭐⭐⭐⭐ Hard  
**Estimated Time**: 12-16 hours  
**Risk**: Medium-High (no production data, development only)

---

## Executive Summary

This phase converts time-series tables (`Activity`, `Post`, `ChatMessage`, `Notification`) to TimescaleDB hypertables for **massive performance improvements** on time-based queries.

### Key Benefits

- ✅ **10-100x faster time-range queries**
- ✅ **Automatic data partitioning** by time
- ✅ **90%+ storage savings** with compression
- ✅ **Automatic retention policies**
- ✅ **Optimized for INSERT-heavy workloads** (perfect for activity streams)

### Why This Matters for Sivar.Os

Your application is **heavily time-based**:
- Activity streams ordered by `PublishedAt`
- Posts ordered by `CreatedAt`
- Feed queries: "Get posts from last 7 days"
- Trending queries: "Get activities from last 24 hours"

**TimescaleDB hypertables** are specifically designed for this type of workload.

---

## Prerequisites Checklist

- [x] ✅ **TimescaleDB extension installed** (confirmed in posimp.md)
- [x] ✅ **PostgreSQL 14+** (confirmed)
- [x] ✅ **No production data** (development environment only)
- [x] ✅ **Database Script System** (from Phase 5, in Updater.cs)
- [x] ✅ **DEVELOPMENT_RULES.md pattern** (use `.Ignore()` for unsupported types)

---

## Strategy: Use Database Script System (Like Phase 5)

Following the proven pattern from Phase 5 (pgvector), we'll use:

1. ✅ **SQL scripts** for TimescaleDB-specific operations
2. ✅ **Updater.cs** to execute scripts automatically
3. ✅ **`.Ignore()` pattern** if we encounter EF Core incompatibilities
4. ✅ **No EF Core migrations** for TimescaleDB features

**Why:** EF Core doesn't understand TimescaleDB hypertables, just like it didn't understand pgvector.

---

## Tables to Convert

| Table | Time Column | Chunk Interval | Rationale |
|-------|-------------|----------------|-----------|
| `Sivar_Activities` | `PublishedAt` | 7 days | High INSERT rate, frequently queried by time |
| `Sivar_Posts` | `CreatedAt` | 30 days | Moderate INSERT rate, long retention |
| `Sivar_ChatMessages` | `CreatedAt` | 7 days | High INSERT rate, recent data most important |
| `Sivar_Notifications` | `CreatedAt` | 7 days | High INSERT rate, short retention period |

---

## Implementation Steps

### Step 1: Create TimescaleDB Enable Script ✅

**File**: `Sivar.Os.Data/Scripts/EnableTimescaleDB.sql`

```sql
-- =====================================================
-- Script: Enable TimescaleDB Extension
-- Purpose: Enable TimescaleDB extension in the database
-- Date: October 31, 2025
-- =====================================================

-- Enable TimescaleDB extension
CREATE EXTENSION IF NOT EXISTS timescaledb CASCADE;

-- Verify extension is installed
SELECT * FROM pg_extension WHERE extname = 'timescaledb';

-- =====================================================
-- IMPORTANT NOTES:
-- - This script is idempotent (safe to run multiple times)
-- - TimescaleDB must be installed in PostgreSQL before running
-- - Extension enables time-series optimizations for PostgreSQL
-- =====================================================
```

---

### Step 2: Create Hypertables Conversion Script ✅

**File**: `Sivar.Os.Data/Scripts/ConvertToHypertables.sql`

```sql
-- =====================================================
-- Script: Convert Tables to TimescaleDB Hypertables
-- Purpose: Convert time-series tables to hypertables for better performance
-- Date: October 31, 2025
-- =====================================================

-- ⚠️ CRITICAL: This script assumes tables exist and have data
-- If tables don't exist yet, hypertable creation will fail gracefully

-- Step 1: Convert Sivar_Activities to hypertable
DO $$
BEGIN
    -- Check if table exists and is not already a hypertable
    IF EXISTS (
        SELECT 1 FROM information_schema.tables 
        WHERE table_name = 'Sivar_Activities'
    ) AND NOT EXISTS (
        SELECT 1 FROM timescaledb_information.hypertables 
        WHERE hypertable_name = 'Sivar_Activities'
    ) THEN
        -- Convert to hypertable with 7-day chunks
        PERFORM create_hypertable(
            'Sivar_Activities', 
            'PublishedAt',
            chunk_time_interval => INTERVAL '7 days',
            if_not_exists => TRUE
        );
        RAISE NOTICE 'Converted Sivar_Activities to hypertable with 7-day chunks';
    ELSE
        RAISE NOTICE 'Sivar_Activities is already a hypertable or table does not exist';
    END IF;
EXCEPTION
    WHEN OTHERS THEN
        RAISE WARNING 'Could not convert Sivar_Activities: %', SQLERRM;
END $$;

-- Step 2: Convert Sivar_Posts to hypertable
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.tables 
        WHERE table_name = 'Sivar_Posts'
    ) AND NOT EXISTS (
        SELECT 1 FROM timescaledb_information.hypertables 
        WHERE hypertable_name = 'Sivar_Posts'
    ) THEN
        PERFORM create_hypertable(
            'Sivar_Posts', 
            'CreatedAt',
            chunk_time_interval => INTERVAL '30 days',
            if_not_exists => TRUE
        );
        RAISE NOTICE 'Converted Sivar_Posts to hypertable with 30-day chunks';
    ELSE
        RAISE NOTICE 'Sivar_Posts is already a hypertable or table does not exist';
    END IF;
EXCEPTION
    WHEN OTHERS THEN
        RAISE WARNING 'Could not convert Sivar_Posts: %', SQLERRM;
END $$;

-- Step 3: Convert Sivar_ChatMessages to hypertable
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.tables 
        WHERE table_name = 'Sivar_ChatMessages'
    ) AND NOT EXISTS (
        SELECT 1 FROM timescaledb_information.hypertables 
        WHERE hypertable_name = 'Sivar_ChatMessages'
    ) THEN
        PERFORM create_hypertable(
            'Sivar_ChatMessages', 
            'CreatedAt',
            chunk_time_interval => INTERVAL '7 days',
            if_not_exists => TRUE
        );
        RAISE NOTICE 'Converted Sivar_ChatMessages to hypertable with 7-day chunks';
    ELSE
        RAISE NOTICE 'Sivar_ChatMessages is already a hypertable or table does not exist';
    END IF;
EXCEPTION
    WHEN OTHERS THEN
        RAISE WARNING 'Could not convert Sivar_ChatMessages: %', SQLERRM;
END $$;

-- Step 4: Convert Sivar_Notifications to hypertable
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.tables 
        WHERE table_name = 'Sivar_Notifications'
    ) AND NOT EXISTS (
        SELECT 1 FROM timescaledb_information.hypertables 
        WHERE hypertable_name = 'Sivar_Notifications'
    ) THEN
        PERFORM create_hypertable(
            'Sivar_Notifications', 
            'CreatedAt',
            chunk_time_interval => INTERVAL '7 days',
            if_not_exists => TRUE
        );
        RAISE NOTICE 'Converted Sivar_Notifications to hypertable with 7-day chunks';
    ELSE
        RAISE NOTICE 'Sivar_Notifications is already a hypertable or table does not exist';
    END IF;
EXCEPTION
    WHEN OTHERS THEN
        RAISE WARNING 'Could not convert Sivar_Notifications: %', SQLERRM;
END $$;

-- Step 5: Verify conversions
SELECT 
    hypertable_schema,
    hypertable_name,
    num_dimensions,
    num_chunks
FROM timescaledb_information.hypertables
WHERE hypertable_name IN ('Sivar_Activities', 'Sivar_Posts', 'Sivar_ChatMessages', 'Sivar_Notifications')
ORDER BY hypertable_name;

-- =====================================================
-- IMPORTANT NOTES:
-- - This script is idempotent (safe to run multiple times)
-- - Tables must exist before conversion
-- - Time columns (PublishedAt, CreatedAt) must have NOT NULL constraint
-- - Existing indexes are preserved after conversion
-- - Foreign keys are preserved after conversion
-- - Chunk intervals can be changed later with set_chunk_time_interval()
-- =====================================================
```

---

### Step 3: Create Retention Policies Script ✅

**File**: `Sivar.Os.Data/Scripts/AddRetentionPolicies.sql`

```sql
-- =====================================================
-- Script: Add Retention Policies to Hypertables
-- Purpose: Automatically delete old data based on retention periods
-- Date: October 31, 2025
-- =====================================================

-- ⚠️ CRITICAL: Retention policies DELETE data permanently
-- Only run this in development or with explicit consent

-- Step 1: Add retention policy for Activities (keep 2 years)
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM timescaledb_information.hypertables 
        WHERE hypertable_name = 'Sivar_Activities'
    ) THEN
        -- Remove existing policy if any
        PERFORM remove_retention_policy('Sivar_Activities', if_exists => true);
        
        -- Add new policy: keep data for 2 years
        PERFORM add_retention_policy(
            'Sivar_Activities', 
            INTERVAL '2 years',
            if_not_exists => true
        );
        
        RAISE NOTICE 'Added retention policy to Sivar_Activities: 2 years';
    ELSE
        RAISE NOTICE 'Sivar_Activities is not a hypertable, skipping retention policy';
    END IF;
END $$;

-- Step 2: Add retention policy for Posts (keep 5 years)
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM timescaledb_information.hypertables 
        WHERE hypertable_name = 'Sivar_Posts'
    ) THEN
        PERFORM remove_retention_policy('Sivar_Posts', if_exists => true);
        
        PERFORM add_retention_policy(
            'Sivar_Posts', 
            INTERVAL '5 years',
            if_not_exists => true
        );
        
        RAISE NOTICE 'Added retention policy to Sivar_Posts: 5 years';
    ELSE
        RAISE NOTICE 'Sivar_Posts is not a hypertable, skipping retention policy';
    END IF;
END $$;

-- Step 3: Add retention policy for ChatMessages (keep 1 year)
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM timescaledb_information.hypertables 
        WHERE hypertable_name = 'Sivar_ChatMessages'
    ) THEN
        PERFORM remove_retention_policy('Sivar_ChatMessages', if_exists => true);
        
        PERFORM add_retention_policy(
            'Sivar_ChatMessages', 
            INTERVAL '1 year',
            if_not_exists => true
        );
        
        RAISE NOTICE 'Added retention policy to Sivar_ChatMessages: 1 year';
    ELSE
        RAISE NOTICE 'Sivar_ChatMessages is not a hypertable, skipping retention policy';
    END IF;
END $$;

-- Step 4: Add retention policy for Notifications (keep 6 months)
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM timescaledb_information.hypertables 
        WHERE hypertable_name = 'Sivar_Notifications'
    ) THEN
        PERFORM remove_retention_policy('Sivar_Notifications', if_exists => true);
        
        PERFORM add_retention_policy(
            'Sivar_Notifications', 
            INTERVAL '6 months',
            if_not_exists => true
        );
        
        RAISE NOTICE 'Added retention policy to Sivar_Notifications: 6 months';
    ELSE
        RAISE NOTICE 'Sivar_Notifications is not a hypertable, skipping retention policy';
    END IF;
END $$;

-- Step 5: Verify retention policies
SELECT 
    hypertable_name,
    drop_after
FROM timescaledb_information.jobs j
JOIN timescaledb_information.job_stats js ON j.job_id = js.job_id
WHERE j.proc_name = 'policy_retention'
ORDER BY hypertable_name;

-- =====================================================
-- IMPORTANT NOTES:
-- - Retention policies run automatically in the background
-- - Data older than the retention period is PERMANENTLY DELETED
-- - Policies can be modified or removed later
-- - In production, coordinate retention periods with backup strategy
-- =====================================================
```

---

### Step 4: Create Compression Policies Script ✅

**File**: `Sivar.Os.Data/Scripts/AddCompressionPolicies.sql`

```sql
-- =====================================================
-- Script: Add Compression Policies to Hypertables
-- Purpose: Automatically compress old data to save 90%+ storage
-- Date: October 31, 2025
-- =====================================================

-- Step 1: Enable compression on Sivar_Activities
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM timescaledb_information.hypertables 
        WHERE hypertable_name = 'Sivar_Activities'
    ) THEN
        -- Enable compression with segmentby and orderby
        ALTER TABLE "Sivar_Activities" SET (
            timescaledb.compress,
            timescaledb.compress_segmentby = 'ActorId,Verb',
            timescaledb.compress_orderby = 'PublishedAt DESC'
        );
        
        -- Add compression policy: compress chunks older than 30 days
        PERFORM add_compression_policy(
            'Sivar_Activities', 
            INTERVAL '30 days',
            if_not_exists => true
        );
        
        RAISE NOTICE 'Enabled compression on Sivar_Activities (compress after 30 days)';
    ELSE
        RAISE NOTICE 'Sivar_Activities is not a hypertable, skipping compression';
    END IF;
END $$;

-- Step 2: Enable compression on Sivar_Posts
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM timescaledb_information.hypertables 
        WHERE hypertable_name = 'Sivar_Posts'
    ) THEN
        ALTER TABLE "Sivar_Posts" SET (
            timescaledb.compress,
            timescaledb.compress_segmentby = 'ProfileId,PostType',
            timescaledb.compress_orderby = 'CreatedAt DESC'
        );
        
        -- Compress chunks older than 90 days
        PERFORM add_compression_policy(
            'Sivar_Posts', 
            INTERVAL '90 days',
            if_not_exists => true
        );
        
        RAISE NOTICE 'Enabled compression on Sivar_Posts (compress after 90 days)';
    ELSE
        RAISE NOTICE 'Sivar_Posts is not a hypertable, skipping compression';
    END IF;
END $$;

-- Step 3: Enable compression on Sivar_ChatMessages
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM timescaledb_information.hypertables 
        WHERE hypertable_name = 'Sivar_ChatMessages'
    ) THEN
        ALTER TABLE "Sivar_ChatMessages" SET (
            timescaledb.compress,
            timescaledb.compress_segmentby = 'ConversationId',
            timescaledb.compress_orderby = 'CreatedAt DESC'
        );
        
        -- Compress chunks older than 30 days
        PERFORM add_compression_policy(
            'Sivar_ChatMessages', 
            INTERVAL '30 days',
            if_not_exists => true
        );
        
        RAISE NOTICE 'Enabled compression on Sivar_ChatMessages (compress after 30 days)';
    ELSE
        RAISE NOTICE 'Sivar_ChatMessages is not a hypertable, skipping compression';
    END IF;
END $$;

-- Step 4: Enable compression on Sivar_Notifications
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM timescaledb_information.hypertables 
        WHERE hypertable_name = 'Sivar_Notifications'
    ) THEN
        ALTER TABLE "Sivar_Notifications" SET (
            timescaledb.compress,
            timescaledb.compress_segmentby = 'UserId,Type',
            timescaledb.compress_orderby = 'CreatedAt DESC'
        );
        
        -- Compress chunks older than 30 days
        PERFORM add_compression_policy(
            'Sivar_Notifications', 
            INTERVAL '30 days',
            if_not_exists => true
        );
        
        RAISE NOTICE 'Enabled compression on Sivar_Notifications (compress after 30 days)';
    ELSE
        RAISE NOTICE 'Sivar_Notifications is not a hypertable, skipping compression';
    END IF;
END $$;

-- Step 5: Verify compression policies
SELECT 
    hypertable_name,
    compression_enabled,
    compress_segmentby,
    compress_orderby
FROM timescaledb_information.compression_settings
WHERE hypertable_name IN ('Sivar_Activities', 'Sivar_Posts', 'Sivar_ChatMessages', 'Sivar_Notifications')
ORDER BY hypertable_name;

-- =====================================================
-- IMPORTANT NOTES:
-- - Compression runs automatically in the background
-- - Saves 90%+ storage space on compressed chunks
-- - Compressed chunks are still queryable (automatic decompression)
-- - compress_segmentby: groups data for better compression
-- - compress_orderby: orders data within segments
-- - Compression cannot be undone on individual chunks
-- =====================================================
```

---

### Step 5: Update Updater.cs to Execute Scripts

**File**: `Xaf.Sivar.Os/Xaf.Sivar.Os.Module/DatabaseUpdate/Updater.cs`

Add the new scripts to `SeedSqlScripts()` method:

```csharp
private void SeedSqlScripts()
{
    // ... existing ConvertContentEmbeddingToVector script ...
    
    // Seed EnableTimescaleDB script
    SeedTimescaleDBEnableScript();
    
    // Seed ConvertToHypertables script
    SeedConvertToHypertablesScript();
    
    // Seed AddRetentionPolicies script
    SeedRetentionPoliciesScript();
    
    // Seed AddCompressionPolicies script
    SeedCompressionPoliciesScript();
}

private void SeedTimescaleDBEnableScript()
{
    const string scriptName = "EnableTimescaleDB";
    
    var existingScript = ObjectSpace.GetObjectsQuery<SqlScript>()
        .FirstOrDefault(s => s.Name == scriptName);
    
    if (existingScript != null) return;
    
    var script = ObjectSpace.CreateObject<SqlScript>();
    script.Name = scriptName;
    script.Description = "Enables TimescaleDB extension in the database for time-series optimizations.";
    script.ExecutionOrder = 2.0m; // After ContentEmbedding (1.0)
    script.BatchName = SqlScriptBatches.AfterSchemaUpdate;
    script.IsActive = true;
    script.RunOnce = true;
    
    script.SqlText = @"
-- Enable TimescaleDB extension
CREATE EXTENSION IF NOT EXISTS timescaledb CASCADE;

-- Verify extension is installed
SELECT * FROM pg_extension WHERE extname = 'timescaledb';
";
}

private void SeedConvertToHypertablesScript()
{
    const string scriptName = "ConvertToHypertables";
    
    var existingScript = ObjectSpace.GetObjectsQuery<SqlScript>()
        .FirstOrDefault(s => s.Name == scriptName);
    
    if (existingScript != null) return;
    
    var script = ObjectSpace.CreateObject<SqlScript>();
    script.Name = scriptName;
    script.Description = "Converts time-series tables (Activities, Posts, ChatMessages, Notifications) to TimescaleDB hypertables.";
    script.ExecutionOrder = 3.0m;
    script.BatchName = SqlScriptBatches.AfterSchemaUpdate;
    script.IsActive = true;
    script.RunOnce = true;
    
    script.SqlText = @"
-- [Include full ConvertToHypertables.sql content here]
";
}

private void SeedRetentionPoliciesScript()
{
    const string scriptName = "AddRetentionPolicies";
    
    var existingScript = ObjectSpace.GetObjectsQuery<SqlScript>()
        .FirstOrDefault(s => s.Name == scriptName);
    
    if (existingScript != null) return;
    
    var script = ObjectSpace.CreateObject<SqlScript>();
    script.Name = scriptName;
    script.Description = "Adds retention policies to hypertables (Activities: 2yr, Posts: 5yr, ChatMessages: 1yr, Notifications: 6mo).";
    script.ExecutionOrder = 4.0m;
    script.BatchName = SqlScriptBatches.AfterSchemaUpdate;
    script.IsActive = true;
    script.RunOnce = false; // Can be modified and re-run
    
    script.SqlText = @"
-- [Include full AddRetentionPolicies.sql content here]
";
}

private void SeedCompressionPoliciesScript()
{
    const string scriptName = "AddCompressionPolicies";
    
    var existingScript = ObjectSpace.GetObjectsQuery<SqlScript>()
        .FirstOrDefault(s => s.Name == scriptName);
    
    if (existingScript != null) return;
    
    var script = ObjectSpace.CreateObject<SqlScript>();
    script.Name = scriptName;
    script.Description = "Adds compression policies to hypertables for 90%+ storage savings on old data.";
    script.ExecutionOrder = 5.0m;
    script.BatchName = SqlScriptBatches.AfterSchemaUpdate;
    script.IsActive = true;
    script.RunOnce = false; // Can be modified and re-run
    
    script.SqlText = @"
-- [Include full AddCompressionPolicies.sql content here]
";
}
```

---

### Step 6: Update Index Configurations (Optional Optimization)

**File**: `Sivar.Os.Data/Configurations/ActivityConfiguration.cs`

No changes needed! Existing indexes are automatically preserved when converting to hypertables.

However, we can add **TimescaleDB-optimized indexes** later for better performance.

---

## EF Core Compatibility Check

### Do We Need `.Ignore()` Pattern?

**Answer: NO** ✅

Unlike pgvector (Phase 5), TimescaleDB hypertables are **fully compatible with EF Core**:

- ✅ EF Core sees hypertables as regular tables
- ✅ All CRUD operations work normally
- ✅ Migrations work (but shouldn't create hypertables)
- ✅ No special type conversions needed

**Why it works:**
- Hypertables are a **database-level feature**, not a type issue
- EF Core doesn't need to know about hypertables
- All queries work exactly the same

---

## Testing Strategy

### 1. Before Conversion
```csharp
// Test query performance BEFORE hypertables
var activities = await _activityRepository.GetByDateRangeAsync(
    DateTime.UtcNow.AddDays(-7),
    DateTime.UtcNow
);
// Log execution time
```

### 2. After Conversion
```csharp
// Test same query AFTER hypertables
var activities = await _activityRepository.GetByDateRangeAsync(
    DateTime.UtcNow.AddDays(-7),
    DateTime.UtcNow
);
// Compare execution time - should be 10-100x faster
```

### 3. Verify Hypertables
```sql
-- Check if tables are hypertables
SELECT * FROM timescaledb_information.hypertables;

-- Check chunks
SELECT * FROM timescaledb_information.chunks 
WHERE hypertable_name = 'Sivar_Activities';

-- Check compression
SELECT * FROM timescaledb_information.compression_settings;
```

---

## Success Criteria

- [ ] ✅ **TimescaleDB extension enabled** in database
- [ ] ✅ **4 tables converted to hypertables** (Activities, Posts, ChatMessages, Notifications)
- [ ] ✅ **Retention policies configured** (automatic old data deletion)
- [ ] ✅ **Compression policies configured** (automatic compression after X days)
- [ ] ✅ **All existing queries still work** (no code changes needed)
- [ ] ✅ **Time-range queries 10-100x faster** (verify with benchmarks)
- [ ] ✅ **Chunks created correctly** (verify in timescaledb_information views)

---

## Rollback Plan

If something goes wrong:

```sql
-- Revert hypertable to regular table (NOT RECOMMENDED, but possible)
-- ⚠️ This drops all chunks and data!

-- Don't actually run this unless absolutely necessary
-- SELECT revert_hypertable('Sivar_Activities');
```

Better approach:
1. Keep database backup before running scripts
2. Test on development database first
3. If issues, restore from backup

---

## Next Steps After Phase 6

Once hypertables are working, we can add:

1. **Phase 7: Continuous Aggregates** (pre-computed dashboards)
2. **Phase 8: Advanced Optimizations** (partial indexes, connection pooling)

---

## File Checklist

- [ ] Create `Sivar.Os.Data/Scripts/EnableTimescaleDB.sql`
- [ ] Create `Sivar.Os.Data/Scripts/ConvertToHypertables.sql`
- [ ] Create `Sivar.Os.Data/Scripts/AddRetentionPolicies.sql`
- [ ] Create `Sivar.Os.Data/Scripts/AddCompressionPolicies.sql`
- [ ] Update `Updater.cs` with seed methods
- [ ] Test scripts manually in database
- [ ] Run application and verify execution
- [ ] Benchmark query performance

---

## Estimated Timeline

| Task | Time | Cumulative |
|------|------|------------|
| Create SQL scripts | 2-3 hours | 3 hours |
| Update Updater.cs | 1-2 hours | 5 hours |
| Test scripts manually | 2 hours | 7 hours |
| Run and verify | 1 hour | 8 hours |
| Performance benchmarking | 2-3 hours | 11 hours |
| Documentation | 1 hour | 12 hours |

**Total**: ~12 hours

---

**Ready to begin implementation!** 🚀
