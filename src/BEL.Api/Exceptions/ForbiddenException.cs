using Microsoft.AspNetCore.Http;

namespace BEL.Api.Exceptions;

public sealed class ForbiddenException(string message)
    : AppException("Forbidden", message, StatusCodes.Status403Forbidden);
