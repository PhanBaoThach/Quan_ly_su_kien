using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quan_ly_su_kien.Data;
using Quan_ly_su_kien.Models;

namespace Quan_ly_su_kien.Controllers
{
    /// <summary>
    /// PHÂN HỆ QUẢN LÝ SỰ KIỆN (US-04 - Phan Bảo Thạch)
    /// Dành cho Ban Tổ Chức (Organizer) và Quản Trị Viên (Admin)
    /// </summary>
    [Authorize(Roles = "Organizer,Admin")]
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EventsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Events/MyEvents (Danh sách sự kiện do tài khoản hiện tại tổ chức)
        public async Task<IActionResult> MyEvents()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var query = _context.Events
                .Include(e => e.Category)
                .Include(e => e.TicketTypes)
                .AsQueryable();

            if (!User.IsInRole("Admin"))
            {
                query = query.Where(e => e.OrganizerId == user.Id);
            }

            var events = await query.OrderByDescending(e => e.CreatedAt).ToListAsync();
            return View(events);
        }

        // GET: /Events/Create (Giao diện tạo sự kiện mới)
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
            return View();
        }

        // POST: /Events/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Event model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                model.OrganizerId = user.Id;
                model.CreatedAt = DateTime.Now;
                _context.Events.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Khởi tạo sự kiện '{model.Title}' thành công!";
                return RedirectToAction(nameof(MyEvents));
            }

            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
            return View(model);
        }
    }
}

