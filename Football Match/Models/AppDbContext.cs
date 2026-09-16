using Microsoft.EntityFrameworkCore;
using Football_Match.Models;

namespace Football_Match
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<MessageReaction> MessageReactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // إلغاء الحذف المتتابع لمنع تضارب Foreign Key
            modelBuilder.Entity<MessageReaction>()
                .HasOne(r => r.ChatMessage)
                .WithMany()
                .HasForeignKey(r => r.ChatMessageId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}