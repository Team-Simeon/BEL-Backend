using Microsoft.AspNetCore.Http;

namespace BEL.Api.Exceptions;

public sealed class BadRequestException(string message)
    : AppException("Bad Request", message, StatusCodes.Status400BadRequest);
