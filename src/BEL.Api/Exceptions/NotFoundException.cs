using Microsoft.AspNetCore.Http;

namespace BEL.Api.Exceptions;

public sealed class NotFoundException(string message)
    : AppException("Not Found", message, StatusCodes.Status404NotFound);
