using EduMaxBot.Models;
using Microsoft.EntityFrameworkCore;

namespace EduMaxBot.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Group> Groups { get; set; } = null!;
    public DbSet<GroupMember> GroupMembers { get; set; } = null!;
    public DbSet<InviteToken> InviteTokens { get; set; } = null!;
    public DbSet<Assignment> Assignments { get; set; } = null!;
    public DbSet<AssignmentVariant> AssignmentVariants { get; set; } = null!;
    public DbSet<Submission> Submissions { get; set; } = null!;
    public DbSet<ReviewSession> ReviewSessions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // ---------- Users ----------
        b.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasKey(x => x.UserId);
        });

        // ---------- Groups -> BotGroups ----------
        b.Entity<Group>(e =>
        {
            // ВАЖНО: используем таблицу "BotGroups", а не "Groups"
            e.ToTable("BotGroups");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired();
            e.HasIndex(x => x.OwnerId);
        });

        // ---------- GroupMembers ----------
        b.Entity<GroupMember>(e =>
        {
            e.ToTable("GroupMembers");
            e.HasKey(x => new { x.GroupId, x.UserId });
        });

        // ---------- InviteTokens ----------
        b.Entity<InviteToken>(e =>
        {
            e.ToTable("InviteTokens");
            e.HasKey(x => x.Token);
            e.HasIndex(x => x.GroupId);
        });

        // ---------- Assignments ----------
        b.Entity<Assignment>(e =>
        {
            e.ToTable("Assignments");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.GroupId);
        });

        // ---------- AssignmentVariants ----------
        b.Entity<AssignmentVariant>(e =>
        {
            e.ToTable("AssignmentVariants");
            e.HasKey(x => new { x.AssignmentId, x.UserId });
        });

        // ---------- Submissions ----------
        b.Entity<Submission>(e =>
        {
            e.ToTable("Submissions");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.AssignmentId, x.Status, x.SubmittedAt });
        });

        // ---------- ReviewSessions ----------
        b.Entity<ReviewSession>(e =>
        {
            e.ToTable("ReviewSessions");
            e.HasKey(x => new { x.AssignmentId, x.ReviewerId });
        });
    }
}
