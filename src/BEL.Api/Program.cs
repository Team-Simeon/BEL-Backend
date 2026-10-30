using System.Text;
using System.Text.Json.Serialization;
using BEL.Api.Data;
using BEL.Api.Middleware;
using BEL.Api.Models;
using BEL.Api.Security;
using BEL.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

builder.Services.AddDbContext<BelDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));

builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ITokenSessionValidator, TokenSessionValidator>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISchoolService, SchoolService>();

var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();

if (string.IsNullOrWhiteSpace(jwtOptions.Key) || Encoding.UTF8.GetByteCount(jwtOptions.Key) < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key is not configured or is shorter than 32 bytes. Configure it with user secrets or the JWT__KEY environment variable.");
}

if (jwtOptions.ExpiryMinutes <= 0)
{
    throw new InvalidOperationException("Jwt:ExpiryMinutes must be greater than zero.");
}

builder.Services.Configure<JwtOptions>(jwtSection);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = System.Security.Claims.ClaimTypes.Name,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var validator = context.HttpContext.RequestServices.GetRequiredService<ITokenSessionValidator>();
                var failure = await validator.ValidateAsync(context.Principal!, context.HttpContext.RequestAborted);

                if (failure is not null)
                {
                    context.Fail(failure);
                }
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.AdminOnly,
        policy => policy.RequireRole(nameof(UserRole.Admin)));

    options.AddPolicy(AuthorizationPolicies.TeacherOrAdmin,
        policy => policy.RequireRole(nameof(UserRole.Teacher), nameof(UserRole.Admin)));
});

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "BEL API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseExceptionHandler();
app.UseStatusCodePages();

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await DataSeeder.SeedAdminAsync(app.Services, app.Configuration, app.Logger);

app.Run();

public partial class Program;
