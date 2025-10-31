# SQL Scripts Execution in XAF Database Updater

## Overview

The XAF Database Updater (`Updater.cs`) now supports automatic execution of SQL scripts during database updates. This is particularly useful for database modifications that cannot be handled by EF Core migrations, such as pgvector column setup.

## How It Works

### 1. **Script Location**
- SQL scripts must be placed in: `Sivar.Os.Data/Scripts/`
- Scripts are plain `.sql` files (e.g., `ConvertContentEmbeddingToVector.sql`)

### 2. **Execution Points**

Scripts can be executed at two different points in the database update lifecycle:

#### **Before Schema Update** (`UpdateDatabaseBeforeUpdateSchema`)
```csharp
public override void UpdateDatabaseBeforeUpdateSchema()
{
    base.UpdateDatabaseBeforeUpdateSchema();
    
    ExecuteSqlScripts(new string[]
    {
        // Scripts that need to run BEFORE EF Core migrations
        // Example: "AddVectorExtension.sql"
    });
}
```

#### **After Schema Update** (`UpdateDatabaseAfterUpdateSchema`)
```csharp
public override void UpdateDatabaseAfterUpdateSchema()
{
    base.UpdateDatabaseAfterUpdateSchema();
    
    // Execute SQL scripts BEFORE seeding data
    ExecuteSqlScripts(new string[]
    {
        "ConvertContentEmbeddingToVector.sql"  // pgvector column setup
    });
    
    // ... rest of the seeding code ...
}
```

### 3. **Execution Order**

The complete database update flow is:

1. `UpdateDatabaseBeforeUpdateSchema()` → SQL scripts (if any)
2. EF Core migrations run
3. `UpdateDatabaseAfterUpdateSchema()` → SQL scripts (if any)
4. Data seeding (CreateDefaultRole, CreateAdminRole, SeedProfileTypes, etc.)

## Adding New SQL Scripts

### Step 1: Create the SQL Script

Create a new `.sql` file in `Sivar.Os.Data/Scripts/`:

```sql
-- Example: AddVectorExtension.sql
CREATE EXTENSION IF NOT EXISTS vector;
```

### Step 2: Add to Updater

Edit `Updater.cs` and add the script name to the appropriate method:

```csharp
ExecuteSqlScripts(new string[]
{
    "ConvertContentEmbeddingToVector.sql",
    "YourNewScript.sql"  // Add your new script here
});
```

### Step 3: Run XAF Application

The next time the XAF application runs and detects a database update is needed, your script will execute automatically.

## Current Scripts

| Script | Purpose | Execution Point |
|--------|---------|----------------|
| `ConvertContentEmbeddingToVector.sql` | Converts ContentEmbedding column from text to vector(384) and creates HNSW index | After Schema Update |

## Features

### ✅ **Idempotent Scripts**
- Scripts should be written to be idempotent (safe to run multiple times)
- Use `IF NOT EXISTS` or `DROP ... IF EXISTS` patterns
- Example from `ConvertContentEmbeddingToVector.sql`:
  ```sql
  CREATE EXTENSION IF NOT EXISTS vector;
  DROP INDEX IF EXISTS "IX_Posts_ContentEmbedding_Hnsw";
  ```

### ✅ **Error Handling**
- If a script fails, the error is logged to Debug output
- Other scripts continue to execute
- Seeding continues even if scripts fail
- This prevents database update failures from blocking app startup

### ✅ **Logging**
- All script execution is logged to Debug output
- Logs include:
  - Script execution start
  - Success/failure status
  - Error messages and stack traces (if failed)
  - Script file path (for debugging)

### ✅ **Dynamic Path Resolution**
- Scripts are located relative to the XAF application's base directory
- Works in both Development and Production environments
- Path: `BaseDirectory/../../../../Sivar.Os.Data/Scripts/ScriptName.sql`

## Troubleshooting

### Script Not Found
If you see: `[SQL Scripts] Script not found: <path>`

**Solution:**
1. Verify the script exists in `Sivar.Os.Data/Scripts/`
2. Check the file name matches exactly (case-sensitive on Linux)
3. Ensure the file is copied to output directory

### Script Execution Error
If you see: `[SQL Scripts] Error executing <script>: <error>`

**Solution:**
1. Check the error message in Debug output
2. Verify SQL syntax is correct for PostgreSQL
3. Test the script manually in pgAdmin or psql
4. Ensure required extensions/tables exist

### ObjectSpace Not EFCoreObjectSpace
If you see: `[SQL Scripts] ObjectSpace is not EFCoreObjectSpace`

**Solution:**
- This indicates a configuration issue with XAF
- Ensure the XAF application is using EF Core (not XPO)
- Contact DevExpress support if issue persists

## Best Practices

### ✅ DO:
- Write idempotent scripts (safe to run multiple times)
- Use transactions for multi-statement operations
- Add comments explaining what the script does
- Test scripts manually before adding to Updater
- Use conditional checks (IF EXISTS, IF NOT EXISTS)
- Log important information with RAISE NOTICE

### ❌ DON'T:
- Don't assume tables/columns exist without checking
- Don't hard-code IDs or GUIDs (unless seeding)
- Don't use DROP TABLE without IF EXISTS
- Don't execute DDL that conflicts with EF Core migrations
- Don't rely on specific execution order between scripts (unless documented)

## Example: Complete Script Template

```sql
-- =====================================================
-- Script: MyNewScript.sql
-- Purpose: Brief description of what this script does
-- Date: [Date Created]
-- =====================================================

-- Step 1: Ensure required extensions
CREATE EXTENSION IF NOT EXISTS my_extension;

-- Step 2: Check if work is needed
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'MyTable' 
        AND column_name = 'MyColumn'
    ) THEN
        -- Step 3: Perform the work
        ALTER TABLE "MyTable" ADD COLUMN "MyColumn" text;
        RAISE NOTICE 'Column MyColumn created';
    ELSE
        RAISE NOTICE 'Column MyColumn already exists';
    END IF;
END $$;

-- Step 4: Verify the change
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'MyTable' AND column_name = 'MyColumn';

-- =====================================================
-- NOTES:
-- - This script is idempotent
-- - Safe to run multiple times
-- - Add any important notes here
-- =====================================================
```

## Integration with Blazor Application

The Blazor application (`Sivar.Os`) has `PostConfiguration.cs` set to **ignore** the `ContentEmbedding` column:

```csharp
builder.Ignore(p => p.ContentEmbedding);
```

This is **required** because:
1. EF Core 9.0 cannot handle pgvector types properly
2. The XAF Updater creates/manages the column via SQL scripts
3. The Blazor app updates the column via raw SQL (`UpdateContentEmbeddingAsync`)

This pattern ensures both applications can work with the same database without conflicts.

---

## Summary

The SQL script execution feature in the XAF Database Updater provides a robust way to handle database modifications that are outside the scope of EF Core migrations. It's particularly useful for:

- PostgreSQL extensions (pgvector, postgis, etc.)
- Custom indexes (HNSW, GIN, etc.)
- Database functions and procedures
- Complex DDL that EF Core doesn't support
- One-time data migrations

All scripts are version-controlled, automatically executed, and safely handled with proper error logging.
