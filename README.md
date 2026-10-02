# BidGuard AI

AI-powered bid compliance verification for GeM procurement — SIH 2026, problem statement SIH26100, Team Innoverse.

BidGuard turns a tender PDF into machine-readable rules (Compliance-as-Code), extracts evidence from bidder documents, verifies credentials against government registries, and gives officers an explainable, evidence-backed verdict. AI only drafts and extracts; a deterministic rule engine decides, and the officer makes the final call. Every step is written to a SHA-256 hash-chained audit log.

## Quick start

Requirements: Docker Desktop. (For local development: .NET 9 SDK and Python 3.12+.)

```bash
docker compose up -d --build
```

| URL | What |
|---|---|
| http://localhost:8080/swagger | API with Swagger UI (click **Authorize** and paste an access token) |
| http://localhost:8080/hangfire | Background job dashboard |
| http://localhost:8000/health | AI sidecar status (LLM on/off, OCR available) |
| http://localhost:9001 | MinIO console |

Demo accounts are seeded in Development: `admin@bidguard.local`, `officer@bidguard.local`, `bidder@shakti.local`. Their passwords are in `backend/src/BidGuard.API/appsettings.Development.json` under `Seed`.

Run the end-to-end demo (tender → AI rules → publish → simulation with fix list → official bid → report → decision → audit check):

```powershell
powershell -ExecutionPolicy Bypass -File scripts/smoke.ps1
```

Sample PDFs live in `samples/`; regenerate them with `python samples/generate_samples.py` (needs PyMuPDF).

To let the sidecar use an LLM for extraction, set `ANTHROPIC_API_KEY` and `BIDGUARD_LLM_MODEL` before `docker compose up`. Without it, deterministic heuristics are used, so the demo works offline.

## Architecture

```
backend/
  src/BidGuard.Domain          Entities with guarded state transitions, enums, repository interfaces
  src/BidGuard.Application     Features/{Commands,Queries}/<Category>/ (Mediator + CQRS), FluentValidation pipeline,
                               RuleEngine, Exemption, Scoring, Audit services, Hangfire job classes, ports
  src/BidGuard.Infrastructure  EF Core (Npgsql, Fluent config per entity), repositories, JWT + refresh tokens,
                               Redis cache, MinIO storage, AI sidecar client, mock GST/MCA/Udyam/EPFO adapters, Hangfire
  src/BidGuard.API             Thin controllers, JWT bearer, Swagger with auth, ProblemDetails, rate limiting
  tests/                       xUnit (domain, rule engine, handlers, jobs, adapters)
bidguard-ai/                   FastAPI sidecar: PyMuPDF text, Tesseract OCR fallback, LLM structured extraction or heuristics
```

Pipeline: upload → Hangfire job → sidecar extraction → registry verification (cached 24 h) → deterministic evaluation (exemptions, weighted score) → officer review → final decision. Simulations run the same pipeline but are visible only to the bidder, who gets a ranked fix list.

Decisions and build log: `PROGRESS.md`.

## Development

```bash
# Backend tests
dotnet test backend/BidGuard.sln

# Sidecar tests
cd bidguard-ai && python -m venv .venv && .venv/Scripts/pip install -r requirements-dev.txt && .venv/Scripts/python -m pytest

# Run the API locally against compose infrastructure (Postgres on host port 5433)
docker compose up -d postgres redis minio bidguard-ai
dotnet run --project backend/src/BidGuard.API --launch-profile http
```

New migration: `dotnet ef migrations add <Name> -p backend/src/BidGuard.Infrastructure -s backend/src/BidGuard.Infrastructure -o Persistence/Migrations`.
