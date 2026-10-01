using System.Text;
using System.Reflection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using SmartBusiness.Api.Middleware;
using SmartBusiness.Api.Configuration;
using SmartBusiness.Api.Services;
using SmartBusiness.Application;
using SmartBusiness.Infrastructure;
using SmartBusiness.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
    throw new InvalidOperationException("Jwt:Key must be configured through user secrets or environment variables and contain at least 32 characters.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
if (string.IsNullOrWhiteSpace(jwtIssuer) || string.IsNullOrWhiteSpace(jwtAudience))
    throw new InvalidOperationException("Jwt:Issuer and Jwt:Audience must be configured.");

builder.Services.AddControllers();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("auth", limiter =>
    {
        limiter.PermitLimit = 10;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
        limiter.AutoReplenishment = true;
    });
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});
builder.Services.Configure<ExchangeRateSettings>(builder.Configuration.GetSection(ExchangeRateSettings.SectionName));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection(EmailSettings.SectionName));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SmartBusiness API",
        Version = "v1",
        Description = "API foundation for the SmartBusiness platform."
    });

    var bearerScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter a valid JWT bearer token."
    };

    options.AddSecurityDefinition("Bearer", bearerScheme);

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document, null!),
            []
        }
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("Management", policy => policy.RequireRole("Admin", "Manager"));
    options.AddPolicy("CustomerOperations", policy => policy.RequireRole("Admin", "Manager", "Employee"));
    options.AddPolicy("EmployeeOperations", policy => policy.RequireRole("Admin", "Manager", "Employee"));
});
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiServices();
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.WithOrigins((builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
            .Where(origin => Uri.TryCreate(origin, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" && !origin.Contains('*', StringComparison.Ordinal))
            .ToArray())
        .AllowAnyHeader()
        .AllowAnyMethod()));
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (!app.Environment.IsEnvironment("DesignTime"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<SmartBusinessDbContext>();
    if (dbContext.Database.GetMigrations().Any())
        dbContext.Database.Migrate();
    else
        dbContext.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

var frontendIndex = Path.Combine(app.Environment.WebRootPath ?? string.Empty, "index.html");
if (File.Exists(frontendIndex))
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }
