using Microsoft.EntityFrameworkCore;
using Domain.Support;

namespace Infrastructure.Contexts;

public class SupportDbContext : DbContext
{
    public SupportDbContext(DbContextOptions<SupportDbContext> options) : base(options) { }

    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Answer> Answers => Set<Answer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("support");

        // Questions
        modelBuilder.Entity<Question>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserId);
            e.Property(x => x.Subject).IsRequired();
            e.Property(x => x.Content).IsRequired();
            // Performance indexes for support query filtering and sorting
            e.HasIndex(x => x.IsAnswered)
                .HasDatabaseName("IX_Questions_IsAnswered");
            e.HasIndex(x => x.CreatedAt)
                .HasDatabaseName("IX_Questions_CreatedAt")
                .IsDescending(true);
            e.HasIndex(x => new { x.UserId, x.IsAnswered })
                .HasDatabaseName("IX_Questions_UserId_IsAnswered");
        });

        // Answers
        modelBuilder.Entity<Answer>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.QuestionId)
                .HasDatabaseName("IX_Answer_QuestionId");
            e.HasOne(x => x.Question)
                .WithOne(x => x.Answer)
                .HasForeignKey<Answer>(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
