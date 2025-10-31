# SQL Script Entity - Database-Driven Script Execution System

## 📋 Overview

Convert the file-based SQL script execution system to a **database-driven** system where SQL scripts are stored as entities with flexible ordering and batch grouping.

---

## 🎯 Goals

1. **Store scripts in database** - Scripts become data, not just files
2. **Flexible ordering** - Use decimal ordering (1.0, 1.5, 2.0) to allow insertion between existing scripts
3. **Batch grouping** - Group scripts by execution point (BeforeSchemaUpdate, AfterSchemaUpdate)
4. **Execution tracking** - Track when scripts run and how many times
5. **Enable/Disable** - Can deactivate scripts without deleting them
6. **XAF UI management** - Manage scripts through XAF interface

---

## 🏗️ Entity Design

### **SqlScript Entity**

```csharp
public class SqlScript : BaseEntity
{
    // Identification
    public string Name { get; set; }              // Unique identifier (e.g., "ConvertContentEmbeddingToVector")
    public string Description { get; set; }        // What the script does
    
    // SQL Content
    public string SqlText { get; set; }           // The actual SQL to execute (can be multiline)
    
    // Execution Control
    public decimal ExecutionOrder { get; set; }   // Order within batch (1.0, 1.5, 2.0, etc.)
    public string BatchName { get; set; }         // Batch group (e.g., "AfterSchemaUpdate", "BeforeSchemaUpdate")
    public bool IsActive { get; set; }            // Can disable without deleting
    public bool RunOnce { get; set; }             // If true, only run if ExecutionCount = 0
    
    // Execution Tracking
    public DateTime? LastExecutedAt { get; set; } // Last execution timestamp
    public int ExecutionCount { get; set; }       // How many times executed
    public string? LastExecutionError { get; set; } // Last error message (if failed)
    
    // Audit (inherited from BaseEntity)
    // - Id (Guid)
    // - CreatedAt
    // - UpdatedAt
    // - IsDeleted
    // - DeletedAt
}
```

### **Batch Names (Constants)**

```csharp
public static class SqlScriptBatches
{
    public const string BeforeSchemaUpdate = "BeforeSchemaUpdate";
    public const string AfterSchemaUpdate = "AfterSchemaUpdate";
    public const string CustomMaintenance = "CustomMaintenance";
}
```

---

## 📊 Database Schema

### **Table: Sivar_SqlScripts**

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | uuid | PK | Primary key |
| Name | varchar(200) | NOT NULL, UNIQUE | Script identifier |
| Description | text | NOT NULL | What the script does |
| SqlText | text | NOT NULL | The SQL to execute |
| ExecutionOrder | decimal(10,2) | NOT NULL | Order within batch |
| BatchName | varchar(100) | NOT NULL | Batch grouping |
| IsActive | boolean | NOT NULL, DEFAULT true | Enable/disable |
| RunOnce | boolean | NOT NULL, DEFAULT true | Execute only once |
| LastExecutedAt | timestamp | NULL | Last execution time |
| ExecutionCount | integer | NOT NULL, DEFAULT 0 | Execution counter |
| LastExecutionError | text | NULL | Last error message |
| CreatedAt | timestamp | NOT NULL | Creation timestamp |
| UpdatedAt | timestamp | NOT NULL | Update timestamp |
| IsDeleted | boolean | NOT NULL, DEFAULT false | Soft delete flag |
| DeletedAt | timestamp | NULL | Deletion timestamp |

### **Indexes**

```sql
-- Primary index
CREATE UNIQUE INDEX IX_SqlScripts_Name ON "Sivar_SqlScripts" ("Name") WHERE "IsDeleted" = false;

-- Execution query optimization
CREATE INDEX IX_SqlScripts_Batch_Order ON "Sivar_SqlScripts" ("BatchName", "ExecutionOrder") 
WHERE "IsActive" = true AND "IsDeleted" = false;
```

---

## 🔄 Execution Flow

### **Current Flow (File-Based)**
```
Updater.UpdateDatabaseAfterUpdateSchema()
    ↓
ExecuteSqlScripts(["ConvertContentEmbeddingToVector.sql"])
    ↓
Read file from disk
    ↓
Execute SQL
```

### **New Flow (Database-Driven)**
```
Updater.UpdateDatabaseAfterUpdateSchema()
    ↓
ExecuteSqlScriptBatch("AfterSchemaUpdate")
    ↓
Query: SELECT * FROM SqlScripts 
       WHERE BatchName = "AfterSchemaUpdate" 
       AND IsActive = true 
       AND IsDeleted = false
       ORDER BY ExecutionOrder ASC
    ↓
For each script:
    - Check if RunOnce = true AND ExecutionCount > 0 → Skip
    - Execute SQL
    - Update: ExecutionCount++, LastExecutedAt = NOW()
    - On error: Update LastExecutionError
```

---

## 🛠️ Implementation Plan

### **Phase 1: Entity & Infrastructure**

✅ **Step 1.1**: Create `SqlScript.cs` entity
- Location: `Sivar.Os.Shared/Entities/SqlScript.cs`
- Inherit from `BaseEntity`
- Add all properties as designed above

✅ **Step 1.2**: Create `SqlScriptConfiguration.cs`
- Location: `Sivar.Os.Data/Configurations/SqlScriptConfiguration.cs`
- Configure table name, indexes, constraints
- Set max lengths for string properties

✅ **Step 1.3**: Add `DbSet<SqlScript>` to `SivarDbContext`
- Location: `Sivar.Os.Data/Context/SivarDbContext.cs`
- Add: `public DbSet<SqlScript> SqlScripts => Set<SqlScript>();`

✅ **Step 1.4**: Create `ISqlScriptRepository` interface
- Location: `Sivar.Os.Shared/Repositories/ISqlScriptRepository.cs`
- Methods:
  - `Task<List<SqlScript>> GetByBatchAsync(string batchName)`
  - `Task<SqlScript?> GetByNameAsync(string name)`
  - `Task UpdateExecutionStatusAsync(Guid scriptId, bool success, string? errorMessage)`

✅ **Step 1.5**: Implement `SqlScriptRepository`
- Location: `Sivar.Os.Data/Repositories/SqlScriptRepository.cs`
- Implement interface methods
- Include proper error handling

### **Phase 2: Updater Integration**

✅ **Step 2.1**: Update `Updater.cs` - Add new method
- Add `ExecuteSqlScriptBatch(string batchName)`
- Replace file-based `ExecuteSqlScripts()` logic
- Query database for scripts in batch
- Execute in order
- Track execution status

✅ **Step 2.2**: Update `UpdateDatabaseAfterUpdateSchema()`
- Change from: `ExecuteSqlScripts(new[] { "file.sql" })`
- Change to: `ExecuteSqlScriptBatch("AfterSchemaUpdate")`

✅ **Step 2.3**: Add `SeedSqlScripts()` method
- Seed the `ConvertContentEmbeddingToVector` script
- Set properties:
  - Name: "ConvertContentEmbeddingToVector"
  - ExecutionOrder: 1.0
  - BatchName: "AfterSchemaUpdate"
  - RunOnce: true

### **Phase 3: Migration & Testing**

✅ **Step 3.1**: Read existing SQL file content
- Read `ConvertContentEmbeddingToVector.sql` content
- Store in seeding method as string constant or embedded resource

✅ **Step 3.2**: Test with XAF application
- Run XAF app
- Verify script execution
- Check execution tracking (LastExecutedAt, ExecutionCount)

✅ **Step 3.3**: Verify in database
- Check `Sivar_SqlScripts` table has seeded data
- Verify vector column exists
- Verify HNSW index created

---

## 📝 Sample Seeding Code

```csharp
void SeedSqlScripts()
{
    // ConvertContentEmbeddingToVector Script
    var vectorScriptName = "ConvertContentEmbeddingToVector";
    var vectorScript = ObjectSpace.FirstOrDefault<SqlScript>(s => s.Name == vectorScriptName);
    
    if (vectorScript == null)
    {
        vectorScript = ObjectSpace.CreateObject<SqlScript>();
        vectorScript.Name = vectorScriptName;
        vectorScript.Description = "Converts ContentEmbedding column from text to vector(384) and creates HNSW index for pgvector similarity search";
        vectorScript.ExecutionOrder = 1.0m;
        vectorScript.BatchName = "AfterSchemaUpdate";
        vectorScript.IsActive = true;
        vectorScript.RunOnce = true;
        vectorScript.SqlText = @"
-- =====================================================
-- Script: Convert ContentEmbedding from TEXT to VECTOR(384)
-- Purpose: Convert existing ContentEmbedding column to pgvector type
--          This is REQUIRED because EF Core 9.0 cannot handle pgvector types
-- Date: October 31, 2025
-- =====================================================

-- Step 1: Ensure pgvector extension is installed
CREATE EXTENSION IF NOT EXISTS vector;

-- Step 2: Check if column exists, if not create it
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Sivar_Posts' AND column_name = 'ContentEmbedding'
    ) THEN
        ALTER TABLE ""Sivar_Posts"" ADD COLUMN ""ContentEmbedding"" vector(384);
        RAISE NOTICE 'Column ContentEmbedding created as vector(384)';
    ELSE
        IF EXISTS (
            SELECT 1 FROM information_schema.columns 
            WHERE table_name = 'Sivar_Posts' AND column_name = 'ContentEmbedding' AND udt_name = 'vector'
        ) THEN
            RAISE NOTICE 'Column ContentEmbedding is already vector type';
        ELSE
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

-- Step 3: Create HNSW index
DROP INDEX IF EXISTS ""IX_Posts_ContentEmbedding_Hnsw"";
CREATE INDEX ""IX_Posts_ContentEmbedding_Hnsw"" 
ON ""Sivar_Posts"" USING hnsw (""ContentEmbedding"" vector_cosine_ops);
";
        vectorScript.CreatedAt = DateTime.UtcNow;
        vectorScript.UpdatedAt = DateTime.UtcNow;
    }
}
```

---

## 💡 Benefits

### **Flexibility**
- ✅ Insert scripts between existing ones (1.0 → 1.5 → 2.0)
- ✅ Reorder scripts without renaming files
- ✅ Group scripts into logical batches

### **Visibility**
- ✅ See all scripts in XAF UI
- ✅ Track execution history
- ✅ Monitor errors

### **Control**
- ✅ Enable/disable scripts without deleting
- ✅ Run scripts once or multiple times
- ✅ Add/edit scripts through UI (admin only)

### **Maintainability**
- ✅ Scripts versioned through database backups
- ✅ No file system dependencies
- ✅ Easy to audit and review

---

## 🎨 Future Enhancements (Optional)

1. **Script Dependencies** - Add `DependsOnScriptId` foreign key
2. **Rollback Scripts** - Add `RollbackSqlText` property
3. **Environment Filtering** - Add `Environment` property (Dev, Prod, etc.)
4. **Execution History Table** - Separate table for execution logs
5. **Pre/Post Validation** - Add validation SQL to check prerequisites
6. **Script Categories** - Additional grouping beyond batches
7. **Scheduled Execution** - Add `ScheduledFor` property for timed scripts

---

## 📦 Deliverables

1. ✅ `SqlScript.cs` entity
2. ✅ `SqlScriptConfiguration.cs` EF configuration
3. ✅ `ISqlScriptRepository.cs` + `SqlScriptRepository.cs`
4. ✅ Updated `Updater.cs` with database-driven execution
5. ✅ Seeded `ConvertContentEmbeddingToVector` script
6. ✅ XAF UI for managing scripts (automatic from entity)
7. ✅ Updated documentation

---

## 🚀 Execution Order Example

| Name | Batch | Order | Description |
|------|-------|-------|-------------|
| AddVectorExtension | BeforeSchemaUpdate | 1.0 | Install pgvector extension |
| ConvertContentEmbedding | AfterSchemaUpdate | 1.0 | Convert embedding column to vector |
| OptimizeIndexes | AfterSchemaUpdate | 2.0 | Create additional indexes |
| CleanupOldData | AfterSchemaUpdate | 3.0 | Remove deprecated data |

**If we need to add a script between ConvertContentEmbedding and OptimizeIndexes:**
- Set ExecutionOrder = 1.5
- Script runs in correct position without renaming others!

---

Ready to implement? Let's start with Phase 1! 🎯
