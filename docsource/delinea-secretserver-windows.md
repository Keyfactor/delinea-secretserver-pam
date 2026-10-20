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

2. The release contains one manifest file per PAM type. To use `Delinea-SecretServer-Windows`, replace the contents of `manifest.json` with the contents of `Windows-manifest.json` (for example, delete `manifest.json` and rename `Windows-manifest.json` to `manifest.json`). The file contains the following:
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
      },
      "Keyfactor:PAMProviders:Delinea-SecretServer-Windows:InitializationInfo": {
        "Host": "https://example.secretserver.internal/SecretServer"
      }
    }
    ```

3. Edit `manifest.json`:

    * Replace `YOUR-PROVIDER-NAME` (the extension entry key and the first `InitializationInfo` section name) with the name of the PAM provider you created in Keyfactor Command for this type, that is, the name you gave the provider instance, not the PAM type name `Delinea-SecretServer-Windows`. The `extensions` entry key is that name itself, with no `PAMProviders.` prefix and no `.PAMProvider` suffix, and the first configuration section uses the same name: `Keyfactor:PAMProviders:<provider name>:InitializationInfo`. For example, for a provider named `Delinea`, the `extensions` entry key is `"Delinea": { ... }` and the section is `"Keyfactor:PAMProviders:Delinea:InitializationInfo": { ... }`.
    * Keep the section named `Keyfactor:PAMProviders:Delinea-SecretServer-Windows:InitializationInfo`. Its name is the PAM type (the provider class name), which the plugin's in-extension lookup uses. Its values must match the provider-named section.
    * Replace the placeholder values in both sections with the values collected in the [requirements](docs/delinea-secretserver-windows.md#requirements) section.
    * Do not leave any other `Keyfactor:PAMProviders:*:InitializationInfo` sections in `manifest.json` (for example, sections left over from the 1.3.0 manifest). In testing, sample sections left in `manifest.json` supplied their placeholder values to the plugin in place of the real ones.

    > [!NOTE]
    > Observed on a Universal Orchestrator (K8SJKS store, store password bound to the provider, named PAM resolver): an extension entry keyed `PAMProviders.<provider name>.PAMProvider` failed with `No extension configuration found for 'Keyfactor.Platform.Extensions.IPAMProvider' with name '<provider name>'` followed by `No PAM provider registered with name <provider name>`. Keying the extension entry by the bare provider name (for example `Delinea`), together with the `Keyfactor:PAMProviders:<provider name>:InitializationInfo` section, cleared the error and the plugin ran with the supplied configuration. Other key shapes were not tested. A placeholder key in angle brackets made an orchestrator fail to start in the tested environment (observed once; the cause was not established), which is why the shipped placeholder is the bracket-free `YOUR-PROVIDER-NAME`.

4. Restart the Universal Orchestrator service after editing `manifest.json`.

> [!IMPORTANT]
> A Universal Orchestrator supports only **one** PAM provider registration per `manifest.json` (until UO v26.4), so only one of the manifest files can be installed as `manifest.json` on a given orchestrator. Using more than one Delinea PAM type requires a separate Universal Orchestrator for each.

## Registration and Naming

The extension entry in the Universal Orchestrator `manifest.json` is keyed by the name of the PAM provider you created in Keyfactor Command (the provider instance name, not the PAM type name `Delinea-SecretServer-Windows`), with no `PAMProviders.` prefix and no `.PAMProvider` suffix. The first `InitializationInfo` section uses the same name. The shipped placeholder for both is `YOUR-PROVIDER-NAME`, which must be replaced before the manifest is usable. The second section, named after the PAM type `Delinea-SecretServer-Windows`, is kept as shipped. Any provider name works as long as the extension key and the first section name match the provider name in Command.

This type always uses Integrated Windows Authentication. A `GrantType` value is not needed and is ignored.

> [!IMPORTANT]
> A Universal Orchestrator supports only **one** PAM provider registration per `manifest.json` (until UO v26.4), so only one of the manifest files can be installed as `manifest.json` on a given orchestrator. Using more than one Delinea PAM type requires a separate Universal Orchestrator for each.

### Universal Orchestrator

The release ships this registration as `Windows-manifest.json`; rename it to `manifest.json` on install. It contains exactly this registration and these `InitializationInfo` sections:

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
  },
  "Keyfactor:PAMProviders:Delinea-SecretServer-Windows:InitializationInfo": {
    "Host": "https://example.secretserver.internal/SecretServer"
  }
}
```

### Keyfactor Command host (Command 10 `web.config`)

```xml
<register type="IPAMProvider" mapTo="Keyfactor.Extensions.Pam.Delinea.SecretServerPamWindows, delinea-secretserver-pam" name="Delinea-SecretServer-Windows" />
```

The assembly name in `mapTo` is the name of the extension DLL (`delinea-secretserver-pam`), and the class is the one for this PAM type.
