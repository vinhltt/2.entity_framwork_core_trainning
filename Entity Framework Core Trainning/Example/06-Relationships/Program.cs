using Microsoft.EntityFrameworkCore;
using Relationships.Data;
using Relationships.Models;

namespace _06_Relationships;

class Program
{
    static async Task Main(string[] args)
    {
        // Create and configure DbContext
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=RelationshipsDemo;Trusted_Connection=True;MultipleActiveResultSets=true")
            .Options;

        await using var context = new ApplicationDbContext(options);

        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Demo 1: Eager Loading - Load all books and author information
        Console.WriteLine("\n=== Eager Loading Example ===");
        var booksWithAuthors = await context.Books
            .Include(b => b.Author)
            .Include(b => b.Publisher)
            .Include(b => b.BookCategories)
                .ThenInclude(bc => bc.Category)
            .ToListAsync();

        foreach (var book in booksWithAuthors)
        {
            Console.WriteLine($"Book: {book.Title}");
            Console.WriteLine($"Author: {book.Author?.Name}");
            Console.WriteLine($"Publisher: {book.Publisher?.Name}");
            Console.WriteLine("Categories:");
            foreach (var bookCategory in book.BookCategories)
            {
                Console.WriteLine($"- {bookCategory.Category?.Name}");
            }
            Console.WriteLine();
        }

        // Demo 2: Explicit Loading - Load author contact information when needed
        Console.WriteLine("\n=== Explicit Loading Example ===");
        var author = await context.Authors.FirstOrDefaultAsync();
        if (author != null)
        {
            await context.Entry(author)
                .Reference(a => a.Contact)
                .LoadAsync();

            Console.WriteLine($"Author: {author.Name}");
            Console.WriteLine($"Contact: {author.Contact?.Email}, {author.Contact?.Phone}");
        }

        // Demo 3: Projection - Get only necessary information
        Console.WriteLine("\n=== Projection Example ===");
        var bookSummaries = await context.Books
            .Select(b => new
            {
                b.Title,
                AuthorName = b.Author.Name,
                PublisherName = b.Publisher.Name,
                CategoryCount = b.BookCategories.Count
            })
            .ToListAsync();

        foreach (var summary in bookSummaries)
        {
            Console.WriteLine($"Book: {summary.Title}");
            Console.WriteLine($"Author: {summary.AuthorName}");
            Console.WriteLine($"Publisher: {summary.PublisherName}");
            Console.WriteLine($"Number of Categories: {summary.CategoryCount}");
            Console.WriteLine();
        }

        // Demo 4: Add new book with relationships
        Console.WriteLine("\n=== Adding New Book with Relationships ===");
        var newBook = new Book
        {
            Title = "New Programming Book",
            Description = "Learn programming concepts",
            Price = 39.99m,
            PublishedDate = DateTime.UtcNow,
            AuthorId = 1,
            PublisherId = 1
        };

        context.Books.Add(newBook);
        await context.SaveChangesAsync();

        // Add categories for new book
        var newBookCategories = new[]
        {
            new BookCategory { BookId = newBook.Id, CategoryId = 1 },
            new BookCategory { BookId = newBook.Id, CategoryId = 3 }
        };

        context.BookCategories.AddRange(newBookCategories);
        await context.SaveChangesAsync();

        Console.WriteLine($"Added new book with ID: {newBook.Id}");

        // Demo 5: Update relationships
        Console.WriteLine("\n=== Updating Relationships ===");
        var bookToUpdate = await context.Books
            .Include(b => b.BookCategories)
            .FirstOrDefaultAsync(b => b.Id == newBook.Id);

        if (bookToUpdate != null)
        {
            // Remove all existing categories
            context.BookCategories.RemoveRange(bookToUpdate.BookCategories);

            // Add new category
            var newCategory = new BookCategory { BookId = bookToUpdate.Id, CategoryId = 2 };
            context.BookCategories.Add(newCategory);

            await context.SaveChangesAsync();
            Console.WriteLine("Updated book categories");
        }

        // Demo 6: Delete relationships
        Console.WriteLine("\n=== Deleting Relationships ===");
        var bookToDelete = await context.Books
            .Include(b => b.BookCategories)
            .FirstOrDefaultAsync(b => b.Id == newBook.Id);

        if (bookToDelete != null)
        {
            // Remove all categories first
            context.BookCategories.RemoveRange(bookToDelete.BookCategories);
            await context.SaveChangesAsync();

            // Then delete the book
            context.Books.Remove(bookToDelete);
            await context.SaveChangesAsync();

            Console.WriteLine("Deleted book and its relationships");
        }
    }
}
