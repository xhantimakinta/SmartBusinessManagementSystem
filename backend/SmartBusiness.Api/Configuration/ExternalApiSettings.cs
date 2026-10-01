namespace SmartBusiness.Api.Configuration;

public sealed class ExchangeRateSettings
{
    public const string SectionName = "ExternalApis:ExchangeRates";

    public string BaseUrl { get; init; } = "https://v6.exchangerate-api.com/v6/";
    public string ApiKey { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 5;
    public int CacheMinutes { get; init; } = 15;
}

public sealed class EmailSettings
{
    public const string SectionName = "ExternalApis:Email";

    public string BaseUrl { get; init; } = "https://api.sendgrid.com/v3/";
    public string ApiKey { get; init; } = string.Empty;
    public string FromEmail { get; init; } = string.Empty;
    public string FromName { get; init; } = "SmartBusiness";
    public int TimeoutSeconds { get; init; } = 10;
}