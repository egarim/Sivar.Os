using Microsoft.EntityFrameworkCore;
using Npgsql;
using System;
using System.Threading.Tasks;

namespace DatabaseChecker
{
    class Program
    {
        private const string ConnectionString = "Host=localhost;Username=postgres;Password=1234567890;Database=XafSivarOs";

        static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("Checking XafSivarOs database...");

                using var connection = new NpgsqlConnection(ConnectionString);
                await connection.OpenAsync();
                Console.WriteLine("✓ Successfully connected to database");

                // Check what tables exist
                var command = new NpgsqlCommand(@"
                    SELECT table_name 
                    FROM information_schema.tables 
                    WHERE table_schema = 'public' 
                    ORDER BY table_name", connection);

                using var reader = await command.ExecuteReaderAsync();
                Console.WriteLine("\nTables in database:");
                while (await reader.ReadAsync())
                {
                    Console.WriteLine($"  - {reader.GetString(0)}");
                }
                reader.Close();

                // Check ProfileType count
                var profileTypeCmd = new NpgsqlCommand("SELECT COUNT(*) FROM \"ProfileType\"", connection);
                var profileTypeCount = await profileTypeCmd.ExecuteScalarAsync();
                Console.WriteLine($"\nProfileType records: {profileTypeCount}");

                // Check if User table exists and count
                try
                {
                    var userCmd = new NpgsqlCommand("SELECT COUNT(*) FROM \"User\"", connection);
                    var userCount = await userCmd.ExecuteScalarAsync();
                    Console.WriteLine($"User records: {userCount}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"User table issue: {ex.Message}");
                }

                // Check if Profile table exists and count
                try
                {
                    var profileCmd = new NpgsqlCommand("SELECT COUNT(*) FROM \"Profile\"", connection);
                    var profileCount = await profileCmd.ExecuteScalarAsync();
                    Console.WriteLine($"Profile records: {profileCount}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Profile table issue: {ex.Message}");
                }

                // Check if Post table exists and count
                try
                {
                    var postCmd = new NpgsqlCommand("SELECT COUNT(*) FROM \"Post\"", connection);
                    var postCount = await postCmd.ExecuteScalarAsync();
                    Console.WriteLine($"Post records: {postCount}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Post table issue: {ex.Message}");
                }

                // Check if Activity table exists and count
                try
                {
                    var activityCmd = new NpgsqlCommand("SELECT COUNT(*) FROM \"Activity\"", connection);
                    var activityCount = await activityCmd.ExecuteScalarAsync();
                    Console.WriteLine($"Activity records: {activityCount}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Activity table issue: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}