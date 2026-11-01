using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public GuestsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Guests/EventGuests/5
        public async Task<IActionResult> EventGuests(int eventId)
        {
            var eventItem = await _context.Events.FindAsync(eventId);
            if (eventItem == null) return NotFound();

            var guests = await _context.Guests
                .Where(g => g.EventId == eventId)
                .ToListAsync();

            ViewBag.Event = eventItem;
            return View(guests);
        }

        // POST: /Guests/ToggleCheckIn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCheckIn(int id)
        {
            var guest = await _context.Guests.FindAsync(id);
            if (guest == null) return NotFound();

            guest.IsCheckedIn = !guest.IsCheckedIn;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã cập nhật trạng thái check-in cho khách mời {guest.FullName}.";
            return RedirectToAction(nameof(EventGuests), new { eventId = guest.EventId });
        }
    }
}

