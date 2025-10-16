using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Synka.Server.Tests.Infrastructure;

internal class AuthenticatedSchemeWebApplicationFactory(Action<TestAuthenticationSchemeOptions>? configureOptions = null) : TestWebApplicationFactory
{
    private const string SchemeName = "TestAuth";
    private readonly Action<TestAuthenticationSchemeOptions>? _configureOptions = configureOptions;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder); // Call base to set up Testing environment and database

        builder.ConfigureServices(services =>
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = SchemeName;
                options.DefaultChallengeScheme = SchemeName;
                options.DefaultSignInScheme = SchemeName;
            }).AddScheme<TestAuthenticationSchemeOptions, TestAuthHandler>(SchemeName, options =>
            {
                options.UserName ??= "TestUser";
                _configureOptions?.Invoke(options);
            });
        });
    }
}
