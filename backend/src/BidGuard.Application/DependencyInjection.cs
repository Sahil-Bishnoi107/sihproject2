using BidGuard.Application.Common.Behaviors;
using BidGuard.Application.Jobs;
using BidGuard.Application.Services.Audit;
using BidGuard.Application.Services.Bids;
using BidGuard.Application.Services.Documents;
using BidGuard.Application.Services.Evaluation;
using BidGuard.Application.Services.Identity;
using BidGuard.Application.Services.Rules;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace BidGuard.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediator(options =>
        {
            options.ServiceLifetime = ServiceLifetime.Scoped;
            options.PipelineBehaviors = [typeof(ValidationBehavior<,>)];
        });

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddSingleton(TimeProvider.System);

        services.AddSingleton<IRuleEngine, RuleEngine>();
        services.AddSingleton<IExemptionService, ExemptionService>();
        services.AddSingleton<IScoringService, ScoringService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<AuthTokenIssuer>();
        services.AddScoped<DocumentUploader>();

        services.AddScoped<BidPipeline>();

        services.AddScoped<ExtractTenderRulesJob>();
        services.AddScoped<ExtractBidEntitiesJob>();
        services.AddScoped<VerifyBidCredentialsJob>();
        services.AddScoped<EvaluateBidJob>();
        services.AddScoped<CredentialExpiryAlertJob>();

        return services;
    }
}
