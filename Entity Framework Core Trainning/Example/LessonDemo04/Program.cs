using Microsoft.EntityFrameworkCore;
using LessonDemo04.Data;
using LessonDemo04.Models;
using Microsoft.Extensions.Logging;

namespace LessonDemo04
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Configure DbContext with logging
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder
                .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=LessonDemo04DemoDb;Trusted_Connection=True;")
                .LogTo(Console.WriteLine, LogLevel.Information)
                .EnableSensitiveDataLogging();

            await using (var context = new ApplicationDbContext(optionsBuilder.Options))
            {
                // Create database and apply migrations
                await context.Database.EnsureCreatedAsync();

                // Demo 1: Insert Operations
                Console.WriteLine("\n=== Demo 1: Insert Operations ===");
                var newProduct = new Product
                {
                    Name = "New Amazing Gadget",
                    Description = "Latest tech gadget",
                    Price = 299.99m,
                    IsAvailable = true,
                    CategoryId = 1,
                    CreatedAt = DateTime.UtcNow
                };

                context.Products.Add(newProduct);
                await context.SaveChangesAsync();
                Console.WriteLine($"Added new product with ID: {newProduct.Id}");

                // Demo 2: Update Operations
                Console.WriteLine("\n=== Demo 2: Update Operations ===");
                var productToUpdate = await context.Products.FindAsync(1);
                if (productToUpdate != null)
                {
                    Console.WriteLine($"Old price: {productToUpdate.Price}");
                    productToUpdate.Price *= 1.1m; // Increase price by 10%
                    await context.SaveChangesAsync();
                    Console.WriteLine($"New price: {productToUpdate.Price}");
                }

                // Demo 3: Delete Operations
                Console.WriteLine("\n=== Demo 3: Delete Operations ===");
                var productToDelete = await context.Products.FindAsync(4);
                if (productToDelete != null)
                {
                    context.Products.Remove(productToDelete);
                    await context.SaveChangesAsync();
                    Console.WriteLine($"Deleted product with ID: {productToDelete.Id}");
                }

                // Demo 4: ExecuteUpdate (EF Core 7+)
                Console.WriteLine("\n=== Demo 4: ExecuteUpdate ===");
                var updatedCount = await context.Products
                    .Where(p => p.CategoryId == 1)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(p => p.Price, p => p.Price * 1.05m)
                        .SetProperty(p => p.IsAvailable, true)
                    );
                Console.WriteLine($"Updated {updatedCount} products in Electronics category");

                // Demo 5: ExecuteDelete (EF Core 7+)
                Console.WriteLine("\n=== Demo 5: ExecuteDelete ===");
                var deletedCount = await context.Products
                    .Where(p => p.Price < 20)
                    .ExecuteDeleteAsync();
                Console.WriteLine($"Deleted {deletedCount} products with price < $20");

                // Demo 6: Tracking States
                Console.WriteLine("\n=== Demo 6: Tracking States ===");
                var product = new Product
                {
                    Name = "Test Product",
                    Price = 99.99m,
                    CategoryId = 1,
                    CreatedAt = DateTime.UtcNow
                };

                Console.WriteLine($"Initial state: {context.Entry(product).State}"); // Detached

                context.Products.Add(product);
                Console.WriteLine($"After Add: {context.Entry(product).State}"); // Added

                await context.SaveChangesAsync();
                Console.WriteLine($"After SaveChanges: {context.Entry(product).State}"); // Unchanged

                product.Price = 89.99m;
                Console.WriteLine($"After modification: {context.Entry(product).State}"); // Modified

                await context.SaveChangesAsync();
                Console.WriteLine($"After second SaveChanges: {context.Entry(product).State}"); // Unchanged

                context.Products.Remove(product);
                Console.WriteLine($"After Remove: {context.Entry(product).State}"); // Deleted

                await context.SaveChangesAsync();
                Console.WriteLine($"After final SaveChanges: {context.Entry(product).State}"); // Detached
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
