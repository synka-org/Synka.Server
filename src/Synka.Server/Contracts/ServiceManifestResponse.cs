using System.Collections.Generic;

namespace Synka.Server.Contracts;

public sealed record ServiceManifestResponse(
    string Service,
    string Version,
    bool RequiresConfiguration,
    IReadOnlyList<ConfigurationTaskStatus> ConfigurationTasks);
