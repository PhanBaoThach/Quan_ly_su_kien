using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quan_ly_su_kien.Data;
using Quan_ly_su_kien.Models;
using Quan_ly_su_kien.Services;
using Quan_ly_su_kien.ViewModels;

namespace Quan_ly_su_kien.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogService _auditLog;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            IAuditLogService auditLog,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
            _auditLog = auditLog;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                return await DetermineUserRedirect(currentUser, returnUrl);
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var user = await _userManager.FindByNameAsync(model.UserNameOrEmail)
                           ?? await _userManager.FindByEmailAsync(model.UserNameOrEmail);

                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Tài khoản hoặc mật khẩu không chính xác.");
                    return View(model);
                }

                if (!user.IsActive)
                {
                    ModelState.AddModelError(string.Empty, "Tài khoản của bạn đã bị khóa hoặc ngừng hoạt động. Vui lòng liên hệ Quản trị viên.");
                    return View(model);
                }

                var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    await _auditLog.LogAsync(user.Id, "Đăng nhập", $"Tài khoản {user.UserName} đăng nhập thành công vào hệ thống.");
                    TempData["SuccessMessage"] = $"Đăng nhập thành công! Chào mừng {user.FullName ?? user.UserName}.";
                    return await DetermineUserRedirect(user, returnUrl);
                }

                ModelState.AddModelError(string.Empty, "Tài khoản hoặc mật khẩu không chính xác.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi kết nối cơ sở dữ liệu khi đăng nhập.");
                var msg = ex.InnerException != null ? $"{ex.Message} ({ex.InnerException.Message})" : ex.Message;
                ModelState.AddModelError(string.Empty, $"Lỗi đăng nhập: {msg}");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickLogin(string roleName)
        {
            try
            {
                string email = roleName switch
                {
                    "Admin" => "kkaitou37@gmail.com",
                    "Organizer" => "duong@gmail.com",
                    _ => "quy@gmail.com"
                };

                var user = await _userManager.FindByEmailAsync(email);
                if (user != null && user.IsActive)
                {
                    await _signInManager.SignInAsync(user, isPersistent: true);
                    await _auditLog.LogAsync(user.Id, "Đăng nhập nhanh", $"Đăng nhập nhanh với vai trò {roleName} ({user.UserName}).");
                    TempData["SuccessMessage"] = $"Đăng nhập thành công với vai trò: {roleName} ({user.FullName})";

                    if (roleName == "Admin")
                    {
                        return RedirectToAction("Index", "Admin", new { area = "" });
                    }
                    return RedirectToAction("Index", "Home", new { area = "" });
                }

                TempData["ErrorMessage"] = "Chưa tìm thấy dữ liệu tài khoản mẫu trong CSDL. Vui lòng kiểm tra kết nối SQL Server.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi kết nối CSDL khi đăng nhập nhanh.");
                TempData["ErrorMessage"] = "Không thể kết nối đến SQL Server. Vui lòng kiểm tra dịch vụ SQL Server.";
                return RedirectToAction("Login");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Register(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                return await DetermineUserRedirect(currentUser, returnUrl);
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (!string.IsNullOrEmpty(model.UserName) && !char.IsUpper(model.UserName[0]))
            {
                ModelState.AddModelError("UserName", "Tên tài khoản bắt buộc chữ cái đầu phải viết hoa.");
                return View(model);
            }

            try
            {
                var existingByUserName = await _userManager.FindByNameAsync(model.UserName);
                if (existingByUserName != null)
                {
                    ModelState.AddModelError("UserName", "Tên đăng nhập này đã được sử dụng.");
                    return View(model);
                }

                var existingByEmail = await _userManager.FindByEmailAsync(model.Email);
                if (existingByEmail != null)
                {
                    ModelState.AddModelError("Email", "Email này đã được đăng ký tài khoản khác.");
                    return View(model);
                }

                string assignedRole = model.Role == "Organizer" ? "Organizer" : "Attendee";

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
                    if (!await _roleManager.RoleExistsAsync(assignedRole))
                    {
                        await _roleManager.CreateAsync(new IdentityRole(assignedRole));
                    }

                    await _userManager.AddToRoleAsync(user, assignedRole);
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    await _auditLog.LogAsync(user.Id, "Đăng ký tài khoản", $"Tạo mới tài khoản {user.UserName} với vai trò {assignedRole}.");

                    TempData["SuccessMessage"] = "Đăng ký tài khoản thành công! Chào mừng bạn gia nhập hệ thống EventMana.";
                    return await DetermineUserRedirect(user, returnUrl);
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi kết nối cơ sở dữ liệu khi đăng ký.");
                var msg = ex.InnerException != null ? $"{ex.Message} ({ex.InnerException.Message})" : ex.Message;
                ModelState.AddModelError(string.Empty, $"Lỗi đăng ký: {msg}");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                await _auditLog.LogAsync(user.Id, "Đăng xuất", $"Tài khoản {user.UserName} đã đăng xuất khỏi hệ thống.");
            }
            await _signInManager.SignOutAsync();
            TempData["SuccessMessage"] = "Bạn đã đăng xuất khỏi hệ thống thành công.";
            return RedirectToAction("Login", "Account", new { area = "" });
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var roles = await _userManager.GetRolesAsync(user);

            // Lấy lịch sử tham gia sự kiện của người dùng (US-11)
            var history = await _context.Registrations
                .Include(r => r.Event)
                .Include(r => r.TicketType)
                .Where(r => r.UserId == user.Id)
                .OrderByDescending(r => r.Id)
                .ToListAsync();

            var model = new ProfileViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                AvatarUrl = user.AvatarUrl,
                CurrentRole = roles.FirstOrDefault() ?? "Thành viên",
                CreatedAt = user.CreatedAt,
                EventHistory = history
            };

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            if (!ModelState.IsValid)
            {
                model.EventHistory = await _context.Registrations
                    .Include(r => r.Event)
                    .Include(r => r.TicketType)
                    .Where(r => r.UserId == user.Id)
                    .OrderByDescending(r => r.Id)
                    .ToListAsync();
                return View("Profile", model);
            }

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.AvatarUrl = model.AvatarUrl;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                await _auditLog.LogAsync(user.Id, "Cập nhật hồ sơ", $"Người dùng {user.UserName} cập nhật thông tin cá nhân (Họ tên: {user.FullName}, SĐT: {user.PhoneNumber}).");
                TempData["SuccessMessage"] = "Cập nhật thông tin hồ sơ thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể cập nhật hồ sơ.";
            }

            return RedirectToAction("Profile");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Thông tin đổi mật khẩu chưa hợp lệ.";
                return RedirectToAction("Profile");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                await _auditLog.LogAsync(user.Id, "Đổi mật khẩu", $"Tài khoản {user.UserName} đã thay đổi mật khẩu thành công.");
                TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
            }
            else
            {
                string errors = string.Join("; ", result.Errors.Select(e => e.Description));
                TempData["ErrorMessage"] = $"Đổi mật khẩu thất bại: {errors}";
            }

            return RedirectToAction("Profile");
        }

        private async Task<IActionResult> DetermineUserRedirect(ApplicationUser? user, string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            if (user != null)
            {
                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                if (isAdmin)
                {
                    return RedirectToAction("Index", "Admin", new { area = "" });
                }
            }

            return RedirectToAction("Index", "Home", new { area = "" });
        }
    }
}