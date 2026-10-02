# Deploying BidGuard AI (showcase)

One image, `sahilbishnoi1/sihproject2`, contains the web app, the API and the AI service. It needs a PostgreSQL database; uploads go to `/data` (mount a volume). Redis and S3 are optional.

## Quickest: Docker Compose

```bash
JWT_SECRET=<32+ random chars> SEED_OFFICER_PASSWORD=<pw> SEED_BIDDER_PASSWORD=<pw> SEED_ADMIN_PASSWORD=<pw> \
  docker compose -f docker-compose.prod.yml up -d
```

Open http://localhost:8080. Passwords need upper- and lowercase letters, a digit and 8+ characters.

## Any container host (Render, Railway, Fly, a VM)

Run `sahilbishnoi1/sihproject2:latest` with:

| Variable | Required | Notes |
|---|---|---|
| `DATABASE_URL` or `ConnectionStrings__Database` | yes | `postgres://user:pass@host:5432/db` is accepted |
| `Jwt__Secret` | yes | random, 32+ characters |
| `Seed__AdminPassword`, `Seed__OfficerPassword`, `Seed__BidderPassword` | for the demo | creates the three demo accounts on first start |
| `PORT` | no | defaults to 8080; most hosts set it |
| `ANTHROPIC_API_KEY` + `BIDGUARD_LLM_MODEL` | no | enables LLM extraction; offline heuristics otherwise |
| `Swagger__Enabled` | no | `true` to expose `/swagger` |
| `Redis__ConnectionString` | no | uses an in-memory cache when empty |
| `Storage__Provider=Minio` + `Minio__*` | no | S3/R2/MinIO instead of the `/data` volume |

Database migrations run automatically on start.
