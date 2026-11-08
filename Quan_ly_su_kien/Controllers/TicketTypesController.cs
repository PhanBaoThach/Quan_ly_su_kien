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

        // GET: /TicketTypes/EventTickets?eventId=5
        public async Task<IActionResult> EventTickets(int eventId)
        {
            var eventItem = await _context.Events.FindAsync(eventId);
            if (eventItem == null)
            {
                return NotFound("Không tìm thấy sự kiện tương ứng.");
            }

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
            ModelState.Remove("Event");
            ModelState.Remove("Registrations");

            if (ModelState.IsValid)
            {
                // Mặc định số lượng còn lại bằng tổng số lượng khi khởi tạo
                ticket.QuantityAvailable = ticket.QuantityTotal;

                _context.TicketTypes.Add(ticket);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Thêm loại vé '{ticket.TypeName}' thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Dữ liệu vé không hợp lệ! Vui lòng kiểm tra lại.";
            }

            return RedirectToAction(nameof(EventTickets), new { eventId = ticket.EventId });
        }

        // POST: /TicketTypes/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TicketType ticket)
        {
            ModelState.Remove("Event");
            ModelState.Remove("Registrations");

            if (ModelState.IsValid)
            {
                var existingTicket = await _context.TicketTypes.FindAsync(ticket.Id);
                if (existingTicket == null) return NotFound();

                int soldCount = existingTicket.QuantityTotal - existingTicket.QuantityAvailable;

                if (ticket.QuantityTotal < soldCount)
                {
                    TempData["ErrorMessage"] = $"Không thể giảm tổng số lượng ({ticket.QuantityTotal}) thấp hơn số lượng vé đã bán ({soldCount})!";
                    return RedirectToAction(nameof(EventTickets), new { eventId = ticket.EventId });
                }

                existingTicket.TypeName = ticket.TypeName;
                existingTicket.Price = ticket.Price;
                existingTicket.Description = ticket.Description;
                existingTicket.IsActive = ticket.IsActive;

                // Cập nhật lại số lượng còn lại theo chênh lệch tổng số lượng mới
                int diff = ticket.QuantityTotal - existingTicket.QuantityTotal;
                existingTicket.QuantityTotal = ticket.QuantityTotal;
                existingTicket.QuantityAvailable += diff;

                _context.Update(existingTicket);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Cập nhật loại vé '{ticket.TypeName}' thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Dữ liệu cập nhật không hợp lệ!";
            }

            return RedirectToAction(nameof(EventTickets), new { eventId = ticket.EventId });
        }

        // POST: /TicketTypes/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var ticket = await _context.TicketTypes.FindAsync(id);
            if (ticket == null) return NotFound();

            int eventId = ticket.EventId;

            // Ràng buộc bảo mật dữ liệu: kiểm tra xem loại vé này đã được đăng ký bán chưa
            bool hasRegistrations = await _context.Registrations.AnyAsync(r => r.TicketTypeId == id);
            if (hasRegistrations)
            {
                TempData["ErrorMessage"] = "Không thể xóa loại vé này vì đã có lượt đặt mua thành công!";
                return RedirectToAction(nameof(EventTickets), new { eventId });
            }

            _context.TicketTypes.Remove(ticket);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã xóa loại vé thành công!";

            return RedirectToAction(nameof(EventTickets), new { eventId });
        }
    }
}