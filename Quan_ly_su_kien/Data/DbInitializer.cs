using Microsoft.AspNetCore.Identity;
using Quan_ly_su_kien.Models;

namespace Quan_ly_su_kien.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Đảm bảo Database đã được tạo
            try
            {
                await context.Database.EnsureCreatedAsync();
            }
            catch (Exception ex)
            {
                // Nếu chưa kết nối được DB thật (ví dụ cấu hình server khác), log lại và tiếp tục
                Console.WriteLine($"Database initialization error: {ex.Message}");
                return;
            }

            // 1. Khởi tạo các Vai trò (Roles)
            string[] roles = { "Admin", "Organizer", "Attendee" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 2. Khởi tạo tài khoản Quản trị viên (Admin - Phan Bảo Thạch)
            var adminEmail = "kkaitou37@gmail.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = "admin",
                    Email = adminEmail,
                    FullName = "Phan Bảo Thạch (Admin)",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                var result = await userManager.CreateAsync(adminUser, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // 3. Khởi tạo tài khoản Ban Tổ Chức (Organizer - Trương Minh Dương)
            var orgEmail = "duong@gmail.com";
            var orgUser = await userManager.FindByEmailAsync(orgEmail);
            if (orgUser == null)
            {
                orgUser = new ApplicationUser
                {
                    UserName = "truongminhduong",
                    Email = orgEmail,
                    FullName = "Trương Minh Dương (Ban Tổ Chức)",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                var result = await userManager.CreateAsync(orgUser, "Org@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(orgUser, "Organizer");
                }
            }

            // 4. Khởi tạo tài khoản Người tham gia (Attendee - Bùi Thái Quý)
            var attEmail = "quy@gmail.com";
            var attUser = await userManager.FindByEmailAsync(attEmail);
            if (attUser == null)
            {
                attUser = new ApplicationUser
                {
                    UserName = "buithaiquy",
                    Email = attEmail,
                    FullName = "Bùi Thái Quý (Người Tham Gia)",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                var result = await userManager.CreateAsync(attUser, "User@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(attUser, "Attendee");
                }
            }

            // 5. Khởi tạo Danh mục sự kiện mẫu (Categories - US-03)
            if (!context.Categories.Any())
            {
                var categories = new List<Category>
                {
                    new Category
                    {
                        Name = "Công nghệ thông tin",
                        Description = "Các hội thảo, cuộc thi lập trình, workshop về công nghệ mới",
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    },
                    new Category
                    {
                        Name = "Âm nhạc & Nghệ thuật",
                        Description = "Đại nhạc hội, triển lãm nghệ thuật, biểu diễn âm nhạc",
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    },
                    new Category
                    {
                        Name = "Thể thao & Sức khỏe",
                        Description = "Giải chạy marathon, hội thao sinh viên, yoga cộng đồng",
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    },
                    new Category
                    {
                        Name = "Giáo dục & Khởi nghiệp",
                        Description = "Tọa đàm hướng nghiệp, kết nối doanh nghiệp và sinh viên",
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    }
                };

                await context.Categories.AddRangeAsync(categories);
                await context.SaveChangesAsync();
            }

            // 6. Khởi tạo Sự kiện mẫu (Events & TicketTypes)
            if (!context.Events.Any() && orgUser != null)
            {
                var techCategory = context.Categories.FirstOrDefault(c => c.Name.Contains("Công nghệ"));
                var musicCategory = context.Categories.FirstOrDefault(c => c.Name.Contains("Âm nhạc"));
                var sportCategory = context.Categories.FirstOrDefault(c => c.Name.Contains("Thể thao"));

                if (techCategory != null)
                {
                    var event1 = new Event
                    {
                        Title = "Hội Thảo Công Nghệ .NET 10 & Trí Tuệ Nhân Tạo (AI)",
                        Description = "Cập nhật những xu hướng công nghệ mới nhất cùng các chuyên gia hàng đầu về ASP.NET Core, Cloud Computing và AI tích hợp trong phát triển phần mềm hiện đại.",
                        Location = "Hội trường A, Đại học Vinh - 182 Lê Duẩn, TP. Vinh",
                        StartDate = DateTime.Now.AddDays(14),
                        EndDate = DateTime.Now.AddDays(14).AddHours(8),
                        ImageUrl = "https://images.unsplash.com/photo-1540575467063-178a50c2df87?w=800&auto=format&fit=crop&q=80",
                        MaxTickets = 300,
                        Status = "Open",
                        CategoryId = techCategory.Id,
                        OrganizerId = orgUser.Id,
                        CreatedAt = DateTime.Now
                    };
                    context.Events.Add(event1);
                    await context.SaveChangesAsync();

                    context.TicketTypes.AddRange(
                        new TicketType
                        {
                            EventId = event1.Id,
                            TypeName = "Vé Miễn Phí (Sinh viên)",
                            Price = 0,
                            Quantity = 200,
                            SoldQuantity = 45,
                            SaleStart = DateTime.Now.AddDays(-5),
                            SaleEnd = DateTime.Now.AddDays(13)
                        },
                        new TicketType
                        {
                            EventId = event1.Id,
                            TypeName = "Vé VIP (Kèm tài liệu & quà)",
                            Price = 250000,
                            Quantity = 100,
                            SoldQuantity = 20,
                            SaleStart = DateTime.Now.AddDays(-5),
                            SaleEnd = DateTime.Now.AddDays(13)
                        }
                    );
                }

                if (musicCategory != null)
                {
                    var event2 = new Event
                    {
                        Title = "Music Festival Mùa Hè 2026 - Rực Rỡ Tuổi Trẻ",
                        Description = "Đại nhạc hội bùng nổ với sự góp mặt của nhiều nghệ sĩ hàng đầu và các ban nhạc sinh viên nổi tiếng, ánh sáng âm thanh chuẩn festival.",
                        Location = "Sân vận động Đại học Vinh, TP. Vinh",
                        StartDate = DateTime.Now.AddDays(20),
                        EndDate = DateTime.Now.AddDays(20).AddHours(5),
                        ImageUrl = "https://images.unsplash.com/photo-1470225620780-dba8ba36b745?w=800&auto=format&fit=crop&q=80",
                        MaxTickets = 1000,
                        Status = "Open",
                        CategoryId = musicCategory.Id,
                        OrganizerId = orgUser.Id,
                        CreatedAt = DateTime.Now
                    };
                    context.Events.Add(event2);
                    await context.SaveChangesAsync();

                    context.TicketTypes.Add(
                        new TicketType
                        {
                            EventId = event2.Id,
                            TypeName = "Vé Thường Fanzone",
                            Price = 100000,
                            Quantity = 800,
                            SoldQuantity = 150,
                            SaleStart = DateTime.Now.AddDays(-3),
                            SaleEnd = DateTime.Now.AddDays(19)
                        }
                    );
                }

                if (sportCategory != null)
                {
                    var event3 = new Event
                    {
                        Title = "Giải Chạy Marathon Sinh Viên Vì Môi Trường 2026",
                        Description = "Lan tỏa lối sống xanh, rèn luyện sức khỏe và gây quỹ học bổng cho sinh viên khó khăn với các cự ly 5km, 10km và 21km.",
                        Location = "Khuôn viên và đường bao quanh Đại học Vinh",
                        StartDate = DateTime.Now.AddDays(35),
                        EndDate = DateTime.Now.AddDays(35).AddHours(6),
                        ImageUrl = "https://images.unsplash.com/photo-1530549387789-4c1017266635?w=800&auto=format&fit=crop&q=80",
                        MaxTickets = 500,
                        Status = "Open",
                        CategoryId = sportCategory.Id,
                        OrganizerId = orgUser.Id,
                        CreatedAt = DateTime.Now
                    };
                    context.Events.Add(event3);
                }

                await context.SaveChangesAsync();
            }

            // 7. Cấu hình hệ thống (SystemConfigs)
            if (!context.SystemConfigs.Any())
            {
                context.SystemConfigs.AddRange(
                    new SystemConfig { ConfigKey = "SiteName", ConfigValue = "EventMana - Nền Tảng Quản Lý Sự Kiện", Description = "Tên website" },
                    new SystemConfig { ConfigKey = "AdminEmail", ConfigValue = "kkaitou37@gmail.com", Description = "Email quản trị" },
                    new SystemConfig { ConfigKey = "Version", ConfigValue = "1.0.0", Description = "Phiên bản hệ thống" }
                );
                await context.SaveChangesAsync();
            }

            // 8. Nhật ký hoạt động mẫu (AuditLogs)
            if (!context.AuditLogs.Any() && adminUser != null)
            {
                context.AuditLogs.Add(new AuditLog
                {
                    UserId = adminUser.Id,
                    Action = "SYSTEM_INITIALIZE",
                    Details = "Khởi tạo hệ thống thành công. Cấu hình cơ sở dữ liệu và bảo mật tài khoản sẵn sàng hoạt động.",
                    IpAddress = "127.0.0.1",
                    Timestamp = DateTime.Now
                });
                await context.SaveChangesAsync();
            }
        }
    }
}

