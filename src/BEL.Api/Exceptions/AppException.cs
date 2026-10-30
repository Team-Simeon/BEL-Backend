namespace BEL.Api.Exceptions;

public abstract class AppException(string title, string message, int statusCode) : Exception(message)
{
    public string Title { get; } = title;

    public int StatusCode { get; } = statusCode;
}
