# Using Entity Framework Core to Query a Database

- Section Overview
- Adding Verbose Logging to EF Core’s Workload
    - **Why Logging?**
        - **Understand EF Core:** See exactly what SQL statements EF Core generates from your LINQ queries.
        - **Debugging:** Identify slow queries, inefficient queries, or queries returning unexpected results.
        - **Optimization:** Analyze SQL to optimize (e.g., check if indexes are being used).
    - **How to Enable Logging (Example in ASP.NET Core with DI):**
        - The simplest way is to use `LogTo` when configuring `DbContextOptionsBuilder` in `Program.cs`.
        
        ```csharp
        // Program.cs
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
                   .LogTo(Console.WriteLine, LogLevel.Information) // Log to Console at Information level
                   // Or LogTo(message => System.Diagnostics.Debug.WriteLine(message)) // Log to Debug Output window
                   .EnableSensitiveDataLogging() // (Optional) Log parameter values - Only use in development!
        );
        ```
        
        - `LogLevel.Information` is usually enough to see generated SQL. Other levels (`Debug`, `Trace`) provide more details.
        - **Warning:** `EnableSensitiveDataLogging()` will log all parameter values in SQL, which may expose sensitive data. **Only enable this in development.**
    - **Result:** When the app runs and executes EF Core queries, you’ll see the corresponding SQL statements printed to the Console or Debug Output window.
- Fix: Database Connection String Refactor
    - This is not exactly a "fix" but a best practice for managing connection strings to avoid security issues and deployment headaches.
    - **Never hardcode:** Avoid writing connection strings directly in code (e.g., in `OnConfiguring`).
    - **Use Configuration Providers:**
        - **`appsettings.json`**: Store connection strings here (can also have `appsettings.Development.json`, `appsettings.Production.json` for different environments).
            
            ```json
            // appsettings.json
            {
              "ConnectionStrings": {
                "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MyEfCoreDb;Trusted_Connection=True;"
              }
              // ...
            }
            ```
            
        - **Environment Variables:** Override config from `appsettings.json`, useful for production/staging/docker.
        - **User Secrets (Development):** Use for sensitive info (like passwords in connection strings) during development to avoid committing them to source control. (Right-click project -> Manage User Secrets).
    - **Configure via Dependency Injection (DI):** Register `DbContext` and pass the connection string from configuration into `UseSqlServer` (or other provider) as shown in the Logging example. This is the standard approach in modern apps.
- LINQ as Entity Framework Core Syntax
    - **LINQ (Language-Integrated Query):** A set of .NET technologies allowing you to write data queries directly in C# (or VB.NET) code in a powerful and intuitive way, regardless of the data source (database, XML, collections, etc.).
    - **EF Core + LINQ:** When you write a LINQ query on a `DbSet<T>` or `IQueryable<T>` from EF Core, **EF Core translates** that LINQ expression into SQL (or the corresponding query language for your database) and executes it on the database server.
    - **Benefits:**
        - **Strongly-typed:** You work with C# objects and properties, checked at compile-time.
        - **Readable, easy to write:** Syntax is familiar to C# developers.
        - **Reusable:** Easy to package query logic into methods.
        - **Database Agnostic (to some extent):** The same LINQ query can run on different databases (SQL Server, PostgreSQL, etc.) if the provider supports translating those operators.
- Querying Basics
    - **`Accessing DbSet:`** Use the `DbSet<T>` properties you defined in your `DbContext`.
    - **Get all records:** Use `ToList()` or `ToListAsync()` to execute the query and retrieve all records from the corresponding table.
        
        ```csharp
        // Assume 'context' is an injected ApplicationDbContext instance
        List<Product> allProducts = context.Products.ToList(); // Synchronous
        // Or (recommended for web/UI)
        List<Product> allProductsAsync = await context.Products.ToListAsync(); // Asynchronous
        ```
        
    - **`IQueryable<T>`**: Note that `context.Products` returns an `IQueryable<Product>`. This is a *query expression*, not actual data. The query is only sent to the database when you call execution methods like `ToList()`, `FirstOrDefault()`, `Count()`, `foreach`, etc. (more on `IQueryable` later).
- Synchronous vs. Asynchronous Syntax
    - **Synchronous:** Methods like `ToList()`, `FirstOrDefault()`, `SaveChanges()`. When called, the current thread is **blocked** until the database operation completes.
        
        ```csharp
        var product = context.Products.FirstOrDefault(p => p.Id == 1); // Blocks thread until query completes
        ```
        
    - **Asynchronous:** Methods with the `Async` suffix like `ToListAsync()`, `FirstOrDefaultAsync()`, `SaveChangesAsync()`. Use with `async` and `await`. When called, the current thread is **not blocked**. It can do other work while waiting for the database. When the database returns, the rest of the method continues (usually on a different thread from the thread pool).
        
        ```csharp
        // In an async method
        var product = await context.Products.FirstOrDefaultAsync(p => p.Id == 1); // Does not block thread
        Console.WriteLine("Can do other work while waiting...");
        // After await completes, code continues here
        if (product != null) { ... }
        ```
        
    - **When to use Async?** **Always prefer Async** for I/O operations (like database queries, API calls, file read/write) in UI or web applications. This improves UI responsiveness and web server scalability by freeing up processing threads. In simple Console Apps, Sync is fine, but using Async is a good habit.
- Querying for a Single Record
    
    EF Core provides several methods to retrieve a single record:
    
    - **`FindAsync(keyValues)`** / **`FindAsync(keyValues, cancellationToken)`**:
        - Finds a record by **Primary Key**.
        - **Advantage:** It will **check the DbContext’s in-memory cache first**. If the entity with that key is already loaded and tracked, it returns immediately without querying the database.
        - Returns `null` if not found.
        - Always use Async if possible: `await context.Products.FindAsync(productId);`
    - **`FirstOrDefaultAsync(predicate)`**:
        - Returns the **first** record matching the filter (`predicate`).
        - Returns `null` if no record matches.
        - Safe when you’re not sure if any record matches.
        - Example: `await context.Products.FirstOrDefaultAsync(p => p.Name == "Laptop XYZ");`
    - **`SingleOrDefaultAsync(predicate)`**:
        - Returns the **single** record matching the filter.
        - Returns `null` if no record matches.
        - **Throws Exception** if **more than one** record matches.
        - Use when you expect only 0 or 1 matching result.
        - Example: `await context.Users.SingleOrDefaultAsync(u => u.Email == "unique.email@example.com");`
    - **`FirstAsync(predicate)`**:
        - Returns the **first** record matching the filter.
        - **Throws Exception** if **no** record matches.
        - Use when you are sure there is at least one matching result.
    - **`SingleAsync(predicate)`**:
        - Returns the **single** record matching the filter.
        - **Throws Exception** if **no** record matches OR **more than one** record matches.
        - Use when you expect *exactly one* matching result.
    
    **Choice:** `FirstOrDefaultAsync` and `FindAsync` are the most common and safest choices in many cases. Use `Single...` when your business logic requires uniqueness.
    
- Add Filters to Queries
    - The `Where()` method lets you filter data based on one or more conditions.
    - It takes a lambda expression (`predicate`) returning `bool`. Only records for which this expression returns `true` are kept.
    - `Where()` returns a new `IQueryable<T>`, allowing you to chain other LINQ methods.
        
        ```csharp
        // Get products with price greater than 100
        var expensiveProducts = await context.Products
                                            .Where(p => p.Price > 100)
                                            .ToListAsync();
        // Get products in CategoryId = 1 AND price below 50
        var cheapElectronics = await context.Products
                                             .Where(p => p.CategoryId == 1 && p.Price < 50)
                                             .ToListAsync();
        // You can call Where multiple times (equivalent to using &&)
        var specificProduct = await context.Products
                                            .Where(p => p.CategoryId == 2)
                                            .Where(p => p.IsAvailable == true)
                                            .FirstOrDefaultAsync();
        ```
        
    - **Important:** The condition in `Where()` will be translated by EF Core into a SQL `WHERE` clause, so filtering happens **on the database server**, which is very efficient.
- Additional Filtering Features
    
    Besides basic comparison (`==`, `!=`, `>`, `<`, `>=`, `<=`) and logic (`&&`, `||`) operators, you can use:
    
    - **`Contains()`**: Checks if a collection (e.g., List) contains a value (translated to `WHERE Id IN (...)`), or if a string contains a substring (translated to `WHERE Name LIKE '%substring%'`).
        
        ```csharp
        List<int> categoryIds = new List<int> { 1, 3, 5 };
        var productsInCategories = await context.Products
                                                 .Where(p => categoryIds.Contains(p.CategoryId))
                                                 .ToListAsync();
        string searchTerm = "book";
        var books = await context.Products
                                 .Where(p => p.Name.Contains(searchTerm)) // Note: Case-sensitivity depends on DB collation
                                 .ToListAsync();
        ```
        
    - **`StartsWith()`** / **`EndsWith()`**: Checks if a string starts/ends with a substring (translated to `LIKE 'prefix%'` or `LIKE '%suffix'`).
        
        ```csharp
        var productsStartingWithA = await context.Products
                                                  .Where(p => p.Name.StartsWith("A"))
                                                  .ToListAsync();
        ```
        
    - **`Check for null:`** Use `== null` or `!= null`.
        
        ```csharp
        var productsWithNoDescription = await context.Products
                                                     .Where(p => p.Description == null)
                                                     .ToListAsync();
        ```
        
    - **`EF.Functions.Like()`**: Provides a more explicit way to use SQL `LIKE` patterns, including wildcards (`%`, `_`).
        
        ```csharp
        // Find products with 'a' at the second position
        var productsLike = await context.Products
                                        .Where(p => EF.Functions.Like(p.Name, "_a%"))
                                        .ToListAsync();
        ```
        
- Alternative LINQ Syntax
    
    Besides method syntax (using extension methods like `.Where()`, `.Select()`), LINQ also has query syntax, which looks more like SQL.
    
    - **Method Syntax (more common):**
        
        ```csharp
        var expensiveProducts = await context.Products
                                            .Where(p => p.Price > 100 && p.IsAvailable)
                                            .OrderBy(p => p.Name)
                                            .Select(p => p.Name)
                                            .ToListAsync();
        ```
        
    - **Query Syntax:**
        
        ```csharp
        var expensiveProductsQuery = from p in context.Products
                                     where p.Price > 100 && p.IsAvailable
                                     orderby p.Name
                                     select p.Name; // Note: still IQueryable<string>
        var expensiveProducts = await expensiveProductsQuery.ToListAsync();
        ```
        
    - **Comparison:**
        - Both syntaxes are translated by EF Core into equivalent SQL.
        - Method Syntax is usually more flexible, easier to chain, and many LINQ operators are only available as methods (`Count()`, `FirstOrDefault()`, `ToList()`, etc.).
        - Query Syntax may be easier to read for those familiar with SQL, especially for complex `join` or `group by` operations.
        - You can mix both syntaxes.
    - **Recommendation:** Master Method Syntax as it is more widely used. Knowing Query Syntax is also helpful.
- Aggregate Methods
    
    Use to perform aggregate calculations on data sets at the database server.
    
    - **`CountAsync()`** / **`LongCountAsync()`**: Count the number of records (optionally with a condition).
        
        ```csharp
        int totalProducts = await context.Products.CountAsync();
        long availableProductsCount = await context.Products.LongCountAsync(p => p.IsAvailable);
        ```
        
    - **`SumAsync()`**: Calculate the sum of a numeric column.
        
        ```csharp
        decimal totalValue = await context.Products.SumAsync(p => p.Price);
        ```
        
    - **`AverageAsync()`**: Calculate the average of a numeric column.
        
        ```csharp
        decimal averagePrice = await context.Products.AverageAsync(p => p.Price);
        ```
        
    - **`MinAsync()`** / **`MaxAsync()`**: Find the minimum/maximum value of a column.
        
        ```csharp
        decimal cheapestPrice = await context.Products.MinAsync(p => p.Price);
        decimal mostExpensive = await context.Products.MaxAsync(p => p.Price);
        ```
        
    - **Efficiency:** These calculations are performed entirely on the database, returning only a single value, which is very efficient.
- Group By
    - The `GroupBy()` method lets you group records by one or more keys.
    - The result is an `IQueryable<IGrouping<TKey, TElement>>`, where `TKey` is the group key type, and `IGrouping` is a collection of elements in that group.
        
        ```csharp
        // Group products by CategoryId
        var productsByCategory = await context.Products
                                              .GroupBy(p => p.CategoryId)
                                              .ToListAsync(); // List<IGrouping<int, Product>>
        foreach (var group in productsByCategory)
        {
            Console.WriteLine($"Category ID: {group.Key}"); // Group key (CategoryId)
            int countInGroup = group.Count(); // Count products in this group
            Console.WriteLine($"  Product Count: {countInGroup}");
            // decimal avgPrice = group.Average(p => p.Price); // Average price in group
            // foreach (var productInGroup in group) { ... }
        }
        // Often combine GroupBy with Select to create summary results
        var categorySummary = await context.Products
                                         .GroupBy(p => p.CategoryId)
                                         .Select(g => new
                                         {
                                             CategoryId = g.Key,
                                             NumberOfProducts = g.Count(),
                                             AveragePrice = g.Average(p => p.Price)
                                         })
                                         .ToListAsync();
        foreach (var summary in categorySummary)
        {
            Console.WriteLine($"Category: {summary.CategoryId}, Count: {summary.NumberOfProducts}, Avg Price: {summary.AveragePrice}");
        }
        ```
        
    - `GroupBy` is translated into a SQL `GROUP BY` clause.
- Order By
    - Use `OrderBy()` (ascending) or `OrderByDescending()` (descending) to sort results by a property.
    - Use `ThenBy()` or `ThenByDescending()` to add secondary sort criteria (when primary values are equal).
        
        ```csharp
        // Sort products by name ascending
        var sortedByName = await context.Products
                                        .OrderBy(p => p.Name)
                                        .ToListAsync();
        // Sort by CategoryId ascending, then by price descending
        var sortedByCategoryThenPrice = await context.Products
                                                    .OrderBy(p => p.CategoryId)
                                                    .ThenByDescending(p => p.Price)
                                                    .ToListAsync();
        ```
        
    - Translated into SQL `ORDER BY`. **Important:** Always sort *before* paging (`Skip`/`Take`).
- Skip and Take
    - Used to implement **paging**, retrieving only a subset of data instead of all.
    - **`Skip(n)`**: Skip the first `n` records.
    - **`Take(m)`**: Take the next `m` records.
        
        ```csharp
        int pageNumber = 2; // Page 2
        int pageSize = 10; // 10 items per page
        var productsPage2 = await context.Products
                                         .OrderBy(p => p.Id) // **MUST OrderBy before Skip/Take**
                                         .Skip((pageNumber - 1) * pageSize) // Skip (2-1)*10 = 10 records
                                         .Take(pageSize) // Take 10 records
                                         .ToListAsync();
        // Get total record count to calculate total pages (usually in a separate query)
        int totalCount = await context.Products.CountAsync();
        int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        ```
        
    - **Why OrderBy first?** SQL does not guarantee result order without `ORDER BY`. To ensure `Skip`/`Take` works consistently, always specify order.
    - `Skip`/`Take` are translated into the appropriate SQL constructs (e.g., `OFFSET FETCH` in SQL Server 2012+, `LIMIT OFFSET` in PostgreSQL/MySQL/SQLite). Paging happens on the database.
- Projections and Custom Data Types
    - **Problem:** By default, when you query `context.Products.ToListAsync()`, EF Core retrieves **all columns** from the `Products` table. This may be inefficient if you only need a few columns.
    - **`Solution: Select() (Projection):`** Lets you specify **exactly what data to retrieve**. You can:
        - Select specific columns.
        - Create new objects (anonymous types or DTOs - Data Transfer Objects) with the desired structure.
    - **Benefits:**
        - **Reduces data transfer:** Only fetch what you need, speeding up queries.
        - **Reduces database server load:** Database only reads and sends less data.
        - **Shapes data:** Create structures suitable for ViewModels, API responses, etc.
    - **Example:**
        
        ```csharp
        // 1. Only get product name and price (anonymous type)
        var productNamesAndPrices = await context.Products
                                                 .Select(p => new { p.Name, p.Price }) // Anonymous type
                                                 .ToListAsync();
        foreach (var item in productNamesAndPrices)
        {
            Console.WriteLine($"Name: {item.Name}, Price: {item.Price}");
            // item.Id is not accessible because it was not selected
        }
        // 2. Create a specific DTO (Data Transfer Object)
        public class ProductSummaryDto
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; }
            public string CategoryName { get; set; } // From related table
        }
        var productSummaries = await context.Products
                                          .Include(p => p.Category) // Need Include to get Category if using navigation property
                                          .Select(p => new ProductSummaryDto
                                          {
                                              ProductId = p.Id,
                                              ProductName = p.Name,
                                              CategoryName = p.Category.Name // Access navigation property
                                          })
                                          .ToListAsync();
        // 3. Only get a single column
        List<string> productNames = await context.Products
                                                .Select(p => p.Name)
                                                .ToListAsync();
        ```
        
    - `Select` is translated into specifying columns in the SQL `SELECT` clause.
- Tracking Vs. No Tracking (Enhancing Performance)
    - **Change Tracking (Default):** When you query data (e.g., `context.Products.ToList()`), EF Core:
        1. Creates entity objects.
        2. Stores a "snapshot" of these entities in the `DbContext`’s memory.
        3. Tracks any changes you make to their properties.
        4. When you call `SaveChangesAsync()`, EF Core compares the current state to the snapshot to know what `UPDATE` statements to generate.
    - **Overhead:** Storing snapshots and comparing states uses memory and CPU, especially when loading lots of data.
    - **`AsNoTracking()`**: If you only **read data** and **do not intend to update** it in the same `DbContext` instance, use `AsNoTracking()`.
        
        ```csharp
        // Only read data, no need to track changes
        var readOnlyProducts = await context.Products
                                            .Where(p => p.IsAvailable)
                                            .AsNoTracking() // **Add this line**
                                            .ToListAsync();
        // If you try to modify and SaveChanges() on readOnlyProducts,
        // EF Core will not know to generate an UPDATE (unless you re-attach them)
        ```
        
    - **Benefits of AsNoTracking():**
        - **Faster:** Query executes faster because it skips snapshot creation and tracking.
        - **Less memory:** No snapshot stored in `DbContext`.
    - **When to use AsNoTracking()?** In almost all **read-only** scenarios (displaying lists, reports, API GETs that don’t need updates, etc.).
- IQueryables vs List Types
    
    This is a **crucial concept** for understanding how EF Core works and writing efficient queries.
    
    - **`IQueryable<T>`**: Represents a **query that has not yet been executed**. It contains an **expression tree** describing what to do (where to get data, how to filter, how to sort, etc.).
        
        ```csharp
        IQueryable<Product> query = context.Products.Where(p => p.Price > 50);
        // AT THIS POINT: No query has been sent to the database!
        // 'query' is just an object describing "I want Products with Price > 50"
        ```
        
    - **Deferred Execution:** The query is only translated to SQL and sent to the database when you "materialize" the `IQueryable` by:
        - Calling execution methods: `ToList()`, `ToArray()`, `FirstOrDefault()`, `Count()`, `Sum()`, `Max()`, etc. (and their Async versions).
        - Using a `foreach` loop on the `IQueryable`.
    - **`IEnumerable<T>`** / **`List<T>`**: Represents a **collection of data already loaded into application memory (in-memory collection)**.
    - **Potential Issue (Client-Side Evaluation):** If you apply LINQ methods *after* the data has been loaded into memory (e.g., call `ToList()` too early then use `Where()`), filtering/sorting/calculations happen **on the client (your app)** instead of the database server. This is **very inefficient**.
        
        ```csharp
        // **WRONG - INEFFICIENT**
        var allProductsInMemory = await context.Products.ToListAsync(); // Loads ALL products into client
        // Filtering happens in app memory, not using DB indexes, wastes RAM/CPU
        var expensiveProductsClientSide = allProductsInMemory.Where(p => p.Price > 1000);
        // **RIGHT - EFFICIENT**
        var expensiveProductsServerSide = await context.Products
                                                      .Where(p => p.Price > 1000) // Where() on IQueryable -> translated to SQL WHERE
                                                      .ToListAsync(); // Only loads filtered products into client
        ```
        
    - **Lesson:** **`*Always build your entire query (Where, OrderBy, Select, Skip, Take, etc.) on IQueryable<T> before calling execution methods like ToListAsync()*`**. The more work (filtering, sorting, grouping, aggregation) is done on the database, the better.
- Efficient Querying Tips and Tricks
    
    Key takeaways:
    
    1. **`Use AsNoTracking() for Read-Only Queries:`** Reduces overhead when only reading data.
    2. **`Project Only Necessary Data (Select)`**: Only fetch the columns you actually need, avoid loading entire entities if not necessary. Use DTOs.
    3. **Filter and Order on the Server:** Apply `Where()`, `OrderBy()`, `Skip()`, `Take()` on `IQueryable` *before* calling `ToListAsync()` or other execution methods.
    4. **`Use Asynchronous Methods (...Async)`**: Avoid blocking threads in web/UI apps.
    5. **Beware of N+1 Problem:** When loading a list of main entities (e.g., `Categories`) and then, in a loop, accessing related collections for each entity (e.g., `category.Products`), EF Core may generate 1 initial query + N additional queries (one for each main entity) if lazy loading is enabled.
        - **Solution:** Use **Eager Loading** with `Include()` and `ThenInclude()` to have EF Core load related data in one (or fewer) queries.
            
            ```csharp
            // N+1 Problem (potential):
            // var categories = await context.Categories.ToListAsync();
            // foreach (var cat in categories) {
            //    Console.WriteLine(cat.Name);
            //    // The line below may cause a separate query for each category if lazy loading is enabled
            //    foreach (var prod in cat.Products) { Console.WriteLine(prod.Name); }
            // }
            // Eager Loading solution:
            var categoriesWithProducts = await context.Categories
                                                      .Include(c => c.Products) // Load related Products
                                                      // .ThenInclude(p => p.Supplier) // Can include deeper
                                                      .AsNoTracking() // Often used with Include for performance
                                                      .ToListAsync();
            // Now accessing cat.Products will not cause extra queries
            ```
            
        - Other techniques: Explicit Loading, Lazy Loading (use with care), Split Queries (EF Core 5+).
    6. **Avoid Complex Logic in LINQ that Can't Be Translated:** Some complex C# methods or business logic in `Where()` or `Select()` may not be translatable by EF Core to SQL. It may throw an Exception or, worse, silently perform client-side evaluation. Keep LINQ expressions relatively simple and check the generated SQL (via logging).
    7. **Use Database Indexes:** Ensure columns frequently used in `Where()` and `OrderBy()` are indexed in the database. EF Core Migrations can help create indexes (`.HasIndex()`).
    8. **Check Generated SQL:** Use logging to see the actual SQL generated, ensuring it is efficient and correct.
    9. **Consider Raw SQL Queries or Stored Procedures:** For extremely complex or highly optimized queries that LINQ cannot express, EF Core allows you to execute raw SQL (`FromSqlRaw`, `ExecuteSqlRawAsync`) or call Stored Procedures.
- Section Review
    - Querying data is essential when working with EF Core. By understanding LINQ, query methods, the difference between `IQueryable` and `IEnumerable`, and optimization techniques like `AsNoTracking` and `Select`, you can write efficient, maintainable code and ensure good performance for your application.
    - Take time to practice these techniques with your own project. If you have any questions, don’t hesitate to ask!
- Section Source Code
