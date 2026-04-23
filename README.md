<h1 align="center" style="border-bottom: none">
    Delinea Secret Server PAM Provider
</h1>

<p align="center">
  <!-- Badges -->
<img src="https://img.shields.io/badge/integration_status-production-3D1973?style=flat-square" alt="Integration Status: production" />
<a href="https://github.com/Keyfactor/delinea-secretserver-pam/releases"><img src="https://img.shields.io/github/v/release/Keyfactor/delinea-secretserver-pam?style=flat-square" alt="Release" /></a>
<img src="https://img.shields.io/github/issues/Keyfactor/delinea-secretserver-pam?style=flat-square" alt="Issues" />
<img src="https://img.shields.io/github/downloads/Keyfactor/delinea-secretserver-pam/total?style=flat-square&label=downloads&color=28B905" alt="GitHub Downloads (all assets, all releases)" />
</p>

<p align="center">
  <!-- TOC -->
  <a href="#support">
    <b>Support</b>
  </a> 
  ·
  <a href="#getting-started">
    <b>Installation</b>
  </a>
  ·
  <a href="#license">
    <b>License</b>
  </a>
  ·
  <a href="https://github.com/orgs/Keyfactor/repositories?q=pam">
    <b>Related Integrations</b>
  </a>
</p>

## Overview

The Delinea Secret Server PAM Provider allows for the retrieval of stored account credentials from a Delinea Secret
Server secret. Three authentication methods are supported: `password` (username/password), `client_credentials`
(OAuth2 application account), and `windows` (Integrated Windows Authentication).

## PAM Types

This provider ships four PAM types. For new installations, use the type-specific variants — they only expose the
fields relevant to the chosen authentication flow, which simplifies configuration in the Keyfactor Command UI.

| PAM Type | Auth Method | Server Parameters |
| --- | --- | --- |
| `Delinea-SecretServer-Password` | Username + Password | `Host`, `Username`, `Password` |
| `Delinea-SecretServer-ClientCredentials` | OAuth2 Client Credentials | `Host`, `ClientId`, `ClientSecret` |
| `Delinea-SecretServer-Windows` | Integrated Windows Authentication | `Host` |
| `Delinea-SecretServer` | Any (selected via `GrantType`) | `Host`, plus credentials for the chosen grant type |

> [!NOTE]
> `Delinea-SecretServer` is the original backwards-compatible type retained for existing installations. It requires
> a `GrantType` field and exposes all credential fields in the Keyfactor Command UI regardless of which grant type
> is active. Existing installations do not need to change.

## Support
The Delinea Secret Server PAM Provider is supported by Keyfactor for Keyfactor customers. If you have a support issue, please open a support ticket with your Keyfactor representative. If you have a support issue, please open a support ticket via the Keyfactor Support Portal at https://support.keyfactor.com. 

> To report a problem or suggest a new feature, use the **[Issues](../../issues)** tab. If you want to contribute actual bug fixes or proposed enhancements, use the **[Pull requests](../../pulls)** tab.

## Getting Started

The Delinea Secret Server PAM Provider is used by Command to resolve PAM-eligible credentials for Universal Orchestrator extensions and for accessing Certificate Authorities. When configured, Command will use the Delinea Secret Server PAM Provider to retrieve credentials needed to communicate with the target system. There are two ways to install the Delinea Secret Server PAM Provider, and you may elect to use one or both methods:

1. **Locally on the Keyfactor Command server**: PAM credential resolution via the Delinea Secret Server PAM Provider will occur on the Keyfactor Command server each time an elegible credential is needed.
2. **Remotely On Universal Orchestrators**: When Jobs are dispatched to Universal Orchestrators, the associated Certificate Store extension assembly will use the Delinea Secret Server PAM Provider to resolve eligible PAM credentials.

Before proceeding with installation, you should consider which pattern is best for your requirements and use case.

### Installation

> [!IMPORTANT]
> For the most up-to-date and complete documentation on how to install a PAM provider extension, please visit our [product documentation](https://software.keyfactor.com/Core-OnPrem/Current/Content/ReferenceGuide/Preparing%20Third%20Party%20PAM%20Providers%20to%20Work%20with.htm?Highlight=pam%20provider#InstallingCustomPAMProviderExtensions)


To install Delinea Secret Server PAM Provider, it is recommended you install [kfutil](https://github.com/Keyfactor/kfutil). `kfutil` is a command-line tool that simplifies the process of creating PAM Types in Keyfactor Command.

The Delinea Secret Server PAM Provider implements 4 PAM Types. Depending on your use case, you may elect to install one, or all of these PAM Types. An overview for each type is linked below:
* [Delinea-SecretServer](docs/delinea-secretserver.md)
* [Delinea-SecretServer-Password](docs/delinea-secretserver-password.md)
* [Delinea-SecretServer-ClientCredentials](docs/delinea-secretserver-clientcredentials.md)
* [Delinea-SecretServer-Windows](docs/delinea-secretserver-windows.md)






<details><summary>Delinea-SecretServer</summary>


#### Requirements
   - Delinea Secret Server instance accessible over HTTPS from the host running Keyfactor Command or the Universal Orchestrator.
   - A service account or application account with permission to view the secrets being retrieved. See the
     [Delinea Secret Server documentation](https://docs.delinea.com/online-help/secret-server/api-scripting/authentication/script-token-auth/index.htm)
     for information on configuring service accounts and application accounts.

#### Create PAM type in Keyfactor Command


##### Using `kfutil`
Create the required PAM Types in the connected Command platform.

```shell
# Delinea-SecretServer
kfutil pam types-create -r delinea-secretserver-pam -n Delinea-SecretServer
```

##### Using the API
For full API docs please visit our [product documentation](https://software.keyfactor.com/Core-OnPrem/Current/Content/WebAPI/KeyfactorAPI/PAMProvidersPOSTTypes.htm?Highlight=pam%20type)

Below is the payload to `POST` to the Keyfactor Command API
```json
{
    "Name": "Delinea-SecretServer",
    "Parameters": [
        {
            "Name": "Host",
            "DisplayName": "Secret Server URL",
            "Description": "The URL to the Secret Server instance. Example: https://example.secretservercloud.com/SecretServer",
            "DataType": 1,
            "InstanceLevel": false
        },
        {
            "Name": "Username",
            "DisplayName": "Secret Server Username",
            "Description": "The username used to authenticate to the Secret Server instance. NOTE: only applicable if using the `password` grant type.",
            "DataType": 1,
            "InstanceLevel": false
        },
        {
            "Name": "Password",
            "DisplayName": "Secret Server Password",
            "Description": "The password used to authenticate to the Secret Server instance. NOTE: only applicable if using the `password` grant type.",
            "DataType": 2,
            "InstanceLevel": false
        },
        {
            "Name": "ClientId",
            "DisplayName": "Secret Server Client ID",
            "Description": "The client ID used to authenticate to the Secret Server instance. NOTE: only applicable if using the `client_credentials` grant type.",
            "DataType": 1,
            "InstanceLevel": false
        },
        {
            "Name": "ClientSecret",
            "DisplayName": "Secret Server Client Secret",
            "Description": "The client secret used to authenticate to the Secret Server instance. NOTE: only applicable if using the `client_credentials` grant type.",
            "DataType": 2,
            "InstanceLevel": false
        },
        {
            "Name": "GrantType",
            "DisplayName": "Grant Type",
            "Description": "The grant type used to authenticate to the Secret Server instance. Valid values are `password`, `client_credentials`, or `windows`. Default is `password`. If not provided the default value `password` will be used to maintain backwards compatibility.",
            "DataType": 1,
            "InstanceLevel": false
        },
        {
            "Name": "SecretId",
            "DisplayName": "Secret ID",
            "Description": "The ID of the secret in Secret Server. This is the integer ID that is used to retrieve the secret from Secret Server.",
            "DataType": 1,
            "InstanceLevel": true
        },
        {
            "Name": "SecretFieldName",
            "DisplayName": "Secret Field Name",
            "Description": "The name of the field in the secret that contains the credential value. NOTE: The field must exist.",
            "DataType": 1,
            "InstanceLevel": true
        }
    ]
}
```

#### Install PAM provider on Keyfactor Command Host (Local)



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
            <register type="IPAMProvider" mapTo="Keyfactor.Extensions.Pam.Delinea.SecretServerPam, Keyfactor.Command.PAMProviders" name="Delinea-SecretServer" />
        </container>
        ```

    4. Repeat steps 2 and 3 for each of the directories listed in step 1. The configuration files are located in the following paths by default:

        * `C:\Program Files\Keyfactor\Keyfactor Platform\WebAgentServices\web.config`
        * `C:\Program Files\Keyfactor\Keyfactor Platform\KeyfactorAPI\web.config`
        * `C:\Program Files\Keyfactor\Keyfactor Platform\WebConsole\web.config`
        * `C:\Program Files\Keyfactor\Keyfactor Platform\Service\CMSTimerService.exe.config`

    </details>

3. Restart the Keyfactor Command services (`iisreset`).




#### Install PAM provider on a Universal Orchestrator Host (Remote)


1. Install the Delinea Secret Server PAM Provider assemblies.

    * **Using kfutil**: On the server that that hosts the Universal Orchestrator, run the following command:

        ```shell
        # Windows Server
        kfutil orchestrator extension -e delinea-secretserver-pam@latest --out "C:\Program Files\Keyfactor\Keyfactor Orchestrator\extensions"

        # Linux
        kfutil orchestrator extension -e delinea-secretserver-pam@latest --out "/opt/keyfactor/orchestrator/extensions"
        ```

    * **Manually**: Download the latest release of the Delinea Secret Server PAM Provider from the [Releases](../../releases) page. Extract the contents of the archive to:

        * **Windows Server**: `C:\Program Files\Keyfactor\Keyfactor Orchestrator\extensions\delinea-secretserver-pam`
        * **Linux**: `/opt/keyfactor/orchestrator/extensions/delinea-secretserver-pam`

2. Included in the release is a `manifest.json` file that contains the following object:
    ```json

    {
        "Keyfactor:PAMProviders:Delinea-SecretServer-Windows:InitializationInfo": {
            "Host": "https://example.secretserver.internal/SecretServer"
        }
    }

    ```

    Populate the fields in this object with credentials and configuration data collected in the [requirements](docs/delinea-secretserver.md#requirements) section.

3. Restart the Universal Orchestrator service.





</details>







<details><summary>Delinea-SecretServer-Password</summary>


#### Requirements
   - Delinea Secret Server instance accessible over HTTPS from the host running Keyfactor Command or the Universal Orchestrator.
   - A service account with a username and password that has permission to view the secrets being retrieved. See the
     [Delinea Secret Server documentation](https://docs.delinea.com/online-help/secret-server/api-scripting/authentication/script-token-auth/index.htm)
     for information on configuring service accounts.

#### Create PAM type in Keyfactor Command


##### Using `kfutil`
Create the required PAM Types in the connected Command platform.

```shell
# Delinea-SecretServer-Password
kfutil pam types-create -r delinea-secretserver-pam -n Delinea-SecretServer-Password
```

##### Using the API
For full API docs please visit our [product documentation](https://software.keyfactor.com/Core-OnPrem/Current/Content/WebAPI/KeyfactorAPI/PAMProvidersPOSTTypes.htm?Highlight=pam%20type)

Below is the payload to `POST` to the Keyfactor Command API
```json
{
    "Name": "Delinea-SecretServer-Password",
    "Parameters": [
        {
            "Name": "Host",
            "DisplayName": "Secret Server URL",
            "Description": "The URL to the Secret Server instance. Example: https://example.secretservercloud.com/SecretServer",
            "DataType": 1,
            "InstanceLevel": false
        },
        {
            "Name": "Username",
            "DisplayName": "Secret Server Username",
            "Description": "The username used to authenticate to the Secret Server instance.",
            "DataType": 1,
            "InstanceLevel": false
        },
        {
            "Name": "Password",
            "DisplayName": "Secret Server Password",
            "Description": "The password used to authenticate to the Secret Server instance.",
            "DataType": 2,
            "InstanceLevel": false
        },
        {
            "Name": "SkipTlsValidation",
            "DisplayName": "Skip TLS Validation",
            "Description": "Set to `true` to disable TLS certificate validation. Use only in non-production environments.",
            "DataType": 1,
            "InstanceLevel": false
        },
        {
            "Name": "SecretId",
            "DisplayName": "Secret ID",
            "Description": "The ID of the secret in Secret Server. This is the integer ID that is used to retrieve the secret from Secret Server.",
            "DataType": 1,
            "InstanceLevel": true
        },
        {
            "Name": "SecretFieldName",
            "DisplayName": "Secret Field Name",
            "Description": "The name of the field in the secret that contains the credential value. NOTE: The field must exist.",
            "DataType": 1,
            "InstanceLevel": true
        }
    ]
}
```

#### Install PAM provider on Keyfactor Command Host (Local)



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
            <register type="IPAMProvider" mapTo="Keyfactor.Extensions.Pam.Delinea.SecretServerPam, Keyfactor.Command.PAMProviders" name="Delinea-SecretServer-Password" />
        </container>
        ```

    4. Repeat steps 2 and 3 for each of the directories listed in step 1. The configuration files are located in the following paths by default:

        * `C:\Program Files\Keyfactor\Keyfactor Platform\WebAgentServices\web.config`
        * `C:\Program Files\Keyfactor\Keyfactor Platform\KeyfactorAPI\web.config`
        * `C:\Program Files\Keyfactor\Keyfactor Platform\WebConsole\web.config`
        * `C:\Program Files\Keyfactor\Keyfactor Platform\Service\CMSTimerService.exe.config`

    </details>

3. Restart the Keyfactor Command services (`iisreset`).




#### Install PAM provider on a Universal Orchestrator Host (Remote)


1. Install the Delinea Secret Server PAM Provider assemblies.

    * **Using kfutil**: On the server that that hosts the Universal Orchestrator, run the following command:

        ```shell
        # Windows Server
        kfutil orchestrator extension -e delinea-secretserver-pam@latest --out "C:\Program Files\Keyfactor\Keyfactor Orchestrator\extensions"

        # Linux
        kfutil orchestrator extension -e delinea-secretserver-pam@latest --out "/opt/keyfactor/orchestrator/extensions"
        ```

    * **Manually**: Download the latest release of the Delinea Secret Server PAM Provider from the [Releases](../../releases) page. Extract the contents of the archive to:

        * **Windows Server**: `C:\Program Files\Keyfactor\Keyfactor Orchestrator\extensions\delinea-secretserver-pam`
        * **Linux**: `/opt/keyfactor/orchestrator/extensions/delinea-secretserver-pam`

2. Included in the release is a `manifest.json` file that contains the following object:
    ```json

    {
        "Keyfactor:PAMProviders:Delinea-SecretServer-Windows:InitializationInfo": {
            "Host": "https://example.secretserver.internal/SecretServer"
        }
    }

    ```

    Populate the fields in this object with credentials and configuration data collected in the [requirements](docs/delinea-secretserver-password.md#requirements) section.

3. Restart the Universal Orchestrator service.





</details>







<details><summary>Delinea-SecretServer-ClientCredentials</summary>


#### Requirements
   - Delinea Secret Server instance accessible over HTTPS from the host running Keyfactor Command or the Universal Orchestrator.
   - An application account (Client ID and Client Secret) with permission to view the secrets being retrieved. See the
     [Delinea Secret Server documentation](https://docs.delinea.com/online-help/secret-server/api-scripting/authentication/script-token-auth/index.htm)
     for information on configuring application accounts.

#### Create PAM type in Keyfactor Command


##### Using `kfutil`
Create the required PAM Types in the connected Command platform.

```shell
# Delinea-SecretServer-ClientCredentials
kfutil pam types-create -r delinea-secretserver-pam -n Delinea-SecretServer-ClientCredentials
```

##### Using the API
For full API docs please visit our [product documentation](https://software.keyfactor.com/Core-OnPrem/Current/Content/WebAPI/KeyfactorAPI/PAMProvidersPOSTTypes.htm?Highlight=pam%20type)

Below is the payload to `POST` to the Keyfactor Command API
```json
{
    "Name": "Delinea-SecretServer-ClientCredentials",
    "Parameters": [
        {
            "Name": "Host",
            "DisplayName": "Secret Server URL",
            "Description": "The URL to the Secret Server instance. Example: https://example.secretservercloud.com/SecretServer",
            "DataType": 1,
            "InstanceLevel": false
        },
        {
            "Name": "ClientId",
            "DisplayName": "OAuth2 Client ID",
            "Description": "The client ID (application account name) used for OAuth2 client credentials authentication.",
            "DataType": 1,
            "InstanceLevel": false
        },
        {
            "Name": "ClientSecret",
            "DisplayName": "OAuth2 Client Secret",
            "Description": "The client secret (application account password) used for OAuth2 client credentials authentication.",
            "DataType": 2,
            "InstanceLevel": false
        },
        {
            "Name": "SkipTlsValidation",
            "DisplayName": "Skip TLS Validation",
            "Description": "Set to `true` to disable TLS certificate validation. Use only in non-production environments.",
            "DataType": 1,
            "InstanceLevel": false
        },
        {
            "Name": "SecretId",
            "DisplayName": "Secret ID",
            "Description": "The ID of the secret in Secret Server. This is the integer ID that is used to retrieve the secret from Secret Server.",
            "DataType": 1,
            "InstanceLevel": true
        },
        {
            "Name": "SecretFieldName",
            "DisplayName": "Secret Field Name",
            "Description": "The name of the field in the secret that contains the credential value. NOTE: The field must exist.",
            "DataType": 1,
            "InstanceLevel": true
        }
    ]
}
```

#### Install PAM provider on Keyfactor Command Host (Local)



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
            <register type="IPAMProvider" mapTo="Keyfactor.Extensions.Pam.Delinea.SecretServerPam, Keyfactor.Command.PAMProviders" name="Delinea-SecretServer-ClientCredentials" />
        </container>
        ```

    4. Repeat steps 2 and 3 for each of the directories listed in step 1. The configuration files are located in the following paths by default:

        * `C:\Program Files\Keyfactor\Keyfactor Platform\WebAgentServices\web.config`
        * `C:\Program Files\Keyfactor\Keyfactor Platform\KeyfactorAPI\web.config`
        * `C:\Program Files\Keyfactor\Keyfactor Platform\WebConsole\web.config`
        * `C:\Program Files\Keyfactor\Keyfactor Platform\Service\CMSTimerService.exe.config`

    </details>

3. Restart the Keyfactor Command services (`iisreset`).




#### Install PAM provider on a Universal Orchestrator Host (Remote)


1. Install the Delinea Secret Server PAM Provider assemblies.

    * **Using kfutil**: On the server that that hosts the Universal Orchestrator, run the following command:

        ```shell
        # Windows Server
        kfutil orchestrator extension -e delinea-secretserver-pam@latest --out "C:\Program Files\Keyfactor\Keyfactor Orchestrator\extensions"

        # Linux
        kfutil orchestrator extension -e delinea-secretserver-pam@latest --out "/opt/keyfactor/orchestrator/extensions"
        ```

    * **Manually**: Download the latest release of the Delinea Secret Server PAM Provider from the [Releases](../../releases) page. Extract the contents of the archive to:

        * **Windows Server**: `C:\Program Files\Keyfactor\Keyfactor Orchestrator\extensions\delinea-secretserver-pam`
        * **Linux**: `/opt/keyfactor/orchestrator/extensions/delinea-secretserver-pam`

2. Included in the release is a `manifest.json` file that contains the following object:
    ```json

    {
        "Keyfactor:PAMProviders:Delinea-SecretServer-Windows:InitializationInfo": {
            "Host": "https://example.secretserver.internal/SecretServer"
        }
    }

    ```

    Populate the fields in this object with credentials and configuration data collected in the [requirements](docs/delinea-secretserver-clientcredentials.md#requirements) section.

3. Restart the Universal Orchestrator service.





</details>







<details><summary>Delinea-SecretServer-Windows</summary>


#### Requirements
   - On-premises Delinea Secret Server instance accessible from the host running Keyfactor Command or the Universal Orchestrator.
   - The Windows service account running Keyfactor Command or the Universal Orchestrator must have permission to view
     the secrets being retrieved. See the
     [Delinea Secret Server IWA documentation](https://docs.delinea.com/online-help/secret-server/authentication/iwa-webservices/webservice-iwa-powershell/index.htm)
     for information on configuring IWA access.
   - The Secret Server instance must be configured to allow Integrated Windows Authentication web service access.

#### Create PAM type in Keyfactor Command


##### Using `kfutil`
Create the required PAM Types in the connected Command platform.

```shell
# Delinea-SecretServer-Windows
kfutil pam types-create -r delinea-secretserver-pam -n Delinea-SecretServer-Windows
```

##### Using the API
For full API docs please visit our [product documentation](https://software.keyfactor.com/Core-OnPrem/Current/Content/WebAPI/KeyfactorAPI/PAMProvidersPOSTTypes.htm?Highlight=pam%20type)

Below is the payload to `POST` to the Keyfactor Command API
```json
{
    "Name": "Delinea-SecretServer-Windows",
    "Parameters": [
        {
            "Name": "Host",
            "DisplayName": "Secret Server URL",
            "Description": "The URL to the Secret Server instance. Example: https://example.secretserver.internal/SecretServer. NOTE: IWA is not supported on Secret Server Cloud.",
            "DataType": 1,
            "InstanceLevel": false
        },
        {
            "Name": "SkipTlsValidation",
            "DisplayName": "Skip TLS Validation",
            "Description": "Set to `true` to disable TLS certificate validation. Use only in non-production environments.",
            "DataType": 1,
            "InstanceLevel": false
        },
        {
            "Name": "SecretId",
            "DisplayName": "Secret ID",
            "Description": "The ID of the secret in Secret Server. This is the integer ID that is used to retrieve the secret from Secret Server.",
            "DataType": 1,
            "InstanceLevel": true
        },
        {
            "Name": "SecretFieldName",
            "DisplayName": "Secret Field Name",
            "Description": "The name of the field in the secret that contains the credential value. NOTE: The field must exist.",
            "DataType": 1,
            "InstanceLevel": true
        }
    ]
}
```

#### Install PAM provider on Keyfactor Command Host (Local)



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
            <register type="IPAMProvider" mapTo="Keyfactor.Extensions.Pam.Delinea.SecretServerPam, Keyfactor.Command.PAMProviders" name="Delinea-SecretServer-Windows" />
        </container>
        ```

    4. Repeat steps 2 and 3 for each of the directories listed in step 1. The configuration files are located in the following paths by default:

        * `C:\Program Files\Keyfactor\Keyfactor Platform\WebAgentServices\web.config`
        * `C:\Program Files\Keyfactor\Keyfactor Platform\KeyfactorAPI\web.config`
        * `C:\Program Files\Keyfactor\Keyfactor Platform\WebConsole\web.config`
        * `C:\Program Files\Keyfactor\Keyfactor Platform\Service\CMSTimerService.exe.config`

    </details>

3. Restart the Keyfactor Command services (`iisreset`).




#### Install PAM provider on a Universal Orchestrator Host (Remote)


1. Install the Delinea Secret Server PAM Provider assemblies.

    * **Using kfutil**: On the server that that hosts the Universal Orchestrator, run the following command:

        ```shell
        # Windows Server
        kfutil orchestrator extension -e delinea-secretserver-pam@latest --out "C:\Program Files\Keyfactor\Keyfactor Orchestrator\extensions"

        # Linux
        kfutil orchestrator extension -e delinea-secretserver-pam@latest --out "/opt/keyfactor/orchestrator/extensions"
        ```

    * **Manually**: Download the latest release of the Delinea Secret Server PAM Provider from the [Releases](../../releases) page. Extract the contents of the archive to:

        * **Windows Server**: `C:\Program Files\Keyfactor\Keyfactor Orchestrator\extensions\delinea-secretserver-pam`
        * **Linux**: `/opt/keyfactor/orchestrator/extensions/delinea-secretserver-pam`

2. Included in the release is a `manifest.json` file that contains the following object:
    ```json

    {
        "Keyfactor:PAMProviders:Delinea-SecretServer-Windows:InitializationInfo": {
            "Host": "https://example.secretserver.internal/SecretServer"
        }
    }

    ```

    Populate the fields in this object with credentials and configuration data collected in the [requirements](docs/delinea-secretserver-windows.md#requirements) section.

3. Restart the Universal Orchestrator service.





</details>





### Usage





<details><summary>Delinea-SecretServer</summary>


#### From Keyfactor Command Host (Local)



##### Define a PAM provider in Command
1. In the Keyfactor Command Portal, hover over the ⚙️  (settings) icon in the top right corner of the screen and select **Priviledged Access Management**.

2. Select the **Add** button to create a new PAM provider. Click the dropdown for **Provider Type** and select **Delinea-SecretServer**.

> [!IMPORTANT]
> If you're running Keyfactor Command 11+, make sure `Remote Provider` is unchecked.

3. Populate the fields with the necessary information collected in the [requirements](docs/delinea-secretserver.md#requirements) section:

| Initialization parameter | Display Name | Description |
| --- | --- | --- |
| Host | Secret Server URL | The URL to the Secret Server instance. Example: https://example.secretservercloud.com/SecretServer |
| Username | Secret Server Username | The username used to authenticate to the Secret Server instance. NOTE: only applicable if using the `password` grant type. |
| Password | Secret Server Password | The password used to authenticate to the Secret Server instance. NOTE: only applicable if using the `password` grant type. |
| ClientId | Secret Server Client ID | The client ID used to authenticate to the Secret Server instance. NOTE: only applicable if using the `client_credentials` grant type. |
| ClientSecret | Secret Server Client Secret | The client secret used to authenticate to the Secret Server instance. NOTE: only applicable if using the `client_credentials` grant type. |
| GrantType | Grant Type | The grant type used to authenticate to the Secret Server instance. Valid values are `password`, `client_credentials`, or `windows`. Default is `password`. If not provided the default value `password` will be used to maintain backwards compatibility. |


4. Click **Save**. The PAM provider is now available for use in Keyfactor Command.

##### Using the PAM provider

Now, when defining Certificate Stores (**Locations**->**Certificate Stores**), **Delinea-SecretServer** will be available as a PAM provider option. When defining new Certificate Stores, the secret parameter form will display tabs for **Load From Keyfactor Secrets** or **Load From PAM Provider**. 

Select the **Load From PAM Provider** tab, choose the **Delinea-SecretServer** provider from the list of **Providers**, and populate the fields with the necessary information from the table below:

| Instance parameter | Display Name | Description |
| --- | --- | --- |
| SecretId | Secret ID | The ID of the secret in Secret Server. This is the integer ID that is used to retrieve the secret from Secret Server. |
| SecretFieldName | Secret Field Name | The name of the field in the secret that contains the credential value. NOTE: The field must exist. |





#### From a Universal Orchestrator Host (Remote)



<details><summary>Keyfactor Command 11+</summary>

##### Define a remote PAM provider in Command

In Command 11 and greater, before using the Delinea-SecretServer PAM type, you must define a Remote PAM Provider in the Command portal.

1. In the Keyfactor Command Portal, hover over the ⚙️  (settings) icon in the top right corner of the screen and select **Priviledged Access Management**.

2. Select the **Add** button to create a new PAM provider.

3. Make sure that `Remote Provider` is checked.

4. Click the dropdown for **Provider Type** and select **Delinea-SecretServer**. 

5. Give the provider a unique name.

6. Click "Save".

##### Using the PAM provider

When defining Certificate Stores (**Locations**->**Certificate Stores**), **Delinea-SecretServer** can be used as a PAM provider. When defining a new Certificate Store, the secret parameter form will display tabs for **Load From Keyfactor Secrets** or **Load From PAM Provider**.

Select the **Load From PAM Provider** tab, choose the **Delinea-SecretServer** provider from the list of **Providers**, and populate the fields with the necessary information from the table below:

| Instance parameter | Display Name | Description |
| --- | --- | --- |
| SecretId | Secret ID | The ID of the secret in Secret Server. This is the integer ID that is used to retrieve the secret from Secret Server. |
| SecretFieldName | Secret Field Name | The name of the field in the secret that contains the credential value. NOTE: The field must exist. |


</details>

<details><summary>Keyfactor Command 10</summary>

When defining Certificate Stores (**Locations**->**Certificate Stores**), **Delinea-SecretServer** can be used as a PAM provider.

When entering Secret fields, select the **Load From Keyfactor Secrets** tab, and populate the **Secret Value** field with the following JSON object:

```json
{"SecretId": "The ID of the secret in Secret Server. This is the integer ID that is used to retrieve the secret from Secret Server.","SecretFieldName": "The name of the field in the secret that contains the credential value. NOTE: The field must exist."}

```

> We recommend creating this JSON object in a text editor, and copying it into the Secret Value field.

</details>





</details>


> [!NOTE]
> Additional information on Delinea-SecretServer can be found in the [supplemental documentation](docs/delinea-secretserver.md).





<details><summary>Delinea-SecretServer-Password</summary>


#### From Keyfactor Command Host (Local)



##### Define a PAM provider in Command
1. In the Keyfactor Command Portal, hover over the ⚙️  (settings) icon in the top right corner of the screen and select **Priviledged Access Management**.

2. Select the **Add** button to create a new PAM provider. Click the dropdown for **Provider Type** and select **Delinea-SecretServer-Password**.

> [!IMPORTANT]
> If you're running Keyfactor Command 11+, make sure `Remote Provider` is unchecked.

3. Populate the fields with the necessary information collected in the [requirements](docs/delinea-secretserver-password.md#requirements) section:

| Initialization parameter | Display Name | Description |
| --- | --- | --- |
| Host | Secret Server URL | The URL to the Secret Server instance. Example: https://example.secretservercloud.com/SecretServer |
| Username | Secret Server Username | The username used to authenticate to the Secret Server instance. |
| Password | Secret Server Password | The password used to authenticate to the Secret Server instance. |
| SkipTlsValidation | Skip TLS Validation | Set to `true` to disable TLS certificate validation. Use only in non-production environments. |


4. Click **Save**. The PAM provider is now available for use in Keyfactor Command.

##### Using the PAM provider

Now, when defining Certificate Stores (**Locations**->**Certificate Stores**), **Delinea-SecretServer-Password** will be available as a PAM provider option. When defining new Certificate Stores, the secret parameter form will display tabs for **Load From Keyfactor Secrets** or **Load From PAM Provider**. 

Select the **Load From PAM Provider** tab, choose the **Delinea-SecretServer-Password** provider from the list of **Providers**, and populate the fields with the necessary information from the table below:

| Instance parameter | Display Name | Description |
| --- | --- | --- |
| SecretId | Secret ID | The ID of the secret in Secret Server. This is the integer ID that is used to retrieve the secret from Secret Server. |
| SecretFieldName | Secret Field Name | The name of the field in the secret that contains the credential value. NOTE: The field must exist. |





#### From a Universal Orchestrator Host (Remote)



<details><summary>Keyfactor Command 11+</summary>

##### Define a remote PAM provider in Command

In Command 11 and greater, before using the Delinea-SecretServer-Password PAM type, you must define a Remote PAM Provider in the Command portal.

1. In the Keyfactor Command Portal, hover over the ⚙️  (settings) icon in the top right corner of the screen and select **Priviledged Access Management**.

2. Select the **Add** button to create a new PAM provider.

3. Make sure that `Remote Provider` is checked.

4. Click the dropdown for **Provider Type** and select **Delinea-SecretServer-Password**. 

5. Give the provider a unique name.

6. Click "Save".

##### Using the PAM provider

When defining Certificate Stores (**Locations**->**Certificate Stores**), **Delinea-SecretServer-Password** can be used as a PAM provider. When defining a new Certificate Store, the secret parameter form will display tabs for **Load From Keyfactor Secrets** or **Load From PAM Provider**.

Select the **Load From PAM Provider** tab, choose the **Delinea-SecretServer-Password** provider from the list of **Providers**, and populate the fields with the necessary information from the table below:

| Instance parameter | Display Name | Description |
| --- | --- | --- |
| SecretId | Secret ID | The ID of the secret in Secret Server. This is the integer ID that is used to retrieve the secret from Secret Server. |
| SecretFieldName | Secret Field Name | The name of the field in the secret that contains the credential value. NOTE: The field must exist. |


</details>

<details><summary>Keyfactor Command 10</summary>

When defining Certificate Stores (**Locations**->**Certificate Stores**), **Delinea-SecretServer-Password** can be used as a PAM provider.

When entering Secret fields, select the **Load From Keyfactor Secrets** tab, and populate the **Secret Value** field with the following JSON object:

```json
{"SecretId": "The ID of the secret in Secret Server. This is the integer ID that is used to retrieve the secret from Secret Server.","SecretFieldName": "The name of the field in the secret that contains the credential value. NOTE: The field must exist."}

```

> We recommend creating this JSON object in a text editor, and copying it into the Secret Value field.

</details>





</details>


> [!NOTE]
> Additional information on Delinea-SecretServer-Password can be found in the [supplemental documentation](docs/delinea-secretserver-password.md).





<details><summary>Delinea-SecretServer-ClientCredentials</summary>


#### From Keyfactor Command Host (Local)



##### Define a PAM provider in Command
1. In the Keyfactor Command Portal, hover over the ⚙️  (settings) icon in the top right corner of the screen and select **Priviledged Access Management**.

2. Select the **Add** button to create a new PAM provider. Click the dropdown for **Provider Type** and select **Delinea-SecretServer-ClientCredentials**.

> [!IMPORTANT]
> If you're running Keyfactor Command 11+, make sure `Remote Provider` is unchecked.

3. Populate the fields with the necessary information collected in the [requirements](docs/delinea-secretserver-clientcredentials.md#requirements) section:

| Initialization parameter | Display Name | Description |
| --- | --- | --- |
| Host | Secret Server URL | The URL to the Secret Server instance. Example: https://example.secretservercloud.com/SecretServer |
| ClientId | OAuth2 Client ID | The client ID (application account name) used for OAuth2 client credentials authentication. |
| ClientSecret | OAuth2 Client Secret | The client secret (application account password) used for OAuth2 client credentials authentication. |
| SkipTlsValidation | Skip TLS Validation | Set to `true` to disable TLS certificate validation. Use only in non-production environments. |


4. Click **Save**. The PAM provider is now available for use in Keyfactor Command.

##### Using the PAM provider

Now, when defining Certificate Stores (**Locations**->**Certificate Stores**), **Delinea-SecretServer-ClientCredentials** will be available as a PAM provider option. When defining new Certificate Stores, the secret parameter form will display tabs for **Load From Keyfactor Secrets** or **Load From PAM Provider**. 

Select the **Load From PAM Provider** tab, choose the **Delinea-SecretServer-ClientCredentials** provider from the list of **Providers**, and populate the fields with the necessary information from the table below:

| Instance parameter | Display Name | Description |
| --- | --- | --- |
| SecretId | Secret ID | The ID of the secret in Secret Server. This is the integer ID that is used to retrieve the secret from Secret Server. |
| SecretFieldName | Secret Field Name | The name of the field in the secret that contains the credential value. NOTE: The field must exist. |





#### From a Universal Orchestrator Host (Remote)



<details><summary>Keyfactor Command 11+</summary>

##### Define a remote PAM provider in Command

In Command 11 and greater, before using the Delinea-SecretServer-ClientCredentials PAM type, you must define a Remote PAM Provider in the Command portal.

1. In the Keyfactor Command Portal, hover over the ⚙️  (settings) icon in the top right corner of the screen and select **Priviledged Access Management**.

2. Select the **Add** button to create a new PAM provider.

3. Make sure that `Remote Provider` is checked.

4. Click the dropdown for **Provider Type** and select **Delinea-SecretServer-ClientCredentials**. 

5. Give the provider a unique name.

6. Click "Save".

##### Using the PAM provider

When defining Certificate Stores (**Locations**->**Certificate Stores**), **Delinea-SecretServer-ClientCredentials** can be used as a PAM provider. When defining a new Certificate Store, the secret parameter form will display tabs for **Load From Keyfactor Secrets** or **Load From PAM Provider**.

Select the **Load From PAM Provider** tab, choose the **Delinea-SecretServer-ClientCredentials** provider from the list of **Providers**, and populate the fields with the necessary information from the table below:

| Instance parameter | Display Name | Description |
| --- | --- | --- |
| SecretId | Secret ID | The ID of the secret in Secret Server. This is the integer ID that is used to retrieve the secret from Secret Server. |
| SecretFieldName | Secret Field Name | The name of the field in the secret that contains the credential value. NOTE: The field must exist. |


</details>

<details><summary>Keyfactor Command 10</summary>

When defining Certificate Stores (**Locations**->**Certificate Stores**), **Delinea-SecretServer-ClientCredentials** can be used as a PAM provider.

When entering Secret fields, select the **Load From Keyfactor Secrets** tab, and populate the **Secret Value** field with the following JSON object:

```json
{"SecretId": "The ID of the secret in Secret Server. This is the integer ID that is used to retrieve the secret from Secret Server.","SecretFieldName": "The name of the field in the secret that contains the credential value. NOTE: The field must exist."}

```

> We recommend creating this JSON object in a text editor, and copying it into the Secret Value field.

</details>





</details>


> [!NOTE]
> Additional information on Delinea-SecretServer-ClientCredentials can be found in the [supplemental documentation](docs/delinea-secretserver-clientcredentials.md).





<details><summary>Delinea-SecretServer-Windows</summary>


#### From Keyfactor Command Host (Local)



##### Define a PAM provider in Command
1. In the Keyfactor Command Portal, hover over the ⚙️  (settings) icon in the top right corner of the screen and select **Priviledged Access Management**.

2. Select the **Add** button to create a new PAM provider. Click the dropdown for **Provider Type** and select **Delinea-SecretServer-Windows**.

> [!IMPORTANT]
> If you're running Keyfactor Command 11+, make sure `Remote Provider` is unchecked.

3. Populate the fields with the necessary information collected in the [requirements](docs/delinea-secretserver-windows.md#requirements) section:

| Initialization parameter | Display Name | Description |
| --- | --- | --- |
| Host | Secret Server URL | The URL to the Secret Server instance. Example: https://example.secretserver.internal/SecretServer. NOTE: IWA is not supported on Secret Server Cloud. |
| SkipTlsValidation | Skip TLS Validation | Set to `true` to disable TLS certificate validation. Use only in non-production environments. |


4. Click **Save**. The PAM provider is now available for use in Keyfactor Command.

##### Using the PAM provider

Now, when defining Certificate Stores (**Locations**->**Certificate Stores**), **Delinea-SecretServer-Windows** will be available as a PAM provider option. When defining new Certificate Stores, the secret parameter form will display tabs for **Load From Keyfactor Secrets** or **Load From PAM Provider**. 

Select the **Load From PAM Provider** tab, choose the **Delinea-SecretServer-Windows** provider from the list of **Providers**, and populate the fields with the necessary information from the table below:

| Instance parameter | Display Name | Description |
| --- | --- | --- |
| SecretId | Secret ID | The ID of the secret in Secret Server. This is the integer ID that is used to retrieve the secret from Secret Server. |
| SecretFieldName | Secret Field Name | The name of the field in the secret that contains the credential value. NOTE: The field must exist. |





#### From a Universal Orchestrator Host (Remote)



<details><summary>Keyfactor Command 11+</summary>

##### Define a remote PAM provider in Command

In Command 11 and greater, before using the Delinea-SecretServer-Windows PAM type, you must define a Remote PAM Provider in the Command portal.

1. In the Keyfactor Command Portal, hover over the ⚙️  (settings) icon in the top right corner of the screen and select **Priviledged Access Management**.

2. Select the **Add** button to create a new PAM provider.

3. Make sure that `Remote Provider` is checked.

4. Click the dropdown for **Provider Type** and select **Delinea-SecretServer-Windows**. 

5. Give the provider a unique name.

6. Click "Save".

##### Using the PAM provider

When defining Certificate Stores (**Locations**->**Certificate Stores**), **Delinea-SecretServer-Windows** can be used as a PAM provider. When defining a new Certificate Store, the secret parameter form will display tabs for **Load From Keyfactor Secrets** or **Load From PAM Provider**.

Select the **Load From PAM Provider** tab, choose the **Delinea-SecretServer-Windows** provider from the list of **Providers**, and populate the fields with the necessary information from the table below:

| Instance parameter | Display Name | Description |
| --- | --- | --- |
| SecretId | Secret ID | The ID of the secret in Secret Server. This is the integer ID that is used to retrieve the secret from Secret Server. |
| SecretFieldName | Secret Field Name | The name of the field in the secret that contains the credential value. NOTE: The field must exist. |


</details>

<details><summary>Keyfactor Command 10</summary>

When defining Certificate Stores (**Locations**->**Certificate Stores**), **Delinea-SecretServer-Windows** can be used as a PAM provider.

When entering Secret fields, select the **Load From Keyfactor Secrets** tab, and populate the **Secret Value** field with the following JSON object:

```json
{"SecretId": "The ID of the secret in Secret Server. This is the integer ID that is used to retrieve the secret from Secret Server.","SecretFieldName": "The name of the field in the secret that contains the credential value. NOTE: The field must exist."}

```

> We recommend creating this JSON object in a text editor, and copying it into the Secret Value field.

</details>





</details>


> [!NOTE]
> Additional information on Delinea-SecretServer-Windows can be found in the [supplemental documentation](docs/delinea-secretserver-windows.md).



## License

Apache License 2.0, see [LICENSE](LICENSE)

## Related Integrations

See all [Keyfactor PAM Provider extensions](https://github.com/orgs/Keyfactor/repositories?q=pam).