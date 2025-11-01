using Microsoft.EntityFrameworkCore;
using Sivar.Os.Data.Context;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Repositories;

namespace Sivar.Os.Data.Repositories;

/// <summary>
/// Base repository implementation with common CRUD operations
/// </summary>
/// <typeparam name="T">Entity type that inherits from BaseEntity</typeparam>
public class BaseRepository<T> : IBaseRepository<T> where T : BaseEntity
{
    protected readonly SivarDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public BaseRepository(SivarDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = _context.Set<T>();
    }

    /// <summary>
    /// Gets all entities of type T (excludes soft deleted)
    /// </summary>
    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    /// <summary>
    /// Gets an entity by its ID
    /// </summary>
    public virtual async Task<T?> GetByIdAsync(Guid id)
    {
        return await _dbSet.FindAsync(id);
    }

    /// <summary>
    /// Gets an entity by its ID, ignoring global query filters (like soft delete filters)
    /// </summary>
    public virtual async Task<T?> GetByIdIgnoringFiltersAsync(Guid id)
    {
        return await _dbSet.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id);
    }

    /// <summary>
    /// Adds a new entity
    /// </summary>
    public virtual async Task<T> AddAsync(T entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        await _dbSet.AddAsync(entity);
        return entity;
    }

    /// <summary>
    /// Updates an existing entity
    /// </summary>
    public virtual async Task<T> UpdateAsync(T entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        _dbSet.Update(entity);
        return await Task.FromResult(entity);
    }

    /// <summary>
    /// Soft deletes an entity
    /// </summary>
    public virtual async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await GetByIdAsync(id);
        if (entity == null)
            return false;

        // Soft delete is handled by the DbContext override
        _dbSet.Remove(entity);
        return true;
    }

    /// <summary>
    /// Checks if an entity exists by ID
    /// </summary>
    public virtual async Task<bool> ExistsAsync(Guid id)
    {
        return await _dbSet.AnyAsync(e => e.Id == id);
    }

    /// <summary>
    /// Saves all pending changes to the database
    /// </summary>
    public virtual async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Gets the underlying DbContext for raw SQL queries
    /// </summary>
    public virtual DbContext GetDbContext()
    {
        return _context;
    }

    /// <summary>
    /// Gets multiple entities by their IDs
    /// </summary>
    public virtual async Task<List<T>> GetByIdsAsync(List<Guid> ids, CancellationToken cancellationToken = default)
    {
        if (ids == null || ids.Count == 0)
            return new List<T>();

        return await _dbSet
            .Where(e => ids.Contains(e.Id))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets queryable for advanced queries
    /// </summary>
    protected virtual IQueryable<T> GetQueryable()
    {
        return _dbSet.AsQueryable();
    }
}