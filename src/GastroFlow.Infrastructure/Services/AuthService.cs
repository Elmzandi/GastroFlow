using GastroFlow.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GastroFlow.Infrastructure.Services;

public sealed class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IJwtService _jwt;
    private readonly JwtOptions _jwtOptions;

    public AuthService(AppDbContext db, IJwtService jwt, IOptions<JwtOptions> jwtOptions)
    {
        _db = db;
        _jwt = jwt;
        _jwtOptions = jwtOptions.Value;
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

        var refreshToken = CreateRefreshToken(user);

        _db.Restaurants.Add(restaurant);
        _db.Users.Add(user);
        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync(ct);

        var accessToken = _jwt.GenerateToken(user);

        return BuildAuthResponse(accessToken, refreshToken.Token, user);
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

        var refreshToken = CreateRefreshToken(user);
        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync(ct);

        var accessToken = _jwt.GenerateToken(user);

        return BuildAuthResponse(accessToken, refreshToken.Token, user);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        // IgnoreQueryFilters: no tenant context exists before the new JWT is issued.
        var existing = await _db.RefreshTokens
            .IgnoreQueryFilters()
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, ct);

        if (existing is null || !existing.IsActive)
            throw new InvalidRefreshTokenException();

        // Token rotation: revoke the consumed token and issue a fresh pair.
        existing.IsRevoked = true;

        var newRefreshToken = CreateRefreshToken(existing.User);
        _db.RefreshTokens.Add(newRefreshToken);
        await _db.SaveChangesAsync(ct);

        var accessToken = _jwt.GenerateToken(existing.User);

        return BuildAuthResponse(accessToken, newRefreshToken.Token, existing.User);
    }

    private RefreshToken CreateRefreshToken(User user) => new()
    {
        Token = _jwt.GenerateRefreshToken(),
        ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays),
        UserId = user.Id,
        RestaurantId = user.RestaurantId
    };

    private AuthResponse BuildAuthResponse(string accessToken, string refreshToken, User user) =>
        new(
            accessToken,
            DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationMinutes),
            refreshToken,
            user.Email,
            user.Role,
            user.RestaurantId
        );
}
