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

        // GET: /
        public async Task<IActionResult> Index()
        {
            try
            {
                var categories = await _context.Categories
                    .Where(c => c.IsActive)
                    .Take(8)
                    .ToListAsync();

                var featuredEvents = await _context.Events
                    .Include(e => e.Category)
                    .Include(e => e.TicketTypes)
                    .OrderByDescending(e => e.CreatedAt)
                    .Take(6)
                    .ToListAsync();

                ViewBag.Categories = categories;
                ViewBag.TotalEvents = await _context.Events.CountAsync();
                ViewBag.TotalUsers = await _context.Users.CountAsync();
                ViewBag.TotalCategories = await _context.Categories.CountAsync();

                return View(featuredEvents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading homepage data");
                ViewBag.Categories = new List<Category>();
                return View(new List<Event>());
            }
        }

        // GET: /Home/Events
        public async Task<IActionResult> Events(string? search, int? categoryId, string? status)
        {
            try
            {
                var query = _context.Events
                    .Include(e => e.Category)
                    .Include(e => e.TicketTypes)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.Trim().ToLower();
                    query = query.Where(e => e.Title.ToLower().Contains(search)
                                          || e.Location.ToLower().Contains(search)
                                          || e.Description.ToLower().Contains(search));
                }

                if (categoryId.HasValue && categoryId.Value > 0)
                {
                    query = query.Where(e => e.CategoryId == categoryId.Value);
                }

                if (!string.IsNullOrWhiteSpace(status) && status != "All")
                {
                    query = query.Where(e => e.Status == status);
                }

                var events = await query.OrderByDescending(e => e.StartDate).ToListAsync();

                ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
                ViewBag.Search = search;
                ViewBag.SelectedCategoryId = categoryId;
                ViewBag.SelectedStatus = status;

                return View(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading events list");
                ViewBag.Categories = new List<Category>();
                return View(new List<Event>());
            }
        }

        // GET: /Home/EventDetail/5
        public async Task<IActionResult> EventDetail(int id)
        {
            var eventItem = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Organizer)
                .Include(e => e.TicketTypes)
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
