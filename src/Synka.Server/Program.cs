using Synka.Server.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddSynkaCoreServices();
builder.AddSynkaDatabase();
builder.AddSynkaAuthentication();
builder.AddSynkaApplicationServices();

var app = builder.Build();

app.MapOpenApiDocument();
app.EnsureDatabaseIsMigrated();

app.UseHttpsRedirection();
app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseAuthentication();
app.UseAuthorization();

// Serve static files from wwwroot (Angular app)
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapHealthChecks("/api/v{version:apiVersion}/health");
app.MapAuthenticationEndpoints();
app.MapServiceManifestEndpoint();
app.MapConfigurationEndpoint();
app.MapFileEndpoints();

// Fallback to index.html for Angular routing
app.MapFallbackToFile("index.html");

app.Run();

public sealed partial class Program;
