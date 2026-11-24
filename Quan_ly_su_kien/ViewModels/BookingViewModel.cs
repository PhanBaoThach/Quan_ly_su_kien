using System.ComponentModel.DataAnnotations;
using Quan_ly_su_kien.Models;

namespace Quan_ly_su_kien.ViewModels
{
    public class BookingViewModel
    {
        public int EventId { get; set; }

        public string EventTitle { get; set; } = string.Empty;

        public string EventLocation { get; set; } = string.Empty;

        public DateTime EventStartDate { get; set; }

        public DateTime EventEndDate { get; set; }

        public string? EventImageUrl { get; set; }

        public string? OrganizerName { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại vé.")]
        [Display(Name = "Loại vé")]
        public int TicketTypeId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số lượng vé.")]
        [Range(1, 10, ErrorMessage = "Mỗi lượt đăng ký được đặt từ 1 đến tối đa 10 vé.")]
        [Display(Name = "Số lượng vé")]
        public int Quantity { get; set; } = 1;

        [Required(ErrorMessage = "Vui lòng nhập họ và tên người nhận vé.")]
        [StringLength(150, ErrorMessage = "Họ tên không được vượt quá 150 ký tự.")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email nhận vé điện tử.")]
        [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ.")]
        [Display(Name = "Email nhận vé")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Số điện thoại liên hệ")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Ghi chú thêm")]
        [StringLength(500, ErrorMessage = "Ghi chú không quá 500 ký tự.")]
        public string? Notes { get; set; }

        public List<TicketType> AvailableTickets { get; set; } = new List<TicketType>();
    }
}

