### Initialization Parameters for each defined PAM Provider instance
| Initialization parameter |      Display Name       | Description                                                               |
|:------------------------:|:-----------------------:|---------------------------------------------------------------------------|
|           Host           |    Secret Server URL    | The IP address or URL of the Vault instance, including any port number    |
|         Username         | Secret Server Username  | The username the PAM provider is going to use to connect to SecretServer. |
|         Password         | Secret Server Password  | The username the PAM provider is going to use to connect to SecretServer. |



### Instance Parameters for each retrieved secret field
| Instance parameter |       Display Name       | Description                                                            |
|:------------------:|:------------------------:|------------------------------------------------------------------------|
|      SecretId      | Secret Server Secret ID  | The integer ID of the secret to use.                                   |
|  SecretFieldName   |    Secret Field Name     | The name of the field to use when looking up a secret on SecretServer. |

![](../images/config.png)