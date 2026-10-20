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

The PAM provider name you create in Keyfactor Command, the extension registration name, and the `InitializationInfo`
key must **all** be `Delinea-SecretServer-Windows`. If they differ, the Universal Orchestrator will not load the provider for this type.

This type always uses Integrated Windows Authentication. A `GrantType` value is not needed and is ignored.

### Universal Orchestrator

The `manifest.json` shipped in the release registers this provider's class under the extension name `Delinea-SecretServer-Windows`
and defines the matching `InitializationInfo` key:

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

The release `manifest.json` registers all four Delinea PAM types, so `extensions` already contains this entry.
Only populate the `InitializationInfo` block for the type(s) you use.

### Keyfactor Command host (Command 10 `web.config`)

```xml
<register type="IPAMProvider" mapTo="Keyfactor.Extensions.Pam.Delinea.SecretServerPamWindows, delinea-secretserver-pam" name="Delinea-SecretServer-Windows" />
```

The assembly name in `mapTo` is the name of the extension DLL (`delinea-secretserver-pam`), and the class is the one
for this PAM type.
