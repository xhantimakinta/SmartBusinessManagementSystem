using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBusiness.Api.Contracts;
using SmartBusiness.Api.Services;

namespace SmartBusiness.Api.Controllers;

[Authorize(Policy = "Management"), ApiController, Route("api/products")]
public sealed class ProductsController(IProductService service) : ControllerBase
{
    /// <summary>Lists products with pagination and optional search.</summary>
    [HttpGet] public async Task<ActionResult<PagedResponse<ProductResponse>>> Get(PaginationRequest request, CancellationToken token) => Ok(await service.GetAsync(request, token));
    /// <summary>Gets a product by identifier.</summary>
    [HttpGet("{id:int}")] public async Task<ActionResult<ProductResponse>> GetById(int id, CancellationToken token) => Ok(await service.GetByIdAsync(id, token));
    /// <summary>Creates a product.</summary>
    [HttpPost] public async Task<ActionResult<ProductResponse>> Create(ProductRequest request, CancellationToken token) { var result = await service.CreateAsync(request, token); return CreatedAtAction(nameof(GetById), new { id = result.Id }, result); }
    /// <summary>Updates a product.</summary>
    [HttpPut("{id:int}")] public async Task<ActionResult<ProductResponse>> Update(int id, ProductRequest request, CancellationToken token) => Ok(await service.UpdateAsync(id, request, token));
    /// <summary>Deletes a product.</summary>
    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id, CancellationToken token) { await service.DeleteAsync(id, token); return NoContent(); }
}