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
          "<COMMAND-PAM-PROVIDER-NAME>": {
            "assemblyPath": "delinea-secretserver-pam.dll",
            "TypeFullName": "Keyfactor.Extensions.Pam.Delinea.SecretServerPamWindows"
          }
        }
      },
      "Keyfactor:PAMProviders:<COMMAND-PAM-PROVIDER-NAME>:InitializationInfo": {
        "Host": "https://example.secretserver.internal/SecretServer"
      },
      "Keyfactor:PAMProviders:Delinea-SecretServer-Windows:InitializationInfo": {
        "Host": "https://example.secretserver.internal/SecretServer"
      }
    }
    ```

3. Edit `manifest.json`:

    * Replace every `<COMMAND-PAM-PROVIDER-NAME>` (the extension entry key and the first `InitializationInfo` section) with the exact name of the PAM *provider* you create in Keyfactor Command for this type, that is, the name you give the provider instance, not the PAM type name `Delinea-SecretServer-Windows`. The `extensions` entry key is the provider name itself, with no `PAMProviders.` prefix and no `.PAMProvider` suffix, and the configuration section name uses the same provider name: `Keyfactor:PAMProviders:<provider name>:InitializationInfo`. For example, for a provider named `Delinea`, the `extensions` entry key is `"Delinea": { ... }` and the section is `"Keyfactor:PAMProviders:Delinea:InitializationInfo": { ... }`.
    * Keep the section named `Keyfactor:PAMProviders:Delinea-SecretServer-Windows:InitializationInfo`. Its name is the PAM type (the provider class name), which the plugin's in-extension lookup uses. Its values must match the provider-named section.
    * Replace the placeholder values in both sections with the values collected in the [requirements](docs/delinea-secretserver-windows.md#requirements) section.
    * Remove any `Keyfactor:PAMProviders:*:InitializationInfo` sample sections you do not use. In testing, sample sections left in `manifest.json` supplied their placeholder values to the plugin in place of the real ones.

    > [!NOTE]
    > Observed on a Universal Orchestrator (K8SJKS store, store password bound to the provider, named PAM resolver): an extension entry keyed `PAMProviders.<provider name>.PAMProvider` failed with `No extension configuration found for 'Keyfactor.Platform.Extensions.IPAMProvider' with name '<provider name>'` followed by `No PAM provider registered with name <provider name>`. Keying the extension entry by the bare provider name (for example `Delinea`), together with the `Keyfactor:PAMProviders:<provider name>:InitializationInfo` section, cleared the error and the plugin ran with the supplied configuration. Other key shapes were not tested.

    The same values can alternatively be supplied as environment variables of the form `Keyfactor__PAMProviders__<provider name>__InitializationInfo__<Key>` on the orchestrator host.

4. Restart the Universal Orchestrator service after editing `manifest.json`.
