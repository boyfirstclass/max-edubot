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

        b.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasKey(x => x.UserId);
        });

        b.Entity<Group>(e =>
        {
            e.ToTable("BotGroups");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired();
            e.HasIndex(x => x.OwnerId);
        });

        b.Entity<GroupMember>(e =>
        {
            e.ToTable("GroupMembers");
            e.HasKey(x => new { x.GroupId, x.UserId });
        });

        b.Entity<InviteToken>(e =>
        {
            e.ToTable("InviteTokens");
            e.HasKey(x => x.Token);
            e.HasIndex(x => x.GroupId);
        });

        b.Entity<Assignment>(e =>
        {
            e.ToTable("Assignments");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.GroupId);
        });

        b.Entity<AssignmentVariant>(e =>
        {
            e.ToTable("AssignmentVariants");
            e.HasKey(x => new { x.AssignmentId, x.UserId });
        });

        b.Entity<Submission>(e =>
        {
            e.ToTable("Submissions");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.AssignmentId, x.Status, x.SubmittedAt });
        });

        b.Entity<ReviewSession>(e =>
        {
            e.ToTable("ReviewSessions");
            e.HasKey(x => new { x.AssignmentId, x.ReviewerId });
        });
    }
}
