using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quan_ly_su_kien.Data;
using Quan_ly_su_kien.Models;
using Quan_ly_su_kien.ViewModels;

namespace Quan_ly_su_kien.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            ILogger<AdminController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _logger = logger;
        }

        // GET: /Admin (Dashboard tổng quan Sprint 1)
        public async Task<IActionResult> Index()
        {
            var totalUsers = await _userManager.Users.CountAsync();
            var activeUsers = await _userManager.Users.CountAsync(u => u.IsActive);
            var totalCategories = await _context.Categories.CountAsync();
            var totalEvents = await _context.Events.CountAsync();

            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            var orgUsers = await _userManager.GetUsersInRoleAsync("Organizer");
            var attUsers = await _userManager.GetUsersInRoleAsync("Attendee");

            ViewBag.TotalUsers = totalUsers;
            ViewBag.ActiveUsers = activeUsers;
            ViewBag.AdminCount = adminUsers.Count;
            ViewBag.OrgCount = orgUsers.Count;
            ViewBag.AttCount = attUsers.Count;
            ViewBag.TotalCategories = totalCategories;
            ViewBag.TotalEvents = totalEvents;

            // Lấy 5 người dùng mới nhất
            var recentUsers = await _userManager.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(5)
                .ToListAsync();

            var recentUserVms = new List<UserManagementViewModel>();
            foreach (var u in recentUsers)
            {
                var roles = await _userManager.GetRolesAsync(u);
                recentUserVms.Add(new UserManagementViewModel
                {
                    Id = u.Id,
                    UserName = u.UserName ?? string.Empty,
                    Email = u.Email ?? string.Empty,
                    FullName = u.FullName ?? string.Empty,
                    PhoneNumber = u.PhoneNumber,
                    Role = roles.FirstOrDefault() ?? "Chưa phân quyền",
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt
                });
            }

            return View(recentUserVms);
        }

        // ==========================================
        // US-02: QUẢN LÝ TÀI KHOẢN VÀ PHÂN QUYỀN
        // ==========================================

        // GET: /Admin/Users
        public async Task<IActionResult> Users(string? search, string? roleFilter)
        {
            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(u => (u.UserName != null && u.UserName.ToLower().Contains(search))
                                      || (u.Email != null && u.Email.ToLower().Contains(search))
                                      || (u.FullName != null && u.FullName.ToLower().Contains(search)));
            }

            var users = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();
            var userVms = new List<UserManagementViewModel>();

            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                var currentRole = roles.FirstOrDefault() ?? "Chưa phân quyền";

                if (!string.IsNullOrEmpty(roleFilter) && roleFilter != "All" && currentRole != roleFilter)
                {
                    continue;
                }

                userVms.Add(new UserManagementViewModel
                {
                    Id = u.Id,
                    UserName = u.UserName ?? string.Empty,
                    Email = u.Email ?? string.Empty,
                    FullName = u.FullName ?? string.Empty,
                    PhoneNumber = u.PhoneNumber,
                    Role = currentRole,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt
                });
            }

            ViewBag.Search = search;
            ViewBag.RoleFilter = roleFilter;
            return View(userVms);
        }

        // POST: /Admin/CreateUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu nhập không hợp lệ. Vui lòng kiểm tra lại.";
                return RedirectToAction("Users");
            }

            var existingUser = await _userManager.FindByNameAsync(model.UserName)
                               ?? await _userManager.FindByEmailAsync(model.Email);

            if (existingUser != null)
            {
                TempData["ErrorMessage"] = "Tên đăng nhập hoặc Email đã tồn tại trong hệ thống.";
                return RedirectToAction("Users");
            }

            var user = new ApplicationUser
            {
                UserName = model.UserName,
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                if (!await _roleManager.RoleExistsAsync(model.Role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(model.Role));
                }

                await _userManager.AddToRoleAsync(user, model.Role);
                TempData["SuccessMessage"] = $"Tạo tài khoản {user.UserName} thành công với quyền {model.Role}!";
            }
            else
            {
                string errors = string.Join("; ", result.Errors.Select(e => e.Description));
                TempData["ErrorMessage"] = $"Lỗi tạo tài khoản: {errors}";
            }

            return RedirectToAction("Users");
        }

        // POST: /Admin/EditUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu chỉnh sửa không hợp lệ.";
                return RedirectToAction("Users");
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
                return RedirectToAction("Users");
            }

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;
            user.IsActive = model.IsActive;

            var updateResult = await _userManager.UpdateAsync(user);
            if (updateResult.Succeeded)
            {
                // Cập nhật vai trò
                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);

                if (!await _roleManager.RoleExistsAsync(model.Role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(model.Role));
                }
                await _userManager.AddToRoleAsync(user, model.Role);

                TempData["SuccessMessage"] = $"Cập nhật tài khoản {user.UserName} thành công!";
            }
            else
            {
                string errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
                TempData["ErrorMessage"] = $"Lỗi cập nhật: {errors}";
            }

            return RedirectToAction("Users");
        }

        // POST: /Admin/ToggleUserStatus (Khóa / Mở khóa)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
                return RedirectToAction("Users");
            }

            // Không cho phép tự khóa tài khoản của chính mình
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null && currentUser.Id == user.Id)
            {
                TempData["ErrorMessage"] = "Bạn không thể khóa tài khoản đang đăng nhập của chính mình!";
                return RedirectToAction("Users");
            }

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

            string statusText = user.IsActive ? "Mở khóa" : "Khóa";
            TempData["SuccessMessage"] = $"{statusText} tài khoản {user.UserName} thành công!";
            return RedirectToAction("Users");
        }

        // POST: /Admin/DeleteUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
                return RedirectToAction("Users");
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null && currentUser.Id == user.Id)
            {
                TempData["ErrorMessage"] = "Bạn không thể xóa tài khoản của chính mình!";
                return RedirectToAction("Users");
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Đã xóa tài khoản {user.UserName}!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể xóa tài khoản này.";
            }

            return RedirectToAction("Users");
        }

        // ==========================================
        // US-03: QUẢN LÝ DANH MỤC SỰ KIỆN (CATEGORIES)
        // ==========================================

        // GET: /Admin/Categories
        public async Task<IActionResult> Categories()
        {
            var categories = await _context.Categories
                .Include(c => c.Events)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return View(categories);
        }

        // POST: /Admin/CreateCategory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(Category category)
        {
            if (string.IsNullOrWhiteSpace(category.Name))
            {
                TempData["ErrorMessage"] = "Tên danh mục không được để trống!";
                return RedirectToAction("Categories");
            }

            category.CreatedAt = DateTime.Now;
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Thêm mới danh mục '{category.Name}' thành công!";
            return RedirectToAction("Categories");
        }

        // POST: /Admin/EditCategory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(Category model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                TempData["ErrorMessage"] = "Tên danh mục không được để trống!";
                return RedirectToAction("Categories");
            }

            var category = await _context.Categories.FindAsync(model.Id);
            if (category == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy danh mục.";
                return RedirectToAction("Categories");
            }

            category.Name = model.Name;
            category.Description = model.Description;
            category.IsActive = model.IsActive;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Cập nhật danh mục '{category.Name}' thành công!";
            return RedirectToAction("Categories");
        }

        // POST: /Admin/ToggleCategoryStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCategoryStatus(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy danh mục.";
                return RedirectToAction("Categories");
            }

            category.IsActive = !category.IsActive;
            await _context.SaveChangesAsync();

            string statusText = category.IsActive ? "kích hoạt" : "tạm dừng";
            TempData["SuccessMessage"] = $"Đã {statusText} danh mục '{category.Name}'!";
            return RedirectToAction("Categories");
        }

        // POST: /Admin/DeleteCategory (Acceptance criteria: Không cho phép xóa danh mục đang có sự kiện liên kết)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Events)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy danh mục.";
                return RedirectToAction("Categories");
            }

            if (category.Events.Any())
            {
                TempData["ErrorMessage"] = $"Không thể xóa danh mục '{category.Name}' vì đang có {category.Events.Count} sự kiện trực thuộc!";
                return RedirectToAction("Categories");
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã xóa danh mục '{category.Name}' thành công!";
            return RedirectToAction("Categories");
        }
    }
}

