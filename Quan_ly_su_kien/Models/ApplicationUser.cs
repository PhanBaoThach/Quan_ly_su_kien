using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Quan_ly_su_kien.Models
{
    public class ApplicationUser : IdentityUser
    {
        [StringLength(150)]
        [Display(Name = "Họ và tên")]
        public string? FullName { get; set; }

        [Display(Name = "Ảnh đại diện")]
        public string? AvatarUrl { get; set; }

        [Display(Name = "Trạng thái hoạt động")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual ICollection<Event> OrganizedEvents { get; set; } = new List<Event>();
        public virtual ICollection<Registration> Registrations { get; set; } = new List<Registration>();
        public virtual ICollection<EventFeedback> Feedbacks { get; set; } = new List<EventFeedback>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    }
}

