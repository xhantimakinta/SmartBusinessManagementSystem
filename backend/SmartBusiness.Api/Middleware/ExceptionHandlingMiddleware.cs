using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartBusiness.Api.Services;

namespace SmartBusiness.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (Exception exception)
        {
            var (status, title) = exception switch
            {
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Authentication is required"),
                NotFoundException => (HttpStatusCode.NotFound, "Resource not found"),
                ConflictException => (HttpStatusCode.Conflict, "Request conflicts with existing data"),
                DbUpdateException => (HttpStatusCode.Conflict, "The request conflicts with existing data"),
                ExternalServiceException => (HttpStatusCode.ServiceUnavailable, "An external service is unavailable"),
                BusinessValidationException => (HttpStatusCode.BadRequest, "Request validation failed"),
                _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred")
            };
            if (status == HttpStatusCode.InternalServerError) logger.LogError(exception, "Unhandled API exception for {Path}", context.Request.Path);
            else logger.LogWarning(exception, "API request rejected with {StatusCode} for {Path}", (int)status, context.Request.Path);
            context.Response.StatusCode = (int)status;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { title, status = (int)status, detail = status == HttpStatusCode.InternalServerError ? null : exception.Message }));
        }
    }
}