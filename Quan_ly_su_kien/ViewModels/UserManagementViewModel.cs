using System.ComponentModel.DataAnnotations;

namespace Quan_ly_su_kien.ViewModels
{
    public class UserManagementViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
        [StringLength(150)]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập Email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        public string UserName { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Số điện thoại")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn vai trò")]
        [Display(Name = "Vai trò")]
        public string Role { get; set; } = "Attendee";
    }

    public class EditUserViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
        [StringLength(150)]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Số điện thoại")]
        public string? PhoneNumber { get; set; }

        [Required]
        [Display(Name = "Vai trò")]
        public string Role { get; set; } = string.Empty;

        [Display(Name = "Trạng thái kích hoạt")]
        public bool IsActive { get; set; }
    }
}

