using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quan_ly_su_kien.Data;

namespace Quan_ly_su_kien.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AuditLogsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuditLogsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search, string? actionFilter, DateTime? fromDate, DateTime? toDate, int page = 1)
        {
            int pageSize = 15;
            var query = _context.AuditLogs
                .Include(a => a.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string kw = search.Trim().ToLower();
                query = query.Where(a =>
                    a.Details.ToLower().Contains(kw) ||
                    (a.IpAddress != null && a.IpAddress.Contains(kw)) ||
                    (a.User != null && (
                        (a.User.UserName != null && a.User.UserName.ToLower().Contains(kw)) ||
                        (a.User.FullName != null && a.User.FullName.ToLower().Contains(kw)) ||
                        (a.User.Email != null && a.User.Email.ToLower().Contains(kw))
                    ))
                );
            }

            if (!string.IsNullOrWhiteSpace(actionFilter))
            {
                query = query.Where(a => a.Action == actionFilter);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(a => a.Timestamp.Date >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                query = query.Where(a => a.Timestamp.Date <= toDate.Value.Date);
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            page = Math.Max(1, Math.Min(page, totalPages == 0 ? 1 : totalPages));

            var logs = await query
                .OrderByDescending(a => a.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Actions = await _context.AuditLogs
                .Select(a => a.Action)
                .Distinct()
                .OrderBy(a => a)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.ActionFilter = actionFilter;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(logs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearOldLogs(int days = 30)
        {
            var cutoff = DateTime.Now.AddDays(-days);
            var oldLogs = await _context.AuditLogs.Where(a => a.Timestamp < cutoff).ToListAsync();

            if (oldLogs.Any())
            {
                _context.AuditLogs.RemoveRange(oldLogs);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã dọn dẹp {oldLogs.Count} bản ghi nhật ký cũ hơn {days} ngày.";
            }
            else
            {
                TempData["ErrorMessage"] = $"Không có bản ghi nào cũ hơn {days} ngày.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}