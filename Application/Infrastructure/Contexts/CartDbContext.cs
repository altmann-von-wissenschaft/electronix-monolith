using Microsoft.EntityFrameworkCore;
using Domain.Cart;

namespace Infrastructure.Contexts;

public class CartDbContext : DbContext
{
    public CartDbContext(DbContextOptions<CartDbContext> options) : base(options) { }

    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("cart");

        // Cart
        modelBuilder.Entity<Cart>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserId).IsUnique();
            e.HasMany(x => x.Items)
                .WithOne(x => x.Cart);
        });

        // CartItems
        modelBuilder.Entity<CartItem>(e =>
        {
            e.HasKey(x => x.Id);
            // Performance index for cross-context product lookups and cart operations
            e.HasIndex(x => x.ProductId);
            e.HasIndex(x => new { x.CartId, x.ProductId })
                .HasDatabaseName("IX_CartItems_CartId_ProductId");
            e.HasOne(x => x.Cart)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.CartId)
                .OnDelete(DeleteBehavior.Cascade);
            // ProductId is not a FK - references Products module
        });
    }
}
