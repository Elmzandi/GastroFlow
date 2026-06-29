namespace GastroFlow.Application.Exceptions;

public sealed class EmailAlreadyExistsException(string email)
    : Exception($"Email '{email}' is already registered.");
