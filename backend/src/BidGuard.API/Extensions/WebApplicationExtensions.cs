using BidGuard.API.Infrastructure;
using BidGuard.Application.Common.Interfaces;
using BidGuard.Application.Jobs;
using BidGuard.Infrastructure.Persistence;
using Hangfire;

namespace BidGuard.API.Extensions;

public static class WebApplicationExtensions
{
    /// <summary>Migrates/seeds the database (when enabled) before the app starts serving.</summary>
    public static async Task InitializeAsync(this WebApplication app)
    {
        if (app.Configuration.GetValue("Database:MigrateOnStartup", true))
            await app.Services.InitializeDatabaseAsync();

        using var scope = app.Services.CreateScope();
        RecurringJobs.Register(scope.ServiceProvider.GetRequiredService<IBackgroundJobScheduler>());
    }

    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        if (app.Configuration.GetValue<bool>("ForwardedHeaders:Enabled"))
            app.UseForwardedHeaders();

        app.UseExceptionHandler();
        app.UseStatusCodePages();

        if (app.Configuration.GetValue("Swagger:Enabled", app.Environment.IsDevelopment()))
        {
            app.UseSwagger();
            app.UseSwaggerUI(o =>
            {
                o.DocumentTitle = "BidGuard AI API";
                o.EnablePersistAuthorization();
            });
        }

        app.UseCors();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            DashboardTitle = "BidGuard background jobs",
            Authorization = [new HangfireDashboardAuthorizationFilter(app.Environment.IsDevelopment())],
        });

        app.MapControllers();
        app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

        return app;
    }
}
