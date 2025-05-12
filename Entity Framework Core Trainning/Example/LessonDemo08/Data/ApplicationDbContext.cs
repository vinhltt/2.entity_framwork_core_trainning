using LessonDemo08.Models;
using Microsoft.EntityFrameworkCore;

namespace LessonDemo08.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Employee> Employees { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Product
            modelBuilder.Entity<Product>(entity =>
            {
                entity.Property(p => p.Name).HasMaxLength(100).IsRequired();
                entity.Property(p => p.Price).HasColumnType("decimal(18,2)");

                // Configure as temporal table
                entity.ToTable("Products", b => b.IsTemporal());

                // Add query filter for soft delete
                entity.HasQueryFilter(p => !p.IsDeleted);
            });

            // Configure Category
            modelBuilder.Entity<Category>(entity =>
            {
                entity.Property(c => c.Name).HasMaxLength(50).IsRequired();

                // Configure as temporal table
                entity.ToTable("Categories", b => b.IsTemporal());

                // Add query filter for soft delete
                entity.HasQueryFilter(c => !c.IsDeleted);
            });

            // Configure Employee
            modelBuilder.Entity<Employee>(entity =>
            {
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Position).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Salary).HasColumnType("decimal(18,2)");

                // Configure as temporal table
                entity.ToTable("Employees", b => b.IsTemporal());
            });

            // Seed Data
            modelBuilder.Entity<Category>().HasData(
                new Category 
                { 
                    Id = 1, 
                    Name = "Electronics", 
                    Description = "Electronic devices and gadgets",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Category 
                { 
                    Id = 2, 
                    Name = "Books", 
                    Description = "Books and publications",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );

            modelBuilder.Entity<Product>().HasData(
                new Product 
                { 
                    Id = 1, 
                    Name = "Laptop", 
                    Description = "High-performance laptop",
                    Price = 999.99m,
                    IsAvailable = true,
                    CategoryId = 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Product 
                { 
                    Id = 2, 
                    Name = "Smartphone", 
                    Description = "Latest smartphone model",
                    Price = 699.99m,
                    IsAvailable = true,
                    CategoryId = 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );

            modelBuilder.Entity<Employee>().HasData(
                new Employee
                {
                    Id = 1,
                    Name = "John Doe",
                    Position = "Software Developer",
                    Salary = 80000.00m,
                    HireDate = DateTime.UtcNow.AddYears(-2),
                    IsActive = true,
                    ValidFrom = DateTime.UtcNow,
                    ValidTo = DateTime.MaxValue
                },
                new Employee
                {
                    Id = 2,
                    Name = "Jane Smith",
                    Position = "Project Manager",
                    Salary = 100000.00m,
                    HireDate = DateTime.UtcNow.AddYears(-1),
                    IsActive = true,
                    ValidFrom = DateTime.UtcNow,
                    ValidTo = DateTime.MaxValue
                }
            );
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            // Set default max length for all string properties
            configurationBuilder.Properties<string>().HaveMaxLength(100);

            // Configure all DateTime properties to use datetime2
            configurationBuilder.Properties<DateTime>().HaveColumnType("datetime2");
        }

        public override int SaveChanges()
        {
            OnBeforeSaving();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            OnBeforeSaving();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void OnBeforeSaving()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is IAuditableEntity &&
                            (e.State == EntityState.Added || e.State == EntityState.Modified));

            var now = DateTime.UtcNow;

            foreach (var entry in entries)
            {
                var entity = (IAuditableEntity)entry.Entity;

                if (entry.State == EntityState.Added)
                {
                    entity.CreatedAt = now;
                }

                entity.UpdatedAt = now;
            }
        }
    }
} 