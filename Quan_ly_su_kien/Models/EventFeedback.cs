using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Quan_ly_su_kien.Models
{
    [Table("EventFeedbacks")]
    public class EventFeedback
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EventId { get; set; }

        [ForeignKey("EventId")]
        public virtual Event? Event { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Điểm đánh giá từ 1 đến 5 sao")]
        [Display(Name = "Đánh giá")]
        public int Rating { get; set; }

        [StringLength(1000)]
        [Display(Name = "Bình luận nhận xét")]
        public string? Comment { get; set; }

        [Display(Name = "Ẩn bình luận")]
        public bool IsHidden { get; set; } = false;

        [Display(Name = "Ngày đánh giá")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

