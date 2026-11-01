using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quan_ly_su_kien.Data;
using Quan_ly_su_kien.Models;
using Quan_ly_su_kien.ViewModels;

namespace Quan_ly_su_kien.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class HomeController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public HomeController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // 1. Thống kê số lượng tổng quan
            ViewBag.TotalUsers = await _userManager.Users.CountAsync();
            ViewBag.ActiveUsers = await _userManager.Users.CountAsync(u => u.IsActive);
            ViewBag.TotalEvents = await _context.Events.CountAsync();
            ViewBag.TotalCategories = await _context.Categories.CountAsync();

            // 2. Thống kê theo phân quyền (Roles)
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            var orgUsers = await _userManager.GetUsersInRoleAsync("Organizer");
            var attUsers = await _userManager.GetUsersInRoleAsync("Attendee");

            ViewBag.AdminCount = adminUsers.Count;
            ViewBag.OrgCount = orgUsers.Count;
            ViewBag.AttCount = attUsers.Count;

            // 3. Lấy 5 người dùng mới nhất truyền làm Model cho View (tránh lỗi null)
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
    }
}