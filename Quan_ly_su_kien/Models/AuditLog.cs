using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Quan_ly_su_kien.Models
{
    [Table("AuditLogs")]
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Hành động")]
        public string Action { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Chi tiết thao tác")]
        public string Details { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Địa chỉ IP")]
        public string? IpAddress { get; set; }

        [Display(Name = "Thời gian ghi nhận")]
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}

