using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SmartBusiness.Api.Contracts;
using SmartBusiness.Domain.Entities;
using SmartBusiness.Infrastructure.Persistence;

namespace SmartBusiness.Api.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}

public interface ICustomerService
{
    Task<PagedResponse<CustomerResponse>> GetAsync(PaginationRequest request, CancellationToken cancellationToken);
    Task<CustomerResponse> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<CustomerResponse> CreateAsync(CustomerRequest request, CancellationToken cancellationToken);
    Task<CustomerResponse> UpdateAsync(int id, CustomerRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
    Task<CustomerResponse> GetOwnAsync(string email, CancellationToken cancellationToken);
    Task<CustomerResponse> UpdateOwnAsync(string email, CustomerRequest request, CancellationToken cancellationToken);
}

public interface ICategoryService
{
    Task<PagedResponse<CategoryResponse>> GetAsync(PaginationRequest request, CancellationToken cancellationToken);
    Task<CategoryResponse> CreateAsync(CategoryRequest request, CancellationToken cancellationToken);
    Task<CategoryResponse> UpdateAsync(int id, CategoryRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}

public interface IProductService
{
    Task<PagedResponse<ProductResponse>> GetAsync(PaginationRequest request, CancellationToken cancellationToken);
    Task<ProductResponse> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<ProductResponse> CreateAsync(ProductRequest request, CancellationToken cancellationToken);
    Task<ProductResponse> UpdateAsync(int id, ProductRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}

public interface IOrderService
{
    Task<PagedResponse<OrderResponse>> GetAsync(PaginationRequest request, CancellationToken cancellationToken);
    Task<OrderResponse> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<OrderResponse> CreateAsync(OrderRequest request, string? customerEmail, CancellationToken cancellationToken);
    Task<OrderResponse> UpdateStatusAsync(int id, OrderStatusRequest request, CancellationToken cancellationToken);
    Task<PagedResponse<OrderResponse>> GetOwnAsync(string email, PaginationRequest request, CancellationToken cancellationToken);
}

public interface IDashboardService
{
    Task<DashboardSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken);
    Task<SalesReportResponse> GetSalesAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken);
    Task<SalesBreakdownResponse> GetSalesBreakdownAsync(DateTime? from, DateTime? to, string? groupBy, CancellationToken cancellationToken);
}

public sealed class AuthService(SmartBusinessDbContext db, IConfiguration configuration, ILogger<AuthService> logger) : IAuthService
{
    private readonly PasswordHasher<User> passwordHasher = new();

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(user => user.Email == email, cancellationToken))
            throw new ConflictException("A user with this email already exists.");

        var user = new User { Name = request.Name.Trim(), Email = email, Role = UserRole.Customer, CreatedAt = DateTime.UtcNow };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        db.Users.Add(user);
        db.Customers.Add(new Customer { Name = user.Name, Email = email, Phone = string.Empty, Address = string.Empty, CreatedAt = user.CreatedAt });
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Registered user {UserId} with role {Role}", user.Id, user.Role);
        return ToResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(item => item.Email == request.Email.Trim().ToLower(), cancellationToken);
        if (user is null || passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
            throw new BusinessValidationException("Invalid email or password.");
        return ToResponse(user);
    }

    private AuthResponse ToResponse(User user)
    {
        var claims = new[] { new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()), new Claim(JwtRegisteredClaimNames.Email, user.Email), new Claim(ClaimTypes.Email, user.Email), new Claim(ClaimTypes.Name, user.Name), new Claim(ClaimTypes.Role, user.Role.ToString()) };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(configuration["Jwt:Issuer"], configuration["Jwt:Audience"], claims,
            expires: DateTime.UtcNow.AddHours(8), signingCredentials: credentials);
        return new AuthResponse(user.Id, user.Name, user.Email, user.Role.ToString(), new JwtSecurityTokenHandler().WriteToken(token));
    }
}

public sealed class CustomerService(SmartBusinessDbContext db) : ICustomerService
{
    public async Task<PagedResponse<CustomerResponse>> GetAsync(PaginationRequest request, CancellationToken cancellationToken)
    {
        var query = db.Customers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Search)) query = query.Where(x => x.Name.Contains(request.Search) || x.Email.Contains(request.Search) || x.Phone.Contains(request.Search));
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Name).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).Select(ToResponseExpression).ToListAsync(cancellationToken);
        return new(items, request.Page, request.PageSize, total);
    }
    public async Task<CustomerResponse> GetByIdAsync(int id, CancellationToken cancellationToken) => await db.Customers.AsNoTracking().Where(x => x.Id == id).Select(ToResponseExpression).SingleOrDefaultAsync(cancellationToken) ?? throw new NotFoundException("Customer not found.");
    public async Task<CustomerResponse> CreateAsync(CustomerRequest request, CancellationToken cancellationToken) { var item = new Customer { Name = request.Name.Trim(), Email = request.Email.Trim(), Phone = request.Phone.Trim(), Address = request.Address.Trim(), CreatedAt = DateTime.UtcNow }; db.Customers.Add(item); await db.SaveChangesAsync(cancellationToken); return await GetByIdAsync(item.Id, cancellationToken); }
    public async Task<CustomerResponse> UpdateAsync(int id, CustomerRequest request, CancellationToken cancellationToken) { var item = await db.Customers.FindAsync([id], cancellationToken) ?? throw new NotFoundException("Customer not found."); item.Name = request.Name.Trim(); item.Email = request.Email.Trim(); item.Phone = request.Phone.Trim(); item.Address = request.Address.Trim(); await db.SaveChangesAsync(cancellationToken); return await GetByIdAsync(id, cancellationToken); }
    public async Task DeleteAsync(int id, CancellationToken cancellationToken) { var item = await db.Customers.FindAsync([id], cancellationToken) ?? throw new NotFoundException("Customer not found."); if (await db.Orders.AnyAsync(x => x.CustomerId == id, cancellationToken)) throw new ConflictException("Customer cannot be deleted while orders exist."); db.Customers.Remove(item); await db.SaveChangesAsync(cancellationToken); }
    public async Task<CustomerResponse> GetOwnAsync(string email, CancellationToken cancellationToken) => await db.Customers.AsNoTracking().Where(x => x.Email == email).Select(ToResponseExpression).SingleOrDefaultAsync(cancellationToken) ?? throw new NotFoundException("Customer profile not found.");
    public async Task<CustomerResponse> UpdateOwnAsync(string email, CustomerRequest request, CancellationToken cancellationToken) { var item = await db.Customers.SingleOrDefaultAsync(x => x.Email == email, cancellationToken) ?? throw new NotFoundException("Customer profile not found."); item.Name = request.Name.Trim(); item.Phone = request.Phone.Trim(); item.Address = request.Address.Trim(); await db.SaveChangesAsync(cancellationToken); return await GetOwnAsync(email, cancellationToken); }
    private static readonly System.Linq.Expressions.Expression<Func<Customer, CustomerResponse>> ToResponseExpression = x => new(x.Id, x.Name, x.Email, x.Phone, x.Address, x.CreatedAt);
}

public sealed class CategoryService(SmartBusinessDbContext db) : ICategoryService
{
    public async Task<PagedResponse<CategoryResponse>> GetAsync(PaginationRequest request, CancellationToken cancellationToken) { var q = db.Categories.AsNoTracking(); if (!string.IsNullOrWhiteSpace(request.Search)) q = q.Where(x => x.Name.Contains(request.Search)); var total = await q.CountAsync(cancellationToken); var items = await q.OrderBy(x => x.Name).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).Select(x => new CategoryResponse(x.Id, x.Name, x.Description)).ToListAsync(cancellationToken); return new(items, request.Page, request.PageSize, total); }
    public async Task<CategoryResponse> CreateAsync(CategoryRequest request, CancellationToken cancellationToken) { var item = new Category { Name = request.Name.Trim(), Description = request.Description.Trim() }; db.Categories.Add(item); await db.SaveChangesAsync(cancellationToken); return new(item.Id, item.Name, item.Description); }
    public async Task<CategoryResponse> UpdateAsync(int id, CategoryRequest request, CancellationToken cancellationToken) { var item = await db.Categories.FindAsync([id], cancellationToken) ?? throw new NotFoundException("Category not found."); item.Name = request.Name.Trim(); item.Description = request.Description.Trim(); await db.SaveChangesAsync(cancellationToken); return new(item.Id, item.Name, item.Description); }
    public async Task DeleteAsync(int id, CancellationToken cancellationToken) { var item = await db.Categories.FindAsync([id], cancellationToken) ?? throw new NotFoundException("Category not found."); if (await db.Products.AnyAsync(x => x.CategoryId == id, cancellationToken)) throw new ConflictException("Category cannot be deleted while products exist."); db.Categories.Remove(item); await db.SaveChangesAsync(cancellationToken); }
}

public sealed class ProductService(SmartBusinessDbContext db) : IProductService
{
    public async Task<PagedResponse<ProductResponse>> GetAsync(PaginationRequest request, CancellationToken cancellationToken) { IQueryable<Product> q = db.Products.AsNoTracking().Include(x => x.Category); if (!string.IsNullOrWhiteSpace(request.Search)) q = q.Where(x => x.Name.Contains(request.Search) || x.Description.Contains(request.Search)); if (request.CategoryId.HasValue) q = q.Where(x => x.CategoryId == request.CategoryId.Value); var total = await q.CountAsync(cancellationToken); var items = await q.OrderBy(x => x.Name).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).Select(ToResponse).ToListAsync(cancellationToken); return new(items, request.Page, request.PageSize, total); }
    public async Task<ProductResponse> GetByIdAsync(int id, CancellationToken cancellationToken) => await db.Products.AsNoTracking().Include(x => x.Category).Where(x => x.Id == id).Select(ToResponse).SingleOrDefaultAsync(cancellationToken) ?? throw new NotFoundException("Product not found.");
    public async Task<ProductResponse> CreateAsync(ProductRequest request, CancellationToken cancellationToken) { await EnsureCategory(request.CategoryId, cancellationToken); var now = DateTime.UtcNow; var item = new Product { CategoryId = request.CategoryId, Name = request.Name.Trim(), Description = request.Description.Trim(), Price = request.Price, StockQuantity = request.StockQuantity, CreatedAt = now, UpdatedAt = now }; db.Products.Add(item); await db.SaveChangesAsync(cancellationToken); return await GetByIdAsync(item.Id, cancellationToken); }
    public async Task<ProductResponse> UpdateAsync(int id, ProductRequest request, CancellationToken cancellationToken) { await EnsureCategory(request.CategoryId, cancellationToken); var item = await db.Products.FindAsync([id], cancellationToken) ?? throw new NotFoundException("Product not found."); item.CategoryId = request.CategoryId; item.Name = request.Name.Trim(); item.Description = request.Description.Trim(); item.Price = request.Price; item.StockQuantity = request.StockQuantity; item.UpdatedAt = DateTime.UtcNow; await db.SaveChangesAsync(cancellationToken); return await GetByIdAsync(id, cancellationToken); }
    public async Task DeleteAsync(int id, CancellationToken cancellationToken) { var item = await db.Products.FindAsync([id], cancellationToken) ?? throw new NotFoundException("Product not found."); if (await db.OrderItems.AnyAsync(x => x.ProductId == id, cancellationToken)) throw new ConflictException("Product cannot be deleted while order items exist."); db.Products.Remove(item); await db.SaveChangesAsync(cancellationToken); }
    private async Task EnsureCategory(int id, CancellationToken token) { if (!await db.Categories.AnyAsync(x => x.Id == id, token)) throw new BusinessValidationException("Category does not exist."); }
    private static readonly System.Linq.Expressions.Expression<Func<Product, ProductResponse>> ToResponse = x => new(x.Id, x.CategoryId, x.Category.Name, x.Name, x.Description, x.Price, x.StockQuantity, x.CreatedAt, x.UpdatedAt);
}

public sealed class OrderService(SmartBusinessDbContext db, IEmailService emailService, ILogger<OrderService> logger) : IOrderService
{
    public async Task<PagedResponse<OrderResponse>> GetAsync(PaginationRequest request, CancellationToken cancellationToken) { IQueryable<Order> q = db.Orders.AsNoTracking().Include(x => x.Customer).Include(x => x.Items).ThenInclude(x => x.Product); if (!string.IsNullOrWhiteSpace(request.Search)) q = q.Where(x => x.Customer.Name.Contains(request.Search) || x.Status.ToString().Contains(request.Search)); var total = await q.CountAsync(cancellationToken); var orders = await q.OrderByDescending(x => x.OrderDate).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync(cancellationToken); return new(orders.Select(ToResponse).ToList(), request.Page, request.PageSize, total); }
    public async Task<OrderResponse> GetByIdAsync(int id, CancellationToken cancellationToken) { var order = await db.Orders.AsNoTracking().Include(x => x.Customer).Include(x => x.Items).ThenInclude(x => x.Product).SingleOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new NotFoundException("Order not found."); return ToResponse(order); }
    public async Task<OrderResponse> CreateAsync(OrderRequest request, string? customerEmail, CancellationToken cancellationToken) { var customer = customerEmail is null ? await db.Customers.SingleOrDefaultAsync(x => x.Id == request.CustomerId, cancellationToken) : await db.Customers.SingleOrDefaultAsync(x => x.Email == customerEmail, cancellationToken); if (customer is null) throw new BusinessValidationException("Customer does not exist."); if (customerEmail is not null && customer.Id != request.CustomerId) throw new BusinessValidationException("Customers may only create orders for their own account."); if (request.Items.Select(x => x.ProductId).Distinct().Count() != request.Items.Count) throw new BusinessValidationException("Each product may appear only once in an order."); var ids = request.Items.Select(x => x.ProductId).ToList(); var products = await db.Products.Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, cancellationToken); if (products.Count != ids.Count) throw new BusinessValidationException("One or more products do not exist."); var order = new Order { CustomerId = customer.Id, OrderDate = DateTime.UtcNow, Status = OrderStatus.Pending }; foreach (var line in request.Items) { var product = products[line.ProductId]; if (product.StockQuantity < line.Quantity) throw new BusinessValidationException($"Insufficient stock for product {product.Name}."); product.StockQuantity -= line.Quantity; order.Items.Add(new OrderItem { ProductId = product.Id, Quantity = line.Quantity, UnitPrice = product.Price, Subtotal = product.Price * line.Quantity }); } order.TotalAmount = order.Items.Sum(x => x.Subtotal); db.Orders.Add(order); await db.SaveChangesAsync(cancellationToken); await emailService.SendOrderConfirmationAsync(new OrderConfirmationEmail(customer.Email, customer.Name, order.Id, order.TotalAmount, order.OrderDate), CancellationToken.None); logger.LogInformation("Created order {OrderId} for customer {CustomerId}", order.Id, customer.Id); return await GetByIdAsync(order.Id, cancellationToken); }
    public async Task<OrderResponse> UpdateStatusAsync(int id, OrderStatusRequest request, CancellationToken cancellationToken) { var order = await db.Orders.FindAsync([id], cancellationToken) ?? throw new NotFoundException("Order not found."); order.Status = request.Status; await db.SaveChangesAsync(cancellationToken); return await GetByIdAsync(id, cancellationToken); }
    public async Task<PagedResponse<OrderResponse>> GetOwnAsync(string email, PaginationRequest request, CancellationToken cancellationToken) { var customerId = await db.Customers.Where(x => x.Email == email).Select(x => (int?)x.Id).SingleOrDefaultAsync(cancellationToken) ?? throw new NotFoundException("Customer profile not found."); var query = db.Orders.AsNoTracking().Include(x => x.Customer).Include(x => x.Items).ThenInclude(x => x.Product).Where(x => x.CustomerId == customerId); var total = await query.CountAsync(cancellationToken); var orders = await query.OrderByDescending(x => x.OrderDate).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync(cancellationToken); return new(orders.Select(ToResponse).ToList(), request.Page, request.PageSize, total); }
    private static OrderResponse ToResponse(Order x) => new(x.Id, x.CustomerId, x.Customer.Name, x.OrderDate, x.Status, x.TotalAmount, x.Items.Select(i => new OrderItemResponse(i.Id, i.ProductId, i.Product.Name, i.Quantity, i.UnitPrice, i.Subtotal)).ToList());
}

public sealed class DashboardService(SmartBusinessDbContext db) : IDashboardService
{
    public async Task<DashboardSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken) { var recentOrders = await db.Orders.AsNoTracking().Include(x => x.Customer).OrderByDescending(x => x.OrderDate).Take(5).Select(x => new RecentOrderResponse(x.Id, x.Customer.Name, x.OrderDate, x.Status, x.TotalAmount)).ToListAsync(cancellationToken); return new(await db.Customers.CountAsync(cancellationToken), await db.Products.CountAsync(cancellationToken), await db.Orders.CountAsync(x => x.Status == OrderStatus.Pending, cancellationToken), await db.Orders.CountAsync(cancellationToken), await db.Orders.Where(x => x.Status != OrderStatus.Cancelled).SumAsync(x => (decimal?)x.TotalAmount, cancellationToken) ?? 0, await db.Products.CountAsync(x => x.StockQuantity <= 5, cancellationToken), recentOrders); }
    public async Task<SalesReportResponse> GetSalesAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken) { var start = from?.Date ?? DateTime.UtcNow.Date.AddDays(-30); var end = (to?.Date ?? DateTime.UtcNow.Date).AddDays(1); if (end <= start) throw new BusinessValidationException("The report end date must be after the start date."); var q = db.Orders.AsNoTracking().Where(x => x.OrderDate >= start && x.OrderDate < end && x.Status != OrderStatus.Cancelled); var rows = await q.GroupBy(x => x.OrderDate.Date).Select(x => new SalesReportRow(x.Key, x.Count(), x.Sum(y => y.TotalAmount))).OrderBy(x => x.Date).ToListAsync(cancellationToken); return new(start, end.AddDays(-1), rows.Sum(x => x.TotalSales), rows.Sum(x => x.OrderCount), rows); }
    public async Task<SalesBreakdownResponse> GetSalesBreakdownAsync(DateTime? from, DateTime? to, string? groupBy, CancellationToken cancellationToken) { var start = from?.Date ?? DateTime.UtcNow.Date.AddDays(-30); var end = (to?.Date ?? DateTime.UtcNow.Date).AddDays(1); if (end <= start) throw new BusinessValidationException("The report end date must be after the start date."); var key = groupBy?.ToLowerInvariant() switch { "product" => "product", "category" => "category", _ => throw new BusinessValidationException("groupBy must be product or category.") }; var query = db.OrderItems.AsNoTracking().Include(x => x.Order).Include(x => x.Product).ThenInclude(x => x.Category).Where(x => x.Order.OrderDate >= start && x.Order.OrderDate < end && x.Order.Status != OrderStatus.Cancelled); var rows = key == "product" ? await query.GroupBy(x => x.Product.Name).Select(x => new SalesBreakdownRow(x.Key, x.Select(y => y.OrderId).Distinct().Count(), x.Sum(y => y.Subtotal))).OrderByDescending(x => x.TotalSales).ToListAsync(cancellationToken) : await query.GroupBy(x => x.Product.Category.Name).Select(x => new SalesBreakdownRow(x.Key, x.Select(y => y.OrderId).Distinct().Count(), x.Sum(y => y.Subtotal))).OrderByDescending(x => x.TotalSales).ToListAsync(cancellationToken); return new(start, end.AddDays(-1), key, rows); }
}