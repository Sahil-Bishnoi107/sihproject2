using System.Text.Json;
using BidGuard.Application.Services.Audit;
using BidGuard.Application.Services.Bids;
using BidGuard.Application.Services.Evaluation;
using BidGuard.Application.Services.Rules;
using BidGuard.Domain.Entities;
using BidGuard.Domain.Enums;
using BidGuard.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace BidGuard.Application.Jobs;

/// <summary>
/// Pipeline step 3: the deterministic decision. Applies statutory exemptions, evaluates every approved rule,
/// attaches evidence (document, page, box, registry record) and computes the weighted score and suggested decision.
/// </summary>
public sealed class EvaluateBidJob(
    IBidRepository bids,
    IComplianceRuleRepository rules,
    IExtractedFieldRepository extractedFields,
    IVerificationResultRepository verifications,
    IRuleEvaluationRepository evaluations,
    IDocumentRepository documents,
    INotificationRepository notifications,
    IUserRepository users,
    IRuleEngine engine,
    IExemptionService exemptions,
    IScoringService scoring,
    IAuditService audit,
    IUnitOfWork unitOfWork,
    ILogger<EvaluateBidJob> logger) : IHandlesPermanentFailure
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static readonly Dictionary<string, VerificationSource> KeySources = new(StringComparer.OrdinalIgnoreCase)
    {
        ["gstin"] = VerificationSource.GST,
        ["cin"] = VerificationSource.MCA,
        ["udyam"] = VerificationSource.Udyam,
        ["epfo"] = VerificationSource.EPFO,
    };

    public async Task RunAsync(Guid bidId, CancellationToken ct)
    {
        var bid = await bids.GetByIdAsync(bidId, ct);
        if (bid is null || bid.Status != BidStatus.Evaluating)
        {
            logger.LogWarning("Skipping evaluation for bid {BidId}: not in Evaluating state", bidId);
            return;
        }

        var ruleSet = (await rules.GetByTenderAsync(bid.TenderId, ct)).Where(r => r.Status == RuleStatus.Approved).ToList();
        var fields = await extractedFields.GetByBidAsync(bid.Id, ct);
        var registry = await verifications.GetByBidAsync(bid.Id, ct);
        var files = (await documents.GetByOwnerAsync(DocumentOwnerType.Bid, bid.Id, ct)).ToDictionary(d => d.Id);

        var facts = FactSetBuilder.Build(bid.Tender, bid.Organization, fields, registry);
        var udyamVerified = registry.Any(v => v.Source == VerificationSource.Udyam && v.Status == VerificationStatus.Verified);

        evaluations.RemoveRange(await evaluations.GetByBidAsync(bid.Id, ct));

        var outcomes = new List<(ComplianceRule Rule, RuleOutcome Outcome)>();
        foreach (var rule in ruleSet)
        {
            var exemption = exemptions.GetExemption(rule, bid.Organization, udyamVerified);
            RuleEvaluation evaluation;
            if (exemption is not null)
            {
                var basis = registry.Where(v => v.Source == VerificationSource.Udyam).Select(RegistryEvidence).ToList<object>();
                evaluation = new RuleEvaluation(bid.Id, rule.Id, RuleOutcome.Exempted, exemption, JsonSerializer.Serialize(basis, Json));
            }
            else
            {
                var result = engine.Evaluate(rule.Expression, facts);
                evaluation = new RuleEvaluation(bid.Id, rule.Id, result.Outcome, result.Explanation,
                    JsonSerializer.Serialize(BuildEvidence(result.UsedKeys, fields, registry, files), Json));
            }
            await evaluations.AddAsync(evaluation, ct);
            outcomes.Add((rule, evaluation.Outcome));
        }

        var score = scoring.Score(outcomes);
        bid.MarkUnderReview(score.Score, score.Decision);

        await NotifyAsync(bid, score, ct);
        await audit.LogAsync("BidEvaluated", nameof(Bid), bid.Id.ToString(), new
        {
            score.Score,
            Decision = score.Decision.ToString(),
            Outcomes = outcomes.Select(o => new { o.Rule.Code, Outcome = o.Outcome.ToString() }),
        }, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public Task OnPermanentFailureAsync(IReadOnlyList<object?> jobArguments, string reason, CancellationToken ct) =>
        BidJobFailure.MarkFailedAsync(jobArguments, "evaluation", reason, bids, audit, unitOfWork, notifications, users, ct);

    private static List<object> BuildEvidence(IReadOnlyList<string> usedKeys, IReadOnlyList<ExtractedField> fields,
        IReadOnlyList<VerificationResult> registry, IReadOnlyDictionary<Guid, Document> files)
    {
        var evidence = new List<object>();
        foreach (var key in usedKeys)
        {
            foreach (var f in fields.Where(f => string.Equals(f.Key, key, StringComparison.OrdinalIgnoreCase)).OrderByDescending(f => f.Confidence))
                evidence.Add(new
                {
                    kind = "document",
                    key = f.Key,
                    value = f.Value,
                    confidence = f.Confidence,
                    documentId = f.DocumentId,
                    fileName = files.TryGetValue(f.DocumentId, out var d) ? d.FileName : null,
                    page = f.Page,
                    boundingBox = f.BoundingBox is null ? null : JsonSerializer.Deserialize<double[]>(f.BoundingBox),
                });

            if (KeySources.TryGetValue(key, out var source))
                evidence.AddRange(registry.Where(v => v.Source == source).Select(RegistryEvidence));
        }
        return evidence;
    }

    private static object RegistryEvidence(VerificationResult v) => new
    {
        kind = "registry",
        source = v.Source.ToString(),
        identifier = v.Identifier,
        status = v.Status.ToString(),
        registeredName = v.RegisteredName,
        checkedAt = v.CheckedAt,
    };

    private async Task NotifyAsync(Bid bid, ScoreResult score, CancellationToken ct)
    {
        var summary = $"score {score.Score:0.##}/100, suggested {score.Decision}";
        if (bid.IsSimulation)
        {
            foreach (var user in await users.GetByOrganizationAsync(bid.OrganizationId, ct))
                await notifications.AddAsync(new Notification(user.Id, NotificationType.BidEvaluated,
                    $"Pre-bid simulation for {bid.Tender.GemBidNumber} is ready: {summary}.", bid.Id.ToString()), ct);
        }
        else
        {
            await notifications.AddAsync(new Notification(bid.Tender.CreatedById, NotificationType.BidEvaluated,
                $"Bid from {bid.Organization.LegalName} on {bid.Tender.GemBidNumber} evaluated: {summary}.", bid.Id.ToString()), ct);
        }
    }
}
