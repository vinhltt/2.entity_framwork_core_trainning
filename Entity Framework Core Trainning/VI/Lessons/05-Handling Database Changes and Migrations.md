# Handling Database Changes and Migrations

**Handling Database Changes and Migrations**

- **Section Overview**
    - Trong section này, chúng ta sẽ tìm hiểu cách quản lý và áp dụng các thay đổi cấu trúc database một cách an toàn và có kiểm soát thông qua EF Core Migrations.
    - Các chủ đề chính bao gồm:
        - Tìm hiểu về EF Core Migrations và cách hoạt động
        - Thêm entities mới và cập nhật database với migrations
        - Sử dụng configuration files để quản lý connection strings
        - Tạo migration scripts cho các môi trường khác nhau
        - Hoàn tác migrations và thay đổi database
    - Mục tiêu của section này là giúp bạn:
        - Hiểu và sử dụng hiệu quả EF Core Migrations
        - Quản lý các thay đổi schema database một cách an toàn
        - Áp dụng migrations vào các môi trường khác nhau
        - Tạo và sử dụng migration scripts
        - Xử lý các tình huống cần hoàn tác migrations
- Review Entity Framework Core Migrations
    
    Hãy cùng nhắc lại nhanh những điểm cốt lõi về Migrations mà chúng ta đã đề cập sơ qua:
    
    - **Mục đích:** Là cơ chế của EF Core để quản lý các thay đổi tuần tự đối với schema database, đồng bộ với những thay đổi trong code model (Entities và DbContext).
    - **Luồng làm việc cơ bản (Code First):**
        1. Thay đổi code model (thêm/sửa/xóa entity, property, relationship...).
        2. Chạy lệnh `dotnet ef migrations add [TenMigration ]` trong terminal tại thư mục project. Lệnh này so sánh model hiện tại với snapshot cuối cùng và tạo ra một file migration mới.
        3. Chạy lệnh `dotnet ef database update` để áp dụng các thay đổi trong migration (thực thi phương thức `Up()`) vào database được cấu hình.
    - **Các thành phần chính:**
        - **`Migration Files (Migrations/[Timestamp]_[TenMigration].cs):`** Chứa code C# với phương thức `Up()` (để áp dụng thay đổi) và `Down()` (để hoàn tác thay đổi).
        - **`Model Snapshot (Migrations/[DbContextName]ModelSnapshot.cs):`** Là "ảnh chụp" trạng thái model của bạn sau khi áp dụng migration cuối cùng. EF Core dùng file này để so sánh và tạo ra migration tiếp theo. **Không nên sửa file này thủ công.**
        - **`Table history (__EFMigrationsHistory):`** Một bảng đặc biệt được EF Core tự động tạo trong database của bạn để theo dõi những migration nào đã được áp dụng thành công. `database update` dựa vào bảng này để biết cần chạy migration nào tiếp theo.
- Adding More Entities and Updating Database with Migration(s)
    
    Hãy xem một ví dụ thực tế. Giả sử ban đầu chúng ta chỉ có entity `Product`:
    
    ```
    // Models/Product.cs (ban đầu)
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
    }
    
    // Data/ApplicationDbContext.cs (ban đầu)
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        public DbSet<Product> Products { get; set; }
    }
    
    ```
    
    Bây giờ, chúng ta muốn thêm `Category` và tạo mối quan hệ một-nhiều (một Category có nhiều Products).
    
    - **Bước 1: Cập nhật Model và DbContext**
        - Tạo entity `Category`:
            
            ```
            // Models/Category.cs (mới)
            public class Category
            {
                public int Id { get; set; }
                public string Name { get; set; }
                public string Description { get; set; }
            
                // Navigation property: Một Category có nhiều Products
                public virtual ICollection<Product> Products { get; set; } = new List<Product>();
            }
            
            ```
            
        - Cập nhật `Product` để có khóa ngoại và navigation property tới `Category`:
            
            ```
            // Models/Product.cs (cập nhật)
            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
                public decimal Price { get; set; }
            
                // Khóa ngoại tới Category
                public int CategoryId { get; set; }
                // Navigation property: Một Product thuộc về một Category
                public virtual Category Category { get; set; }
            }
            
            ```
            
        - Thêm `DbSet<Category>` vào `DbContext`:
            
            ```
            // Data/ApplicationDbContext.cs (cập nhật)
            public class ApplicationDbContext : DbContext
            {
                public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
                public DbSet<Product> Products { get; set; }
                public DbSet<Category> Categories { get; set; } // Thêm DbSet mới
            
                // (Tùy chọn) Cấu hình mối quan hệ bằng Fluent API nếu cần
                // protected override void OnModelCreating(ModelBuilder modelBuilder) { ... }
            }
            
            ```
            
    - **Bước 2: Tạo Migration mới**
        - Mở terminal tại thư mục gốc của project.
        - Chạy lệnh:
            
            ```
            dotnet ef migrations add AddCategoryAndRelation
            
            ```
            
        - EF Core sẽ build project, so sánh model mới (có `Category` và mối quan hệ) với `ModelSnapshot.cs` cũ (chỉ có `Product`).
        - Nó sẽ tạo ra một file migration mới (ví dụ: `Migrations/20250411102000_AddCategoryAndRelation.cs`) chứa các lệnh như:
            - `migrationBuilder.CreateTable("Categories", ...)`
            - `migrationBuilder.AddColumn<int>("CategoryId", "Products", ...)`
            - `migrationBuilder.CreateIndex(...)`
            - `migrationBuilder.AddForeignKey(...)`
        - Nó cũng cập nhật file `ModelSnapshot.cs` để phản ánh trạng thái mới của model.
        - **`Luôn kiểm tra file migration .cs`** để đảm bảo nó đúng ý định của bạn.
    - **Bước 3: Áp dụng Migration vào Database**
        - Chạy lệnh:
            
            ```
            dotnet ef database update
            
            ```
            
        - EF Core sẽ kiểm tra bảng `__EFMigrationsHistory`, thấy migration `AddCategoryAndRelation` chưa được áp dụng.
        - Nó sẽ thực thi phương thức `Up()` trong file migration đó, tạo bảng `Categories`, thêm cột `CategoryId` và khóa ngoại vào bảng `Products` trong database của bạn.
        - Cuối cùng, nó ghi tên migration `AddCategoryAndRelation` vào bảng `__EFMigrationsHistory`.
    
    Vậy là database của bạn đã được cập nhật để khớp với model mới!
    
- Using Configuration Files
    - **Connection String:** Các lệnh `dotnet ef` (như `database update`, `migrations add`) cần biết phải kết nối tới database nào. Chúng thường tìm connection string theo các cách sau (ưu tiên từ trên xuống):
        1. Tham số dòng lệnh (ví dụ: `-connection "..."` - ít dùng).
        2. User Secrets (trong môi trường Development).
        3. Environment Variables.
        4. File `appsettings.Development.json` (nếu môi trường là Development).
        5. File `appsettings.json`.
        6. Trong phương thức `OnConfiguring` của `DbContext` (nếu có và không được cấu hình từ bên ngoài).
        7. Thông qua `IDesignTimeDbContextFactory<T>`: Nếu project của bạn phức tạp (ví dụ: DbContext ở project khác với startup project), bạn có thể cần tạo một class implement interface này để chỉ cho EF tools cách tạo `DbContext` và lấy connection string lúc design time.
    - **Environments:** Biến môi trường `ASPNETCORE_ENVIRONMENT` (ví dụ: `Development`, `Staging`, `Production`) ảnh hưởng đến việc file `appsettings.[Environment].json` nào được ưu tiên. Điều này cho phép bạn dễ dàng áp dụng migrations vào các database khác nhau (dev DB, test DB, production DB) bằng cách chỉ định đúng connection string cho từng môi trường và chạy lệnh `dotnet ef database update` trong môi trường tương ứng.
- Generating Migration Scripts
    
    Đôi khi, bạn không thể hoặc không muốn chạy `dotnet ef database update` trực tiếp trên một môi trường (đặc biệt là Production). Thay vào đó, bạn có thể tạo ra một script SQL chứa tất cả các lệnh cần thiết để cập nhật database.
    
    - **Lệnh:**
        
        ```
        dotnet ef migrations script [FromMigration] [ToMigration] -o output.sql --idempotent
        
        ```
        
        - `[FromMigration]` (Tùy chọn): Migration bắt đầu. Nếu bỏ qua, mặc định là `0` (trạng thái database trống).
        - `[ToMigration]` (Tùy chọn): Migration cuối cùng muốn áp dụng. Nếu bỏ qua, mặc định là migration mới nhất trong project.
        - `o output.sql` hoặc `-output output.sql`: Lưu script vào file `output.sql`. Nếu không có, script sẽ in ra màn hình console.
        - `-idempotent`: **Rất quan trọng!** Tùy chọn này tạo ra một script "idempotent", nghĩa là script có thể chạy nhiều lần mà không gây lỗi. Nó sẽ kiểm tra bảng `__EFMigrationsHistory` trước khi thực thi lệnh của mỗi migration, đảm bảo chỉ chạy những migration chưa được áp dụng. **Luôn nên dùng tùy chọn này khi tạo script cho production.**
    - **Ví dụ:**
        - Tạo script cho tất cả migrations từ đầu đến mới nhất:
            
            ```
            dotnet ef migrations script -o full_migration.sql --idempotent
            
            ```
            
        - Tạo script từ migration `AddCategoryAndRelation` đến migration mới nhất:
            
            ```
            dotnet ef migrations script AddCategoryAndRelation -o incremental_migration.sql --idempotent
            
            ```
            
    - **Sử dụng:** Script SQL này có thể được:
        - Review bởi DBA (Quản trị viên CSDL).
        - Chạy thủ công bằng các công cụ quản lý database (như SQL Server Management Studio, Azure Data Studio, psql...).
        - Tích hợp vào quy trình CI/CD để tự động triển khai thay đổi database.
- Rolling Back Migrations and Database Changes
    
    Nếu bạn phát hiện ra lỗi sau khi áp dụng migration hoặc đơn giản là muốn quay lại trạng thái schema trước đó, có hai cách chính:
    
    - **`Cách 1: Hoàn tác Database về Migration trước đó (Dùng database update)`**
        - Lệnh này sẽ thực thi phương thức `Down()` của các migration cần thiết để đưa schema database về trạng thái của migration mục tiêu.
        - **Lệnh:**
            
            ```
            dotnet ef database update [TenMigrationMucTieu]
            
            ```
            
            - `[TenMigrationMucTieu]`: Tên của migration mà bạn muốn database quay về trạng thái **sau khi** migration đó đã được áp dụng.
            - Để hoàn tác tất cả các migration và đưa database về trạng thái trống (schema ban đầu), dùng tên migration là `0`:
                
                ```
                dotnet ef database update 0
                
                ```
                
        - **Ví dụ:** Nếu bạn có `MigrationA`, `MigrationB`, `MigrationC` đã áp dụng, chạy `dotnet ef database update MigrationA` sẽ thực thi `Down()` của `MigrationC` và `MigrationB`.
        - **Lưu ý:** Phương thức `Down()` phải được viết đúng để hoàn tác chính xác các thay đổi của `Up()`. EF Core tự tạo `Down()` khá tốt cho các thao tác cơ bản, nhưng với các thay đổi phức tạp hoặc tùy chỉnh trong `Up()`, bạn cần kiểm tra kỹ `Down()`. Việc hoàn tác có thể làm mất dữ liệu nếu `Down()` bao gồm lệnh `DROP TABLE` hoặc `DROP COLUMN`.
    - **`Cách 2: Xóa Migration cuối cùng khỏi Project (Dùng migrations remove)`**
        - **Lệnh:**
            
            ```
            dotnet ef migrations remove
            
            ```
            
        - **Tác dụng:** Lệnh này **`chỉ xóa file migration .cs cuối cùng`** khỏi thư mục `Migrations` và **`cập nhật lại file ModelSnapshot.cs`** về trạng thái của migration trước đó.
        - **CẢNH BÁO QUAN TRỌNG:** Lệnh này **KHÔNG THAY ĐỔI DATABASE CỦA BẠN**. Nó chỉ thay đổi code trong project.
        - **Khi nào dùng?** Chỉ nên dùng lệnh này khi:
            1. Bạn vừa chạy `migrations add` và nhận ra migration đó bị sai hoặc bạn muốn thay đổi model thêm nữa *trước khi* tạo migration cuối cùng.
            2. Migration cuối cùng đó **chưa bao giờ được áp dụng** vào bất kỳ database nào (kể cả dev DB), HOẶC bạn đã **hoàn tác (rollback) database** về trạng thái trước migration đó bằng `database update [MigrationTruocDo]`.
        - **`KHÔNG BAO GIỜ dùng migrations remove nếu migration đó đã được áp dụng vào database (đặc biệt là production hoặc database dùng chung) mà bạn không thể hoặc không muốn rollback database đó.`** Việc này sẽ làm mất đồng bộ giữa code migrations và trạng thái thực tế của database, gây lỗi nghiêm trọng sau này.
- EF Bundles
    
    EF Bundles là một cách đóng gói migrations của bạn thành một file thực thi duy nhất, giúp đơn giản hóa việc triển khai.
    
    - **Khái niệm:** Một file thực thi nhỏ gọn chứa tất cả migrations của bạn và đủ logic EF Core cần thiết để áp dụng chúng vào database.
    - **Lợi ích:**
        - Không cần cài đặt .NET SDK hoặc EF Core tools trên server đích.
        - Dễ dàng tích hợp vào CI/CD pipeline.
        - Triển khai đơn giản hơn: chỉ cần copy file bundle và chạy nó.
    - **Tạo Bundle:**
        
        ```
        # Tạo bundle mặc định (thường cho HĐH hiện tại)
        dotnet ef migrations bundle -o ./efbundle --force --verbose
        
        # Tạo bundle cho một runtime cụ thể (ví dụ: Linux x64)
        # dotnet ef migrations bundle -o ./efbundle-linux-x64 --runtime linux-x64 --force --verbose
        
        ```
        
        - `o ./efbundle`: Đặt tên và đường dẫn cho file bundle output.
        - `-force`: Ghi đè nếu file bundle đã tồn tại.
        - `-verbose`: Hiển thị log chi tiết.
        - `-runtime`: (Tùy chọn) Chỉ định Runtime Identifier (RID) nếu muốn tạo bundle cho HĐH/kiến trúc khác.
    - **Sử dụng Bundle:**
        - Copy file bundle (ví dụ: `efbundle` hoặc `efbundle.exe`) lên server đích.
        - Chạy file bundle từ command line, truyền vào connection string:
            
            ```
            # Linux/macOS
            ./efbundle --connection "Server=your_server;Database=your_db;User ID=user;Password=pass;"
            
            # Windows
            .\efbundle.exe --connection "Server=your_server;Database=your_db;User ID=user;Password=pass;"
            
            ```
            
        - Bundle sẽ tự động kết nối tới database và áp dụng các migration còn thiếu, tương tự như `dotnet ef database update`.
- Applying Migrations at Runtime
    
    Bạn cũng có thể áp dụng migrations một cách tự động khi ứng dụng khởi động bằng cách gọi phương thức `dbContext.Database.MigrateAsync()` (hoặc `Migrate()` cho bản đồng bộ).
    
    - **`Cách thực hiện (Ví dụ trong Program.cs của ASP.NET Core):`**
        
        ```
        // Program.cs
        var builder = WebApplication.CreateBuilder(args);
        // ... services ...
        var app = builder.Build();
        
        // **Lấy DbContext instance từ service provider**
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            try
            {
                var dbContext = services.GetRequiredService<ApplicationDbContext>();
                // **Áp dụng migrations**
                await dbContext.Database.MigrateAsync(); // Hoặc dbContext.Database.Migrate();
                Console.WriteLine("Migrations applied successfully.");
        
                // (Tùy chọn) Có thể thêm seeding data ở đây sau khi migrate
                // await SeedData.InitializeAsync(services);
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred while migrating or seeding the database.");
                // Cân nhắc việc dừng ứng dụng nếu migration thất bại nghiêm trọng
                // throw;
            }
        }
        
        // ... configure pipeline ...
        app.Run();
        
        ```
        
    - **Ưu điểm:**
        - Đơn giản cho các kịch bản triển khai đơn giản (single-instance application).
        - Đảm bảo database luôn được cập nhật schema mới nhất khi ứng dụng khởi động.
    - **Nhược điểm và Rủi ro (Rất quan trọng):**
        - **Không phù hợp cho môi trường multi-instance:** Nếu bạn chạy nhiều instance của ứng dụng (ví dụ: web farm, Kubernetes pods, serverless functions), các instance có thể cùng lúc cố gắng chạy `MigrateAsync()`, dẫn đến xung đột (race condition), lỗi hoặc thậm chí làm hỏng database.
        - **Khởi động chậm hoặc thất bại:** Nếu quá trình migration mất nhiều thời gian hoặc gặp lỗi, ứng dụng sẽ khởi động chậm hoặc không thể khởi động được.
        - **Vấn đề phân quyền:** Tài khoản người dùng mà ứng dụng chạy cần có quyền thay đổi schema database (ALTER, CREATE, DROP...), điều này có thể không mong muốn hoặc không an toàn trong môi trường production.
    - **Khuyến nghị:** **Tránh áp dụng migrations tự động lúc runtime trong môi trường production hoặc các môi trường có nhiều instance.** Thay vào đó, hãy coi việc áp dụng migration là một **bước riêng biệt trong quy trình triển khai (deployment)** của bạn, sử dụng `dotnet ef database update`, SQL scripts, hoặc EF Bundles. Việc áp dụng runtime chỉ nên cân nhắc cho môi trường development hoặc các ứng dụng đơn giản, chạy đơn lẻ.
- Section Review
    - Migrations là công cụ thiết yếu để quản lý sự thay đổi schema database một cách có hệ thống và đồng bộ với code.
    - Luôn tạo migration (`migrations add`) sau khi thay đổi model và áp dụng (`database update`) để cập nhật database.
    - Sử dụng script SQL (`migrations script --idempotent`) cho việc review hoặc triển khai thủ công/tự động hóa.
    - Hiểu rõ cách hoàn tác (`database update [Target]`) và khi nào nên/không nên dùng `migrations remove`.
    - EF Bundles (`migrations bundle`) là cách đóng gói và triển khai migrations tiện lợi, đặc biệt trong CI/CD.
    - Áp dụng migrations lúc runtime (`Database.MigrateAsync()`) tiện lợi nhưng tiềm ẩn rủi ro trong môi trường production và multi-instance; nên thực hiện migration như một bước triển khai riêng biệt.
- Section Source Code
