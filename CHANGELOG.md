# v1.3.0

## Improvements
- Enhanced debug logging for token endpoint requests: the obfuscated request body (credentials redacted) and full raw response body are now logged on token request failures to aid troubleshooting.

# v1.2.0

## Features
- Added support for `windows` grant type for Integrated Windows Authentication (IWA). NOTE: IWA is not supported on Secret Server Cloud.

# v1.1.0

## Features
- Added support for `client_credential` grant type for OAuth2 authentication.

# v1.0.0
- Initial release of Delinea SecretServer PAM Provider