using Microsoft.EntityFrameworkCore;
using Domain.Orders;

namespace Infrastructure.Contexts;

public class OrdersDbContext : DbContext
{
    public OrdersDbContext(DbContextOptions<OrdersDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("orders");

        // Orders
        modelBuilder.Entity<Order>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.TotalAmount).HasPrecision(14, 2);
            e.HasIndex(x => x.UserId);
            e.HasMany(x => x.Items)
                .WithOne(x => x.Order);
            e.HasMany(x => x.StatusHistory)
                .WithOne(x => x.Order);
        });

        // OrderItems
        modelBuilder.Entity<OrderItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.PriceAtPurchase).HasPrecision(12, 2);
            e.HasOne(x => x.Order)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // OrderStatusHistory
        modelBuilder.Entity<OrderStatusHistory>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Order)
                .WithMany(x => x.StatusHistory)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
