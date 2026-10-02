# Build context: backend/
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Directory.Build.props BidGuard.sln ./
COPY src/BidGuard.Domain/BidGuard.Domain.csproj src/BidGuard.Domain/
COPY src/BidGuard.Application/BidGuard.Application.csproj src/BidGuard.Application/
COPY src/BidGuard.Infrastructure/BidGuard.Infrastructure.csproj src/BidGuard.Infrastructure/
COPY src/BidGuard.API/BidGuard.API.csproj src/BidGuard.API/
RUN dotnet restore src/BidGuard.API/BidGuard.API.csproj

COPY src/ src/
RUN dotnet publish src/BidGuard.API/BidGuard.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
USER app
ENTRYPOINT ["dotnet", "BidGuard.API.dll"]
