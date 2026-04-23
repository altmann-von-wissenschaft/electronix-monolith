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
            // Performance indexes for order queries and sorting
            e.HasIndex(x => x.CreatedAt)
                .HasDatabaseName("IX_Orders_CreatedAt")
                .IsDescending(true);
            e.HasIndex(x => new { x.UserId, x.CreatedAt })
                .HasDatabaseName("IX_Orders_UserId_CreatedAt")
                .IsDescending(new[] { false, true });
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
            // Performance index for cross-context product lookups
            e.HasIndex(x => new { x.OrderId, x.ProductId })
                .HasDatabaseName("IX_OrderItems_OrderId_ProductId");
            e.HasOne(x => x.Order)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // OrderStatusHistory
        modelBuilder.Entity<OrderStatusHistory>(e =>
        {
            e.HasKey(x => x.Id);
            // Performance indexes for status history audit trails and sorting
            e.HasIndex(x => x.ChangedAt)
                .HasDatabaseName("IX_OrderStatusHistory_ChangedAt")
                .IsDescending(true);
            e.HasIndex(x => new { x.OrderId, x.ChangedAt })
                .HasDatabaseName("IX_OrderStatusHistory_OrderId_ChangedAt")
                .IsDescending(new[] { false, true });
            e.HasOne(x => x.Order)
                .WithMany(x => x.StatusHistory)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
