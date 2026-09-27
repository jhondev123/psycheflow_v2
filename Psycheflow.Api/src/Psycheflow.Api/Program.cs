using Psycheflow.Api.Common;
using Psycheflow.Api.Common.Persistence;
using Psycheflow.Api.Features;
using Scalar.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddCommon(builder.Configuration)
    .AddPersistence(builder.Environment)
    .AddFeatures();

WebApplication app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference(options => options.WithTitle("Psycheflow API")).AllowAnonymous();
}

app.MapHealthChecks("/health").AllowAnonymous();
app.MapFeatures();

if (app.Configuration.GetValue<bool>(PersistenceSetup.MigrateOnStartupKey))
{
    await app.ApplyMigrationsAsync();
}

await app.RunAsync();
