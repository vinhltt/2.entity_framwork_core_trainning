# Using Entity Framework Core to Manipulate Database

- **Section Overview**
    - Trong section này, chúng ta sẽ tìm hiểu cách sử dụng Entity Framework Core để thao tác với dữ liệu trong database một cách hiệu quả và an toàn.
    - Các chủ đề chính bao gồm:
        - Hiểu về cơ chế Tracking và lưu thay đổi trong EF Core
        - Thực hiện các thao tác CRUD cơ bản (Create, Read, Update, Delete)
        - Các phương thức mới trong EF Core 7+ để tối ưu hiệu năng
        - Quản lý trạng thái của entities
        - Xử lý các thao tác hàng loạt (bulk operations)
    - Mục tiêu của section này là giúp bạn:
        - Hiểu và sử dụng hiệu quả Change Tracker
        - Thực hiện các thao tác dữ liệu một cách an toàn và hiệu quả
        - Áp dụng các best practices khi thao tác với database
        - Tối ưu hiệu năng cho các thao tác dữ liệu
        - Xử lý các thao tác hàng loạt một cách hiệu quả

- Understanding Tracking and Saving Changes
    
    Đây là cơ chế nền tảng của EF Core khi làm việc với dữ liệu.
    
    - **`DbContext`** là Unit of Work: Như đã đề cập, `DbContext` không chỉ là cầu nối mà còn hoạt động như một "Unit of Work". Nó quản lý một tập hợp các đối tượng (entities) và theo dõi trạng thái của chúng trong suốt vòng đời của `DbContext` instance.
    - **Change Tracker:** Mỗi `DbContext` instance có một `ChangeTracker`. Nhiệm vụ của nó là:
        - **Tự động theo dõi:** Khi bạn truy vấn dữ liệu từ database bằng `DbContext`, các đối tượng entity trả về sẽ tự động được `DbContext` theo dõi.
        - **Theo dõi khi thao tác:** Khi bạn `Add` một entity mới, `Remove` một entity, hoặc **thay đổi giá trị thuộc tính** của một entity đang được theo dõi, `ChangeTracker` sẽ ghi nhận những thay đổi này.
    - **Entity States (Trạng thái của Entity):** `ChangeTracker` duy trì trạng thái (State) cho mỗi entity mà nó quản lý. Các trạng thái chính bao gồm:
        - `Detached`: Entity không được `DbContext` theo dõi. Đây là trạng thái của một đối tượng mới tạo hoặc đối tượng lấy từ một `DbContext` khác hoặc đã bị detach.
        - `Unchanged`: Entity đang được theo dõi và không có thay đổi nào so với dữ liệu gốc từ database (hoặc từ lần `SaveChanges()` cuối cùng).
        - `Added`: Entity mới được thêm vào `DbContext` (dùng `Add` hoặc `AddRange`) và chưa tồn tại trong database. `SaveChanges()` sẽ tạo lệnh `INSERT`.
        - `Modified`: Một hoặc nhiều thuộc tính của entity đang được theo dõi đã bị thay đổi giá trị. `SaveChanges()` sẽ tạo lệnh `UPDATE`.
        - `Deleted`: Entity đang được theo dõi và đã được đánh dấu để xóa (dùng `Remove` hoặc `RemoveRange`). `SaveChanges()` sẽ tạo lệnh `DELETE`.
        - Bạn có thể xem trạng thái của một entity bằng `context.Entry(entity).State`.
    - **`SaveChanges()`** / **`SaveChangesAsync()`**: Đây là phương thức **then chốt** để lưu các thay đổi vào database. Khi bạn gọi nó:
        1. `DbContext` quét qua tất cả các entity mà `ChangeTracker` đang theo dõi.
        2. Nó phát hiện các entity có trạng thái là `Added`, `Modified`, hoặc `Deleted`.
        3. Dựa trên trạng thái, nó tạo ra các câu lệnh SQL tương ứng (`INSERT`, `UPDATE`, `DELETE`).
        4. Nó thực thi các câu lệnh này trong một **transaction** duy nhất (mặc định). Nếu có lỗi xảy ra, toàn bộ transaction sẽ được rollback, đảm bảo tính toàn vẹn dữ liệu.
        5. Sau khi thực thi thành công, trạng thái của các entity sẽ được cập nhật (ví dụ: `Added` -> `Unchanged`, `Modified` -> `Unchanged`, `Deleted` -> `Detached`). Các giá trị do database tạo ra (như khóa chính tự tăng) cũng sẽ được cập nhật lại vào đối tượng entity của bạn.
        
        ```
        // Bất kỳ thay đổi nào trước dòng này (Add, Update, Remove) sẽ được chuẩn bị
        int affectedRows = await context.SaveChangesAsync(); // Thực thi SQL và lưu thay đổi
        // affectedRows chứa số lượng bản ghi bị ảnh hưởng trong database
        
        ```
        
- Simple Insert Operations
    - **Quy trình:** Tạo đối tượng -> Thêm vào Context -> Lưu thay đổi.
    - **Các bước:**
        1. **Tạo instance mới:** Tạo một đối tượng mới của lớp entity bạn muốn thêm.
            
            ```
            var newProduct = new Product
            {
                Name = "New Amazing Gadget",
                Price = 199.99m,
                CategoryId = 1, // Giả sử CategoryId = 1 tồn tại
                IsAvailable = true
            };
            
            ```
            
        2. **Thêm vào DbContext:** Sử dụng phương thức `Add()` hoặc `AddAsync()` của `DbSet<T>` (hoặc `DbContext.Add(entity)`). Thao tác này đính kèm entity vào `DbContext` và đánh dấu trạng thái của nó là `Added`.
            
            ```
            context.Products.Add(newProduct);
            // Hoặc: await context.Products.AddAsync(newProduct);
            // Hoặc: context.Add(newProduct);
            
            ```
            
        3. **Thêm nhiều bản ghi:** Dùng `AddRange()` hoặc `AddRangeAsync()` để thêm nhiều entity cùng lúc, hiệu quả hơn gọi `Add()` nhiều lần.
            
            ```
            var productsToAdd = new List<Product>
            {
                new Product { Name = "Product A", Price = 10 },
                new Product { Name = "Product B", Price = 20 }
            };
            context.Products.AddRange(productsToAdd);
            // Hoặc: await context.Products.AddRangeAsync(productsToAdd);
            
            ```
            
        4. **Lưu thay đổi:** Gọi `await context.SaveChangesAsync()`. EF Core sẽ tạo và thực thi lệnh `INSERT` cho các entity có trạng thái `Added`.
            
            ```
            await context.SaveChangesAsync();
            
            // Sau khi SaveChangesAsync thành công:
            // 1. Trạng thái của newProduct (và các product trong productsToAdd) trở thành Unchanged.
            // 2. Nếu Id là khóa chính tự tăng, newProduct.Id sẽ được cập nhật giá trị mới từ database.
            Console.WriteLine($"New product added with ID: {newProduct.Id}");
            
            ```
            
- Simple Update Operations
    - **Quy trình chuẩn:** Truy vấn -> Sửa đổi -> Lưu thay đổi.
    - **Các bước:**
        1. **Truy vấn (Query):** Lấy entity bạn muốn cập nhật từ database bằng `DbContext`. Entity này bây giờ sẽ được `ChangeTracker` theo dõi với trạng thái `Unchanged`.
            
            ```
            int productIdToUpdate = 101; // ID của sản phẩm cần cập nhật
            var productToUpdate = await context.Products.FindAsync(productIdToUpdate);
            // Hoặc dùng FirstOrDefaultAsync, SingleOrDefaultAsync...
            // var productToUpdate = await context.Products.FirstOrDefaultAsync(p => p.Id == productIdToUpdate);
            
            if (productToUpdate == null)
            {
                Console.WriteLine("Product not found!");
                return;
            }
            // Tại thời điểm này, context.Entry(productToUpdate).State là Unchanged
            
            ```
            
        2. **Sửa đổi (Modify):** Thay đổi giá trị các thuộc tính mong muốn của đối tượng entity vừa lấy về.
            
            ```
            Console.WriteLine($"Old price: {productToUpdate.Price}");
            productToUpdate.Price = productToUpdate.Price * 1.1m; // Tăng giá 10%
            productToUpdate.IsAvailable = false; // Đánh dấu không còn hàng
            
            // Ngay sau khi bạn thay đổi thuộc tính đầu tiên (Price),
            // ChangeTracker sẽ tự động phát hiện và đổi trạng thái của productToUpdate thành Modified.
            // Console.WriteLine($"State after modification: {context.Entry(productToUpdate).State}"); // Output: Modified
            
            ```
            
        3. **Lưu thay đổi (Save):** Gọi `await context.SaveChangesAsync()`. EF Core sẽ:
            - Phát hiện `productToUpdate` có trạng thái `Modified`.
            - Tạo một câu lệnh `UPDATE` chỉ bao gồm các cột đã bị thay đổi (trong ví dụ này là `Price` và `IsAvailable`).
            - Thực thi lệnh `UPDATE` vào database.
            - Cập nhật trạng thái của `productToUpdate` về `Unchanged`.
            
            ```
            await context.SaveChangesAsync();
            Console.WriteLine($"Product {productIdToUpdate} updated. New price: {productToUpdate.Price}");
            // Console.WriteLine($"State after save: {context.Entry(productToUpdate).State}"); // Output: Unchanged
            ```
            
- Simple Delete Operations
    - **Quy trình:** Truy vấn -> Đánh dấu xóa -> Lưu thay đổi.
    - **Các bước:**
        1. **Truy vấn (Query):** Lấy entity bạn muốn xóa từ database. Entity này được theo dõi với trạng thái `Unchanged`.
            
            ```
            int productIdToDelete = 102; // ID của sản phẩm cần xóa
            var productToDelete = await context.Products.FindAsync(productIdToDelete);
            
            if (productToDelete == null)
            {
                Console.WriteLine("Product not found!");
                return;
            }
            // context.Entry(productToDelete).State là Unchanged
            
            ```
            
        2. **Đánh dấu xóa (Remove):** Sử dụng phương thức `Remove()` của `DbSet<T>` (hoặc `DbContext.Remove(entity)`). Thao tác này **không xóa entity ngay lập tức** mà chỉ thay đổi trạng thái của nó trong `ChangeTracker` thành `Deleted`.
            
            ```
            context.Products.Remove(productToDelete);
            // Hoặc: context.Remove(productToDelete);
            
            // Console.WriteLine($"State after remove: {context.Entry(productToDelete).State}"); // Output: Deleted
            
            ```
            
        3. **Xóa nhiều bản ghi:** Dùng `RemoveRange()` để đánh dấu xóa nhiều entity cùng lúc.
            
            ```
            // var productsToDelete = await context.Products.Where(p => p.Price < 10).ToListAsync();
            // context.Products.RemoveRange(productsToDelete);
            
            ```
            
        4. **Lưu thay đổi (Save):** Gọi `await context.SaveChangesAsync()`. EF Core sẽ:
            - Phát hiện `productToDelete` có trạng thái `Deleted`.
            - Tạo và thực thi lệnh `DELETE` tương ứng trong database.
            - Sau khi xóa thành công, entity sẽ bị **ngắt kết nối (detach)** khỏi `DbContext` (trạng thái trở thành `Detached`).
            
            ```
            int affectedRows = await context.SaveChangesAsync();
            if (affectedRows > 0)
            {
                Console.WriteLine($"Product {productIdToDelete} deleted successfully.");
                // Console.WriteLine($"State after save: {context.Entry(productToDelete).State}"); // Output: Detached
            }
            
            ```
            
- ExecuteUpdate and ExecuteDelete (>= EF Core 7)
    
    Đây là các phương thức mới được giới thiệu từ EF Core 7, cung cấp một cách **hiệu quả hơn** để thực hiện các thao tác cập nhật hoặc xóa **hàng loạt (bulk)** mà **không cần tải dữ liệu vào bộ nhớ** và **không cần thông qua Change Tracker**.
    
    - **Motivation:** Khi bạn muốn cập nhật/xóa nhiều bản ghi dựa trên một điều kiện (ví dụ: tăng giá 5% cho tất cả sản phẩm trong một danh mục, hoặc xóa tất cả đơn hàng cũ hơn 1 năm), việc tải từng entity về, sửa đổi/xóa rồi lưu lại có thể rất tốn kém tài nguyên (bộ nhớ, CPU, thời gian).
    - **Cách hoạt động:** Các phương thức này cho phép bạn xây dựng một truy vấn LINQ để xác định các bản ghi cần thao tác (`Where()`), sau đó chỉ định các hành động cập nhật (`ExecuteUpdateAsync`) hoặc chỉ đơn giản là xóa (`ExecuteDeleteAsync`). EF Core sẽ dịch trực tiếp thành một câu lệnh `UPDATE` hoặc `DELETE` duy nhất và thực thi nó trên database.
    - **`ExecuteDeleteAsync()`**: Xóa các bản ghi khớp với điều kiện `Where()`.
        
        ```
        // Xóa tất cả sản phẩm có giá dưới 10
        int deletedCount = await context.Products
                                        .Where(p => p.Price < 10)
                                        .ExecuteDeleteAsync(); // Thực thi DELETE ngay lập tức
        
        Console.WriteLine($"{deletedCount} products were deleted.");
        // **KHÔNG cần gọi SaveChangesAsync()**
        
        ```
        
    - **`ExecuteUpdateAsync()`**: Cập nhật các cột được chỉ định cho các bản ghi khớp với điều kiện `Where()`.
        
        ```
        // Tăng giá 5% và đánh dấu không còn hàng cho các sản phẩm thuộc CategoryId = 2
        int updatedCount = await context.Products
                                        .Where(p => p.CategoryId == 2)
                                        .ExecuteUpdateAsync(setters => setters
                                            .SetProperty(p => p.Price, p => p.Price * 1.05m) // Cập nhật Price dựa trên giá trị hiện tại
                                            .SetProperty(p => p.IsAvailable, false) // Cập nhật IsAvailable thành giá trị cố định
                                        ); // Thực thi UPDATE ngay lập tức
        
        Console.WriteLine($"{updatedCount} products were updated.");
        // **KHÔNG cần gọi SaveChangesAsync()**
        
        ```
        
    - **Ưu điểm:**
        - **Hiệu quả cao:** Thực thi một lệnh SQL duy nhất cho nhiều bản ghi.
        - **Tiết kiệm bộ nhớ:** Không cần load entities vào bộ nhớ client.
        - **Nhanh chóng:** Bỏ qua overhead của Change Tracker.
    - **Nhược điểm và Lưu ý:**
        - **Bỏ qua Change Tracker:** Không theo dõi thay đổi, không tự động cập nhật trạng thái các entity (nếu có) trong `DbContext`.
        - **Thực thi ngay lập tức:** Lệnh SQL được gửi đi ngay khi gọi phương thức, không chờ `SaveChangesAsync()`.
        - **`Không kích hoạt sự kiện/interceptors của SaveChanges():`** Nếu bạn có logic tùy chỉnh trong `SaveChanges()`, nó sẽ bị bỏ qua.
        - **Concurrency Control:** Cơ chế kiểm soát xung đột đồng thời (concurrency control) dựa trên Change Tracker không được áp dụng.
        - **Cascading Actions:** Các hành động xóa/cập nhật dây chuyền (cascading) có thể hoạt động khác biệt (phụ thuộc vào cấu hình database) so với khi dùng `SaveChanges()`.
    - **Khi nào dùng?** Khi cần thực hiện các thao tác UPDATE/DELETE đơn giản trên **nhiều bản ghi** dựa trên một bộ lọc và **hiệu năng là ưu tiên hàng đầu**. Không thay thế hoàn toàn cho phương pháp dùng Change Tracker, đặc biệt khi cần xử lý logic phức tạp hoặc cập nhật các mối quan hệ.
- Section Review
    - `DbContext` và `ChangeTracker` là trung tâm của việc quản lý thay đổi trong EF Core, theo dõi trạng thái (`Added`, `Modified`, `Deleted`...) của các entities.
    - `SaveChangesAsync()` là lệnh cuối cùng để chuyển các thay đổi được theo dõi thành các lệnh SQL (`INSERT`, `UPDATE`, `DELETE`) và thực thi chúng trong một transaction.
    - Thao tác **Thêm (Insert)**: Tạo mới -> `Add()`/`AddRange()` -> `SaveChangesAsync()`.
    - Thao tác **Cập nhật (Update)** chuẩn: Truy vấn -> Sửa đổi thuộc tính -> `SaveChangesAsync()`.
    - Thao tác **Xóa (Delete)** chuẩn: Truy vấn -> `Remove()`/`RemoveRange()` -> `SaveChangesAsync()`.
    - **`ExecuteUpdateAsync()`** và **`ExecuteDeleteAsync()`** (EF Core 7+) cung cấp cách hiệu quả để cập nhật/xóa hàng loạt trực tiếp trên database, bỏ qua Change Tracker, hữu ích cho tối ưu hiệu năng trong các kịch bản cụ thể.
- Section Source Code