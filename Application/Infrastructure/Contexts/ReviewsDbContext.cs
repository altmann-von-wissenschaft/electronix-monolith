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
            e.HasIndex(x => x.ProductId);
            e.HasIndex(x => x.UserId);
            // ProductId and UserId are not FKs - reference other modules
        });
    }
}
