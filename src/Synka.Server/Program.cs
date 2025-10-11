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

app.MapHealthChecks("/health");
app.MapAuthenticationEndpoints();
app.MapServiceManifestEndpoint();
app.MapConfigurationEndpoint();

app.Run();

public sealed partial class Program;
