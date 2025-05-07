# Interacting With Related Records

- Section Overview
    - After mastering model creation, querying, data modification, and schema management with migrations, today we will focus on how to **interact with related records** in the database. This is the core of data modeling in the real world.
- Database Relationships and Entity Framework Core
    - **In Database:** Relationships between tables are typically represented by **Foreign Keys (FK)**. A column (or multiple columns) in one table (dependent table) references the Primary Key (PK) of another table (principal table).
    - **In EF Core:** These relationships are represented in C# code using:
        - **Navigation Properties:** These are properties in your entity class that point to related entities. They can be:
            - **Reference Navigation Property:** Points to a single related entity (e.g., `public virtual Category Category { get; set; }` in the `Product` class). Represents the "one" side of the relationship.
            - **Collection Navigation Property:** Points to a collection of related entities (e.g., `public virtual ICollection<Product> Products { get; set; }` in the `Category` class). Represents the "many" side of the relationship. Typically uses `ICollection<T>`, `List<T>`, or `HashSet<T>`.
        - **Foreign Key Property:** A property in the dependent entity that stores the primary key value of the related principal entity (e.g., `public int CategoryId { get; set; }` in the `Product` class).
    - EF Core uses **conventions**, **data annotations**, or **Fluent API** to detect and configure these relationships based on how you define the properties above.
- One to Many Relationships
    - **Concept:** The most common relationship. One record in the principal table can be related to many records in the dependent table, but a record in the dependent table can only be related to one record in the principal table.
    - **Example:** A `Category` can have many `Products`, but a `Product` belongs to only one `Category`.
- Adding One-To-Many Relationships
    
    EF Core typically automatically detects this relationship based on naming conventions:
    
    - **Step 1: Define Entities with Navigation Properties and Foreign Key**
        
        ```csharp
        // Models/Category.cs
        public class Category
        {
            public int Id { get; set; } // Primary Key (PK) of Category
            public string Name { get; set; }
        
            // Collection Navigation Property (Many side)
            // A Category has many Products
            public virtual ICollection<Product> Products { get; set; } = new List<Product>();
        }
        
        // Models/Product.cs
        public class Product
        {
            public int Id { get; set; } // Primary Key (PK) of Product
            public string Name { get; set; }
            public decimal Price { get; set; }
        
            // --- Important part for relationship ---
            // 1. Foreign Key Property
            //    Naming convention: [Principal Navigation Property Name]Id => CategoryId
            public int CategoryId { get; set; }
        
            // 2. Reference Navigation Property (One side)
            //    Points to the Category this Product belongs to
            public virtual Category Category { get; set; }
            // --- End of important part ---
        }
        ```
        
        - **`virtual`**: The `virtual` keyword on navigation properties is necessary if you want to use **Lazy Loading** (discussed later). Even if not using Lazy Loading, adding `virtual` has no negative impact and is good practice.
    - **Step 2: Update DbContext (if not already done)**
    Ensure both `DbSet<Product>` and `DbSet<Category>` are in the `DbContext`.
        
        ```csharp
        public class ApplicationDbContext : DbContext
        {
            // ... constructor ...
            public DbSet<Product> Products { get; set; }
            public DbSet<Category> Categories { get; set; }
        }
        ```
        
    - **Step 3: (Optional) Configure using Fluent API**
    If conventions are insufficient or you want to customize (e.g., different foreign key name, delete behavior...), you can use Fluent API in the `OnModelCreating` method of `DbContext`:
        
        ```csharp
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>() // Start configuring Product
                .HasOne(p => p.Category) // Product has one Category (Reference Navigation)
                .WithMany(c => c.Products) // Category has many Products (Collection Navigation)
                .HasForeignKey(p => p.CategoryId) // Specify foreign key as CategoryId
                .OnDelete(DeleteBehavior.Restrict); // (Example) Specify delete behavior (discussed later)
                // .IsRequired(); // (Example) Default is required if FK is non-nullable (int)
        }
        ```
        
- View Diagram with Entity Framework Core Tools
    
    After you create a migration and update the database (`dotnet ef database update`), the table structure and relationships (foreign keys) will be created in the database. To visualize these relationships:
    
    - **SQL Server Management Studio (SSMS) / Azure Data Studio:** If using SQL Server, you can connect to the database and use the "Database Diagrams" feature to view the relationship diagram between tables.
    - **EF Core Power Tools:** This is a very useful extension for Visual Studio. After installation, you can right-click on the project containing `DbContext`, select "EF Core Power Tools" -> "View DbContext Model Diagram". It will create a DGML file showing your entities and their relationships based on your EF Core model (even before updating the database).
    
    Viewing the diagram helps you verify if relationships are defined as expected.
    
- Many to Many Relationships
    - **Concept:** A record in table A can be related to many records in table B, and vice versa, a record in table B can be related to many records in table A.
    - **Example:** A `Product` can have many `Tags`, and a `Tag` can be assigned to many `Products`.
    - **In Database:** This relationship is typically implemented using a **Join Table** containing foreign keys referencing both tables A and B.
- Adding Many-To-Many Relationships
    - **Approach (EF Core 5+ - Recommended): Skip Navigations**
    EF Core 5 and above supports much simpler M-M configuration by just defining Collection Navigation Properties on both sides. EF Core will automatically create a hidden join table.
        - **Step 1: Define Entities with Collection Navigations**
            
            ```csharp
            // Models/Product.cs (add Tag part)
            public class Product
            {
                // ... other properties ...
                public int CategoryId { get; set; }
                public virtual Category Category { get; set; }
            
                // Collection Navigation Property to Tags
                public virtual ICollection<Tag> Tags { get; set; } = new List<Tag>();
            }
            
            // Models/Tag.cs (new)
            public class Tag
            {
                public int Id { get; set; }
                public string Name { get; set; }
            
                // Collection Navigation Property to Products
                public virtual ICollection<Product> Products { get; set; } = new List<Product>();
            }
            ```
            
        - **Step 2: Update DbContext**
        Add `DbSet<Tag>` to `DbContext`.
            
            ```csharp
            public class ApplicationDbContext : DbContext
            {
                // ... constructor ...
                public DbSet<Product> Products { get; set; }
                public DbSet<Category> Categories { get; set; }
                public DbSet<Tag> Tags { get; set; } // Add new DbSet
            }
            ```
            
        - **Step 3: (Usually not needed) Configure Fluent API**
        In many cases, EF Core automatically detects this M-M relationship and creates a join table (`ProductTag`) with `ProductsId` and `TagsId` columns. If you need to customize the join table name or column names, you can use Fluent API:
            
            ```csharp
            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                // ... other configurations ...
            
                modelBuilder.Entity<Product>()
                    .HasMany(p => p.Tags) // Product has many Tags
                    .WithMany(t => t.Products) // Tag has many Products
                    .UsingEntity(j => j.ToTable("ProductTags")); // (Example) Customize join table name
            }
            ```
            
    - **Approach (Pre-EF Core 5 / Or when needing additional join table data): Explicit Join Entity**
    You must create an entity representing the join table (e.g., `ProductTag`) and define two separate One-to-Many relationships from `Product` and `Tag` to `ProductTag`. This approach is more complex but allows you to add data columns to the relationship itself (e.g., tag assignment date).
- Understanding One-To-One Relationships
    - **Concept:** Less common. A record in table A can be related to at most one record in table B, and vice versa.
    - **Example:** A `User` has one `UserProfile`. An `Order` has one `ShippingAddress`.
    - **Implementation:** Typically implemented by:
        - The dependent table has a foreign key pointing to the primary key of the principal table.
        - This foreign key is also the primary key of the dependent table (Shared Primary Key), OR has a unique constraint on the foreign key column.
- Adding One-To-One Relationships
    
    One-to-One relationships often need explicit configuration using Fluent API because conventions are not strong enough to determine which is principal and which is dependent.
    
    - **Step 1: Define Entities with Reference Navigations**
        
        ```csharp
        // Models/User.cs
        public class User
        {
            public int Id { get; set; }
            public string Username { get; set; }
        
            // Reference Navigation to UserProfile
            public virtual UserProfile Profile { get; set; }
        }
        
        // Models/UserProfile.cs
        public class UserProfile
        {
            // Method 1: Shared Primary Key (PK is also FK)
            public int Id { get; set; } // PK of UserProfile, also FK pointing to User.Id
            public string FullName { get; set; }
            public string AvatarUrl { get; set; }
        
            // Reference Navigation to User
            public virtual User User { get; set; }
        
            // Method 2: Separate PK and FK (less common for 1-1)
            // public int ProfileId { get; set; } // Separate PK
            // public int UserId { get; set; } // FK pointing to User.Id (needs Unique Constraint)
            // public string FullName { get; set; }
            // public virtual User User { get; set; }
        }
        ```
        
    - **Step 2: Update DbContext**
    Add `DbSet<User>` and `DbSet<UserProfile>` to `DbContext`.
    - **Step 3: Configure using Fluent API (Very important)**
    In `OnModelCreating`, you need to specify the relationship and foreign key.
        
        ```csharp
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ... other configurations ...
        
            // Configure 1-1 relationship User <-> UserProfile
            modelBuilder.Entity<User>()
                .HasOne(u => u.Profile) // User has one Profile
                .WithOne(p => p.User) // Profile belongs to one User
                .HasForeignKey<UserProfile>(p => p.Id); // Specify FK in UserProfile is Id column (Shared PK)
        
            // If using Method 2 (Separate PK/FK):
            // modelBuilder.Entity<User>()
            //     .HasOne(u => u.Profile)
            //     .WithOne(p => p.User)
            //     .HasForeignKey<UserProfile>(p => p.UserId); // Specify FK as UserId
            // // Need to add Unique Constraint for UserId if it's not PK
            // modelBuilder.Entity<UserProfile>()
            //     .HasIndex(p => p.UserId)
            //     .IsUnique();
        }
        ```
        
- Update Database With Relationships
    
    After you have defined relationships in your model (1-Many, M-Many, 1-1) by adding navigation properties, foreign key properties, and Fluent API configuration (if needed):
    
    1. **Create Migration:** Run `dotnet ef migrations add AddRelationships` (or another descriptive name). EF Core will detect new relationships and create necessary SQL commands (add foreign key columns, create join tables, create foreign key constraints...).
    2. **Apply Migration:** Run `dotnet ef database update`. Your database will be updated with new tables and constraints.
- Inserting Related Data
    
    When inserting related data, EF Core provides several flexible approaches:
    
    - **Method 1: Directly Assign Foreign Key Property** (When you know the ID of the principal entity)
        
        ```csharp
        var existingCategory = await context.Categories.FindAsync(1); // Assume Category Id=1 exists
        if (existingCategory != null)
        {
            var newProduct = new Product
            {
                Name = "Product via FK",
                Price = 50,
                CategoryId = existingCategory.Id // Directly assign FK
            };
            context.Products.Add(newProduct);
            await context.SaveChangesAsync();
        }
        ```
        
    - **Method 2: Assign Navigation Property** (When you have the principal entity instance)
        
        ```csharp
        var existingCategory = await context.Categories.FindAsync(1);
        if (existingCategory != null)
        {
            var newProduct = new Product
            {
                Name = "Product via Navigation",
                Price = 75,
                Category = existingCategory // Assign navigation property
            };
            context.Products.Add(newProduct);
            await context.SaveChangesAsync();
        }
        ```
        
    - **Method 3: Add to Collection Navigation Property**
        
        ```csharp
        var existingCategory = await context.Categories.FindAsync(1);
        if (existingCategory != null)
        {
            var newProduct = new Product
            {
                Name = "Product via Collection",
                Price = 100
            };
            existingCategory.Products.Add(newProduct); // Add to collection
            await context.SaveChangesAsync();
        }
        ```
        
- Querying Related Data
    
    EF Core provides several ways to load related data:
    
    - **Eager Loading:** Load related data immediately with the main query
        - Using `Include()`:
            ```csharp
            var products = await context.Products
                .Include(p => p.Category) // Load Category
                .Include(p => p.Tags) // Load Tags
                .ToListAsync();
            ```
        - Using `ThenInclude()` for nested relationships:
            ```csharp
            var products = await context.Products
                .Include(p => p.Category)
                    .ThenInclude(c => c.Products) // Load Products of Category
                .ToListAsync();
            ```
    
    - **Explicit Loading:** Load related data on demand
        ```csharp
        var product = await context.Products.FindAsync(1);
        if (product != null)
        {
            await context.Entry(product)
                .Reference(p => p.Category)
                .LoadAsync(); // Load Category
            
            await context.Entry(product)
                .Collection(p => p.Tags)
                .LoadAsync(); // Load Tags
        }
        ```
    
    - **Lazy Loading:** Automatically load related data when accessed
        - Requires:
            1. Navigation properties marked as `virtual`
            2. Lazy loading package installed (`Microsoft.EntityFrameworkCore.Proxies`)
            3. Lazy loading enabled in `DbContext`:
                ```csharp
                protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
                {
                    optionsBuilder.UseLazyLoadingProxies();
                }
                ```
        - Usage:
            ```csharp
            var product = await context.Products.FindAsync(1);
            var categoryName = product.Category.Name; // Category loaded automatically
            ```
    
    - **Selective Loading:** Load only needed properties
        ```csharp
        var products = await context.Products
            .Select(p => new
            {
                p.Name,
                CategoryName = p.Category.Name,
                TagCount = p.Tags.Count
            })
            .ToListAsync();
        ```
