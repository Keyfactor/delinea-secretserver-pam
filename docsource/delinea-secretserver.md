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
- `GrantType` is optional and defaults to `password` when omitted. It is case-sensitive and must be exactly `password`, `client_credentials` or `windows`; the shipped `manifest.json` placeholder `password|client_credentials|windows` must be replaced with one of these.
- The `SecretFieldName` value must match a field's name or slug on the secret exactly (the comparison is case-sensitive). An empty value, or a field that does not exist on the secret, is an error.

**Dummy values (`N/A`).** The Keyfactor Command UI requires all declared fields of this type to be populated. For
fields that do not apply to the chosen authentication flow, enter `N/A` as the value. The provider treats `N/A`
(case-insensitive) as an empty value and does not use it. The type-specific PAM types avoid this because they only
expose the fields relevant to their authentication flow.

## Platform Install

1. On the server that hosts Keyfactor Command, download and unzip the latest release of the Delinea Secret Server PAM Provider from the [Releases](../../releases) page.

2. Copy the assemblies to the appropriate directories on the Keyfactor Command server:

    <details><summary>Keyfactor Command 11+</summary>

    1. Copy the unzipped assemblies to each of the following directories:

        * `C:\Program Files\Keyfactor\Keyfactor Platform\WebAgentServices\Extensions\delinea-secretserver-pam`
        * `C:\Program Files\Keyfactor\Keyfactor Platform\WebConsole\Extensions\delinea-secretserver-pam`
        * `C:\Program Files\Keyfactor\Keyfactor Platform\KeyfactorAPI\Extensions\delinea-secretserver-pam`

    </details>

    <details><summary>Keyfactor Command 10</summary>

    1. Copy the assemblies to each of the following directories:

        * `C:\Program Files\Keyfactor\Keyfactor Platform\WebAgentServices\bin\delinea-secretserver-pam`
        * `C:\Program Files\Keyfactor\Keyfactor Platform\KeyfactorAPI\bin\delinea-secretserver-pam`
        * `C:\Program Files\Keyfactor\Keyfactor Platform\WebConsole\bin\delinea-secretserver-pam`
        * `C:\Program Files\Keyfactor\Keyfactor Platform\Service\delinea-secretserver-pam`

    2. Open a text editor on the Keyfactor Command server as an administrator and open the `web.config` file located in the `WebAgentServices` directory.

    3. In the `web.config` file, locate the `<container> </container>` section and add the following registration:

        ```xml
        <container>
            ...
            <!--The following are PAM Provider registrations. Uncomment them to use them in the Keyfactor Product:-->

            <!--Add the following line exactly to register the PAM Provider-->
            <register type="IPAMProvider" mapTo="Keyfactor.Extensions.Pam.Delinea.SecretServerPam, delinea-secretserver-pam" name="Delinea-SecretServer" />
        </container>
        ```

    4. Repeat steps 2 and 3 for each of the directories listed in step 1. The configuration files are located in the following paths by default:

        * `C:\Program Files\Keyfactor\Keyfactor Platform\WebAgentServices\web.config`
        * `C:\Program Files\Keyfactor\Keyfactor Platform\KeyfactorAPI\web.config`
        * `C:\Program Files\Keyfactor\Keyfactor Platform\WebConsole\web.config`
        * `C:\Program Files\Keyfactor\Keyfactor Platform\Service\CMSTimerService.exe.config`

    </details>

3. Restart the Keyfactor Command services (`iisreset`).

## Orchestrator Install

> [!NOTE]
> Before installing, create the PAM type and define a provider instance in Keyfactor Command, as described in the sections above for this PAM type and in [Usage](#usage).

1. Install the Delinea Secret Server PAM Provider assemblies as described in Step 1 of [Installing on a Universal Orchestrator](#installing-on-a-universal-orchestrator).

2. The `Delinea-SecretServer` type uses the `manifest.json` file as shipped; no rename is needed. (The release also contains `Password-manifest.json`, `ClientCredentials-manifest.json` and `Windows-manifest.json` for the other PAM types.) The file contents are shown in [Delinea-SecretServer Registration Snippets](#delinea-secretserver-registration-snippets).

3. Edit `manifest.json` as described in Step 3 of [Installing on a Universal Orchestrator](#installing-on-a-universal-orchestrator): replace `YOUR-PROVIDER-NAME` with the name of your PAM provider in Keyfactor Command and fill in the placeholder values from the [requirements](docs/delinea-secretserver.md#requirements) section.

4. Restart the Universal Orchestrator service after editing `manifest.json`.

## Delinea-SecretServer Registration Snippets

Shared registration and naming rules are described in [Registration and Naming](/README.md#registration-and-naming).

This generic type selects its authentication flow from `GrantType` and defaults to `password` when it is not supplied. For new installations prefer the type-specific PAM types.

> [!IMPORTANT]
> A Universal Orchestrator supports only **one** PAM provider registration per `manifest.json` (until UO v26.4). The shipped `manifest.json` registers this generic type. To use a type-specific provider instead, replace `manifest.json` with that type's manifest file. Using more than one Delinea PAM type requires a separate Universal Orchestrator for each.

### Generic: Universal Orchestrator manifest

The release ships this registration as `manifest.json`. It contains exactly this registration and this `InitializationInfo` section:

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
  }
}
```

### Generic: Command 10 `web.config` registration

```xml
<register type="IPAMProvider" mapTo="Keyfactor.Extensions.Pam.Delinea.SecretServerPam, delinea-secretserver-pam" name="Delinea-SecretServer" />
```

The assembly name in `mapTo` is the name of the extension DLL (`delinea-secretserver-pam`), and the class is the one for this PAM type.
