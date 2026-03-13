using Microsoft.EntityFrameworkCore;
using Domain.Catalog;

namespace Infrastructure.Catalog
{
    public class CatalogDbContext : DbContext
    {
        public CatalogDbContext(DbContextOptions<CatalogDbContext> options)
            : base(options) { }

        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductAttribute> Attributes => Set<ProductAttribute>();
        public DbSet<ProductAttributeValue> ProductAttributeValues => Set<ProductAttributeValue>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("catalog");

            modelBuilder.Entity<Category>(e =>
            {
                e.HasKey(x => x.Id);

                e.HasOne(x => x.Parent)
                    .WithMany(x => x.Children)
                    .HasForeignKey(x => x.ParentId);
            });

            modelBuilder.Entity<Product>(e =>
            {
                e.HasKey(x => x.Id);

                e.Property(x => x.Price)
                    .HasPrecision(12, 2);

                e.HasIndex(x => x.CategoryId);

                e.HasOne(x => x.Category)
                    .WithMany(x => x.Products)
                    .HasForeignKey(x => x.CategoryId);
            });

            modelBuilder.Entity<ProductAttribute>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.Name);
            });

            modelBuilder.Entity<ProductAttributeValue>(e =>
            {
                e.HasKey(x => new { x.ProductId, x.AttributeId });

                e.HasOne(x => x.Product)
                    .WithMany(x => x.AttributeValues)
                    .HasForeignKey(x => x.ProductId);

                e.HasOne(x => x.Attribute)
                    .WithMany(x => x.Values)
                    .HasForeignKey(x => x.AttributeId);
            });
        }
    }
}