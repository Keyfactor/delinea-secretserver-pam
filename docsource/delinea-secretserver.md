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

2. This provider's `Delinea-SecretServer` type uses the `manifest.json` file as shipped; no rename is needed. (The release also contains `Password-manifest.json`, `ClientCredentials-manifest.json` and `Windows-manifest.json` for the other PAM types.) The file contains the following:
    ```json
    {
      "extensions": {
        "Keyfactor.Platform.Extensions.IPAMProvider": {
          "PAMProviders.<COMMAND-PAM-PROVIDER-NAME>.PAMProvider": {
            "assemblyPath": "delinea-secretserver-pam.dll",
            "TypeFullName": "Keyfactor.Extensions.Pam.Delinea.SecretServerPam"
          }
        }
      },
      "Keyfactor:PAMProviders:<COMMAND-PAM-PROVIDER-NAME>:InitializationInfo": {
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

3. Edit `manifest.json`:

    * Replace every `<COMMAND-PAM-PROVIDER-NAME>` (the extension entry key and the first `InitializationInfo` section) with the exact name of the PAM *provider* you create in Keyfactor Command for this type, that is, the name you give the provider instance, not the PAM type name `Delinea-SecretServer`. The `extensions` entry key must read `PAMProviders.<provider name>.PAMProvider`, and the section must read `Keyfactor:PAMProviders:<provider name>:InitializationInfo`.
    * Keep the section named `Keyfactor:PAMProviders:Delinea-SecretServer:InitializationInfo`. Its name is the PAM type (the provider class name), which the plugin's in-extension lookup uses. Its values must match the provider-named section.
    * Replace the placeholder values in both sections with the values collected in the [requirements](docs/delinea-secretserver.md#requirements) section.
    * Remove any `Keyfactor:PAMProviders:*:InitializationInfo` sample sections you do not use. In testing, sample sections left in `manifest.json` supplied their placeholder values to the plugin in place of the real ones.

    > [!NOTE]
    > Observed on a Universal Orchestrator: a certificate store field resolved through a PAM provider failed with `No PAM provider registered with name <provider name>` when the extension entry was keyed by the PAM type name instead of the PAM provider name. The orchestrator resolves the provider by the name of the provider you created in Command, so use that name for the extension entry and its `InitializationInfo` section.

    The same values can alternatively be supplied as environment variables of the form `Keyfactor__PAMProviders__<provider name>__InitializationInfo__<Key>` on the orchestrator host.

4. Restart the Universal Orchestrator service after editing `manifest.json`.
