using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quan_ly_su_kien.Data;

namespace Quan_ly_su_kien.Controllers
{
    [Authorize(Roles = "Organizer,Admin")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Reports/Index
        public async Task<IActionResult> Index(int? eventId)
        {
            // 1. Lấy danh sách sự kiện làm bộ lọc Dropdown
            var events = await _context.Events.ToListAsync();
            ViewBag.Events = events;
            ViewBag.SelectedEventId = eventId;

            // 2. Truy vấn dữ liệu vé kèm thông tin sự kiện
            var ticketQuery = _context.TicketTypes
                .Include(t => t.Event)
                .AsQueryable();

            if (eventId.HasValue && eventId.Value > 0)
            {
                ticketQuery = ticketQuery.Where(t => t.EventId == eventId.Value);
            }

            var tickets = await ticketQuery.ToListAsync();

            // 3. Tính toán thống kê chi tiết cho từng hạng vé
            var reportData = tickets.Select(t =>
            {
                int soldCount = t.QuantityTotal - t.QuantityAvailable;
                if (soldCount < 0) soldCount = 0;

                return new TicketReportItem
                {
                    EventTitle = t.Event?.Title ?? "Chưa xác định",
                    TicketName = t.TypeName,
                    Price = t.Price,
                    TotalQuantity = t.QuantityTotal,
                    SoldCount = soldCount,
                    Revenue = soldCount * t.Price
                };
            }).ToList();

            // 4. Tính các chỉ số tổng quan (KPI Cards)
            ViewBag.TotalRevenue = reportData.Sum(x => x.Revenue);
            ViewBag.TotalTicketsSold = reportData.Sum(x => x.SoldCount);
            ViewBag.TotalTicketsIssued = reportData.Sum(x => x.TotalQuantity);
            ViewBag.TotalEvents = eventId.HasValue && eventId.Value > 0 ? 1 : events.Count;

            // 5. Dữ liệu truyền sang biểu đồ Chart.js
            ViewBag.ChartLabels = reportData.Select(x => $"{x.EventTitle} ({x.TicketName})").ToList();
            ViewBag.ChartRevenues = reportData.Select(x => x.Revenue).ToList();

            return View(reportData);
        }
    }

    public class TicketReportItem
    {
        public string EventTitle { get; set; } = string.Empty;
        public string TicketName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int TotalQuantity { get; set; }
        public int SoldCount { get; set; }
        public decimal Revenue { get; set; }
    }
}