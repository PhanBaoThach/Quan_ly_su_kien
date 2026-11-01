using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Quan_ly_su_kien.Models
{
    [Table("TicketTypes")]
    public class TicketType
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EventId { get; set; }

        [ForeignKey("EventId")]
        public virtual Event? Event { get; set; }

        [Required(ErrorMessage = "Tên loại vé không được để trống")]
        [StringLength(100)]
        [Display(Name = "Tên loại vé")]
        public string TypeName { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Giá vé")]
        public decimal Price { get; set; } = 0;

        [Required]
        [Display(Name = "Số lượng vé phát hành")]
        public int Quantity { get; set; }

        [Display(Name = "Số lượng vé đã bán")]
        public int SoldQuantity { get; set; } = 0;

        [Required]
        [Display(Name = "Thời gian mở bán")]
        public DateTime SaleStart { get; set; }

        [Required]
        [Display(Name = "Thời gian đóng bán")]
        public DateTime SaleEnd { get; set; }

        public virtual ICollection<Registration> Registrations { get; set; } = new List<Registration>();
    }
}

