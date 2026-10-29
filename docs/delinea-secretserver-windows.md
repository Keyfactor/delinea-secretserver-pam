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

## Registration and Naming

The extension entry in the Universal Orchestrator `manifest.json` is keyed by the name of the PAM provider you created in Keyfactor Command (the provider instance name, not the PAM type name `Delinea-SecretServer-Windows`), with no `PAMProviders.` prefix and no `.PAMProvider` suffix. The `InitializationInfo` section uses the same name (`Keyfactor:PAMProviders:<provider name>:InitializationInfo`). The shipped placeholder for both is `YOUR-PROVIDER-NAME`, which must be replaced before the manifest is usable. Any provider name works as long as the extension key and the section name match the provider name in Command. The manifest contains only this one section; no separate section named after the PAM type is needed.

This type always uses Integrated Windows Authentication. A `GrantType` value is not needed and is ignored.

> [!IMPORTANT]
> A Universal Orchestrator supports only **one** PAM provider registration per `manifest.json` (until UO v26.4), so only one of the manifest files can be installed as `manifest.json` on a given orchestrator. Using more than one Delinea PAM type requires a separate Universal Orchestrator for each.

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

