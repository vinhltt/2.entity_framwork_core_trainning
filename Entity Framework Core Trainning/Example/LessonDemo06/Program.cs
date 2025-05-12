using LessonDemo06.Data;
using LessonDemo06.Models;
using Microsoft.EntityFrameworkCore;

namespace LessonDemo06
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await using var context = new ApplicationDbContext();
            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Example 1: Inserting Related Data
            Console.WriteLine("Example 1: Inserting Related Data");
            await InsertRelatedDataExample(context);

            // Example 2: Querying Related Data
            Console.WriteLine("\nExample 2: Querying Related Data");
            await QueryRelatedDataExample(context);

            // Example 3: Updating Related Data
            Console.WriteLine("\nExample 3: Updating Related Data");
            await UpdateRelatedDataExample(context);

            // Example 4: Deleting Related Data
            Console.WriteLine("\nExample 4: Deleting Related Data");
            await DeleteRelatedDataExample(context);
        }

        static async Task InsertRelatedDataExample(ApplicationDbContext context)
        {
            // Method 1: Direct FK assignment
            var category = new Category { Name = "Electronics" };
            context.Categories.Add(category);
            await context.SaveChangesAsync();

            var product1 = new Product
            {
                Name = "Laptop",
                Price = 999.99m,
                CategoryId = category.Id // Direct FK assignment
            };
            context.Products.Add(product1);

            // Method 2: Navigation property assignment
            var product2 = new Product
            {
                Name = "Smartphone",
                Price = 499.99m,
                Category = category // Navigation property assignment
            };
            context.Products.Add(product2);

            // Method 3: Collection navigation
            var product3 = new Product
            {
                Name = "Tablet",
                Price = 299.99m
            };
            category.Products.Add(product3); // Collection navigation

            // Many-to-Many relationship
            var tag1 = new Tag { Name = "New" };
            var tag2 = new Tag { Name = "Popular" };
            context.Tags.AddRange(tag1, tag2);

            product1.Tags.Add(tag1);
            product2.Tags.Add(tag1);
            product2.Tags.Add(tag2);

            // One-to-One relationship
            var user = new User { Username = "john_doe" };
            var profile = new UserProfile
            {
                FullName = "John Doe",
                AvatarUrl = "https://example.com/avatar.jpg",
                User = user
            };
            context.Users.Add(user);
            context.UserProfiles.Add(profile);

            await context.SaveChangesAsync();
            Console.WriteLine("Data inserted successfully!");
        }

        static async Task QueryRelatedDataExample(ApplicationDbContext context)
        {
            // Eager Loading
            Console.WriteLine("\nEager Loading Example:");
            var products = await context.Products
                .Include(p => p.Category)
                .Include(p => p.Tags)
                .ToListAsync();

            foreach (var product in products)
            {
                Console.WriteLine($"Product: {product.Name}, Category: {product.Category.Name}");
                Console.WriteLine("Tags: " + string.Join(", ", product.Tags.Select(t => t.Name)));
            }

            // Explicit Loading
            Console.WriteLine("\nExplicit Loading Example:");
            var firstProduct = await context.Products.FirstAsync();
            await context.Entry(firstProduct)
                .Reference(p => p.Category)
                .LoadAsync();
            await context.Entry(firstProduct)
                .Collection(p => p.Tags)
                .LoadAsync();

            Console.WriteLine($"Product: {firstProduct.Name}, Category: {firstProduct.Category.Name}");
            Console.WriteLine("Tags: " + string.Join(", ", firstProduct.Tags.Select(t => t.Name)));

            // Lazy Loading
            Console.WriteLine("\nLazy Loading Example:");
            var lazyProduct = await context.Products.FirstAsync();
            Console.WriteLine($"Product: {lazyProduct.Name}, Category: {lazyProduct.Category.Name}");
            Console.WriteLine("Tags: " + string.Join(", ", lazyProduct.Tags.Select(t => t.Name)));

            // Selective Loading
            Console.WriteLine("\nSelective Loading Example:");
            var productInfo = await context.Products
                .Select(p => new
                {
                    p.Name,
                    CategoryName = p.Category.Name,
                    TagCount = p.Tags.Count
                })
                .ToListAsync();

            foreach (var info in productInfo)
            {
                Console.WriteLine($"Product: {info.Name}, Category: {info.CategoryName}, Tag Count: {info.TagCount}");
            }
        }

        static async Task UpdateRelatedDataExample(ApplicationDbContext context)
        {
            // Update product's category
            var product = await context.Products.FirstAsync();
            var newCategory = new Category { Name = "New Category" };
            context.Categories.Add(newCategory);
            await context.SaveChangesAsync();

            product.Category = newCategory;
            await context.SaveChangesAsync();
            Console.WriteLine($"Updated product {product.Name} to category {newCategory.Name}");

            // Update product's tags
            var newTag = new Tag { Name = "Updated" };
            context.Tags.Add(newTag);
            await context.SaveChangesAsync();

            product.Tags.Clear();
            product.Tags.Add(newTag);
            await context.SaveChangesAsync();
            Console.WriteLine($"Updated product {product.Name} tags to {newTag.Name}");

            // Update user profile
            var user = await context.Users.FirstAsync();
            user.Profile.FullName = "John Doe Updated";
            await context.SaveChangesAsync();
            Console.WriteLine($"Updated user profile for {user.Username}");
        }

        static async Task DeleteRelatedDataExample(ApplicationDbContext context)
        {
            // Delete a product (will not delete its category due to Restrict delete behavior)
            var product = await context.Products.FirstAsync();
            context.Products.Remove(product);
            await context.SaveChangesAsync();
            Console.WriteLine($"Deleted product {product.Name}");

            // Delete a category (will fail if it has products due to Restrict delete behavior)
            try
            {
                var category = await context.Categories.FirstAsync();
                context.Categories.Remove(category);
                await context.SaveChangesAsync();
                Console.WriteLine($"Deleted category {category.Name}");
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Cannot delete category: {ex.Message}");
            }

            // Delete a user (will delete the profile due to cascade delete)
            var user = await context.Users.FirstAsync();
            context.Users.Remove(user);
            await context.SaveChangesAsync();
            Console.WriteLine($"Deleted user {user.Username} and their profile");
        }
    }
}
