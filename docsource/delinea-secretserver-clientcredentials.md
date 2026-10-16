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
      },
      "Keyfactor:PAMProviders:Delinea-SecretServer-ClientCredentials:InitializationInfo": {
        "Host": "https://example.secretservercloud.com/SecretServer",
        "ClientId": "<OAUTH_CLIENT_ID>",
        "ClientSecret": "<OAUTH_CLIENT_SECRET>"
      }
    }
    ```

3. Edit `manifest.json`:

    * Replace `YOUR-PROVIDER-NAME` (the extension entry key and the first `InitializationInfo` section name) with the name of the PAM provider you created in Keyfactor Command for this type, that is, the name you gave the provider instance, not the PAM type name `Delinea-SecretServer-ClientCredentials`. The `extensions` entry key is that name itself, with no `PAMProviders.` prefix and no `.PAMProvider` suffix, and the first configuration section uses the same name: `Keyfactor:PAMProviders:<provider name>:InitializationInfo`. For example, for a provider named `Delinea`, the `extensions` entry key is `"Delinea": { ... }` and the section is `"Keyfactor:PAMProviders:Delinea:InitializationInfo": { ... }`.
    * Keep the section named `Keyfactor:PAMProviders:Delinea-SecretServer-ClientCredentials:InitializationInfo`. Its name is the PAM type (the provider class name), which the plugin's in-extension lookup uses. Its values must match the provider-named section.
    * Replace the placeholder values in both sections with the values collected in the [requirements](docs/delinea-secretserver-clientcredentials.md#requirements) section.
    * Remove any `Keyfactor:PAMProviders:*:InitializationInfo` sample sections you do not use. In testing, sample sections left in `manifest.json` supplied their placeholder values to the plugin in place of the real ones.

    > [!NOTE]
    > Observed on a Universal Orchestrator (K8SJKS store, store password bound to the provider, named PAM resolver): an extension entry keyed `PAMProviders.<provider name>.PAMProvider` failed with `No extension configuration found for 'Keyfactor.Platform.Extensions.IPAMProvider' with name '<provider name>'` followed by `No PAM provider registered with name <provider name>`. Keying the extension entry by the bare provider name (for example `Delinea`), together with the `Keyfactor:PAMProviders:<provider name>:InitializationInfo` section, cleared the error and the plugin ran with the supplied configuration. Other key shapes were not tested. A placeholder key in angle brackets made an orchestrator fail to start in the tested environment (observed once; the cause was not established), which is why the shipped placeholder is the bracket-free `YOUR-PROVIDER-NAME`.

    The same values can alternatively be supplied as environment variables of the form `Keyfactor__PAMProviders__<provider name>__InitializationInfo__<Key>` on the orchestrator host.

4. Restart the Universal Orchestrator service after editing `manifest.json`.
