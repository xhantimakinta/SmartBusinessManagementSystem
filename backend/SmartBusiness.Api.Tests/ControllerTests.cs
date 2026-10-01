using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using SmartBusiness.Api.Contracts;
using SmartBusiness.Api.Controllers;
using SmartBusiness.Api.Middleware;
using SmartBusiness.Api.Services;
using SmartBusiness.Domain.Entities;
using Xunit;

namespace SmartBusiness.Api.Tests;

public sealed class ControllerTests
{
    [Fact]
    public async Task Register_controller_returns_created_response_without_password_data()
    {
        var controller = new AuthController(new FakeAuthService());

        var result = await controller.Register(new RegisterRequest { Name = "New Customer", Email = "new@example.com", Password = "password" }, CancellationToken.None);

        var created = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        Assert.IsType<AuthResponse>(created.Value);
        Assert.DoesNotContain("PasswordHash", JsonSerializer.Serialize(created.Value));
    }

    [Fact]
    public async Task Exception_middleware_returns_safe_generic_error_details()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new ExceptionHandlingMiddleware(_ => throw new InvalidOperationException("database password leaked"), NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.DoesNotContain("database password leaked", body);
        Assert.Contains("unexpected error occurred", body, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FakeAuthService : IAuthService
    {
        public Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken) => Task.FromResult(new AuthResponse(1, request.Name, request.Email, UserRole.Customer.ToString(), "test-token"));
        public Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken) => Task.FromResult(new AuthResponse(1, "Customer", request.Email, UserRole.Customer.ToString(), "test-token"));
    }
}
