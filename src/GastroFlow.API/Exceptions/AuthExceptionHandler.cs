using GastroFlow.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace GastroFlow.API.Exceptions;

public sealed class AuthExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken ct)
    {
        var (statusCode, title) = exception switch
        {
            EmailAlreadyExistsException => (StatusCodes.Status409Conflict, "Email Already Exists"),
            InvalidCredentialsException  => (StatusCodes.Status401Unauthorized, "Invalid Credentials"),
            _                            => (0, string.Empty)
        };

        if (statusCode == 0) return false;

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title  = title,
            Detail = exception.Message
        }, ct);

        return true;
    }
}
