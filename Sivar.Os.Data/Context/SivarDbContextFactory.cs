using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Sivar.Os.Data.Context;

/// <summary>
/// Design-time factory for SivarDbContext (required for EF Core migrations)
/// </summary>
public class SivarDbContextFactory : IDesignTimeDbContextFactory<SivarDbContext>
{
    public SivarDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SivarDbContext>();
        
        // Use a dummy connection string for design-time operations
        // The actual connection string comes from appsettings.json at runtime
        optionsBuilder.UseNpgsql("Host=localhost;Database=sivar_design;Username=postgres;Password=postgres");
        
        return new SivarDbContext(optionsBuilder.Options);
    }
}
