using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Quan_ly_su_kien.Models;

namespace Quan_ly_su_kien.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<TicketType> TicketTypes { get; set; }
        public DbSet<Registration> Registrations { get; set; }
        public DbSet<Guest> Guests { get; set; }
        public DbSet<EventFeedback> EventFeedbacks { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<SystemConfig> SystemConfigs { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure Event
            builder.Entity<Event>()
                .HasOne(e => e.Category)
                .WithMany(c => c.Events)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Event>()
                .HasOne(e => e.Organizer)
                .WithMany(u => u.OrganizedEvents)
                .HasForeignKey(e => e.OrganizerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure TicketType
            builder.Entity<TicketType>()
                .HasOne(t => t.Event)
                .WithMany(e => e.TicketTypes)
                .HasForeignKey(t => t.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Registration
            builder.Entity<Registration>()
                .HasOne(r => r.Event)
                .WithMany(e => e.Registrations)
                .HasForeignKey(r => r.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Registration>()
                .HasOne(r => r.User)
                .WithMany(u => u.Registrations)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Registration>()
                .HasOne(r => r.TicketType)
                .WithMany(t => t.Registrations)
                .HasForeignKey(r => r.TicketTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Guest
            builder.Entity<Guest>()
                .HasOne(g => g.Event)
                .WithMany(e => e.Guests)
                .HasForeignKey(g => g.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure EventFeedback
            builder.Entity<EventFeedback>()
                .HasOne(f => f.Event)
                .WithMany(e => e.Feedbacks)
                .HasForeignKey(f => f.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<EventFeedback>()
                .HasOne(f => f.User)
                .WithMany(u => u.Feedbacks)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Notification
            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure AuditLog
            builder.Entity<AuditLog>()
                .HasOne(a => a.User)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Unique index for SystemConfig key
            builder.Entity<SystemConfig>()
                .HasIndex(s => s.ConfigKey)
                .IsUnique();
        }
    }
}
