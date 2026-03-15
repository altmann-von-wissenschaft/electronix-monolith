using Microsoft.EntityFrameworkCore;
using Domain.Users;
using Domain.Products;
using Domain.Orders;
using Domain.Cart;
using Domain.Reviews;
using Domain.Support;

namespace Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Users Module
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    // Products Module
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductAttribute> ProductAttributes => Set<ProductAttribute>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();

    // Orders Module
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();

    // Cart Module
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();

    // Reviews Module
    public DbSet<Review> Reviews => Set<Review>();

    // Support Module
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Answer> Answers => Set<Answer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Users Module Configuration
        modelBuilder.Entity<Role>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Name).IsRequired();
        });

        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).IsRequired();
            e.Property(x => x.PasswordHash).IsRequired();
            e.HasMany(x => x.UserRoles)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserRole>(e =>
        {
            e.HasKey(x => new { x.UserId, x.RoleId });
            e.HasOne(x => x.User)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.UserId);
            e.HasOne(x => x.Role)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.RoleId);
        });

        // Products Module Configuration
        modelBuilder.Entity<Category>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired();
            e.HasIndex(x => x.Name);
            e.HasOne(x => x.Parent)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired();
            e.Property(x => x.Price).HasPrecision(12, 2);
            e.HasIndex(x => x.CategoryId);
            e.HasOne(x => x.Category)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProductAttribute>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Product)
                .WithMany(x => x.Attributes)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProductImage>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Product)
                .WithMany(x => x.Images)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Orders Module Configuration
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

        modelBuilder.Entity<OrderItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.PriceAtPurchase).HasPrecision(12, 2);
            e.HasOne(x => x.Order)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderStatusHistory>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Order)
                .WithMany(x => x.StatusHistory)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Cart Module Configuration
        modelBuilder.Entity<Cart>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserId).IsUnique();
            e.HasMany(x => x.Items)
                .WithOne(x => x.Cart);
        });

        modelBuilder.Entity<CartItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Cart)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.CartId)
                .OnDelete(DeleteBehavior.Cascade);
            // ProductId is a reference only (no FK) - Product is in separate ProductsDbContext
            e.HasIndex(x => x.ProductId);
        });

        // Reviews Module Configuration
        modelBuilder.Entity<Review>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ProductId);
            e.HasIndex(x => x.UserId);
            // ProductId and UserId are references only (no FK) - they exist in other DbContexts
        });

        // Support Module Configuration
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

        modelBuilder.Entity<Answer>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Question)
                .WithMany(x => x.Answers)
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed default roles
        SeedRoles(modelBuilder);
    }

    private void SeedRoles(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Code = "GUEST", Name = "Guest", Hierarchy = 0 },
            new Role { Id = 2, Code = "CLIENT", Name = "Client", Hierarchy = 1 },
            new Role { Id = 3, Code = "MANAGER", Name = "Manager", Hierarchy = 2 },
            new Role { Id = 4, Code = "MODERATOR", Name = "Moderator", Hierarchy = 3 },
            new Role { Id = 5, Code = "ADMINISTRATOR", Name = "Administrator", Hierarchy = 4 }
        );
    }
}
