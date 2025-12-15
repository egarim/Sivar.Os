using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Sivar.Os.Data.Context;
using Sivar.Os.Data.Repositories;
using Sivar.Os.DataSeeder.Services;
using Sivar.Os.Shared.Repositories;

namespace Sivar.Os.DataSeeder;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            Log.Information("🌱 Starting Sivar.Os Data Seeder...");

            var host = CreateHostBuilder(args).Build();

            using var scope = host.Services.CreateScope();
            
            // Seed category definitions first (for multilingual search)
            var categorySeeder = scope.ServiceProvider.GetRequiredService<CategoryDefinitionSeeder>();
            await categorySeeder.SeedCategoriesAsync();
            
            var seeder = scope.ServiceProvider.GetRequiredService<DataSeedingService>();

            await seeder.SeedDataAsync();

            Log.Information("✅ Data seeding completed successfully!");
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "❌ Data seeding failed with exception");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureServices((context, services) =>
            {
                // Database Context
                var connectionString = context.Configuration.GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

                services.AddSingleton<Sivar.Os.Data.Interceptors.VectorTypeInterceptor>();

                services.AddDbContext<SivarDbContext>((serviceProvider, options) =>
                {
                    var vectorInterceptor = serviceProvider.GetRequiredService<Sivar.Os.Data.Interceptors.VectorTypeInterceptor>();
                    options.UseNpgsql(connectionString, o => o.UseVector())
                        .AddInterceptors(vectorInterceptor);
                });

                // Repositories
                services.AddScoped<IUserRepository, UserRepository>();
                services.AddScoped<IProfileRepository, ProfileRepository>();
                services.AddScoped<IProfileTypeRepository, ProfileTypeRepository>();
                services.AddScoped<IProfileFollowerRepository, ProfileFollowerRepository>();
                services.AddScoped<IPostRepository, PostRepository>();
                services.AddScoped<ICommentRepository, CommentRepository>();
                services.AddScoped<IReactionRepository, ReactionRepository>();

                // Seeding Service
                services.AddScoped<CategoryDefinitionSeeder>();
                services.AddScoped<DataSeedingService>();
            });
}