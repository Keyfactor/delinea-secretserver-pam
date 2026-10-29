## Delinea-SecretServer-Windows

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

## Provider Registration Snippets

Shared registration and naming rules are described in [Registration and Naming](https://github.com/Keyfactor/delinea-secretserver-pam/blob/main/README.md#registration-and-naming).

This type always uses Integrated Windows Authentication. A `GrantType` value is not needed and is ignored.

### Universal Orchestrator

The release ships this registration as `Windows-manifest.json`; rename it to `manifest.json` on install. It contains exactly this registration and this `InitializationInfo` section:

```json
{
  "extensions": {
    "Keyfactor.Platform.Extensions.IPAMProvider": {
      "YOUR-PROVIDER-NAME": {
        "assemblyPath": "delinea-secretserver-pam.dll",
        "TypeFullName": "Keyfactor.Extensions.Pam.Delinea.SecretServerPamWindows"
      }
    }
  },
  "Keyfactor:PAMProviders:YOUR-PROVIDER-NAME:InitializationInfo": {
    "Host": "https://example.secretserver.internal/SecretServer"
  }
}
```

### Keyfactor Command host (Command 10 `web.config`)

```xml
<register type="IPAMProvider" mapTo="Keyfactor.Extensions.Pam.Delinea.SecretServerPamWindows, delinea-secretserver-pam" name="Delinea-SecretServer-Windows" />
```

The assembly name in `mapTo` is the name of the extension DLL (`delinea-secretserver-pam`), and the class is the one for this PAM type.

