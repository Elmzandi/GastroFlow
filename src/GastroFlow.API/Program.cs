using GastroFlow.API.Exceptions;
using GastroFlow.Application.Interfaces;
using GastroFlow.Infrastructure.Options;
using GastroFlow.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

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
// Binds appsettings.json "Jwt" section to JwtOptions record.
// ValidateOnStart() crashes the app immediately if any required field is missing
// instead of failing silently on the first request.
// ────────────────────────────────────────────────────────────────────────────
builder.Services
    .AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

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
// Any unhandled exception falls through to the default 500 handler.
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
// PIPELINE
// Order matters:
//   1. UseExceptionHandler — catches everything, must be first
//   2. UseHttpsRedirection
//   3. MapControllers
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
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger"));
app.Run();
