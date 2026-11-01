-- =====================================================
-- HỆ THỐNG QUẢN LÝ SỰ KIỆN - SQL SERVER SCRIPT
-- Công nghệ: ASP.NET Core MVC (.NET 8), Entity Framework Core
-- =====================================================

CREATE DATABASE EventManagementDb;
GO

USE EventManagementDb;
GO

-- 1. Bảng AspNetRoles (Phân quyền chuẩn Identity)
CREATE TABLE AspNetRoles (
    Id NVARCHAR(450) NOT NULL PRIMARY KEY,
    Name NVARCHAR(256) NULL,
    NormalizedName NVARCHAR(256) NULL,
    ConcurrencyStamp NVARCHAR(MAX) NULL
);

-- 2. Bảng AspNetUsers (Quản lý tài khoản người dùng + mở rộng thông tin)
CREATE TABLE AspNetUsers (
    Id NVARCHAR(450) NOT NULL PRIMARY KEY,
    UserName NVARCHAR(256) NULL,
    NormalizedUserName NVARCHAR(256) NULL,
    Email NVARCHAR(256) NULL,
    NormalizedEmail NVARCHAR(256) NULL,
    EmailConfirmed BIT NOT NULL,
    PasswordHash NVARCHAR(MAX) NULL,
    SecurityStamp NVARCHAR(MAX) NULL,
    ConcurrencyStamp NVARCHAR(MAX) NULL,
    PhoneNumber NVARCHAR(MAX) NULL,
    PhoneNumberConfirmed BIT NOT NULL,
    TwoFactorEnabled BIT NOT NULL,
    LockoutEnd DATETIMEOFFSET NULL,
    LockoutEnabled BIT NOT NULL,
    AccessFailedCount INT NOT NULL,
    -- Thuộc tính bổ sung theo User Story (US-02, US-11)
    FullName NVARCHAR(150) NULL,
    AvatarUrl NVARCHAR(MAX) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
);

-- 3. Bảng AspNetUserRoles
CREATE TABLE AspNetUserRoles (
    UserId NVARCHAR(450) NOT NULL,
    RoleId NVARCHAR(450) NOT NULL,
    CONSTRAINT PK_AspNetUserRoles PRIMARY KEY (UserId, RoleId),
    CONSTRAINT FK_AspNetUserRoles_Roles FOREIGN KEY (RoleId) REFERENCES AspNetRoles(Id) ON DELETE CASCADE,
    CONSTRAINT FK_AspNetUserRoles_Users FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE
);

-- 3.1. Bảng AspNetUserClaims (Bắt buộc cho ASP.NET Core Identity SignInManager)
CREATE TABLE AspNetUserClaims (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UserId NVARCHAR(450) NOT NULL,
    ClaimType NVARCHAR(MAX) NULL,
    ClaimValue NVARCHAR(MAX) NULL,
    CONSTRAINT FK_AspNetUserClaims_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE
);

-- 3.2. Bảng AspNetRoleClaims
CREATE TABLE AspNetRoleClaims (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    RoleId NVARCHAR(450) NOT NULL,
    ClaimType NVARCHAR(MAX) NULL,
    ClaimValue NVARCHAR(MAX) NULL,
    CONSTRAINT FK_AspNetRoleClaims_AspNetRoles_RoleId FOREIGN KEY (RoleId) REFERENCES AspNetRoles(Id) ON DELETE CASCADE
);

-- 3.3. Bảng AspNetUserLogins
CREATE TABLE AspNetUserLogins (
    LoginProvider NVARCHAR(128) NOT NULL,
    ProviderKey NVARCHAR(128) NOT NULL,
    ProviderDisplayName NVARCHAR(MAX) NULL,
    UserId NVARCHAR(450) NOT NULL,
    CONSTRAINT PK_AspNetUserLogins PRIMARY KEY (LoginProvider, ProviderKey),
    CONSTRAINT FK_AspNetUserLogins_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE
);

-- 3.4. Bảng AspNetUserTokens
CREATE TABLE AspNetUserTokens (
    UserId NVARCHAR(450) NOT NULL,
    LoginProvider NVARCHAR(128) NOT NULL,
    Name NVARCHAR(128) NOT NULL,
    Value NVARCHAR(MAX) NULL,
    CONSTRAINT PK_AspNetUserTokens PRIMARY KEY (UserId, LoginProvider, Name),
    CONSTRAINT FK_AspNetUserTokens_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE
);

-- 4. Bảng Categories (Quản lý danh mục sự kiện - US-03)
CREATE TABLE Categories (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(150) NOT NULL,
    Description NVARCHAR(500) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
);

-- 5. Bảng Events (Quản lý thông tin sự kiện - US-03, US-04)
CREATE TABLE Events (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX) NOT NULL,
    Location NVARCHAR(250) NOT NULL,
    StartDate DATETIME2 NOT NULL,
    EndDate DATETIME2 NOT NULL,
    ImageUrl NVARCHAR(MAX) NULL,
    MaxTickets INT NOT NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT 'Draft', -- Draft, Open, Ongoing, Cancelled, Completed
    CategoryId INT NOT NULL,
    OrganizerId NVARCHAR(450) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Events_Categories FOREIGN KEY (CategoryId) REFERENCES Categories(Id) ON DELETE NO ACTION,
    CONSTRAINT FK_Events_Organizer FOREIGN KEY (OrganizerId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE
);

-- 6. Bảng TicketTypes (Quản lý loại vé & giá vé - US-06)
CREATE TABLE TicketTypes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    EventId INT NOT NULL,
    TypeName NVARCHAR(100) NOT NULL, -- Miễn phí, Thường, VIP
    Price DECIMAL(18,2) NOT NULL DEFAULT 0,
    Quantity INT NOT NULL,
    SoldQuantity INT NOT NULL DEFAULT 0,
    SaleStart DATETIME2 NOT NULL,
    SaleEnd DATETIME2 NOT NULL,
    CONSTRAINT FK_TicketTypes_Events FOREIGN KEY (EventId) REFERENCES Events(Id) ON DELETE CASCADE
);

-- 7. Bảng Registrations (Quản lý đăng ký tham gia & vé điện tử - US-07)
CREATE TABLE Registrations (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    EventId INT NOT NULL,
    UserId NVARCHAR(450) NOT NULL,
    TicketTypeId INT NOT NULL,
    Quantity INT NOT NULL DEFAULT 1,
    TotalPrice DECIMAL(18,2) NOT NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT 'Pending', -- Pending, Confirmed, Cancelled
    CheckInStatus BIT NOT NULL DEFAULT 0,
    CheckInTime DATETIME2 NULL,
    QrCode NVARCHAR(100) NOT NULL,
    RegisteredAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Registrations_Events FOREIGN KEY (EventId) REFERENCES Events(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Registrations_Users FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE NO ACTION,
    CONSTRAINT FK_Registrations_TicketTypes FOREIGN KEY (TicketTypeId) REFERENCES TicketTypes(Id) ON DELETE NO ACTION
);

-- 8. Bảng Guests (Quản lý khách mời và diễn giả - US-08)
CREATE TABLE Guests (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    EventId INT NOT NULL,
    FullName NVARCHAR(150) NOT NULL,
    Email NVARCHAR(256) NOT NULL,
    RoleTitle NVARCHAR(150) NULL, -- Diễn giả, Khách mời đặc biệt
    InvitationCode NVARCHAR(100) NOT NULL,
    IsCheckedIn BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Guests_Events FOREIGN KEY (EventId) REFERENCES Events(Id) ON DELETE CASCADE
);

-- 9. Bảng EventFeedbacks (Quản lý đánh giá và phản hồi sự kiện - US-12)
CREATE TABLE EventFeedbacks (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    EventId INT NOT NULL,
    UserId NVARCHAR(450) NOT NULL,
    Rating INT NOT NULL CHECK (Rating BETWEEN 1 AND 5),
    Comment NVARCHAR(1000) NULL,
    IsHidden BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Feedbacks_Events FOREIGN KEY (EventId) REFERENCES Events(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Feedbacks_Users FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE NO ACTION
);

-- 10. Bảng Notifications (Thông báo tự động & nhắc lịch - US-10)
CREATE TABLE Notifications (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId NVARCHAR(450) NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    Message NVARCHAR(MAX) NOT NULL,
    IsRead BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Notifications_Users FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE
);

-- 11. Bảng SystemConfigs (Quản lý cấu hình hệ thống - US-13)
CREATE TABLE SystemConfigs (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ConfigKey NVARCHAR(150) NOT NULL UNIQUE,
    ConfigValue NVARCHAR(MAX) NOT NULL,
    Description NVARCHAR(300) NULL
);

-- 12. Bảng AuditLogs (Nhật ký hoạt động hệ thống - US-14)
CREATE TABLE AuditLogs (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId NVARCHAR(450) NULL,
    Action NVARCHAR(100) NOT NULL,
    Details NVARCHAR(MAX) NOT NULL,
    IpAddress NVARCHAR(50) NULL,
    Timestamp DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_AuditLogs_Users FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE SET NULL
);
GO

-- =====================================================
-- THÊM DỮ LIỆU MẪU (SEED DATA)
-- =====================================================

-- 1. Thêm Roles
INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES 
('role-admin-id', 'Admin', 'ADMIN', NEWID()),
('role-org-id', 'Organizer', 'ORGANIZER', NEWID()),
('role-attendee-id', 'Attendee', 'ATTENDEE', NEWID());

-- 2. Thêm Users (Mật khẩu mẫu đã mã hóa dạng Hash)
INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, FullName, IsActive) VALUES 
('user-admin-1', 'phản bảo thạch', 'PHAN BAO THACH', 'kkaitou37@gmail.com', 'KKAITOU37@GMAIL.COM', 1, 'AQAAAAEAACcQAAAAEF...samplehash...', N'Phan Bảo Thạch (Admin)', 1),
('user-org-1', 'truongminhduong', 'TRUONG MINH DUONG', 'duong@gmail.com', 'DUONG@GMAIL.COM', 1, 'AQAAAAEAACcQAAAAEF...samplehash...', N'Trương Minh Dương (Ban Tổ Chức)', 1),
('user-att-1', 'buithaiquy', 'BUI THAI QUY', 'quy@gmail.com', 'QUY@GMAIL.COM', 1, 'AQAAAAEAACcQAAAAEF...samplehash...', N'Bùi Thái Quý (Người Tham Gia)', 1);

-- 3. Gán Role cho Users
INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES 
('user-admin-1', 'role-admin-id'),
('user-org-1', 'role-org-id'),
('user-att-1', 'role-attendee-id');

-- 4. Thêm Categories (Danh mục sự kiện)
INSERT INTO Categories (Name, Description, IsActive) VALUES 
(N'Công nghệ thông tin', N'Các hội thảo, cuộc thi lập trình, workshop về công nghệ mới', 1),
(N'Âm nhạc & Nghệ thuật', N'Đại nhạc hội, triển lãm nghệ thuật, biểu diễn âm nhạc', 1),
(N'Thể thao & Sức khỏe', N'Giải chạy marathon, hội thao sinh viên, yoga', 1);

-- 5. Thêm Events (Thông tin sự kiện)
INSERT INTO Events (Title, Description, Location, StartDate, EndDate, ImageUrl, MaxTickets, Status, CategoryId, OrganizerId) VALUES 
(N'Hội Thảo Công Nghệ .NET 8 & AI', N'Cập nhật những xu hướng công nghệ mới nhất cùng các chuyên gia hàng đầu về ASP.NET Core và AI tích hợp.', N'Hội trường A, Đại học Vinh', '2026-11-15 08:00:00', '2026-11-15 17:00:00', 'https://picsum.photos/600/300?random=1', 300, 'Open', 1, 'user-org-1'),
(N'Music Festival Mùa Hè 2026', N'Đại nhạc hội bùng nổ với sự góp mặt của nhiều nghệ sĩ và ban nhạc sinh viên nổi tiếng.', N'Sân vận động Đại học Vinh', '2026-11-20 18:00:00', '2026-11-20 22:30:00', 'https://picsum.photos/600/300?random=2', 1000, 'Open', 2, 'user-org-1'),
(N'Giải Chạy Marathon Sinh Viên 2026', N'Lan tỏa lối sống xanh, rèn luyện sức khỏe và gây quỹ học bổng cho sinh viên khó khăn.', N'Khuôn viên Đại học Vinh', '2026-12-05 06:00:00', '2026-12-05 11:00:00', 'https://picsum.photos/600/300?random=3', 500, 'Draft', 3, 'user-org-1');

-- 6. Thêm TicketTypes (Loại vé)
INSERT INTO TicketTypes (EventId, TypeName, Price, Quantity, SoldQuantity, SaleStart, SaleEnd) VALUES 
(1, N'Vé Miễn Phí (Sinh viên)', 0.00, 200, 45, '2026-10-01 00:00:00', '2026-11-14 23:59:00'),
(1, N'Vé VIP (Kèm tài liệu & quà)', 250000.00, 100, 20, '2026-10-01 00:00:00', '2026-11-14 23:59:00'),
(2, N'Vé Thường Fanzone', 100000.00, 800, 150, '2026-10-05 00:00:00', '2026-11-19 23:59:00');

-- 7. Thêm Registrations (Đăng ký vé mẫu)
INSERT INTO Registrations (EventId, UserId, TicketTypeId, Quantity, TotalPrice, Status, CheckInStatus, CheckInTime, QrCode) VALUES 
(1, 'user-att-1', 2, 1, 250000.00, 'Confirmed', 1, '2026-11-15 08:30:00', 'QR-EV1-VIP-88231');

-- 8. Thêm Guests (Khách mời & diễn giả)
INSERT INTO Guests (EventId, FullName, Email, RoleTitle, InvitationCode, IsCheckedIn) VALUES 
(1, N'PGS. TS Nguyễn Văn A', 'vana@vinhuni.edu.vn', N'Diễn giả chính .NET 8', 'INV-NET-01', 1),
(1, N'ThS. Trần Thị B', 'thib@tech.com', N'Chuyên gia AI Engineer', 'INV-NET-02', 0);

-- 9. Thêm EventFeedbacks (Đánh giá)
INSERT INTO EventFeedbacks (EventId, UserId, Rating, Comment, IsHidden) VALUES 
(1, 'user-att-1', 5, N'Sự kiện rất chuyên nghiệp và cung cấp nhiều kiến thức thực tế cực kỳ hữu ích!', 0);

-- 10. Thêm Notifications (Thông báo)
INSERT INTO Notifications (UserId, Title, Message, IsRead) VALUES 
('user-att-1', N'Đăng ký vé thành công', N'Bạn đã đăng ký thành công vé VIP cho sự kiện Hội Thảo Công Nghệ .NET 8 & AI.', 1);

-- 11. Thêm SystemConfigs (Cấu hình hệ thống)
INSERT INTO SystemConfigs (ConfigKey, ConfigValue, Description) VALUES 
('SiteName', N'EventMana - Hệ Thống Quản Lý Sự Kiện', N'Tên hiển thị trên tiêu đề website'),
('AdminEmail', 'kkaitou37@gmail.com', N'Email hệ thống nhận thông báo'),
('DefaultPageSize', '10', N'Số lượng bản ghi hiển thị trên một trang phân trang');

-- 12. Thêm AuditLogs (Nhật ký hoạt động)
INSERT INTO AuditLogs (UserId, Action, Details, IpAddress) VALUES 
('user-admin-1', 'LOGIN', N'Quản trị viên đăng nhập hệ thống thành công.', '127.0.0.1'),
('user-org-1', 'CREATE_EVENT', N'Tạo mới sự kiện: Hội Thảo Công Nghệ .NET 8 & AI', '192.168.1.50');
GO
