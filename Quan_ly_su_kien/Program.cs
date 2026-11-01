using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Quan_ly_su_kien.Data;
using Quan_ly_su_kien.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình Database SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Chuỗi kết nối 'DefaultConnection' chưa được cấu hình.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Cấu hình ASP.NET Core Identity & Phân quyền Role (US-01, US-09)
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Cấu hình mật khẩu thuận tiện cho học tập & đồ án
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;

    // Cấu hình khóa tài khoản khi đăng nhập sai nhiều lần
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // Cấu hình xác nhận người dùng
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// 3. Cấu hình Cookie Authentication & Đường dẫn phân quyền (US-09)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.Name = ".EventMana.AuthCookie";
});

// 4. Đăng ký MVC Controllers with Views
builder.Services.AddControllersWithViews();

var app = builder.Build();

// 5. Tự động khởi tạo Database và Dữ liệu mẫu (Seed Data)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await DbInitializer.SeedAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Đã xảy ra lỗi trong quá trình khởi tạo dữ liệu mẫu.");
    }
}

// 6. Cấu hình HTTP Request Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Thứ tự Middleware Xác thực & Phân quyền (bắt buộc)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
