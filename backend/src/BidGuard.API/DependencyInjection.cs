using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using BidGuard.API.Infrastructure;
using BidGuard.API.Services;
using BidGuard.Application.Common.Interfaces;
using BidGuard.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace BidGuard.API;

public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers()
            .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddJwtAuthentication(configuration);
        services.AddAuthorizationPolicies();
        services.AddSwaggerWithAuth();
        services.AddAuthRateLimiting();

        // Behind the web container's nginx, trust X-Forwarded-For so rate limits apply per client, not per proxy.
        // Opt-in only: when the API is reachable directly, clients could otherwise spoof the header.
        if (configuration.GetValue<bool>("ForwardedHeaders:Enabled"))
            services.Configure<ForwardedHeadersOptions>(o =>
            {
                o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                o.KnownNetworks.Clear();
                o.KnownProxies.Clear();
            });

        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));

        return services;
    }

    private static void AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false; // keep user_id / email / role claim names as issued
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = BidGuardClaims.Email,
                    RoleClaimType = BidGuardClaims.Role,
                };
            });
    }

    private static void AddAuthorizationPolicies(this IServiceCollection services) =>
        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.AdminOnly, p => p.RequireRole("Admin"))
            .AddPolicy(Policies.OfficerOrAdmin, p => p.RequireRole("Officer", "Admin"))
            .AddPolicy(Policies.BidderOnly, p => p.RequireRole("Bidder"));

    private static void AddSwaggerWithAuth(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "BidGuard AI API",
                Version = "v1",
                Description = "AI-powered bid compliance verification for GeM procurement (SIH26100). "
                              + "Login via /api/auth/login, then click Authorize and paste the access token.",
            });

            var scheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "JWT access token (without the 'Bearer ' prefix).",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = JwtBearerDefaults.AuthenticationScheme },
            };
            options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, scheme);
            options.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = [] });
            options.SupportNonNullableReferenceTypes();
            options.CustomSchemaIds(t => t.FullName!.Replace('+', '.'));
        });
    }

    private static void AddAuthRateLimiting(this IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(Policies.AuthRateLimit, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1) }));
        });
}

public static class Policies
{
    public const string AdminOnly = nameof(AdminOnly);
    public const string OfficerOrAdmin = nameof(OfficerOrAdmin);
    public const string BidderOnly = nameof(BidderOnly);
    public const string AuthRateLimit = nameof(AuthRateLimit);
}
