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
            e.HasMany(x => x.Answers)
                .WithOne(x => x.Question)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Answers
        modelBuilder.Entity<Answer>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Question)
                .WithMany(x => x.Answers)
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
