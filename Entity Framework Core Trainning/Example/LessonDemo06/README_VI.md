# Demo Entity Framework Core Relationships

Demo này minh họa cách làm việc với các quan hệ trong Entity Framework Core, bao gồm:
- One-to-One (Author - AuthorContact)
- One-to-Many (Author - Books, Publisher - Books)
- Many-to-Many (Books - Categories)

## Cấu trúc Project

```
Models/
  ├── Author.cs
  ├── AuthorContact.cs
  ├── Book.cs
  ├── Publisher.cs
  ├── Category.cs
  └── BookCategory.cs
Data/
  └── ApplicationDbContext.cs
Program.cs
```

## Các Tính Năng Demo

1. **Eager Loading**
   - Load tất cả sách và thông tin liên quan (tác giả, nhà xuất bản, danh mục)
   - Sử dụng Include() và ThenInclude()

2. **Explicit Loading**
   - Load thông tin liên hệ của tác giả khi cần
   - Sử dụng Entry().Reference().Load()

3. **Projection**
   - Chỉ lấy thông tin cần thiết từ các quan hệ
   - Sử dụng Select() để tạo anonymous type

4. **Thêm Quan Hệ Mới**
   - Thêm sách mới với các quan hệ
   - Thêm nhiều categories cho một sách

5. **Cập Nhật Quan Hệ**
   - Cập nhật categories của sách
   - Xóa và thêm categories mới

6. **Xóa Quan Hệ**
   - Xóa categories trước khi xóa sách
   - Xóa sách và các quan hệ liên quan

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
- Các quan hệ được cấu hình với các ràng buộc phù hợp (cascade delete, restrict delete)
- Sử dụng async/await cho tất cả các thao tác database

## Best Practices

1. **Loading Related Data**
   - Sử dụng Eager Loading khi cần tất cả dữ liệu liên quan
   - Sử dụng Explicit Loading khi chỉ cần load một số quan hệ
   - Sử dụng Projection để tối ưu hiệu suất

2. **Managing Relationships**
   - Luôn xóa các quan hệ phụ thuộc trước khi xóa entity chính
   - Sử dụng transaction khi cần đảm bảo tính toàn vẹn dữ liệu
   - Cấu hình DeleteBehavior phù hợp cho từng quan hệ

3. **Performance**
   - Tránh N+1 query problem
   - Sử dụng AsNoTracking() khi chỉ cần đọc dữ liệu
   - Tối ưu các câu query bằng cách chỉ select các cột cần thiết 