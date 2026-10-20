## Overview

`Delinea-SecretServer` is the original backwards-compatible PAM type. It supports all three authentication methods
(`password`, `client_credentials`, `windows`) selected at runtime via the `GrantType` configuration parameter.
The Keyfactor Command UI will display every credential field regardless of which grant type is active.

For new installations, use the type-specific variants (`Delinea-SecretServer-Password`,
`Delinea-SecretServer-ClientCredentials`, or `Delinea-SecretServer-Windows`) which only show the fields relevant
to the chosen authentication method.

## Requirements

- Delinea Secret Server instance accessible over HTTPS from the host running Keyfactor Command or the Universal Orchestrator.
- A service account or application account with permission to view the secrets being retrieved. See the
  [Delinea Secret Server documentation](https://docs.delinea.com/online-help/secret-server/api-scripting/authentication/script-token-auth/index.htm)
  for information on configuring service accounts and application accounts.


## Registration and Naming

The PAM provider name you create in Keyfactor Command, the extension registration name, and the `InitializationInfo`
key must **all** be `Delinea-SecretServer`. If they differ, the Universal Orchestrator will not load the provider for this type.

This generic type selects its authentication flow from `GrantType` and defaults to `password` when it is not supplied. For new installations prefer the type-specific PAM types.

### Universal Orchestrator

The `manifest.json` shipped in the release registers this provider's class under the extension name `Delinea-SecretServer`
and defines the matching `InitializationInfo` key:

```json
{
  "extensions": {
    "Keyfactor.Platform.Extensions.IPAMProvider": {
      "PAMProviders.Delinea-SecretServer.PAMProvider": {
        "assemblyPath": "delinea-secretserver-pam.dll",
        "TypeFullName": "Keyfactor.Extensions.Pam.Delinea.SecretServerPam"
      }
    }
  },
  "Keyfactor:PAMProviders:Delinea-SecretServer:InitializationInfo": {
    "Host": "https://example.secretservercloud.com/SecretServer",
    "Username": "<USERNAME>",
    "Password": "<PASSWORD>",
    "ClientId": "<OAUTH_CLIENT_ID>",
    "ClientSecret": "<OAUTH_CLIENT_SECRET>",
    "GrantType": "password|client_credentials|windows"
  }
}
```

The release `manifest.json` registers all four Delinea PAM types, so `extensions` already contains this entry.
Only populate the `InitializationInfo` block for the type(s) you use.

### Keyfactor Command host (Command 10 `web.config`)

```xml
<register type="IPAMProvider" mapTo="Keyfactor.Extensions.Pam.Delinea.SecretServerPam, delinea-secretserver-pam" name="Delinea-SecretServer" />
```

The assembly name in `mapTo` is the name of the extension DLL (`delinea-secretserver-pam`), and the class is the one
for this PAM type.
