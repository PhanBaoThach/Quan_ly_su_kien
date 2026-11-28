using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Quan_ly_su_kien.Data;
using Quan_ly_su_kien.Models;
using Quan_ly_su_kien.ViewModels;

namespace Quan_ly_su_kien.Controllers
{
    [Authorize(Roles = "Organizer")]
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<EventsController> _logger;

        public EventsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment,
            ILogger<EventsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> MyEvents(string? search, int? categoryId, string? status)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var query = _context.Events
                .Include(e => e.Category)
                .Include(e => e.Organizer)
                .Include(e => e.TicketTypes)
                .Include(e => e.Registrations)
                .AsQueryable();

            query = query.Where(e => e.OrganizerId == user.Id);

            var allMyEvents = await query.ToListAsync();
            ViewBag.TotalEvents = allMyEvents.Count;
            ViewBag.OpenCount = allMyEvents.Count(e => e.Status == "Open");
            ViewBag.DraftCount = allMyEvents.Count(e => e.Status == "Draft");
            ViewBag.CompletedCount = allMyEvents.Count(e => e.Status == "Completed");
            ViewBag.CancelledCount = allMyEvents.Count(e => e.Status == "Cancelled");

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(e => e.Title.ToLower().Contains(search) 
                                      || e.Location.ToLower().Contains(search));
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(e => e.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                query = query.Where(e => e.Status == status);
            }

            var events = await query.OrderByDescending(e => e.CreatedAt).ToListAsync();

            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
            ViewBag.Search = search;
            ViewBag.SelectedCategoryId = categoryId;
            ViewBag.SelectedStatus = status;

            return View(events);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PopulateCategoriesDropdown();
            var model = new EventFormViewModel
            {
                StartDate = DateTime.Now.AddDays(7).Date.AddHours(8),
                EndDate = DateTime.Now.AddDays(7).Date.AddHours(17),
                Status = "Draft",
                MaxTickets = 200
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EventFormViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                await PopulateCategoriesDropdown();
                return View(model);
            }

            try
            {
                string? finalImageUrl = model.ImageUrl;
                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    finalImageUrl = await ProcessUploadedFile(model.ImageFile);
                }

                if (string.IsNullOrWhiteSpace(finalImageUrl))
                {
                    finalImageUrl = "https://images.unsplash.com/photo-1540575467063-178a50c2df87?w=1000&auto=format&fit=crop&q=80";
                }

                var newEvent = new Event
                {
                    Title = model.Title.Trim(),
                    Description = model.Description.Trim(),
                    Location = model.Location.Trim(),
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    ImageUrl = finalImageUrl,
                    MaxTickets = model.MaxTickets,
                    Status = model.Status,
                    CategoryId = model.CategoryId,
                    OrganizerId = user.Id,
                    CreatedAt = DateTime.Now
                };

                _context.Events.Add(newEvent);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Khởi tạo sự kiện '{newEvent.Title}' thành công!";
                return RedirectToAction(nameof(MyEvents));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo mới sự kiện");
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi trong quá trình lưu dữ liệu sự kiện: " + ex.Message);
                await PopulateCategoriesDropdown();
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var eventItem = await _context.Events.FindAsync(id);
            if (eventItem == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sự kiện yêu cầu.";
                return RedirectToAction(nameof(MyEvents));
            }

            if (eventItem.OrganizerId != user.Id)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var model = new EventFormViewModel
            {
                Id = eventItem.Id,
                Title = eventItem.Title,
                Description = eventItem.Description,
                Location = eventItem.Location,
                StartDate = eventItem.StartDate,
                EndDate = eventItem.EndDate,
                ImageUrl = eventItem.ImageUrl,
                MaxTickets = eventItem.MaxTickets,
                Status = eventItem.Status,
                CategoryId = eventItem.CategoryId
            };

            await PopulateCategoriesDropdown();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EventFormViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                await PopulateCategoriesDropdown();
                return View(model);
            }

            var eventItem = await _context.Events.FindAsync(model.Id);
            if (eventItem == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sự kiện.";
                return RedirectToAction(nameof(MyEvents));
            }

            if (eventItem.OrganizerId != user.Id)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            try
            {
                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    eventItem.ImageUrl = await ProcessUploadedFile(model.ImageFile);
                }
                else if (!string.IsNullOrWhiteSpace(model.ImageUrl))
                {
                    eventItem.ImageUrl = model.ImageUrl;
                }

                eventItem.Title = model.Title.Trim();
                eventItem.Description = model.Description.Trim();
                eventItem.Location = model.Location.Trim();
                eventItem.StartDate = model.StartDate;
                eventItem.EndDate = model.EndDate;
                eventItem.MaxTickets = model.MaxTickets;
                eventItem.Status = model.Status;
                eventItem.CategoryId = model.CategoryId;

                _context.Update(eventItem);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Cập nhật sự kiện '{eventItem.Title}' thành công!";
                return RedirectToAction(nameof(MyEvents));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật sự kiện id={Id}", model.Id);
                ModelState.AddModelError(string.Empty, "Lỗi khi lưu cập nhật: " + ex.Message);
                await PopulateCategoriesDropdown();
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var eventItem = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Organizer)
                .Include(e => e.TicketTypes)
                .Include(e => e.Registrations)
                    .ThenInclude(r => r.User)
                .Include(e => e.Guests)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eventItem == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sự kiện.";
                return RedirectToAction(nameof(MyEvents));
            }

            if (eventItem.OrganizerId != user.Id)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return View(eventItem);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string newStatus)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var eventItem = await _context.Events.FindAsync(id);
            if (eventItem == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sự kiện.";
                return RedirectToAction(nameof(MyEvents));
            }

            if (eventItem.OrganizerId != user.Id)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            string[] validStatuses = { "Draft", "Open", "Ongoing", "Completed", "Cancelled" };
            if (!validStatuses.Contains(newStatus))
            {
                TempData["ErrorMessage"] = "Trạng thái không hợp lệ.";
                return RedirectToAction(nameof(MyEvents));
            }

            eventItem.Status = newStatus;
            await _context.SaveChangesAsync();

            string statusText = newStatus switch
            {
                "Open" => "Mở đăng ký vé",
                "Ongoing" => "Đang diễn ra",
                "Completed" => "Đã kết thúc",
                "Cancelled" => "Hủy sự kiện",
                _ => "Dự thảo (Draft)"
            };

            TempData["SuccessMessage"] = $"Đã cập nhật trạng thái sự kiện '{eventItem.Title}' thành [{statusText}]!";
            return RedirectToAction(nameof(MyEvents));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var eventItem = await _context.Events
                .Include(e => e.Registrations)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eventItem == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sự kiện.";
                return RedirectToAction(nameof(MyEvents));
            }

            if (eventItem.OrganizerId != user.Id)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (eventItem.Registrations.Any(r => r.Status != "Cancelled"))
            {
                TempData["ErrorMessage"] = $"Không thể xóa sự kiện '{eventItem.Title}' vì đã có {eventItem.Registrations.Count} người đăng ký vé! Bạn hãy chuyển trạng thái sang [Đã Hủy].";
                return RedirectToAction(nameof(MyEvents));
            }

            _context.Events.Remove(eventItem);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã xóa sự kiện '{eventItem.Title}' thành công!";
            return RedirectToAction(nameof(MyEvents));
        }

        private async Task PopulateCategoriesDropdown()
        {
            var categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
        }

        private async Task<string> ProcessUploadedFile(IFormFile file)
        {
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "events");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return $"/uploads/events/{uniqueFileName}";
        }
    }
}
