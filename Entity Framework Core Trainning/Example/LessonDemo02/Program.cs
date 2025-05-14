using LessonDemo02.Data;
using LessonDemo02.Models;
using Microsoft.EntityFrameworkCore;

namespace LessonDemo02
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Configure DbContext
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=GettingStartedDb;Trusted_Connection=True;");

            await using (var context = new ApplicationDbContext(optionsBuilder.Options))
            {
                // Create database and apply migrations
                await context.Database.EnsureCreatedAsync();

                // Demo 1: Get all products
                Console.WriteLine("Product List:");
                var products = context.Products.Include(p => p.Category).ToList();
                foreach (var product in products)
                {
                    Console.WriteLine($"- {product.Name} (${product.Price}) - Category: {product.Category?.Name ?? "N/A"}");
                }

                // Demo 2: Add new product
                Console.WriteLine("\nAdding new product...");
                var newProduct = new Product
                {
                    Name = "New Product",
                    Price = 99.99m,
                    CategoryId = 1
                };
                context.Products.Add(newProduct);
                await context.SaveChangesAsync();
                Console.WriteLine("New product added!");

                // Demo 3: Update product
                Console.WriteLine("\nUpdating product...");
                var productToUpdate = await context.Products.FindAsync(1);
                if (productToUpdate != null)
                {
                    productToUpdate.Price = 1299.99m;
                    await context.SaveChangesAsync();
                    Console.WriteLine("Product price updated!");
                }

                // Demo 4: Delete product
                Console.WriteLine("\nDeleting product...");
                var productToDelete = await context.Products.FindAsync(3);
                if (productToDelete != null)
                {
                    context.Products.Remove(productToDelete);
                    await context.SaveChangesAsync();
                    Console.WriteLine("Product deleted!");
                }

                // Demo 5: Complex query
                Console.WriteLine("\nProducts with price > $1000:");
                var expensiveProducts = await context.Products
                    .Include(p => p.Category)
                    .Where(p => p.Price > 1000)
                    .ToListAsync();
                foreach (var product in expensiveProducts)
                {
                    Console.WriteLine($"- {product.Name} (${product.Price}) - Category: {product.Category?.Name ?? "N/A"}");
                }
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
