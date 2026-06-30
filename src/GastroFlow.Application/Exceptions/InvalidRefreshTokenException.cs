namespace GastroFlow.Application.Exceptions;

public sealed class InvalidRefreshTokenException()
    : Exception("The refresh token is invalid or has expired.");
