using Microsoft.AspNetCore.Http;

namespace BEL.Api.Exceptions;

public sealed class UnauthorizedException(string message)
    : AppException("Unauthorized", message, StatusCodes.Status401Unauthorized);
