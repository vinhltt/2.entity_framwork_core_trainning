using Microsoft.EntityFrameworkCore;
using Relationships.Models;

namespace Relationships.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Author> Authors { get; set; }
        public DbSet<AuthorContact> AuthorContacts { get; set; }
        public DbSet<Book> Books { get; set; }
        public DbSet<Publisher> Publishers { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<BookCategory> BookCategories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Author
            modelBuilder.Entity<Author>()
                .Property(a => a.Name)
                .HasMaxLength(100)
                .IsRequired();

            // Configure AuthorContact (One-to-One with Author)
            modelBuilder.Entity<AuthorContact>()
                .HasOne(ac => ac.Author)
                .WithOne(a => a.Contact)
                .HasForeignKey<AuthorContact>(ac => ac.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Book
            modelBuilder.Entity<Book>()
                .Property(b => b.Title)
                .HasMaxLength(200)
                .IsRequired();

            modelBuilder.Entity<Book>()
                .Property(b => b.Price)
                .HasColumnType("decimal(18,2)");

            // Configure Book-Author relationship (Many-to-One)
            modelBuilder.Entity<Book>()
                .HasOne(b => b.Author)
                .WithMany(a => a.Books)
                .HasForeignKey(b => b.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Book-Publisher relationship (Many-to-One)
            modelBuilder.Entity<Book>()
                .HasOne(b => b.Publisher)
                .WithMany(p => p.Books)
                .HasForeignKey(b => b.PublisherId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure BookCategory (Many-to-Many)
            modelBuilder.Entity<BookCategory>()
                .HasKey(bc => new { bc.BookId, bc.CategoryId });

            modelBuilder.Entity<BookCategory>()
                .HasOne(bc => bc.Book)
                .WithMany(b => b.BookCategories)
                .HasForeignKey(bc => bc.BookId);

            modelBuilder.Entity<BookCategory>()
                .HasOne(bc => bc.Category)
                .WithMany(c => c.BookCategories)
                .HasForeignKey(bc => bc.CategoryId);

            // Seed Data
            modelBuilder.Entity<Author>().HasData(
                new Author { Id = 1, Name = "John Smith", Biography = "Famous author", DateOfBirth = new DateTime(1970, 1, 1) },
                new Author { Id = 2, Name = "Jane Doe", Biography = "Award-winning writer", DateOfBirth = new DateTime(1980, 5, 15) }
            );

            modelBuilder.Entity<AuthorContact>().HasData(
                new AuthorContact { Id = 1, AuthorId = 1, Email = "john@example.com", Phone = "123-456-7890" },
                new AuthorContact { Id = 2, AuthorId = 2, Email = "jane@example.com", Phone = "098-765-4321" }
            );

            modelBuilder.Entity<Publisher>().HasData(
                new Publisher { Id = 1, Name = "Tech Books", Description = "Technology book publisher" },
                new Publisher { Id = 2, Name = "Fiction House", Description = "Fiction book publisher" }
            );

            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Technology", Description = "Technology related books" },
                new Category { Id = 2, Name = "Fiction", Description = "Fiction books" },
                new Category { Id = 3, Name = "Programming", Description = "Programming books" }
            );

            modelBuilder.Entity<Book>().HasData(
                new Book 
                { 
                    Id = 1, 
                    Title = "C# Programming", 
                    Description = "Learn C# programming",
                    Price = 49.99m,
                    PublishedDate = DateTime.UtcNow.AddDays(-100),
                    AuthorId = 1,
                    PublisherId = 1
                },
                new Book 
                { 
                    Id = 2, 
                    Title = "The Adventure", 
                    Description = "An exciting adventure story",
                    Price = 29.99m,
                    PublishedDate = DateTime.UtcNow.AddDays(-50),
                    AuthorId = 2,
                    PublisherId = 2
                }
            );

            modelBuilder.Entity<BookCategory>().HasData(
                new BookCategory { BookId = 1, CategoryId = 1 },
                new BookCategory { BookId = 1, CategoryId = 3 },
                new BookCategory { BookId = 2, CategoryId = 2 }
            );
        }
    }
} 