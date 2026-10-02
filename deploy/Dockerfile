# BidGuard AI — single deployable image: web app (nginx) + .NET API + Python AI sidecar.
# Build from the repository root:  docker build -f deploy/Dockerfile -t sahilbishnoi1/sihproject2 .
# Needs only a PostgreSQL database at runtime (see deploy/DEPLOY.md).

# ---- web: React build ----
FROM node:22-alpine AS web
WORKDIR /src
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

# ---- api: .NET publish ----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS api
WORKDIR /src
COPY backend/Directory.Build.props backend/BidGuard.sln ./
COPY backend/src/BidGuard.Domain/BidGuard.Domain.csproj src/BidGuard.Domain/
COPY backend/src/BidGuard.Application/BidGuard.Application.csproj src/BidGuard.Application/
COPY backend/src/BidGuard.Infrastructure/BidGuard.Infrastructure.csproj src/BidGuard.Infrastructure/
COPY backend/src/BidGuard.API/BidGuard.API.csproj src/BidGuard.API/
RUN dotnet restore src/BidGuard.API/BidGuard.API.csproj
COPY backend/src/ src/
RUN dotnet publish src/BidGuard.API/BidGuard.API.csproj -c Release -o /out --no-restore

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:9.0

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        nginx supervisor gettext-base curl \
        python3 python3-venv \
        tesseract-ocr tesseract-ocr-eng tesseract-ocr-hin \
    && rm -rf /var/lib/apt/lists/*

# AI sidecar in its own virtualenv
COPY bidguard-ai/requirements.txt /app/ai/requirements.txt
RUN python3 -m venv /opt/ai-venv \
    && /opt/ai-venv/bin/pip install --no-cache-dir -r /app/ai/requirements.txt
COPY bidguard-ai/app /app/ai/app

COPY --from=api /out /app/api
COPY --from=web /src/dist /app/web
COPY deploy/nginx.conf.template deploy/supervisord.conf /etc/bidguard/
COPY deploy/entrypoint.sh /usr/local/bin/bidguard-entrypoint
RUN chmod +x /usr/local/bin/bidguard-entrypoint \
    && mkdir -p /data/uploads \
    && chown -R app:app /data

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://127.0.0.1:5000 \
    PORT=8080 \
    Sidecar__BaseUrl=http://127.0.0.1:8000/ \
    ForwardedHeaders__Enabled=true \
    Storage__Provider=Local \
    Storage__LocalPath=/data/uploads \
    PYTHONUNBUFFERED=1

VOLUME ["/data"]
EXPOSE 8080
USER app

HEALTHCHECK --interval=30s --timeout=5s --start-period=60s --retries=3 \
    CMD curl -fs "http://127.0.0.1:${PORT}/health" || exit 1

ENTRYPOINT ["bidguard-entrypoint"]
