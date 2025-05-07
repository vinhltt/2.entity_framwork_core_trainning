using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Migrations.Data;

namespace Migrations
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Entity Framework Core Migrations Demo");
            Console.WriteLine("====================================");

            // Configure DbContext with logging
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder
                .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=MigrationsDemoDb;Trusted_Connection=True;")
                .LogTo(Console.WriteLine, LogLevel.Information)
                .EnableSensitiveDataLogging();

            using (var context = new ApplicationDbContext(optionsBuilder.Options))
            {
                // Demo 1: Apply Migrations at Runtime
                Console.WriteLine("\n=== Demo 1: Apply Migrations at Runtime ===");
                try
                {
                    await context.Database.MigrateAsync();
                    Console.WriteLine("Migrations applied successfully!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error applying migrations: {ex.Message}");
                }

                // Demo 2: Check Database Connection
                Console.WriteLine("\n=== Demo 2: Check Database Connection ===");
                try
                {
                    bool canConnect = await context.Database.CanConnectAsync();
                    Console.WriteLine($"Can connect to database: {canConnect}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error connecting to database: {ex.Message}");
                }

                // Demo 3: Get Pending Migrations
                Console.WriteLine("\n=== Demo 3: Get Pending Migrations ===");
                try
                {
                    var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                    Console.WriteLine("Pending migrations:");
                    foreach (var migration in pendingMigrations)
                    {
                        Console.WriteLine($"- {migration}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error getting pending migrations: {ex.Message}");
                }

                // Demo 4: Get Applied Migrations
                Console.WriteLine("\n=== Demo 4: Get Applied Migrations ===");
                try
                {
                    var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
                    Console.WriteLine("Applied migrations:");
                    foreach (var migration in appliedMigrations)
                    {
                        Console.WriteLine($"- {migration}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error getting applied migrations: {ex.Message}");
                }
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
} 