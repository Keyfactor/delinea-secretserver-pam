## Overview

The `Delinea-SecretServer-ClientCredentials` PAM type authenticates to Delinea Secret Server using OAuth2 client
credentials (application account name and password). This is the recommended type for service-to-service
integrations where an application account is used instead of a user account.

## Requirements

- Delinea Secret Server instance accessible over HTTPS from the host running Keyfactor Command or the Universal Orchestrator.
- An application account (Client ID and Client Secret) with permission to view the secrets being retrieved. See the
  [Delinea Secret Server documentation](https://docs.delinea.com/online-help/secret-server/api-scripting/authentication/script-token-auth/index.htm)
  for information on configuring application accounts.

## Orchestrator Install

1. Install the Delinea Secret Server PAM Provider assemblies.

    * **Using kfutil**: On the server that hosts the Universal Orchestrator, run the following command:

        ```shell
        # Windows Server
        kfutil orchestrator extension -e delinea-secretserver-pam@latest --out "C:\Program Files\Keyfactor\Keyfactor Orchestrator\extensions"

        # Linux
        kfutil orchestrator extension -e delinea-secretserver-pam@latest --out "/opt/keyfactor/orchestrator/extensions"
        ```

    * **Manually**: Download the latest release of the Delinea Secret Server PAM Provider from the [Releases](../../releases) page. Extract the contents of the archive to:

        * **Windows Server**: `C:\Program Files\Keyfactor\Keyfactor Orchestrator\extensions\delinea-secretserver-pam`
        * **Linux**: `/opt/keyfactor/orchestrator/extensions/delinea-secretserver-pam`

2. The release contains one manifest file per PAM type. To use `Delinea-SecretServer-ClientCredentials`, replace the contents of `manifest.json` with the contents of `ClientCredentials-manifest.json` (for example, delete `manifest.json` and rename `ClientCredentials-manifest.json` to `manifest.json`). The file contains the following:
    ```json
    {
      "extensions": {
        "Keyfactor.Platform.Extensions.IPAMProvider": {
          "YOUR-PROVIDER-NAME": {
            "assemblyPath": "delinea-secretserver-pam.dll",
            "TypeFullName": "Keyfactor.Extensions.Pam.Delinea.SecretServerPamClientCredentials"
          }
        }
      },
      "Keyfactor:PAMProviders:YOUR-PROVIDER-NAME:InitializationInfo": {
        "Host": "https://example.secretservercloud.com/SecretServer",
        "ClientId": "<OAUTH_CLIENT_ID>",
        "ClientSecret": "<OAUTH_CLIENT_SECRET>"
      }
    }
    ```

3. Edit `manifest.json`:

    * Replace `YOUR-PROVIDER-NAME` (the extension entry key and the `InitializationInfo` section name) with the name of the PAM provider you created in Keyfactor Command for this type, that is, the name you gave the provider instance, not the PAM type name `Delinea-SecretServer-ClientCredentials`. The `extensions` entry key is that name itself, with no `PAMProviders.` prefix and no `.PAMProvider` suffix, and the configuration section uses the same name: `Keyfactor:PAMProviders:<provider name>:InitializationInfo`. For example, for a provider named `Delinea`, the `extensions` entry key is `"Delinea": { ... }` and the section is `"Keyfactor:PAMProviders:Delinea:InitializationInfo": { ... }`.
    * Replace the placeholder values in the section with the values collected in the [requirements](docs/delinea-secretserver-clientcredentials.md#requirements) section.
    * Do not leave any other `Keyfactor:PAMProviders:*:InitializationInfo` sections in `manifest.json` (for example, sections left over from the 1.3.0 manifest). In testing, sample sections left in `manifest.json` supplied their placeholder values to the plugin in place of the real ones.

    > [!IMPORTANT]
    > Update placeholder values `YOUR-PROVIDER-NAME`. This needs to match the name of the provider instance defined in
    > Keyfactor Command, if it does not you may encounter failed with `No extension configuration found 
    > for 'Keyfactor.Platform.Extensions.IPAMProvider' with name '<provider name>'` followed by `No PAM provider registered 
    > with name <provider name>`.  
    

4. Restart the Universal Orchestrator service after editing `manifest.json`.

## Registration and Naming

The extension entry in the Universal Orchestrator `manifest.json` is keyed by the name of the PAM provider you created in Keyfactor Command (the provider instance name, not the PAM type name `Delinea-SecretServer-ClientCredentials`), with no `PAMProviders.` prefix and no `.PAMProvider` suffix. The `InitializationInfo` section uses the same name (`Keyfactor:PAMProviders:<provider name>:InitializationInfo`). The shipped placeholder for both is `YOUR-PROVIDER-NAME`, which must be replaced before the manifest is usable. Any provider name works as long as the extension key and the section name match the provider name in Command. The manifest contains only this one section; no separate section named after the PAM type is needed.

This type always uses the `client_credentials` flow. A `GrantType` value is not needed and is ignored. If the type-specific class is not the one registered in `manifest.json`, the Universal Orchestrator loads the generic provider, which defaults to `password`.

> [!IMPORTANT]
> A Universal Orchestrator supports only **one** PAM provider registration per `manifest.json` (until UO v26.4), so only one of the manifest files can be installed as `manifest.json` on a given orchestrator. Using more than one Delinea PAM type requires a separate Universal Orchestrator for each.

### Universal Orchestrator

The release ships this registration as `ClientCredentials-manifest.json`; rename it to `manifest.json` on install. It contains exactly this registration and this `InitializationInfo` section:

```json
{
  "extensions": {
    "Keyfactor.Platform.Extensions.IPAMProvider": {
      "YOUR-PROVIDER-NAME": {
        "assemblyPath": "delinea-secretserver-pam.dll",
        "TypeFullName": "Keyfactor.Extensions.Pam.Delinea.SecretServerPamClientCredentials"
      }
    }
  },
  "Keyfactor:PAMProviders:YOUR-PROVIDER-NAME:InitializationInfo": {
    "Host": "https://example.secretservercloud.com/SecretServer",
    "ClientId": "<OAUTH_CLIENT_ID>",
    "ClientSecret": "<OAUTH_CLIENT_SECRET>"
  }
}
```

### Keyfactor Command host (Command 10 `web.config`)

```xml
<register type="IPAMProvider" mapTo="Keyfactor.Extensions.Pam.Delinea.SecretServerPamClientCredentials, delinea-secretserver-pam" name="Delinea-SecretServer-ClientCredentials" />
```

The assembly name in `mapTo` is the name of the extension DLL (`delinea-secretserver-pam`), and the class is the one for this PAM type.
