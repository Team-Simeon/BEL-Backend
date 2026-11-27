using Microsoft.AspNetCore.Http;

namespace BEL.Api.Exceptions;

public sealed class DatabaseUnavailableException(string message, Exception? innerException = null)
    : AppException("Database Unavailable", message, StatusCodes.Status503ServiceUnavailable, innerException);
