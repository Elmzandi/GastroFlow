using Microsoft.EntityFrameworkCore;

namespace GastroFlow.Infrastructure.Services;

public sealed class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IJwtService _jwt;

    public AuthService(AppDbContext db, IJwtService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        // IgnoreQueryFilters: at registration there is no tenant JWT yet,
        // so the global tenant filter would resolve to Guid.Empty and miss existing emails.
        var emailTaken = await _db.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == request.Email, ct);

        if (emailTaken)
            throw new EmailAlreadyExistsException(request.Email);

        var restaurant = new Restaurant
        {
            Id = Guid.NewGuid(),
            Name = request.RestaurantName
        };

        var user = new User
        {
            RestaurantId = restaurant.Id,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "Owner"
        };

        _db.Restaurants.Add(restaurant);
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        var token = _jwt.GenerateToken(user);

        return new AuthResponse(token, user.Email, user.Role, user.RestaurantId);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        // IgnoreQueryFilters: same reason — no tenant context exists before the JWT is issued.
        var user = await _db.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == request.Email, ct);

        // All three checks use the same exception to avoid leaking whether
        // the email exists, the password is wrong, or the account is disabled.
        if (user is null || !user.IsActive || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new InvalidCredentialsException();

        var token = _jwt.GenerateToken(user);

        return new AuthResponse(token, user.Email, user.Role, user.RestaurantId);
    }
}
