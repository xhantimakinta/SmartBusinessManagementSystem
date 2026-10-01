using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBusiness.Api.Contracts;
using SmartBusiness.Api.Services;

namespace SmartBusiness.Api.Controllers;

[Authorize(Policy = "Management"), ApiController, Route("api/categories")]
public sealed class CategoriesController(ICategoryService service) : ControllerBase
{
    /// <summary>Lists categories with pagination and optional search.</summary>
    [HttpGet] public async Task<ActionResult<PagedResponse<CategoryResponse>>> Get(PaginationRequest request, CancellationToken token) => Ok(await service.GetAsync(request, token));
    /// <summary>Creates a category.</summary>
    [HttpPost] public async Task<ActionResult<CategoryResponse>> Create(CategoryRequest request, CancellationToken token) { var result = await service.CreateAsync(request, token); return Created($"api/categories/{result.Id}", result); }
    /// <summary>Updates a category.</summary>
    [HttpPut("{id:int}")] public async Task<ActionResult<CategoryResponse>> Update(int id, CategoryRequest request, CancellationToken token) => Ok(await service.UpdateAsync(id, request, token));
    /// <summary>Deletes a category.</summary>
    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id, CancellationToken token) { await service.DeleteAsync(id, token); return NoContent(); }
}