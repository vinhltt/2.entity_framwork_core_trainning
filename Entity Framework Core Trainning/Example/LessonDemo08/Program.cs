using LessonDemo08.Data;
using LessonDemo08.Models;
using Microsoft.EntityFrameworkCore;

// Create and configure DbContext with retry policy
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseSqlServer(
        "Server=(localdb)\\mssqllocaldb;Database=AdditionalFeaturesDemo;Trusted_Connection=True;MultipleActiveResultSets=true",
        sqlServerOptionsAction: sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        })
    .Options;

await using var context = new ApplicationDbContext(options);

// Ensure database is created
context.Database.EnsureCreated();

// Seed sample data
if (!await context.Categories.AnyAsync())
{
    var category = new Category
    {
        Name = "Electronics",
        Description = "Electronic devices and accessories",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
    context.Categories.Add(category);
    await context.SaveChangesAsync();

    var sampleProduct = new Product
    {
        Name = "Smartphone",
        Description = "Latest model smartphone",
        Price = 999.99m,
        IsAvailable = true,
        CategoryId = category.Id,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
    context.Products.Add(sampleProduct);
    await context.SaveChangesAsync();

    var sampleEmployee = new Employee
    {
        Name = "John Doe",
        Position = "Software Developer",
        Salary = 80000
    };
    context.Employees.Add(sampleEmployee);
    await context.SaveChangesAsync();
}

// Demo 1: Soft Delete
Console.WriteLine("\n=== Soft Delete Demo ===");
var product = await context.Products.FirstOrDefaultAsync(p => p.Id == 1);
if (product != null)
{
    product.IsDeleted = true;
    await context.SaveChangesAsync();
    Console.WriteLine($"Product {product.Name} has been soft deleted.");

    // Query should not return soft-deleted items
    var activeProducts = await context.Products.ToListAsync();
    Console.WriteLine($"Active products count: {activeProducts.Count}");

    // Use IgnoreQueryFilters to include soft-deleted items
    var allProducts = await context.Products.IgnoreQueryFilters().ToListAsync();
    Console.WriteLine($"All products count (including deleted): {allProducts.Count}");
}

// Demo 2: Temporal Tables
Console.WriteLine("\n=== Temporal Tables Demo ===");
var employee = await context.Employees.FirstOrDefaultAsync(e => e.Id == 1);
if (employee != null)
{
    // Get current state
    Console.WriteLine($"Current employee position: {employee.Position}");

    // Update employee
    var oldPosition = employee.Position;
    employee.Position = "Senior Software Developer";
    await context.SaveChangesAsync();

    // Get historical versions
    var history = await context.Employees
        .TemporalAll()
        .Where(e => e.Id == 1)
        .OrderBy(e => e.ValidFrom)
        .Select(e => new
        {
            e.Position,
            ValidFrom = e.ValidFrom
        })
        .ToListAsync();

    Console.WriteLine("Employee history:");
    foreach (var version in history)
    {
        Console.WriteLine($"Position: {version.Position}, ValidFrom: {version.ValidFrom}");
    }
}

// Demo 3: Concurrency Handling
Console.WriteLine("\n=== Concurrency Demo ===");
try
{
    // Simulate concurrent update
    var product1 = await context.Products.FirstOrDefaultAsync(p => p.Id == 1);
    var product2 = await context.Products.FirstOrDefaultAsync(p => p.Id == 1);

    if (product1 != null && product2 != null)
    {
        // First update
        product1.Price += 100;
        await context.SaveChangesAsync();

        // Second update (should fail due to concurrency)
        product2.Price += 200;
        await context.SaveChangesAsync();
    }
}
catch (DbUpdateConcurrencyException ex)
{
    Console.WriteLine("Concurrency conflict detected!");
    var entry = ex.Entries.Single();
    var databaseValues = await entry.GetDatabaseValuesAsync();

    if (databaseValues != null)
    {
        var databaseEntity = (Product)databaseValues.ToObject();
        Console.WriteLine($"Database value: {databaseEntity.Price}");
        Console.WriteLine($"Current value: {((Product)entry.Entity).Price}");
    }
}

// Demo 4: Transaction
Console.WriteLine("\n=== Transaction Demo ===");
var strategy = context.Database.CreateExecutionStrategy();
await strategy.ExecuteAsync(async () =>
{
    await using var transaction = await context.Database.BeginTransactionAsync();
    try
    {
        // Add new category
        var newCategory = new Category
        {
            Name = "New Category",
            Description = "Created in transaction",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Categories.Add(newCategory);
        await context.SaveChangesAsync();

        // Add new product
        var newProduct = new Product
        {
            Name = "New Product",
            Description = "Created in transaction",
            Price = 199.99m,
            IsAvailable = true,
            CategoryId = newCategory.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Products.Add(newProduct);
        await context.SaveChangesAsync();

        // Commit transaction
        await transaction.CommitAsync();
        Console.WriteLine("Transaction committed successfully.");
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        Console.WriteLine($"Transaction rolled back: {ex.Message}");
    }
});

// Demo 5: Data Validation
Console.WriteLine("\n=== Data Validation Demo ===");
try
{
    var category = await context.Categories.FirstAsync();
    var invalidProduct = new Product
    {
        Name = "A", // Too short
        Price = -100, // Invalid price
        CategoryId = category.Id, // Valid category
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
    context.Products.Add(invalidProduct);
    await context.SaveChangesAsync();
}
catch (DbUpdateException ex)
{
    Console.WriteLine($"Validation error: {ex.InnerException?.Message}");
} 