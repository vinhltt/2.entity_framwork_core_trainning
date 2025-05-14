using LessonDemo03.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LessonDemo03
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Configure DbContext with logging
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder
                .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=QueryingDemoDb;Trusted_Connection=True;")
                .LogTo(Console.WriteLine, LogLevel.Information)
                .EnableSensitiveDataLogging();

            await using (var context = new ApplicationDbContext(optionsBuilder.Options))
            {
                // Create database and apply migrations
                await context.Database.EnsureCreatedAsync();

                // Demo 1: Basic Querying
                Console.WriteLine("\n=== Demo 1: Basic Querying ===");
                var allProducts = await context.Products.ToListAsync();
                Console.WriteLine($"Total products: {allProducts.Count}");

                // Demo 2: Filtering
                Console.WriteLine("\n=== Demo 2: Filtering ===");
                var availableProducts = await context.Products
                    .Where(p => p.IsAvailable)
                    .ToListAsync();
                Console.WriteLine($"Available products: {availableProducts.Count}");

                // Demo 3: Complex Filtering
                Console.WriteLine("\n=== Demo 3: Complex Filtering ===");
                var expensiveElectronics = await context.Products
                    .Where(p => p.Price > 100 && p.CategoryId == 1)
                    .ToListAsync();
                Console.WriteLine($"Expensive electronics: {expensiveElectronics.Count}");

                // Demo 4: Sorting
                Console.WriteLine("\n=== Demo 4: Sorting ===");
                var sortedProducts = await context.Products
                    .OrderBy(p => p.CategoryId)
                    .ThenByDescending(p => p.Price)
                    .ToListAsync();
                Console.WriteLine("Products sorted by category and price:");
                foreach (var product in sortedProducts)
                {
                    Console.WriteLine($"- {product.Name} (${product.Price})");
                }

                // Demo 5: Projection
                Console.WriteLine("\n=== Demo 5: Projection ===");
                var productSummaries = await context.Products
                    .Select(p => new
                    {
                        p.Name,
                        p.Price,
                        CategoryName = p.Category.Name
                    })
                    .ToListAsync();
                Console.WriteLine("Product summaries:");
                foreach (var summary in productSummaries)
                {
                    Console.WriteLine($"- {summary.Name} (${summary.Price}) - {summary.CategoryName}");
                }

                // Demo 6: Aggregation
                Console.WriteLine("\n=== Demo 6: Aggregation ===");
                var stats = await context.Products
                    .GroupBy(p => p.CategoryId)
                    .Select(g => new
                    {
                        CategoryId = g.Key,
                        Count = g.Count(),
                        AveragePrice = g.Average(p => p.Price),
                        MaxPrice = g.Max(p => p.Price)
                    })
                    .ToListAsync();
                Console.WriteLine("Category statistics:");
                foreach (var stat in stats)
                {
                    Console.WriteLine($"Category {stat.CategoryId}: {stat.Count} products, " +
                                    $"Avg: ${stat.AveragePrice:F2}, Max: ${stat.MaxPrice:F2}");
                }

                // Demo 7: Paging
                Console.WriteLine("\n=== Demo 7: Paging ===");
                int pageSize = 2;
                int pageNumber = 1;
                var pagedProducts = await context.Products
                    .OrderBy(p => p.Id)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
                Console.WriteLine($"Page {pageNumber} (size: {pageSize}):");
                foreach (var product in pagedProducts)
                {
                    Console.WriteLine($"- {product.Name}");
                }

                // Demo 8: Eager Loading
                Console.WriteLine("\n=== Demo 8: Eager Loading ===");
                var categoriesWithProducts = await context.Categories
                    .Include(c => c.Products)
                    .ToListAsync();
                Console.WriteLine("Categories with their products:");
                foreach (var category in categoriesWithProducts)
                {
                    Console.WriteLine($"\n{category.Name}:");
                    foreach (var product in category.Products)
                    {
                        Console.WriteLine($"  - {product.Name} (${product.Price})");
                    }
                }

                // Demo 9: No Tracking
                Console.WriteLine("\n=== Demo 9: No Tracking ===");
                var readOnlyProducts = await context.Products
                    .AsNoTracking()
                    .ToListAsync();
                Console.WriteLine($"Read-only products loaded: {readOnlyProducts.Count}");

                // Demo 10: Raw SQL
                Console.WriteLine("\n=== Demo 10: Raw SQL ===");
                var expensiveProducts = await context.Products
                    .FromSqlRaw("SELECT * FROM Products WHERE Price > {0}", 100)
                    .ToListAsync();
                Console.WriteLine($"Products over $100: {expensiveProducts.Count}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
