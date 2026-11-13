using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Quan_ly_su_kien.Data;
using Quan_ly_su_kien.Models;

namespace Quan_ly_su_kien.Controllers
{
    /// <summary>
    /// PHÂN HỆ QUẢN LÝ KHÁCH MỜI & CHECK-IN (US-08 - Trương Minh Dương)
    /// Dành cho Ban Tổ Chức (Organizer) và Quản Trị Viên (Admin)
    /// </summary>
    [Authorize(Roles = "Organizer,Admin")]
    public class GuestsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public GuestsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Lấy danh sách sự kiện theo quyền (Admin thấy hết, Organizer thấy sự kiện của mình)
        private async Task<List<Event>> GetAccessibleEventsAsync()
        {
            var query = _context.Events.AsQueryable();
            if (!User.IsInRole("Admin"))
            {
                var userId = _userManager.GetUserId(User);
                query = query.Where(e => e.OrganizerId == userId);
            }
            return await query.OrderByDescending(e => e.StartDate).ToListAsync();
        }

        // GET: /Guests (Trang quản lý tổng hợp tất cả khách mời)
        public async Task<IActionResult> Index(int? eventId, string? search, string? status)
        {
            var events = await GetAccessibleEventsAsync();
            var eventIds = events.Select(e => e.Id).ToList();

            var query = _context.Guests
                .Include(g => g.Event)
                .Where(g => eventIds.Contains(g.EventId))
                .AsQueryable();

            if (eventId.HasValue && eventId.Value > 0)
            {
                query = query.Where(g => g.EventId == eventId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var kw = search.Trim().ToLower();
                query = query.Where(g => g.FullName.ToLower().Contains(kw)
                                      || g.Email.ToLower().Contains(kw)
                                      || g.InvitationCode.ToLower().Contains(kw)
                                      || (g.RoleTitle != null && g.RoleTitle.ToLower().Contains(kw)));
            }

            if (status == "checked")
            {
                query = query.Where(g => g.IsCheckedIn);
            }
            else if (status == "unchecked")
            {
                query = query.Where(g => !g.IsCheckedIn);
            }

            var guests = await query.OrderByDescending(g => g.Id).ToListAsync();

            ViewBag.Events = new SelectList(events, "Id", "Title", eventId);
            ViewBag.SelectedEventId = eventId;
            ViewBag.Search = search;
            ViewBag.Status = status ?? "all";
            ViewBag.TotalGuests = guests.Count;
            ViewBag.CheckedInCount = guests.Count(g => g.IsCheckedIn);
            ViewBag.UncheckedCount = guests.Count(g => !g.IsCheckedIn);

            return View(guests);
        }

        // GET: /Guests/EventGuests/5 (Giữ nguyên route cũ của team, chuyển hướng thẳng sang Index lọc theo sự kiện)
        public IActionResult EventGuests(int eventId)
        {
            return RedirectToAction(nameof(Index), new { eventId = eventId });
        }

        // GET: /Guests/Create
        public async Task<IActionResult> Create(int? eventId)
        {
            var events = await GetAccessibleEventsAsync();
            ViewBag.EventId = new SelectList(events, "Id", "Title", eventId);

            var model = new Guest
            {
                EventId = eventId ?? (events.FirstOrDefault()?.Id ?? 0),
                InvitationCode = GenerateInvitationCode()
            };
            return View(model);
        }

        // POST: /Guests/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Guest guest)
        {
            if (string.IsNullOrWhiteSpace(guest.InvitationCode))
            {
                guest.InvitationCode = GenerateInvitationCode();
            }

            ModelState.Remove("Event");

            if (ModelState.IsValid)
            {
                bool codeExists = await _context.Guests.AnyAsync(g => g.InvitationCode == guest.InvitationCode);
                if (codeExists)
                {
                    guest.InvitationCode = GenerateInvitationCode();
                }

                _context.Guests.Add(guest);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã thêm khách mời '{guest.FullName}' với mã mời {guest.InvitationCode}.";
                return RedirectToAction(nameof(Index), new { eventId = guest.EventId });
            }

            var events = await GetAccessibleEventsAsync();
            ViewBag.EventId = new SelectList(events, "Id", "Title", guest.EventId);
            return View(guest);
        }

        // GET: /Guests/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var guest = await _context.Guests.FindAsync(id);
            if (guest == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khách mời.";
                return RedirectToAction(nameof(Index));
            }

            var events = await GetAccessibleEventsAsync();
            ViewBag.EventId = new SelectList(events, "Id", "Title", guest.EventId);
            return View(guest);
        }

        // POST: /Guests/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Guest guest)
        {
            if (id != guest.Id) return NotFound();

            ModelState.Remove("Event");

            if (ModelState.IsValid)
            {
                _context.Update(guest);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật thông tin khách mời thành công.";
                return RedirectToAction(nameof(Index), new { eventId = guest.EventId });
            }

            var events = await GetAccessibleEventsAsync();
            ViewBag.EventId = new SelectList(events, "Id", "Title", guest.EventId);
            return View(guest);
        }

        // POST: /Guests/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var guest = await _context.Guests.FindAsync(id);
            if (guest != null)
            {
                int evId = guest.EventId;
                _context.Guests.Remove(guest);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã xóa khách mời '{guest.FullName}'.";
                return RedirectToAction(nameof(Index), new { eventId = evId });
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: /Guests/ToggleCheckIn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCheckIn(int id, int? currentEventId)
        {
            var guest = await _context.Guests.FindAsync(id);
            if (guest == null) return NotFound();

            guest.IsCheckedIn = !guest.IsCheckedIn;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = guest.IsCheckedIn
                ? $"Check-in thành công cho khách mời {guest.FullName}."
                : $"Đã hủy trạng thái check-in cho khách mời {guest.FullName}.";

            return RedirectToAction(nameof(Index), new { eventId = currentEventId ?? guest.EventId });
        }

        // GET: /Guests/CheckIn (Màn hình quét QR / nhập mã điểm danh tại cửa)
        public async Task<IActionResult> CheckIn(int? eventId)
        {
            var events = await GetAccessibleEventsAsync();
            var eventIds = events.Select(e => e.Id).ToList();

            var recentCheckedIn = await _context.Guests
                .Include(g => g.Event)
                .Where(g => eventIds.Contains(g.EventId) && g.IsCheckedIn)
                .OrderByDescending(g => g.Id)
                .Take(10)
                .ToListAsync();

            ViewBag.Events = new SelectList(events, "Id", "Title", eventId);
            ViewBag.SelectedEventId = eventId;
            return View(recentCheckedIn);
        }

        // POST: /Guests/VerifyCode (API AJAX xử lý quét mã QR / nhập mã)
        [HttpPost]
        public async Task<IActionResult> VerifyCode([FromBody] CheckInRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Code))
            {
                return Json(new { success = false, message = "Vui lòng nhập hoặc quét mã thư mời." });
            }

            var code = request.Code.Trim().ToUpper();
            var guest = await _context.Guests
                .Include(g => g.Event)
                .FirstOrDefaultAsync(g => g.InvitationCode.ToUpper() == code);

            if (guest == null)
            {
                return Json(new { success = false, message = $"Không tìm thấy khách mời nào có mã: {code}" });
            }

            if (request.EventId.HasValue && request.EventId.Value > 0 && guest.EventId != request.EventId.Value)
            {
                return Json(new
                {
                    success = false,
                    message = $"Mã này thuộc sự kiện '{guest.Event?.Title}', không khớp sự kiện đang chọn!"
                });
            }

            if (guest.IsCheckedIn)
            {
                return Json(new
                {
                    success = false,
                    alreadyChecked = true,
                    message = $"Khách mời {guest.FullName} ({guest.RoleTitle ?? "Khách mời"}) ĐÃ CHECK-IN trước đó rồi!",
                    guestName = guest.FullName,
                    roleTitle = guest.RoleTitle ?? "Khách mời",
                    eventTitle = guest.Event?.Title,
                    code = guest.InvitationCode
                });
            }

            guest.IsCheckedIn = true;
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = $"Check-in thành công! Chào mừng {guest.FullName}.",
                guestName = guest.FullName,
                roleTitle = guest.RoleTitle ?? "Khách mời",
                eventTitle = guest.Event?.Title,
                code = guest.InvitationCode
            });
        }

        private string GenerateInvitationCode()
        {
            return "INV-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
        }
    }

    public class CheckInRequest
    {
        public string Code { get; set; } = string.Empty;
        public int? EventId { get; set; }
    }
}