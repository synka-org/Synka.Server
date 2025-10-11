namespace Synka.Server.Data;

internal sealed class DatabaseProviderAccessor(IConfiguration configuration) : IDatabaseProviderAccessor
{
    private const string ProviderConfigKey = "Database:Provider";

    private readonly (DatabaseProvider Provider, string ConnectionStringName, string? ConnectionString) _resolvedConfiguration =
        ResolveConfiguration(configuration);

    public DatabaseProvider Provider => _resolvedConfiguration.Provider;

    public string ConnectionStringName => _resolvedConfiguration.ConnectionStringName;

    public string? ConnectionString => _resolvedConfiguration.ConnectionString;

    private static (DatabaseProvider Provider, string ConnectionStringName, string? ConnectionString) ResolveConfiguration(
        IConfiguration configuration)
    {
        var provider = DetermineProvider(configuration);
        var connectionStringName = DetermineConnectionStringName(provider);
        var connectionString = configuration.GetConnectionString(connectionStringName);

        return (provider, connectionStringName, connectionString);
    }

    private static DatabaseProvider DetermineProvider(IConfiguration configuration)
    {
        var providerName = configuration.GetValue<string?>(ProviderConfigKey) ?? nameof(DatabaseProvider.Sqlite);

        return providerName.ToUpperInvariant() switch
        {
            "POSTGRES" or "POSTGRESQL" or "PG" => DatabaseProvider.PostgreSql,
            _ => DatabaseProvider.Sqlite,
        };
    }

    private static string DetermineConnectionStringName(DatabaseProvider provider)
    {
        return provider switch
        {
            DatabaseProvider.PostgreSql => "Postgres",
            _ => "Sqlite",
        };
    }
}
