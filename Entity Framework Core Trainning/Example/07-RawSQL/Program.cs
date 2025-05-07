using Microsoft.EntityFrameworkCore;
using RawSQL.Data;

// Create and configure DbContext
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=RawSQLDemo;Trusted_Connection=True;MultipleActiveResultSets=true")
    .Options;

await using var context = new ApplicationDbContext(options);

// Ensure database is created
context.Database.EnsureCreated();

// Create view if not exists
await context.Database.ExecuteSqlRawAsync(@"
    IF NOT EXISTS (SELECT * FROM sys.views WHERE name = 'ProductSummaryView')
    BEGIN
        EXEC('
            CREATE VIEW ProductSummaryView AS
            SELECT 
                p.Name,
                c.Name AS CategoryName,
                p.Price
            FROM Products p
            INNER JOIN Categories c ON p.CategoryId = c.Id
        ')
    END
");

// Create UDF if not exists
await context.Database.ExecuteSqlRawAsync(@"
    IF NOT EXISTS (SELECT * FROM sys.objects WHERE type = 'FN' AND name = 'IsProductPopular')
    BEGIN
        EXEC('
            CREATE FUNCTION IsProductPopular(@ProductId int)
            RETURNS bit
            AS
            BEGIN
                DECLARE @IsPopular bit = 0;
                
                -- For demo purposes, consider products in Electronics category as popular
                IF EXISTS (
                    SELECT 1 
                    FROM Products p 
                    WHERE p.Id = @ProductId 
                    AND p.CategoryId = 1
                )
                BEGIN
                    SET @IsPopular = 1;
                END

                RETURN @IsPopular;
            END
        ')
    END
");

// Demo 1: Querying with Raw SQL using FromSqlInterpolated
Console.WriteLine("\n=== Raw SQL Query Example ===");
var categoryId = 1;
decimal minPrice = 500;

var products = await context.Products
    .FromSqlInterpolated($"SELECT * FROM Products WHERE CategoryId = {categoryId} AND Price >= {minPrice}")
    .ToListAsync();

foreach (var product in products)
{
    Console.WriteLine($"Product: {product.Name}, Price: {product.Price:C}");
}

// Demo 2: Composing LINQ with Raw SQL
Console.WriteLine("\n=== Composing LINQ with Raw SQL ===");
var nameFilter = "%phone%";

var filteredProducts = await context.Products
    .FromSqlInterpolated($"SELECT * FROM Products WHERE CategoryId = {categoryId}")
    .Where(p => EF.Functions.Like(p.Name, nameFilter))
    .OrderByDescending(p => p.Price)
    .ToListAsync();

foreach (var product in filteredProducts)
{
    Console.WriteLine($"Product: {product.Name}, Price: {product.Price:C}");
}

// Demo 3: Querying View (Keyless Entity)
Console.WriteLine("\n=== Querying View Example ===");
var productSummaries = await context.ProductSummaries
    .Where(ps => ps.Price < 1000)
    .OrderBy(ps => ps.CategoryName)
    .ToListAsync();

foreach (var summary in productSummaries)
{
    Console.WriteLine($"Product: {summary.Name}, Category: {summary.CategoryName}, Price: {summary.Price:C}");
}

// Demo 4: Executing Non-Query SQL
Console.WriteLine("\n=== Executing Non-Query SQL ===");
var priceIncrease = 1.05m; // 5% increase

var affectedRows = await context.Database.ExecuteSqlInterpolatedAsync(
    $"UPDATE Products SET Price = Price * {priceIncrease} WHERE CategoryId = {categoryId}");

Console.WriteLine($"{affectedRows} products were updated.");

// Demo 5: Querying Scalar Value
Console.WriteLine("\n=== Querying Scalar Value ===");
var productCount = 0;
var connection = context.Database.GetDbConnection();
try
{
    await connection.OpenAsync();
    await using var command = connection.CreateCommand();
    command.CommandText = "SELECT COUNT(*) FROM Products WHERE CategoryId = @categoryId";
    var parameter = command.CreateParameter();
    parameter.ParameterName = "@categoryId";
    parameter.Value = categoryId;
    command.Parameters.Add(parameter);

    var result = await command.ExecuteScalarAsync();
    if (result != null && result != DBNull.Value)
    {
        productCount = Convert.ToInt32(result);
    }
}
finally
{
    await connection.CloseAsync();
}

Console.WriteLine($"Number of products in category {categoryId}: {productCount}");

// Demo 6: Using UDF in SQL
Console.WriteLine("\n=== Using UDF in SQL ===");
var popularProducts = await context.Products
    .FromSqlRaw("SELECT * FROM Products WHERE dbo.IsProductPopular(Id) = 1")
    .ToListAsync();

foreach (var product in popularProducts)
{
    Console.WriteLine($"Popular Product: {product.Name}");
} 