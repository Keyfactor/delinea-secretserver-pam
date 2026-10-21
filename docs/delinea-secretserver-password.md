## Delinea-SecretServer-Password

The `Delinea-SecretServer-Password` PAM type authenticates to Delinea Secret Server using a username and password
(OAuth2 `password` grant). This is the recommended type for environments where a service account with a username
and password is used.

## Requirements

- Delinea Secret Server instance accessible over HTTPS from the host running Keyfactor Command or the Universal Orchestrator.
- A service account with a username and password that has permission to view the secrets being retrieved. See the
  [Delinea Secret Server documentation](https://docs.delinea.com/online-help/secret-server/api-scripting/authentication/script-token-auth/index.htm)
  for information on configuring service accounts.

The `Delinea-SecretServer-Password` PAM type authenticates to Delinea Secret Server using a username and password
(OAuth2 `password` grant). This is the recommended type for environments where a service account with a username
and password is used.

- Delinea Secret Server instance accessible over HTTPS from the host running Keyfactor Command or the Universal Orchestrator.
- A service account with a username and password that has permission to view the secrets being retrieved. See the
  [Delinea Secret Server documentation](https://docs.delinea.com/online-help/secret-server/api-scripting/authentication/script-token-auth/index.htm)
  for information on configuring service accounts.

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

2. The release contains one manifest file per PAM type. To use `Delinea-SecretServer-Password`, replace the contents of `manifest.json` with the contents of `Password-manifest.json` (for example, delete `manifest.json` and rename `Password-manifest.json` to `manifest.json`). The file contains the following:
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

3. Edit `manifest.json`:

    * Replace `YOUR-PROVIDER-NAME` (the extension entry key and the `InitializationInfo` section name) with the name of the PAM provider you created in Keyfactor Command for this type, that is, the name you gave the provider instance, not the PAM type name `Delinea-SecretServer-Password`. The `extensions` entry key is that name itself, with no `PAMProviders.` prefix and no `.PAMProvider` suffix, and the configuration section uses the same name: `Keyfactor:PAMProviders:<provider name>:InitializationInfo`. For example, for a provider named `Delinea`, the `extensions` entry key is `"Delinea": { ... }` and the section is `"Keyfactor:PAMProviders:Delinea:InitializationInfo": { ... }`.
    * Replace the placeholder values in the section with the values collected in the [requirements](docs/delinea-secretserver-password.md#requirements) section.
    * Do not leave any other `Keyfactor:PAMProviders:*:InitializationInfo` sections in `manifest.json` (for example, sections left over from the 1.3.0 manifest). In testing, sample sections left in `manifest.json` supplied their placeholder values to the plugin in place of the real ones.

    > [!NOTE]
    > Observed on a Universal Orchestrator (K8SJKS store, store password bound to the provider, named PAM resolver): an extension entry keyed `PAMProviders.<provider name>.PAMProvider` failed with `No extension configuration found for 'Keyfactor.Platform.Extensions.IPAMProvider' with name '<provider name>'` followed by `No PAM provider registered with name <provider name>`. Keying the extension entry by the bare provider name (for example `Delinea`), together with the `Keyfactor:PAMProviders:<provider name>:InitializationInfo` section, cleared the error and the plugin ran with the supplied configuration. Other key shapes were not tested. A placeholder key in angle brackets made an orchestrator fail to start in the tested environment (observed once; the cause was not established), which is why the shipped placeholder is the bracket-free `YOUR-PROVIDER-NAME`.

4. Restart the Universal Orchestrator service after editing `manifest.json`.

> [!IMPORTANT]
> A Universal Orchestrator supports only **one** PAM provider registration per `manifest.json` (until UO v26.4), so only one of the manifest files can be installed as `manifest.json` on a given orchestrator. Using more than one Delinea PAM type requires a separate Universal Orchestrator for each.

## Registration and Naming

The extension entry in the Universal Orchestrator `manifest.json` is keyed by the name of the PAM provider you created in Keyfactor Command (the provider instance name, not the PAM type name `Delinea-SecretServer-Password`), with no `PAMProviders.` prefix and no `.PAMProvider` suffix. The `InitializationInfo` section uses the same name (`Keyfactor:PAMProviders:<provider name>:InitializationInfo`). The shipped placeholder for both is `YOUR-PROVIDER-NAME`, which must be replaced before the manifest is usable. Any provider name works as long as the extension key and the section name match the provider name in Command. The manifest contains only this one section; no separate section named after the PAM type is needed.

This type always uses the `password` grant. A `GrantType` value is not needed and is ignored.

> [!IMPORTANT]
> A Universal Orchestrator supports only **one** PAM provider registration per `manifest.json` (until UO v26.4), so only one of the manifest files can be installed as `manifest.json` on a given orchestrator. Using more than one Delinea PAM type requires a separate Universal Orchestrator for each.

### Universal Orchestrator

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

### Keyfactor Command host (Command 10 `web.config`)

```xml
<register type="IPAMProvider" mapTo="Keyfactor.Extensions.Pam.Delinea.SecretServerPamPassword, delinea-secretserver-pam" name="Delinea-SecretServer-Password" />
```

The assembly name in `mapTo` is the name of the extension DLL (`delinea-secretserver-pam`), and the class is the one for this PAM type.

