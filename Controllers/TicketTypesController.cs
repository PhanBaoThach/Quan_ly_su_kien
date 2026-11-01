using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quan_ly_su_kien.Data;
using Quan_ly_su_kien.Models;

namespace Quan_ly_su_kien.Controllers
{
    /// <summary>
    /// PHÂN HỆ QUẢN LÝ VÉ SỰ KIỆN (US-06 - Bùi Thái Quý)
    /// Dành cho Ban Tổ Chức (Organizer) và Quản Trị Viên (Admin)
    /// </summary>
    [Authorize(Roles = "Organizer,Admin")]
    public class TicketTypesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TicketTypesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /TicketTypes/EventTickets/5 (Danh sách các hạng vé của 1 sự kiện)
        public async Task<IActionResult> EventTickets(int eventId)
        {
            var eventItem = await _context.Events.FindAsync(eventId);
            if (eventItem == null) return NotFound();

            var tickets = await _context.TicketTypes
                .Where(t => t.EventId == eventId)
                .ToListAsync();

            ViewBag.Event = eventItem;
            return View(tickets);
        }

        // POST: /TicketTypes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TicketType ticket)
        {
            if (ModelState.IsValid)
            {
                _context.TicketTypes.Add(ticket);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Thêm loại vé '{ticket.TypeName}' thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Dữ liệu vé không hợp lệ!";
            }

            return RedirectToAction(nameof(EventTickets), new { eventId = ticket.EventId });
        }
    }
}

