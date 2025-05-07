# Handling Database Changes and Migrations

**Handling Database Changes and Migrations**

- **Section Overview**
    - In this section, we will learn how to manage and apply database structure changes safely and in a controlled manner through EF Core Migrations.
    - Main topics include:
        - Understanding EF Core Migrations and how they work
        - Adding new entities and updating database with migrations
        - Using configuration files to manage connection strings
        - Creating migration scripts for different environments
        - Rolling back migrations and database changes
    - The objectives of this section are to help you:
        - Understand and effectively use EF Core Migrations
        - Manage database schema changes safely
        - Apply migrations to different environments
        - Create and use migration scripts
        - Handle situations requiring migration rollbacks
- Review Entity Framework Core Migrations
    
    Let's quickly recap the core concepts of Migrations that we briefly mentioned earlier:
    
    - **Purpose:** It's EF Core's mechanism for managing sequential changes to database schema, synchronized with changes in your model code (Entities and DbContext).
    - **Basic Workflow (Code First):**
        1. Change your model code (add/edit/remove entity, property, relationship...).
        2. Run the command `dotnet ef migrations add [DescriptiveMigrationName]` in the terminal at the project directory. This command compares the current model with the last snapshot and creates a new migration file.
        3. Run the command `dotnet ef database update` to apply the changes in the migration (execute the `Up()` method) to the configured database.
    - **Key Components:**
        - **`Migration Files (Migrations/[Timestamp]_[MigrationName].cs):`** Contains C# code with `Up()` method (to apply changes) and `Down()` method (to revert changes).
        - **`Model Snapshot (Migrations/[DbContextName]ModelSnapshot.cs):`** A "snapshot" of your model's state after applying the last migration. EF Core uses this file for comparison when creating the next migration. **You should not manually edit this file.**
        - **`History table (__EFMigrationsHistory):`** A special table automatically created by EF Core in your database to track which migrations have been successfully applied. `database update` relies on this table to know which migrations to run next.
- Adding More Entities and Updating Database with Migration(s)
    
    Let's look at a practical example. Suppose initially we only had a `Product` entity:
    
    ```csharp
    // Models/Product.cs (initial)
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
    }
    
    // Data/ApplicationDbContext.cs (initial)
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        public DbSet<Product> Products { get; set; }
    }
    ```
    
    Now, we want to add `Category` and create a one-to-many relationship (one Category has many Products).
    
    - **Step 1: Update the Model and DbContext**
        - Create the `Category` entity:
            
            ```csharp
            // Models/Category.cs (new)
            public class Category
            {
                public int Id { get; set; }
                public string Name { get; set; }
                public string Description { get; set; }
            
                // Navigation property: One Category has many Products
                public virtual ICollection<Product> Products { get; set; } = new List<Product>();
            }
            ```
            
        - Update `Product` to have a foreign key and navigation property to `Category`:
            
            ```csharp
            // Models/Product.cs (updated)
            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
                public decimal Price { get; set; }
            
                // Foreign key to Category
                public int CategoryId { get; set; }
                // Navigation property: One Product belongs to one Category
                public virtual Category Category { get; set; }
            }
            ```
            
        - Add `DbSet<Category>` to `DbContext`:
            
            ```csharp
            // Data/ApplicationDbContext.cs (updated)
            public class ApplicationDbContext : DbContext
            {
                public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
                public DbSet<Product> Products { get; set; }
                public DbSet<Category> Categories { get; set; } // Add new DbSet
            
                // (Optional) Configure relationship with Fluent API if needed
                // protected override void OnModelCreating(ModelBuilder modelBuilder) { ... }
            }
            ```
            
    - **Step 2: Create a New Migration**
        - Open the terminal at the root of your project.
        - Run the command:
            
            ```bash
            dotnet ef migrations add AddCategoryAndRelation
            ```
            
        - EF Core will build the project, compare the new model (with `Category` and the relationship) to the old `ModelSnapshot.cs` (with just `Product`).
        - It will create a new migration file (e.g., `Migrations/20250411102000_AddCategoryAndRelation.cs`) containing commands like:
            - `migrationBuilder.CreateTable("Categories", ...)`
            - `migrationBuilder.AddColumn<int>("CategoryId", "Products", ...)`
            - `migrationBuilder.CreateIndex(...)`
            - `migrationBuilder.AddForeignKey(...)`
        - It will also update the `ModelSnapshot.cs` file to reflect the new state of the model.
        - **`Always check the .cs migration file`** to ensure it matches your intentions.
    - **Step 3: Apply the Migration to the Database**
        - Run the command:
            
            ```bash
            dotnet ef database update
            ```
            
        - EF Core will check the `__EFMigrationsHistory` table and see that the `AddCategoryAndRelation` migration hasn't been applied yet.
        - It will execute the `Up()` method in that migration file, creating the `Categories` table and adding the `CategoryId` column and foreign key to the `Products` table in your database.
        - Finally, it will record the migration name `AddCategoryAndRelation` in the `__EFMigrationsHistory` table.
    
    Now your database has been updated to match your new model!
    
- Using Configuration Files
    - **Connection String:** The `dotnet ef` commands (like `database update`, `migrations add`) need to know which database to connect to. They typically look for the connection string in the following ways (in order of priority):
        1. Command line parameter (e.g., `-connection "..."` - rarely used).
        2. User Secrets (in the Development environment).
        3. Environment Variables.
        4. The `appsettings.Development.json` file (if the environment is Development).
        5. The `appsettings.json` file.
        6. In the `OnConfiguring` method of the `DbContext` (if it exists and isn't configured from outside).
        7. Through `IDesignTimeDbContextFactory<T>`: If your project is complex (e.g., DbContext is in a different project from the startup project), you might need to create a class implementing this interface to tell EF tools how to create a `DbContext` and get the connection string at design time.
    - **Environments:** The `ASPNETCORE_ENVIRONMENT` environment variable (e.g., `Development`, `Staging`, `Production`) affects which `appsettings.[Environment].json` file is prioritized. This allows you to easily apply migrations to different databases (dev DB, test DB, production DB) by specifying the correct connection string for each environment and running the `dotnet ef database update` command in the corresponding environment.
- Generating Migration Scripts
    
    Sometimes, you can't or don't want to run `dotnet ef database update` directly on an environment (especially Production). Instead, you can generate an SQL script containing all the necessary commands to update the database.
    
    - **Command:**
        
        ```bash
        dotnet ef migrations script [FromMigration] [ToMigration] -o output.sql --idempotent
        ```
        
        - `[FromMigration]` (Optional): The starting migration. If omitted, defaults to `0` (empty database state).
        - `[ToMigration]` (Optional): The last migration to apply. If omitted, defaults to the latest migration in the project.
        - `-o output.sql` or `-output output.sql`: Saves the script to the `output.sql` file. If not specified, the script will print to the console.
        - `--idempotent`: **Very important!** This option creates an "idempotent" script, meaning the script can be run multiple times without causing errors. It will check the `__EFMigrationsHistory` table before executing the commands of each migration, ensuring only migrations that haven't been applied are run. **Always use this option when creating scripts for production.**
    - **Examples:**
        - Create a script for all migrations from the beginning to the latest:
            
            ```bash
            dotnet ef migrations script -o full_migration.sql --idempotent
            ```
            
        - Create a script from the `AddCategoryAndRelation` migration to the latest migration:
            
            ```bash
            dotnet ef migrations script AddCategoryAndRelation -o incremental_migration.sql --idempotent
            ```
            
    - **Usage:** This SQL script can be:
        - Reviewed by a DBA (Database Administrator).
        - Run manually using database management tools (like SQL Server Management Studio, Azure Data Studio, psql...).
        - Integrated into a CI/CD process to automatically deploy database changes.
- Rolling Back Migrations and Database Changes
    
    If you discover an error after applying a migration or simply want to revert to a previous schema state, there are two main ways:
    
    - **`Method 1: Roll Back the Database to a Previous Migration (Using database update)`**
        - This command will execute the `Down()` method of the necessary migrations to bring the database schema back to the state of the target migration.
        - **Command:**
            
            ```bash
            dotnet ef database update [TargetMigrationName]
            ```
            
            - `[TargetMigrationName]`: The name of the migration that you want the database to revert to the state **after** that migration has been applied.
            - To undo all migrations and revert the database to the empty state (initial schema), use `0` as the migration name:
                
                ```bash
                dotnet ef database update 0
                ```
                
        - **Example:** If you have `MigrationA`, `MigrationB`, `MigrationC` applied, running `dotnet ef database update MigrationA` will execute the `Down()` method of `MigrationC` and `MigrationB`.
        - **Note:** The `Down()` method must be written correctly to accurately undo the changes made by `Up()`. EF Core auto-generates `Down()` fairly well for basic operations, but for complex or customized changes in `Up()`, you should carefully check `Down()`. Rolling back can result in data loss if `Down()` includes `DROP TABLE` or `DROP COLUMN` commands.
    - **`Method 2: Remove the Last Migration from the Project (Using migrations remove)`**
        - **Command:**
            
            ```bash
            dotnet ef migrations remove
            ```
            
        - **Effect:** This command **`only removes the last .cs migration file`** from the `Migrations` directory and **`updates the ModelSnapshot.cs file`** back to the state of the previous migration.
        - **IMPORTANT WARNING:** This command **DOES NOT CHANGE YOUR DATABASE**. It only changes the code in your project.
        - **When to use?** You should only use this command when:
            1. You just ran `migrations add` and realized that the migration is incorrect or you want to change the model further *before* creating the final migration.
            2. The last migration has **never been applied** to any database (including the dev DB), OR you have already **rolled back (reverted) the database** to the state before that migration using `database update [PreviousMigration]`.
        - **`NEVER use migrations remove if the migration has already been applied to a database (especially production or shared databases) and you cannot or do not want to roll back that database.`** This will cause a synchronization loss between your migration code and the actual database state, leading to serious errors later.
- EF Bundles
    
    EF Bundles is a way to package your migrations into a single executable file, simplifying deployment.
    
    - **Concept:** A compact executable file containing all your migrations and enough EF Core logic necessary to apply them to a database.
    - **Benefits:**
        - No need to install .NET SDK or EF Core tools on the target server.
        - Easy integration into CI/CD pipelines.
        - Simpler deployment: just copy the bundle file and run it.
    - **Creating a Bundle:**
        
        ```bash
        # Create default bundle (usually for current OS)
        dotnet ef migrations bundle -o ./efbundle --force --verbose
        
        # Create bundle for a specific runtime (e.g., Linux x64)
        # dotnet ef migrations bundle -o ./efbundle-linux-x64 --runtime linux-x64 --force --verbose
        ```
        
        - `-o ./efbundle`: Sets the name and path for the bundle output file.
        - `--force`: Overwrites the bundle file if it already exists.
        - `--verbose`: Shows detailed logs.
        - `--runtime`: (Optional) Specifies the Runtime Identifier (RID) if you want to create a bundle for a different OS/architecture.
    - **Using the Bundle:**
        - Copy the bundle file (e.g., `efbundle` or `efbundle.exe`) to the target server.
        - Run the bundle file from the command line, passing in the connection string:
            
            ```bash
            # Linux/macOS
            ./efbundle --connection "Server=your_server;Database=your_db;User ID=user;Password=pass;"
            
            # Windows
            .\efbundle.exe --connection "Server=your_server;Database=your_db;User ID=user;Password=pass;"
            ```
            
        - The bundle will automatically connect to the database and apply any missing migrations, similar to `dotnet ef database update`.
- Applying Migrations at Runtime
    
    You can also apply migrations automatically when the application starts by calling the `dbContext.Database.MigrateAsync()` method (or `Migrate()` for the synchronous version).
    
    - **`Implementation (Example in ASP.NET Core's Program.cs):`**
        
        ```csharp
        // Program.cs
        var builder = WebApplication.CreateBuilder(args);
        // ... services ...
        var app = builder.Build();
        
        // **Get DbContext instance from service provider**
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            try
            {
                var dbContext = services.GetRequiredService<ApplicationDbContext>();
                // **Apply migrations**
                await dbContext.Database.MigrateAsync(); // Or dbContext.Database.Migrate();
                Console.WriteLine("Migrations applied successfully.");
        
                // (Optional) Can add seeding data here after migration
                // await SeedData.InitializeAsync(services);
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred while migrating or seeding the database.");
                // Consider stopping the application if migration fails critically
                // throw;
            }
        }
        
        // ... configure pipeline ...
        app.Run();
        ```
        
    - **Advantages:**
        - Simple for straightforward deployment scenarios (single-instance application).
        - Ensures the database always has the latest schema when the application starts.
    - **Disadvantages and Risks (Very Important):**
        - **Not suitable for multi-instance environments:** If you run multiple instances of your application (e.g., web farm, Kubernetes pods, serverless functions), the instances might simultaneously try to run `MigrateAsync()`, leading to race conditions, errors, or even database corruption.
        - **Slow or failed startup:** If the migration process takes a long time or encounters errors, the application will start slowly or fail to start.
        - **Permission issues:** The user account that the application runs under needs permissions to modify the database schema (ALTER, CREATE, DROP...), which might be undesirable or unsafe in a production environment.
    - **Recommendation:** **Avoid applying migrations automatically at runtime in production environments or multi-instance environments.** Instead, consider migration application as a **separate step in your deployment process**, using `dotnet ef database update`, SQL scripts, or EF Bundles. Runtime application should only be considered for development environments or simple, single-instance applications.
- Section Review
    - Migrations are an essential tool for managing database schema changes systematically and in sync with your code.
    - Always create a migration (`migrations add`) after changing your model and apply it (`database update`) to update the database.
    - Use SQL scripts (`migrations script --idempotent`) for review or manual/automated deployment.
    - Understand how to roll back (`database update [Target]`) and when you should/shouldn't use `migrations remove`.
    - EF Bundles (`migrations bundle`) are a convenient way to package and deploy migrations, especially in CI/CD.
    - Applying migrations at runtime (`Database.MigrateAsync()`) is convenient but risky in production and multi-instance environments; consider migration as a separate deployment step instead.
- Section Source Code