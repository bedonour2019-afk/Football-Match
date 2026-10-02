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
        public DbSet<Story> Stories { get; set; }
        public DbSet<DirectMessage> DirectMessages { get; set; }
        public DbSet<MatchSetting> MatchSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DirectMessage>()
                .HasOne(d => d.Sender)
                .WithMany()
                .HasForeignKey(d => d.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DirectMessage>()
                .HasOne(d => d.Receiver)
                .WithMany()
                .HasForeignKey(d => d.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

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

            modelBuilder.Entity<Story>()
                .HasOne(s => s.Author)
                .WithMany(a => a.Stories)
                .HasForeignKey(s => s.AttendanceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Post>()
                .HasOne(p => p.Author)
                .WithMany(a => a.Posts)
                .HasForeignKey(p => p.AttendanceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
