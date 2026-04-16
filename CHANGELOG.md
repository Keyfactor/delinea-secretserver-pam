# v1.3.0

## Improvements
- Enhanced debug logging for token endpoint requests: the obfuscated request body (credentials redacted) and raw response body are now logged on token request failures to aid troubleshooting.
- Added structured audit log event on every `GetPassword` invocation recording caller identity, machine name, target URL, grant type, SecretId, and field name.
- Added response duration logging (ms) for both the OAuth token endpoint and secret retrieval API calls.
- Success and failure log events now include SecretId, field name, grant type, and URL for complete audit trail.
- Auth failure log events now include the target URL, grant type, and caller identity.
- Error responses from Secret Server are truncated to 500 characters before logging to prevent sensitive metadata exposure.
- Removed raw token response body from deserialization failure log path to prevent accidental bearer token exposure.

## Bug Fixes
- Replaced `.Result` with `.GetAwaiter().GetResult()` in `GetPassword` to prevent exception masking on async task failures.

## Maintenance
- Removed dead `IValidatableObject` implementation from `DelineaConfiguration`; validation is enforced in `ValidateServerConfigurationParams`.
- Masked password value in TestConsole output.
- Bumped TestConsole target framework to net10.0 and global SDK pin to 10.0.0.
- Added `.env`, `scripts/`, and `client_pam.json` to `.gitignore`.

# v1.2.0

## Features
- Added support for `windows` grant type for Integrated Windows Authentication (IWA). NOTE: IWA is not supported on Secret Server Cloud.

# v1.1.0

## Features
- Added support for `client_credential` grant type for OAuth2 authentication.

# v1.0.0
- Initial release of Delinea SecretServer PAM Provider