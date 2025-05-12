# Demo Entity Framework Core Additional Features

Demo này minh họa các tính năng bổ sung và cân nhắc quan trọng khi làm việc với Entity Framework Core.

## Cấu trúc Project

```
Models/
  ├── IAuditableEntity.cs
  ├── Product.cs
  └── Category.cs
Data/
  └── ApplicationDbContext.cs
Program.cs
```

## Các Tính Năng Demo

1. **Soft Delete**
   - Sử dụng query filter để tự động lọc các bản ghi đã xóa
   - Sử dụng IgnoreQueryFilters để truy vấn tất cả bản ghi

2. **Temporal Tables**
   - Cấu hình bảng temporal
   - Truy vấn lịch sử thay đổi
   - Xem dữ liệu tại một thời điểm cụ thể

3. **Concurrency Handling**
   - Sử dụng RowVersion để phát hiện xung đột
   - Xử lý DbUpdateConcurrencyException
   - Chiến lược giải quyết xung đột

4. **Transactions**
   - Sử dụng explicit transaction
   - Commit và rollback transaction
   - Kết hợp nhiều thao tác trong một transaction

5. **Data Validation**
   - Sử dụng Data Annotations
   - Validation rules
   - Xử lý validation errors

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
- Sử dụng async/await cho tất cả các thao tác database
- Có cấu hình retry policy cho database connection

## Best Practices

1. **Soft Delete**
   - Luôn sử dụng query filter để tránh truy vấn dữ liệu đã xóa
   - Cân nhắc sử dụng IgnoreQueryFilters khi cần truy vấn tất cả dữ liệu

2. **Temporal Tables**
   - Sử dụng cho các bảng cần theo dõi lịch sử thay đổi
   - Cấu hình rõ ràng tên bảng history và cột thời gian

3. **Concurrency**
   - Ưu tiên sử dụng RowVersion thay vì ConcurrencyCheck
   - Có chiến lược xử lý xung đột rõ ràng

4. **Transactions**
   - Sử dụng using statement để đảm bảo transaction được dispose
   - Xử lý rollback trong trường hợp lỗi

5. **Validation**
   - Kết hợp Data Annotations với Fluent API
   - Xử lý validation errors một cách phù hợp 