# Interacting With Related Records

- Section Overview
    - Sau khi đã nắm vững cách tạo model, truy vấn, thay đổi dữ liệu và quản lý schema bằng migrations, hôm nay chúng ta sẽ tập trung vào cách **tương tác với các bản ghi có liên quan** đến nhau trong database. Đây là cốt lõi của việc mô hình hóa dữ liệu trong thế giới thực.
- Database Relationships and Entity Framework Core
    - **Trong Database:** Các mối quan hệ giữa các bảng thường được thể hiện bằng **khóa ngoại (Foreign Keys - FK)**. Một cột (hoặc nhiều cột) trong một bảng (bảng phụ thuộc - dependent table) tham chiếu đến khóa chính (Primary Key - PK) của một bảng khác (bảng chính - principal table).
    - **Trong EF Core:** Các mối quan hệ này được biểu diễn trong code C# bằng cách sử dụng:
        - **Navigation Properties (Thuộc tính điều hướng):** Đây là các thuộc tính trong lớp entity của bạn trỏ đến các entity liên quan khác. Chúng có thể là:
            - **Reference Navigation Property:** Trỏ đến một entity liên quan duy nhất (ví dụ: `public virtual Category Category { get; set; }` trong lớp `Product`). Thể hiện phía "một" của mối quan hệ.
            - **Collection Navigation Property:** Trỏ đến một tập hợp các entity liên quan (ví dụ: `public virtual ICollection<Product> Products { get; set; }` trong lớp `Category`). Thể hiện phía "nhiều" của mối quan hệ. Thường dùng kiểu `ICollection<T>`, `List<T>`, `HashSet<T>`.
        - **Foreign Key Property (Thuộc tính khóa ngoại):** Một thuộc tính trong entity phụ thuộc lưu trữ giá trị khóa chính của entity chính liên quan (ví dụ: `public int CategoryId { get; set; }` trong lớp `Product`).
    - EF Core sử dụng **quy ước (conventions)**, **data annotations**, hoặc **Fluent API** để phát hiện và cấu hình các mối quan hệ này dựa trên cách bạn định nghĩa các thuộc tính trên.
- One to Many Relationships
    - **Khái niệm:** Mối quan hệ phổ biến nhất. Một bản ghi ở bảng chính có thể liên quan đến nhiều bản ghi ở bảng phụ thuộc, nhưng một bản ghi ở bảng phụ thuộc chỉ liên quan đến một bản ghi duy nhất ở bảng chính.
    - **Ví dụ:** Một `Category` có thể có nhiều `Product`, nhưng một `Product` chỉ thuộc về một `Category`.
- Adding One-To-Many Relationships
    
    EF Core thường tự động phát hiện mối quan hệ này dựa trên quy ước đặt tên:
    
    - **Bước 1: Định nghĩa Entities với Navigation Properties và Foreign Key**
        
        ```
        // Models/Category.cs
        public class Category
        {
            public int Id { get; set; } // Khóa chính (PK) của Category
            public string Name { get; set; }
        
            // Collection Navigation Property (Phía "Nhiều")
            // Một Category có nhiều Products
            public virtual ICollection<Product> Products { get; set; } = new List<Product>();
        }
        
        // Models/Product.cs
        public class Product
        {
            public int Id { get; set; } // Khóa chính (PK) của Product
            public string Name { get; set; }
            public decimal Price { get; set; }
        
            // --- Phần quan trọng cho mối quan hệ ---
            // 1. Foreign Key Property (Khóa ngoại)
            //    Tên theo quy ước: [Tên Principal Navigation Property]Id => CategoryId
            public int CategoryId { get; set; }
        
            // 2. Reference Navigation Property (Phía "Một")
            //    Trỏ đến Category mà Product này thuộc về
            public virtual Category Category { get; set; }
            // --- Hết phần quan trọng ---
        }
        
        ```
        
        - **`virtual`**: Từ khóa `virtual` trên navigation properties là cần thiết nếu bạn muốn sử dụng **Lazy Loading** (sẽ nói sau). Ngay cả khi không dùng Lazy Loading, việc thêm `virtual` cũng không ảnh hưởng gì và là thực hành tốt.
    - **Bước 2: Cập nhật DbContext (nếu chưa có)**
    Đảm bảo cả `DbSet<Product>` và `DbSet<Category>` đều có trong `DbContext`.
        
        ```
        public class ApplicationDbContext : DbContext
        {
            // ... constructor ...
            public DbSet<Product> Products { get; set; }
            public DbSet<Category> Categories { get; set; }
        }
        
        ```
        
    - **Bước 3: (Tùy chọn) Cấu hình bằng Fluent API**
    Nếu quy ước không đủ hoặc bạn muốn tùy chỉnh (ví dụ: tên khóa ngoại khác, hành vi xóa...), bạn có thể dùng Fluent API trong `OnModelCreating` của `DbContext`:
        
        ```
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>() // Bắt đầu cấu hình cho Product
                .HasOne(p => p.Category) // Product có một Category (Reference Navigation)
                .WithMany(c => c.Products) // Category có nhiều Products (Collection Navigation)
                .HasForeignKey(p => p.CategoryId) // Chỉ định khóa ngoại là CategoryId
                .OnDelete(DeleteBehavior.Restrict); // (Ví dụ) Chỉ định hành vi khi xóa Category (sẽ nói sau)
                // .IsRequired(); // (Ví dụ) Mặc định là required nếu FK là non-nullable (int)
        }
        
        ```
        
- View Diagram with Entity Framework Core Tools
    
    Sau khi bạn tạo migration và cập nhật database (`dotnet ef database update`), cấu trúc bảng và các mối quan hệ (khóa ngoại) sẽ được tạo ra trong database. Để trực quan hóa các mối quan hệ này:
    
    - **SQL Server Management Studio (SSMS) / Azure Data Studio:** Nếu dùng SQL Server, bạn có thể kết nối tới database và sử dụng tính năng "Database Diagrams" để xem sơ đồ quan hệ giữa các bảng.
    - **EF Core Power Tools:** Đây là một extension rất hữu ích cho Visual Studio. Sau khi cài đặt, bạn có thể chuột phải vào project chứa `DbContext`, chọn "EF Core Power Tools" -> "View DbContext Model Diagram". Nó sẽ tạo ra một file DGML hiển thị các entities và mối quan hệ giữa chúng dựa trên model EF Core của bạn (ngay cả trước khi cập nhật database).
    
    Xem sơ đồ giúp bạn kiểm tra xem các mối quan hệ đã được định nghĩa đúng như mong đợi hay chưa.
    
- Many to Many Relationships
    - **Khái niệm:** Một bản ghi ở bảng A có thể liên quan đến nhiều bản ghi ở bảng B, và ngược lại, một bản ghi ở bảng B cũng có thể liên quan đến nhiều bản ghi ở bảng A.
    - **Ví dụ:** Một `Product` có thể có nhiều `Tag` (nhãn), và một `Tag` có thể được gán cho nhiều `Product`.
    - **Trong Database:** Mối quan hệ này thường được triển khai bằng một **bảng trung gian (Join Table)** chứa khóa ngoại tham chiếu đến cả hai bảng A và B.
- Adding Many-To-Many Relationships
    - **Cách tiếp cận (EF Core 5+ - Khuyến nghị): Skip Navigations**
    EF Core 5 trở lên hỗ trợ cấu hình M-M đơn giản hơn nhiều bằng cách chỉ cần định nghĩa Collection Navigation Properties ở cả hai phía. EF Core sẽ tự động tạo bảng join ẩn đi.
        - **Bước 1: Định nghĩa Entities với Collection Navigations**
            
            ```
            // Models/Product.cs (thêm phần cho Tag)
            public class Product
            {
                // ... các thuộc tính khác ...
                public int CategoryId { get; set; }
                public virtual Category Category { get; set; }
            
                // Collection Navigation Property tới Tags
                public virtual ICollection<Tag> Tags { get; set; } = new List<Tag>();
            }
            
            // Models/Tag.cs (mới)
            public class Tag
            {
                public int Id { get; set; }
                public string Name { get; set; }
            
                // Collection Navigation Property tới Products
                public virtual ICollection<Product> Products { get; set; } = new List<Product>();
            }
            
            ```
            
        - **Bước 2: Cập nhật DbContext**
        Thêm `DbSet<Tag>` vào `DbContext`.
            
            ```
            public class ApplicationDbContext : DbContext
            {
                // ... constructor ...
                public DbSet<Product> Products { get; set; }
                public DbSet<Category> Categories { get; set; }
                public DbSet<Tag> Tags { get; set; } // Thêm DbSet mới
            }
            
            ```
            
        - **Bước 3: (Thường không cần) Cấu hình Fluent API**
        Trong nhiều trường hợp, EF Core tự động phát hiện mối quan hệ M-M này và tạo bảng join (`ProductTag`) với các cột `ProductsId` và `TagsId`. Nếu cần tùy chỉnh tên bảng join hoặc tên cột, bạn có thể dùng Fluent API:
            
            ```
            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                // ... các cấu hình khác ...
            
                modelBuilder.Entity<Product>()
                    .HasMany(p => p.Tags) // Product có nhiều Tags
                    .WithMany(t => t.Products) // Tag có nhiều Products
                    .UsingEntity(j => j.ToTable("ProductTags")); // (Ví dụ) Tùy chỉnh tên bảng join
            }
            
            ```
            
    - **Cách tiếp cận (Trước EF Core 5 / Hoặc khi cần thêm thông tin vào bảng join): Explicit Join Entity**
    Bạn phải tự tạo một entity đại diện cho bảng join (ví dụ: `ProductTag`) và định nghĩa hai mối quan hệ One-to-Many riêng biệt từ `Product` và `Tag` tới `ProductTag`. Cách này phức tạp hơn nhưng cho phép bạn thêm các cột dữ liệu vào chính mối quan hệ đó (ví dụ: ngày gán tag).
- Understanding One-To-One Relationships
    - **Khái niệm:** Ít phổ biến hơn. Một bản ghi ở bảng A chỉ có thể liên quan đến tối đa một bản ghi ở bảng B, và ngược lại.
    - **Ví dụ:** Một `User` có một `UserProfile`. Một `Order` có một `ShippingAddress`.
    - **Triển khai:** Thường được thực hiện bằng cách:
        - Bảng phụ thuộc (dependent) có khóa ngoại trỏ đến khóa chính của bảng chính (principal).
        - Khóa ngoại này đồng thời cũng là khóa chính của bảng phụ thuộc (Shared Primary Key), HOẶC có một ràng buộc duy nhất (Unique Constraint) trên cột khóa ngoại đó.
- Adding One-To-One Relationships
    
    Mối quan hệ One-to-One thường cần cấu hình rõ ràng bằng Fluent API vì quy ước không đủ mạnh để xác định đâu là principal và dependent.
    
    - **Bước 1: Định nghĩa Entities với Reference Navigations**
        
        ```
        // Models/User.cs
        public class User
        {
            public int Id { get; set; }
            public string Username { get; set; }
        
            // Reference Navigation tới UserProfile
            public virtual UserProfile Profile { get; set; }
        }
        
        // Models/UserProfile.cs
        public class UserProfile
        {
            // Cách 1: Shared Primary Key (PK cũng là FK)
            public int Id { get; set; } // PK của UserProfile, cũng là FK trỏ tới User.Id
            public string FullName { get; set; }
            public string AvatarUrl { get; set; }
        
            // Reference Navigation tới User
            public virtual User User { get; set; }
        
            // Cách 2: Separate PK and FK (ít phổ biến hơn cho 1-1)
            // public int ProfileId { get; set; } // PK riêng
            // public int UserId { get; set; } // FK trỏ tới User.Id (cần Unique Constraint)
            // public string FullName { get; set; }
            // public virtual User User { get; set; }
        }
        
        ```
        
    - **Bước 2: Cập nhật DbContext**
    Thêm `DbSet<User>` và `DbSet<UserProfile>` vào `DbContext`.
    - **Bước 3: Cấu hình bằng Fluent API (Rất quan trọng)**
    Trong `OnModelCreating`, bạn cần chỉ rõ mối quan hệ và khóa ngoại.
        
        ```
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ... các cấu hình khác ...
        
            // Cấu hình cho mối quan hệ 1-1 User <-> UserProfile
            modelBuilder.Entity<User>()
                .HasOne(u => u.Profile) // User có một Profile
                .WithOne(p => p.User) // Profile thuộc về một User
                .HasForeignKey<UserProfile>(p => p.Id); // Chỉ định FK trong UserProfile là cột Id (Shared PK)
        
            // Nếu dùng cách 2 (Separate PK/FK):
            // modelBuilder.Entity<User>()
            //     .HasOne(u => u.Profile)
            //     .WithOne(p => p.User)
            //     .HasForeignKey<UserProfile>(p => p.UserId); // Chỉ định FK là UserId
            // // Cần thêm Unique Constraint cho UserId nếu nó không phải PK
            // modelBuilder.Entity<UserProfile>()
            //     .HasIndex(p => p.UserId)
            //     .IsUnique();
        }
        ```
        
- Update Database With Relationships
    
    Sau khi bạn đã định nghĩa các mối quan hệ trong model (1-Many, M-Many, 1-1) bằng cách thêm các navigation properties, foreign key properties và cấu hình Fluent API (nếu cần):
    
    1. **Tạo Migration:** Chạy `dotnet ef migrations add AddRelationships` (hoặc tên mô tả khác). EF Core sẽ phát hiện các mối quan hệ mới và tạo các lệnh SQL cần thiết (thêm cột khóa ngoại, tạo bảng join, tạo ràng buộc khóa ngoại...).
    2. **Áp dụng Migration:** Chạy `dotnet ef database update`. Database của bạn sẽ được cập nhật với các bảng và ràng buộc mới.
- Inserting Related Data
    
    Khi chèn dữ liệu có mối quan hệ, EF Core cung cấp nhiều cách linh hoạt:
    
    - **Cách 1: Gán trực tiếp Foreign Key Property** (Khi bạn biết ID của principal entity)
        
        ```
        var existingCategory = await context.Categories.FindAsync(1); // Giả sử Category Id=1 tồn tại
        if (existingCategory != null)
        {
            var newProduct = new Product
            {
                Name = "Product via FK",
                Price = 50,
                CategoryId = existingCategory.Id // Gán trực tiếp FK
            };
            context.Products.Add(newProduct);
            await context.SaveChangesAsync();
        }
        
        ```
        
    - **Cách 2: Gán Reference Navigation Property** (Cách thường dùng và rõ ràng hơn)
        
        ```
        var existingCategory = await context.Categories.FindAsync(1);
        if (existingCategory != null)
        {
            var newProduct = new Product
            {
                Name = "Product via Nav Prop",
                Price = 60,
                Category = existingCategory // Gán đối tượng Category liên quan
            };
            context.Products.Add(newProduct);
            await context.SaveChangesAsync();
            // EF Core sẽ tự động lấy existingCategory.Id để gán cho newProduct.CategoryId
        }
        
        ```
        
    - **Cách 3: Thêm vào Collection Navigation Property** (Hữu ích khi thêm dependent vào principal đã có)
        
        ```
        var existingCategory = await context.Categories.FindAsync(1); // Cần Include Products nếu muốn thấy ngay
        if (existingCategory != null)
        {
            var newProduct = new Product { Name = "Product via Collection", Price = 70 };
            existingCategory.Products.Add(newProduct); // Thêm vào collection của Category
            // EF Core sẽ tự động gán newProduct.CategoryId = existingCategory.Id
            await context.SaveChangesAsync();
        }
        
        ```
        
    - **Cách 4: Chèn Many-to-Many (Skip Navigations)**
        
        ```
        var product = await context.Products.FindAsync(101);
        var tag1 = await context.Tags.FirstOrDefaultAsync(t => t.Name == "New");
        var tag2 = await context.Tags.FirstOrDefaultAsync(t => t.Name == "Featured");
        
        if (product != null && tag1 != null && tag2 != null)
        {
            product.Tags.Add(tag1); // Thêm tag vào collection của product
            product.Tags.Add(tag2);
            // Hoặc: tag1.Products.Add(product);
            await context.SaveChangesAsync(); // EF Core sẽ tự động cập nhật bảng join
        }
        
        ```
        
    - **Cách 5: Chèn One-to-One**
    Thường bạn sẽ tạo cả hai đối tượng và gán navigation property cho nhau.
        
        ```
        var newUser = new User { Username = "johndoe" };
        var newUserProfile = new UserProfile
        {
            FullName = "John Doe",
            AvatarUrl = "/avatars/johndoe.png",
            User = newUser // Gán User cho Profile (hoặc ngược lại newUser.Profile = newUserProfile)
        };
        // Nếu dùng Shared PK, không cần gán Id cho UserProfile
        // Nếu dùng FK riêng, cần gán UserId nếu không gán User navigation property
        
        context.Users.Add(newUser);
        context.UserProfiles.Add(newUserProfile); // Hoặc chỉ cần add một cái nếu đã gán nav prop
        await context.SaveChangesAsync();
        
        ```
        
    
    **Quan trọng:** Dù dùng cách nào, cuối cùng bạn vẫn cần gọi `SaveChangesAsync()` để lưu tất cả các thay đổi (bao gồm cả việc tạo mối quan hệ) vào database.
    
- Understanding Loading Methods
    
    Khi bạn truy vấn một entity (ví dụ: `context.Categories.FindAsync(1)`), EF Core **không** tự động tải tất cả các entity liên quan của nó (ví dụ: các `Product` thuộc `Category` đó). Việc này giúp tránh tải quá nhiều dữ liệu không cần thiết.
    
    EF Core cung cấp 3 chiến lược chính để tải dữ liệu liên quan khi bạn cần:
    
    1. **Eager Loading (Tải Tham lam):** Tải dữ liệu liên quan **cùng lúc** với entity chính trong một query duy nhất.
    2. **Explicit Loading (Tải Tường minh):** Tải dữ liệu liên quan **sau khi** entity chính đã được tải, bằng một lệnh gọi tường minh.
    3. **Lazy Loading (Tải Lười biếng):** Tải dữ liệu liên quan **tự động** khi bạn truy cập vào navigation property lần đầu tiên.
    
    Việc chọn chiến lược nào phụ thuộc vào nhu cầu cụ thể của từng tình huống.
    
- Including Related Data with Eager Loading
    - **Cách thực hiện:** Sử dụng phương thức `Include()` (và `ThenInclude()` cho các cấp độ sâu hơn) trong câu lệnh LINQ query ban đầu.
    - **Mục đích:** Chỉ định rõ ràng những navigation property nào cần được tải cùng với entity chính.
    - **Cú pháp:**
        
        ```
        // Tải tất cả Categories và các Products liên quan của chúng
        var categoriesWithProducts = await context.Categories
                                                  .Include(c => c.Products) // Tải Products
                                                  .ToListAsync();
        
        // Tải Product có Id=1, kèm theo Category của nó
        var productWithCategory = await context.Products
                                               .Include(p => p.Category) // Tải Category
                                               .FirstOrDefaultAsync(p => p.Id == 1);
        
        // Tải Categories, kèm Products, và kèm cả Tags của từng Product đó (M-M)
        var categoriesDeep = await context.Categories
                                          .Include(c => c.Products) // Tải Products
                                              .ThenInclude(p => p.Tags) // Từ Products, tải tiếp Tags
                                          .ToListAsync();
        
        // Tải nhiều mối quan hệ cùng cấp
        var productComplex = await context.Products
                                          .Include(p => p.Category) // Tải Category
                                          .Include(p => p.Tags) // Tải Tags
                                          .FirstOrDefaultAsync(p => p.Id == 1);
        
        ```
        
    - **Ưu điểm:**
        - Tất cả dữ liệu cần thiết được tải trong một (hoặc ít) query, giảm thiểu số lượt round-trip đến database.
        - Kiểm soát rõ ràng dữ liệu nào được tải.
    - **Nhược điểm:**
        - Nếu `Include` quá nhiều hoặc các mối quan hệ phức tạp, câu lệnh SQL được tạo ra có thể trở nên rất lớn và phức tạp (Cartesian Explosion), ảnh hưởng đến hiệu năng.
        - Có thể tải về dữ liệu không thực sự cần thiết nếu chỉ dùng một phần nhỏ của entity liên quan.
    - **Khi nào dùng:** Khi bạn biết chắc chắn mình sẽ cần dữ liệu liên quan ngay sau khi tải entity chính. Đây là cách phổ biến và thường được khuyến nghị nhất.
- Including Related Data with Explicit Loading
    - **Cách thực hiện:** Sau khi đã tải entity chính, sử dụng `DbContext.Entry(entity)` để truy cập `ChangeTracker` và gọi phương thức `LoadAsync()` (hoặc `Load()`) trên `Collection()` hoặc `Reference()`.
    - **Mục đích:** Tải dữ liệu liên quan một cách tường minh theo yêu cầu, sau khi entity chính đã nằm trong context.
    - **Cú pháp:**
        
        ```
        // Bước 1: Tải entity chính (không Include)
        var category = await context.Categories.FindAsync(1);
        
        if (category != null)
        {
            // Bước 2: Tải tường minh collection Products liên quan (nếu chưa được tải)
            bool productsLoaded = context.Entry(category).Collection(c => c.Products).IsLoaded;
            if (!productsLoaded)
            {
                await context.Entry(category).Collection(c => c.Products).LoadAsync();
                // Giờ đây category.Products đã chứa dữ liệu
            }
        
            // Tải tường minh reference Category của một Product
            var product = await context.Products.FindAsync(101);
            if (product != null)
            {
                bool categoryLoaded = context.Entry(product).Reference(p => p.Category).IsLoaded;
                if (!categoryLoaded)
                {
                    await context.Entry(product).Reference(p => p.Category).LoadAsync();
                    // Giờ đây product.Category đã chứa dữ liệu
                }
            }
        
            // Có thể lọc khi tải tường minh
            await context.Entry(category)
                         .Collection(c => c.Products)
                         .Query() // Lấy IQueryable cho collection
                         .Where(p => p.Price > 100) // Áp dụng bộ lọc
                         .LoadAsync(); // Chỉ tải các product khớp điều kiện
        }
        
        ```
        
    - **Ưu điểm:**
        - Cho phép tải dữ liệu liên quan một cách có điều kiện, chỉ khi thực sự cần.
        - Tránh các query lớn ban đầu như Eager Loading nếu chỉ cần dữ liệu liên quan trong một số trường hợp.
    - **Nhược điểm:**
        - Yêu cầu thêm một lượt round-trip đến database cho mỗi lần gọi `LoadAsync()`.
        - Code dài dòng hơn Eager Loading.
    - **Khi nào dùng:** Khi bạn không chắc chắn có cần dữ liệu liên quan hay không, hoặc chỉ cần nó trong một số điều kiện nhất định sau khi entity chính đã được xử lý.
- Including Related Data with Lazy Loading
    - **Cách thực hiện:** Dữ liệu liên quan được tải tự động từ database vào **lần đầu tiên** bạn truy cập vào navigation property đó trong code.
    - **Cài đặt:**
        1. **Cài đặt package:** `Microsoft.EntityFrameworkCore.Proxies`
        2. **Bật trong DbContext:** Gọi `optionsBuilder.UseLazyLoadingProxies()` khi cấu hình `DbContextOptions`.
        3. **`Navigation Properties phải là public và virtual:`** Điều này cho phép EF Core tạo ra các lớp proxy kế thừa từ entity của bạn để ghi đè các navigation property và thêm logic tải lười biếng vào đó.
            
            ```
            // Trong DbContext configuration (Program.cs hoặc OnConfiguring)
            optionsBuilder.UseLazyLoadingProxies();
            
            // Trong Entity (ví dụ Category)
            public class Category
            {
                public int Id { get; set; }
                public string Name { get; set; }
                // PHẢI LÀ VIRTUAL
                public virtual ICollection<Product> Products { get; set; } = new List<Product>();
            }
            
            ```
            
    - **Sử dụng:**
        
        ```
        // Tải category mà không Include Products
        var category = await context.Categories.FindAsync(1);
        
        if (category != null)
        {
            Console.WriteLine($"Category: {category.Name}");
            // LẦN ĐẦU TIÊN truy cập category.Products:
            // EF Core sẽ tự động chạy một query ngầm để tải các Product liên quan
            foreach (var product in category.Products) // <--- Query ngầm xảy ra ở đây!
            {
                Console.WriteLine($"  - Product: {product.Name}");
            }
        }
        
        ```
        
    - **Ưu điểm:**
        - Rất tiện lợi, code trông gọn gàng vì không cần `Include` hay `LoadAsync`.
        - Chỉ tải dữ liệu khi thực sự truy cập đến nó.
    - **Nhược điểm (Rất nguy hiểm):**
        - **N+1 Problem:** Nếu bạn lặp qua một danh sách các entity chính và trong vòng lặp lại truy cập vào navigation property (được lazy load) của từng entity, bạn sẽ tạo ra 1 query ban đầu để lấy danh sách chính + N query ngầm tiếp theo (mỗi query cho một entity trong vòng lặp). Điều này cực kỳ không hiệu quả và có thể làm chậm ứng dụng nghiêm trọng.
            
            ```
            // **VÍ DỤ VỀ N+1 PROBLEM VỚI LAZY LOADING**
            var categories = await context.Categories.ToListAsync(); // Query 1: Lấy tất cả Categories
            foreach (var cat in categories)
            {
                Console.WriteLine($"Category: {cat.Name}");
                // Dòng dưới sẽ chạy 1 query riêng cho MỖI category -> N queries!
                foreach (var prod in cat.Products)
                {
                    Console.WriteLine($"  - Product: {prod.Name}");
                }
            }
            // Tổng cộng: 1 + N queries!
            
            ```
            
        - **Khó kiểm soát/debug:** Các query ngầm có thể khó phát hiện và gây ra các vấn đề hiệu năng không mong muốn.
        - **`Yêu cầu virtual:`** Có thể ảnh hưởng đến thiết kế class của bạn.
        - Cần `DbContext` còn "sống" (chưa bị dispose) khi truy cập navigation property.
    - **Khuyến nghị:** **Sử dụng Lazy Loading một cách cực kỳ cẩn thận.** Hiểu rõ khi nào nó sẽ kích hoạt query. Tránh sử dụng trong các vòng lặp hoặc các kịch bản nhạy cảm về hiệu năng. **Eager Loading hoặc Explicit Loading thường là lựa chọn an toàn và dễ kiểm soát hơn.**
- Filtering on Related Records
    
    Bạn có thể dễ dàng lọc các entity chính dựa trên dữ liệu của các entity liên quan bằng cách sử dụng navigation properties trong mệnh đề `Where()`.
    
    ```
    // Lấy các Categories mà CÓ ÍT NHẤT một Product giá > 1000
    var categoriesWithExpensiveProducts = await context.Categories
        .Where(c => c.Products.Any(p => p.Price > 1000)) // Dùng Any() để kiểm tra sự tồn tại
        .ToListAsync();
    
    // Lấy các Categories mà TẤT CẢ Products đều IsAvailable = true
    var categoriesWithAllAvailable = await context.Categories
        .Where(c => c.Products.All(p => p.IsAvailable)) // Dùng All() để kiểm tra tất cả
        .ToListAsync();
    
    // Lấy các Categories có ĐÚNG 5 Products
    var categoriesWithFiveProducts = await context.Categories
        .Where(c => c.Products.Count() == 5) // Dùng Count()
        .ToListAsync();
    
    // Lấy các Products thuộc về Category có tên là "Electronics"
    var electronicProducts = await context.Products
        .Where(p => p.Category.Name == "Electronics") // Truy cập thuộc tính của nav prop phía "Một"
        .ToListAsync();
    
    // Lấy các Products có ít nhất một Tag tên là "Featured" (M-M)
    var featuredProducts = await context.Products
        .Where(p => p.Tags.Any(t => t.Name == "Featured"))
        .ToListAsync();
    
    ```
    
    EF Core đủ thông minh để dịch các biểu thức LINQ này thành các câu lệnh SQL hiệu quả, thường sử dụng `JOIN` hoặc `EXISTS` / `IN` tùy thuộc vào query.
    
- Projections and Anonymous Data Types
    
    Như đã học ở phần truy vấn, `Select()` rất hữu ích để chỉ lấy các cột cần thiết. Điều này càng quan trọng hơn khi làm việc với dữ liệu liên quan, giúp tránh tải toàn bộ các entity liên quan không cần thiết.
    
    ```
    // Chỉ lấy Id, Tên Product và Tên Category liên quan
    var productSummaries = await context.Products
        .Select(p => new // Tạo anonymous type hoặc DTO
        {
            ProductId = p.Id,
            ProductName = p.Name,
            CategoryName = p.Category.Name // Truy cập nav prop trong Select
        })
        .ToListAsync();
    
    // Lấy tên Category và số lượng Product trong mỗi Category
    var categoryCounts = await context.Categories
        .Select(c => new
        {
            CategoryName = c.Name,
            ProductCount = c.Products.Count() // Dùng Count() trên collection nav prop
        })
        .ToListAsync();
    
    ```
    
    Sử dụng projection giúp tối ưu hóa đáng kể việc truy vấn dữ liệu liên quan bằng cách giảm lượng dữ liệu truyền tải.
    
- Understanding Delete Behaviors
    
    Khi bạn xóa một entity chính (principal), điều gì sẽ xảy ra với các entity phụ thuộc (dependent) liên quan đến nó? EF Core cho phép bạn cấu hình hành vi này thông qua `OnDelete()` trong Fluent API.
    
    - **Các hành vi chính:**
        - **`DeleteBehavior.Cascade`**: Khi entity chính bị xóa, tất cả các entity phụ thuộc liên quan cũng **tự động bị xóa** theo.
            - Đây là **mặc định** cho các mối quan hệ **bắt buộc (required)** (khi khóa ngoại là non-nullable, ví dụ `int CategoryId`).
            - Cẩn thận khi dùng vì có thể xóa dữ liệu không mong muốn.
        - **`DeleteBehavior.Restrict`**: **Ngăn chặn** việc xóa entity chính nếu nó vẫn còn các entity phụ thuộc liên quan. EF Core sẽ ném ra một Exception.
            - Đây là **mặc định** cho các mối quan hệ **tùy chọn (optional)** (khi khóa ngoại là nullable, ví dụ `int? CategoryId`).
            - An toàn hơn Cascade, buộc bạn phải xử lý (ví dụ: xóa hoặc cập nhật FK) các bản ghi phụ thuộc trước khi xóa bản ghi chính.
        - **`DeleteBehavior.SetNull`**: Khi entity chính bị xóa, giá trị khóa ngoại trong các entity phụ thuộc liên quan sẽ được **`cập nhật thành NULL`**.
            - Chỉ áp dụng cho các mối quan hệ **tùy chọn** (khóa ngoại phải là nullable).
        - **`DeleteBehavior.ClientSetNull`**: Tương tự `SetNull`, nhưng EF Core sẽ tự động cập nhật giá trị khóa ngoại thành `null` cho các entity phụ thuộc *đang được theo dõi (tracked)* trong `DbContext` **trước khi** gửi lệnh `DELETE` của entity chính đến database. Hành vi này ít phổ biến hơn và thường dùng `SetNull` hoặc `Restrict`.
    - **Cấu hình (Fluent API):**
        
        ```
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict); // Ví dụ: Không cho xóa Category nếu còn Product
        
            // Ví dụ cho SetNull (giả sử khóa ngoại là nullable: int? OrderId)
            // modelBuilder.Entity<OrderItem>()
            //     .HasOne(oi => oi.Order)
            //     .WithMany(o => o.OrderItems)
            //     .HasForeignKey(oi => oi.OrderId) // OrderId phải là nullable: int?
            //     .OnDelete(DeleteBehavior.SetNull); // Khi xóa Order, OrderId trong OrderItem thành NULL
        }
        
        ```
        
    - Việc hiểu và cấu hình đúng Delete Behavior là rất quan trọng để đảm bảo tính toàn vẹn dữ liệu của bạn.
- Section Review
    - EF Core mô hình hóa mối quan hệ database bằng Navigation Properties (Reference & Collection) và Foreign Key Properties.
    - Nắm vững cách định nghĩa các mối quan hệ phổ biến: One-to-Many, Many-to-Many (ưu tiên Skip Navigation trong EF Core 5+), One-to-One (thường cần Fluent API).
    - Luôn tạo và áp dụng migrations sau khi thay đổi model để cập nhật schema database.
    - Có nhiều cách để chèn dữ liệu liên quan (gán FK, gán Nav Prop, thêm vào Collection).
    - Hiểu rõ 3 chiến lược tải dữ liệu liên quan và chọn đúng cách:
        - **`Eager Loading (Include/ThenInclude):`** Tải cùng lúc, kiểm soát tốt, phổ biến nhất.
        - **`Explicit Loading (LoadAsync):`** Tải theo yêu cầu sau khi entity chính đã được tải.
        - **Lazy Loading (Proxies):** Tải tự động khi truy cập, tiện lợi nhưng tiềm ẩn nguy cơ N+1 problem.
    - Sử dụng navigation properties để lọc và chiếu (project) dữ liệu liên quan một cách hiệu quả.
    - Cấu hình đúng Delete Behavior (`Cascade`, `Restrict`, `SetNull`) để kiểm soát việc xóa dữ liệu liên quan và đảm bảo toàn vẹn dữ liệu.
- Section Source Code