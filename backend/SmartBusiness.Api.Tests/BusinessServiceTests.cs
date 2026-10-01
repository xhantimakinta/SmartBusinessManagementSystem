using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using SmartBusiness.Api.Contracts;
using SmartBusiness.Api.Controllers;
using SmartBusiness.Api.Services;
using SmartBusiness.Domain.Entities;
using SmartBusiness.Infrastructure.Persistence;
using Xunit;

namespace SmartBusiness.Api.Tests;

public sealed class BusinessServiceTests
{
    [Fact]
    public async Task Register_creates_customer_with_hashed_password()
    {
        await using var db = CreateDb();
        var service = CreateAuthService(db);

        var response = await service.RegisterAsync(new RegisterRequest { Name = "Taylor Customer", Email = "Taylor@example.com", Password = "correct horse battery staple" }, CancellationToken.None);
        var user = await db.Users.SingleAsync();

        Assert.Equal(UserRole.Customer.ToString(), response.Role);
        Assert.Equal("taylor@example.com", user.Email);
        Assert.NotEqual("correct horse battery staple", user.PasswordHash);
        Assert.StartsWith("AQAAAA", user.PasswordHash);
        Assert.Single(db.Customers);
    }

    [Fact]
    public async Task Register_rejects_duplicate_email_case_insensitively()
    {
        await using var db = CreateDb();
        var service = CreateAuthService(db);
        await service.RegisterAsync(new RegisterRequest { Name = "First User", Email = "same@example.com", Password = "password-one" }, CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() => service.RegisterAsync(new RegisterRequest { Name = "Second User", Email = "SAME@example.com", Password = "password-two" }, CancellationToken.None));
    }

    [Fact]
    public async Task Login_rejects_invalid_credentials_without_disclosing_which_field_failed()
    {
        await using var db = CreateDb();
        var service = CreateAuthService(db);
        await service.RegisterAsync(new RegisterRequest { Name = "Login User", Email = "login@example.com", Password = "correct-password" }, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(() => service.LoginAsync(new LoginRequest { Email = "login@example.com", Password = "wrong-password" }, CancellationToken.None));
        Assert.Equal("Invalid email or password.", exception.Message);
    }

    [Fact]
    public void Protected_endpoints_have_expected_role_policies()
    {
        var productPolicy = typeof(ProductsController).GetCustomAttribute<AuthorizeAttribute>()?.Policy;
        var customerPolicy = typeof(CustomersController).GetMethod(nameof(CustomersController.GetOwn))?.GetCustomAttribute<AuthorizeAttribute>()?.Roles;
        var orderPolicy = typeof(OrdersController).GetMethod(nameof(OrdersController.GetById))?.GetCustomAttribute<AuthorizeAttribute>()?.Policy;
        var ownOrdersRoles = typeof(OrdersController).GetMethod(nameof(OrdersController.GetOwn))?.GetCustomAttribute<AuthorizeAttribute>()?.Roles;

        Assert.Equal("Management", productPolicy);
        Assert.Equal("Customer", customerPolicy);
        Assert.Equal("EmployeeOperations", orderPolicy);
        Assert.Equal("Customer", ownOrdersRoles);
    }

    [Fact]
    public async Task Create_order_calculates_total_and_decreases_stock()
    {
        await using var db = CreateDb();
        var product = new Product { Id = 10, CategoryId = 1, Name = "Keyboard", Description = "Desk keyboard", Price = 25.50m, StockQuantity = 8, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, Category = new Category { Id = 1, Name = "Tech", Description = "Technology" } };
        db.Products.Add(product);
        db.Customers.Add(new Customer { Id = 20, Name = "Buyer", Email = "buyer@example.com", Phone = "1", Address = "Address", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var service = CreateOrderService(db);

        var result = await service.CreateAsync(new OrderRequest { CustomerId = 20, Items = [new OrderItemRequest { ProductId = 10, Quantity = 3 }] }, null, CancellationToken.None);

        Assert.Equal(76.50m, result.TotalAmount);
        Assert.Equal(3, result.Items[0].Quantity);
        Assert.Equal(5, (await db.Products.FindAsync(10))!.StockQuantity);
    }

    [Fact]
    public async Task Create_order_rejects_insufficient_stock_without_mutating_stock()
    {
        await using var db = CreateDb();
        await SeedOrderData(db, stockQuantity: 2);
        var service = CreateOrderService(db);

        await Assert.ThrowsAsync<BusinessValidationException>(() => service.CreateAsync(new OrderRequest { CustomerId = 20, Items = [new OrderItemRequest { ProductId = 10, Quantity = 3 }] }, null, CancellationToken.None));
        Assert.Equal(2, (await db.Products.FindAsync(10))!.StockQuantity);
        Assert.Empty(db.Orders);
    }

    [Fact]
    public async Task Create_order_rejects_unknown_or_deleted_product()
    {
        await using var db = CreateDb();
        await SeedOrderData(db, stockQuantity: 5);
        var service = CreateOrderService(db);

        await Assert.ThrowsAsync<BusinessValidationException>(() => service.CreateAsync(new OrderRequest { CustomerId = 20, Items = [new OrderItemRequest { ProductId = 999, Quantity = 1 }] }, null, CancellationToken.None));
        db.Products.Remove(await db.Products.FindAsync(10) ?? throw new InvalidOperationException());
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<BusinessValidationException>(() => service.CreateAsync(new OrderRequest { CustomerId = 20, Items = [new OrderItemRequest { ProductId = 10, Quantity = 1 }] }, null, CancellationToken.None));
    }

    [Fact]
    public async Task Customer_cannot_create_order_for_another_customer()
    {
        await using var db = CreateDb();
        await SeedOrderData(db, stockQuantity: 5);
        db.Customers.Add(new Customer { Id = 21, Name = "Other", Email = "other@example.com", Phone = "2", Address = "Other address", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var service = CreateOrderService(db);

        await Assert.ThrowsAsync<BusinessValidationException>(() => service.CreateAsync(new OrderRequest { CustomerId = 21, Items = [new OrderItemRequest { ProductId = 10, Quantity = 1 }] }, "buyer@example.com", CancellationToken.None));
    }

    [Fact]
    public async Task Own_orders_only_return_the_authenticated_customers_orders()
    {
        await using var db = CreateDb();
        await SeedOrderData(db, stockQuantity: 5);
        db.Customers.Add(new Customer { Id = 21, Name = "Other", Email = "other@example.com", Phone = "2", Address = "Other address", CreatedAt = DateTime.UtcNow });
        db.Orders.Add(new Order { Id = 100, CustomerId = 20, OrderDate = DateTime.UtcNow, Status = OrderStatus.Pending, TotalAmount = 10m });
        db.Orders.Add(new Order { Id = 101, CustomerId = 21, OrderDate = DateTime.UtcNow, Status = OrderStatus.Pending, TotalAmount = 20m });
        await db.SaveChangesAsync();
        var service = CreateOrderService(db);

        var result = await service.GetOwnAsync("buyer@example.com", new PaginationRequest(), CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal(20, result.Items[0].CustomerId);
    }

    [Fact]
    public void Request_validation_rejects_invalid_order_and_product_values()
    {
        var order = new OrderRequest { CustomerId = 0, Items = [] };
        var product = new ProductRequest { CategoryId = 0, Name = "", Description = "", Price = -1, StockQuantity = -1 };

        Assert.False(IsValid(order));
        Assert.False(IsValid(product));
    }

    private static bool IsValid(object value) => Validator.TryValidateObject(value, new ValidationContext(value), [], validateAllProperties: true);

    private static SmartBusinessDbContext CreateDb() => new(new DbContextOptionsBuilder<SmartBusinessDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static AuthService CreateAuthService(SmartBusinessDbContext db)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Key"] = "test-key-that-is-at-least-32-characters-long", ["Jwt:Issuer"] = "test-issuer", ["Jwt:Audience"] = "test-audience" }).Build();
        return new AuthService(db, configuration, NullLogger<AuthService>.Instance);
    }

    private static OrderService CreateOrderService(SmartBusinessDbContext db) => new(db, new NoopEmailService(), NullLogger<OrderService>.Instance);

    private static async Task SeedOrderData(SmartBusinessDbContext db, int stockQuantity)
    {
        db.Categories.Add(new Category { Id = 1, Name = "Tech", Description = "Technology" });
        db.Products.Add(new Product { Id = 10, CategoryId = 1, Name = "Keyboard", Description = "Desk keyboard", Price = 25.50m, StockQuantity = stockQuantity, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.Customers.Add(new Customer { Id = 20, Name = "Buyer", Email = "buyer@example.com", Phone = "1", Address = "Address", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
    }

    private sealed class NoopEmailService : IEmailService
    {
        public Task SendOrderConfirmationAsync(OrderConfirmationEmail email, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
