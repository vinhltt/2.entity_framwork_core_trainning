# Using Entity Framework Core to Query a Database

- **Section Overview**
    - Trong section này, chúng ta sẽ tìm hiểu cách sử dụng Entity Framework Core để truy vấn dữ liệu từ database một cách hiệu quả và an toàn.
    - Các chủ đề chính bao gồm:
        - Cấu hình logging để theo dõi và debug các câu lệnh SQL
        - Quản lý connection string an toàn và hiệu quả
        - Sử dụng LINQ để viết các truy vấn database
        - Các phương thức truy vấn cơ bản và nâng cao
        - Phân biệt và sử dụng synchronous vs asynchronous operations
        - Các kỹ thuật tối ưu hiệu năng như AsNoTracking và Projections
        - Hiểu và tránh các vấn đề về hiệu năng với IQueryable
    - Mục tiêu của section này là giúp bạn:
        - Viết được các truy vấn LINQ hiệu quả và an toàn
        - Hiểu và tránh được các vấn đề về hiệu năng phổ biến
        - Áp dụng các best practices khi làm việc với EF Core
        - Debug và tối ưu hóa các truy vấn database
        - Quản lý connection string một cách an toàn
- Adding Verbose Logging to EF Core's Workload
    - **Tại sao cần Logging?**
        - **Hiểu EF Core:** Xem chính xác câu lệnh SQL nào được EF Core tạo ra từ LINQ query của bạn.
        - **Debugging:** Phát hiện các query chậm, query không hiệu quả, hoặc query trả về kết quả không mong muốn.
        - **Tối ưu:** Phân tích SQL để tối ưu hóa (ví dụ: kiểm tra index có được sử dụng không).
    - **Cách bật Logging (Ví dụ trong ASP.NET Core với DI):**
        - Cách đơn giản nhất là dùng `LogTo` khi cấu hình `DbContextOptionsBuilder` trong `Program.cs`.
        
        ```
        // Program.cs
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
                   .LogTo(Console.WriteLine, LogLevel.Information) // Log ra Console với mức Information
                   // Hoặc LogTo(message => System.Diagnostics.Debug.WriteLine(message)) // Log ra cửa sổ Debug Output
                   .EnableSensitiveDataLogging() // (Optional) Bật log giá trị tham số - Chỉ dùng khi dev!
        );
        
        ```
        
        - `LogLevel.Information` thường đủ để xem SQL được tạo ra. Các mức khác (`Debug`, `Trace`) sẽ cung cấp nhiều chi tiết hơn.
        - **Cảnh báo:** `EnableSensitiveDataLogging()` sẽ log cả giá trị tham số trong SQL, có thể lộ thông tin nhạy cảm. **Chỉ bật tính năng này trong môi trường development.**
    - **Kết quả:** Khi ứng dụng chạy và thực hiện query EF Core, bạn sẽ thấy các câu lệnh SQL tương ứng được in ra Console hoặc cửa sổ Debug Output.
- Fix: Database Connection String Refactor
    - Phần này không hẳn là "fix" mà là cách quản lý connection string đúng đắn, tránh các lỗi bảo mật và khó khăn khi triển khai.
    - **Không bao giờ hardcode:** Tránh viết thẳng connection string vào code (ví dụ trong `OnConfiguring`).
    - **Sử dụng Configuration Provider:**
        - **`appsettings.json`**: Lưu connection string trong file này (có thể có `appsettings.Development.json`, `appsettings.Production.json` cho các môi trường khác nhau).
            
            ```
            // appsettings.json
            {
              "ConnectionStrings": {
                "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MyEfCoreDb;Trusted_Connection=True;"
              }
              // ...
            }
            
            ```
            
        - **Environment Variables:** Ghi đè cấu hình từ `appsettings.json`, hữu ích cho môi trường production/staging/docker.
        - **User Secrets (Development):** Dùng cho thông tin nhạy cảm (như password trong connection string) trong quá trình phát triển để không commit vào source control. (Chuột phải vào project -> Manage User Secrets).
    - **Cấu hình qua Dependency Injection (DI):** Đăng ký `DbContext` và truyền connection string lấy từ configuration vào `UseSqlServer` (hoặc provider khác) như ví dụ ở phần Logging. Đây là cách làm chuẩn trong các ứng dụng hiện đại.
- LINQ as Entity Framework Core Syntax
    - **LINQ (Language-Integrated Query):** Là một tập hợp các công nghệ trong .NET cho phép bạn viết truy vấn dữ liệu trực tiếp trong code C# (hoặc VB.NET) một cách mạnh mẽ và tường minh, bất kể nguồn dữ liệu là gì (database, XML, collections...).
    - **EF Core + LINQ:** Khi bạn viết LINQ query trên một `DbSet<T>` hoặc `IQueryable<T>` từ EF Core, **EF Core sẽ dịch (translate)** biểu thức LINQ đó thành câu lệnh SQL (hoặc ngôn ngữ truy vấn tương ứng của database) và thực thi nó trên server database.
    - **Lợi ích:**
        - **Strongly-typed:** Bạn làm việc với các đối tượng và thuộc tính C#, được kiểm tra lỗi tại thời điểm biên dịch (compile-time).
        - **Dễ đọc, dễ viết:** Cú pháp gần gũi với lập trình viên C#.
        - **Tái sử dụng:** Dễ dàng đóng gói logic truy vấn vào các phương thức.
        - **Database Agnostic (Tương đối):** Cùng một LINQ query có thể chạy trên các database khác nhau (SQL Server, PostgreSQL...) nếu provider hỗ trợ dịch các toán tử đó.
- Querying Basics
    - **`Truy cập DbSet:`** Sử dụng thuộc tính `DbSet<T>` bạn đã định nghĩa trong `DbContext`.
    - **Lấy tất cả bản ghi:** Dùng phương thức `ToList()` hoặc `ToListAsync()` để thực thi query và lấy tất cả các bản ghi từ bảng tương ứng.
        
        ```
        // Giả sử 'context' là một instance của ApplicationDbContext đã được inject
        List<Product> allProducts = context.Products.ToList(); // Đồng bộ
        
        // Hoặc (khuyến khích dùng trong web/UI)
        List<Product> allProductsAsync = await context.Products.ToListAsync(); // Bất đồng bộ
        
        ```
        
    - **`IQueryable<T>`**: Lưu ý rằng `context.Products` trả về một `IQueryable<Product>`. Đây là một *biểu thức truy vấn*, chưa phải là dữ liệu. Query chỉ thực sự được gửi đến database khi bạn gọi các phương thức thực thi như `ToList()`, `FirstOrDefault()`, `Count()`, `foreach`... (sẽ nói kỹ hơn ở phần `IQueryable`).
- Synchronous vs. Asynchronous Syntax
    - **Synchronous (Đồng bộ):** Các phương thức như `ToList()`, `FirstOrDefault()`, `SaveChanges()`. Khi gọi, luồng (thread) hiện tại sẽ bị **block** cho đến khi thao tác với database hoàn tất.
        
        ```
        var product = context.Products.FirstOrDefault(p => p.Id == 1); // Block thread cho đến khi query xong
        
        ```
        
    - **Asynchronous (Bất đồng bộ):** Các phương thức tương ứng có hậu tố `Async` như `ToListAsync()`, `FirstOrDefaultAsync()`, `SaveChangesAsync()`. Sử dụng với `async` và `await`. Khi gọi, luồng hiện tại **không bị block**. Nó có thể làm việc khác trong khi chờ database xử lý. Khi database trả về kết quả, phần còn lại của phương thức sẽ được thực thi (thường trên một luồng khác từ thread pool).
        
        ```
        // Trong một phương thức async
        var product = await context.Products.FirstOrDefaultAsync(p => p.Id == 1); // Không block thread
        Console.WriteLine("Có thể làm việc khác trong khi chờ...");
        // Sau khi await hoàn tất, code tiếp tục từ đây
        if (product != null) { ... }
        
        ```
        
    - **Khi nào dùng Async?** **Luôn luôn ưu tiên dùng Async** cho các thao tác I/O (như truy vấn database, gọi API, đọc/ghi file) trong các ứng dụng có giao diện người dùng (UI) hoặc ứng dụng web (ASP.NET Core). Điều này giúp cải thiện khả năng đáp ứng (responsiveness) của UI và khả năng mở rộng (scalability) của web server bằng cách giải phóng các luồng xử lý. Trong Console App đơn giản thì dùng Sync cũng được, nhưng tập dùng Async là thói quen tốt.
- Querying for a Single Record
    
    EF Core cung cấp nhiều phương thức để lấy một bản ghi:
    
    - **`FindAsync(keyValues)`** / **`FindAsync(keyValues, cancellationToken)`**:
        - Tìm bản ghi dựa trên **khóa chính (Primary Key)**.
        - **Ưu điểm:** Nó sẽ **kiểm tra trong bộ nhớ đệm (cache) của DbContext trước**. Nếu entity với khóa đó đã được load và đang được theo dõi (tracked), nó sẽ trả về ngay lập tức mà không cần query database.
        - Trả về `null` nếu không tìm thấy.
        - Luôn dùng Async nếu có thể: `await context.Products.FindAsync(productId);`
    - **`FirstOrDefaultAsync(predicate)`**:
        - Trả về bản ghi **đầu tiên** khớp với điều kiện lọc (`predicate`).
        - Trả về `null` nếu không có bản ghi nào khớp.
        - An toàn khi bạn không chắc có bản ghi nào khớp hay không.
        - Ví dụ: `await context.Products.FirstOrDefaultAsync(p => p.Name == "Laptop XYZ");`
    - **`SingleOrDefaultAsync(predicate)`**:
        - Trả về bản ghi **duy nhất** khớp với điều kiện lọc.
        - Trả về `null` nếu không có bản ghi nào khớp.
        - **Ném ra Exception** nếu có **nhiều hơn một** bản ghi khớp.
        - Dùng khi bạn kỳ vọng chỉ có 0 hoặc 1 kết quả khớp.
        - Ví dụ: `await context.Users.SingleOrDefaultAsync(u => u.Email == "unique.email@example.com");`
    - **`FirstAsync(predicate)`**:
        - Trả về bản ghi **đầu tiên** khớp với điều kiện lọc.
        - **Ném ra Exception** nếu **không có** bản ghi nào khớp.
        - Dùng khi bạn chắc chắn phải có ít nhất một kết quả khớp.
    - **`SingleAsync(predicate)`**:
        - Trả về bản ghi **duy nhất** khớp với điều kiện lọc.
        - **Ném ra Exception** nếu **không có** bản ghi nào khớp HOẶC có **nhiều hơn một** bản ghi khớp.
        - Dùng khi bạn chắc chắn phải có *chính xác một* kết quả khớp.
    
    **Lựa chọn:** `FirstOrDefaultAsync` và `FindAsync` là những lựa chọn phổ biến và an toàn nhất trong nhiều trường hợp. Dùng `Single...` khi logic nghiệp vụ yêu cầu sự duy nhất.
    
- Add Filters to Queries
    - Phương thức `Where()` cho phép bạn lọc dữ liệu dựa trên một hoặc nhiều điều kiện.
    - Nó nhận vào một biểu thức lambda (`predicate`) trả về `bool`. Chỉ những bản ghi nào làm cho biểu thức này trả về `true` mới được giữ lại.
    - `Where()` trả về một `IQueryable<T>` mới, cho phép bạn nối chuỗi (chaining) các phương thức LINQ khác.
        
        ```
        // Lấy các sản phẩm có giá lớn hơn 100
        var expensiveProducts = await context.Products
                                            .Where(p => p.Price > 100)
                                            .ToListAsync();
        
        // Lấy các sản phẩm thuộc CategoryId = 1 VÀ giá dưới 50
        var cheapElectronics = await context.Products
                                             .Where(p => p.CategoryId == 1 && p.Price < 50)
                                             .ToListAsync();
        
        // Có thể gọi Where nhiều lần (tương đương dùng &&)
        var specificProduct = await context.Products
                                            .Where(p => p.CategoryId == 2)
                                            .Where(p => p.IsAvailable == true)
                                            .FirstOrDefaultAsync();
        
        ```
        
    - **Quan trọng:** Điều kiện trong `Where()` sẽ được EF Core dịch thành mệnh đề `WHERE` trong SQL, nghĩa là việc lọc diễn ra **tại database server**, rất hiệu quả.
- Additional Filtering Features
    
    Ngoài các toán tử so sánh cơ bản (`==`, `!=`, `>`, `<`, `>=`, `<=`) và logic (`&&`, `||`), bạn có thể dùng:
    
    - **`Contains()`**: Kiểm tra một collection (ví dụ: List) có chứa một giá trị không (dịch thành `WHERE Id IN (...)`), hoặc một chuỗi có chứa chuỗi con không (dịch thành `WHERE Name LIKE '%substring%'`).
        
        ```
        List<int> categoryIds = new List<int> { 1, 3, 5 };
        var productsInCategories = await context.Products
                                                 .Where(p => categoryIds.Contains(p.CategoryId))
                                                 .ToListAsync();
        
        string searchTerm = "book";
        var books = await context.Products
                                 .Where(p => p.Name.Contains(searchTerm)) // Chú ý: Case-sensitivity tùy thuộc vào collation của DB
                                 .ToListAsync();
        
        ```
        
    - **`StartsWith()`** / **`EndsWith()`**: Kiểm tra chuỗi bắt đầu/kết thúc bằng chuỗi con (dịch thành `LIKE 'prefix%'` hoặc `LIKE '%suffix'`).
        
        ```
        var productsStartingWithA = await context.Products
                                                  .Where(p => p.Name.StartsWith("A"))
                                                  .ToListAsync();
        
        ```
        
    - **`Kiểm tra null:`** Dùng `== null` hoặc `!= null`.
        
        ```
        var productsWithNoDescription = await context.Products
                                                     .Where(p => p.Description == null)
                                                     .ToListAsync();
        
        ```
        
    - **`EF.Functions.Like()`**: Cung cấp cách rõ ràng hơn để dùng các pattern của `LIKE` trong SQL, bao gồm các ký tự đại diện (`%`, `_`).
        
        ```
        // Tìm sản phẩm có chữ 'a' ở vị trí thứ 2
        var productsLike = await context.Products
                                        .Where(p => EF.Functions.Like(p.Name, "_a%"))
                                        .ToListAsync();
        
        ```
        
- Alternative LINQ Syntax
    
    Ngoài cú pháp phương thức (Method Syntax) dùng các extension method như `.Where()`, `.Select()` mà chúng ta đã thấy, LINQ còn có cú pháp truy vấn (Query Syntax) trông giống SQL hơn.
    
    - **Method Syntax (Phổ biến hơn):**
        
        ```
        var expensiveProducts = await context.Products
                                            .Where(p => p.Price > 100 && p.IsAvailable)
                                            .OrderBy(p => p.Name)
                                            .Select(p => p.Name)
                                            .ToListAsync();
        
        ```
        
    - **Query Syntax:**
        
        ```
        var expensiveProductsQuery = from p in context.Products
                                     where p.Price > 100 && p.IsAvailable
                                     orderby p.Name
                                     select p.Name; // Lưu ý: đây vẫn là IQueryable<string>
        
        var expensiveProducts = await expensiveProductsQuery.ToListAsync();
        
        ```
        
    - **So sánh:**
        - Cả hai cú pháp đều được EF Core dịch sang SQL tương đương.
        - Method Syntax thường linh hoạt hơn, dễ nối chuỗi, và nhiều toán tử LINQ chỉ có sẵn ở dạng Method Syntax (`Count()`, `FirstOrDefault()`, `ToList()`...).
        - Query Syntax có thể dễ đọc hơn đối với những người quen thuộc với SQL, đặc biệt với các phép `join` hoặc `group by` phức tạp.
        - Bạn có thể kết hợp cả hai cú pháp.
    - **Khuyến nghị:** Nắm vững Method Syntax vì nó được sử dụng rộng rãi hơn. Biết Query Syntax cũng hữu ích.
- Aggregate Methods
    
    Dùng để thực hiện các phép tính tổng hợp trên tập dữ liệu tại database server.
    
    - **`CountAsync()`** / **`LongCountAsync()`**: Đếm số lượng bản ghi (có thể có điều kiện).
        
        ```
        int totalProducts = await context.Products.CountAsync();
        long availableProductsCount = await context.Products.LongCountAsync(p => p.IsAvailable);
        
        ```
        
    - **`SumAsync()`**: Tính tổng của một cột số.
        
        ```
        decimal totalValue = await context.Products.SumAsync(p => p.Price);
        
        ```
        
    - **`AverageAsync()`**: Tính trung bình cộng của một cột số.
        
        ```
        decimal averagePrice = await context.Products.AverageAsync(p => p.Price);
        
        ```
        
    - **`MinAsync()`** / **`MaxAsync()`**: Tìm giá trị nhỏ nhất/lớn nhất của một cột.
        
        ```
        decimal cheapestPrice = await context.Products.MinAsync(p => p.Price);
        decimal mostExpensive = await context.Products.MaxAsync(p => p.Price);
        
        ```
        
    - **Hiệu quả:** Các phép tính này được thực hiện hoàn toàn ở database, chỉ trả về một giá trị duy nhất, rất hiệu quả.
- Group By
    - Phương thức `GroupBy()` cho phép nhóm các bản ghi dựa trên một hoặc nhiều khóa.
    - Kết quả trả về là một `IQueryable<IGrouping<TKey, TElement>>`, trong đó `TKey` là kiểu dữ liệu của khóa nhóm, và `IGrouping` là một collection chứa các phần tử thuộc nhóm đó.
        
        ```
        // Nhóm sản phẩm theo CategoryId
        var productsByCategory = await context.Products
                                              .GroupBy(p => p.CategoryId)
                                              .ToListAsync(); // List<IGrouping<int, Product>>
        
        foreach (var group in productsByCategory)
        {
            Console.WriteLine($"Category ID: {group.Key}"); // Khóa nhóm (CategoryId)
            int countInGroup = group.Count(); // Đếm số sản phẩm trong nhóm này
            Console.WriteLine($"  Product Count: {countInGroup}");
            // decimal avgPrice = group.Average(p => p.Price); // Tính trung bình giá trong nhóm
        
            // foreach (var productInGroup in group) // Lặp qua các sản phẩm trong nhóm
            // {
            //     Console.WriteLine($"    - {productInGroup.Name}");
            // }
        }
        
        // Thường kết hợp GroupBy với Select để tạo kết quả tổng hợp
        var categorySummary = await context.Products
                                         .GroupBy(p => p.CategoryId)
                                         .Select(g => new
                                         {
                                             CategoryId = g.Key,
                                             NumberOfProducts = g.Count(),
                                             AveragePrice = g.Average(p => p.Price)
                                         })
                                         .ToListAsync();
        
        foreach (var summary in categorySummary)
        {
            Console.WriteLine($"Category: {summary.CategoryId}, Count: {summary.NumberOfProducts}, Avg Price: {summary.AveragePrice}");
        }
        
        ```
        
    - `GroupBy` được dịch thành mệnh đề `GROUP BY` trong SQL.
- Order By
    - Sử dụng `OrderBy()` (tăng dần) hoặc `OrderByDescending()` (giảm dần) để sắp xếp kết quả theo một thuộc tính.
    - Sử dụng `ThenBy()` hoặc `ThenByDescending()` để thêm tiêu chí sắp xếp phụ (khi các giá trị của tiêu chí chính bằng nhau).
        
        ```
        // Sắp xếp sản phẩm theo tên tăng dần
        var sortedByName = await context.Products
                                        .OrderBy(p => p.Name)
                                        .ToListAsync();
        
        // Sắp xếp theo CategoryId tăng dần, sau đó theo giá giảm dần
        var sortedByCategoryThenPrice = await context.Products
                                                    .OrderBy(p => p.CategoryId)
                                                    .ThenByDescending(p => p.Price)
                                                    .ToListAsync();
        
        ```
        
    - Được dịch thành mệnh đề `ORDER BY` trong SQL. **Quan trọng:** Luôn sắp xếp *trước khi* phân trang (`Skip`/`Take`).
- Skip and Take
    - Dùng để triển khai **phân trang (paging)**, chỉ lấy một tập con dữ liệu thay vì toàn bộ.
    - **`Skip(n)`**: Bỏ qua `n` bản ghi đầu tiên.
    - **`Take(m)`**: Lấy `m` bản ghi tiếp theo.
        
        ```
        int pageNumber = 2; // Trang số 2
        int pageSize = 10; // 10 mục mỗi trang
        
        var productsPage2 = await context.Products
                                         .OrderBy(p => p.Id) // **BẮT BUỘC phải OrderBy trước khi Skip/Take**
                                         .Skip((pageNumber - 1) * pageSize) // Bỏ qua (2-1)*10 = 10 bản ghi đầu
                                         .Take(pageSize) // Lấy 10 bản ghi tiếp theo
                                         .ToListAsync();
        
        // Lấy tổng số bản ghi để tính tổng số trang (thường làm trong một query riêng)
        int totalCount = await context.Products.CountAsync();
        int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        
        ```
        
    - **`Tại sao phải OrderBy trước?`** SQL không đảm bảo thứ tự trả về nếu không có `ORDER BY`. Để `Skip`/`Take` hoạt động nhất quán và đúng đắn, bạn phải chỉ định thứ tự rõ ràng.
    - `Skip`/`Take` được dịch thành các cấu trúc SQL tương ứng (ví dụ: `OFFSET FETCH` trong SQL Server 2012+, `LIMIT OFFSET` trong PostgreSQL/MySQL/SQLite). Việc phân trang diễn ra tại database.
- Projections and Custom Data Types
    - **Vấn đề:** Mặc định, khi bạn query `context.Products.ToListAsync()`, EF Core sẽ lấy **tất cả các cột** của bảng `Products` về client. Điều này có thể không hiệu quả nếu bạn chỉ cần một vài cột.
    - **`Giải pháp: Select() (Projection):`** Cho phép bạn chỉ định **chính xác những dữ liệu nào cần lấy về**. Bạn có thể:
        - Chọn một vài cột cụ thể.
        - Tạo ra các đối tượng mới (anonymous types hoặc DTOs - Data Transfer Objects) với cấu trúc mong muốn.
    - **Lợi ích:**
        - **Giảm lượng dữ liệu truyền tải:** Chỉ lấy những gì cần thiết, tăng tốc độ query.
        - **Giảm tải cho database server:** Database chỉ cần đọc và gửi ít dữ liệu hơn.
        - **Định hình dữ liệu:** Tạo ra cấu trúc phù hợp cho ViewModel, API response...
    - **Ví dụ:**
        
        ```
        // 1. Chỉ lấy tên và giá sản phẩm (tạo anonymous type)
        var productNamesAndPrices = await context.Products
                                                 .Select(p => new { p.Name, p.Price }) // Anonymous type
                                                 .ToListAsync();
        
        foreach (var item in productNamesAndPrices)
        {
            Console.WriteLine($"Name: {item.Name}, Price: {item.Price}");
            // item.Id sẽ không truy cập được vì không select
        }
        
        // 2. Tạo đối tượng DTO (Data Transfer Object) cụ thể
        public class ProductSummaryDto
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; }
            public string CategoryName { get; set; } // Lấy từ bảng liên quan
        }
        
        var productSummaries = await context.Products
                                          .Include(p => p.Category) // Cần Include để lấy Category nếu dùng navigation property
                                          .Select(p => new ProductSummaryDto
                                          {
                                              ProductId = p.Id,
                                              ProductName = p.Name,
                                              CategoryName = p.Category.Name // Truy cập navigation property
                                          })
                                          .ToListAsync();
        
        // 3. Chỉ lấy một cột duy nhất
        List<string> productNames = await context.Products
                                                .Select(p => p.Name)
                                                .ToListAsync();
        
        ```
        
    - `Select` được dịch thành việc chỉ định các cột cần lấy trong mệnh đề `SELECT` của SQL.
- Tracking Vs. No Tracking (Enhancing Performance
    - **Change Tracking (Mặc định):** Khi bạn query dữ liệu (ví dụ: `context.Products.ToList()`), EF Core sẽ:
        1. Tạo các đối tượng entity.
        2. Lưu một "bản gốc" (snapshot) của các entity này vào bộ nhớ của `DbContext`.
        3. Theo dõi mọi thay đổi bạn thực hiện trên các thuộc tính của entity đó.
        4. Khi bạn gọi `SaveChangesAsync()`, EF Core so sánh trạng thái hiện tại với snapshot để biết cần tạo lệnh `UPDATE` nào.
    - **Overhead:** Việc lưu snapshot và so sánh trạng thái tốn bộ nhớ và CPU, đặc biệt khi load nhiều dữ liệu.
    - **`AsNoTracking()`**: Nếu bạn chỉ cần **đọc dữ liệu** và **không có ý định cập nhật** nó trong cùng một `DbContext` instance, hãy sử dụng `AsNoTracking()`.
        
        ```
        // Chỉ đọc dữ liệu, không cần theo dõi thay đổi
        var readOnlyProducts = await context.Products
                                            .Where(p => p.IsAvailable)
                                            .AsNoTracking() // **Thêm dòng này**
                                            .ToListAsync();
        
        // Nếu bạn cố gắng sửa đổi và SaveChanges() trên readOnlyProducts,
        // EF Core sẽ không biết để tạo lệnh UPDATE (trừ khi bạn Attach lại)
        
        ```
        
    - **`Lợi ích của AsNoTracking():`**
        - **Nhanh hơn:** Query thực thi nhanh hơn vì bỏ qua bước tạo snapshot và theo dõi.
        - **Ít bộ nhớ hơn:** Không lưu snapshot trong `DbContext`.
    - **`Khi nào dùng AsNoTracking()?`** Trong hầu hết các kịch bản **chỉ đọc dữ liệu** (hiển thị danh sách, báo cáo, API GET không cần update...).
- IQueryables vs List Types
    
    Đây là một khái niệm **cực kỳ quan trọng** để hiểu cách EF Core hoạt động và viết query hiệu quả.
    
    - **`IQueryable<T>`**: Đại diện cho một **truy vấn chưa được thực thi**. Nó chứa một **biểu thức cây (Expression Tree)** mô tả các bước cần làm (lấy từ đâu, lọc gì, sắp xếp thế nào...).
        
        ```
        IQueryable<Product> query = context.Products.Where(p => p.Price > 50);
        // LÚC NÀY: Chưa có query nào được gửi đến database!
        // 'query' chỉ là một đối tượng mô tả "tôi muốn lấy Product có Price > 50"
        
        ```
        
    - **Deferred Execution (Thực thi trì hoãn):** Query chỉ thực sự được dịch sang SQL và gửi đến database khi bạn "hiện thực hóa" (materialize) `IQueryable` bằng các cách như:
        - Gọi các phương thức thực thi: `ToList()`, `ToArray()`, `FirstOrDefault()`, `Count()`, `Sum()`, `Max()`... (và các phiên bản `Async`).
        - Dùng vòng lặp `foreach` trên `IQueryable`.
    - **`IEnumerable<T>`** / **`List<T>`**: Đại diện cho một **tập hợp dữ liệu đã được tải vào bộ nhớ** của ứng dụng (in-memory collection).
    - **Vấn đề tiềm ẩn (Client-Side Evaluation):** Nếu bạn áp dụng các phương thức LINQ *sau khi* dữ liệu đã được tải vào bộ nhớ (ví dụ, gọi `ToList()` quá sớm rồi mới `Where()`), việc lọc/sắp xếp/tính toán sẽ diễn ra **tại client (ứng dụng của bạn)** thay vì tại database server. Điều này **rất không hiệu quả**.
        
        ```
        // **CÁCH LÀM SAI - KHÔNG HIỆU QUẢ**
        var allProductsInMemory = await context.Products.ToListAsync(); // Tải TẤT CẢ sản phẩm về client
        // Việc lọc diễn ra trong bộ nhớ ứng dụng, không dùng index của DB, tốn RAM/CPU client
        var expensiveProductsClientSide = allProductsInMemory.Where(p => p.Price > 1000);
        
        // **CÁCH LÀM ĐÚNG - HIỆU QUẢ**
        var expensiveProductsServerSide = await context.Products
                                                      .Where(p => p.Price > 1000) // Where() trên IQueryable -> dịch sang SQL WHERE
                                                      .ToListAsync(); // Chỉ tải các sản phẩm đã lọc về client
        ```
        
    - **Bài học:** **`*Hãy xây dựng toàn bộ query của bạn (Where, OrderBy, Select, Skip, Take...) trên IQueryable<T> trước khi gọi các phương thức thực thi như ToListAsync()*`**. Đảm bảo rằng càng nhiều công việc (lọc, sắp xếp, gom nhóm, tổng hợp) được thực hiện tại database càng tốt.
- Efficient Querying Tips and Tricks
    
    Tổng hợp lại các điểm quan trọng nhất:
    
    1. **`Use AsNoTracking() for Read-Only Queries:`** Giảm overhead đáng kể khi chỉ đọc dữ liệu.
    2. **`Project Only Necessary Data (Select)`**: Chỉ lấy những cột bạn thực sự cần, tránh lấy toàn bộ entity nếu không cần thiết. Dùng DTOs.
    3. **Filter and Order on the Server:** Áp dụng `Where()`, `OrderBy()`, `Skip()`, `Take()` trên `IQueryable` *trước khi* gọi `ToListAsync()` hoặc các phương thức thực thi khác.
    4. **`Use Asynchronous Methods (...Async)`**: Tránh block thread trong ứng dụng web/UI.
    5. **Beware of N+1 Problem:** Khi bạn load một danh sách các entity chính (ví dụ: `Categories`) rồi trong vòng lặp lại truy cập vào collection liên quan của từng entity (ví dụ: `category.Products`), EF Core có thể tạo ra 1 query ban đầu + N query tiếp theo (mỗi query cho từng entity chính).
        - **Giải pháp:** Dùng **Eager Loading** với `Include()` và `ThenInclude()` để bảo EF Core tải dữ liệu liên quan cùng lúc trong một (hoặc ít) query hơn.
            
            ```
            // N+1 Problem (potential):
            // var categories = await context.Categories.ToListAsync();
            // foreach (var cat in categories) {
            //    Console.WriteLine(cat.Name);
            //    // Dòng dưới có thể gây ra query riêng cho mỗi category nếu lazy loading bật
            //    foreach (var prod in cat.Products) { Console.WriteLine(prod.Name); }
            // }
            
            // Giải pháp Eager Loading:
            var categoriesWithProducts = await context.Categories
                                                      .Include(c => c.Products) // Tải luôn Products liên quan
                                                      // .ThenInclude(p => p.Supplier) // Có thể Include sâu hơn
                                                      .AsNoTracking() // Thường dùng với Include cho hiệu quả
                                                      .ToListAsync();
            // Bây giờ truy cập cat.Products sẽ không gây thêm query
            
            ```
            
        - Các kỹ thuật khác: Explicit Loading, Lazy Loading (cẩn thận khi dùng), Split Queries (EF Core 5+).
    6. **Avoid Complex Logic in LINQ that Can't Be Translated:** Một số phương thức C# phức tạp hoặc logic nghiệp vụ trong `Where()` hoặc `Select()` có thể không được EF Core dịch sang SQL. Nó có thể ném Exception hoặc tệ hơn là âm thầm thực hiện client-side evaluation. Giữ cho biểu thức LINQ tương đối đơn giản và kiểm tra SQL được tạo ra (bằng logging).
    7. **Use Database Indexes:** Đảm bảo các cột thường dùng trong `Where()` và `OrderBy()` được đánh index trong database. EF Core Migrations có thể giúp tạo index (`.HasIndex()`).
    8. **Check Generated SQL:** Dùng logging để xem SQL thực tế được tạo ra, đảm bảo nó hiệu quả và đúng ý đồ.
    9. **Consider Raw SQL Queries or Stored Procedures:** Đối với các query cực kỳ phức tạp hoặc cần tối ưu hóa đến mức tối đa mà LINQ không đáp ứng được, EF Core cho phép bạn thực thi SQL thô (`FromSqlRaw`, `ExecuteSqlRawAsync`) hoặc gọi Stored Procedure.
- Section Review
    - Truy vấn dữ liệu là một phần không thể thiếu khi làm việc với EF Core. Bằng cách hiểu rõ LINQ, các phương thức truy vấn, sự khác biệt giữa `IQueryable` và `IEnumerable`, cũng như các kỹ thuật tối ưu như `AsNoTracking` và `Select`, em có thể viết code hiệu quả, dễ bảo trì và đảm bảo hiệu năng tốt cho ứng dụng.
    - Hãy dành thời gian thực hành các kỹ thuật này với project của mình nhé. Nếu có bất kỳ câu hỏi nào, đừng ngần ngại hỏi anh!
- Section Source Code