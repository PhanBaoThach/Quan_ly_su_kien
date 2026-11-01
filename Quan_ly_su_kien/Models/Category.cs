using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Quan_ly_su_kien.Models
{
    [Table("Categories")]
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên danh mục không được để trống")]
        [StringLength(150, ErrorMessage = "Tên danh mục tối đa 150 ký tự")]
        [Display(Name = "Tên danh mục")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự")]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Display(Name = "Trạng thái kích hoạt")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation property
        public virtual ICollection<Event> Events { get; set; } = new List<Event>();
    }
}
