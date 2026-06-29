namespace GastroFlow.Application.Interfaces;

public interface IJwtService
{
    string GenerateToken(User user);
}
