using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AIstudentskillexchange.Models;

namespace AIstudentskillexchange.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Skill> Skills { get; set; }
        public DbSet<StudentSkill> StudentSkills { get; set; }
        public DbSet<LearningRequest> LearningRequests { get; set; }
        public DbSet<LearningSession> LearningSessions { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<LearningRequest>()
                .HasOne(lr => lr.Sender)
                .WithMany(u => u.SentRequests)
                .HasForeignKey(lr => lr.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<LearningRequest>()
                .HasOne(lr => lr.Receiver)
                .WithMany(u => u.ReceivedRequests)
                .HasForeignKey(lr => lr.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
