using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SmartBusiness.Api.Configuration;

namespace SmartBusiness.Api.Services;

public sealed record ExchangeRateResult(string BaseCurrency, string TargetCurrency, decimal Rate, DateTime RetrievedAt);

public interface IExchangeRateService
{
    Task<ExchangeRateResult> GetRateAsync(string baseCurrency, string targetCurrency, CancellationToken cancellationToken);
}

public interface IEmailService
{
    Task SendOrderConfirmationAsync(OrderConfirmationEmail email, CancellationToken cancellationToken);
}

public sealed record OrderConfirmationEmail(string RecipientEmail, string RecipientName, int OrderId, decimal TotalAmount, DateTime OrderDate);

public sealed class ExchangeRateService(
    IHttpClientFactory httpClientFactory,
    IMemoryCache cache,
    IOptions<ExchangeRateSettings> options,
    ILogger<ExchangeRateService> logger) : IExchangeRateService
{
    private readonly ExchangeRateSettings settings = options.Value;

    public async Task<ExchangeRateResult> GetRateAsync(string baseCurrency, string targetCurrency, CancellationToken cancellationToken)
    {
        var baseCode = NormalizeCurrency(baseCurrency);
        var targetCode = NormalizeCurrency(targetCurrency);
        if (baseCode == targetCode) return new(baseCode, targetCode, 1m, DateTime.UtcNow);

        var cacheKey = $"exchange-rate:{baseCode}:{targetCode}";
        if (cache.TryGetValue(cacheKey, out ExchangeRateResult? cached) && cached is not null) return cached;
        if (string.IsNullOrWhiteSpace(settings.ApiKey)) throw new ExternalServiceException("Exchange-rate provider is not configured.");

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, settings.TimeoutSeconds)));
        try
        {
            var client = httpClientFactory.CreateClient("ExchangeRates");
            using var response = await client.GetAsync($"{settings.ApiKey}/latest/{baseCode}", timeout.Token);
            if (!response.IsSuccessStatusCode) throw new ExternalServiceException($"Exchange-rate provider returned {(int)response.StatusCode}.");
            var payload = await response.Content.ReadFromJsonAsync<ExchangeRateResponse>(timeout.Token);
            if (payload?.Result != "success" || payload.ConversionRates is null || !payload.ConversionRates.TryGetValue(targetCode, out var rate)) throw new ExternalServiceException("Exchange-rate provider returned an invalid response.");
            var result = new ExchangeRateResult(baseCode, targetCode, rate, DateTime.UtcNow);
            cache.Set(cacheKey, result, TimeSpan.FromMinutes(Math.Max(1, settings.CacheMinutes)));
            return result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Exchange-rate request timed out for {BaseCurrency} to {TargetCurrency}", baseCode, targetCode);
            throw new ExternalServiceException("Exchange-rate provider timed out.");
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Exchange-rate request failed for {BaseCurrency} to {TargetCurrency}", baseCode, targetCode);
            throw new ExternalServiceException("Exchange-rate provider is unavailable.");
        }
    }

    private static string NormalizeCurrency(string value) => string.IsNullOrWhiteSpace(value) || value.Trim().Length != 3 ? throw new BusinessValidationException("Currency codes must contain exactly three letters.") : value.Trim().ToUpperInvariant();

    private sealed class ExchangeRateResponse
    {
        [JsonPropertyName("result")] public string? Result { get; init; }
        [JsonPropertyName("conversion_rates")] public Dictionary<string, decimal>? ConversionRates { get; init; }
    }
}

public sealed class SendGridEmailService(
    IHttpClientFactory httpClientFactory,
    IOptions<EmailSettings> options,
    ILogger<SendGridEmailService> logger) : IEmailService
{
    private readonly EmailSettings settings = options.Value;

    public async Task SendOrderConfirmationAsync(OrderConfirmationEmail email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey) || string.IsNullOrWhiteSpace(settings.FromEmail))
        {
            logger.LogWarning("Order confirmation email skipped because the email provider is not configured for order {OrderId}", email.OrderId);
            return;
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, settings.TimeoutSeconds)));
        try
        {
            var client = httpClientFactory.CreateClient("EmailProvider");
            using var request = new HttpRequestMessage(HttpMethod.Post, "mail/send")
            {
                Content = JsonContent.Create(new
                {
                    personalizations = new[] { new { to = new[] { new { email.RecipientEmail, name = email.RecipientName } } } },
                    from = new { email = settings.FromEmail, name = settings.FromName },
                    subject = $"SmartBusiness order #{email.OrderId} confirmed",
                    content = new[] { new { type = "text/plain", value = $"Hello {email.RecipientName}, your order #{email.OrderId} was received on {email.OrderDate:u}. Total: {email.TotalAmount:C}." } }
                })
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
            using var response = await client.SendAsync(request, timeout.Token);
            if (!response.IsSuccessStatusCode) logger.LogWarning("Email provider returned {StatusCode} for order {OrderId}", (int)response.StatusCode, email.OrderId);
            else logger.LogInformation("Order confirmation email sent for order {OrderId}", email.OrderId);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Order confirmation email timed out for order {OrderId}", email.OrderId);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Order confirmation email failed for order {OrderId}", email.OrderId);
        }
    }
}

public sealed class ExternalServiceException(string message) : Exception(message);