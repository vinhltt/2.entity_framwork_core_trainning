# Getting Started

**Getting Started with Entity Framework Core**

- **Section Overview**
    - In this section, we will learn the basic knowledge needed to start working with Entity Framework Core, a powerful and popular tool in the .NET ecosystem for working with databases.
    - Main topics include:
        - Understanding Data Models and how to create them with EF Core
        - Understanding Database Context and its role
        - Configuring database connections and choosing appropriate providers
        - Getting familiar with Code First Development and Migrations
        - Setting up a Console App project to practice EF Core
    - The objectives of this section are to help you:
        - Grasp basic concepts about Data Models and Database Context
        - Learn how to configure and connect to databases with EF Core
        - Understand the Code First Development process
        - Create and manage Migrations
        - Set up a development environment with EF Core

This is an extremely powerful and popular tool in the .NET ecosystem for working with databases.

- What are Data Models?
    - **Concept:** In the context of EF Core (and programming in general), Data Models are **C# classes** that you create to **represent data structures** that you want to store or retrieve from a database.
    - **Example:** If you have a `Products` table in your database with columns like `Id`, `Name`, `Price`, you would create a `Product` class in C# with corresponding properties: `Id`, `Name`, `Price`.
        
        ```csharp
        public class Product
        {
            public int Id { get; set; } // Usually the Primary Key
            public string Name { get; set; }
            public decimal Price { get; set; }
            // You might have properties for relationships, for example:
            // public int CategoryId { get; set; }
            // public virtual Category Category { get; set; }
        }
        ```
        
- Creating the Data Models with EF Core
    - **How to do it:** Simply create C# classes as in the example above (often called POCOs - Plain Old CLR Objects).
    - **Conventions:** EF Core has many implicit conventions to automatically understand your model:
        - Properties named `Id` or `[ClassName]Id` (e.g., `ProductId`) are typically automatically identified as primary keys.
        - Properties with basic data types (int, string, decimal, bool, DateTime, etc.) will be mapped to the corresponding data types in the database.
        - Properties that are other classes (e.g., `Category` in `Product`) represent relationships.
    - **Advanced configuration (Data Annotations & Fluent API):** When conventions are not enough or you want more detailed customization (maximum string length, different table/column names than class/property names, indexing, complex relationships, etc.), you can use:
        - **Data Annotations:** Attributes placed directly on classes or properties (e.g., `[Key]`, `[Required]`, `[MaxLength(100)]`, `[Table("DifferentTableName")]`). Easy to use and read.
        - **Fluent API:** Configuration in the `OnModelCreating` method of `DbContext` (will be discussed later). More powerful and flexible than Data Annotations, helps separate configuration from the model.
- Understanding the Database Context
    - **Concept:** `DbContext` is the **heart** of EF Core. It's a C# class that inherits from `Microsoft.EntityFrameworkCore.DbContext`.
    - **Key roles:**
        - **Bridge:** Acts as a bridge between your model classes (Entities) and the actual database.
        - **Session/Unit of Work:** Manages a session with the database. It includes connection information and model configuration.
        - **Querying:** Allows you to write LINQ (Language Integrated Query) queries on `DbSet` objects to retrieve data. EF Core will translate this LINQ into corresponding SQL.
        - **Change Tracking:** Automatically tracks the state of entities that you retrieve or add to the context. When you call `SaveChanges()`, it knows which objects have been modified, added, or deleted to generate appropriate SQL statements (INSERT, UPDATE, DELETE).
        - **Transaction Management:** By default, `SaveChanges()` executes all changes within a single transaction, ensuring data integrity.
- Adding a Database Context
    - **How to do it:**
        1. Create a new class that inherits from `DbContext`. For example: `ApplicationDbContext`.
        2. Add properties of type `DbSet<T>` for each entity (model) you want to manage through this context. `DbSet<T>` represents a collection of entities (equivalent to a table in the database).
        3. Create a constructor that accepts `DbContextOptions<YourContextName>` and passes it to the base `DbContext` constructor. This allows configuring the context from outside (e.g., connection string, database provider).
        4. (Optional but common) Override the `OnModelCreating(ModelBuilder modelBuilder)` method to configure the model using Fluent API if needed.
    - **Example:**
        
        ```csharp
        using Microsoft.EntityFrameworkCore;
        
        public class ApplicationDbContext : DbContext
        {
            // Constructor to receive configuration from outside
            public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
                : base(options)
            {
            }
        
            // DbSet for each entity you want to manage
            public DbSet<Product> Products { get; set; }
            public DbSet<Category> Categories { get; set; } // Example adding Category
        
            // (Optional) Configuration using Fluent API
            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder); // Should call the base method
        
                // Example Fluent API configuration
                modelBuilder.Entity<Product>()
                    .Property(p => p.Name)
                    .HasMaxLength(200)
                    .IsRequired(); // Set max length and required for Name
        
                modelBuilder.Entity<Category>().HasData( // Example Seeding Data (will be discussed later)
                    new Category { Id = 1, Name = "Electronics" },
                    new Category { Id = 2, Name = "Books" }
                );
            }
        }
        ```
        
- EF Core and Database Support
    - **Database Providers:** EF Core itself doesn't communicate directly with any specific database type. It needs a specific **Database Provider** for each database management system (DBMS) you want to work with.
    - **Popular Providers:**
        - **SQL Server:** `Microsoft.EntityFrameworkCore.SqlServer`
        - **SQLite:** `Microsoft.EntityFrameworkCore.Sqlite` (lightweight, good for development, testing, small applications)
        - **PostgreSQL:** `Npgsql.EntityFrameworkCore.PostgreSQL`
        - **MySQL:** `Pomelo.EntityFrameworkCore.MySql` or `MySql.EntityFrameworkCore` (from Oracle)
        - **In-Memory:** `Microsoft.EntityFrameworkCore.InMemory` (only for testing, doesn't store actual data)
        - **Cosmos DB:** `Microsoft.EntityFrameworkCore.Cosmos` (for NoSQL)
    - **Installation:** You need to install the corresponding NuGet package for your chosen provider into your project.
- Specifying the Data Provider and Connection String
    - **Purpose:** You need to tell EF Core:
        1. **Which Provider to use?** (e.g., SQL Server, SQLite)
        2. **Which Database to connect to?** (server information, database name, authentication...)
    - **How to do it (Most common in ASP.NET Core or Worker Services):**
        - **Connection String:** Usually stored in a configuration file like `appsettings.json`.
            
            ```json
            // appsettings.json
            {
              "ConnectionStrings": {
                "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MyDatabaseName;Trusted_Connection=True;"
                // Or example for SQLite: "Data Source=mydatabase.db"
              }
            }
            ```
            
        - **Registering DbContext and Provider in `Program.cs` (or `Startup.cs` in older .NET versions):** Using Dependency Injection (DI) to configure and provide `DbContext` to the application.
            
            ```csharp
            // Program.cs (example for .NET 6+)
            var builder = WebApplication.CreateBuilder(args);
            
            // Get connection string from appsettings.json
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            
            // Register DbContext with DI and specify provider, connection string
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString)); // Or UseSqlite, UseNpgsql,...
            
            // ... other services
            
            var app = builder.Build();
            // ... pipeline configuration
            app.Run();
            
            ```
            
    - **Alternative (less common, e.g., simple Console App):** Override the `OnConfiguring` method in `DbContext`.
        
        ```csharp
        // In ApplicationDbContext.cs
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Only configure if not already configured from outside
            if (!optionsBuilder.IsConfigured) 
            {
                optionsBuilder.UseSqlServer("Your_Connection_String_Here");
            }
        }
        ```
        
- Understanding Code First Development and Migrations
    - **Code First Development:**
        - **Philosophy:** You **write code first** (define Entity classes and DbContext), and then EF Core will help you **create or update the database schema** based on that code.
        - **Advantages:**
            - Developers focus on business logic and object modeling.
            - Easy management of database schema versions alongside code (source control).
            - Suitable for new projects starting from scratch.
    - **Migrations:**
        - **Problem:** When your model changes (adding/removing properties, entities, changing data types, relationships...), how do you update the database schema accordingly without losing existing data?
        - **Solution:** EF Core Migrations is a mechanism to manage these changes safely and in a controlled manner.
        - **How it works:**
            1. When you change your model, you create a new "migration."
            2. EF Core compares the current model with the "snapshot" of the model from the last migration.
            3. It creates a migration file (containing C# code) describing the changes needed to the database (e.g., `ALTER TABLE`, `CREATE TABLE`, `DROP COLUMN`...). This file has two main methods: `Up()` (apply changes) and `Down()` (revert changes).
            4. You "apply" this migration to the database, and EF Core will execute the commands in the `Up()` method.
        - **Benefits:**
            - Manage schema changes step by step, with the ability to roll back.
            - Schema versions are stored in code, easy to track and coordinate in a team.
            - Automates database updates.
- Setup Console App Project
    - **Purpose:** Create a simple environment to practice EF Core without the complexity of ASP.NET Core.
    - **Steps:**
        - Create project: Open terminal or command prompt, navigate to the directory where you want to create the project and run the command:
        
        ```bash
        dotnet new console -o MyEfCoreApp
        cd MyEfCoreApp
        ```
        
        - **Install necessary NuGet packages:**
            - **EF Core Core:** `Microsoft.EntityFrameworkCore` (usually installed automatically by other packages)
            - **EF Core Tools:** `Microsoft.EntityFrameworkCore.Tools` (Necessary to run `dotnet ef` commands like migrations, scaffolding).
            - **Database Provider:** Choose the provider you want (e.g., `Microsoft.EntityFrameworkCore.SqlServer` or `Microsoft.EntityFrameworkCore.Sqlite`).
            - **(Optional) Design Time:** `Microsoft.EntityFrameworkCore.Design` (Usually necessary for `dotnet ef` to work correctly, especially when projects are separated).
            
            ```bash
            dotnet add package Microsoft.EntityFrameworkCore.SqlServer # Or .Sqlite, .Npgsql
            dotnet add package Microsoft.EntityFrameworkCore.Tools
            dotnet add package Microsoft.EntityFrameworkCore.Design
            ```
            
        - **Create Models and DbContext:** Create `.cs` files for entity classes (e.g., `Product.cs`) and the `DbContext` class (e.g., `ApplicationDbContext.cs`) as described in the previous sections.
        - **Configure Connection String:** Since this is a Console App without an `appsettings.json` and built-in DI like ASP.NET Core, the simplest way initially is to override `OnConfiguring` in `DbContext` (as described in section 6), or you can install the `Microsoft.Extensions.Configuration.Json` package to read from `appsettings.json` if desired.
- Adding a Migration
    - **Prerequisites:** Installed `Microsoft.EntityFrameworkCore.Tools` and `Microsoft.EntityFrameworkCore.Design`, have `DbContext` and models in place.
    - **Command:** Open terminal/command prompt **at the root directory of your project** (where the `.csproj` file is) and run:
        
        ```bash
        dotnet ef migrations add MigrationName # e.g., add migration
        ```
        
        - `dotnet ef`: Call the EF Core command-line tool.
        - `migrations add`: Command to add a new migration.
        - `MigrationName`: The name you give to this migration. Should be a meaningful name describing the change (e.g., `InitialCreate`, `AddProductPrice`, `RenameUserEmailColumn`).
    - **Result:**
        - EF Core will build your project.
        - Compare the current model with the last snapshot (or nothing if it's the first time).
        - Create a `Migrations` directory (if it doesn't exist).
        - In the `Migrations` directory, create 2 files:
            - `[Timestamp]_[MigrationName].cs`: Contains C# code with `Up()` and `Down()` methods describing schema changes.
            - `[DbContextName]ModelSnapshot.cs`: Contains a "snapshot" of the current model after applying this migration. EF Core uses this file for comparison when creating the next migration.
    - **Important:** **Always review the generated migration file** (`[Timestamp]_[MigrationName].cs`) to ensure that the SQL commands EF Core intends to execute match your intentions, especially for complex changes.
- IMPORTANT: EF Core 9.0 Migration Errors
    - **Version note:** Currently (April 2025), EF Core 9.0 may still be in a preview stage or newly officially released. Preview versions often can have bugs or breaking changes.
    - **Common Migration errors (in all EF Core versions, not just 9.0):**
        - **Model Inconsistency:** Current model doesn't match the last snapshot (possibly due to manual edits to the snapshot file or errors in the model).
        - **Database Schema Drift:** The actual database structure doesn't match what migrations expect (e.g., someone manually changed the database).
        - **Provider Specific Issues:** Some model changes might not be supported by the provider or translated into inaccurate/inefficient SQL.
        - **Complex Renames/Moves:** Complex table/column renames sometimes aren't automatically detected correctly by EF Core, requiring manual intervention in the migration file.
        - **Data Loss Warnings:** When a change risks data loss (e.g., dropping a column, narrowing a data type), EF Core will warn you. You need to confirm that you accept this risk.
        - **Circular Dependencies:** Complex relationships creating loops in your model.
    - **How to handle Migration errors:**
        1. **Read the error message carefully:** Error messages from `dotnet ef` are usually quite detailed and point to the problem.
        2. **Check the Migration file (`.cs`):** Look at the code in the `Up()` method to see if it makes sense. If needed, you *can* edit this file (but be careful and understand what you're doing).
        3. **Check Model and Snapshot:** Ensure your model is consistent and the snapshot file is accurate.
        4. **Consider `dotnet ef migrations remove`:** If a newly created migration is problematic and you want to redo it, this command will remove the last `.cs` migration file and update the snapshot. *Note: Only use when that migration hasn't been applied to the database yet!*
        5. **Check the Database:** Ensure the database schema is in the state that the previous migration left it in.
        6. **Search for specific errors:** Google the error message along with the EF Core version and provider you're using. The Stack Overflow community and Microsoft documentation are good sources of help.
    - **Specifically for EF Core 9.0 (if applicable):** Monitor Microsoft's official documentation page and the EF Core GitHub repository for known issues or breaking changes in this version related to Migrations.
- Generating a Database (Code-First)
    - **Purpose:** Apply pending migrations to the actual database. If the database doesn't exist yet, this command will typically create the database first and then apply the migrations.
    - **Command:** Open terminal/command prompt at the root directory of your project:
        
        ```bash
        dotnet ef database update
        ```
        
    - `dotnet ef`: Call the EF Core tool.
    - `database update`: Command to apply migrations to the database specified in your connection string and provider configuration.
    - **How it works:**
        1. EF Core checks the migrations history table (`__EFMigrationsHistory` - default name) in the database to see which migrations have been applied.
        2. It looks for migration files in your project that aren't in the history table.
        3. It executes the `Up()` method of each pending migration, in timestamp order.
        4. After successfully executing each migration, it records the information in the `__EFMigrationsHistory` table.
    - **Specifying a specific Migration:** You can apply up to a specific migration (or roll back to an older migration):
        
        ```bash
        dotnet ef database update TargetMigrationName # Apply up to this migration
        dotnet ef database update 0 # Rollback all migrations
        dotnet ef database update PreviousMigrationName # Rollback to the state after this migration
        ```
        
- Understanding Database First Development
    - **Philosophy:** Opposite to Code First. You have **an existing database first**, and you want EF Core to **create Entity classes and DbContext** based on that database's schema.
    - **When to use:**
        - Working with existing databases, third-party databases.
        - Projects where the database is designed and managed by a DBA (Database Administrator).
        - Quick model creation from a complex schema.
    - **Process:** Use EF Core's "Reverse Engineering" (or "Scaffolding") tool.
- Reverse Engineer Existing Database
    - **Purpose:** Automatically generate C# code (Entities and DbContext) from an existing database schema.
    - **Command (Scaffolding):** Open terminal/command prompt at the root directory of your project:
    
    ```bash
    dotnet ef dbcontext scaffold "Your_Connection_String_Here" Provider.Package.Name [options]
    ```
    
    - `"Your_Connection_String_Here"`: Connection string to the database you want to reverse engineer.
    - `Provider.Package.Name`: Name of the database provider package you're using (e.g., `Microsoft.EntityFrameworkCore.SqlServer`, `Npgsql.EntityFrameworkCore.PostgreSQL`).
    - **`[options]` (Common options):**
        - `o` or `-output-dir`: Directory to contain the generated entity files (e.g., `o Models`).
        - `c` or `-context`: Name you want to give to the generated `DbContext` class (e.g., `c MyExistingDbContext`).
        - `-context-dir`: Directory to contain the generated `DbContext` file (separate from models).
        - `t` or `-table`: Specify specific tables to create models for (if not specified, it will create for all). Example: `t Products -t Categories`.
        - `-use-database-names`: Use original column and table names from the database instead of trying to convert to C# naming conventions.
        - `-no-onconfiguring`: Don't generate an `OnConfiguring` method with a hardcoded connection string in the `DbContext`. You should use this option and configure the connection string through DI.
        - `-data-annotations`: Use Data Annotations instead of Fluent API to configure the model (default is Fluent API).
        - `f` or `-force`: Overwrite existing files if running the command again.
    - Example:
        
        ```bash
        # Create models in "Entities" directory, context named "LegacyDb" in "DataAccess" directory, using SQL Server, no hardcoded connection string
        dotnet ef dbcontext scaffold "Server=.;Database=LegacyDB;Trusted_Connection=True;" Microsoft.EntityFrameworkCore.SqlServer -o Entities -c LegacyDb --context-dir DataAccess --no-onconfiguring
        ```
        
        - **Result:** EF Core will connect to the database, read the schema and create `.cs` files for Entities and `DbContext` with Fluent API (or Data Annotations) configuration reflecting the database structure.
        - **Note:** The generated code is "one-way." If the database changes later, you need to run the scaffold command again (usually with the `f` option) to update the code, or you have to manually update the code. Database First doesn't use Migrations in the same way as Code First. If you want to switch to managing with Migrations after scaffolding, additional setup steps are required.
- Seeding Data
    - **Purpose:** Provide initial data for the database when it's created or when migrations are applied. Useful for:
        - Basic configuration data (e.g., user roles, default categories).
        - Test data.
        - Data necessary for the application to function.
    - **How to implement (Recommended):** Use the `OnModelCreating` method in `DbContext` and the `HasData` method of `EntityTypeBuilder`.
    
    ```csharp
    // In ApplicationDbContext.cs
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    
        // Seed data for Categories table
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Electronics", Description = "Gadgets and devices" },
            new Category { Id = 2, Name = "Books", Description = "Paperback and hardcover books" },
            new Category { Id = 3, Name = "Clothing", Description = "Apparel and accessories" }
        );
    
        // Seed data for Products table (example with foreign key to Category)
        // Important: Must provide primary key value (Id) for seed data
        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 101, Name = "Laptop Pro", Price = 1200.00m, CategoryId = 1 },
            new Product { Id = 102, Name = "Learning EF Core", Price = 45.50m, CategoryId = 2 },
            new Product { Id = 103, Name = "Wireless Mouse", Price = 25.99m, CategoryId = 1 }
        );
    
        // ... other configurations ...
    }
    ```
    
    - **How it works:**
        1. When you add or change data in `HasData`, you need to create a new migration (`dotnet ef migrations add AddSeedData`).
        2. This migration will contain `InsertData`, `UpdateData`, `DeleteData` commands accordingly.
        3. When you run `dotnet ef database update`, this migration will be applied and the data will be inserted/updated in the database.
    - **Advantages:**
        - Seed data is managed together with schema migrations.
        - Database provider independent (EF Core creates appropriate SQL).
        - Easy to manage in source control.
    - **Important notes:**
        - You **must** provide primary key (PK) values for seed data.
        - Seed data is managed by migrations. If you change data in `HasData`, EF Core will create a migration to update it in the database. If you remove data from `HasData`, the migration will remove it from the database. It's not simply inserting if not present.
- Section Review
- Section Source Code