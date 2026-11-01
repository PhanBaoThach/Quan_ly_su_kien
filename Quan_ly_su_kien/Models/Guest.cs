using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Quan_ly_su_kien.Models
{
    [Table("Guests")]
    public class Guest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EventId { get; set; }

        [ForeignKey("EventId")]
        public virtual Event? Event { get; set; }

        [Required(ErrorMessage = "Họ tên khách mời không được để trống")]
        [StringLength(150)]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(256)]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [StringLength(150)]
        [Display(Name = "Vai trò / Chức danh")]
        public string? RoleTitle { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Mã thư mời")]
        public string InvitationCode { get; set; } = string.Empty;

        [Display(Name = "Đã check-in")]
        public bool IsCheckedIn { get; set; } = false;
    }
}

