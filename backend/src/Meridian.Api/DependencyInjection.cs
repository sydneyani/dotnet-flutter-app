using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Meridian.Api.Errors;
using Meridian.Api.OpenApi;
using Meridian.Api.Security;
using Meridian.Application.Abstractions;
using Meridian.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace Meridian.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddMeridianApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddExceptionHandler<ApiExceptionHandler>();
        services.AddProblemDetails();

        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
        services.AddEndpointsApiExplorer();
        services.AddHealthChecks()
            .AddDbContextCheck<MeridianDbContext>("database", tags: ["ready"]);

        var signingKey = configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey is required.");
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = configuration["Jwt:Audience"],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    RoleClaimType = ClaimTypesRole()
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                Policies.CanManageAssets,
                policy => policy.RequireRole(nameof(Domain.UserRole.Supervisor), nameof(Domain.UserRole.Admin)));
        });

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("auth", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 30,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
        });

        services.AddCors(options =>
        {
            options.AddPolicy("client", policy =>
            {
                policy.SetIsOriginAllowed(origin =>
                    Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                    && (uri.Host is "localhost" or "127.0.0.1"))
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Meridian Field Operations API",
                Version = "v1",
                Description = "Equipment maintenance and work-order API for energy field crews. Authenticate with POST /api/auth/login, then send the access token as a Bearer token."
            });
            options.SchemaFilter<StringEnumSchemaFilter>();
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Access token from POST /api/auth/login."
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }

    private static string ClaimTypesRole() => System.Security.Claims.ClaimTypes.Role;
}

public static class Policies
{
    public const string CanManageAssets = "CanManageAssets";
}
