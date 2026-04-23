using Microsoft.EntityFrameworkCore;
using Domain.Reviews;

namespace Infrastructure.Contexts;

public class ReviewsDbContext : DbContext
{
    public ReviewsDbContext(DbContextOptions<ReviewsDbContext> options) : base(options) { }

    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("reviews");

        // Reviews
        modelBuilder.Entity<Review>(e =>
        {
            e.HasKey(x => x.Id);
            // Performance indexes for review filtering and sorting
            e.HasIndex(x => x.ProductId);
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.IsApproved)
                .HasDatabaseName("IX_Reviews_IsApproved");
            e.HasIndex(x => x.CreatedAt)
                .HasDatabaseName("IX_Reviews_CreatedAt")
                .IsDescending(true);
            e.HasIndex(x => new { x.ProductId, x.IsApproved })
                .HasDatabaseName("IX_Reviews_ProductId_IsApproved");
            e.HasIndex(x => new { x.UserId, x.IsApproved, x.CreatedAt })
                .HasDatabaseName("IX_Reviews_UserId_IsApproved_CreatedAt")
                .IsDescending(new[] { false, false, true });
            // ProductId and UserId are not FKs - reference other modules
        });
    }
}
