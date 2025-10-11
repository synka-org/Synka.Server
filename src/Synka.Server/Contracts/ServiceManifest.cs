namespace Synka.Server.Contracts;

public sealed record ServiceManifest(string Service, string Version, bool RequiresConfiguration);
