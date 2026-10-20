## Overview

The `Delinea-SecretServer-Password` PAM type authenticates to Delinea Secret Server using a username and password
(OAuth2 `password` grant). This is the recommended type for environments where a service account with a username
and password is used.

## Requirements

- Delinea Secret Server instance accessible over HTTPS from the host running Keyfactor Command or the Universal Orchestrator.
- A service account with a username and password that has permission to view the secrets being retrieved. See the
  [Delinea Secret Server documentation](https://docs.delinea.com/online-help/secret-server/api-scripting/authentication/script-token-auth/index.htm)
  for information on configuring service accounts.


## Registration and Naming

The PAM provider name you create in Keyfactor Command, the extension registration name, and the `InitializationInfo`
key must **all** be `Delinea-SecretServer-Password`. If they differ, the Universal Orchestrator will not load the provider for this type.

This type always uses the `password` grant. A `GrantType` value is not needed and is ignored.

### Universal Orchestrator

The `manifest.json` shipped in the release registers this provider's class under the extension name `Delinea-SecretServer-Password`
and defines the matching `InitializationInfo` key:

```json
{
  "extensions": {
    "Keyfactor.Platform.Extensions.IPAMProvider": {
      "PAMProviders.Delinea-SecretServer-Password.PAMProvider": {
        "assemblyPath": "delinea-secretserver-pam.dll",
        "TypeFullName": "Keyfactor.Extensions.Pam.Delinea.SecretServerPamPassword"
      }
    }
  },
  "Keyfactor:PAMProviders:Delinea-SecretServer-Password:InitializationInfo": {
    "Host": "https://example.secretservercloud.com/SecretServer",
    "Username": "<USERNAME>",
    "Password": "<PASSWORD>"
  }
}
```

The release `manifest.json` registers all four Delinea PAM types, so `extensions` already contains this entry.
Only populate the `InitializationInfo` block for the type(s) you use.

### Keyfactor Command host (Command 10 `web.config`)

```xml
<register type="IPAMProvider" mapTo="Keyfactor.Extensions.Pam.Delinea.SecretServerPamPassword, delinea-secretserver-pam" name="Delinea-SecretServer-Password" />
```

The assembly name in `mapTo` is the name of the extension DLL (`delinea-secretserver-pam`), and the class is the one
for this PAM type.
