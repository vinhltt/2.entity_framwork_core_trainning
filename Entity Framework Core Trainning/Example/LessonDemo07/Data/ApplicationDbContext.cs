using LessonDemo07.Models;
using Microsoft.EntityFrameworkCore;

namespace LessonDemo07.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<ProductSummary> ProductSummaries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Product
            modelBuilder.Entity<Product>()
                .Property(p => p.Name)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            // Configure Category
            modelBuilder.Entity<Category>()
                .Property(c => c.Name)
                .HasMaxLength(50)
                .IsRequired();

            // Configure ProductSummary (View)
            modelBuilder.Entity<ProductSummary>(eb =>
            {
                eb.HasNoKey();
                eb.ToView("ProductSummaryView");
            });

            // Seed Data
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Electronics", Description = "Electronic devices and gadgets" },
                new Category { Id = 2, Name = "Books", Description = "Books and publications" },
                new Category { Id = 3, Name = "Clothing", Description = "Apparel and fashion items" }
            );

            modelBuilder.Entity<Product>().HasData(
                new Product 
                { 
                    Id = 1, 
                    Name = "Laptop", 
                    Description = "High-performance laptop",
                    Price = 999.99m,
                    IsAvailable = true,
                    CategoryId = 1
                },
                new Product 
                { 
                    Id = 2, 
                    Name = "Smartphone", 
                    Description = "Latest smartphone model",
                    Price = 699.99m,
                    IsAvailable = true,
                    CategoryId = 1
                },
                new Product 
                { 
                    Id = 3, 
                    Name = "Programming Book", 
                    Description = "Learn programming",
                    Price = 49.99m,
                    IsAvailable = true,
                    CategoryId = 2
                }
            );
        }

        // Stub method for UDF
        public static bool IsProductPopular(int productId)
        {
            throw new NotSupportedException();
        }
    }
} 