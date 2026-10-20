## Delinea-SecretServer-ClientCredentials

The `Delinea-SecretServer-ClientCredentials` PAM type authenticates to Delinea Secret Server using OAuth2 client
credentials (application account name and password). This is the recommended type for service-to-service
integrations where an application account is used instead of a user account.

## Requirements

- Delinea Secret Server instance accessible over HTTPS from the host running Keyfactor Command or the Universal Orchestrator.
- An application account (Client ID and Client Secret) with permission to view the secrets being retrieved. See the
  [Delinea Secret Server documentation](https://docs.delinea.com/online-help/secret-server/api-scripting/authentication/script-token-auth/index.htm)
  for information on configuring application accounts.




## Registration and Naming

The PAM provider name you create in Keyfactor Command, the extension registration name, and the `InitializationInfo`
key must **all** be `Delinea-SecretServer-ClientCredentials`. If they differ, the Universal Orchestrator will not load the provider for this type.

This type always uses the `client_credentials` flow. A `GrantType` value is not needed and is ignored. If the registration is missing or its name does not match, the Universal Orchestrator falls back to the generic provider, which defaults to `password`.

### Universal Orchestrator

The `manifest.json` shipped in the release registers this provider's class under the extension name `Delinea-SecretServer-ClientCredentials`
and defines the matching `InitializationInfo` key:

```json
{
  "extensions": {
    "Keyfactor.Platform.Extensions.IPAMProvider": {
      "PAMProviders.Delinea-SecretServer-ClientCredentials.PAMProvider": {
        "assemblyPath": "delinea-secretserver-pam.dll",
        "TypeFullName": "Keyfactor.Extensions.Pam.Delinea.SecretServerPamClientCredentials"
      }
    }
  },
  "Keyfactor:PAMProviders:Delinea-SecretServer-ClientCredentials:InitializationInfo": {
    "Host": "https://example.secretservercloud.com/SecretServer",
    "ClientId": "<OAUTH_CLIENT_ID>",
    "ClientSecret": "<OAUTH_CLIENT_SECRET>"
  }
}
```

The release `manifest.json` registers all four Delinea PAM types, so `extensions` already contains this entry.
Only populate the `InitializationInfo` block for the type(s) you use.

### Keyfactor Command host (Command 10 `web.config`)

```xml
<register type="IPAMProvider" mapTo="Keyfactor.Extensions.Pam.Delinea.SecretServerPamClientCredentials, delinea-secretserver-pam" name="Delinea-SecretServer-ClientCredentials" />
```

The assembly name in `mapTo` is the name of the extension DLL (`delinea-secretserver-pam`), and the class is the one
for this PAM type.
