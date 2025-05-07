# Demo Entity Framework Core Raw SQL, Views và Stored Procedures

Demo này minh họa cách làm việc với SQL thô, Views và Stored Procedures trong Entity Framework Core.

## Cấu trúc Project

```
Models/
  ├── Product.cs
  ├── Category.cs
  └── ProductSummary.cs
Data/
  └── ApplicationDbContext.cs
Migrations/
  └── 20240315000000_AddProductSummaryView.cs
Program.cs
```

## Các Tính Năng Demo

1. **Raw SQL Query**
   - Sử dụng FromSqlInterpolated để thực thi SQL thô
   - Truy vấn sản phẩm theo category và giá

2. **Composing LINQ with Raw SQL**
   - Kết hợp SQL thô với LINQ
   - Lọc và sắp xếp kết quả

3. **Querying View**
   - Sử dụng Keyless Entity để truy vấn View
   - Lọc và sắp xếp dữ liệu từ View

4. **Executing Non-Query SQL**
   - Thực thi lệnh UPDATE
   - Lấy số lượng bản ghi bị ảnh hưởng

5. **Querying Scalar Value**
   - Sử dụng ADO.NET để lấy giá trị đơn lẻ
   - Thực thi COUNT query

6. **Using UDF in LINQ**
   - Ánh xạ User-Defined Function
   - Sử dụng UDF trong LINQ query

## Cách Chạy Demo

1. Đảm bảo đã cài đặt SQL Server LocalDB
2. Mở terminal và di chuyển đến thư mục project
3. Chạy các lệnh sau:

```bash
dotnet restore
dotnet run
```

## Lưu ý

- Demo sử dụng SQL Server LocalDB làm database
- Dữ liệu mẫu được tạo tự động khi chạy ứng dụng
- View được tạo thông qua migration
- Sử dụng async/await cho tất cả các thao tác database

## Best Practices

1. **Raw SQL**
   - Luôn sử dụng tham số hóa để tránh SQL Injection
   - Ưu tiên FromSqlInterpolated thay vì FromSqlRaw
   - Kiểm tra SQL được tạo ra để đảm bảo hiệu năng

2. **Views**
   - Sử dụng Keyless Entity để ánh xạ View
   - Chỉ dùng View cho truy vấn đọc
   - Cập nhật View thông qua migration

3. **Stored Procedures và UDFs**
   - Ánh xạ UDF vào phương thức C#
   - Sử dụng tham số hóa khi gọi SP
   - Kiểm tra tính tương thích giữa các database provider 