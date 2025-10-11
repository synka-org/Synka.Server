namespace Synka.Server.Contracts;

public sealed record ServiceManifestResponse(string Service, string Version, bool RequiresConfiguration);
