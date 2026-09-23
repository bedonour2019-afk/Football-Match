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
        public DbSet<Post> Posts { get; set; }
        public DbSet<PostComment> PostComments { get; set; }
        public DbSet<PostReaction> PostReactions { get; set; }
        public DbSet<PushSubscriber> PushSubscribers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<MessageReaction>()
                .HasOne(r => r.ChatMessage)
                .WithMany(m => m.Reactions)
                .HasForeignKey(r => r.ChatMessageId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MessageReaction>()
                .HasOne(r => r.Reacter)
                .WithMany(a => a.MessageReactions)
                .HasForeignKey(r => r.AttendanceId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ChatMessage>()
                .HasOne(m => m.Sender)
                .WithMany(a => a.ChatMessages)
                .HasForeignKey(m => m.AttendanceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
