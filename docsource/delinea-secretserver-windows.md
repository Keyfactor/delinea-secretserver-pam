## Overview

The `Delinea-SecretServer-Windows` PAM type authenticates to Delinea Secret Server using Integrated Windows
Authentication (IWA). No credentials are required in the configuration — the provider uses the Windows identity
of the process running Keyfactor Command or the Universal Orchestrator.

> [!IMPORTANT]
> Integrated Windows Authentication is not supported on Delinea Secret Server Cloud. This type is only compatible
> with on-premises Secret Server installations.

## Requirements

- On-premises Delinea Secret Server instance accessible from the host running Keyfactor Command or the Universal Orchestrator.
- The Windows service account running Keyfactor Command or the Universal Orchestrator must have permission to view
  the secrets being retrieved. See the
  [Delinea Secret Server IWA documentation](https://docs.delinea.com/online-help/secret-server/authentication/iwa-webservices/webservice-iwa-powershell/index.htm)
  for information on configuring IWA access.
- The Secret Server instance must be configured to allow Integrated Windows Authentication web service access.

## Registration and Naming

The extension registration name and the `InitializationInfo` key in `manifest.json` must both match the PAM provider
name you created in Keyfactor Command (by default `Delinea-SecretServer-Windows`). Any provider name works as long as all three match; if they
differ, the Universal Orchestrator will not load the provider for this type.

This type always uses Integrated Windows Authentication. A `GrantType` value is not needed and is ignored.

> [!IMPORTANT]
> A Universal Orchestrator supports only **one** PAM provider registration per `manifest.json` (until UO v26.4). To
> use this type, copy `samples/manifest.windows.json` from the release over `manifest.json` and fill in the values. Using more
> than one Delinea PAM type requires a separate Universal Orchestrator for each.

### Universal Orchestrator

`samples/manifest.windows.json` contains exactly this registration and `InitializationInfo` block:

```json
{
  "extensions": {
    "Keyfactor.Platform.Extensions.IPAMProvider": {
      "PAMProviders.Delinea-SecretServer-Windows.PAMProvider": {
        "assemblyPath": "delinea-secretserver-pam.dll",
        "TypeFullName": "Keyfactor.Extensions.Pam.Delinea.SecretServerPamWindows"
      }
    }
  },
  "Keyfactor:PAMProviders:Delinea-SecretServer-Windows:InitializationInfo": {
    "Host": "https://example.secretserver.internal/SecretServer"
  }
}
```

### Keyfactor Command host (Command 10 `web.config`)

```xml
<register type="IPAMProvider" mapTo="Keyfactor.Extensions.Pam.Delinea.SecretServerPamWindows, delinea-secretserver-pam" name="Delinea-SecretServer-Windows" />
```

The assembly name in `mapTo` is the name of the extension DLL (`delinea-secretserver-pam`), and the class is the one
for this PAM type.
