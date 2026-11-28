using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quan_ly_su_kien.Data;
using Quan_ly_su_kien.Models;

namespace Quan_ly_su_kien.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? filter)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);
            await EnsureRemindersGeneratedAsync(user, roles);

            var query = _context.Notifications
                .Where(n => n.UserId == user.Id)
                .AsQueryable();

            if (filter == "unread")
            {
                query = query.Where(n => !n.IsRead);
            }
            else if (filter == "read")
            {
                query = query.Where(n => n.IsRead);
            }
            else if (filter == "reminder")
            {
                query = query.Where(n => n.Title.Contains("Nhắc Lịch") || n.Title.Contains("Nhắc Tổ Chức") || n.Title.Contains("Nhắc Nhở"));
            }

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            ViewBag.CurrentFilter = filter ?? "all";
            ViewBag.TotalCount = await _context.Notifications.CountAsync(n => n.UserId == user.Id);
            ViewBag.UnreadCount = await _context.Notifications.CountAsync(n => n.UserId == user.Id && !n.IsRead);
            ViewBag.ReminderCount = await _context.Notifications.CountAsync(n => n.UserId == user.Id && (n.Title.Contains("Nhắc Lịch") || n.Title.Contains("Nhắc Tổ Chức") || n.Title.Contains("Nhắc Nhở")));

            return View(notifications);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false, message = "Chưa đăng nhập" });

            var notif = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == user.Id);
            if (notif != null)
            {
                notif.IsRead = true;
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Không tìm thấy thông báo" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var unreadList = await _context.Notifications
                .Where(n => n.UserId == user.Id && !n.IsRead)
                .ToListAsync();

            foreach (var item in unreadList)
            {
                item.IsRead = true;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã đánh dấu tất cả thông báo là đã đọc.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var notif = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == user.Id);
            if (notif != null)
            {
                _context.Notifications.Remove(notif);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa thông báo.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAllRead()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var readList = await _context.Notifications
                .Where(n => n.UserId == user.Id && n.IsRead)
                .ToListAsync();

            _context.Notifications.RemoveRange(readList);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã xóa {readList.Count} thông báo đã đọc.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetUnreadSummary()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { unreadCount = 0, recent = new List<object>() });

            var roles = await _userManager.GetRolesAsync(user);
            await EnsureRemindersGeneratedAsync(user, roles);

            var unreadCount = await _context.Notifications.CountAsync(n => n.UserId == user.Id && !n.IsRead);
            var recent = await _context.Notifications
                .Where(n => n.UserId == user.Id)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .Select(n => new
                {
                    n.Id,
                    n.Title,
                    Message = n.Message.Length > 80 ? n.Message.Substring(0, 80) + "..." : n.Message,
                    n.IsRead,
                    CreatedAt = n.CreatedAt.ToString("HH:mm dd/MM/yyyy")
                })
                .ToListAsync();

            return Json(new { unreadCount, recent });
        }

        private async Task EnsureRemindersGeneratedAsync(ApplicationUser user, IList<string> roles)
        {
            var now = DateTime.Now;
            var reminderThreshold = now.AddDays(7);
            var isOrganizer = roles.Contains("Organizer");
            var isAdmin = roles.Contains("Admin");

            if (isOrganizer)
            {
                var myUpcomingEvents = await _context.Events
                    .Include(e => e.Registrations)
                    .Include(e => e.TicketTypes)
                    .Where(e => e.OrganizerId == user.Id && e.StartDate >= now && e.StartDate <= reminderThreshold && e.Status != "Cancelled")
                    .ToListAsync();

                foreach (var ev in myUpcomingEvents)
                {
                    var exists = await _context.Notifications.AnyAsync(n =>
                        n.UserId == user.Id &&
                        n.Title.Contains("[Nhắc Tổ Chức]") &&
                        n.Title.Contains(ev.Title) &&
                        n.CreatedAt >= now.AddDays(-2));

                    if (!exists)
                    {
                        var daysLeft = (ev.StartDate.Date - now.Date).Days;
                        var daysText = daysLeft == 0 ? "hôm nay" : (daysLeft == 1 ? "ngày mai" : $"trong {daysLeft} ngày tới");
                        var regCount = ev.Registrations.Count(r => r.Status != "Cancelled");

                        _context.Notifications.Add(new Notification
                        {
                            UserId = user.Id,
                            Title = $"[Nhắc Tổ Chức] Sự kiện '{ev.Title}' sắp diễn ra {daysText}",
                            Message = $"Sự kiện do bạn tổ chức sẽ bắt đầu lúc {ev.StartDate:HH:mm dd/MM/yyyy} tại {ev.Location}. Hiện có {regCount} lượt đăng ký. Hãy kiểm tra địa điểm và cổng check-in QR!",
                            IsRead = false,
                            CreatedAt = now
                        });
                    }
                }

                var draftEvents = await _context.Events
                    .Where(e => e.OrganizerId == user.Id && e.Status == "Draft" && e.CreatedAt <= now.AddDays(-1))
                    .ToListAsync();

                foreach (var draft in draftEvents)
                {
                    var exists = await _context.Notifications.AnyAsync(n =>
                        n.UserId == user.Id &&
                        n.Title.Contains("[Bản Nháp]") &&
                        n.Title.Contains(draft.Title) &&
                        n.CreatedAt >= now.AddDays(-3));

                    if (!exists)
                    {
                        _context.Notifications.Add(new Notification
                        {
                            UserId = user.Id,
                            Title = $"[Nhắc Nhở] Hoàn thiện sự kiện bản nháp: {draft.Title}",
                            Message = $"Sự kiện '{draft.Title}' đang ở trạng thái bản nháp. Hãy thêm hạng vé và chuyển trạng thái sang [Mở vé] để khán giả có thể đăng ký tham gia.",
                            IsRead = false,
                            CreatedAt = now
                        });
                    }
                }
            }
            else if (isAdmin)
            {
                var newEventsInLast24h = await _context.Events
                    .Include(e => e.Organizer)
                    .Where(e => e.CreatedAt >= now.AddDays(-1))
                    .ToListAsync();

                foreach (var ev in newEventsInLast24h)
                {
                    var exists = await _context.Notifications.AnyAsync(n =>
                        n.UserId == user.Id &&
                        n.Title.Contains("[Sự Kiện Mới]") &&
                        n.Title.Contains(ev.Title));

                    if (!exists)
                    {
                        var orgName = ev.Organizer != null ? (ev.Organizer.FullName ?? ev.Organizer.UserName) : "Ban Tổ Chức";
                        _context.Notifications.Add(new Notification
                        {
                            UserId = user.Id,
                            Title = $"[Sự Kiện Mới] {ev.Title} vừa được tạo",
                            Message = $"Ban tổ chức '{orgName}' vừa tạo sự kiện mới '{ev.Title}' dự kiến diễn ra từ {ev.StartDate:dd/MM/yyyy}. Hãy kiểm tra nội dung và phân loại sự kiện.",
                            IsRead = false,
                            CreatedAt = now
                        });
                    }
                }

                var hasPeriodicSummary = await _context.Notifications.AnyAsync(n =>
                    n.UserId == user.Id &&
                    n.Title.Contains("[Hệ Thống] Điểm tin vận hành") &&
                    n.CreatedAt >= now.AddDays(-1));

                if (!hasPeriodicSummary)
                {
                    var totalUsers = await _context.Users.CountAsync();
                    var totalEvents = await _context.Events.CountAsync();
                    var totalRegistrations = await _context.Registrations.CountAsync(r => r.Status != "Cancelled");

                    _context.Notifications.Add(new Notification
                    {
                        UserId = user.Id,
                        Title = $"[Hệ Thống] Điểm tin vận hành hệ thống",
                        Message = $"Hệ thống EventMana hiện có tổng cộng {totalUsers} người dùng, {totalEvents} sự kiện và {totalRegistrations} vé đã đăng ký hợp lệ. Tất cả dịch vụ đang hoạt động ổn định.",
                        IsRead = false,
                        CreatedAt = now
                    });
                }
            }
            else
            {
                var myUpcomingTickets = await _context.Registrations
                    .Include(r => r.Event)
                    .Include(r => r.TicketType)
                    .Where(r => r.UserId == user.Id && r.Status != "Cancelled" && r.Event != null && r.Event.StartDate >= now && r.Event.StartDate <= reminderThreshold && r.Event.Status != "Cancelled")
                    .ToListAsync();

                foreach (var reg in myUpcomingTickets)
                {
                    var ev = reg.Event!;
                    var exists = await _context.Notifications.AnyAsync(n =>
                        n.UserId == user.Id &&
                        n.Title.Contains("[Nhắc Lịch]") &&
                        n.Title.Contains(ev.Title) &&
                        n.CreatedAt >= now.AddDays(-2));

                    if (!exists)
                    {
                        var daysLeft = (ev.StartDate.Date - now.Date).Days;
                        var daysText = daysLeft == 0 ? "hôm nay" : (daysLeft == 1 ? "ngày mai" : $"trong {daysLeft} ngày nữa");
                        var ticketInfo = reg.TicketType != null ? $"hạng vé [{reg.TicketType.TypeName}]" : "vé";

                        _context.Notifications.Add(new Notification
                        {
                            UserId = user.Id,
                            Title = $"[Nhắc Lịch] Sự kiện '{ev.Title}' sẽ diễn ra {daysText}",
                            Message = $"Bạn có {ticketInfo} cho sự kiện '{ev.Title}' bắt đầu lúc {ev.StartDate:HH:mm dd/MM/yyyy} tại {ev.Location}. Mã vé: #{reg.Id:D6}. Vui lòng mang mã vé khi đến check-in!",
                            IsRead = false,
                            CreatedAt = now
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
