using System.Net;
using System.Net.Http.Json;
using GastroFlow.Application.DTOs.Auth;
using GastroFlow.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using GastroFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GastroFlow.IntegrationTests.Controllers;

public sealed class AuthControllerTests : IClassFixture<PostgresContainerFixture>, IAsyncLifetime
{
    private readonly GastroFlowFactory _factory;
    private readonly HttpClient _client;

    public AuthControllerTests(PostgresContainerFixture fixture)
    {
        _factory = new GastroFlowFactory(fixture.ConnectionString);
        _client  = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    // Wipes all rows before each test so tests don't share state.
    // TRUNCATE Restaurants CASCADE removes all dependent Users and RefreshTokens too.
    public async Task InitializeAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Restaurants\" CASCADE");
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // REGISTER
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_ValidRequest_Returns201WithToken()
    {
        var request = new RegisterRequest
        {
            FirstName      = "Ali",
            LastName       = "Test",
            Email          = "ali@test.com",
            Password       = "Password123!",
            RestaurantName = "Test Restaurant"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrEmpty(body.AccessToken));
        Assert.Equal("ali@test.com", body.Email);
        Assert.Equal("Owner", body.Role);
        Assert.NotEqual(Guid.Empty, body.RestaurantId);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var request = new RegisterRequest
        {
            FirstName      = "Ali",
            LastName       = "Test",
            Email          = "dup@test.com",
            Password       = "Password123!",
            RestaurantName = "Restaurant A"
        };

        await _client.PostAsJsonAsync("/api/auth/register", request);
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // LOGIN
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithToken()
    {
        var register = new RegisterRequest
        {
            FirstName      = "Ali",
            LastName       = "Test",
            Email          = "login@test.com",
            Password       = "Password123!",
            RestaurantName = "My Restaurant"
        };
        await _client.PostAsJsonAsync("/api/auth/register", register);

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email    = "login@test.com",
            Password = "Password123!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrEmpty(body.AccessToken));
        Assert.Equal("login@test.com", body.Email);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var register = new RegisterRequest
        {
            FirstName      = "Ali",
            LastName       = "Test",
            Email          = "wrongpw@test.com",
            Password       = "Password123!",
            RestaurantName = "My Restaurant"
        };
        await _client.PostAsJsonAsync("/api/auth/register", register);

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email    = "wrongpw@test.com",
            Password = "WrongPassword!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email    = "nobody@test.com",
            Password = "Password123!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
