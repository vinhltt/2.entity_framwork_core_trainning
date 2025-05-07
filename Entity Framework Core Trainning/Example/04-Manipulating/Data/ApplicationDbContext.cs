using Microsoft.EntityFrameworkCore;
using Manipulating.Models;

namespace Manipulating.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Product
            modelBuilder.Entity<Product>()
                .Property(p => p.Name)
                .HasMaxLength(200)
                .IsRequired();

            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            // Configure Category
            modelBuilder.Entity<Category>()
                .Property(c => c.Name)
                .HasMaxLength(100)
                .IsRequired();

            // Seed Data
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Electronics", Description = "Gadgets and devices" },
                new Category { Id = 2, Name = "Books", Description = "Paperback and hardcover books" },
                new Category { Id = 3, Name = "Clothing", Description = "Apparel and accessories" }
            );

            modelBuilder.Entity<Product>().HasData(
                new Product 
                { 
                    Id = 1, 
                    Name = "Laptop Pro", 
                    Description = "High-performance laptop",
                    Price = 1200.00m, 
                    IsAvailable = true,
                    CategoryId = 1,
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                },
                new Product 
                { 
                    Id = 2, 
                    Name = "Learning EF Core", 
                    Description = "Book about Entity Framework Core",
                    Price = 45.50m, 
                    IsAvailable = true,
                    CategoryId = 2,
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new Product 
                { 
                    Id = 3, 
                    Name = "Wireless Mouse", 
                    Description = "Ergonomic wireless mouse",
                    Price = 25.99m, 
                    IsAvailable = true,
                    CategoryId = 1,
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                },
                new Product 
                { 
                    Id = 4, 
                    Name = "T-Shirt", 
                    Description = "Cotton t-shirt",
                    Price = 19.99m, 
                    IsAvailable = false,
                    CategoryId = 3,
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                }
            );
        }
    }
} 