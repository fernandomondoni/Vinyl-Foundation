using Serilog;
using Vinyl.Identity.Adapters;
using Vinyl.Identity.Application;
using Vinyl.Identity.API.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});

builder.Services.AddProblemDetails();
builder.Services.AddIdentityApplication();
builder.Services.AddIdentityAdapters(builder.Configuration);
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();
app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new
{
    service = "Vinyl.Identity",
    status = "running"
}));

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.MapIdentityEndpoints();
app.MapWorkspaceEndpoints();

app.Run();

public partial class Program
{
}
