## Delinea-SecretServer-ClientCredentials

The `Delinea-SecretServer-ClientCredentials` PAM type authenticates to Delinea Secret Server with a Client ID and
Client Secret. This is the recommended type for service-to-service integrations that use an application account or an
SDK client account instead of a user account.

> [!WARNING]
> **The token request depends on the kind of account your Client ID belongs to.** Secret Server has two kinds of
> client credentials, and they authenticate differently:
>
> * **SDK client account** (`ClientId` starts with `sdk-client-`): the provider sends
>   `grant_type=client_credentials` with `client_id` and `client_secret`. Enter the Client ID exactly as Secret Server
>   issued it, **including** the `sdk-client-` prefix. Requires v1.3.1 or later; earlier versions fail with HTTP 400
>   `{"error":"Login failed."}`.
> * **Application account** (any other `ClientId`): the provider sends `grant_type=password` with the Client ID as
>   `username` and the Client Secret as `password`, as in earlier versions.
>
> See [Client Credentials: Application Accounts vs SDK Client Accounts](/README.md#client-credentials-application-accounts-vs-sdk-client-accounts)
> for how the two kinds differ, where each comes from, and links to the Delinea documentation.

## Requirements

- Delinea Secret Server instance accessible over HTTPS from the host running Keyfactor Command or the Universal Orchestrator.
- One of the following, with permission to view the secrets being retrieved:
  - An **application account**: `ClientId` is its username and `ClientSecret` its password. See
    [User Settings](https://docs.delinea.com/online-help/secret-server/users-roles/users/user-settings/index.htm) and
    [Script Authentication Using Tokens](https://docs.delinea.com/online-help/secret-server/api-scripting/authentication/script-token-auth/index.htm).
  - An **SDK client account**: `ClientId` is the issued `sdk-client-…` ID and `ClientSecret` the issued client secret.
    The secret must be shared with the application account the SDK client onboarding rule is tied to. See
    [Using the Secret Server SDK for DevOps](https://docs.delinea.com/online-help/secret-server/api-scripting/sdk-devops/using-sdk/index.htm).
- The `SecretFieldName` value must match a field's name or slug on the secret exactly (the comparison is case-sensitive). An empty value, or a field that does not exist on the secret, is an error.

The `Delinea-SecretServer-ClientCredentials` PAM type authenticates to Delinea Secret Server with a Client ID and
Client Secret. This is the recommended type for service-to-service integrations that use an application account or an
SDK client account instead of a user account.

> [!WARNING]
> **The token request depends on the kind of account your Client ID belongs to.** Secret Server has two kinds of
> client credentials, and they authenticate differently:
>
> * **SDK client account** (`ClientId` starts with `sdk-client-`): the provider sends
>   `grant_type=client_credentials` with `client_id` and `client_secret`. Enter the Client ID exactly as Secret Server
>   issued it, **including** the `sdk-client-` prefix. Requires v1.3.1 or later; earlier versions fail with HTTP 400
>   `{"error":"Login failed."}`.
> * **Application account** (any other `ClientId`): the provider sends `grant_type=password` with the Client ID as
>   `username` and the Client Secret as `password`, as in earlier versions.
>
> See [Client Credentials: Application Accounts vs SDK Client Accounts](/README.md#client-credentials-application-accounts-vs-sdk-client-accounts)
> for how the two kinds differ, where each comes from, and links to the Delinea documentation.

- Delinea Secret Server instance accessible over HTTPS from the host running Keyfactor Command or the Universal Orchestrator.
- One of the following, with permission to view the secrets being retrieved:
  - An **application account**: `ClientId` is its username and `ClientSecret` its password. See
    [User Settings](https://docs.delinea.com/online-help/secret-server/users-roles/users/user-settings/index.htm) and
    [Script Authentication Using Tokens](https://docs.delinea.com/online-help/secret-server/api-scripting/authentication/script-token-auth/index.htm).
  - An **SDK client account**: `ClientId` is the issued `sdk-client-…` ID and `ClientSecret` the issued client secret.
    The secret must be shared with the application account the SDK client onboarding rule is tied to. See
    [Using the Secret Server SDK for DevOps](https://docs.delinea.com/online-help/secret-server/api-scripting/sdk-devops/using-sdk/index.htm).
- The `SecretFieldName` value must match a field's name or slug on the secret exactly (the comparison is case-sensitive). An empty value, or a field that does not exist on the secret, is an error.

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
            <register type="IPAMProvider" mapTo="Keyfactor.Extensions.Pam.Delinea.SecretServerPamClientCredentials, delinea-secretserver-pam" name="Delinea-SecretServer-ClientCredentials" />
        </container>
        ```

    4. Repeat steps 2 and 3 for each of the directories listed in step 1. The configuration files are located in the following paths by default:

        * `C:\Program Files\Keyfactor\Keyfactor Platform\WebAgentServices\web.config`
        * `C:\Program Files\Keyfactor\Keyfactor Platform\KeyfactorAPI\web.config`
        * `C:\Program Files\Keyfactor\Keyfactor Platform\WebConsole\web.config`
        * `C:\Program Files\Keyfactor\Keyfactor Platform\Service\CMSTimerService.exe.config`

    </details>

3. Restart the Keyfactor Command services (`iisreset`).

> [!NOTE]
> Before installing, create the PAM type and define a provider instance in Keyfactor Command, as described in the sections above for this PAM type and in [Usage](#usage).

1. Install the Delinea Secret Server PAM Provider assemblies as described in Step 1 of [Installing on a Universal Orchestrator](#installing-on-a-universal-orchestrator).

2. To use `Delinea-SecretServer-ClientCredentials`, replace the contents of `manifest.json` with the contents of `ClientCredentials-manifest.json` (for example, delete `manifest.json` and rename `ClientCredentials-manifest.json` to `manifest.json`). The file contents are shown in [Delinea-SecretServer-ClientCredentials Registration Snippets](#delinea-secretserver-clientcredentials-registration-snippets).

3. Edit `manifest.json` as described in Step 3 of [Installing on a Universal Orchestrator](#installing-on-a-universal-orchestrator): replace `YOUR-PROVIDER-NAME` with the name of your PAM provider in Keyfactor Command and fill in the placeholder values from the [requirements](docs/delinea-secretserver-clientcredentials.md#requirements) section.

4. Restart the Universal Orchestrator service after editing `manifest.json`.

## Delinea-SecretServer-ClientCredentials Registration Snippets

Shared registration and naming rules are described in [Registration and Naming](/README.md#registration-and-naming).

This type always uses the `client_credentials` flow. A `GrantType` value is not needed and is ignored. If the type-specific class is not the one registered in `manifest.json`, the Universal Orchestrator loads the generic provider, which defaults to `password`.

### ClientCredentials: Universal Orchestrator manifest

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

### ClientCredentials: Command 10 `web.config` registration

```xml
<register type="IPAMProvider" mapTo="Keyfactor.Extensions.Pam.Delinea.SecretServerPamClientCredentials, delinea-secretserver-pam" name="Delinea-SecretServer-ClientCredentials" />
```

The assembly name in `mapTo` is the name of the extension DLL (`delinea-secretserver-pam`), and the class is the one for this PAM type.

