using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SmartBusiness.Api.Configuration;

namespace SmartBusiness.Api.Services;

public static class ServiceRegistration
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddMemoryCache();
        services.AddOptions<ExchangeRateSettings>();
        services.AddOptions<EmailSettings>();
        services.AddHttpClient("ExchangeRates", (serviceProvider, client) =>
        {
            var settings = serviceProvider.GetRequiredService<IOptions<ExchangeRateSettings>>().Value;
            client.BaseAddress = new Uri(settings.BaseUrl);
        });
        services.AddHttpClient("EmailProvider", (serviceProvider, client) =>
        {
            var settings = serviceProvider.GetRequiredService<IOptions<EmailSettings>>().Value;
            client.BaseAddress = new Uri(settings.BaseUrl);
        });
        services.AddScoped<IExchangeRateService, ExchangeRateService>();
        services.AddScoped<IEmailService, SendGridEmailService>();
        return services;
    }
}