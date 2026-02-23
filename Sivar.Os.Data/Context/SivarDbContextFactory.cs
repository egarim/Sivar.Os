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
        
        // Use production connection string for design-time migrations
        optionsBuilder.UseNpgsql("Host=86.48.30.121;Port=5432;Database=sivaros;Username=postgres;Password=Xa1Hf4M3EnAKG8g");
        
        return new SivarDbContext(optionsBuilder.Options);
    }
}
