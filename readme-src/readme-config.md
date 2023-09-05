## Configuring for PAM Usage
### Delinea Secret Server
When configuring the Delinea Secret Server for use as a PAM Provider with Keyfactor, you will need to ensure that your 
instance is configured for API access. This can be done by logging into the Delinea Secret Server as an administrator.
For more details visit the vendor docs [here](https://docs.delinea.com/secrets/current/api-scripting/sdk-cli/index.md#setup_procedure).

Once API access is configured a user account with a username and password is required. That account *MUST* be granted access 
to view secret's you'll be using. 

After adding and sharing a secret on SecretServer, you can use the secret's ID (the "Secret ID") and the desired value's 
field name (the "Secret Field Name") to retrieve credentials from the Delinea Secret Server as a PAM Provider.
