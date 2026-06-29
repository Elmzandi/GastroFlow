using System.Text;
using GastroFlow.API.Exceptions;
using GastroFlow.Application.Interfaces;
using GastroFlow.Infrastructure.Options;
using GastroFlow.Infrastructure.Persistence;
using GastroFlow.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ────────────────────────────────────────────────────────────────────────────
// TENANT
// Reads the restaurantId claim from the JWT on every request.
// Used by AppDbContext to scope every query to the current tenant automatically.
// ────────────────────────────────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, TenantContext>();

// ────────────────────────────────────────────────────────────────────────────
// OPTIONS
// Binds appsettings "Jwt" section to JwtOptions record.
// ValidateOnStart() crashes the app at startup if any required field is missing
// instead of failing silently on the first request.
// ────────────────────────────────────────────────────────────────────────────
builder.Services
    .AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// ────────────────────────────────────────────────────────────────────────────
// AUTHENTICATION
// Validates every incoming JWT against the same key/issuer/audience used to
// generate it. Without this, [Authorize] on future controllers does nothing.
// ────────────────────────────────────────────────────────────────────────────
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwt["Issuer"],
            ValidAudience            = jwt["Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(
                                           Encoding.UTF8.GetBytes(jwt["Key"]!))
        };
    });

builder.Services.AddAuthorization();

// ────────────────────────────────────────────────────────────────────────────
// SERVICES
// ────────────────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// ────────────────────────────────────────────────────────────────────────────
// EXCEPTION HANDLING
// AuthExceptionHandler maps domain exceptions to the correct HTTP status:
//   EmailAlreadyExistsException  → 409 Conflict
//   InvalidCredentialsException  → 401 Unauthorized
// Unhandled exceptions fall through to the default 500 handler.
// ────────────────────────────────────────────────────────────────────────────
builder.Services.AddExceptionHandler<AuthExceptionHandler>();
builder.Services.AddProblemDetails();

// ────────────────────────────────────────────────────────────────────────────
// DATABASE
// ────────────────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ────────────────────────────────────────────────────────────────────────────
// API + SWAGGER
// ────────────────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ────────────────────────────────────────────────────────────────────────────
// PIPELINE  — order is mandatory:
//   1. UseExceptionHandler  — wraps everything, must be first
//   2. UseAuthentication    — reads the JWT and populates HttpContext.User
//   3. UseAuthorization     — checks [Authorize] against HttpContext.User
//   4. MapControllers       — routes the request to the right action
// ────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "GastroFlow API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger"));
app.Run();
