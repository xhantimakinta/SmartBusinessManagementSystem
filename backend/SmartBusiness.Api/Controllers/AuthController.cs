using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SmartBusiness.Api.Contracts;
using SmartBusiness.Api.Services;

namespace SmartBusiness.Api.Controllers;

[ApiController, Route("api/auth")]
 [EnableRateLimiting("auth")]
public sealed class AuthController(IAuthService service) : ControllerBase
{
    /// <summary>Registers a user and returns a JWT.</summary>
    [AllowAnonymous, HttpPost("register"), ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken) => StatusCode(StatusCodes.Status201Created, await service.RegisterAsync(request, cancellationToken));

    /// <summary>Authenticates a user and returns a JWT.</summary>
    [AllowAnonymous, HttpPost("login"), ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken) => Ok(await service.LoginAsync(request, cancellationToken));
}