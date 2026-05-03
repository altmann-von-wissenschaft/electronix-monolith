using Microsoft.EntityFrameworkCore;
using Domain.Products;

namespace Infrastructure.Contexts;

public class ProductsDbContext : DbContext
{
    public ProductsDbContext(DbContextOptions<ProductsDbContext> options) : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Characteristic> Characteristics => Set<Characteristic>();
    public DbSet<CategoryCharacteristic> CategoryCharacteristics => Set<CategoryCharacteristic>();
    public DbSet<ProductCharacteristicValue> ProductCharacteristicValues => Set<ProductCharacteristicValue>();
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

        // Characteristic - Reusable product characteristics/attributes
        modelBuilder.Entity<Characteristic>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired();
            e.Property(x => x.Unit).IsRequired();
            e.HasIndex(x => x.Name)
                .HasDatabaseName("IX_Characteristics_Name");
            e.HasMany(x => x.CategoryCharacteristics)
                .WithOne(x => x.Characteristic)
                .HasForeignKey(x => x.CharacteristicId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.ProductValues)
                .WithOne(x => x.Characteristic)
                .HasForeignKey(x => x.CharacteristicId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // CategoryCharacteristic - Join table for Category-Characteristic relationship
        modelBuilder.Entity<CategoryCharacteristic>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Category)
                .WithMany(x => x.Characteristics)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Characteristic)
                .WithMany(x => x.CategoryCharacteristics)
                .HasForeignKey(x => x.CharacteristicId)
                .OnDelete(DeleteBehavior.Restrict);
            // Composite unique index: Each category can have each characteristic once
            e.HasIndex(x => new { x.CategoryId, x.CharacteristicId })
                .IsUnique()
                .HasDatabaseName("IX_CategoryCharacteristic_Unique");
        });

        // ProductCharacteristicValue - Product-specific values for characteristics
        modelBuilder.Entity<ProductCharacteristicValue>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Value).IsRequired();
            e.HasOne(x => x.Product)
                .WithMany(x => x.CharacteristicValues)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Characteristic)
                .WithMany(x => x.ProductValues)
                .HasForeignKey(x => x.CharacteristicId)
                .OnDelete(DeleteBehavior.Restrict);
            // Composite unique index: Each product has each characteristic value once
            e.HasIndex(x => new { x.ProductId, x.CharacteristicId })
                .IsUnique()
                .HasDatabaseName("IX_ProductCharacteristicValue_Unique");
            // Index for product characteristics lookup
            e.HasIndex(x => x.ProductId)
                .HasDatabaseName("IX_ProductCharacteristicValue_ProductId");
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
