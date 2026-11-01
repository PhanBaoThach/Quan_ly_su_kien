using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Quan_ly_su_kien.Models
{
    [Table("Events")]
    public class Event
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tiêu đề sự kiện không được để trống")]
        [StringLength(200, ErrorMessage = "Tiêu đề tối đa 200 ký tự")]
        [Display(Name = "Tiêu đề sự kiện")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mô tả sự kiện không được để trống")]
        [Display(Name = "Mô tả chi tiết")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa điểm không được để trống")]
        [StringLength(250, ErrorMessage = "Địa điểm tối đa 250 ký tự")]
        [Display(Name = "Địa điểm tổ chức")]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Thời gian bắt đầu không được để trống")]
        [Display(Name = "Thời gian bắt đầu")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Thời gian kết thúc không được để trống")]
        [Display(Name = "Thời gian kết thúc")]
        public DateTime EndDate { get; set; }

        [Display(Name = "Hình ảnh")]
        public string? ImageUrl { get; set; }

        [Required(ErrorMessage = "Số lượng vé tối đa không được để trống")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng vé phải lớn hơn 0")]
        [Display(Name = "Số lượng vé tối đa")]
        public int MaxTickets { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Draft"; // Draft, Open, Ongoing, Cancelled, Completed

        [Required]
        [Display(Name = "Danh mục")]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        [Required]
        [Display(Name = "Người tổ chức")]
        public string OrganizerId { get; set; } = string.Empty;

        [ForeignKey("OrganizerId")]
        public virtual ApplicationUser? Organizer { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual ICollection<TicketType> TicketTypes { get; set; } = new List<TicketType>();
        public virtual ICollection<Registration> Registrations { get; set; } = new List<Registration>();
        public virtual ICollection<Guest> Guests { get; set; } = new List<Guest>();
        public virtual ICollection<EventFeedback> Feedbacks { get; set; } = new List<EventFeedback>();
    }
}

