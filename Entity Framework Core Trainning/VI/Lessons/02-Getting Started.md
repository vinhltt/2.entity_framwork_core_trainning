# Getting Started

**Getting Started with Entity Framework Core**

- **Section Overview**
    - Trong section này, chúng ta sẽ tìm hiểu những kiến thức cơ bản để bắt đầu làm việc với Entity Framework Core, một công cụ mạnh mẽ và phổ biến trong hệ sinh thái .NET để làm việc với cơ sở dữ liệu.
    - Các chủ đề chính bao gồm:
        - Tìm hiểu về Data Models và cách tạo chúng với EF Core
        - Hiểu về Database Context và vai trò của nó
        - Cấu hình kết nối database và chọn provider phù hợp
        - Làm quen với Code First Development và Migrations
        - Thiết lập project Console App để thực hành EF Core
    - Mục tiêu của section này là giúp bạn:
        - Nắm được các khái niệm cơ bản về Data Models và Database Context
        - Biết cách cấu hình và kết nối database với EF Core
        - Hiểu được quy trình Code First Development
        - Có thể tạo và quản lý Migrations
        - Thiết lập được môi trường phát triển với EF Core

Đây là một công cụ cực kỳ mạnh mẽ và phổ biến trong hệ sinh thái .NET để làm việc với cơ sở dữ liệu. 

- What are Data Models?
    - **Khái niệm:** Trong ngữ cảnh của EF Core (và lập trình nói chung), Data Models (Mô hình dữ liệu) là các **lớp (class) C#** mà bạn tạo ra để **đại diện cho các cấu trúc dữ liệu** mà bạn muốn lưu trữ hoặc truy xuất từ cơ sở dữ liệu.
    - **Ví dụ:** Nếu bạn có một bảng `Products` trong database với các cột `Id`, `Name`, `Price`, thì bạn sẽ tạo một class `Product` trong C# với các thuộc tính (properties) tương ứng: `Id`, `Name`, `Price`.
        
        ```csharp
        public class Product
        {
        		public int Id { get; set; } // Thường là khóa chính (Primary Key)
        		public string Name { get; set; }
        		public decimal Price { get; set; }
        		// Có thể có các thuộc tính cho mối quan hệ, ví dụ:
        		// public int CategoryId { get; set; }
        		// public virtual Category Category { get; set; }
        }
        ```
        
- Creating the Data Models with EF Core
    - **Cách thực hiện:** Đơn giản là bạn tạo các lớp C# như ví dụ trên (thường gọi là POCO - Plain Old CLR Objects).
    - **Quy ước (Conventions):** EF Core có nhiều quy ước ngầm định để tự động hiểu mô hình của bạn:
        - Thuộc tính tên là `Id` hoặc `[TênClass]Id` (ví dụ `ProductId`) thường được tự động nhận diện là khóa chính (Primary Key).
        - Các thuộc tính kiểu dữ liệu cơ bản (int, string, decimal, bool, DateTime,...) sẽ được ánh xạ sang các kiểu dữ liệu tương ứng trong database.
        - Các thuộc tính là class khác (ví dụ `Category` trong `Product`) thể hiện mối quan hệ (relationship).
    - **Cấu hình nâng cao (Data Annotations & Fluent API):** Khi quy ước không đủ hoặc bạn muốn tùy chỉnh chi tiết hơn (độ dài tối đa của chuỗi, tên bảng/cột khác với tên class/property, đánh index, mối quan hệ phức tạp,...), bạn có thể dùng:
        - **Data Annotations:** Các attribute đặt trực tiếp trên class hoặc property (ví dụ: `[Key]`, `[Required]`, `[MaxLength(100)]`, `[Table("TenBangKhac")]`). Dễ dùng, dễ đọc.
        - **Fluent API:** Cấu hình trong phương thức `OnModelCreating` của `DbContext` (sẽ nói ở phần sau). Mạnh mẽ và linh hoạt hơn Data Annotations, giúp tách biệt cấu hình khỏi model.
- Understanding the Database Context
    - **Khái niệm:** `DbContext` là **trái tim** của EF Core. Nó là một lớp C# kế thừa từ `Microsoft.EntityFrameworkCore.DbContext`.
    - **Vai trò chính:**
        - **Cầu nối (Bridge):** Là cầu nối giữa các lớp mô hình (Entities) của bạn và cơ sở dữ liệu thực tế.
        - **Phiên làm việc (Session/Unit of Work):** Quản lý một phiên làm việc với database. Nó bao gồm thông tin kết nối, cấu hình model.
        - **Truy vấn (Querying):** Cho phép bạn viết các truy vấn LINQ (Language Integrated Query) trên các `DbSet` để lấy dữ liệu. EF Core sẽ dịch LINQ này thành SQL tương ứng.
        - **Theo dõi thay đổi (Change Tracking):** Tự động theo dõi trạng thái của các đối tượng (entities) mà bạn truy xuất hoặc thêm vào context. Khi bạn gọi `SaveChanges()`, nó biết được đối tượng nào đã bị thay đổi, thêm mới, hay xóa đi để tạo ra các câu lệnh SQL (INSERT, UPDATE, DELETE) phù hợp.
        - **Quản lý giao dịch (Transaction Management):** Mặc định, `SaveChanges()` thực thi tất cả các thay đổi trong một transaction duy nhất, đảm bảo tính toàn vẹn dữ liệu.
- Adding a Database Context
    - **Cách thực hiện:**
        1. Tạo một class mới kế thừa từ `DbContext`. Ví dụ: `ApplicationDbContext`.
        2. Thêm các thuộc tính kiểu `DbSet<T>` cho mỗi entity (model) mà bạn muốn quản lý thông qua context này. `DbSet<T>` đại diện cho một tập hợp các thực thể (tương đương một bảng trong database).
        3. Tạo một constructor nhận `DbContextOptions<TênContextCủaBạn>` và truyền nó vào constructor của lớp `DbContext` cơ sở. Điều này cho phép cấu hình context từ bên ngoài (ví dụ: connection string, database provider).
        4. (Tùy chọn nhưng phổ biến) Ghi đè (override) phương thức `OnModelCreating(ModelBuilder modelBuilder)` để cấu hình model bằng Fluent API nếu cần.
    - **Ví dụ:**
        
        ```csharp
        using Microsoft.EntityFrameworkCore;
        
        public class ApplicationDbContext : DbContext
        {
            // Constructor để nhận cấu hình từ bên ngoài
            public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
                : base(options)
            {
            }
        
            // DbSet cho mỗi entity bạn muốn quản lý
            public DbSet<Product> Products { get; set; }
            public DbSet<Category> Categories { get; set; } // Ví dụ thêm Category
        
            // (Tùy chọn) Cấu hình bằng Fluent API
            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder); // Nên gọi phương thức cơ sở
        
                // Ví dụ cấu hình Fluent API
                modelBuilder.Entity<Product>()
                    .Property(p => p.Name)
                    .HasMaxLength(200)
                    .IsRequired(); // Đặt độ dài max và yêu cầu bắt buộc cho Name
        
                modelBuilder.Entity<Category>().HasData( // Ví dụ Seeding Data (sẽ nói sau)
                    new Category { Id = 1, Name = "Electronics" },
                    new Category { Id = 2, Name = "Books" }
                );
            }
        }
        ```
        
- EF Core and Database Support
    - **Database Providers:** Bản thân EF Core không trực tiếp giao tiếp với một loại database cụ thể nào. Nó cần một **Database Provider** riêng cho từng hệ quản trị CSDL (DBMS) mà bạn muốn làm việc.
    - **Các Provider phổ biến:**
        - **SQL Server:** `Microsoft.EntityFrameworkCore.SqlServer`
        - **SQLite:** `Microsoft.EntityFrameworkCore.Sqlite` (nhẹ, tốt cho development, testing, ứng dụng nhỏ)
        - **PostgreSQL:** `Npgsql.EntityFrameworkCore.PostgreSQL`
        - **MySQL:** `Pomelo.EntityFrameworkCore.MySql` hoặc `MySql.EntityFrameworkCore` (từ Oracle)
        - **In-Memory:** `Microsoft.EntityFrameworkCore.InMemory` (chỉ dùng cho testing, không lưu trữ dữ liệu thực tế)
        - **Cosmos DB:** `Microsoft.EntityFrameworkCore.Cosmos` (cho NoSQL)
    - **Cài đặt:** Bạn cần cài đặt NuGet package tương ứng với provider bạn chọn vào project của mình.
- Specifying the Data Provider and Connection String
    - **Mục đích:** Bạn cần cho EF Core biết:
        1. **Dùng Provider nào?** (ví dụ: SQL Server, SQLite)
        2. **Kết nối tới Database nào?** (thông tin server, tên database, authentication...)
    - **Cách thực hiện (Phổ biến nhất trong ASP.NET Core hoặc Worker Services):**
        - **Chuỗi kết nối (Connection String):** Thường được lưu trữ trong file cấu hình như `appsettings.json`.
            
            ```json
            // appsettings.json
            {
              "ConnectionStrings": {
                "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MyDatabaseName;Trusted_Connection=True;"
                // Hoặc ví dụ cho SQLite: "Data Source=mydatabase.db"
              }
            }
            ```
            
        - **Đăng ký DbContext và Provider trong `Program.cs` (hoặc `Startup.cs` ở các phiên bản .NET cũ hơn):** Sử dụng Dependency Injection (DI) để cấu hình và cung cấp `DbContext` cho ứng dụng.
            
            ```csharp
            // Program.cs (ví dụ .NET 6+)
            var builder = WebApplication.CreateBuilder(args);
            
            // Lấy connection string từ appsettings.json
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            
            // Đăng ký DbContext với DI và chỉ định provider, connection string
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString)); // Hoặc UseSqlite, UseNpgsql,...
            
            // ... các services khác
            
            var app = builder.Build();
            // ... cấu hình pipeline
            app.Run();
            
            ```
            
    - **Cách khác (ít phổ biến hơn, ví dụ Console App đơn giản):** Ghi đè phương thức `OnConfiguring` trong `DbContext`.
        
        ```csharp
        // Trong ApplicationDbContext.cs
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        		// Chỉ cấu hình nếu chưa được cấu hình từ bên ngoài
            if (!optionsBuilder.IsConfigured) 
            {
                optionsBuilder.UseSqlServer("Your_Connection_String_Here");
            }
        }
        ```
        
- Understanding Code First Development and Migrations
    - **Code First Development:**
        - **Triết lý:** Bạn **viết code trước** (định nghĩa các lớp Entities và DbContext), sau đó EF Core sẽ giúp bạn **tạo hoặc cập nhật cấu trúc (schema) của database** dựa trên code đó.
        - **Ưu điểm:**
            - Lập trình viên tập trung vào logic nghiệp vụ và mô hình đối tượng.
            - Dễ dàng quản lý phiên bản schema database cùng với code (source control).
            - Phù hợp với các dự án mới bắt đầu.
    - **Migrations:**
        - **Vấn đề:** Khi model của bạn thay đổi (thêm/xóa property, entity, thay đổi kiểu dữ liệu, mối quan hệ...), làm sao để cập nhật schema database tương ứng mà không làm mất dữ liệu hiện có?
        - **Giải pháp:** EF Core Migrations là cơ chế để quản lý các thay đổi này một cách an toàn và có kiểm soát.
        - **Cách hoạt động:**
            1. Khi bạn thay đổi model, bạn tạo một "migration" mới.
            2. EF Core so sánh model hiện tại với "snapshot" của model ở lần migration cuối cùng.
            3. Nó tạo ra một file migration (chứa code C#) mô tả các thay đổi cần thực hiện trên database (ví dụ: `ALTER TABLE`, `CREATE TABLE`, `DROP COLUMN`...). File này có 2 phương thức chính: `Up()` (áp dụng thay đổi) và `Down()` (hoàn tác thay đổi).
            4. Bạn "áp dụng" migration này vào database, EF Core sẽ thực thi các lệnh trong phương thức `Up()`.
        - **Lợi ích:**
            - Quản lý thay đổi schema theo từng bước, có thể hoàn tác.
            - Phiên bản schema được lưu trong code, dễ dàng theo dõi và phối hợp trong team.
            - Tự động hóa việc cập nhật database.
- Setup Console App Project
    - **Mục đích:** Tạo một môi trường đơn giản để thực hành EF Core mà không cần sự phức tạp của ASP.NET Core.
    - **Các bước:**
        - Tạo project: Mở terminal hoặc command prompt, di chuyển đến thư mục bạn muốn tạo project và chạy lệnh:
        
        ```bash
        dotnet new console -o MyEfCoreApp
        cd MyEfCoreApp
        ```
        
        - **Cài đặt các gói NuGet cần thiết:**
            - **EF Core Core:** `Microsoft.EntityFrameworkCore` (thường được cài tự động bởi các gói khác)
            - **EF Core Tools:** `Microsoft.EntityFrameworkCore.Tools` (Cần thiết để chạy các lệnh `dotnet ef` như migrations, scaffolding).
            - **Database Provider:** Chọn provider bạn muốn (ví dụ: `Microsoft.EntityFrameworkCore.SqlServer` hoặc `Microsoft.EntityFrameworkCore.Sqlite`).
            - **(Tùy chọn) Design Time:** `Microsoft.EntityFrameworkCore.Design` (Thường cần thiết cho `dotnet ef` hoạt động đúng cách, đặc biệt khi tách biệt project).
            
            ```bash
            dotnet add package Microsoft.EntityFrameworkCore.SqlServer # Hoặc .Sqlite, .Npgsql
            dotnet add package Microsoft.EntityFrameworkCore.Tools
            dotnet add package Microsoft.EntityFrameworkCore.Design
            ```
            
        - **Tạo Models và DbContext:** Tạo các file `.cs` cho các lớp entity (ví dụ `Product.cs`) và lớp `DbContext` (ví dụ `ApplicationDbContext.cs`) như đã mô tả ở các phần trước.
        - **Cấu hình Connection String:** Vì đây là Console App không có `appsettings.json` và DI sẵn như ASP.NET Core, cách đơn giản nhất ban đầu là override `OnConfiguring` trong `DbContext` (như đã nói ở phần 6), hoặc bạn có thể cài thêm gói `Microsoft.Extensions.Configuration.Json` để đọc từ `appsettings.json` nếu muốn.
- Adding a Migration
    - **Điều kiện:** Đã cài đặt `Microsoft.EntityFrameworkCore.Tools` và `Microsoft.EntityFrameworkCore.Design`, đã có `DbContext` và các models.
    - **Lệnh:** Mở terminal/command prompt **tại thư mục gốc của project** (nơi có file `.csproj`) và chạy:
        
        ```bash
        dotnet ef migrations add MigrationName # vd add migration
        ```
        
        - `dotnet ef`: Gọi công cụ dòng lệnh của EF Core.
        - `migrations add`: Lệnh để thêm một migration mới.
        - `MigrationName`: Tên bạn đặt cho migration này. Nên đặt tên có ý nghĩa mô tả thay đổi (ví dụ: `InitialCreate`, `AddProductPrice`, `RenameUserEmailColumn`).
    - **Kết quả:**
        - EF Core sẽ build project của bạn.
        - So sánh model hiện tại với snapshot cuối cùng (hoặc không có gì nếu là lần đầu).
        - Tạo một thư mục `Migrations` (nếu chưa có).
        - Trong thư mục `Migrations`, tạo 2 file:
            - `[Timestamp]_[TenMigration].cs`: Chứa code C# với phương thức `Up()` và `Down()` mô tả thay đổi schema.
            - `[DbContextName]ModelSnapshot.cs`: Chứa "ảnh chụp" (snapshot) của model hiện tại sau khi áp dụng migration này. EF Core dùng file này để so sánh cho lần tạo migration tiếp theo.
    - **Quan trọng:** **Luôn kiểm tra lại file migration** vừa được tạo ra (`[Timestamp]_[TenMigration].cs`) để đảm bảo các lệnh SQL mà EF Core dự định thực thi là đúng với ý định của bạn, đặc biệt với các thay đổi phức tạp.
- IMPORTANT: EF Core 9.0 Migration Errors
    - **Lưu ý về phiên bản:** Hiện tại (Tháng 4 năm 2025), EF Core 9.0 có thể vẫn đang trong giai đoạn preview hoặc mới được phát hành chính thức. Các phiên bản preview thường có thể có lỗi hoặc thay đổi đột phá (breaking changes).
    - **Các lỗi Migration phổ biến (trong mọi phiên bản EF Core, không chỉ 9.0):**
        - **Model Inconsistency:** Model hiện tại không khớp với snapshot cuối cùng (có thể do bạn chỉnh sửa thủ công file snapshot hoặc có lỗi trong model).
        - **Database Schema Drift:** Cấu trúc database thực tế không khớp với những gì migrations mong đợi (ví dụ: ai đó đã thay đổi database thủ công).
        - **Provider Specific Issues:** Một số thay đổi model có thể không được provider hỗ trợ hoặc được dịch thành SQL không chính xác/hiệu quả.
        - **Complex Renames/Moves:** Đổi tên bảng/cột phức tạp đôi khi EF Core không tự động phát hiện đúng, cần can thiệp thủ công vào file migration.
        - **Data Loss Warnings:** Khi một thay đổi có nguy cơ làm mất dữ liệu (ví dụ: xóa cột, thu hẹp kiểu dữ liệu), EF Core sẽ cảnh báo. Bạn cần xác nhận là chấp nhận rủi ro đó.
        - **Circular Dependencies:** Các mối quan hệ phức tạp tạo vòng lặp trong model.
    - **Cách xử lý lỗi Migration:**
        1. **Đọc kỹ thông báo lỗi:** Thông báo lỗi của `dotnet ef` thường khá chi tiết và chỉ ra vấn đề.
        2. **Kiểm tra file Migration (`.cs`):** Xem code trong phương thức `Up()` có hợp lý không. Nếu cần, bạn *có thể* chỉnh sửa file này (nhưng hãy cẩn thận và hiểu rõ mình đang làm gì).
        3. **Kiểm tra Model và Snapshot:** Đảm bảo model của bạn nhất quán và file snapshot là chính xác.
        4. **Xem xét `dotnet ef migrations remove`:** Nếu migration vừa tạo bị lỗi và bạn muốn làm lại, lệnh này sẽ xóa file migration `.cs` cuối cùng và cập nhật lại snapshot. *Chú ý: Chỉ dùng khi migration đó chưa được áp dụng vào database!*
        5. **Kiểm tra Database:** Đảm bảo schema database đang ở trạng thái mà migration trước đó đã tạo ra.
        6. **Tìm kiếm lỗi cụ thể:** Google thông báo lỗi cùng với phiên bản EF Core và provider bạn đang dùng. Cộng đồng Stack Overflow và tài liệu Microsoft là nguồn trợ giúp tốt.
    - **Riêng về EF Core 9.0 (nếu có):** Theo dõi trang tài liệu chính thức của Microsoft và GitHub repository của EF Core để biết về các vấn đề đã biết (known issues) hoặc breaking changes trong phiên bản này liên quan đến Migrations.
- Generating a Database (Code-First)
    - **Mục đích:** Áp dụng các migration đang chờ (pending migrations) vào cơ sở dữ liệu thực tế. Nếu database chưa tồn tại, lệnh này thường sẽ tạo mới database và sau đó áp dụng các migration.
    - **Lệnh:** Mở terminal/command prompt tại thư mục gốc của project:
        
        ```bash
        dotnet ef database update
        ```
        
    - `dotnet ef`: Gọi công cụ EF Core.
    - `database update`: Lệnh để áp dụng các migration vào database được chỉ định trong connection string và cấu hình provider.
    - **Cách hoạt động:**
        1. EF Core kiểm tra table history migrations (`__EFMigrationsHistory` - tên mặc định) trong database để xem migration nào đã được áp dụng.
        2. Nó tìm các file migration trong project mà chưa có trong table history.
        3. Thực thi phương thức `Up()` của từng migration chưa được áp dụng, theo thứ tự timestamp.
        4. Sau khi thực thi thành công mỗi migration, nó ghi lại thông tin vào bảng `__EFMigrationsHistory`.
    - **Chỉ định Migration cụ thể:** Bạn có thể áp dụng đến một migration cụ thể (hoặc rollback về một migration cũ hơn):
        
        ```bash
        dotnet ef database update TenMigrationMucTieu # Áp dụng đến migration này
        dotnet ef database update 0 # Rollback tất cả các migrations
        dotnet ef database update TenMigrationTruocDo # Rollback về trạng thái sau migration này
        ```
        
- Understanding Database First Development
    - **Triết lý:** Ngược lại với Code First. Bạn **có một cơ sở dữ liệu đã tồn tại trước**, và bạn muốn EF Core **tạo ra các lớp Entities và DbContext** tương ứng dựa trên schema của database đó.
    - **Khi nào dùng:**
        - Làm việc với các database có sẵn, database của bên thứ ba.
        - Các dự án mà database được thiết kế và quản lý bởi DBA (Database Administrator).
        - Muốn nhanh chóng tạo model từ một schema phức tạp.
    - **Quy trình:** Sử dụng công cụ "Reverse Engineering" (hoặc "Scaffolding") của EF Core.
- Reverse Engineer Existing Database
    - **Mục đích:** Tự động tạo code C# (Entities và DbContext) từ schema của một database đã tồn tại.
    - **Lệnh (Scaffolding):** Mở terminal/command prompt tại thư mục gốc của project:
    
    ```bash
    dotnet ef dbcontext scaffold "Your_Connection_String_Here" Provider.Package.Name [options]
    ```
    
    - `"Your_Connection_String_Here"`: Chuỗi kết nối tới database bạn muốn reverse engineer.
    - `Provider.Package.Name`: Tên package của database provider bạn đang dùng (ví dụ: `Microsoft.EntityFrameworkCore.SqlServer`, `Npgsql.EntityFrameworkCore.PostgreSQL`).
    - **`[options]` (Các tùy chọn phổ biến):**
        - `o` hoặc `-output-dir`: Thư mục để chứa các file entity được tạo ra (ví dụ: `o Models`).
        - `c` hoặc `-context`: Tên bạn muốn đặt cho lớp `DbContext` được tạo ra (ví dụ: `c MyExistingDbContext`).
        - `-context-dir`: Thư mục để chứa file `DbContext` được tạo ra (tách biệt với models).
        - `t` hoặc `-table`: Chỉ định các bảng cụ thể muốn tạo model (nếu không chỉ định, nó sẽ tạo cho tất cả). Ví dụ: `t Products -t Categories`.
        - `-use-database-names`: Sử dụng tên cột và bảng gốc từ database thay vì cố gắng chuyển đổi sang quy ước đặt tên C#.
        - `-no-onconfiguring`: Không tạo ra phương thức `OnConfiguring` với connection string bị hardcode trong `DbContext`. Nên dùng tùy chọn này và cấu hình connection string qua DI.
        - `-data-annotations`: Sử dụng Data Annotations thay vì Fluent API để cấu hình model (mặc định là Fluent API).
        - `f` hoặc `-force`: Ghi đè lên các file đã tồn tại nếu chạy lại lệnh.
    - Ví dụ:
        
        ```bash
        # Tạo models trong thư mục "Entities", context tên "LegacyDb" trong thư mục "DataAccess", dùng SQL Server, không hardcode connection string
        dotnet ef dbcontext scaffold "Server=.;Database=LegacyDB;Trusted_Connection=True;" Microsoft.EntityFrameworkCore.SqlServer -o Entities -c LegacyDb --context-dir DataAccess --no-onconfiguring
        ```
        
        - **Kết quả:** EF Core sẽ kết nối tới database, đọc schema và tạo ra các file `.cs` cho Entities và `DbContext` với cấu hình Fluent API (hoặc Data Annotations) phản ánh cấu trúc database.
        - **Lưu ý:** Code được tạo ra là "một chiều". Nếu database thay đổi sau này, bạn cần chạy lại lệnh scaffold (thường với tùy chọn `f`) để cập nhật code, hoặc bạn phải tự cập nhật code thủ công. Database First không dùng Migrations theo cách giống Code First. Nếu bạn muốn chuyển sang quản lý bằng Migrations sau khi scaffold, cần một số bước thiết lập thêm.
- Seeding Data
    - **Mục đích:** Cung cấp dữ liệu ban đầu cho database khi nó được tạo hoặc khi migration được áp dụng. Hữu ích cho:
        - Dữ liệu cấu hình cơ bản (ví dụ: vai trò người dùng, danh mục mặc định).
        - Dữ liệu thử nghiệm (test data).
        - Dữ liệu cần thiết để ứng dụng hoạt động.
    - **Cách thực hiện (Khuyến nghị):** Sử dụng phương thức `OnModelCreating` trong `DbContext` và phương thức `HasData` của `EntityTypeBuilder`.
    
    ```csharp
    // Trong ApplicationDbContext.cs
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    
        // Seed data cho bảng Categories
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Electronics", Description = "Gadgets and devices" },
            new Category { Id = 2, Name = "Books", Description = "Paperback and hardcover books" },
            new Category { Id = 3, Name = "Clothing", Description = "Apparel and accessories" }
        );
    
        // Seed data cho bảng Products (ví dụ có khóa ngoại tới Category)
        // Quan trọng: Phải cung cấp giá trị khóa chính (Id) cho dữ liệu seed
        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 101, Name = "Laptop Pro", Price = 1200.00m, CategoryId = 1 },
            new Product { Id = 102, Name = "Learning EF Core", Price = 45.50m, CategoryId = 2 },
            new Product { Id = 103, Name = "Wireless Mouse", Price = 25.99m, CategoryId = 1 }
        );
    
        // ... các cấu hình khác ...
    }
    ```
    
    - **Cách hoạt động:**
        1. Khi bạn thêm hoặc thay đổi dữ liệu trong `HasData`, bạn cần tạo một migration mới (`dotnet ef migrations add AddSeedData`).
        2. Migration này sẽ chứa các lệnh `InsertData`, `UpdateData`, `DeleteData` tương ứng.
        3. Khi bạn chạy `dotnet ef database update`, migration này sẽ được áp dụng và dữ liệu sẽ được chèn/cập nhật vào database.
    - **Ưu điểm:**
        - Dữ liệu seed được quản lý cùng với schema migrations.
        - Độc lập với database provider (EF Core tạo SQL phù hợp).
        - Dễ quản lý trong source control.
    - **Lưu ý quan trọng:**
        - Bạn **phải** cung cấp giá trị khóa chính (PK) cho dữ liệu seed.
        - Dữ liệu seed được quản lý bởi migrations. Nếu bạn thay đổi dữ liệu trong `HasData`, EF Core sẽ tạo migration để cập nhật nó trong database. Nếu bạn xóa dữ liệu khỏi `HasData`, migration sẽ xóa nó khỏi database. Nó không chỉ đơn thuần là chèn nếu chưa có.
- Section Review
- Section Source Code