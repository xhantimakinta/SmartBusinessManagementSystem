namespace SmartBusiness.Api.Services;

public sealed class NotFoundException(string message) : Exception(message);
public sealed class ConflictException(string message) : Exception(message);
public sealed class BusinessValidationException(string message) : Exception(message);