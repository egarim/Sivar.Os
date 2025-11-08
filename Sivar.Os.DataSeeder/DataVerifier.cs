using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sivar.Os.Data.Context;

namespace Sivar.Os.DataSeeder;

/// <summary>
/// Simple verification script to check seeded data
/// </summary>
public class DataVerifier
{
    public static async Task VerifyData(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SivarDbContext>();

        Console.WriteLine("🔍 Verifying Seeded Data...\n");

        // Check Users
        var users = await context.Users.ToListAsync();
        Console.WriteLine($"👥 Users: {users.Count}");
        foreach (var user in users)
        {
            Console.WriteLine($"   - {user.FirstName} {user.LastName} ({user.Email}) [KeycloakId: {user.KeycloakId}]");
        }

        Console.WriteLine();

        // Check Profiles
        var profiles = await context.Profiles
            .Include(p => p.User)
            .Include(p => p.ProfileType)
            .ToListAsync();
        Console.WriteLine($"📝 Profiles: {profiles.Count}");
        foreach (var profile in profiles)
        {
            Console.WriteLine($"   - {profile.DisplayName} (@{profile.Handle}) - {profile.ProfileType.DisplayName}");
        }

        Console.WriteLine();

        // Check Posts
        var posts = await context.Posts
            .Include(p => p.Profile)
            .ToListAsync();
        Console.WriteLine($"📰 Posts: {posts.Count}");
        foreach (var profile in profiles)
        {
            var profilePosts = posts.Where(p => p.ProfileId == profile.Id).ToList();
            Console.WriteLine($"   - {profile.DisplayName}: {profilePosts.Count} posts");
        }

        Console.WriteLine();

        // Check Follow Relationships
        var follows = await context.ProfileFollowers
            .Include(f => f.FollowerProfile)
            .Include(f => f.FollowedProfile)
            .ToListAsync();
        Console.WriteLine($"🤝 Follow Relationships: {follows.Count}");
        foreach (var follow in follows)
        {
            Console.WriteLine($"   - {follow.FollowerProfile.DisplayName} follows {follow.FollowedProfile.DisplayName}");
        }

        Console.WriteLine();

        // Check Reactions
        var reactions = await context.Reactions
            .Include(r => r.Profile)
            .Include(r => r.Post)
            .ThenInclude(p => p.Profile)
            .ToListAsync();
        Console.WriteLine($"❤️ Reactions: {reactions.Count}");
        var reactionsByProfile = reactions.GroupBy(r => r.Profile.DisplayName);
        foreach (var group in reactionsByProfile)
        {
            Console.WriteLine($"   - {group.Key}: {group.Count()} reactions");
        }

        Console.WriteLine("\n✅ Data verification complete!");
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                var connectionString = context.Configuration.GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

                services.AddSingleton<Sivar.Os.Data.Interceptors.VectorTypeInterceptor>();

                services.AddDbContext<SivarDbContext>((serviceProvider, options) =>
                {
                    var vectorInterceptor = serviceProvider.GetRequiredService<Sivar.Os.Data.Interceptors.VectorTypeInterceptor>();
                    options.UseNpgsql(connectionString, o => o.UseVector())
                        .AddInterceptors(vectorInterceptor);
                });
            });
}