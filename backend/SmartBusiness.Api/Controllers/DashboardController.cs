using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBusiness.Api.Contracts;
using SmartBusiness.Api.Services;

namespace SmartBusiness.Api.Controllers;

[Authorize(Policy = "Management"), ApiController, Route("api/dashboard")]
public sealed class DashboardController(IDashboardService service) : ControllerBase
{
    /// <summary>Returns operational dashboard totals.</summary>
    [HttpGet("summary")] public async Task<ActionResult<DashboardSummaryResponse>> Summary(CancellationToken token) => Ok(await service.GetSummaryAsync(token));
}