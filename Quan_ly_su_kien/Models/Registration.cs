using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Quan_ly_su_kien.Models
{
    [Table("Registrations")]
    public class Registration
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
        public int TicketTypeId { get; set; }

        [ForeignKey("TicketTypeId")]
        public virtual TicketType? TicketType { get; set; }

        [Required]
        [Display(Name = "Số lượng vé")]
        public int Quantity { get; set; } = 1;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Tổng tiền")]
        public decimal TotalPrice { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Trạng thái đăng ký")]
        public string Status { get; set; } = "Pending"; // Pending, Confirmed, Cancelled

        [Display(Name = "Trạng thái Check-in")]
        public bool CheckInStatus { get; set; } = false;

        [Display(Name = "Thời gian Check-in")]
        public DateTime? CheckInTime { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Mã QR")]
        public string QrCode { get; set; } = string.Empty;

        [Display(Name = "Thời gian đăng ký")]
        public DateTime RegisteredAt { get; set; } = DateTime.Now;
    }
}

