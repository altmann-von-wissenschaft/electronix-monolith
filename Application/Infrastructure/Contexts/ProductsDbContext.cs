using Microsoft.EntityFrameworkCore;
using Domain.Products;

namespace Infrastructure.Contexts;

public class ProductsDbContext : DbContext
{
    public ProductsDbContext(DbContextOptions<ProductsDbContext> options) : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductAttribute> ProductAttributes => Set<ProductAttribute>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("products");

        // Categories
        modelBuilder.Entity<Category>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired();
            e.HasIndex(x => x.Name);
            // Performance index for hierarchical category queries
            e.HasIndex(x => new { x.ParentId, x.DisplayOrder, x.Name })
                .HasDatabaseName("IX_Categories_ParentId_DisplayOrder_Name");
            e.HasOne(x => x.Parent)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Products
        modelBuilder.Entity<Product>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired();
            e.Property(x => x.Price).HasPrecision(12, 2);
            e.HasIndex(x => x.CategoryId);
            // Performance indexes for high-traffic queries
            e.HasIndex(x => x.IsHidden)
                .HasDatabaseName("IX_Products_IsHidden");
            e.HasIndex(x => new { x.CategoryId, x.IsHidden })
                .HasDatabaseName("IX_Products_CategoryId_IsHidden");
            e.HasIndex(x => x.CreatedAt)
                .HasDatabaseName("IX_Products_CreatedAt")
                .IsDescending(true);
            e.HasOne(x => x.Category)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ProductAttributes
        modelBuilder.Entity<ProductAttribute>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Product)
                .WithMany(x => x.Attributes)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ProductImages
        modelBuilder.Entity<ProductImage>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Product)
                .WithMany(x => x.Images)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
