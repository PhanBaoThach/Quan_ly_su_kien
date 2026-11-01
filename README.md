# 🎟️ EVENTMANA - HỆ THỐNG QUẢN LÝ & ĐẶT VÉ SỰ KIỆN TRỰC TUYẾN

> **Đồ án Công Nghệ Phần Mềm**  
> **Công nghệ:** ASP.NET Core MVC (.NET 10), Entity Framework Core  
> **Cơ sở dữ liệu:** Microsoft SQL Server  
> **Frontend:** HTML5, CSS3, Bootstrap 5, FontAwesome 6, Razor Views  

---

## 👥 THÀNH VIÊN NHÓM & PHÂN CÔNG TRÁCH NHIỆM

| STT | Thành Viên | Vai Trò | Nhiệm Vụ Phụ Trách & File Tương Ứng |
| :---: | :--- | :--- | :--- |
| **1** | **Phan Bảo Thạch** | **Nhóm trưởng** | • Kiến trúc Solution, Entity Framework Core, SQL Server<br>• Xác thực & Phân quyền ASP.NET Identity: [`AccountController.cs`](Quan_ly_su_kien/Controllers/AccountController.cs)<br>• Quản lý thông tin sự kiện: [`EventsController.cs`](Quan_ly_su_kien/Controllers/EventsController.cs)<br>• Quản lý đăng ký tham gia vé điện tử: `RegistrationsController.cs`<br>• Thông báo hệ thống & Cấu hình |
| **2** | **Trương Minh Dương** | **Thành viên** | • Quản lý tài khoản người dùng, khóa/mở tài khoản: [`AdminController.cs`](Quan_ly_su_kien/Controllers/AdminController.cs)<br>• Bộ lọc & Tìm kiếm sự kiện nâng cao: [`HomeController.cs`](Quan_ly_su_kien/Controllers/HomeController.cs)<br>• Quản lý khách mời, diễn giả & Check-in QR: [`GuestsController.cs`](Quan_ly_su_kien/Controllers/GuestsController.cs)<br>• Quản lý hồ sơ cá nhân & Nhật ký hoạt động (Audit Log) |
| **3** | **Bùi Thái Quý** | **Thành viên** | • Quản lý danh mục sự kiện (Categories): [`AdminController.cs`](Quan_ly_su_kien/Controllers/AdminController.cs)<br>• Quản lý loại vé & giá vé phát hành: [`TicketTypesController.cs`](Quan_ly_su_kien/Controllers/TicketTypesController.cs)<br>• Thống kê doanh thu & Báo cáo biểu đồ: `ReportsController.cs`<br>• Quản lý đánh giá/phản hồi sự kiện & Kiểm thử UI/UX |

---

## 🏗️ CẤU TRÚC THƯ MỤC DỰ ÁN (PROJECT ARCHITECTURE)

Dự án được tổ chức theo đúng chuẩn **ASP.NET Core MVC 3 lớp** để các thành viên khi clone về có thể làm việc song song mà không bị xung đột (conflict):

```text
Quan_ly_su_kien/
├── EventManagementDb.sql          # File kịch bản tạo CSDL SQL Server (chạy trong SSMS nếu cần)
├── README.md                      # Tài liệu hướng dẫn đồ án và phân chia công việc
├── Quan_ly_su_kien.slnx           # File Solution Visual Studio
└── Quan_ly_su_kien/               # Project Source Code
    ├── Controllers/               # Tầng điều khiển (Xử lý logic nghiệp vụ)
    │   ├── AccountController.cs   # Đăng nhập, đăng ký, đổi mật khẩu, phân quyền
    │   ├── AdminController.cs     # Bảng điều khiển quản trị, quản lý người dùng, quản lý danh mục
    │   ├── HomeController.cs      # Trang chủ, trang khám phá sự kiện, trang chi tiết sự kiện
    │   ├── EventsController.cs    # Quản lý sự kiện dành cho Ban tổ chức (Thạch)
    │   ├── TicketTypesController.cs # Quản lý các loại vé & giá vé (Quý)
    │   └── GuestsController.cs    # Quản lý khách mời & check-in QR (Dương)
    ├── Data/                      # Tầng truy xuất dữ liệu (Entity Framework Core)
    │   ├── ApplicationDbContext.cs# Cấu hình DbContext, DbSet và quan hệ Foreign Key
    │   └── DbInitializer.cs       # Tự động nạp dữ liệu mẫu (Roles, Users, Events, Categories)
    ├── Models/                    # Tầng thực thể dữ liệu (Entity Models)
    │   ├── ApplicationUser.cs     # Mở rộng IdentityUser (FullName, AvatarUrl, IsActive)
    │   ├── Category.cs            # Danh mục sự kiện
    │   ├── Event.cs               # Thông tin sự kiện
    │   ├── TicketType.cs          # Hạng vé & giá tiền
    │   ├── Registration.cs        # Đơn đặt vé & mã QR
    │   ├── Guest.cs               # Khách mời & check-in
    │   ├── EventFeedback.cs       # Đánh giá & nhận xét
    │   ├── Notification.cs        # Thông báo người dùng
    │   ├── SystemConfig.cs        # Cấu hình website
    │   └── AuditLog.cs            # Nhật ký hoạt động
    ├── ViewModels/                # Dữ liệu truyền giữa Controller và View
    │   ├── LoginViewModel.cs
    │   ├── RegisterViewModel.cs
    │   ├── ProfileViewModel.cs
    │   └── UserManagementViewModel.cs
    ├── Views/                     # Tầng giao diện người dùng (Razor Pages / HTML5 / Bootstrap 5)
    │   ├── Account/               # Views: Login, Register, Profile, AccessDenied
    │   ├── Admin/                 # Views: Index (Dashboard), Users, Categories
    │   ├── Home/                  # Views: Index, Events, EventDetail, Privacy
    │   └── Shared/                # Views dùng chung: _Layout.cshtml, _ValidationScriptsPartial
    ├── wwwroot/                   # Tài nguyên tĩnh
    │   ├── css/site.css           # Custom CSS của dự án
    │   └── js/site.js             # Custom JS
    ├── appsettings.json           # Cấu hình chuỗi kết nối CSDL SQL Server
    └── Program.cs                 # Cấu hình Middleware, Identity, Pipeline và Seeder
```

---

## 🚀 HƯỚNG DẪN DÀNH CHO THÀNH VIÊN KHI CLONE CODE

### Bước 1: Mở dự án
- Mở Visual Studio $\rightarrow$ Chọn **Open a project or solution** $\rightarrow$ Chọn file `Quan_ly_su_kien.slnx`.

### Bước 2: Kiểm tra chuỗi kết nối CSDL trong `appsettings.json`
- Mở file `Quan_ly_su_kien/appsettings.json`, kiểm tra xem tên Server SQL có khớp với máy tính của bạn không:
  - Nếu dùng **SQL Server Express** (phổ biến nhất khi cài SSMS):
    ```json
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=EventManagementDb;Trusted_Connection=True;TrustServerCertificate=True;"
    ```
  - Nếu dùng **Localhost** hoặc `.` (bản Developer/Enterprise):
    ```json
    "DefaultConnection": "Server=localhost;Database=EventManagementDb;Trusted_Connection=True;TrustServerCertificate=True;"
    ```

### Bước 3: Chạy ứng dụng
- Nhấn **F5** hoặc **Ctrl + F5** trong Visual Studio (hoặc chạy lệnh `dotnet run --project Quan_ly_su_kien` từ Terminal).
- **Hệ thống sẽ tự động**:
  1. Tự tạo Database `EventManagementDb` nếu máy bạn chưa có.
  2. Tự nạp 3 vai trò (`Admin`, `Organizer`, `Attendee`).
  3. Tự nạp 3 tài khoản thử nghiệm của nhóm và các sự kiện mẫu có sẵn.

---

## 🔑 TÀI KHOẢN MẪU ĐỂ KIỂM THỬ HỆ THỐNG

| Vai trò (Role) | Tên đăng nhập | Email | Mật khẩu | Quyền hạn chính |
| :--- | :--- | :--- | :--- | :--- |
| **Quản Trị Viên (Admin)** | `admin` | `kkaitou37@gmail.com` | `Admin@123` | Toàn quyền quản trị hệ thống, quản lý tài khoản người dùng, danh mục sự kiện |
| **Ban Tổ Chức (Organizer)**| `truongminhduong` | `duong@gmail.com` | `Org@123` | Tạo và quản lý sự kiện, phát hành vé, check-in khách mời |
| **Thành Viên (Attendee)** | `buithaiquy` | `quy@gmail.com` | `User@123` | Đặt vé, xem sự kiện, nhận mã QR, viết đánh giá |

*(Trên màn hình Đăng nhập cũng có sẵn danh sách tài khoản mẫu để bạn bấm 1 chạm đăng nhập ngay).*

---

## 🌿 QUY TẮC PHỐI HỢP GIT & CODE CONVENTION

1. **Nhánh Git (Branches)**:
   - `main`: Nhánh chạy ổn định, chỉ merge khi tính năng đã hoàn thiện.
   - `develop`: Nhánh tích hợp code chung giữa các thành viên.
   - Khi làm chức năng mới, hãy tạo nhánh từ `develop`:
     ```bash
     git checkout develop
     git pull origin develop
     git checkout -b feature/ten-chuc-nang
     ```
2. **Quy tắc thêm giao diện View**:
   - Mọi view mới đều kế thừa `_Layout.cshtml` mặc định.
   - Sử dụng các class của Bootstrap 5 (`card`, `table`, `btn-primary`, `badge`, `shadow-sm`) để đồng nhất giao diện toàn hệ thống.
3. **Quy tắc phân quyền Endpoint**:
   - Trang chỉ dành cho Quản trị viên: thêm `[Authorize(Roles = "Admin")]` trên Controller hoặc Action.
   - Trang dành cho Ban tổ chức: thêm `[Authorize(Roles = "Organizer,Admin")]`.
   - Trang yêu cầu đăng nhập: thêm `[Authorize]`.

