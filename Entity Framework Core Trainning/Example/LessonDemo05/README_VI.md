# Entity Framework Core Migrations Demo

Demo này minh họa cách sử dụng Entity Framework Core Migrations để quản lý thay đổi cấu trúc database.

## Cấu trúc Project

- `Models/`: Chứa các entity class
  - `Product.cs`: Model sản phẩm
  - `Category.cs`: Model danh mục
- `Data/`: Chứa DbContext và cấu hình
  - `ApplicationDbContext.cs`: DbContext chính
- `Program.cs`: File chính chứa các demo

## Các bước thực hiện

1. Tạo migration đầu tiên:
```bash
dotnet ef migrations add InitialCreate
```

2. Tạo migration cho thay đổi model:
```bash
dotnet ef migrations add AddNewField
```

3. Cập nhật database:
```bash
dotnet ef database update
```

4. Tạo script SQL:
```bash
dotnet ef migrations script -o migration.sql --idempotent
```

5. Rollback migration:
```bash
dotnet ef database update PreviousMigrationName
```

6. Xóa migration cuối cùng:
```bash
dotnet ef migrations remove
```

7. Tạo EF Bundle:
```bash
dotnet ef migrations bundle -o ./efbundle --force
```

## Các tính năng demo

1. **Apply Migrations at Runtime**: Sử dụng `Database.MigrateAsync()`
2. **Check Database Connection**: Kiểm tra kết nối database
3. **Get Pending Migrations**: Xem các migration chưa được áp dụng
4. **Get Applied Migrations**: Xem các migration đã được áp dụng

## Lưu ý

- Đảm bảo đã cài đặt SQL Server LocalDB
- Các lệnh migration cần được chạy trong thư mục gốc của project
- Luôn kiểm tra nội dung file migration trước khi áp dụng
- Sử dụng `--idempotent` khi tạo script SQL cho production
- Không xóa migration đã được áp dụng vào database 