using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBusiness.Api.Contracts;
using SmartBusiness.Api.Services;

namespace SmartBusiness.Api.Controllers;

[Authorize, ApiController, Route("api/orders")]
public sealed class OrdersController(IOrderService service) : ControllerBase
{
    /// <summary>Lists orders with pagination and optional customer/status search.</summary>
    [Authorize(Policy = "EmployeeOperations"), HttpGet] public async Task<ActionResult<PagedResponse<OrderResponse>>> Get(PaginationRequest request, CancellationToken token) => Ok(await service.GetAsync(request, token));
    /// <summary>Gets an order including its lines.</summary>
    [Authorize(Policy = "EmployeeOperations"), HttpGet("{id:int}")] public async Task<ActionResult<OrderResponse>> GetById(int id, CancellationToken token) => Ok(await service.GetByIdAsync(id, token));
    /// <summary>Creates an order; prices and totals are calculated from the database.</summary>
    [Authorize(Roles = "Admin,Manager,Employee,Customer"), HttpPost] public async Task<ActionResult<OrderResponse>> Create(OrderRequest request, CancellationToken token) { var result = await service.CreateAsync(request, User.IsInRole("Customer") ? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value : null, token); return CreatedAtAction(nameof(GetById), new { id = result.Id }, result); }
    /// <summary>Updates an order status.</summary>
    [Authorize(Policy = "EmployeeOperations"), HttpPut("{id:int}/status")] public async Task<ActionResult<OrderResponse>> UpdateStatus(int id, OrderStatusRequest request, CancellationToken token) => Ok(await service.UpdateStatusAsync(id, request, token));

    /// <summary>Lists orders belonging to the authenticated customer.</summary>
    [Authorize(Roles = "Customer"), HttpGet("mine")]
    public async Task<ActionResult<PagedResponse<OrderResponse>>> GetOwn(PaginationRequest request, CancellationToken token) => Ok(await service.GetOwnAsync(User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? throw new UnauthorizedAccessException(), request, token));
}