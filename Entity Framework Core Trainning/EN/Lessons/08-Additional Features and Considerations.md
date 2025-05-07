# Additional Features and Considerations

**Additional Features and Considerations**

- **Section Overview**
    - In this section, we will explore advanced features and important considerations when working with Entity Framework Core to build better applications.
    - Main topics include:
        - Processing data before saving changes
        - Working with SQL Server Temporal Tables
        - Validating data with Data Annotations
        - Configuring models with Pre-convention
        - Other advanced features of EF Core
    - The objectives of this section are to help you:
        - Understand and apply advanced features of EF Core
        - Automate data processing operations
        - Manage data change history
        - Ensure data integrity
        - Implement advanced data validation and configuration

- Manipulate Entries Before Saving Changes
    
    Sometimes, you want to automatically perform certain actions right before changes are saved to the database. A common example is automatically updating `CreatedAt` and `UpdatedAt` columns.
    
    - **`Method 1: Using the SavingChanges Event`**
        - `DbContext` provides the `SavingChanges` (and `SavedChanges` - after saving) events that you can subscribe to for custom logic.
        - Inside the event handler, you can access `DbContext.ChangeTracker.Entries()` to loop through entities about to be saved, check their state (`Added`, `Modified`), and update the necessary properties.
        - **Example:** Automatically set `CreatedAt` and `UpdatedAt`.
            
            ```csharp
            // In ApplicationDbContext.cs
            public override int SaveChanges(bool acceptAllChangesOnSuccess)
            {
                OnBeforeSaving(); // Call pre-save handler
                return base.SaveChanges(acceptAllChangesOnSuccess);
            }
            
            public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
            {
                OnBeforeSaving(); // Call pre-save handler
                return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            }
            
            private void OnBeforeSaving()
            {
                var entries = ChangeTracker.Entries()
                    .Where(e => e.Entity is IAuditableEntity &&
                                (e.State == EntityState.Added || e.State == EntityState.Modified));
            
                var now = DateTime.UtcNow; // Or DateTime.Now as needed
            
                foreach (var entry in entries)
                {
                    var entity = (IAuditableEntity)entry.Entity;
            
                    if (entry.State == EntityState.Added)
                    {
                        entity.CreatedAt = now;
                    }
            
                    entity.UpdatedAt = now; // Always update UpdatedAt on Add or Modify
                }
            }
            
            // Example interface
            public interface IAuditableEntity
            {
                DateTime CreatedAt { get; set; }
                DateTime UpdatedAt { get; set; }
            }
            ```
            
    - **Method 2: Using Interceptors (More Advanced)**
        - EF Core provides an Interceptor mechanism (e.g., `ISaveChangesInterceptor`, `IDbCommandInterceptor`...) that allows you to "intercept" various stages of EF Core operations in a more powerful and clean way than events.
        - For example, `ISaveChangesInterceptor` has methods like `SavingChangesAsync`, `SavedChangesAsync` that you can implement to perform similar logic as above.
        - Interceptors are usually registered when configuring `DbContextOptions`. This approach is more flexible but a bit more complex than the `SavingChanges` event.
- SQL Server Temporal Tables
    - **Concept:** Temporal Tables are a SQL Server feature (since 2016) that allows the database to **automatically track and store the history of changes** to data in a table. Whenever a row is updated or deleted, the old version of that row is stored in a separate history table.
    - **Support in EF Core:** EF Core (since 5.0) supports mapping entities to these temporal tables.
    - **Configuration:** Use Fluent API in `OnModelCreating`.
        
        ```csharp
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure Employee as a Temporal Table
            modelBuilder.Entity<Employee>()
                .ToTable("Employees", b => b.IsTemporal(t =>
                {
                    // (Optional) Configure history table name and period columns
                    // t.HasHistoryTable("EmployeeHistory");
                    // t.UseHistoryTable("EmployeeHistory", "historySchema");
                    // t.HasPeriodStart("ValidFrom");
                    // t.HasPeriodEnd("ValidTo");
                }));
        }
        ```
        
        When you create a migration and update the database, EF Core will create the `Employees` table with `SYSTEM_VERSIONING = ON` and the corresponding history table (e.g., `EmployeeHistory`).
        
    - **Querying history:** EF Core provides special LINQ methods to query data at a specific point in the past or within a time range.
        
        ```csharp
        var employeeId = 1;
        var specificTime = new DateTime(2024, 1, 15, 10, 0, 0);
        // Get the state of Employee at specificTime
        var employeeAsOf = await context.Employees
            .TemporalAsOf(specificTime)
            .FirstOrDefaultAsync(e => e.Id == employeeId);
        // Get all historical versions of Employee
        var employeeHistory = await context.Employees
            .TemporalAll()
            .Where(e => e.Id == employeeId)
            .OrderBy(e => EF.Property<DateTime>(e, "ValidFrom"))
            .ToListAsync();
        // Get versions within a time range
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 2, 1);
        var employeeBetween = await context.Employees
            .TemporalBetween(startDate, endDate)
            .Where(e => e.Id == employeeId)
            .ToListAsync();
        ```
        
    - **Benefits:** Automates data history tracking at the database level, useful for auditing or data recovery.
- Data Validation with Data Annotations
    
    Ensuring data validity before saving to the database is very important. Data Annotations are a way to declare validation rules directly on your model.
    
    - **How to use:** Add attributes from the `System.ComponentModel.DataAnnotations` namespace to your entity properties.
        
        ```csharp
        using System.ComponentModel.DataAnnotations;
        using System.ComponentModel.DataAnnotations.Schema;
        
        public class Product
        {
            public int Id { get; set; }
        
            [Required(ErrorMessage = "Product name is required.")]
            [StringLength(100, MinimumLength = 3, ErrorMessage = "Product name must be between 3 and 100 characters.")]
            public string Name { get; set; }
        
            [Range(0.01, 10000.00, ErrorMessage = "Price must be between 0.01 and 10000.")]
            [Column(TypeName = "decimal(18,2)")]
            public decimal Price { get; set; }
        
            [EmailAddress(ErrorMessage = "Supplier email is invalid.")]
            public string SupplierEmail { get; set; }
        
            // ... other properties ...
        }
        ```
        
    - **Integration with EF Core:**
        - **Schema Generation:** EF Core uses some data annotations to generate the database schema. For example: `[Required]` usually makes the corresponding column `NOT NULL`, `[MaxLength]` or `[StringLength]` affects column length (`nvarchar(100)`), `[Column(TypeName = "...")]` specifies a specific data type.
        - **`Validation on SaveChanges:`** EF Core *may* perform validation based on some annotations (like `[Required]`, `[MaxLength]`) before sending commands to the database. If violated, it will throw a `DbUpdateException`.
        - **Validation in the application:** In apps like ASP.NET Core MVC/API, Data Annotation validation usually happens earlier, during model binding. If the model is invalid (`ModelState.IsValid` is `false`), you typically won't call `SaveChanges()`. However, having these annotations is still useful for both schema generation and validation at different layers.
- Pre-convention model configuration
    
    EF Core has many implicit conventions to infer your model structure from code. Sometimes, you want to change or add to these conventions globally instead of configuring each entity individually with Fluent API.
    
    - **`Method 1: Loop through Entity Types in OnModelCreating (Older Way)`**
    You can get a list of entity types and apply common configuration.
        
        ```csharp
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        
            // Example: Set default max length for all string columns to 256
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(string) && property.GetMaxLength() == null)
                    {
                        property.SetMaxLength(256);
                    }
                }
            }
        
            // Example: Apply snake_case naming convention to all tables
            // foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            // {
            //     entityType.SetTableName(ToSnakeCase(entityType.GetTableName()));
            // }
        }
        // Custom ToSnakeCase function
        ```
        
    - **`Method 2: Use ConfigureConventions (EF Core 5+ - Recommended)`**
    This approach provides a clearer, more structured API for customizing conventions. Override the `ConfigureConventions` method in `DbContext`.
        
        ```csharp
        // In ApplicationDbContext.cs
        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            // Example: Set default max length for all string properties to 250
            configurationBuilder
                .Properties<string>()
                .HaveMaxLength(250);
        
            // Example: All DateTime properties map to 'datetime2' in SQL Server
            configurationBuilder
                .Properties<DateTime>()
                .HaveColumnType("datetime2");
        
            // Example: Apply Value Converter for all properties of MyCustomType
            // configurationBuilder
            //     .Properties<MyCustomType>()
            //     .HaveConversion<MyCustomTypeValueConverter>();
        }
        ```
        
        This approach is more explicit and less error-prone than manually looping in `OnModelCreating`.
        
- Support For Database Transactions
    - **`Default Transaction in SaveChanges():`** As mentioned, each call to `SaveChanges()` or `SaveChangesAsync()` automatically executes all `INSERT`, `UPDATE`, `DELETE` commands inside a single transaction. If there is an error, the transaction is rolled back.
    - **Explicit Transactions:** When do you need them?
        - When you want to group **multiple SaveChanges() calls** into a single atomic unit of work.
        - When you want to combine EF Core operations with other database operations (e.g., ADO.NET, Dapper) in the same transaction.
    - **How to use:**
        1. Start a transaction with `context.Database.BeginTransactionAsync()` (or `BeginTransaction()`).
        2. Perform EF Core operations (`Add`, `Update`, `Remove`, `SaveChanges`) and/or other database operations.
        3. If all succeed, call `transaction.CommitAsync()` (or `Commit()`).
        4. If an error occurs, call `transaction.RollbackAsync()` (or `Rollback()`) in the `catch` block.
        5. Use a `using` block for the transaction to ensure it is disposed properly.
        
        ```csharp
        using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // Operation 1: Add Product
            var newProduct = new Product { Name = "Transactional Product", Price = 100, CategoryId = 1 };
            context.Products.Add(newProduct);
            await context.SaveChangesAsync(); // Save change 1
        
            // Operation 2: Update Category (example)
            var category = await context.Categories.FindAsync(1);
            if (category != null)
            {
                category.Name += " (Updated)";
                // No need to SaveChanges immediately if you want to group in the transaction
            }
            await context.SaveChangesAsync(); // Save change 2
        
            // (You can perform additional ADO.NET operations using the same connection here)
            // var connection = context.Database.GetDbConnection();
            // command.Connection = connection;
            // command.Transaction = (System.Data.Common.DbTransaction)transaction.GetDbTransaction();
            // await command.ExecuteNonQueryAsync();
        
            // If all is well, commit the transaction
            await transaction.CommitAsync();
            Console.WriteLine("Transaction committed successfully.");
        }
        catch (Exception ex)
        {
            // On error, rollback the transaction
            Console.WriteLine($"Error occurred: {ex.Message}. Rolling back transaction.");
            await transaction.RollbackAsync();
            // Handle error (log, notify, etc.)
        }
        ```
        
- Handling Data Concurrency Issues
    - **Problem:** When multiple users simultaneously read and try to update the same data record. The last user to save will overwrite the previous user's changes without knowing ("Last in wins"). This can lead to data loss or inconsistency.
    - **Common solution: Optimistic Concurrency Control**
        - Assumes conflicts are rare.
        - Does not lock data when reading.
        - **Detects** conflicts at save time (`SaveChanges()`). If a conflict is detected, saving is not allowed and an error is thrown.
    - **How to detect conflicts in EF Core:**
        1. **Use Timestamp / RowVersion (Recommended):**
            - Add a `byte[]` property to the entity and mark it with the `[Timestamp]` attribute or configure with Fluent API `.IsRowVersion()`.
                
                ```csharp
                public class Product
                {
                    // ... other properties ...
        
                    [Timestamp] // Mark as rowversion column
                    public byte[] RowVersion { get; set; }
                }
                ```
                
            - When you create a migration, EF Core will create a `rowversion` column (SQL Server) or equivalent. The database will automatically update this value whenever the row is modified.
            - When `SaveChanges()` performs `UPDATE` or `DELETE`, EF Core adds a `WHERE RowVersion = [OriginalRowVersionValue]` condition to the SQL statement.
            - If the row has been modified by someone else (the `RowVersion` value in the DB has changed), the `UPDATE`/`DELETE` will affect no rows (`0 rows affected`). EF Core detects this and throws a `DbUpdateConcurrencyException`.
        2. **Use Concurrency Token:**
            - Mark one or more regular properties with the `[ConcurrencyCheck]` attribute or configure with Fluent API `.IsConcurrencyToken()`.
            - When `SaveChanges()`, EF Core adds a `WHERE [CheckedProperty] = [OriginalValue]` condition for *all* marked properties to the `UPDATE`/`DELETE` statement.
            - If any original value does not match, a `DbUpdateConcurrencyException` is thrown.
            - Less efficient and more complex than `RowVersion`, usually only used if you cannot add a `rowversion` column.
    - **`Handling DbUpdateConcurrencyException:`**
        - Wrap the `SaveChangesAsync()` call in a `try...catch` block.
        - Catch `DbUpdateConcurrencyException`.
        - Inside the `catch`, decide how to handle the conflict:
            - **Notify the user:** Most common. Inform that the data has been changed by someone else and ask them to reload the latest data and try again.
            - **Client Wins:** Overwrite the current database data with the client's data (be careful, may lose others' changes).
            - **Database Wins:** Discard the client's changes and reload the latest data from the database.
            - **Merge:** Try to merge changes (very complex, usually not feasible).
        
        ```csharp
        try
        {
            // Assume productToUpdate has been loaded from DB and modified
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            Console.WriteLine("Concurrency conflict detected!");
            // Get the entry that caused the error
            var entry = ex.Entries.Single();
            // Get the current value in the database
            var databaseValues = await entry.GetDatabaseValuesAsync();
        
            if (databaseValues == null)
            {
                Console.WriteLine("The entity was deleted by another user.");
                // Handle deletion case
            }
            else
            {
                // Get the value the client is trying to save
                var clientValues = entry.CurrentValues;
                // Get the original value when the client read it
                var originalValues = entry.OriginalValues;
        
                // TODO: Implement conflict resolution strategy
                // Example: Notify user and ask to reload
                Console.WriteLine("Data has been changed by another user. Please reload and try again.");
        
                // Example: Database Wins (reload value from DB)
                // await entry.ReloadAsync();
        
                // Example: Client Wins (must get new rowversion from DB before retrying save)
                // var databaseEntity = (Product)databaseValues.ToObject();
                // entry.OriginalValues.SetValues(databaseValues); // Treat DB value as new original
                // entry.CurrentValues[nameof(Product.RowVersion)] = databaseEntity.RowVersion; // Update RowVersion
                // await context.SaveChangesAsync(); // Retry save (overwrite)
            }
        }
        ```
        
- Using Query Filters
    - **Concept:** Global Query Filters are LINQ `Where` filters that are **automatically applied** to **all** LINQ queries for a specific entity type in that `DbContext`.
    - **Use Cases:**
        - **Soft Deletes:** Always exclude records marked as deleted (e.g., `IsDeleted = true`) from all normal queries.
        - **Multi-tenancy:** Automatically filter data based on the current user's `TenantId`, ensuring users only see their own data.
    - **Configuration:** Use `HasQueryFilter()` in `OnModelCreating`.
        
        ```csharp
        public class Product
        {
            // ... other properties ...
            public bool IsDeleted { get; set; } // Soft delete column
            // public Guid TenantId { get; set; } // For multi-tenancy
        }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Apply Soft Delete filter to Product
            modelBuilder.Entity<Product>()
                .HasQueryFilter(p => !p.IsDeleted); // Only get non-deleted Products
        
            // Example Multi-tenancy filter (assume TenantId property in DbContext)
            // modelBuilder.Entity<Product>()
            //     .HasQueryFilter(p => p.TenantId == this.TenantId);
        }
        ```
        
    - **Usage:** After configuration, every `context.Products...` query will automatically include `WHERE IsDeleted = 0` (or equivalent) in the generated SQL.
        
        ```csharp
        // This query will automatically only return Products with IsDeleted = false
        var activeProducts = await context.Products.ToListAsync();
        ```
        
    - **Bypass the Filter:** If you need to query *all* records (including those filtered by the Global Filter, e.g., to view soft-deleted records), use `IgnoreQueryFilters()`.
        
        ```csharp
        // Get all Products, including soft-deleted ones
        var allProductsIncludingDeleted = await context.Products
                                                       .IgnoreQueryFilters()
                                                       .ToListAsync();
        ```
        
    - **Benefits:** Helps keep query code clean, avoids repeating the same `Where` condition everywhere, and reduces the risk of forgetting to apply important filters (like soft delete or multi-tenancy).
- Database Connection Retry and Timeout Policies
    
    In real-world applications, especially cloud apps, network connections to the database can experience transient errors. EF Core provides **Connection Resiliency** to automatically retry failed operations due to such temporary issues.
    
    - **Enable Retry:** Use the `EnableRetryOnFailure()` method when configuring the database provider in `DbContextOptionsBuilder`.
        
        ```csharp
        // Program.cs or where you configure DbContext
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, sqlServerOptionsAction: sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5, // Maximum retry attempts
                    maxDelay: TimeSpan.FromSeconds(30), // Max delay between retries
                    errorNumbersToAdd: null // (Optional) Specific SQL Server error numbers to retry
                );
                // sqlOptions.CommandTimeout(60); // (Optional) Set command timeout to 60 seconds
            })
        );
        ```
        
        - `EnableRetryOnFailure()` will automatically retry database commands (query, save changes) if they fail due to errors identified as transient (e.g., network errors, deadlocks, etc.).
        - You can customize the number of retries, max delay, and specific error codes.
    - **Command Timeout:** You can also configure the maximum time EF Core waits for a SQL command to complete before throwing an exception using `CommandTimeout(seconds)`. The default is usually 30 seconds. Increase this if you have long-running queries or `SaveChanges` operations.
    
    Using `EnableRetryOnFailure` makes your application more resilient to temporary database connectivity issues.
    
- Section Source Code
    - You can hook into the save process using the `SavingChanges` event or Interceptors to perform custom actions (e.g., update timestamps).
    - EF Core supports mapping and querying **SQL Server Temporal Tables** for automatic data history tracking.
    - **Data Annotations** help define validation rules on models, used by EF Core for schema generation and can be used for validation at other layers.
    - Customize global **Conventions** with `ConfigureConventions` (EF Core 5+) for more consistent model configuration.
    - Use **Explicit Transactions** (`BeginTransactionAsync`, `CommitAsync`, `RollbackAsync`) when you need to group multiple operations or combine EF Core with ADO.NET.
    - Handle **Concurrency Conflicts** with Optimistic Concurrency (prefer `[Timestamp]`/`RowVersion`) and catch `DbUpdateConcurrencyException`.
    - **Global Query Filters** (`HasQueryFilter`) automatically apply filters to queries, useful for soft delete and multi-tenancy.
    - Improve application stability with **Connection Resiliency** (`EnableRetryOnFailure`) to automatically retry on transient connection errors.
    
    These features add powerful tools to your EF Core skillset, helping you build more complex, efficient, and reliable .NET applications. Congratulations on completing the EF Core training series! If you have any questions in real-world work, don't hesitate to reach out.
