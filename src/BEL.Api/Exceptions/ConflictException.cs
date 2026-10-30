using Microsoft.AspNetCore.Http;

namespace BEL.Api.Exceptions;

public sealed class ConflictException(string message)
    : AppException("Conflict", message, StatusCodes.Status409Conflict);
