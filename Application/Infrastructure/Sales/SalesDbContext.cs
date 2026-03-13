using Microsoft.EntityFrameworkCore;
using Domain.Sales;

namespace Infrastructure.Sales
{
    public class SalesDbContext : DbContext
    {
        public SalesDbContext(DbContextOptions<SalesDbContext> options)
            : base(options) { }

        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("sales");

            modelBuilder.Entity<Order>(e =>
            {
                e.HasKey(x => x.Id);

                e.Property(x => x.TotalAmount)
                    .HasPrecision(14, 2);
            });

            modelBuilder.Entity<OrderItem>(e =>
            {
                e.HasKey(x => new { x.OrderId, x.ProductId });

                e.Property(x => x.PriceAtPurchase)
                    .HasPrecision(12, 2);

                e.HasOne(x => x.Order)
                    .WithMany(x => x.Items)
                    .HasForeignKey(x => x.OrderId);
            });
        }
    }
}