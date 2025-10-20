using Asp.Versioning;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Synka.Server.Authorization;
using Synka.Server.Data;
using Synka.Server.Data.Entities;
using Synka.Server.Services;

namespace Synka.Server.Extensions;

internal static class WebApplicationBuilderExtensions
{
    public static void AddSynkaCoreServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddProblemDetails();
        builder.Services.AddHealthChecks();
        builder.Services.AddOpenApi();
        builder.Services.AddHttpContextAccessor();

        // Add API versioning
        builder.Services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        });

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicies.AdminOnly, policy =>
                policy.RequireRole(RoleNames.Admin));

            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });
    }

    public static void AddSynkaDatabase(this WebApplicationBuilder builder)
    {
        var providerAccessor = new DatabaseProviderAccessor(builder.Configuration);
        builder.Services.AddSingleton<IDatabaseProviderAccessor>(providerAccessor);

        var connectionString = providerAccessor.ConnectionString
            ?? throw new InvalidOperationException($"Connection string '{providerAccessor.ConnectionStringName}' was not found.");

        Action<DbContextOptionsBuilder> configureOptions = options =>
        {
            switch (providerAccessor.Provider)
            {
                case DatabaseProvider.PostgreSql:
                    options.UseNpgsql(connectionString);
                    break;
                default:
                    options.UseSqlite(connectionString);
                    break;
            }
        };

        builder.Services.AddDbContext<SynkaDbContext>(configureOptions);
    }

    public static void AddSynkaAuthentication(this WebApplicationBuilder builder)
    {
        var identityBuilder = builder.Services.AddIdentityCore<ApplicationUserEntity>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequireDigit = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequiredLength = 8;
        });

        identityBuilder = identityBuilder.AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<SynkaDbContext>()
            .AddSignInManager();

        identityBuilder.AddDefaultTokenProviders();
        identityBuilder.AddApiEndpoints();

        var authBuilder = builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = IdentityConstants.ApplicationScheme;
            options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
        });

        authBuilder.AddIdentityCookies();
        authBuilder.AddBearerToken(IdentityConstants.BearerScheme);

        var oidcSection = builder.Configuration.GetSection("Authentication:OIDC");
        if (!string.IsNullOrWhiteSpace(oidcSection.GetValue<string>("Authority")))
        {
            authBuilder.AddOpenIdConnect("oidc", options =>
            {
                oidcSection.Bind(options);
                options.SignInScheme = IdentityConstants.ExternalScheme;
                options.ResponseType ??= OpenIdConnectResponseType.Code;
                if (options.CallbackPath == default)
                {
                    options.CallbackPath = "/signin-oidc";
                }

                options.SaveTokens = true;

                if (options.Scope.Count == 0)
                {
                    options.Scope.Add("openid");
                    options.Scope.Add("profile");
                }
                else if (!options.Scope.Contains("openid"))
                {
                    options.Scope.Add("openid");
                }
            });

            builder.Services.Configure<AuthenticationOptions>(options => options.DefaultChallengeScheme = "oidc");
        }
    }

    public static void AddSynkaApplicationServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IConfigurationService, ConfigurationService>();
        builder.Services.AddScoped<IFileService, FileService>();
        builder.Services.AddScoped<IFolderService, FolderService>();
        builder.Services.AddScoped<IFolderAccessService, FolderAccessService>();
        builder.Services.AddScoped<IFileSystemScannerService, FileSystemScannerService>();

        // Background service for automatic folder watching
        // Register as both IHostedService and IFileSystemWatcherManager (singleton)
        builder.Services.AddSingleton<FileSystemWatcherHostedService>();
        builder.Services.AddSingleton<IFileSystemWatcherManager>(sp => sp.GetRequiredService<FileSystemWatcherHostedService>());
        builder.Services.AddHostedService(sp => sp.GetRequiredService<FileSystemWatcherHostedService>());
    }
}
