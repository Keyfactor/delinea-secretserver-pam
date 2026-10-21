## Delinea-SecretServer

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

The extension entry in the Universal Orchestrator `manifest.json` is keyed by the name of the PAM provider you created in Keyfactor Command (the provider instance name, not the PAM type name `Delinea-SecretServer`), with no `PAMProviders.` prefix and no `.PAMProvider` suffix. The first `InitializationInfo` section uses the same name. The shipped placeholder for both is `YOUR-PROVIDER-NAME`, which must be replaced before the manifest is usable. The second section, named after the PAM type `Delinea-SecretServer`, is kept as shipped. Any provider name works as long as the extension key and the first section name match the provider name in Command.

This generic type selects its authentication flow from `GrantType` and defaults to `password` when it is not supplied. For new installations prefer the type-specific PAM types.

> [!IMPORTANT]
> A Universal Orchestrator supports only **one** PAM provider registration per `manifest.json` (until UO v26.4). The shipped `manifest.json` registers this generic type. To use a type-specific provider instead, replace `manifest.json` with that type's manifest file. Using more than one Delinea PAM type requires a separate Universal Orchestrator for each.

### Universal Orchestrator

The release ships this registration as `manifest.json`. It contains exactly this registration and these `InitializationInfo` sections:

```json
{
  "extensions": {
    "Keyfactor.Platform.Extensions.IPAMProvider": {
      "YOUR-PROVIDER-NAME": {
        "assemblyPath": "delinea-secretserver-pam.dll",
        "TypeFullName": "Keyfactor.Extensions.Pam.Delinea.SecretServerPam"
      }
    }
  },
  "Keyfactor:PAMProviders:YOUR-PROVIDER-NAME:InitializationInfo": {
    "Host": "https://example.secretservercloud.com/SecretServer",
    "Username": "<USERNAME>",
    "Password": "<PASSWORD>",
    "ClientId": "<OAUTH_CLIENT_ID>",
    "ClientSecret": "<OAUTH_CLIENT_SECRET>",
    "GrantType": "password|client_credentials|windows"
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

### Keyfactor Command host (Command 10 `web.config`)

```xml
<register type="IPAMProvider" mapTo="Keyfactor.Extensions.Pam.Delinea.SecretServerPam, delinea-secretserver-pam" name="Delinea-SecretServer" />
```

The assembly name in `mapTo` is the name of the extension DLL (`delinea-secretserver-pam`), and the class is the one for this PAM type.
