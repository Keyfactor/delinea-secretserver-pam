# v1.3.0

## Compliance Remediation (SOX/SOC2)

- **CRIT-3**: `GetDelineaSecretAsync` now throws `InvalidSecretConfigurationException` when the requested field is not found in the secret, rather than silently returning an empty string. This prevents silent credential resolution failures from going undetected.
- **CRIT-1**: Token endpoint error response body is now truncated to 500 characters before logging to prevent secret metadata exposure in log sinks.
- **CRIT-2**: Added an explicit `LogInformation` audit event for the Windows authentication path recording OS identity, machine name, target URL, and SecretId before the HTTP call is made.
- **HIGH-4**: Added an authentication success `LogInformation` event in `GetAccessToken` recording the caller identity and target URL with a structured `AuthenticationResult=Success` field.
- **HIGH-3**: A `Guid`-based correlation ID is generated at the start of each `GetPassword` invocation and threaded as a trailing structured field through all `LogInformation` and `LogError` calls in `GetDelineaSecretAsync` and `GetAccessToken`, enabling log correlation across a full PAM operation.
- **MED-5**: `Stopwatch` instances for the token POST and secret GET HTTP calls are now declared outside their try blocks; catch blocks record elapsed duration and emit a structured `HTTP call failed` log event so network failure timing is preserved in exception paths.
- **MED-3**: Removed the duplicate `SecretResponse` class defined inline at the bottom of `SecretServerPam.cs`. The canonical definition in `Models/SecretResponse.cs` (which includes `Id`, `Name`, `SecretTemplateId`, `FolderId`, and `Active` in addition to `Items`) is now the sole definition, resolved via the existing `using Keyfactor.Extensions.Pam.Delinea.Models;` import.
- **MED-2**: `Username` and `ClientId` parameters in `integration-manifest.json` changed from `DataType: 2` (secret/masked) to `DataType: 1` (plain text). These are non-secret identifiers and should not be stored or displayed as secrets in the Keyfactor Command UI.
- **MED-4**: TestConsole no longer provides hardcoded fallback values for `SECRET_SERVER_URL`, `SECRET_SERVER_USERNAME`, `SECRET_SERVER_PASSWORD`, `SECRET_SERVER_CLIENT_ID`, `SECRET_SERVER_CLIENT_SECRET`, and `SECRET_SERVER_SECRET_ID`. A `RequireEnv` helper is used for all six; missing variables throw `InvalidOperationException` immediately to prevent accidental runs against unintended targets.
- **MED-1**: Added inline comments at each `Environment.UserName` usage site in `GetPassword` and the Windows auth path documenting that this value reflects the OS service account identity, not the Keyfactor Command caller identity, since `IPAMProvider` does not expose caller context.

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