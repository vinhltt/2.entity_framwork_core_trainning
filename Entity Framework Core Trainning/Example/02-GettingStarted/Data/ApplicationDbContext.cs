using Microsoft.EntityFrameworkCore;
using GettingStarted.Models;

namespace GettingStarted.Data
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
                new Product { Id = 1, Name = "Laptop Pro", Price = 1200.00m, CategoryId = 1 },
                new Product { Id = 2, Name = "Learning EF Core", Price = 45.50m, CategoryId = 2 },
                new Product { Id = 3, Name = "Wireless Mouse", Price = 25.99m, CategoryId = 1 }
            );
        }
    }
}