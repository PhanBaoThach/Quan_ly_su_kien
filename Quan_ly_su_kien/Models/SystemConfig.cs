using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Quan_ly_su_kien.Models
{
    [Table("SystemConfigs")]
    public class SystemConfig
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        [Display(Name = "Khóa cấu hình")]
        public string ConfigKey { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Giá trị cấu hình")]
        public string ConfigValue { get; set; } = string.Empty;

        [StringLength(300)]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }
    }
}

