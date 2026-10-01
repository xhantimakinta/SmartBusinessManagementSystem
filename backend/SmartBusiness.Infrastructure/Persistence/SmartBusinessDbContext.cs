using Microsoft.EntityFrameworkCore;
using SmartBusiness.Domain.Entities;

namespace SmartBusiness.Infrastructure.Persistence;

public sealed class SmartBusinessDbContext(DbContextOptions<SmartBusinessDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Name).HasMaxLength(120).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(320).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(user => user.Role).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(user => user.CreatedAt).HasPrecision(0).IsRequired();
            entity.HasIndex(user => user.Email).IsUnique();
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(customer => customer.Id);
            entity.Property(customer => customer.Name).HasMaxLength(120).IsRequired();
            entity.Property(customer => customer.Email).HasMaxLength(320).IsRequired();
            entity.Property(customer => customer.Phone).HasMaxLength(40).IsRequired();
            entity.Property(customer => customer.Address).HasMaxLength(500).IsRequired();
            entity.Property(customer => customer.CreatedAt).HasPrecision(0).IsRequired();
            entity.HasIndex(customer => customer.Email);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(category => category.Id);
            entity.Property(category => category.Name).HasMaxLength(100).IsRequired();
            entity.Property(category => category.Description).HasMaxLength(500).IsRequired();
            entity.HasIndex(category => category.Name).IsUnique();
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(product => product.Id);
            entity.Property(product => product.Name).HasMaxLength(160).IsRequired();
            entity.Property(product => product.Description).HasMaxLength(1000).IsRequired();
            entity.Property(product => product.Price).HasPrecision(18, 2).IsRequired();
            entity.Property(product => product.CreatedAt).HasPrecision(0).IsRequired();
            entity.Property(product => product.UpdatedAt).HasPrecision(0).IsRequired();
            entity.HasIndex(product => product.CategoryId);
            entity.HasIndex(product => product.Name);
            entity.HasCheckConstraint("CK_Products_Price_NonNegative", "[Price] >= 0");
            entity.HasCheckConstraint("CK_Products_StockQuantity_NonNegative", "[StockQuantity] >= 0");
            entity.HasOne(product => product.Category)
                .WithMany(category => category.Products)
                .HasForeignKey(product => product.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(order => order.Id);
            entity.Property(order => order.OrderDate).HasPrecision(0).IsRequired();
            entity.Property(order => order.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(order => order.TotalAmount).HasPrecision(18, 2).IsRequired();
            entity.HasIndex(order => new { order.CustomerId, order.OrderDate });
            entity.HasCheckConstraint("CK_Orders_TotalAmount_NonNegative", "[TotalAmount] >= 0");
            entity.HasOne(order => order.Customer)
                .WithMany(customer => customer.Orders)
                .HasForeignKey(order => order.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.UnitPrice).HasPrecision(18, 2).IsRequired();
            entity.Property(item => item.Subtotal).HasPrecision(18, 2).IsRequired();
            entity.HasIndex(item => new { item.OrderId, item.ProductId }).IsUnique();
            entity.HasCheckConstraint("CK_OrderItems_Quantity_Positive", "[Quantity] > 0");
            entity.HasCheckConstraint("CK_OrderItems_UnitPrice_NonNegative", "[UnitPrice] >= 0");
            entity.HasCheckConstraint("CK_OrderItems_Subtotal_NonNegative", "[Subtotal] >= 0");
            entity.HasOne(item => item.Order)
                .WithMany(order => order.Items)
                .HasForeignKey(item => item.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Product)
                .WithMany(product => product.OrderItems)
                .HasForeignKey(item => item.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        SeedDevelopmentData(modelBuilder);
    }

    private static void SeedDevelopmentData(ModelBuilder modelBuilder)
    {
        var createdAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<User>().HasData(new User
        {
            Id = 1,
            Name = "Development Admin",
            Email = "admin@smartbusiness.local",
            PasswordHash = "sha256:7f5c7c7f8e9a7d4a1ce6c9e5d65a70c4b39f6a8f9b8e7d2c6a5e4f3d2c1b0a9",
            Role = UserRole.Admin,
            CreatedAt = createdAt
        });

        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Office Supplies", Description = "Everyday supplies for business operations." },
            new Category { Id = 2, Name = "Technology", Description = "Hardware and technology products." });

        modelBuilder.Entity<Customer>().HasData(new Customer
        {
            Id = 1,
            Name = "Development Customer",
            Email = "customer@smartbusiness.local",
            Phone = "+1 555 0100",
            Address = "100 Main Street",
            CreatedAt = createdAt
        });

        modelBuilder.Entity<Product>().HasData(
            new Product
            {
                Id = 1, CategoryId = 1, Name = "Notebook", Description = "Hardcover business notebook.",
                Price = 12.50m, StockQuantity = 100, CreatedAt = createdAt, UpdatedAt = createdAt
            },
            new Product
            {
                Id = 2, CategoryId = 2, Name = "Wireless Keyboard", Description = "Compact wireless keyboard.",
                Price = 49.99m, StockQuantity = 25, CreatedAt = createdAt, UpdatedAt = createdAt
            });
    }
}