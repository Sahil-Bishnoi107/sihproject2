using BidGuard.API;
using BidGuard.API.Extensions;
using BidGuard.Application;
using BidGuard.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApiServices(builder.Configuration);

var app = builder.Build();

await app.InitializeAsync();
app.UseApiPipeline();

await app.RunAsync();

public partial class Program;
