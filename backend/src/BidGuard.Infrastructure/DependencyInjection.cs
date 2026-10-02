using BidGuard.Application.Common.Interfaces;
using BidGuard.Application.Common.Interfaces.Verification;
using BidGuard.Domain.Interfaces;
using BidGuard.Infrastructure.AI;
using BidGuard.Infrastructure.BackgroundJobs;
using BidGuard.Infrastructure.Caching;
using BidGuard.Infrastructure.ExternalServices.MockGov;
using BidGuard.Infrastructure.Identity;
using BidGuard.Infrastructure.Persistence;
using BidGuard.Infrastructure.Persistence.Repositories;
using BidGuard.Infrastructure.Storage;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Minio;
using StackExchange.Redis;

namespace BidGuard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddIdentityServices(configuration);
        services.AddCaching(configuration);
        services.AddFileStorage(configuration);
        services.AddDocumentIntelligence(configuration);
        services.AddGovernmentRegistries(configuration);
        services.AddBackgroundJobs(configuration);
        return services;
    }

    private static string DatabaseConnection(IConfiguration configuration) =>
        configuration.GetConnectionString("Database")
        ?? throw new InvalidOperationException("ConnectionStrings:Database is not configured.");

    private static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<BidGuardDbContext>(options => options
            .UseNpgsql(DatabaseConnection(configuration),
                npgsql => npgsql.MigrationsAssembly(typeof(BidGuardDbContext).Assembly.FullName))
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<BidGuardDbContext>());

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<ICredentialRepository, CredentialRepository>();
        services.AddScoped<ITenderRepository, TenderRepository>();
        services.AddScoped<IComplianceRuleRepository, ComplianceRuleRepository>();
        services.AddScoped<IBidRepository, BidRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IExtractedFieldRepository, ExtractedFieldRepository>();
        services.AddScoped<IVerificationResultRepository, VerificationResultRepository>();
        services.AddScoped<IRuleEvaluationRepository, RuleEvaluationRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IReportingRepository, ReportingRepository>();

        return services;
    }

    private static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .Validate(s => s.Secret.Length >= 32, "Jwt:Secret must be at least 32 characters.")
            .ValidateOnStart();

        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        return services;
    }

    /// <summary>Redis when Redis:ConnectionString is set; otherwise an in-process cache (fine for a single instance).</summary>
    private static IServiceCollection AddCaching(this IServiceCollection services, IConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration["Redis:ConnectionString"]))
        {
            services.AddMemoryCache();
            services.AddSingleton<ICacheService, MemoryCacheService>();
            return services;
        }

        var settings = configuration.GetSection(RedisSettings.SectionName).Get<RedisSettings>() ?? new RedisSettings();
        services.AddSingleton(settings);
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var options = ConfigurationOptions.Parse(settings.ConnectionString);
            options.AbortOnConnectFail = false; // keep serving (cache misses) while Redis is unavailable
            return ConnectionMultiplexer.Connect(options);
        });
        services.AddSingleton<ICacheService, RedisCacheService>();
        return services;
    }

    /// <summary>Storage:Provider = "Minio" (any S3-compatible store: MinIO, S3, Cloudflare R2) or "Local" (a mounted volume).</summary>
    private static IServiceCollection AddFileStorage(this IServiceCollection services, IConfiguration configuration)
    {
        if (string.Equals(configuration["Storage:Provider"], "Local", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton(new LocalStorageSettings { RootPath = configuration["Storage:LocalPath"] ?? "/data/uploads" });
            services.AddSingleton<IFileStorageService, LocalFileStorageService>();
            return services;
        }

        var settings = configuration.GetSection(MinioSettings.SectionName).Get<MinioSettings>() ?? new MinioSettings();
        services.AddSingleton(settings);
        services.AddSingleton<IMinioClient>(_ => new MinioClient()
            .WithEndpoint(settings.Endpoint)
            .WithCredentials(settings.AccessKey, settings.SecretKey)
            .WithSSL(settings.UseSsl)
            .Build());
        services.AddSingleton<IFileStorageService, MinioFileStorageService>();
        return services;
    }

    private static IServiceCollection AddDocumentIntelligence(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(SidecarSettings.SectionName).Get<SidecarSettings>() ?? new SidecarSettings();
        services.AddHttpClient<IDocumentIntelligenceService, SidecarDocumentIntelligenceService>(client =>
        {
            client.BaseAddress = new Uri(settings.BaseUrl.EndsWith('/') ? settings.BaseUrl : settings.BaseUrl + "/");
            client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
        });
        return services;
    }

    private static IServiceCollection AddGovernmentRegistries(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MockGovSettings>(configuration.GetSection(MockGovSettings.SectionName));

        services.AddSingleton<IGstVerificationService, MockGstVerificationService>();
        services.AddSingleton<IMcaVerificationService, MockMcaVerificationService>();
        services.AddSingleton<IUdyamVerificationService, MockUdyamVerificationService>();
        services.AddSingleton<IEpfoVerificationService, MockEpfoVerificationService>();

        // Jobs enumerate every registry adapter through the common port.
        services.AddSingleton<IRegistryVerificationService>(sp => sp.GetRequiredService<IGstVerificationService>());
        services.AddSingleton<IRegistryVerificationService>(sp => sp.GetRequiredService<IMcaVerificationService>());
        services.AddSingleton<IRegistryVerificationService>(sp => sp.GetRequiredService<IUdyamVerificationService>());
        services.AddSingleton<IRegistryVerificationService>(sp => sp.GetRequiredService<IEpfoVerificationService>());
        return services;
    }

    private static IServiceCollection AddBackgroundJobs(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = DatabaseConnection(configuration);
        services.AddSingleton<PermanentFailureFilter>();
        services.AddHangfire((sp, config) => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseFilter(sp.GetRequiredService<PermanentFailureFilter>())
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connection),
                new PostgreSqlStorageOptions { SchemaName = "hangfire", PrepareSchemaIfNecessary = true }));

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = Math.Max(2, Environment.ProcessorCount);
            options.Queues = ["default"];
        });

        services.AddScoped<IBackgroundJobScheduler, HangfireJobScheduler>();
        GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute { Attempts = 3, DelaysInSeconds = [5, 15, 30] });
        return services;
    }
}
