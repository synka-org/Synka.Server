namespace Synka.Server.Contracts;

public sealed record ConfigurationTaskStatus(
    string Key,
    string Title,
    string Description,
    ConfigurationTaskState State);
