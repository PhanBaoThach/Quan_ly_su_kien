using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Quan_ly_su_kien.ViewModels
{
    public class EventFormViewModel : IValidatableObject
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề sự kiện")]
        [StringLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự")]
        [Display(Name = "Tiêu đề sự kiện")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mô tả chi tiết")]
        [Display(Name = "Mô tả sự kiện")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa điểm tổ chức")]
        [StringLength(250, ErrorMessage = "Địa điểm không được vượt quá 250 ký tự")]
        [Display(Name = "Địa điểm")]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn thời gian bắt đầu")]
        [Display(Name = "Thời gian bắt đầu")]
        public DateTime StartDate { get; set; } = DateTime.Now.AddDays(1);

        [Required(ErrorMessage = "Vui lòng chọn thời gian kết thúc")]
        [Display(Name = "Thời gian kết thúc")]
        public DateTime EndDate { get; set; } = DateTime.Now.AddDays(1).AddHours(4);

        [Display(Name = "Đường dẫn ảnh bìa (URL)")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Tải lên ảnh bìa từ máy")]
        public IFormFile? ImageFile { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số lượng vé tối đa")]
        [Range(1, 1000000, ErrorMessage = "Số lượng vé phát hành phải lớn hơn 0")]
        [Display(Name = "Số lượng vé tối đa")]
        public int MaxTickets { get; set; } = 100;

        [Required(ErrorMessage = "Vui lòng chọn trạng thái")]
        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Draft";

        [Required(ErrorMessage = "Vui lòng chọn danh mục sự kiện")]
        [Display(Name = "Danh mục sự kiện")]
        public int CategoryId { get; set; }

        // Kiểm tra hợp lệ thời gian kết thúc phải sau thời gian bắt đầu
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EndDate <= StartDate)
            {
                yield return new ValidationResult(
                    "Thời gian kết thúc phải diễn ra sau thời gian bắt đầu.",
                    new[] { nameof(EndDate) });
            }
        }
    }
}

