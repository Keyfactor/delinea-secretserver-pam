## Configuring for PAM Usage
### Delinea Secret Server
When configuring the Delinea Secret Server for use as a PAM Provider with Keyfactor, you will need to ensure that your
instance is configured for API access. This can be done by logging into the Delinea Secret Server as an administrator.
For more details visit the vendor docs [here](https://docs.delinea.com/secrets/current/api-scripting/sdk-cli/index.md#setup_procedure).

Once API access is configured a user account with a username and password is required. That account *MUST* be granted access
to view secret's you'll be using.

After adding and sharing a secret on SecretServer, you can use the secret's ID (the "Secret ID") and the desired value's
field name (the "Secret Field Name") to retrieve credentials from the Delinea Secret Server as a PAM Provider.

### On Keyfactor Universal Orchestrator
#### Installation
Configuring the UO to use the Delinea Secret Server PAM Provider requires first installing it as an extension by copying the
release contents into a new extension folder named `Delinea-SecretServer`. A `manifest.json` file is included in the release.
This file needs to be edited to enter in the `InitializationInfo` parameters for the PAM Provider. Specifically values need
to be entered for the parameters in the `manifest.json` of the PAM Provider extension:
```json
"Keyfactor:PAMProviders:Delinea-SecretServer:InitializationInfo": {
    "Host": "https://example.secretservercloud.com/SecretServer",
    "Username": "my_secretserver_service_account",
    "Password": "xxxxxx"
  }
```

#### Usage
To use the PAM Provider to resolve a field, for example a `Server Password`, instead of entering in the actual value for
the `Server Password`, enter a json object with the parameters specifying the field. The parameters needed are the
"instance" parameters above:
```json
{"SecretId":"1","SecretFieldName":"password"}
```
If a field supports PAM but should not use PAM, simply enter in the actual value to be used instead of the json format
object above.

### In Keyfactor - PAM Provider
#### Installation
In order to setup a new PAM Provider in the Keyfactor Platform for the first time, you will need to run the `kfutil`
tool (see Initial Configuration of PAM Provider).

After the installation is run, the DLLs need to be installed to the correct location for the PAM Provider to function.
From the release, the `delinea-secretserver-pam.dll` should be copied to the following folder locations in the Keyfactor
installation. Once the DLL has been copied to these folders, edit the corresponding config file. You will need to add a
new Unity entry as follows under `<container>`, next to other `<register>` tags.

| Install Location | DLL Binary Folder     | Config File                         |
|------------------|-----------------------|-------------------------------------|
| WebAgentServices | WebAgentServices\bin\ | WebAgentServices\web.config         |
| Service          | Service\              | Service\CMSTimerService.exe.config  |
| KeyfactorAPI     | KeyfactorAPI\bin\     | KeyfactorAPI\web.config             |
| WebConsole       | WebConsole\bin\       | WebConsole\web.config               |

When enabling a PAM provider for Orchestrators only, the first line for `WebAgentServices` is the only installation needed.

The Keyfactor service and IIS Server should be restarted after making these changes.

```xml
<register type="IPAMProvider" mapTo="Keyfactor.Extensions.Pam.Delinea.SecretServerPam, delinea-secretserver-pam" name="Delinea-SecretServer" />
```

#### Usage
In order to use the PAM Provider, the provider's configuration must be set in the Keyfactor Platform. In the settings
menu (upper right cog) you can select the ___Priviledged Access Management___ option to configure your provider instance.

![](images/setting.png)

After it is set up, you can now use your PAM Provider when configuring certificate stores. Any field that is treated as
a Keyfactor secret, such as server passwords and certificate store passwords can be retrieved from your PAM Provider
instead of being entered in directly as a secret.

![](images/password.png)