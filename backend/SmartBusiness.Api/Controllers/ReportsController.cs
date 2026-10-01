using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBusiness.Api.Contracts;
using SmartBusiness.Api.Services;

namespace SmartBusiness.Api.Controllers;

[Authorize(Policy = "Management"), ApiController, Route("api/reports")]
public sealed class ReportsController(IDashboardService service) : ControllerBase
{
    /// <summary>Returns sales totals grouped by day for a date range.</summary>
    [HttpGet("sales")] public async Task<ActionResult<SalesReportResponse>> Sales([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken token) => Ok(await service.GetSalesAsync(from, to, token));
    /// <summary>Returns sales grouped by product or category.</summary>
    [HttpGet("sales/breakdown")] public async Task<ActionResult<SalesBreakdownResponse>> Breakdown([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string groupBy, CancellationToken token) => Ok(await service.GetSalesBreakdownAsync(from, to, groupBy, token));
}