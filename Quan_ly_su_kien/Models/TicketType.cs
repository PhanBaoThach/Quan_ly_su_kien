using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Quan_ly_su_kien.Models
{
    public class TicketType
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn sự kiện.")]
        public int EventId { get; set; }

        [ForeignKey("EventId")]
        public virtual Event? Event { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên loại vé.")]
        [StringLength(100, ErrorMessage = "Tên loại vé không vượt quá 100 ký tự.")]
        [Display(Name = "Tên loại vé")]
        public string TypeName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập giá vé.")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá vé không được nhỏ hơn 0.")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Giá vé (VNĐ)")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tổng số lượng vé.")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng vé phải lớn hơn 0.")]
        [Display(Name = "Tổng số lượng")]
        public int QuantityTotal { get; set; }

        [Display(Name = "Số lượng còn lại")]
        public int QuantityAvailable { get; set; }

        [StringLength(500, ErrorMessage = "Mô tả không quá 500 ký tự.")]
        [Display(Name = "Mô tả quyền lợi")]
        public string? Description { get; set; }

        [Display(Name = "Trạng thái mở bán")]
        public bool IsActive { get; set; } = true;

        // --- CÁC THUỘC TÍNH TƯƠNG THÍCH VỚI CODE CỦA TEAM ---

        // Map Quantity tương đương với QuantityTotal
        [NotMapped]
        public int Quantity
        {
            get => QuantityTotal;
            set => QuantityTotal = value;
        }

        // Cho phép DbInitializer gán SoldQuantity mà không bị lỗi CS0200
        private int _soldQuantity;
        [NotMapped]
        public int SoldQuantity
        {
            get => QuantityTotal > 0 ? (QuantityTotal - QuantityAvailable) : _soldQuantity;
            set => _soldQuantity = value;
        }

        // Khai báo kiểu DateTime để hỗ trợ hàm .ToString("dd/MM/yyyy") trong Details.cshtml
        [Display(Name = "Thời gian bắt đầu bán")]
        public DateTime SaleStart { get; set; } = DateTime.Now;

        [Display(Name = "Thời gian kết thúc bán")]
        public DateTime SaleEnd { get; set; } = DateTime.Now.AddDays(30);

        // Relationship với Registration
        public virtual ICollection<Registration>? Registrations { get; set; }
    }
}