using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Quan_ly_su_kien.Models
{
    [Table("Notifications")]
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Tiêu đề")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Nội dung")]
        public string Message { get; set; } = string.Empty;

        [Display(Name = "Đã đọc")]
        public bool IsRead { get; set; } = false;

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

