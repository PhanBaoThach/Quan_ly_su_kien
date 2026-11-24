using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quan_ly_su_kien.Data;
using Quan_ly_su_kien.Models;
using Quan_ly_su_kien.ViewModels;

namespace Quan_ly_su_kien.Controllers
{
    [Authorize]
    public class RegistrationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public RegistrationsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> MyTickets(string? status, string? search)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var baseQuery = _context.Registrations
                .Include(r => r.Event)
                    .ThenInclude(e => e!.Category)
                .Include(r => r.TicketType)
                .Where(r => r.UserId == currentUser.Id);

            var allUserTickets = await baseQuery.ToListAsync();

            ViewBag.TotalTicketsCount = allUserTickets.Where(r => r.Status != "Cancelled").Sum(r => r.Quantity);
            ViewBag.UpcomingCount = allUserTickets.Count(r => r.Status == "Confirmed" && r.Event != null && r.Event.StartDate > DateTime.Now);
            ViewBag.CheckedInCount = allUserTickets.Count(r => r.CheckInStatus);
            ViewBag.CancelledCount = allUserTickets.Count(r => r.Status == "Cancelled");
            ViewBag.TotalSpent = allUserTickets.Where(r => r.Status != "Cancelled").Sum(r => r.TotalPrice);

            var filteredQuery = baseQuery.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                filteredQuery = filteredQuery.Where(r => 
                    (r.Event != null && r.Event.Title.ToLower().Contains(term)) ||
                    r.QrCode.ToLower().Contains(term) ||
                    (r.TicketType != null && r.TicketType.TypeName.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                switch (status.ToLower())
                {
                    case "upcoming":
                        filteredQuery = filteredQuery.Where(r => r.Status == "Confirmed" && r.Event != null && r.Event.StartDate > DateTime.Now);
                        break;
                    case "past":
                        filteredQuery = filteredQuery.Where(r => (r.Event != null && r.Event.EndDate < DateTime.Now) || r.CheckInStatus);
                        break;
                    case "cancelled":
                        filteredQuery = filteredQuery.Where(r => r.Status == "Cancelled");
                        break;
                    case "confirmed":
                        filteredQuery = filteredQuery.Where(r => r.Status == "Confirmed");
                        break;
                }
            }

            var tickets = await filteredQuery
                .OrderByDescending(r => r.RegisteredAt)
                .ToListAsync();

            ViewBag.CurrentStatus = status ?? "all";
            ViewBag.CurrentSearch = search ?? string.Empty;

            return View(tickets);
        }

        public async Task<IActionResult> TicketDetails(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var registration = await _context.Registrations
                .Include(r => r.Event)
                    .ThenInclude(e => e!.Category)
                .Include(r => r.Event)
                    .ThenInclude(e => e!.Organizer)
                .Include(r => r.TicketType)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (registration == null)
            {
                return NotFound("Không tìm thấy thông tin vé đăng ký.");
            }

            bool isOwner = registration.UserId == currentUser.Id;
            bool isOrganizer = registration.Event != null && registration.Event.OrganizerId == currentUser.Id;
            bool isAdmin = User.IsInRole("Admin");

            if (!isOwner && !isOrganizer && !isAdmin)
            {
                return Forbid();
            }

            return View(registration);
        }

        [HttpGet]
        public async Task<IActionResult> Book(int eventId, int? ticketTypeId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var eventItem = await _context.Events
                .Include(e => e.Organizer)
                .Include(e => e.TicketTypes)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (eventItem == null)
            {
                return NotFound("Không tìm thấy thông tin sự kiện.");
            }

            if (eventItem.Status != "Open" || eventItem.EndDate <= DateTime.Now)
            {
                TempData["ErrorMessage"] = "Sự kiện hiện không mở đăng ký vé hoặc đã kết thúc.";
                return RedirectToAction("EventDetail", "Home", new { id = eventId });
            }

            var availableTickets = await _context.TicketTypes
                .Where(t => t.EventId == eventId && t.IsActive && t.QuantityAvailable > 0)
                .ToListAsync();

            if (!availableTickets.Any())
            {
                TempData["ErrorMessage"] = "Hiện tại sự kiện đã hết vé hoặc chưa có loại vé nào được mở bán.";
                return RedirectToAction("EventDetail", "Home", new { id = eventId });
            }

            int selectedTicketId = ticketTypeId.HasValue && availableTickets.Any(t => t.Id == ticketTypeId.Value)
                ? ticketTypeId.Value
                : availableTickets.First().Id;

            var model = new BookingViewModel
            {
                EventId = eventItem.Id,
                EventTitle = eventItem.Title,
                EventLocation = eventItem.Location,
                EventStartDate = eventItem.StartDate,
                EventEndDate = eventItem.EndDate,
                EventImageUrl = eventItem.ImageUrl,
                OrganizerName = eventItem.Organizer?.FullName ?? eventItem.Organizer?.UserName ?? "Ban tổ chức",
                TicketTypeId = selectedTicketId,
                Quantity = 1,
                FullName = currentUser.FullName ?? string.Empty,
                Email = currentUser.Email ?? string.Empty,
                PhoneNumber = currentUser.PhoneNumber,
                AvailableTickets = availableTickets
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(BookingViewModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var eventItem = await _context.Events
                .Include(e => e.Organizer)
                .Include(e => e.Registrations)
                .FirstOrDefaultAsync(e => e.Id == model.EventId);

            if (eventItem == null)
            {
                return NotFound("Không tìm thấy sự kiện tương ứng.");
            }

            if (eventItem.Status != "Open" || eventItem.EndDate <= DateTime.Now)
            {
                TempData["ErrorMessage"] = "Sự kiện đã kết thúc hoặc không mở đăng ký vé.";
                return RedirectToAction("EventDetail", "Home", new { id = model.EventId });
            }

            var ticketType = await _context.TicketTypes
                .FirstOrDefaultAsync(t => t.Id == model.TicketTypeId && t.EventId == model.EventId);

            if (ticketType == null || !ticketType.IsActive)
            {
                ModelState.AddModelError(string.Empty, "Loại vé được chọn không hợp lệ hoặc đã dừng phát hành.");
            }
            else if (ticketType.QuantityAvailable < model.Quantity)
            {
                ModelState.AddModelError("Quantity", $"Số lượng vé còn lại trong hệ thống chỉ còn {ticketType.QuantityAvailable} vé.");
            }

            int currentRegisteredTickets = eventItem.Registrations?
                .Where(r => r.Status != "Cancelled")
                .Sum(r => r.Quantity) ?? 0;

            if (eventItem.MaxTickets > 0 && (currentRegisteredTickets + model.Quantity) > eventItem.MaxTickets)
            {
                ModelState.AddModelError("Quantity", "Số lượng vé đăng ký vượt quá tổng công suất tiếp nhận của sự kiện.");
            }

            if (!ModelState.IsValid)
            {
                model.EventTitle = eventItem.Title;
                model.EventLocation = eventItem.Location;
                model.EventStartDate = eventItem.StartDate;
                model.EventEndDate = eventItem.EndDate;
                model.EventImageUrl = eventItem.ImageUrl;
                model.OrganizerName = eventItem.Organizer?.FullName ?? eventItem.Organizer?.UserName ?? "Ban tổ chức";
                model.AvailableTickets = await _context.TicketTypes
                    .Where(t => t.EventId == model.EventId && t.IsActive && t.QuantityAvailable > 0)
                    .ToListAsync();
                return View(model);
            }

            decimal totalPrice = ticketType!.Price * model.Quantity;
            string qrCode = $"EM-{eventItem.Id:D3}-{ticketType.Id:D2}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

            var registration = new Registration
            {
                EventId = model.EventId,
                UserId = currentUser.Id,
                TicketTypeId = model.TicketTypeId,
                Quantity = model.Quantity,
                TotalPrice = totalPrice,
                Status = "Confirmed",
                CheckInStatus = false,
                QrCode = qrCode,
                RegisteredAt = DateTime.Now
            };

            ticketType.QuantityAvailable -= model.Quantity;

            var notification = new Notification
            {
                UserId = currentUser.Id,
                Title = "Đăng ký vé thành công",
                Message = $"Bạn đã đăng ký thành công {model.Quantity} vé '{ticketType.TypeName}' cho sự kiện '{eventItem.Title}'. Mã vé: {qrCode}.",
                IsRead = false,
                CreatedAt = DateTime.Now
            };

            _context.Registrations.Add(registration);
            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Chúc mừng! Bạn đã đăng ký thành công {model.Quantity} vé cho sự kiện '{eventItem.Title}'.";

            return RedirectToAction(nameof(TicketDetails), new { id = registration.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var registration = await _context.Registrations
                .Include(r => r.Event)
                .Include(r => r.TicketType)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (registration == null)
            {
                return NotFound("Không tìm thấy thông tin vé đăng ký.");
            }

            if (registration.UserId != currentUser.Id && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            if (registration.Status == "Cancelled")
            {
                TempData["ErrorMessage"] = "Vé này đã được hủy trước đó.";
                return RedirectToAction(nameof(MyTickets));
            }

            if (registration.CheckInStatus)
            {
                TempData["ErrorMessage"] = "Vé đã được Check-in tại sự kiện, không thể hủy.";
                return RedirectToAction(nameof(MyTickets));
            }

            if (registration.Event != null && registration.Event.StartDate <= DateTime.Now)
            {
                TempData["ErrorMessage"] = "Sự kiện đã bắt đầu hoặc đã kết thúc, không thể thực hiện hủy vé.";
                return RedirectToAction(nameof(MyTickets));
            }

            registration.Status = "Cancelled";

            if (registration.TicketType != null)
            {
                registration.TicketType.QuantityAvailable += registration.Quantity;
            }

            var notification = new Notification
            {
                UserId = currentUser.Id,
                Title = "Hủy vé thành công",
                Message = $"Bạn đã hủy thành công đăng ký vé ({registration.QrCode}) cho sự kiện '{registration.Event?.Title}'.",
                IsRead = false,
                CreatedAt = DateTime.Now
            };

            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã hủy đăng ký vé thành công! Số lượng vé đã được hoàn trả lại hệ thống.";

            return RedirectToAction(nameof(MyTickets));
        }
    }
}
