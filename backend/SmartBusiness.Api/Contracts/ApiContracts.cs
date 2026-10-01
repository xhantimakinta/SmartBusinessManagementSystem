using System.ComponentModel.DataAnnotations;
using SmartBusiness.Domain.Entities;

namespace SmartBusiness.Api.Contracts;

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public sealed class PaginationRequest
{
    [Range(1, 1000)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;

    public string? Search { get; init; }

    public int? CategoryId { get; init; }
}

public sealed class RegisterRequest
{
    [Required, StringLength(120, MinimumLength = 2)] public string Name { get; init; } = string.Empty;
    [Required, EmailAddress, StringLength(320)] public string Email { get; init; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 8)] public string Password { get; init; } = string.Empty;
}

public sealed class LoginRequest
{
    [Required, EmailAddress] public string Email { get; init; } = string.Empty;
    [Required] public string Password { get; init; } = string.Empty;
}

public sealed record AuthResponse(int Id, string Name, string Email, string Role, string Token);

public sealed class CustomerRequest
{
    [Required, StringLength(120, MinimumLength = 2)] public string Name { get; init; } = string.Empty;
    [Required, EmailAddress, StringLength(320)] public string Email { get; init; } = string.Empty;
    [Required, StringLength(40)] public string Phone { get; init; } = string.Empty;
    [Required, StringLength(500)] public string Address { get; init; } = string.Empty;
}

public sealed record CustomerResponse(int Id, string Name, string Email, string Phone, string Address, DateTime CreatedAt);

public sealed class CategoryRequest
{
    [Required, StringLength(100, MinimumLength = 2)] public string Name { get; init; } = string.Empty;
    [Required, StringLength(500)] public string Description { get; init; } = string.Empty;
}

public sealed record CategoryResponse(int Id, string Name, string Description);

public sealed class ProductRequest
{
    [Range(1, int.MaxValue)] public int CategoryId { get; init; }
    [Required, StringLength(160, MinimumLength = 2)] public string Name { get; init; } = string.Empty;
    [Required, StringLength(1000)] public string Description { get; init; } = string.Empty;
    [Range(0, double.MaxValue)] public decimal Price { get; init; }
    [Range(0, int.MaxValue)] public int StockQuantity { get; init; }
}

public sealed record ProductResponse(int Id, int CategoryId, string CategoryName, string Name, string Description,
    decimal Price, int StockQuantity, DateTime CreatedAt, DateTime UpdatedAt);

public sealed class OrderItemRequest
{
    [Range(1, int.MaxValue)] public int ProductId { get; init; }
    [Range(1, int.MaxValue)] public int Quantity { get; init; }
}

public sealed class OrderRequest
{
    [Range(1, int.MaxValue)] public int CustomerId { get; init; }
    [Required, MinLength(1)] public IReadOnlyList<OrderItemRequest> Items { get; init; } = [];
}

public sealed class OrderStatusRequest
{
    [EnumDataType(typeof(OrderStatus))] public OrderStatus Status { get; init; }
}

public sealed record OrderItemResponse(int Id, int ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal Subtotal);
public sealed record OrderResponse(int Id, int CustomerId, string CustomerName, DateTime OrderDate, OrderStatus Status,
    decimal TotalAmount, IReadOnlyList<OrderItemResponse> Items);

public sealed record DashboardSummaryResponse(int CustomerCount, int ProductCount, int PendingOrderCount,
    int OrderCount, decimal TotalSales, int LowStockProductCount, IReadOnlyList<RecentOrderResponse> RecentOrders);

public sealed record RecentOrderResponse(int Id, string CustomerName, DateTime OrderDate, OrderStatus Status, decimal TotalAmount);

public sealed record SalesReportResponse(DateTime From, DateTime To, decimal TotalSales, int OrderCount,
    IReadOnlyList<SalesReportRow> Rows);

public sealed record SalesReportRow(DateTime Date, int OrderCount, decimal TotalSales);

public sealed record SalesBreakdownResponse(DateTime From, DateTime To, string GroupBy, IReadOnlyList<SalesBreakdownRow> Rows);

public sealed record SalesBreakdownRow(string Label, int OrderCount, decimal TotalSales);