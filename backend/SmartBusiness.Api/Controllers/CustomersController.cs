using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBusiness.Api.Contracts;
using SmartBusiness.Api.Services;

namespace SmartBusiness.Api.Controllers;

[Authorize, ApiController, Route("api/customers")]
public sealed class CustomersController(ICustomerService service) : ControllerBase
{
    /// <summary>Lists customers with pagination and optional search.</summary>
    [Authorize(Policy = "CustomerOperations"), HttpGet, ProducesResponseType(typeof(PagedResponse<CustomerResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<CustomerResponse>>> Get(PaginationRequest request, CancellationToken token) => Ok(await service.GetAsync(request, token));
    /// <summary>Gets a customer by identifier.</summary>
    [Authorize(Policy = "CustomerOperations"), HttpGet("{id:int}"), ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerResponse>> GetById(int id, CancellationToken token) => Ok(await service.GetByIdAsync(id, token));
    /// <summary>Creates a customer.</summary>
    [Authorize(Policy = "CustomerOperations"), HttpPost, ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<CustomerResponse>> Create(CustomerRequest request, CancellationToken token) { var result = await service.CreateAsync(request, token); return CreatedAtAction(nameof(GetById), new { id = result.Id }, result); }
    /// <summary>Updates a customer.</summary>
    [Authorize(Policy = "CustomerOperations"), HttpPut("{id:int}"), ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerResponse>> Update(int id, CustomerRequest request, CancellationToken token) => Ok(await service.UpdateAsync(id, request, token));
    /// <summary>Deletes a customer.</summary>
    [Authorize(Policy = "CustomerOperations"), HttpDelete("{id:int}"), ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken token) { await service.DeleteAsync(id, token); return NoContent(); }

    /// <summary>Returns the authenticated customer's own profile.</summary>
    [Authorize(Roles = "Customer"), HttpGet("me")]
    public async Task<ActionResult<CustomerResponse>> GetOwn(CancellationToken token) => Ok(await service.GetOwnAsync(User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? throw new UnauthorizedAccessException(), token));

    /// <summary>Updates the authenticated customer's own profile.</summary>
    [Authorize(Roles = "Customer"), HttpPut("me")]
    public async Task<ActionResult<CustomerResponse>> UpdateOwn(CustomerRequest request, CancellationToken token) => Ok(await service.UpdateOwnAsync(User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? throw new UnauthorizedAccessException(), request, token));
}