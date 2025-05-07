# ASP.NET Core and EF Core

**ASP.NET Core and EF Core**

- **Section Overview**
    - In this section, we will explore how ASP.NET Core and Entity Framework Core work together to build web applications and APIs that interact with databases.
    - Main topics include:
        - Integrating EF Core with ASP.NET Core through Dependency Injection
        - Managing DbContext lifecycle in web applications
        - Configuring database connections in ASP.NET Core
        - Handling common errors when working with EF Core in ASP.NET Core
    - The objectives of this section are to help you:
        - Understand how ASP.NET Core and EF Core integrate with each other
        - Learn how to configure and use DbContext in web applications
        - Master best practices when working with EF Core in ASP.NET Core


**How EF Core and ASP.NET Core Work**
    
    The integration between EF Core and ASP.NET Core primarily relies on two core mechanisms of ASP.NET Core:
    
    - **Dependency Injection (DI):**
        - ASP.NET Core has a powerful built-in DI system. Instead of creating `DbContext` instances everywhere, you register your `DbContext` with the DI container.
        - Then, whenever a component in your application (like a Controller, Service, or Razor Page Model) needs to use the `DbContext`, it simply declares the `DbContext` as a parameter in its constructor. The DI container will automatically create and provide (inject) an appropriate `DbContext` instance.
    - **`DbContext` Lifetime: Scoped**
        - When you register a `DbContext` using the `AddDbContext` method (which we'll see later), its default lifetime is **Scoped**.
        - **Scoped Lifetime** means: A new `DbContext` instance will be created for **each HTTP request** to your application. This instance will be used throughout the processing of that request (e.g., in middleware, controller, services called by that controller). When the request ends, that `DbContext` instance will be automatically **disposed** (resources released).
        - **Why is Scoped appropriate?**
            - Ensures each request has its own Unit of Work, avoiding issues with state sharing or tracking errors between different requests.
            - `DbContext` is not thread-safe, limiting it to a single request helps avoid multi-threading issues.
    - **Configuration:**
        - ASP.NET Core's configuration system (reading from `appsettings.json`, environment variables, user secrets...) is used to provide necessary information to the `DbContext`, most importantly the **connection string**.

**Connect to the Database Context**
    
    This is the configuration step to make ASP.NET Core aware of your `DbContext` and how to create it.
    
    - **Step 1: Install Required NuGet Packages**
        - Ensure you have installed the necessary EF Core packages in your ASP.NET Core project:
            
            ```
            dotnet add package Microsoft.EntityFrameworkCore.Design
            dotnet add package Microsoft.EntityFrameworkCore.SqlServer # Or other provider (Npgsql, Sqlite...)
            # Tools package needed for dotnet ef commands, but usually installed as a global tool or already present
            # dotnet add package Microsoft.EntityFrameworkCore.Tools
            ```
            
    - **Step 2: Add Connection String to appsettings.json**
        - Open `appsettings.json` (and `appsettings.Development.json` if needed).
        - Add the `ConnectionStrings` section:
            
            ```json
            {
              "Logging": { ... },
              "AllowedHosts": "*",
              "ConnectionStrings": {
                "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MyWebAppDb;Trusted_Connection=True;MultipleActiveResultSets=true"
                // Replace with your actual connection string
              }
            }
            ```
            
    - **Step 3: Register DbContext with Dependency Injection (Program.cs)**
        - Open `Program.cs` (for .NET 6+).
        - Find the services configuration section (`builder.Services...`).
        - Use the `AddDbContext` method to register your `DbContext`.
            
            ```csharp
            using Microsoft.EntityFrameworkCore;
            // using MyEfCoreApi.Data; // Namespace containing your ApplicationDbContext
            // using MyEfCoreApi.Models; // Namespace containing your models
            
            var builder = WebApplication.CreateBuilder(args);
            
            // 1. Get connection string from configuration (appsettings.json)
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            }
            
            // 2. Register ApplicationDbContext with DI container
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString)); // Or UseNpgsql, UseSqlite...
            
            // Add other services (Controllers, Swagger, etc.)
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            
            var app = builder.Build();
            
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            
            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
            
            ```
            
        - `AddDbContext<ApplicationDbContext>`: Registers `ApplicationDbContext` with DI.
        - `options => options.UseSqlServer(connectionString)`: Configures `DbContextOptions` to use SQL Server provider and the retrieved connection string. EF Core will automatically read these `DbContextOptions` when the DI container creates a `DbContext` instance.
        - By default, `AddDbContext` registers the `DbContext` with **Scoped lifetime**.
    
    Now, you can inject `ApplicationDbContext` into the constructors of Controllers, Services, or Razor Page Models.
    
    ```csharp
    // Example in an API Controller
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context; // Inject DbContext
    
        public ProductsController(ApplicationDbContext context) // Receive DbContext via constructor
        {
            _context = context;
        }
    
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            return await _context.Products.Include(p => p.Category).ToListAsync(); // Use DbContext
        }
        // ... other actions (POST, PUT, DELETE) ...
    }
    
    ```
    
**Fixing EF Core Design Time Errors**
    
    When you run `dotnet ef` commands (e.g., `dotnet ef migrations add InitialCreate`, `dotnet ef database update`) in an ASP.NET Core environment, you might sometimes encounter errors. These commands need to be able to create a `DbContext` instance to read the model configuration, and they run in a different context than the actual running application.
    
    - **Common Causes and Solutions:**
        1. **Missing Microsoft.EntityFrameworkCore.Design package:**
            - **Error:** Usually reports not finding the `dotnet ef` command or assembly-related errors.
            - **Solution:** Ensure you have installed the `Microsoft.EntityFrameworkCore.Design` package in your ASP.NET Core **startup project**.
                
                ```bash
                dotnet add package Microsoft.EntityFrameworkCore.Design
                ```
                
        2. **Incorrect Startup Project:**
            - **Error:** `dotnet ef` can't find the `DbContext` or necessary configuration because it's running in the wrong directory.
            - **Solution:**
                - Run `dotnet ef` commands from the directory containing the **startup project** (main ASP.NET Core project).
                - If your `DbContext` or migrations are in a separate class library project, you need to specify the startup project when running commands:
                    
                    ```powershell
                    # Run from directory containing DbContext/Migrations
                    dotnet ef migrations add MyMigration --startup-project ../MyWebAppProject
                    dotnet ef database update --startup-project ../MyWebAppProject
                    ```
                    
        3. **Unable to Create DbContext Instance at Design Time:**
            - **Error:** Usually reports "Unable to create an object of type 'ApplicationDbContext'. For the different patterns supported at design time, see [https://go.microsoft.com/fwlink/?linkid=851728](https://go.microsoft.com/fwlink/?linkid=851728)"
            - **Cause:** Your `DbContext` has a constructor that requires parameters (like `DbContextOptions` or other services) that the `dotnet ef` tool cannot automatically provide.
            - **Solution 1: Rely on Application Host (Default):** `dotnet ef` tries to call `Program.CreateHostBuilder(args).Build().Services.GetRequiredService<ApplicationDbContext>()`. Ensure your `Program.cs` is correctly configured to create a host and provide the `DbContext` (including reading the connection string). This usually works well with standard configuration.
            - **Solution 2: Implement IDesignTimeDbContextFactory<T> (Recommended when Solution 1 fails/is complex):**
                - Create a new class in your project containing the `DbContext`, implementing the `IDesignTimeDbContextFactory<ApplicationDbContext>` interface.
                - This interface requires a `CreateDbContext(string[] args)` method that returns an `ApplicationDbContext` instance.
                - Inside this method, you manually configure the `DbContextOptionsBuilder` (e.g., manually read connection string from `appsettings.json`) and create the `DbContext`. The `dotnet ef` tool will prioritize using this factory if found.
                
                ```csharp
                using Microsoft.EntityFrameworkCore;
                using Microsoft.EntityFrameworkCore.Design;
                using Microsoft.Extensions.Configuration;
                using System.IO;
                
                // Place this class in the project containing ApplicationDbContext
                public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
                {
                    public ApplicationDbContext CreateDbContext(string[] args)
                    {
                        // Get path to project containing DbContext (may need adjustment)
                        // This assumes appsettings.json is at the same level or parent level of this project
                        // Or you can point directly to the WebApp/API project to read its appsettings.json
                        IConfigurationRoot configuration = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory()) // Or path to WebApp/API project
                            .AddJsonFile("appsettings.json")
                            .AddJsonFile("appsettings.Development.json", optional: true) // Also read Development file
                            .Build();
                
                        var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
                        var connectionString = configuration.GetConnectionString("DefaultConnection");
                
                        builder.UseSqlServer(connectionString); // Or other provider
                
                        return new ApplicationDbContext(builder.Options);
                    }
                }
                ```
                
**Scaffolding Code with Visual Studio**
    
    Scaffolding is the process of automatically generating basic code (boilerplate code) for CRUD (Create, Read, Update, Delete) operations based on your model and `DbContext`. Visual Studio provides a visual tool for this.
    
    - **How to do it:**
        1. Right-click on the `Controllers` folder (for API/MVC) or `Pages` folder (for Razor Pages) in Solution Explorer.
        2. Select **Add** -> **New Scaffolded Item...**.
        3. In the "Add New Scaffolded Item" window:
            - For API: Select **API Controller with actions, using Entity Framework**.
            - For Razor Pages: Select **Razor Pages using Entity Framework (CRUD)**.
            - For MVC: Select **MVC Controller with views, using Entity Framework**.
        4. Click **Add**.
        5. In the next window:
            - Select **Model class** (e.g., `Product`).
            - Select **Database context class** (e.g., `ApplicationDbContext`).
            - Name the Controller/Pages (e.g., `ProductsController`, `Pages/Products` folder).
            - (Optional) Configure other options (async actions, views...).
        6. Click **Add**. Visual Studio will automatically create Controller files (`.cs`) and/or Razor files (`.cshtml`, `.cshtml.cs`) with basic CRUD actions/handlers.
- Scaffolding Code with Visual Studio Code
    
    If you use VS Code or want to perform scaffolding from the command line, you need the `dotnet-aspnet-codegenerator` tool.
    
    - **Step 1: Install the tool**
        - Install globally (recommended):
            
            ```
            dotnet tool install --global dotnet-aspnet-codegenerator
            
            ```
            
        - Or install locally for the project (create manifest if not exists):
            
            ```
            dotnet new tool-manifest # Only run first time if .config/dotnet-tools.json doesn't exist
            dotnet tool install dotnet-aspnet-codegenerator
            
            ```
            
            (If installed locally, you need to run commands using `dotnet tool run dotnet-aspnet-codegenerator ...`)
            
    - **Step 2: Run Scaffolding Command**
        - Open terminal at the root of your ASP.NET Core project.
        - **Create API Controller:**
            
            ```
            dotnet aspnet-codegenerator controller -name ProductsController -async -api -m Product -dc ApplicationDbContext -outDir Controllers
            
            ```
            
            - `name ProductsController`: Controller name.
            - `async`: Create async actions.
            - `api`: Specify creating API controller (no Views).
            - `m Product`: Model class name.
            - `dc ApplicationDbContext`: DbContext class name.
            - `outDir Controllers`: Output directory.
        - **Create Razor Pages (CRUD):**
            
            ```
            dotnet aspnet-codegenerator razorpage ProductCRUD -m Product -dc ApplicationDbContext -udl -outDir Pages/Products --referenceScriptLibraries
            
            ```
            
            - `ProductCRUD`: Name for the set of Razor pages (not a specific file name).
            - `m Product`: Model class.
            - `dc ApplicationDbContext`: DbContext class.
            - `udl`: Use default layout.
            - `outDir Pages/Products`: Output directory (will create Create.cshtml, Delete.cshtml, Details.cshtml, Edit.cshtml, Index.cshtml).
            - `-referenceScriptLibraries`: Add script tags for client-side validation.
- Exploring Scaffolded Code
    
    Open a Controller or Razor Page Model (`.cs`) that was just created:
    
    - **Dependency Injection:** You'll see `ApplicationDbContext` injected into the constructor.
        
        ```
        private readonly ApplicationDbContext _context;
        public ProductsController(ApplicationDbContext context) { _context = context; }
        
        ```
        
    - **CRUD Actions/Handlers:**
        - **GET (Index/GetAll):** Usually uses `_context.Products.ToListAsync()` (may include `Include`).
        - **GET (Details/GetById):** Uses `_context.Products.FindAsync(id)` or `FirstOrDefaultAsync(m => m.Id == id)`.
        - **POST (Create):** Creates new instance, uses `_context.Products.Add(product)`, `await _context.SaveChangesAsync()`.
        - **PUT (Edit):** Queries entity, updates properties, `_context.Update(product)` or just `await _context.SaveChangesAsync()` (if entity is tracked). Needs concurrency handling.
        - **DELETE:** Queries entity, uses `_context.Products.Remove(product)`, `await _context.SaveChangesAsync()`.
    - **Note:** Scaffolded code is **starter** code. You **must** review and improve it:
        - Add more detailed validation.
        - Improve error handling.
        - Use DTOs (Data Transfer Objects) instead of directly returning/receiving entities in API.
        - Add more complex business logic (usually moved to Service/Repository classes).
        - Add authentication and authorization.
        - Handle concurrency explicitly.
- Review Best Practices
    
    When using EF Core in ASP.NET Core, remember these best practices (many points already mentioned in previous lessons):
    
    1. **Register DbContext with Scoped Lifetime:** Always use `AddDbContext` in `Program.cs`.
    2. **Inject DbContext via Constructor:** Let DI manage instance creation and provision.
    3. **Use Async Methods:** Always use `...Async` (e.g., `ToListAsync`, `SaveChangesAsync`) in controller/page actions/handlers to avoid blocking threads.
    4. **Unit of Work per Request:** Leverage Scoped lifetime. Don't share a `DbContext` instance across multiple requests.
    5. **Keep Controllers/Actions "Thin":** Move complex data access and business logic to separate Service or Repository classes. Controller should only coordinate.
    6. **Use Projections (DTOs):** Especially important for APIs. Don't return full entities. Create DTO classes containing only necessary data for the client. This helps optimize, secure, and make the API more stable when the model changes.
    7. **Handle Concurrency:** Implement Optimistic Concurrency (e.g., using `[Timestamp]`) to avoid data loss when multiple users edit simultaneously.
    8. **Error Handling and Logging:** Implement appropriate error handling and logging mechanisms.
    9. **Connection Resiliency:** Enable `EnableRetryOnFailure` to increase stability.
    10. **Separation of Concerns:** Consider separating `DbContext` and models into a separate Class Library project (Infrastructure/Data layer), especially for larger applications. The ASP.NET Core project will reference that project.
- Section Review
    - EF Core and ASP.NET Core integrate tightly through Dependency Injection and Configuration.
    - `DbContext` is typically registered with Scoped lifetime (`AddDbContext`), ensuring each HTTP request has its own Unit of Work.
    - Configure connection string in `appsettings.json` and register `DbContext` in `Program.cs`.
    - Master how to fix common errors when running `dotnet ef` (missing Design package, wrong startup project, implementing `IDesignTimeDbContextFactory`).
    - Use scaffolding tools (Visual Studio or `dotnet-aspnet-codegenerator`) to quickly generate basic CRUD code.
    - Always review and improve scaffolded code, applying best practices like using async, DTOs, handling concurrency, separation of concerns...
    
    Integrating EF Core into ASP.NET Core is a foundational skill for building modern web applications with .NET. By applying the right techniques and best practices, you can build efficient, maintainable, and scalable data applications. This is also the final part of the basic EF Core training series. Congratulations on completing it! Keep practicing and exploring further.
    
- Section Source Code