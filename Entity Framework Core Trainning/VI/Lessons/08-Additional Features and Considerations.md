# Additional Features and Considerations

**Additional Features and Considerations**

- **Section Overview**
    - Trong section này, chúng ta sẽ khám phá các tính năng nâng cao và những điểm cần lưu ý khi làm việc với Entity Framework Core để xây dựng ứng dụng tốt hơn.
    - Các chủ đề chính bao gồm:
        - Xử lý dữ liệu trước khi lưu thay đổi
        - Làm việc với SQL Server Temporal Tables
        - Kiểm tra dữ liệu với Data Annotations
        - Cấu hình model với Pre-convention
        - Các tính năng nâng cao khác của EF Core
    - Mục tiêu của section này là giúp bạn:
        - Hiểu và áp dụng các tính năng nâng cao của EF Core
        - Tự động hóa các thao tác xử lý dữ liệu
        - Quản lý lịch sử thay đổi dữ liệu
        - Đảm bảo tính toàn vẹn của dữ liệu
        - Tối ưu hóa cấu hình và hiệu suất ứng dụng

- Manipulate Entries Before Saving Changes
    
    Đôi khi, bạn muốn tự động thực hiện một số hành động ngay trước khi các thay đổi được lưu vào database. Ví dụ phổ biến là tự động cập nhật các cột `CreatedAt` và `UpdatedAt`.
    
    - **`Cách 1: Sử dụng Sự kiện SavingChanges`**
        - `DbContext` cung cấp sự kiện `SavingChanges` (và `SavedChanges` - sau khi lưu) mà bạn có thể đăng ký (subscribe) để thực thi logic tùy chỉnh.
        - Bên trong event handler, bạn có thể truy cập `DbContext.ChangeTracker.Entries()` để lặp qua các entity sắp được lưu, kiểm tra trạng thái (`Added`, `Modified`) và cập nhật các thuộc tính cần thiết.
        - **Ví dụ:** Tự động đặt `CreatedAt` và `UpdatedAt`.
            
            ```
            // Trong ApplicationDbContext.cs
            public override int SaveChanges(bool acceptAllChangesOnSuccess)
            {
                OnBeforeSaving(); // Gọi phương thức xử lý trước khi lưu
                return base.SaveChanges(acceptAllChangesOnSuccess);
            }
            
            public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
            {
                OnBeforeSaving(); // Gọi phương thức xử lý trước khi lưu
                return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            }
            
            private void OnBeforeSaving()
            {
                var entries = ChangeTracker.Entries()
                    .Where(e => e.Entity is IAuditableEntity && // Giả sử có interface IAuditableEntity
                                (e.State == EntityState.Added || e.State == EntityState.Modified));
            
                var now = DateTime.UtcNow; // Hoặc DateTime.Now tùy yêu cầu
            
                foreach (var entry in entries)
                {
                    var entity = (IAuditableEntity)entry.Entity;
            
                    if (entry.State == EntityState.Added)
                    {
                        entity.CreatedAt = now;
                    }
            
                    entity.UpdatedAt = now; // Luôn cập nhật UpdatedAt khi Add hoặc Modify
                }
            }
            
            // Interface ví dụ
            public interface IAuditableEntity
            {
                DateTime CreatedAt { get; set; }
                DateTime UpdatedAt { get; set; }
            }
            
            ```
            
    - **Cách 2: Sử dụng Interceptors (Nâng cao hơn)**
        - EF Core cung cấp cơ chế Interceptor (ví dụ: `ISaveChangesInterceptor`, `IDbCommandInterceptor`...) cho phép bạn "xen vào" các giai đoạn khác nhau trong hoạt động của EF Core một cách mạnh mẽ và sạch sẽ hơn so với sự kiện.
        - Ví dụ, `ISaveChangesInterceptor` có các phương thức như `SavingChangesAsync`, `SavedChangesAsync` mà bạn có thể implement để thực hiện logic tương tự như ví dụ trên.
        - Interceptors thường được đăng ký khi cấu hình `DbContextOptions`. Cách này linh hoạt hơn nhưng cũng phức tạp hơn một chút so với sự kiện `SavingChanges`.
- SQL Server Temporal Tables
    - **Khái niệm:** Temporal Tables là một tính năng của SQL Server (từ 2016) cho phép database **tự động theo dõi và lưu trữ lịch sử thay đổi** của dữ liệu trong một bảng. Mỗi khi một hàng được cập nhật hoặc xóa, phiên bản cũ của hàng đó sẽ được lưu vào một table history riêng.
    - **Hỗ trợ trong EF Core:** EF Core (từ bản 5.0) hỗ trợ việc ánh xạ entity tới các temporal table này.
    - **Cấu hình:** Sử dụng Fluent API trong `OnModelCreating`.
        
        ```
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Cấu hình Employee là một Temporal Table
            modelBuilder.Entity<Employee>()
                .ToTable("Employees", b => b.IsTemporal(t =>
                {
                    // (Tùy chọn) Cấu hình tên table history và các cột thời gian
                    // t.HasHistoryTable("EmployeeHistory");
                    // t.UseHistoryTable("EmployeeHistory", "historySchema");
                    // t.HasPeriodStart("ValidFrom");
                    // t.HasPeriodEnd("ValidTo");
                }));
        }
        
        ```
        
        Khi bạn tạo migration và update database, EF Core sẽ tạo bảng `Employees` với cấu hình `SYSTEM_VERSIONING = ON` và table history tương ứng (ví dụ: `EmployeeHistory`).
        
    - **Truy vấn lịch sử:** EF Core cung cấp các phương thức LINQ đặc biệt để truy vấn dữ liệu tại một thời điểm trong quá khứ hoặc trong một khoảng thời gian.
        
        ```
        var employeeId = 1;
        var specificTime = new DateTime(2024, 1, 15, 10, 0, 0);
        
        // Lấy trạng thái của Employee tại thời điểm specificTime
        var employeeAsOf = await context.Employees
            .TemporalAsOf(specificTime) // Truy vấn lịch sử tại thời điểm
            .FirstOrDefaultAsync(e => e.Id == employeeId);
        
        // Lấy tất cả các phiên bản lịch sử của Employee
        var employeeHistory = await context.Employees
            .TemporalAll() // Lấy tất cả các phiên bản (hiện tại + lịch sử)
            .Where(e => e.Id == employeeId)
            .OrderBy(e => EF.Property<DateTime>(e, "ValidFrom")) // Sắp xếp theo thời gian bắt đầu hiệu lực
            .ToListAsync();
        
        // Lấy các phiên bản trong một khoảng thời gian
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 2, 1);
        var employeeBetween = await context.Employees
            .TemporalBetween(startDate, endDate) // Lấy các phiên bản có hiệu lực trong khoảng thời gian
            .Where(e => e.Id == employeeId)
            .ToListAsync();
        
        ```
        
    - **Lợi ích:** Tự động hóa việc theo dõi lịch sử dữ liệu ở cấp độ database, hữu ích cho việc kiểm toán (auditing) hoặc khôi phục dữ liệu.
- Data Validation with Data Annotations
    
    Việc đảm bảo dữ liệu hợp lệ trước khi lưu vào database là rất quan trọng. Data Annotations là một cách khai báo các quy tắc kiểm tra trực tiếp trên model.
    
    - **Cách sử dụng:** Thêm các attribute từ namespace `System.ComponentModel.DataAnnotations` vào các thuộc tính của entity.
        
        ```
        using System.ComponentModel.DataAnnotations;
        using System.ComponentModel.DataAnnotations.Schema;
        
        public class Product
        {
            public int Id { get; set; }
        
            [Required(ErrorMessage = "Tên sản phẩm là bắt buộc.")] // Không được null hoặc rỗng
            [StringLength(100, MinimumLength = 3, ErrorMessage = "Tên sản phẩm phải từ 3 đến 100 ký tự.")] // Giới hạn độ dài
            public string Name { get; set; }
        
            [Range(0.01, 10000.00, ErrorMessage = "Giá sản phẩm phải từ 0.01 đến 10000.")] // Giới hạn khoảng giá trị
            [Column(TypeName = "decimal(18,2)")]
            public decimal Price { get; set; }
        
            [EmailAddress(ErrorMessage = "Email nhà cung cấp không hợp lệ.")] // Kiểm tra định dạng email (ví dụ)
            public string SupplierEmail { get; set; }
        
            // ... các thuộc tính khác ...
        }
        
        ```
        
    - **Tích hợp với EF Core:**
        - **Schema Generation:** EF Core sử dụng một số data annotations để tạo schema database. Ví dụ: `[Required]` thường làm cho cột tương ứng là `NOT NULL`, `[MaxLength]` hoặc `[StringLength]` ảnh hưởng đến độ dài cột (`nvarchar(100)`). `[Column(TypeName = "...")]` chỉ định kiểu dữ liệu cụ thể.
        - **`Validation khi SaveChanges:`** EF Core *có thể* thực hiện kiểm tra dựa trên một số annotations (như `[Required]`, `[MaxLength]`) trước khi gửi lệnh đến database. Nếu vi phạm, nó sẽ ném `DbUpdateException`.
        - **Validation trong ứng dụng:** Trong các ứng dụng như ASP.NET Core MVC/API, việc kiểm tra Data Annotations thường diễn ra sớm hơn, ở giai đoạn model binding. Nếu model không hợp lệ (`ModelState.IsValid` là `false`), bạn thường sẽ không gọi đến `SaveChanges()`. Tuy nhiên, việc có các annotations này vẫn hữu ích cho cả việc tạo schema và validation ở các tầng khác nhau.
- Pre-convention model configuration
    
    EF Core có nhiều quy ước ngầm định (conventions) để suy ra cấu trúc model từ code của bạn. Đôi khi, bạn muốn thay đổi hoặc bổ sung các quy ước này một cách toàn cục thay vì phải cấu hình từng entity riêng lẻ bằng Fluent API.
    
    - **`Cách 1: Lặp qua các Entity Types trong OnModelCreating (Cách cũ hơn)`**
    Bạn có thể lấy danh sách các entity type và áp dụng cấu hình chung.
        
        ```
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        
            // Ví dụ: Đặt độ dài mặc định cho tất cả các cột string là 256
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(string) && property.GetMaxLength() == null) // Chỉ áp dụng nếu chưa có MaxLength cụ thể
                    {
                        property.SetMaxLength(256);
                    }
                }
            }
        
            // Ví dụ: Áp dụng quy tắc đặt tên snake_case cho tất cả các bảng
            // foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            // {
            //     entityType.SetTableName(ToSnakeCase(entityType.GetTableName()));
            // }
        }
        // Hàm ToSnakeCase tự định nghĩa
        
        ```
        
    - **`Cách 2: Sử dụng ConfigureConventions (EF Core 5+ - Khuyến nghị)`**
    Cách này cung cấp API rõ ràng và có cấu trúc hơn để tùy chỉnh conventions. Ghi đè phương thức `ConfigureConventions` trong `DbContext`.
        
        ```
        // Trong ApplicationDbContext.cs
        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            // Ví dụ: Đặt độ dài tối đa mặc định cho tất cả thuộc tính string là 250
            configurationBuilder
                .Properties<string>()
                .HaveMaxLength(250);
        
            // Ví dụ: Tất cả thuộc tính kiểu DateTime sẽ ánh xạ sang kiểu 'datetime2' trong SQL Server
            configurationBuilder
                .Properties<DateTime>()
                .HaveColumnType("datetime2");
        
            // Ví dụ: Áp dụng Value Converter cho tất cả thuộc tính kiểu MyCustomType
            // configurationBuilder
            //     .Properties<MyCustomType>()
            //     .HaveConversion<MyCustomTypeValueConverter>();
        }
        
        ```
        
        Cách này tường minh và ít bị lỗi hơn so với việc lặp thủ công trong `OnModelCreating`.
        
- Support For Database Transactions
    - **`Transaction mặc định của SaveChanges():`** Như đã biết, mỗi lần gọi `SaveChanges()` hoặc `SaveChangesAsync()`, EF Core sẽ tự động thực hiện tất cả các lệnh `INSERT`, `UPDATE`, `DELETE` bên trong một transaction duy nhất. Nếu có lỗi, transaction sẽ rollback.
    - **Explicit Transactions (Giao dịch tường minh):** Khi nào cần?
        - Khi bạn muốn nhóm **`nhiều thao tác SaveChanges()`** vào cùng một đơn vị công việc (Unit of Work) nguyên tử.
        - Khi bạn muốn kết hợp các thao tác của EF Core với các thao tác database khác (ví dụ: gọi ADO.NET, Dapper) trong cùng một transaction.
    - **Cách sử dụng:**
        1. Bắt đầu transaction bằng `context.Database.BeginTransactionAsync()` (hoặc `BeginTransaction()`).
        2. Thực hiện các thao tác EF Core (`Add`, `Update`, `Remove`, `SaveChanges`) và/hoặc các thao tác database khác.
        3. Nếu tất cả thành công, gọi `transaction.CommitAsync()` (hoặc `Commit()`).
        4. Nếu có lỗi xảy ra, gọi `transaction.RollbackAsync()` (hoặc `Rollback()`) trong khối `catch`.
        5. Sử dụng khối `using` cho transaction để đảm bảo nó được dispose đúng cách.
        
        ```
        using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // Thao tác 1: Thêm Product
            var newProduct = new Product { Name = "Transactional Product", Price = 100, CategoryId = 1 };
            context.Products.Add(newProduct);
            await context.SaveChangesAsync(); // Lưu thay đổi 1
        
            // Thao tác 2: Cập nhật Category (ví dụ)
            var category = await context.Categories.FindAsync(1);
            if (category != null)
            {
                category.Name += " (Updated)";
                // Không cần SaveChanges ngay nếu muốn gộp transaction
            }
            await context.SaveChangesAsync(); // Lưu thay đổi 2
        
            // (Có thể thực hiện thêm các thao tác ADO.NET dùng cùng connection ở đây)
            // var connection = context.Database.GetDbConnection();
            // command.Connection = connection;
            // command.Transaction = (System.Data.Common.DbTransaction)transaction.GetDbTransaction(); // Gán transaction
            // await command.ExecuteNonQueryAsync();
        
            // Nếu mọi thứ ổn, commit transaction
            await transaction.CommitAsync();
            Console.WriteLine("Transaction committed successfully.");
        }
        catch (Exception ex)
        {
            // Có lỗi, rollback transaction
            Console.WriteLine($"Error occurred: {ex.Message}. Rolling back transaction.");
            await transaction.RollbackAsync();
            // Xử lý lỗi (log, thông báo...)
        }
        
        ```
        
- Handling Data Concurrency Issues
    - **Vấn đề:** Khi nhiều người dùng cùng lúc đọc và cố gắng cập nhật cùng một bản ghi dữ liệu. Người dùng cuối cùng lưu sẽ ghi đè lên thay đổi của người dùng trước đó mà không biết ("Last in wins"). Điều này có thể dẫn đến mất mát dữ liệu hoặc dữ liệu không nhất quán.
    - **Giải pháp phổ biến: Optimistic Concurrency Control (Kiểm soát đồng thời lạc quan)**
        - Giả định rằng xung đột (conflict) hiếm khi xảy ra.
        - Không khóa (lock) dữ liệu khi đọc.
        - **Phát hiện** xung đột tại thời điểm lưu (`SaveChanges()`). Nếu phát hiện xung đột, không cho phép lưu và thông báo lỗi.
    - **Cách phát hiện xung đột trong EF Core:**
        1. **Sử dụng Timestamp / RowVersion (Khuyến nghị):**
            - Thêm một thuộc tính `byte[]` vào entity và đánh dấu bằng attribute `[Timestamp]` hoặc cấu hình bằng Fluent API `.IsRowVersion()`.
                
                ```
                public class Product
                {
                    // ... other properties ...
                
                    [Timestamp] // Đánh dấu đây là cột rowversion
                    public byte[] RowVersion { get; set; }
                }
                
                ```
                
            - Khi tạo migration, EF Core sẽ tạo cột kiểu `rowversion` (SQL Server) hoặc tương đương. Database sẽ tự động cập nhật giá trị này mỗi khi hàng được sửa đổi.
            - Khi `SaveChanges()` thực hiện `UPDATE` hoặc `DELETE`, EF Core sẽ thêm điều kiện `WHERE RowVersion = [OriginalRowVersionValue]` vào câu lệnh SQL.
            - Nếu hàng đã bị sửa đổi bởi người khác (giá trị `RowVersion` trong DB đã khác), câu lệnh `UPDATE`/`DELETE` sẽ không ảnh hưởng đến hàng nào (`0 rows affected`). EF Core phát hiện điều này và ném ra `DbUpdateConcurrencyException`.
        2. **Sử dụng Concurrency Token:**
            - Đánh dấu một hoặc nhiều thuộc tính thông thường bằng attribute `[ConcurrencyCheck]` hoặc cấu hình bằng Fluent API `.IsConcurrencyToken()`.
            - Khi `SaveChanges()`, EF Core sẽ thêm điều kiện `WHERE [CheckedProperty] = [OriginalValue]` cho *tất cả* các thuộc tính được đánh dấu vào câu lệnh `UPDATE`/`DELETE`.
            - Nếu bất kỳ giá trị gốc nào không khớp, `DbUpdateConcurrencyException` sẽ được ném ra.
            - Ít hiệu quả và phức tạp hơn `RowVersion`, thường chỉ dùng khi không thể thêm cột `rowversion`.
    - **`Xử lý DbUpdateConcurrencyException:`**
        - Bọc lời gọi `SaveChangesAsync()` trong khối `try...catch`.
        - Bắt (catch) `DbUpdateConcurrencyException`.
        - Bên trong `catch`, bạn cần quyết định cách xử lý xung đột:
            - **Thông báo người dùng:** Cách phổ biến nhất. Thông báo rằng dữ liệu đã bị thay đổi bởi người khác và yêu cầu họ tải lại dữ liệu mới nhất rồi thử lại thao tác.
            - **Client Wins:** Ghi đè lên dữ liệu hiện tại trong database bằng dữ liệu của client (cẩn thận, có thể làm mất thay đổi của người khác).
            - **Database Wins:** Bỏ qua thay đổi của client và tải lại dữ liệu mới nhất từ database.
            - **Merge:** Cố gắng hợp nhất các thay đổi (rất phức tạp, thường không khả thi).
        
        ```
        try
        {
            // Giả sử productToUpdate đã được lấy từ DB và sửa đổi
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            Console.WriteLine("Concurrency conflict detected!");
            // Lấy entry gây ra lỗi
            var entry = ex.Entries.Single();
            // Lấy giá trị hiện tại trong database
            var databaseValues = await entry.GetDatabaseValuesAsync();
        
            if (databaseValues == null)
            {
                Console.WriteLine("The entity was deleted by another user.");
                // Xử lý trường hợp bị xóa
            }
            else
            {
                // Lấy giá trị client đang cố gắng lưu
                var clientValues = entry.CurrentValues;
                // Lấy giá trị gốc khi client đọc lên
                var originalValues = entry.OriginalValues;
        
                // TODO: Implement conflict resolution strategy
                // Ví dụ: Thông báo người dùng và yêu cầu tải lại
                Console.WriteLine("Data has been changed by another user. Please reload and try again.");
        
                // Ví dụ: Database Wins (tải lại giá trị từ DB)
                // await entry.ReloadAsync();
        
                // Ví dụ: Client Wins (cần lấy lại rowversion từ DB trước khi thử lưu lại)
                // var databaseEntity = (Product)databaseValues.ToObject();
                // entry.OriginalValues.SetValues(databaseValues); // Coi giá trị DB là gốc mới
                // entry.CurrentValues[nameof(Product.RowVersion)] = databaseEntity.RowVersion; // Cập nhật RowVersion
                // await context.SaveChangesAsync(); // Thử lưu lại (ghi đè)
            }
        }
        
        ```
        
- Using Query Filters
    - **Khái niệm:** Global Query Filters là các bộ lọc LINQ `Where` được **tự động áp dụng** cho **tất cả** các truy vấn LINQ đối với một entity type cụ thể trong `DbContext` đó.
    - **Use Cases:**
        - **Soft Deletes (Xóa mềm):** Luôn loại trừ các bản ghi đã được đánh dấu là đã xóa (ví dụ: có cột `IsDeleted = true`) khỏi tất cả các truy vấn thông thường.
        - **Multi-tenancy:** Tự động lọc dữ liệu dựa trên `TenantId` của người dùng hiện tại, đảm bảo người dùng chỉ thấy dữ liệu của họ.
    - **Cấu hình:** Sử dụng `HasQueryFilter()` trong `OnModelCreating`.
        
        ```
        public class Product
        {
            // ... other properties ...
            public bool IsDeleted { get; set; } // Cột cho soft delete
            // public Guid TenantId { get; set; } // Cột cho multi-tenancy
        }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Áp dụng bộ lọc Soft Delete cho Product
            modelBuilder.Entity<Product>()
                .HasQueryFilter(p => !p.IsDeleted); // Chỉ lấy Product chưa bị xóa
        
            // Ví dụ bộ lọc Multi-tenancy (giả sử có thuộc tính TenantId trong DbContext)
            // modelBuilder.Entity<Product>()
            //     .HasQueryFilter(p => p.TenantId == this.TenantId);
        }
        
        ```
        
    - **Sử dụng:** Sau khi cấu hình, mọi truy vấn `context.Products...` sẽ tự động có thêm điều kiện `WHERE IsDeleted = 0` (hoặc tương đương) trong SQL được tạo ra.
        
        ```
        // Query này sẽ tự động chỉ trả về các Product có IsDeleted = false
        var activeProducts = await context.Products.ToListAsync();
        
        ```
        
    - **Bỏ qua Bộ lọc:** Nếu bạn cần truy vấn *tất cả* các bản ghi (bao gồm cả những bản ghi bị lọc bởi Global Filter, ví dụ: để xem các bản ghi đã xóa mềm), hãy sử dụng `IgnoreQueryFilters()`.
        
        ```
        // Lấy tất cả Products, bao gồm cả những cái đã bị xóa mềm
        var allProductsIncludingDeleted = await context.Products
                                                       .IgnoreQueryFilters()
                                                       .ToListAsync();
        
        ```
        
    - **Lợi ích:** Giúp code truy vấn gọn gàng hơn, tránh lặp lại cùng một điều kiện `Where` ở nhiều nơi, và giảm nguy cơ quên áp dụng bộ lọc quan trọng (như soft delete hay multi-tenancy).
- Database Connection Retry and Timeout Policies
    
    Trong các ứng dụng thực tế, đặc biệt là ứng dụng cloud, kết nối mạng đến database có thể gặp lỗi tạm thời (transient errors). EF Core cung cấp cơ chế **Connection Resiliency** để tự động thử lại các thao tác bị lỗi do những vấn đề tạm thời này.
    
    - **Bật Tính năng Thử lại (Retry):** Sử dụng phương thức `EnableRetryOnFailure()` khi cấu hình database provider trong `DbContextOptionsBuilder`.
        
        ```
        // Program.cs hoặc nơi cấu hình DbContext
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, sqlServerOptionsAction: sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5, // Số lần thử lại tối đa
                    maxDelay: TimeSpan.FromSeconds(30), // Thời gian chờ tối đa giữa các lần thử
                    errorNumbersToAdd: null // (Tùy chọn) Các mã lỗi SQL Server cụ thể cần thử lại
                );
                // sqlOptions.CommandTimeout(60); // (Tùy chọn) Đặt command timeout là 60 giây
            })
        );
        
        ```
        
        - `EnableRetryOnFailure()` sẽ tự động thử lại các lệnh database (query, save changes) nếu chúng thất bại do các lỗi được xác định là tạm thời (ví dụ: lỗi mạng, deadlock...).
        - Bạn có thể tùy chỉnh số lần thử lại, thời gian chờ tối đa, và các mã lỗi cụ thể.
    - **Command Timeout:** Bạn cũng có thể cấu hình thời gian tối đa mà EF Core chờ một lệnh SQL thực thi xong trước khi ném ra exception bằng `CommandTimeout(seconds)`. Giá trị mặc định thường là 30 giây. Tăng giá trị này nếu bạn có các query hoặc thao tác `SaveChanges` chạy lâu.
    
    Sử dụng `EnableRetryOnFailure` giúp ứng dụng của bạn ổn định hơn khi đối mặt với các vấn đề kết nối tạm thời đến database.
    
- Section Source Code
    - Bạn có thể can thiệp vào quá trình lưu thay đổi bằng sự kiện `SavingChanges` hoặc Interceptors để thực hiện các hành động tùy chỉnh (ví dụ: cập nhật timestamp).
    - EF Core hỗ trợ ánh xạ và truy vấn **SQL Server Temporal Tables** để theo dõi lịch sử dữ liệu tự động.
    - **Data Annotations** giúp định nghĩa các quy tắc validation trên model, được EF Core sử dụng để tạo schema và có thể dùng cho validation ở các tầng khác.
    - Tùy chỉnh **Conventions** toàn cục bằng `ConfigureConventions` (EF Core 5+) giúp quản lý cấu hình model nhất quán hơn.
    - Sử dụng **Explicit Transactions** (`BeginTransactionAsync`, `CommitAsync`, `RollbackAsync`) khi cần nhóm nhiều thao tác hoặc kết hợp EF Core với ADO.NET.
    - Xử lý **Concurrency Conflicts** bằng Optimistic Concurrency (ưu tiên `[Timestamp]`/`RowVersion`) và bắt `DbUpdateConcurrencyException`.
    - **Global Query Filters** (`HasQueryFilter`) tự động áp dụng bộ lọc cho các truy vấn, hữu ích cho soft delete và multi-tenancy.
    - Tăng độ ổn định ứng dụng bằng **Connection Resiliency** (`EnableRetryOnFailure`) để tự động thử lại khi có lỗi kết nối tạm thời.
    
    Những tính năng này bổ sung thêm các công cụ mạnh mẽ vào bộ kỹ năng EF Core của em, giúp em xây dựng các ứng dụng .NET phức tạp, hiệu quả và đáng tin cậy hơn. Chúc mừng em đã hoàn thành series training về EF Core! Nếu có bất kỳ câu hỏi nào trong quá trình làm việc thực tế, đừng ngần ngại liên hệ anh nhé.