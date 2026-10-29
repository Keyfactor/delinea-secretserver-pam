## Delinea-SecretServer-Password

The `Delinea-SecretServer-Password` PAM type authenticates to Delinea Secret Server using a username and password
(OAuth2 `password` grant). This is the recommended type for environments where a service account with a username
and password is used.

## Requirements

- Delinea Secret Server instance accessible over HTTPS from the host running Keyfactor Command or the Universal Orchestrator.
- A service account with a username and password that has permission to view the secrets being retrieved. See the
  [Delinea Secret Server documentation](https://docs.delinea.com/online-help/secret-server/api-scripting/authentication/script-token-auth/index.htm)
  for information on configuring service accounts.
- The `SecretFieldName` value must match a field's name or slug on the secret exactly (the comparison is case-sensitive). An empty value, or a field that does not exist on the secret, is an error.

## Delinea-SecretServer-Password Registration Snippets

Shared registration and naming rules are described in [Registration and Naming](https://github.com/Keyfactor/delinea-secretserver-pam/blob/main/README.md#registration-and-naming).

This type always uses the `password` grant. A `GrantType` value is not needed and is ignored.

### Password: Universal Orchestrator manifest

The release ships this registration as `Password-manifest.json`; rename it to `manifest.json` on install. It contains exactly this registration and this `InitializationInfo` section:

```json
{
  "extensions": {
    "Keyfactor.Platform.Extensions.IPAMProvider": {
      "YOUR-PROVIDER-NAME": {
        "assemblyPath": "delinea-secretserver-pam.dll",
        "TypeFullName": "Keyfactor.Extensions.Pam.Delinea.SecretServerPamPassword"
      }
    }
  },
  "Keyfactor:PAMProviders:YOUR-PROVIDER-NAME:InitializationInfo": {
    "Host": "https://example.secretservercloud.com/SecretServer",
    "Username": "<USERNAME>",
    "Password": "<PASSWORD>"
  }
}
```

### Password: Command 10 `web.config` registration

```xml
<register type="IPAMProvider" mapTo="Keyfactor.Extensions.Pam.Delinea.SecretServerPamPassword, delinea-secretserver-pam" name="Delinea-SecretServer-Password" />
```

The assembly name in `mapTo` is the name of the extension DLL (`delinea-secretserver-pam`), and the class is the one for this PAM type.
