# ASP.NET Core and EF Core

**ASP.NET Core and EF Core**

- Section Overview
    - [ASP.NET](http://asp.net/) Core để xây dựng các ứng dụng web và API tương tác với cơ sở dữ liệu. [ASP.NET](http://asp.net/) Core được thiết kế để làm việc liền mạch với EF Core thông qua các cơ chế như Dependency Injection và Configuration.
- How EF Core and ASP.NET Core Work
    
    Sự tích hợp giữa EF Core và ASP.NET Core chủ yếu dựa trên hai cơ chế cốt lõi của ASP.NET Core:
    
    - **Dependency Injection (DI):**
        - ASP.NET Core có một hệ thống DI tích hợp sẵn mạnh mẽ. Thay vì bạn phải tự tạo instance của `DbContext` ở khắp nơi, bạn sẽ đăng ký (register) `DbContext` của mình với DI container.
        - Sau đó, bất cứ khi nào một thành phần trong ứng dụng (như Controller, Service, Razor Page Model) cần dùng `DbContext`, nó chỉ cần khai báo `DbContext` đó như một tham số trong constructor. DI container sẽ tự động tạo và cung cấp (inject) một instance `DbContext` phù hợp.
    - **`DbContext`** Lifetime (Vòng đời DbContext): Scoped
        - Khi bạn đăng ký `DbContext` bằng phương thức `AddDbContext` (sẽ xem ở phần sau), vòng đời mặc định của nó là **Scoped**.
        - **Scoped Lifetime** có nghĩa là: Một instance `DbContext` **mới** sẽ được tạo ra cho **mỗi một HTTP request** đến ứng dụng của bạn. Instance này sẽ được sử dụng trong suốt quá trình xử lý request đó (ví dụ: trong middleware, controller, services được gọi bởi controller đó). Khi request kết thúc, instance `DbContext` đó sẽ tự động được **dispose** (giải phóng tài nguyên).
        - **Tại sao Scoped lại phù hợp?**
            - Đảm bảo mỗi request có một Unit of Work riêng biệt, tránh các vấn đề về chia sẻ trạng thái hoặc lỗi tracking giữa các request khác nhau.
            - `DbContext` không phải là thread-safe, việc giới hạn nó trong một request giúp tránh các vấn đề về đa luồng.
    - **Configuration:**
        - Hệ thống cấu hình của ASP.NET Core (đọc từ `appsettings.json`, environment variables, user secrets...) được sử dụng để cung cấp các thông tin cần thiết cho `DbContext`, quan trọng nhất là **connection string**.
- Connect to the Database Context
    
    Đây là bước cấu hình để ASP.NET Core biết về `DbContext` của bạn và cách tạo nó.
    
    - **Bước 1: Cài đặt các gói NuGet cần thiết**
        - Đảm bảo bạn đã cài đặt các gói EF Core cần thiết vào project ASP.NET Core của mình:
            
            ```
            dotnet add package Microsoft.EntityFrameworkCore.Design
            dotnet add package Microsoft.EntityFrameworkCore.SqlServer # Hoặc provider khác (Npgsql, Sqlite...)
            # Gói Tools cần thiết cho các lệnh dotnet ef, nhưng thường được cài dưới dạng global tool hoặc đã có
            # dotnet add package Microsoft.EntityFrameworkCore.Tools
            ```
            
    - **`Bước 2: Thêm Connection String vào appsettings.json`**
        - Mở file `appsettings.json` (và `appsettings.Development.json` nếu cần).
        - Thêm mục `ConnectionStrings`:
            
            ```json
            {
              "Logging": { ... },
              "AllowedHosts": "*",
              "ConnectionStrings": {
                "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MyWebAppDb;Trusted_Connection=True;MultipleActiveResultSets=true"
                // Thay bằng connection string thực tế của bạn
              }
            }
            ```
            
    - **`Bước 3: Đăng ký DbContext với Dependency Injection (Program.cs)`**
        - Mở file `Program.cs` (đối với .NET 6+).
        - Tìm đến phần cấu hình services (`builder.Services...`).
        - Sử dụng phương thức `AddDbContext` để đăng ký `DbContext` của bạn.
            
            ```csharp
            using Microsoft.EntityFrameworkCore;
            // using MyEfCoreApi.Data; // Namespace chứa ApplicationDbContext của bạn
            // using MyEfCoreApi.Models; // Namespace chứa các model
            
            var builder = WebApplication.CreateBuilder(args);
            
            // 1. Lấy connection string từ cấu hình (appsettings.json)
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            }
            
            // 2. Đăng ký ApplicationDbContext với DI container
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString)); // Hoặc UseNpgsql, UseSqlite...
            
            // Thêm các services khác (Controllers, Swagger, etc.)
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
            
        - `AddDbContext<ApplicationDbContext>`: Đăng ký `ApplicationDbContext` với DI.
        - `options => options.UseSqlServer(connectionString)`: Cấu hình `DbContextOptions` để sử dụng SQL Server provider và connection string đã lấy được. EF Core sẽ tự động đọc `DbContextOptions` này khi DI container tạo instance `DbContext`.
        - Mặc định, `AddDbContext` đăng ký `DbContext` với **Scoped lifetime**.
    
    Bây giờ, bạn có thể inject `ApplicationDbContext` vào constructors của Controllers, Services, hoặc Razor Page Models.
    
    ```csharp
    // Ví dụ trong một API Controller
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context; // Inject DbContext
    
        public ProductsController(ApplicationDbContext context) // Nhận DbContext qua constructor
        {
            _context = context;
        }
    
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            return await _context.Products.Include(p => p.Category).ToListAsync(); // Sử dụng DbContext
        }
        // ... các actions khác (POST, PUT, DELETE) ...
    }
    
    ```
    
- Fixing EF Core Design Time Errors
    
    Khi bạn chạy các lệnh `dotnet ef` (ví dụ: `dotnet ef migrations add InitialCreate`, `dotnet ef database update`) trong môi trường ASP.NET Core, đôi khi bạn sẽ gặp lỗi. Các lệnh này cần có khả năng tạo một instance của `DbContext` để đọc cấu hình model, và chúng chạy trong một ngữ cảnh khác với ứng dụng đang chạy thực tế.
    
    - **Nguyên nhân phổ biến và Giải pháp:**
        1. **`Thiếu gói Microsoft.EntityFrameworkCore.Design:`**
            - **Lỗi:** Thường báo không tìm thấy lệnh `dotnet ef` hoặc lỗi liên quan đến assembly.
            - **Giải pháp:** Đảm bảo bạn đã cài đặt package `Microsoft.EntityFrameworkCore.Design` vào **project khởi động (startup project)** của ứng dụng ASP.NET Core.
                
                ```bash
                dotnet add package Microsoft.EntityFrameworkCore.Design
                ```
                
        2. **Startup Project không đúng:**
            - **Lỗi:** `dotnet ef` không tìm thấy `DbContext` hoặc cấu hình cần thiết vì nó đang chạy ở thư mục sai.
            - **Giải pháp:**
                - Chạy lệnh `dotnet ef` từ thư mục chứa **project khởi động** (project ASP.NET Core chính).
                - Nếu `DbContext` hoặc migrations nằm ở một project class library riêng biệt, bạn cần chỉ định project khởi động khi chạy lệnh:
                    
                    ```powershell
                    # Chạy từ thư mục chứa DbContext/Migrations
                    dotnet ef migrations add MyMigration --startup-project ../MyWebAppProject
                    dotnet ef database update --startup-project ../MyWebAppProject
                    ```
                    
        3. **`Không thể tạo instance DbContext lúc Design Time:`**
            - **Lỗi:** Thường báo lỗi "Unable to create an object of type 'ApplicationDbContext'. For the different patterns supported at design time, see [https://go.microsoft.com/fwlink/?linkid=851728](https://go.microsoft.com/fwlink/?linkid=851728)"
            - **Nguyên nhân:** `DbContext` của bạn có constructor yêu cầu các tham số (như `DbContextOptions` hoặc các services khác) mà công cụ `dotnet ef` không tự động cung cấp được.
            - **Giải pháp 1: Dựa vào Application Host (Mặc định):** `dotnet ef` cố gắng gọi `Program.CreateHostBuilder(args).Build().Services.GetRequiredService<ApplicationDbContext>()`. Đảm bảo `Program.cs` của bạn được cấu hình đúng để có thể tạo host và cung cấp `DbContext` (bao gồm cả việc đọc connection string). Cách này thường hoạt động tốt với cấu hình chuẩn.
            - **`Giải pháp 2: Implement IDesignTimeDbContextFactory<T> (Khuyến nghị khi cách 1 lỗi/phức tạp):`**
                - Tạo một class mới trong project chứa `DbContext` của bạn, implement interface `IDesignTimeDbContextFactory<ApplicationDbContext>`.
                - Interface này yêu cầu một phương thức `CreateDbContext(string[] args)` trả về một instance `ApplicationDbContext`.
                - Bên trong phương thức này, bạn tự cấu hình `DbContextOptionsBuilder` (ví dụ: đọc connection string từ `appsettings.json` thủ công) và tạo `DbContext`. Công cụ `dotnet ef` sẽ ưu tiên sử dụng factory này nếu tìm thấy.
                
                ```csharp
                using Microsoft.EntityFrameworkCore;
                using Microsoft.EntityFrameworkCore.Design;
                using Microsoft.Extensions.Configuration;
                using System.IO;
                
                // Đặt class này trong project chứa ApplicationDbContext
                public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
                {
                    public ApplicationDbContext CreateDbContext(string[] args)
                    {
                        // Lấy đường dẫn đến project chứa DbContext (có thể cần điều chỉnh)
                        // Cách này giả định appsettings.json nằm cùng cấp hoặc cấp cha của project này
                        // Hoặc bạn có thể trỏ thẳng đến project WebApp/API để đọc appsettings.json của nó
                        IConfigurationRoot configuration = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory()) // Hoặc đường dẫn tới project WebApp/API
                            .AddJsonFile("appsettings.json")
                            .AddJsonFile("appsettings.Development.json", optional: true) // Đọc cả file Development
                            .Build();
                
                        var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
                        var connectionString = configuration.GetConnectionString("DefaultConnection");
                
                        builder.UseSqlServer(connectionString); // Hoặc provider khác
                
                        return new ApplicationDbContext(builder.Options);
                    }
                }
                ```
                
- Scaffolding Code with Visual Studio
    
    Scaffolding là quá trình tự động tạo code cơ bản (boilerplate code) cho các thao tác CRUD (Create, Read, Update, Delete) dựa trên model và `DbContext` của bạn. Visual Studio cung cấp công cụ trực quan cho việc này.
    
    - **Cách thực hiện:**
        1. Chuột phải vào thư mục `Controllers` (cho API/MVC) hoặc `Pages` (cho Razor Pages) trong Solution Explorer.
        2. Chọn **Add** -> **New Scaffolded Item...**.
        3. Trong cửa sổ "Add New Scaffolded Item":
            - Đối với API: Chọn **API Controller with actions, using Entity Framework**.
            - Đối với Razor Pages: Chọn **Razor Pages using Entity Framework (CRUD)**.
            - Đối với MVC: Chọn **MVC Controller with views, using Entity Framework**.
        4. Click **Add**.
        5. Trong cửa sổ tiếp theo:
            - Chọn **Model class** (ví dụ: `Product`).
            - Chọn **Database context class** (ví dụ: `ApplicationDbContext`).
            - Đặt tên cho Controller/Pages (ví dụ: `ProductsController`, thư mục `Pages/Products`).
            - (Tùy chọn) Cấu hình các tùy chọn khác (async actions, views...).
        6. Click **Add**. Visual Studio sẽ tự động tạo các file Controller (`.cs`) và/hoặc các file Razor (`.cshtml`, `.cshtml.cs`) với các action/handler cơ bản cho CRUD.
- Scaffolding Code with Visual Studio Code
    
    Nếu bạn dùng VS Code hoặc muốn thực hiện scaffolding từ dòng lệnh, bạn cần công cụ `dotnet-aspnet-codegenerator`.
    
    - **Bước 1: Cài đặt công cụ**
        - Cài đặt toàn cục (recommended):
            
            ```
            dotnet tool install --global dotnet-aspnet-codegenerator
            
            ```
            
        - Hoặc cài đặt cục bộ cho project (tạo file manifest nếu chưa có):
            
            ```
            dotnet new tool-manifest # Chỉ chạy lần đầu nếu chưa có file .config/dotnet-tools.json
            dotnet tool install dotnet-aspnet-codegenerator
            
            ```
            
            (Nếu cài cục bộ, bạn cần chạy lệnh bằng `dotnet tool run dotnet-aspnet-codegenerator ...`)
            
    - **Bước 2: Chạy lệnh Scaffolding**
        - Mở terminal tại thư mục gốc của project ASP.NET Core.
        - **Tạo API Controller:**
            
            ```
            dotnet aspnet-codegenerator controller -name ProductsController -async -api -m Product -dc ApplicationDbContext -outDir Controllers
            
            ```
            
            - `name ProductsController`: Tên controller.
            - `async`: Tạo các action bất đồng bộ.
            - `api`: Chỉ định tạo API controller (không có Views).
            - `m Product`: Tên Model class.
            - `dc ApplicationDbContext`: Tên DbContext class.
            - `outDir Controllers`: Thư mục output.
        - **Tạo Razor Pages (CRUD):**
            
            ```
            dotnet aspnet-codegenerator razorpage ProductCRUD -m Product -dc ApplicationDbContext -udl -outDir Pages/Products --referenceScriptLibraries
            
            ```
            
            - `ProductCRUD`: Tên cho tập hợp các trang Razor (không phải tên file cụ thể).
            - `m Product`: Model class.
            - `dc ApplicationDbContext`: DbContext class.
            - `udl`: Sử dụng layout mặc định.
            - `outDir Pages/Products`: Thư mục output (sẽ tạo các file Create.cshtml, Delete.cshtml, Details.cshtml, Edit.cshtml, Index.cshtml).
            - `-referenceScriptLibraries`: Thêm các thẻ script cho validation phía client.
- Exploring Scaffolded Code
    
    Hãy mở một Controller hoặc Razor Page Model (`.cs`) vừa được tạo ra:
    
    - **Dependency Injection:** Bạn sẽ thấy `ApplicationDbContext` được inject vào constructor.
        
        ```
        private readonly ApplicationDbContext _context;
        public ProductsController(ApplicationDbContext context) { _context = context; }
        
        ```
        
    - **CRUD Actions/Handlers:**
        - **GET (Index/GetAll):** Thường dùng `_context.Products.ToListAsync()` (có thể kèm `Include`).
        - **GET (Details/GetById):** Dùng `_context.Products.FindAsync(id)` hoặc `FirstOrDefaultAsync(m => m.Id == id)`.
        - **POST (Create):** Tạo instance mới, dùng `_context.Products.Add(product)`, `await _context.SaveChangesAsync()`.
        - **PUT (Edit):** Truy vấn entity, cập nhật thuộc tính, `_context.Update(product)` hoặc chỉ cần `await _context.SaveChangesAsync()` (nếu entity được tracked). Cần xử lý concurrency.
        - **DELETE:** Truy vấn entity, dùng `_context.Products.Remove(product)`, `await _context.SaveChangesAsync()`.
    - **Lưu ý:** Code được scaffold là code **khởi đầu**. Bạn **cần phải** xem xét vàปรับปรุง nó:
        - Thêm validation chi tiết hơn.
        - Cải thiện xử lý lỗi.
        - Sử dụng DTOs (Data Transfer Objects) thay vì trả về/nhận vào entity trực tiếp trong API.
        - Thêm logic nghiệp vụ phức tạp hơn (thường đưa vào các lớp Service/Repository).
        - Thêm xác thực (Authentication) và phân quyền (Authorization).
        - Xử lý concurrency một cách rõ ràng.
- Review Best Practices
    
    Khi sử dụng EF Core trong ASP.NET Core, hãy ghi nhớ các thực hành tốt sau (nhiều điểm đã được đề cập trong các bài trước):
    
    1. **Đăng ký DbContext với Scoped Lifetime:** Luôn dùng `AddDbContext` trong `Program.cs`.
    2. **Inject DbContext qua Constructor:** Để DI quản lý việc tạo và cung cấp instance.
    3. **Sử dụng Async Methods:** Luôn dùng `...Async` (ví dụ: `ToListAsync`, `SaveChangesAsync`) trong các action/handler của controller/page để tránh block thread.
    4. **Unit of Work cho mỗi Request:** Tận dụng Scoped lifetime. Không dùng chung một instance `DbContext` cho nhiều request.
    5. **Giữ Controllers/Actions "Mỏng":** Di chuyển logic truy cập dữ liệu và nghiệp vụ phức tạp sang các lớp Service hoặc Repository riêng biệt. Controller chỉ nên điều phối.
    6. **Sử dụng Projections (DTOs):** Đặc biệt quan trọng với API. Không trả về entity đầy đủ. Tạo các lớp DTO chỉ chứa dữ liệu cần thiết cho client. Điều này giúp tối ưu, bảo mật và làm API ổn định hơn khi model thay đổi.
    7. **Xử lý Concurrency:** Implement Optimistic Concurrency (ví dụ: dùng `[Timestamp]`) để tránh mất dữ liệu khi nhiều người dùng cùng sửa.
    8. **Xử lý Lỗi và Logging:** Implement cơ chế xử lý lỗi và ghi log phù hợp.
    9. **Connection Resiliency:** Bật `EnableRetryOnFailure` để tăng độ ổn định.
    10. **Tách biệt Concerns:** Cân nhắc tách `DbContext` và các model vào một project Class Library (Infrastructure/Data layer) riêng biệt, đặc biệt với các ứng dụng lớn. Project ASP.NET Core sẽ tham chiếu đến project đó.
- Section Review
    - EF Core và ASP.NET Core tích hợp chặt chẽ qua Dependency Injection và Configuration.
    - `DbContext` thường được đăng ký với Scoped lifetime (`AddDbContext`), đảm bảo mỗi HTTP request có một Unit of Work riêng.
    - Cấu hình connection string trong `appsettings.json` và đăng ký `DbContext` trong `Program.cs`.
    - Nắm vững cách khắc phục các lỗi phổ biến khi chạy `dotnet ef` (thiếu package Design, sai startup project, implement `IDesignTimeDbContextFactory`).
    - Sử dụng công cụ scaffolding (Visual Studio hoặc `dotnet-aspnet-codegenerator`) để nhanh chóng tạo code CRUD cơ bản.
    - Luôn xem xét và cải thiện code được scaffold, áp dụng các best practices như dùng async, DTOs, xử lý concurrency, tách biệt concerns...
    
    Việc tích hợp EF Core vào ASP.NET Core là kỹ năng nền tảng để xây dựng các ứng dụng web hiện đại với .NET. Bằng cách áp dụng đúng các kỹ thuật và best practices, em có thể xây dựng các ứng dụng dữ liệu hiệu quả, dễ bảo trì và mở rộng. Đây cũng là phần cuối cùng trong series training cơ bản về EF Core. Chúc mừng em đã hoàn thành! Hãy tiếp tục thực hành và khám phá sâu hơn nhé.
    
- Section Source Code