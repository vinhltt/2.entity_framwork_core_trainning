# Tổng Quan Khóa Học Entity Framework Core

Kho lưu trữ này cung cấp một chương trình đào tạo thực hành toàn diện để làm chủ Entity Framework Core (EF Core) trong .NET. Khóa học được tổ chức thành các bài học lý thuyết (bằng cả tiếng Anh và tiếng Việt) và các dự án demo thực tế, bao quát mọi khía cạnh quan trọng từ khởi đầu đến các tính năng nâng cao.

## Cấu Trúc Đào Tạo

### 1. Bài Học Lý Thuyết

- **Ngôn ngữ:** Tiếng Anh (`EN/Lessons/`) và Tiếng Việt (`VI/Lessons/`)
- **Các Chủ Đề Bao Gồm:**
    - 01.Giới thiệu về ASP.NET Core và EF Core
    - 02.Bắt đầu với EF Core
    - 03.Truy vấn cơ sở dữ liệu với EF Core
    - 04.Thao tác dữ liệu với EF Core
    - 05.Quản lý thay đổi cơ sở dữ liệu và Migration
    - 06.Làm việc với các bản ghi liên quan (Quan hệ)
    - 07.Sử dụng Raw SQL, View và Stored Procedure
    - 08.Các tính năng bổ sung và thực tiễn tốt nhất

Mỗi bài học cung cấp giải thích chi tiết, ví dụ mã nguồn và các thực tiễn tốt nhất cho phát triển ứng dụng thực tế.

### 2. Dự Án Demo Thực Hành

Nằm trong thư mục `Example/`, mỗi dự án `LessonDemoXX` tương ứng với một bài học và minh họa các khái niệm qua mã nguồn:

- **LessonDemo02:** Bắt đầu với EF Core
- **LessonDemo03:** Truy vấn dữ liệu
- **LessonDemo04:** Thao tác dữ liệu
- **LessonDemo05:** Migration và thay đổi lược đồ
- **LessonDemo06:** Quan hệ (Một-Một, Một-Nhiều, Nhiều-Nhiều)
- **LessonDemo07:** Raw SQL, View, Stored Procedure và UDF
- **LessonDemo08:** Tính năng nâng cao (Soft Delete, Temporal Table, Đồng bộ, Transaction, Validation)

Mỗi demo bao gồm:
- Cấu trúc dự án rõ ràng (`Models/`, `Data/`, `Program.cs`)
- Dữ liệu mẫu và thiết lập cơ sở dữ liệu
- Hướng dẫn từng bước trong README của dự án
- Thực tiễn tốt nhất và lưu ý cho từng chủ đề

## Cách Sử Dụng

1. **Đọc các bài học lý thuyết** bằng ngôn ngữ bạn chọn để hiểu các khái niệm.
2. **Khám phá dự án demo tương ứng** để thấy các khái niệm được áp dụng thực tế.
3. **Làm theo hướng dẫn** trong README của từng demo để chạy và thử nghiệm mã nguồn.
4. **Áp dụng thực tiễn tốt nhất** và thử thay đổi để nâng cao hiểu biết.

## Yêu Cầu

- .NET SDK (khuyến nghị 8.0 hoặc mới hơn)
- SQL Server LocalDB (để chạy các demo)
- Kiến thức cơ bản về C# và .NET

## Bắt Đầu

Clone kho lưu trữ, di chuyển vào bất kỳ thư mục `LessonDemoXX` nào và làm theo hướng dẫn trong README để chạy demo.

```bash
dotnet restore
dotnet run
```

---

**Khóa học này phù hợp cho cả người mới bắt đầu và lập trình viên có kinh nghiệm muốn làm chủ EF Core cho các ứng dụng .NET hiện đại.** 