# Working With Raw SQL, Views and Stored Procedures

**Working With Raw SQL, Views and Stored Procedures**

- **Section Overview**
    
    We have covered how EF Core helps abstract database interaction through LINQ and object models. 
    
    However, in practice, there are situations where using raw SQL, Views, or Stored Procedures is a better or even required choice.
    
    **Why use Raw SQL/Views/SPs?**
    
    - **Performance optimization:** Some complex queries can be written more efficiently in raw SQL than with LINQ-to-Entities.
    - **Complex logic:** Stored Procedures can encapsulate complex business logic directly in the database.
    - **Legacy Database:** Working with existing databases that already have many Views, Stored Procedures, and Functions you need to leverage.
    - **Database features not directly supported by EF Core:** Use special features of your DBMS.
    
    This approach comes with certain trade-offs, which we will discuss.
    
- Adding Non-Table Objects with Migrations
    
    Although Migrations mainly focus on creating/modifying tables for your entities, you can execute arbitrary SQL to create other database objects like Views, Stored Procedures, Functions, Triggers, or even modify tables in ways EF Core does not directly support.
    
    - **How to do it:** Use the `migrationBuilder.Sql()` method inside the `Up()` (to create/modify) and `Down()` (to revert/delete) methods of a migration file.
    - **Example:** Create a simple View called `ProductSummaryView` in a migration.
        
        ```csharp
        // In a new migration file (e.g., Migrations/20250411110000_AddProductSummaryView.cs)
        using Microsoft.EntityFrameworkCore.Migrations;
        
        public partial class AddProductSummaryView : Migration
        {
            protected override void Up(MigrationBuilder migrationBuilder)
            {
                // SQL to create View
                migrationBuilder.Sql(@"
                    CREATE VIEW ProductSummaryView AS
                    SELECT p.Id, p.Name, p.Price, c.Name AS CategoryName
                    FROM Products p
                    INNER JOIN Categories c ON p.CategoryId = c.Id
                    WHERE p.IsAvailable = 1;
                ");
            }
        
            protected override void Down(MigrationBuilder migrationBuilder)
            {
                // SQL to drop View when rolling back
                migrationBuilder.Sql(@"
                    DROP VIEW ProductSummaryView;
                ");
            }
        }
        ```
        
    - **Important:**
        - Always write the corresponding SQL in `Down()` to ensure the migration can be rolled back (`dotnet ef database update [PreviousMigration]`).
        - The SQL syntax must match your DBMS (SQL Server, PostgreSQL, etc.).
        - EF Core does not manage or understand the content of Views/SPs created this way. It simply executes the SQL you provide.
    
    After creating this migration, running `dotnet ef database update` will create the `ProductSummaryView` in your database.
    
- Querying Keyless Entities (Like Views)
    
    When you have a View (or a similar object without a clear primary key, such as the result of some Stored Procedures or `FromSql`), you need a way for EF Core to understand and query it. This is where **Keyless Entity Types** are useful.
    
    - **Concept:** These are entity classes defined in your EF Core model but configured as **having no primary key (No Key)**.
    - **Use for Views:** This is the most common way to map and query Views in the database.
    - **Steps:**
        1. **Create a C# class matching the View:** Create a POCO class with properties matching the columns and data types of the View.
            
            ```csharp
            // Models/ProductSummary.cs (class representing the View)
            public class ProductSummary
            {
                public int Id { get; set; } // Even if the view has an Id column, we still declare it as keyless
                public string Name { get; set; }
                public decimal Price { get; set; }
                public string CategoryName { get; set; }
            }
            ```
            
        2. **Configure in DbContext:** Use Fluent API in `OnModelCreating` to:
            - Register the entity.
            - Specify it has no key (`HasNoKey()`).
            - Map it to the View in the database (`ToView("ViewName")`).
            
            ```csharp
            // Data/ApplicationDbContext.cs
            public class ApplicationDbContext : DbContext
            {
                // ... constructor and other DbSets ...
                public DbSet<ProductSummary> ProductSummaries { get; set; } // DbSet for keyless entity
            
                protected override void OnModelCreating(ModelBuilder modelBuilder)
                {
                    // ... other configurations ...
            
                    modelBuilder.Entity<ProductSummary>(eb =>
                    {
                        eb.HasNoKey(); // **Important: Mark as keyless**
                        eb.ToView("ProductSummaryView"); // Map to View in DB
                    });
                }
            }
            ```
            
        3. **Query:** Now you can query `DbSet<ProductSummary>` like any other `DbSet` using LINQ.
            
            ```csharp
            // Get all data from the View
            var summaries = await context.ProductSummaries.ToListAsync();
            // Filter data from the View
            var cheapSummaries = await context.ProductSummaries
                                              .Where(s => s.Price < 50)
                                              .OrderBy(s => s.Name)
                                              .ToListAsync();
            foreach (var summary in cheapSummaries)
            {
                Console.WriteLine($"Product: {summary.Name}, Category: {summary.CategoryName}, Price: {summary.Price}");
            }
            ```
            
    - **Limitations of Keyless Entities:**
        - **No change tracking:** Because there is no primary key, EF Core cannot track changes to instances of keyless entities. You cannot update or delete them via `DbContext` like normal entities. They are mainly for reading data.
        - Cannot define relationships *from* a keyless entity to other entities (but you can have relationships *to* it from normal entities, though this is rare).
- Querying with Raw SQL - Part 1
    
    When you need to execute a raw SQL statement to retrieve **instances of a mapped entity type** (e.g., get `Product` objects using custom SQL), you can use `FromSqlRaw` or `FromSqlInterpolated`.
    
    - **`DbSet<TEntity>.FromSqlRaw(string sql, params object[] parameters)`**:
        - Executes raw SQL and maps the result to `TEntity` objects.
        - **Parameterization:** Use placeholders like `{0}`, `{1}` in the SQL string and pass parameter values in `parameters`. **This is crucial to avoid SQL Injection.**
        - **Example:**
            
            ```csharp
            int categoryId = 1;
            decimal minPrice = 50;
            // Get Products in categoryId and with price >= minPrice
            var products = await context.Products
                .FromSqlRaw("SELECT * FROM Products WHERE CategoryId = {0} AND Price >= {1}", categoryId, minPrice)
                .ToListAsync();
            ```
            
    - **`DbSet<TEntity>.FromSqlInterpolated(FormattableString sql)`**:
        - Similar to `FromSqlRaw` but uses C# **interpolated string** syntax (`$""`).
        - **Safer:** EF Core automatically converts interpolated variables to `DbParameter`, helping prevent SQL Injection. **This is the recommended way.**
        - **Example:**
            
            ```csharp
            int categoryId = 1;
            decimal minPrice = 50;
            // Same logic, safer syntax
            var products = await context.Products
                .FromSqlInterpolated($"SELECT * FROM Products WHERE CategoryId = {categoryId} AND Price >= {minPrice}")
                .ToListAsync();
            ```
            
    - **Important requirements:**
        - The SQL statement **must** return columns with names and data types matching the properties of the entity type `TEntity` you are querying (`Product` in the above example).
        - If the entity type has properties not returned by the SQL, EF Core may assign default values or throw errors depending on configuration.
    - **Change tracking:** Entities returned by `FromSql...` **will be tracked by the Change Tracker** (by default) if the entity type has a primary key. You can call `.AsNoTracking()` after `FromSql...` if you only want to read data.
- Querying with Raw SQL - Part 2
    
    A strength of `FromSqlRaw` and `FromSqlInterpolated` is that they return `IQueryable<TEntity>`. This means you can **compose** other LINQ operators *after* calling `FromSql...`.
    
    - **How it works:** EF Core treats your raw SQL as a data source (usually as a subquery or CTE in the final SQL) and applies LINQ operators (`Where`, `OrderBy`, `Include`, `Select`, `Skip`, `Take`, etc.) on top of that data source.
    - **Example:**
        
        ```csharp
        string nameFilter = "%gadget%";
        int categoryId = 1;
        var filteredSortedAndIncludedProducts = await context.Products
            .FromSqlInterpolated($"SELECT * FROM Products WHERE CategoryId = {categoryId}") // SQL as base data source
            .Where(p => EF.Functions.Like(p.Name, nameFilter)) // Additional LINQ Where
            .Include(p => p.Category) // Include (EF Core will handle the join)
            .OrderByDescending(p => p.Price) // OrderBy
            .Skip(5) // Paging
            .Take(10)
            .ToListAsync();
        ```
        
        In this example, EF Core will generate a more complex SQL statement, possibly with a subquery or CTE based on your original SQL, then apply additional `WHERE`, `JOIN` (for `Include`), `ORDER BY`, and `OFFSET FETCH` (for `Skip`/`Take`).
        
    - **Notes:**
        - Composing LINQ after `FromSql` is powerful but you should check the generated SQL (via logging) to ensure performance.
        - `Include` works, but your base SQL must return the necessary foreign key columns for EF Core to perform the join.
        - Some complex LINQ operators may not be fully compatible with `FromSql`.
- Querying scalar
    
    When you want to execute SQL statements that do not return entity data (such as custom `INSERT`, `UPDATE`, `DELETE`, or call a Stored Procedure that performs an action), or to retrieve a single scalar value.
    
    - **`DatabaseFacade.ExecuteSqlRawAsync(string sql, params object[] parameters)`**
    - **`DatabaseFacade.ExecuteSqlInterpolatedAsync(FormattableString sql)`**
        - Use these methods on `context.Database` to execute non-query SQL statements.
        - Always prefer `ExecuteSqlInterpolatedAsync` for safety (prevents SQL Injection).
        - Returns `int` as the number of rows affected by the SQL statement.
        - **Completely bypasses the Change Tracker.**
        - **Example:**
            
            ```csharp
            decimal priceIncrease = 1.05m;
            int categoryId = 2;
            // Increase price by 5% for products in category 2
            int affectedRows = await context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE Products SET Price = Price * {priceIncrease} WHERE CategoryId = {categoryId}");
            Console.WriteLine($"{affectedRows} product prices were updated.");
            // Call a Stored Procedure that does not return results (just performs an action)
            string userEmail = "test@example.com";
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"EXECUTE sp_DeactivateUser @Email={userEmail}");
            ```
            
    - **Querying scalar values:**
        - EF Core does not have a high-level, convenient method like `ExecuteScalarAsync` in ADO.NET.
        - **Method 1 (Simple):** If your SQL returns a single column, you *can* use `FromSql...` with a basic data type (e.g., `context.Set<int>().FromSqlRaw("SELECT COUNT(*) FROM Products").FirstOrDefaultAsync()`), but this is a bit of a hack and may not always work or be clear.
        - **Method 2 (Common):** Use ADO.NET directly via EF Core’s connection.
            
            ```csharp
            int productCount = 0;
            var connection = context.Database.GetDbConnection(); // Get current connection
            try
            {
                await connection.OpenAsync(); // Open connection (if not already open)
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT COUNT(*) FROM Products";
                    var result = await command.ExecuteScalarAsync(); // Execute and get scalar value
                    if (result != null && result != DBNull.Value)
                    {
                        productCount = Convert.ToInt32(result);
                    }
                }
            }
            finally
            {
                await connection.CloseAsync(); // Close connection (if you opened it)
            }
            Console.WriteLine($"Total products: {productCount}");
            ```
            
        - **Method 3:** Map a Stored Procedure or Function that returns a scalar value (see next section).
- Executing User-defined Functions
    
    EF Core allows you to map user-defined functions (UDFs) in the database to C# methods in your code (usually in `DbContext`).
    
    - **Scalar UDFs:**
        1. **Create a C# stub method:** Create a `static` (or instance) method in DbContext as a "stub"—it does not need an implementation, just the correct signature.
            
            ```csharp
            // In ApplicationDbContext.cs
            // Stub for IsProductPopular in DB (assume it returns bool)
            public static bool IsProductPopular(int productId)
            {
                // No implementation needed here, EF Core will translate it
                throw new NotSupportedException();
            }
            ```
            
        2. **Map in OnModelCreating:** Use `modelBuilder.HasDbFunction(...).HasName("DbFunctionName")`.
            
            ```csharp
            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                // ...
                // Get MethodInfo for the stub
                var methodInfo = typeof(ApplicationDbContext).GetMethod(nameof(IsProductPopular), new[] { typeof(int) });
            
                modelBuilder.HasDbFunction(methodInfo) // Map C# method
                            .HasName("udf_IsProductPopular"); // Name of the function in DB (optionally with schema)
                            // .HasSchema("dbo"); // (Optional) Specify schema
            }
            ```
            
        3. **Call in LINQ:** Call the C# stub directly in LINQ queries. EF Core will translate it to a function call in SQL.
            
            ```csharp
            var popularProducts = await context.Products
                .Where(p => ApplicationDbContext.IsProductPopular(p.Id)) // Call mapped function
                .ToListAsync();
            ```
            
    - **Table-Valued Functions (TVFs):**
        1. **Create a C# class for the result:** Like for a View, create a class (usually keyless entity) to hold the TVF result.
        2. **Map in OnModelCreating:** Use `modelBuilder.Entity<ResultType>().HasNoKey()` and map the stub method returning `IQueryable<ResultType>` with `HasDbFunction`.
        3. **Call in LINQ:** Call the stub in LINQ; the result can be used like a `DbSet` (e.g., `context.GetProductsByCategory(categoryId).Where(...)`).
    - **Note:** UDF mapping and translation may differ between database providers.
- Limitations of Raw Queries and EF Core
    
    Using raw SQL provides flexibility but also comes with limitations and trade-offs:
    
    - **Bypasses part or all of EF Core:**
        - `ExecuteSql...` completely bypasses the Change Tracker.
        - `FromSql...` returns tracked entities (if they have PK), but the original SQL logic is not deeply "understood" by EF Core like LINQ.
    - **Dependent on Database Schema:** Raw SQL can break if the database schema changes and your SQL is not updated accordingly. Migrations only manage schema for parts mapped by EF Core.
    - **Dependent on Database Provider:** SQL syntax may differ between SQL Server, PostgreSQL, MySQL, SQLite, etc., reducing application portability.
    - **No compile-time checking:** SQL syntax errors in strings are only caught at runtime.
    - **Security risk (SQL Injection):** **Always** use parameterization (`FromSqlInterpolated`, `ExecuteSqlInterpolatedAsync`, or `DbParameter` with `FromSqlRaw`/`ExecuteSqlRawAsync`) to avoid serious security vulnerabilities. **Never concatenate user input directly into SQL strings.**
    - **Less composable:** While you can compose LINQ after `FromSql...`, this can be complex and less efficient than writing the whole query in LINQ in some cases.
    
    **When should you consider using Raw SQL?** When the performance or feature benefits outweigh these limitations, and you are prepared to manage the associated risks.
    
- Section Review
    - EF Core allows executing raw SQL and working with Views/SPs/UDFs when needed.
    - Use `migrationBuilder.Sql()` to create/delete non-table database objects (Views, SPs, etc.) in migrations.
    - Map Views (or other keyless data sources) using **Keyless Entity Types** (`HasNoKey().ToView(...)`) and query them via `DbSet`.
    - Use `DbSet<TEntity>.FromSqlInterpolated()` (preferred) or `FromSqlRaw()` (with careful parameterization) to execute raw SQL returning mapped entities. You can compose LINQ after that.
    - Use `context.Database.ExecuteSqlInterpolatedAsync()` (preferred) or `ExecuteSqlRawAsync()` to execute non-query SQL (`UPDATE`, `DELETE`, `INSERT`, call SPs, etc.).
    - Map database UDFs to C# methods using `HasDbFunction()` and call them in LINQ.
    - Always be aware of the **limitations and risks** of raw SQL: bypassing change tracking, schema/provider dependency, no compile-time checking, SQL injection risk.
- Section Source Code
