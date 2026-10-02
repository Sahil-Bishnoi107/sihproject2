# BidGuard AI — Progress Log

Running record of what was built and why.

## Decisions

| Date | Decision | Why |
|---|---|---|
| 2026-09-29 | Backend in .NET 9 (clean architecture); OCR/AI in a Python FastAPI sidecar | Team backend conventions; Python has the better OCR/LLM tooling |
| 2026-09-29 | Hangfire job calls the sidecar over HTTP (not a message queue) | Fewest moving parts for a demo; free retries + dashboard; adapter hides transport |
| 2026-09-29 | `Mediator` (source-generated, MIT) instead of MediatR | MediatR is commercially licensed from v13 |
| 2026-09-29 | Hangfire abstractions/jobs in Application, server/storage in Infrastructure | Keeps Application free of infrastructure packages |
| 2026-09-29 | Government APIs mocked behind adapters | Real GSTN/MCA access is unavailable for a hackathon; swap later without touching Application |
| 2026-09-29 | Final decision is deterministic (rule engine); AI only drafts rules and extracts data | Explainability; avoids LLM hallucinations deciding outcomes |

## Competitor scan (2026-09-29)

Scanned public GitHub repos for SIH26100: GEM-Sentinal, GeM-Sentinel_SIH2026, ComplyGem, bidsureAI, gem-ai-bid-verifier (ComplianceOS), gem-bid-compliance-v3. Table stakes: OCR extraction, rule engine, mock registries, override, audit. Proposed unique features: Tender Fairness Linter, Corrigendum Diff & Re-evaluation, Fix-Path Simulator, Credential Wallet, Make-in-India / Purchase Preference.

## Build log

| Task | Status | Notes |
|---|---|---|
| 1. Domain entities & state machines | Done | 13 entities, guarded status transitions, repository interfaces |
| 2-9. Core, rule engine, exemptions, audit, persistence/auth, adapters, API, tenders | Done | 89 tests; API live-tested with Postgres/Redis/Hangfire |
| 10. Bid pipeline (extract → verify → evaluate) | Done | Cross-matching: document vs profile identifiers → Mismatch; evidence with page + bbox |
| 11. Review, report, simulator, alerts, dashboard, wallet | Done | Report includes a ranked fix list (first cut of the Fix-Path Simulator) |
| 12. Python AI sidecar | Done | LLM extraction (structured JSON) or offline heuristics; OCR only for pages without text |
| 13. Compose, samples, smoke test, README | Done | `scripts/smoke.ps1` passes end to end on the full Docker stack |

## Demo results (smoke test, 2026-09-29)

- Tender PDF → 6 AI-drafted rules (turnover ≥ ₹50 lakh, 3 years experience, GST verified, ISO 9001 valid through bid end, OEM authorization, EMD).
- Weak simulation bid → score 20, NonCompliant, fix list: turnover (+30 pts), experience (+20), ISO renewal (+20), OEM letter (+10).
- Full official bid → score 100, Compliant; EMD exempted because the bidder is an MSE with a verified Udyam registration; all 4 registries verified.
- Officers cannot see simulations (404); the audit chain verifies intact.
- Bug found by the smoke test and fixed: tender clauses wrapped across PDF lines lost their "Lakh" unit (the rule read ₹50, not ₹50 lakh). The sidecar now splits tender text into sentences.

