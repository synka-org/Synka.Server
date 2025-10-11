namespace Synka.Server.Data;

internal interface IDatabaseProviderAccessor
{
    DatabaseProvider Provider { get; }

    string ConnectionStringName { get; }

    string? ConnectionString { get; }
}
