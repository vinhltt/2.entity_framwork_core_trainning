# Working With Raw SQL, Views and Stored Procedures

**Working With Raw SQL, Views and Stored Procedures**

- **Section Overview**
    
    Chúng ta đã đi qua cách EF Core giúp trừu tượng hóa việc tương tác với database thông qua LINQ và các mô hình đối tượng. 
    
    Tuy nhiên, trong thực tế, có những tình huống mà việc sử dụng SQL thô, Views, hoặc Stored Procedures lại là lựa chọn tốt hơn hoặc thậm chí là bắt buộc.
    
    **Tại sao cần dùng Raw SQL/Views/SPs?**
    
    - **Tối ưu hiệu năng:** Một số truy vấn phức tạp có thể viết bằng SQL thô hiệu quả hơn so với LINQ-to-Entities.
    - **Logic phức tạp:** Stored Procedures có thể đóng gói logic nghiệp vụ phức tạp ngay tại database.
    - **Legacy Database:** Làm việc với các database có sẵn chứa nhiều Views, Stored Procedures, Functions mà bạn cần tận dụng.
    - **Các tính năng Database không được EF Core hỗ trợ trực tiếp:** Sử dụng các tính năng đặc thù của hệ quản trị CSDL.
    
    Tuy nhiên, việc này cũng đi kèm với những đánh đổi nhất định mà chúng ta sẽ thảo luận.
    
- Adding Non-Table Objects with Migrations
    
    Mặc dù Migrations chủ yếu tập trung vào việc tạo/sửa đổi các bảng tương ứng với entities của bạn, bạn hoàn toàn có thể thực thi các lệnh SQL tùy ý để tạo các đối tượng database khác như Views, Stored Procedures, Functions, Triggers, hoặc thậm chí sửa đổi bảng theo cách mà EF Core không hỗ trợ trực tiếp.
    
    - **Cách thực hiện:** Sử dụng phương thức `migrationBuilder.Sql()` bên trong phương thức `Up()` (để tạo/sửa đổi) và `Down()` (để hoàn tác/xóa) của một file migration.
    - **Ví dụ:** Tạo một View đơn giản tên là `ProductSummaryView` trong một migration.
        
        ```
        // Trong file migration mới tạo (ví dụ: Migrations/20250411110000_AddProductSummaryView.cs)
        using Microsoft.EntityFrameworkCore.Migrations;
        
        public partial class AddProductSummaryView : Migration
        {
            protected override void Up(MigrationBuilder migrationBuilder)
            {
                // SQL để tạo View
                migrationBuilder.Sql(@"
                    CREATE VIEW ProductSummaryView AS
                    SELECT p.Id, p.Name, p.Price, c.Name AS CategoryName
                    FROM Products p
                    INNER JOIN Categories c ON p.CategoryId = c.Id
                    WHERE p.IsAvailable = 1;
                ");
            }
        
            protected override void Down(MigrationBuilder migrationBuilder)
            {
                // SQL để xóa View khi rollback
                migrationBuilder.Sql(@"
                    DROP VIEW ProductSummaryView;
                ");
            }
        }
        
        ```
        
    - **Quan trọng:**
        - Luôn viết mã SQL tương ứng trong `Down()` để đảm bảo migration có thể được hoàn tác (`dotnet ef database update [PreviousMigration]`).
        - Cú pháp SQL phải chính xác với hệ quản trị CSDL bạn đang dùng (SQL Server, PostgreSQL...).
        - EF Core không quản lý hay hiểu nội dung của View/SP bạn tạo bằng cách này. Nó chỉ thực thi đoạn SQL bạn cung cấp.
    
    Sau khi tạo migration này, chạy `dotnet ef database update` sẽ tạo View `ProductSummaryView` trong database của bạn.
    
- Querying Keyless Entities (Like Views)
    
    Khi bạn đã có một View (hoặc một đối tượng tương tự không có khóa chính rõ ràng như kết quả từ một số Stored Procedure hoặc `FromSql`), bạn cần một cách để EF Core hiểu và truy vấn nó. Đây là lúc **Keyless Entity Types** phát huy tác dụng.
    
    - **Khái niệm:** Là các lớp entity được định nghĩa trong model EF Core nhưng được cấu hình là **không có khóa chính (No Key)**.
    - **Sử dụng cho Views:** Đây là cách phổ biến nhất để ánh xạ và truy vấn các View trong database.
    - **Các bước:**
        1. **Tạo lớp C# tương ứng với View:** Tạo một lớp POCO có các thuộc tính khớp với các cột và kiểu dữ liệu của View.
            
            ```
            // Models/ProductSummary.cs (Lớp đại diện cho View)
            public class ProductSummary
            {
                public int Id { get; set; } // Dù view có cột Id, ta vẫn khai báo là keyless
                public string Name { get; set; }
                public decimal Price { get; set; }
                public string CategoryName { get; set; }
            }
            
            ```
            
        2. **Cấu hình trong DbContext:** Sử dụng Fluent API trong `OnModelCreating` để:
            - Đăng ký entity.
            - Chỉ định nó không có khóa chính (`HasNoKey()`).
            - Ánh xạ nó tới View trong database (`ToView("ViewName")`).
            
            ```
            // Data/ApplicationDbContext.cs
            public class ApplicationDbContext : DbContext
            {
                // ... constructor và các DbSet khác ...
                public DbSet<ProductSummary> ProductSummaries { get; set; } // DbSet cho keyless entity
            
                protected override void OnModelCreating(ModelBuilder modelBuilder)
                {
                    // ... các cấu hình khác ...
            
                    modelBuilder.Entity<ProductSummary>(eb =>
                    {
                        eb.HasNoKey(); // **Quan trọng: Đánh dấu là không có khóa chính**
                        eb.ToView("ProductSummaryView"); // Ánh xạ tới View trong DB
                    });
                }
            }
            
            ```
            
        3. **Truy vấn:** Bây giờ bạn có thể truy vấn `DbSet<ProductSummary>` như các `DbSet` thông thường khác bằng LINQ.
            
            ```
            // Lấy tất cả dữ liệu từ View
            var summaries = await context.ProductSummaries.ToListAsync();
            
            // Lọc dữ liệu từ View
            var cheapSummaries = await context.ProductSummaries
                                              .Where(s => s.Price < 50)
                                              .OrderBy(s => s.Name)
                                              .ToListAsync();
            
            foreach (var summary in cheapSummaries)
            {
                Console.WriteLine($"Product: {summary.Name}, Category: {summary.CategoryName}, Price: {summary.Price}");
            }
            
            ```
            
    - **Hạn chế của Keyless Entities:**
        - **Không thể theo dõi thay đổi (Change Tracking):** Vì không có khóa chính, EF Core không thể theo dõi các thay đổi trên các instance của keyless entity. Bạn không thể cập nhật hay xóa chúng thông qua `DbContext` như các entity thông thường. Chúng chủ yếu dùng để đọc dữ liệu.
        - Không thể định nghĩa mối quan hệ *từ* keyless entity đến các entity khác (nhưng có thể có mối quan hệ *tới* nó từ entity thông thường, dù ít phổ biến).
- Querying with Raw SQL - Part 1
    
    Khi bạn cần thực thi một câu lệnh SQL thô để lấy về các **thể hiện của một entity type đã được ánh xạ** (ví dụ: lấy các `Product` bằng SQL tùy chỉnh), bạn có thể dùng `FromSqlRaw` hoặc `FromSqlInterpolated`.
    
    - **`DbSet<TEntity>.FromSqlRaw(string sql, params object[] parameters)`**:
        - Thực thi SQL thô và ánh xạ kết quả trả về thành các đối tượng `TEntity`.
        - **Tham số hóa:** Sử dụng các placeholder dạng `{index}` (ví dụ: `{0}`, `{1}`) trong chuỗi SQL và truyền các giá trị tham số tương ứng vào `parameters`. **Việc này cực kỳ quan trọng để tránh lỗ hổng SQL Injection.**
        - **Ví dụ:**
            
            ```
            int categoryId = 1;
            decimal minPrice = 50;
            // Lấy Products thuộc categoryId và có giá >= minPrice
            var products = await context.Products
                .FromSqlRaw("SELECT * FROM Products WHERE CategoryId = {0} AND Price >= {1}", categoryId, minPrice)
                .ToListAsync();
            
            ```
            
    - **`DbSet<TEntity>.FromSqlInterpolated(FormattableString sql)`**:
        - Tương tự `FromSqlRaw` nhưng sử dụng cú pháp **chuỗi nội suy (interpolated string)** của C# (`$""`).
        - **An toàn hơn:** EF Core tự động chuyển đổi các biến nội suy thành `DbParameter`, giúp chống SQL Injection một cách tự nhiên hơn. **Đây là cách được khuyến nghị.**
        - **Ví dụ:**
            
            ```
            int categoryId = 1;
            decimal minPrice = 50;
            // Cú pháp tương tự nhưng an toàn hơn
            var products = await context.Products
                .FromSqlInterpolated($"SELECT * FROM Products WHERE CategoryId = {categoryId} AND Price >= {minPrice}")
                .ToListAsync();
            
            ```
            
    - **Yêu cầu quan trọng:**
        - Câu lệnh SQL **phải** trả về các cột có tên và kiểu dữ liệu khớp với các thuộc tính của entity type `TEntity` mà bạn đang truy vấn (`Product` trong ví dụ trên).
        - Nếu entity type có các thuộc tính không được trả về bởi SQL, EF Core có thể sẽ gán giá trị mặc định hoặc ném lỗi tùy cấu hình.
    - **Theo dõi thay đổi:** Các entity trả về bởi `FromSql...` **sẽ được theo dõi bởi Change Tracker** (mặc định) nếu entity type đó có khóa chính. Bạn có thể gọi `.AsNoTracking()` sau `FromSql...` nếu chỉ muốn đọc dữ liệu.
- Querying with Raw SQL - Part 2
    
    Một điểm mạnh của `FromSqlRaw` và `FromSqlInterpolated` là chúng trả về `IQueryable<TEntity>`. Điều này có nghĩa là bạn có thể **kết hợp (compose)** các toán tử LINQ khác *sau khi* gọi `FromSql...`
    
    - **Cách hoạt động:** EF Core sẽ coi SQL thô của bạn như một nguồn dữ liệu (thường là một subquery hoặc CTE trong SQL cuối cùng) và áp dụng các toán tử LINQ (như `Where`, `OrderBy`, `Include`, `Select`, `Skip`, `Take`...) lên trên nguồn dữ liệu đó.
    - **Ví dụ:**
        
        ```
        string nameFilter = "%gadget%";
        int categoryId = 1;
        
        var filteredSortedAndIncludedProducts = await context.Products
            .FromSqlInterpolated($"SELECT * FROM Products WHERE CategoryId = {categoryId}") // SQL làm nguồn dữ liệu gốc
            .Where(p => EF.Functions.Like(p.Name, nameFilter)) // Áp dụng thêm LINQ Where
            .Include(p => p.Category) // Áp dụng Include (EF Core sẽ xử lý việc join)
            .OrderByDescending(p => p.Price) // Áp dụng OrderBy
            .Skip(5) // Áp dụng phân trang
            .Take(10)
            .ToListAsync();
        
        ```
        
        Trong ví dụ này, EF Core sẽ tạo một câu SQL phức tạp hơn, có thể bao gồm subquery hoặc CTE dựa trên SQL gốc của bạn, sau đó áp dụng thêm các mệnh đề `WHERE`, `JOIN` (cho `Include`), `ORDER BY`, và `OFFSET FETCH` (cho `Skip`/`Take`).
        
    - **Lưu ý:**
        - Việc kết hợp LINQ sau `FromSql` rất mạnh mẽ nhưng cần kiểm tra SQL được tạo ra (qua logging) để đảm bảo hiệu năng.
        - `Include` hoạt động nhưng SQL gốc của bạn phải trả về các cột khóa ngoại cần thiết để EF Core thực hiện join.
        - Một số toán tử LINQ phức tạp có thể không tương thích hoàn toàn khi kết hợp với `FromSql`.
- Querying scalar
    
    Khi bạn muốn thực thi các lệnh SQL không trả về dữ liệu entity (như `INSERT`, `UPDATE`, `DELETE` tùy chỉnh, gọi Stored Procedure thực hiện hành động), hoặc lấy về một giá trị đơn lẻ (scalar value).
    
    - **`DatabaseFacade.ExecuteSqlRawAsync(string sql, params object[] parameters)`**:
    - **`DatabaseFacade.ExecuteSqlInterpolatedAsync(FormattableString sql)`**:
        - Sử dụng các phương thức này trên `context.Database` để thực thi các lệnh SQL non-query.
        - Luôn ưu tiên `ExecuteSqlInterpolatedAsync` vì lý do an toàn (chống SQL Injection).
        - Trả về `int` là số lượng bản ghi bị ảnh hưởng bởi lệnh SQL.
        - **Hoàn toàn bỏ qua Change Tracker.**
        - **Ví dụ:**
            
            ```
            decimal priceIncrease = 1.05m;
            int categoryId = 2;
            
            // Tăng giá 5% cho các sản phẩm thuộc category 2
            int affectedRows = await context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE Products SET Price = Price * {priceIncrease} WHERE CategoryId = {categoryId}");
            
            Console.WriteLine($"{affectedRows} product prices were updated.");
            
            // Gọi một Stored Procedure không trả về kết quả (chỉ thực hiện hành động)
            string userEmail = "test@example.com";
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"EXECUTE sp_DeactivateUser @Email={userEmail}");
            
            ```
            
    - **Truy vấn giá trị Scalar (Scalar Value):**
        - EF Core không có phương thức cấp cao trực tiếp, tiện lợi như `ExecuteScalarAsync` trong ADO.NET.
        - **Cách 1 (Đơn giản):** Nếu SQL của bạn chỉ trả về một cột duy nhất, bạn *có thể* dùng `FromSql...` với một kiểu dữ liệu cơ bản (ví dụ: `context.Set<int>().FromSqlRaw("SELECT COUNT(*) FROM Products").FirstOrDefaultAsync()`), nhưng cách này hơi "hack" và không phải lúc nào cũng hoạt động/rõ ràng.
        - **Cách 2 (Phổ biến):** Sử dụng ADO.NET trực tiếp thông qua connection của EF Core.
            
            ```
            int productCount = 0;
            var connection = context.Database.GetDbConnection(); // Lấy connection hiện tại
            try
            {
                await connection.OpenAsync(); // Mở connection (nếu chưa mở)
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT COUNT(*) FROM Products";
                    var result = await command.ExecuteScalarAsync(); // Thực thi và lấy giá trị đơn lẻ
                    if (result != null && result != DBNull.Value)
                    {
                        productCount = Convert.ToInt32(result);
                    }
                }
            }
            finally
            {
                await connection.CloseAsync(); // Đóng connection (nếu bạn đã mở)
            }
            Console.WriteLine($"Total products: {productCount}");
            
            ```
            
        - **Cách 3:** Ánh xạ một Stored Procedure hoặc Function trả về giá trị scalar (xem phần sau).
- Executing User-defined Funcitons
    
    EF Core cho phép bạn ánh xạ các hàm do người dùng định nghĩa (User-defined Functions - UDFs) trong database vào các phương thức C# trong code của bạn (thường là trong `DbContext`).
    
    - **Scalar UDFs (Hàm trả về giá trị đơn lẻ):**
        1. **Tạo phương thức C# Stub:** Tạo một phương thức `static` (hoặc instance method trong DbContext) làm "stub" - nó không cần chứa logic thực thi, chỉ cần có chữ ký phù hợp.
            
            ```
            // Trong ApplicationDbContext.cs
            // Stub cho hàm IsProductPopular trong DB (giả sử trả về bool)
            public static bool IsProductPopular(int productId)
            {
                // Không cần implementation ở đây, EF Core sẽ dịch nó
                throw new NotSupportedException();
            }
            
            ```
            
        2. **`Ánh xạ trong OnModelCreating:`** Sử dụng `modelBuilder.HasDbFunction(...).HasName("DbFunctionName")`.
            
            ```
            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                // ...
                // Lấy MethodInfo của phương thức stub
                var methodInfo = typeof(ApplicationDbContext).GetMethod(nameof(IsProductPopular), new[] { typeof(int) });
            
                modelBuilder.HasDbFunction(methodInfo) // Ánh xạ phương thức C#
                            .HasName("udf_IsProductPopular"); // Tên hàm tương ứng trong DB (có thể kèm schema)
                            // .HasSchema("dbo"); // (Tùy chọn) Chỉ định schema
            }
            
            ```
            
        3. **Gọi trong LINQ:** Gọi phương thức C# stub trực tiếp trong các truy vấn LINQ. EF Core sẽ dịch nó thành lời gọi hàm trong SQL.
            
            ```
            var popularProducts = await context.Products
                .Where(p => ApplicationDbContext.IsProductPopular(p.Id)) // Gọi hàm đã ánh xạ
                .ToListAsync();
            
            ```
            
    - **Table-Valued Functions (TVFs - Hàm trả về bảng):**
        1. **Tạo lớp C# đại diện kết quả:** Tương tự như View, tạo một lớp (thường là keyless entity) để chứa kết quả trả về của TVF.
        2. **`Ánh xạ trong OnModelCreating:`** Dùng `modelBuilder.Entity<ResultType>().HasNoKey()` và ánh xạ phương thức stub trả về `IQueryable<ResultType>` bằng `HasDbFunction`.
        3. **Gọi trong LINQ:** Gọi phương thức stub trong LINQ, kết quả trả về có thể được dùng như một `DbSet` (ví dụ: `context.GetProductsByCategory(categoryId).Where(...)`).
    - **Lưu ý:** Khả năng ánh xạ và dịch UDF có thể khác nhau giữa các database provider.
- Limitations of Raw Queries and EF Core
    
    Việc sử dụng SQL thô mang lại sự linh hoạt nhưng cũng có những hạn chế và đánh đổi:
    
    - **Bỏ qua một phần hoặc toàn bộ EF Core:**
        - `ExecuteSql...` hoàn toàn bỏ qua Change Tracker.
        - `FromSql...` trả về entity được theo dõi (nếu có PK), nhưng logic SQL gốc không được EF Core "hiểu" sâu như LINQ.
    - **Phụ thuộc vào Schema Database:** SQL thô dễ bị lỗi nếu schema database thay đổi mà code SQL không được cập nhật tương ứng. Migrations chỉ quản lý schema cho các phần được EF Core ánh xạ.
    - **Phụ thuộc vào Database Provider:** Cú pháp SQL có thể khác nhau giữa SQL Server, PostgreSQL, MySQL, SQLite... làm giảm tính khả chuyển của ứng dụng.
    - **Không kiểm tra lúc biên dịch:** Lỗi cú pháp trong chuỗi SQL chỉ được phát hiện lúc runtime.
    - **Rủi ro Bảo mật (SQL Injection):** Cần **luôn luôn** sử dụng tham số hóa (`FromSqlInterpolated`, `ExecuteSqlInterpolatedAsync`, hoặc `DbParameter` với `FromSqlRaw`/`ExecuteSqlRawAsync`) để tránh lỗ hổng bảo mật nghiêm trọng này. **Không bao giờ nối chuỗi trực tiếp giá trị đầu vào từ người dùng vào câu lệnh SQL.**
    - **Khó kết hợp (Composable) hơn:** Mặc dù có thể kết hợp LINQ sau `FromSql...`, việc này có thể phức tạp và kém hiệu quả hơn so với việc viết toàn bộ bằng LINQ trong một số trường hợp.
    
    **Khi nào nên cân nhắc dùng Raw SQL?** Khi lợi ích về hiệu năng hoặc khả năng truy cập các tính năng database đặc thù vượt trội hơn những hạn chế này, và bạn sẵn sàng quản lý những rủi ro đi kèm.
    
- Section Review
    - F Core cho phép thực thi SQL thô và làm việc với Views/SPs/UDFs khi cần thiết.
    - Sử dụng `migrationBuilder.Sql()` để tạo/xóa các đối tượng database không phải bảng (Views, SPs...) trong migrations.
    - Ánh xạ Views (hoặc các nguồn dữ liệu không có khóa chính khác) bằng **Keyless Entity Types** (`HasNoKey().ToView(...)`) và truy vấn chúng qua `DbSet`.
    - Sử dụng `DbSet<TEntity>.FromSqlInterpolated()` (ưu tiên) hoặc `FromSqlRaw()` (với tham số hóa cẩn thận) để thực thi SQL thô trả về các entity được ánh xạ. Có thể kết hợp LINQ sau đó.
    - Sử dụng `context.Database.ExecuteSqlInterpolatedAsync()` (ưu tiên) hoặc `ExecuteSqlRawAsync()` để thực thi các lệnh SQL non-query (`UPDATE`, `DELETE`, `INSERT` tùy chỉnh, gọi SP...).
    - Ánh xạ UDFs của database vào các phương thức C# bằng `HasDbFunction()` và gọi chúng trong LINQ.
    - Luôn nhận thức về các **hạn chế và rủi ro** khi dùng SQL thô: bỏ qua change tracking, phụ thuộc schema/provider, không có kiểm tra biên dịch, rủi ro SQL injection.
- Section Source Code