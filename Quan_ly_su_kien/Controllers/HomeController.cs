using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quan_ly_su_kien.Data;
using System.Diagnostics;
using Quan_ly_su_kien.Models;

namespace Quan_ly_su_kien.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .Where(c => c.IsActive)
                .Take(8)
                .ToListAsync();

            var featuredEvents = await _context.Events
                .Include(e => e.Category)
                .OrderByDescending(e => e.CreatedAt)
                .Take(6)
                .ToListAsync();

            ViewBag.Categories = categories;
            ViewBag.TotalEvents = await _context.Events.CountAsync();
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.TotalCategories = await _context.Categories.CountAsync();

            return View(featuredEvents);
        }

        public async Task<IActionResult> Events(
            string? search, int? categoryId, string? status, DateTime? fromDate, DateTime? toDate)
        {
            var categoryList = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Categories = categoryList;
            ViewBag.Search = search?.Trim();
            ViewBag.SelectedCategoryId = categoryId;
            ViewBag.SelectedStatus = string.IsNullOrWhiteSpace(status) ? "All" : status;
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;

            // Bỏ .Include(e => e.TicketTypes) để tuyệt đối không chạm vào bảng TicketTypes đang lệch cột
            var query = _context.Events
                .Include(e => e.Category)
                .Where(e => e.Status != "Draft")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var kw = search.Trim().ToLower();
                query = query.Where(e => e.Title.ToLower().Contains(kw)
                                      || e.Location.ToLower().Contains(kw)
                                      || e.Description.ToLower().Contains(kw)
                                      || (e.Category != null && e.Category.Name.ToLower().Contains(kw)));
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(e => e.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "All" && status != "Draft")
            {
                query = query.Where(e => e.Status == status);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(e => e.StartDate >= fromDate.Value.Date);
            }
            if (toDate.HasValue)
            {
                query = query.Where(e => e.StartDate < toDate.Value.Date.AddDays(1));
            }

            var rawEvents = await query
                .OrderBy(e => e.StartDate)
                .ToListAsync();

            var events = rawEvents
                .OrderBy(e => (e.Status == "Completed" || e.Status == "Cancelled") ? 1 : 0)
                .ThenBy(e => e.StartDate)
                .ToList();

            return View(events);
        }

        public async Task<IActionResult> EventDetail(int id)
        {
            var eventItem = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Organizer)
                .Include(e => e.Guests)
                .Include(e => e.Feedbacks)
                    .ThenInclude(f => f.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eventItem == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sự kiện yêu cầu.";
                return RedirectToAction("Events");
            }

            return View(eventItem);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}