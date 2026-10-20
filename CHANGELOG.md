# v1.3.1

## Upgrade Notes

- A Universal Orchestrator supports only one PAM provider registration per `manifest.json` (until UO v26.4). The type-specific sample manifests ship in a `samples/` subfolder so the UO never loads them by accident: copy `samples/manifest.<type>.json` over `manifest.json`. The shipped `manifest.json` registers only the generic `Delinea-SecretServer` provider, as in 1.3.0. To use a type-specific provider, copy its sample manifest (`samples/manifest.password.json`, `samples/manifest.clientcredentials.json` or `samples/manifest.windows.json`) over `manifest.json` and fill in the values. Using more than one Delinea PAM type requires a separate Universal Orchestrator for each.
- If you are on 1.3.0 and hand-edited the extension registration name or the `InitializationInfo` key in `manifest.json` to match a custom PAM provider name, those edits are lost when you replace `manifest.json` with the 1.3.1 version. Re-apply your naming, or rename the provider in Command to match. The registration name and `InitializationInfo` key must both match the PAM provider name you created in Command.
- Remove any `GrantType` workaround you added to `InitializationInfo` for the type-specific providers; it is no longer needed once the matching sample manifest is in place.

## Bug Fixes

- The extension registration name in `manifest.json` (`PAMProviders.Delinea-SecretServer.PAMProvider`) now matches the `InitializationInfo` key (`Delinea-SecretServer`) and the provider `Name`. Previously the registration was named `Delinea` while the `InitializationInfo` key was `Delinea-SecretServer`.
- The type-specific provider classes (`SecretServerPamPassword`, `SecretServerPamClientCredentials`, `SecretServerPamWindows`) can now actually be loaded by the Universal Orchestrator: each has a sample manifest registering that class under its own name. Previously none of them was registered, so type-specific PAM types silently loaded the generic class and defaulted the grant type to `password` unless `GrantType` was supplied.
- The generic `Delinea-SecretServer` PAM type in `integration-manifest.json` now includes the `SkipTlsValidation` parameter, consistent with the type-specific types and the code.

## Documentation

- Each PAM type's page now documents its own registration, `InitializationInfo` block and sample manifest, and the Command 10 `web.config` registration with the correct class and assembly name (`delinea-secretserver-pam`).
- Documented that the registration name and `InitializationInfo` key must match the PAM provider name created in Command, and the one-provider-per-`manifest.json` limitation.

## Tests

- Added `ManifestRegistrationTests`: each shipped manifest has exactly one registration whose `TypeFullName` resolves and whose name matches the class `Name` and the single `InitializationInfo` key; together the manifests cover all four classes; no template enables `SkipTlsValidation`; all manifests are copied to the build output; and each type-specific class uses its fixed grant type when no `GrantType` is supplied.

# v1.3.0

## Features

- Added three grant-type-specific PAM type variants. Each type exposes only the fields relevant to its authentication flow, resolving the Keyfactor Command UI requirement that all declared fields be populated.
  - `Delinea-SecretServer-Password` — Username + Password authentication. Server parameters: `Host`, `Username`, `Password`, `SkipTlsValidation`.
  - `Delinea-SecretServer-ClientCredentials` — OAuth2 client credentials flow. Server parameters: `Host`, `ClientId`, `ClientSecret`, `SkipTlsValidation`.
  - `Delinea-SecretServer-Windows` — Integrated Windows Authentication (IWA). Server parameters: `Host`, `SkipTlsValidation`. NOTE: IWA is not supported on Secret Server Cloud.
- All shared logic (HTTP, validation, secret retrieval, audit logging) is implemented once in the new `SecretServerPamBase` abstract class.
- The existing `Delinea-SecretServer` type is unchanged and fully backwards compatible.
- TLS certificate validation can now be disabled via the `KEYFACTOR_PAM_SKIP_TLS_VALIDATION` environment variable (`true` or `1`), in addition to the existing `SkipTlsValidation` configuration parameter. Either setting is sufficient to disable validation.
- Parameter values of `N/A` (case-insensitive, whitespace-trimmed) are now treated as empty/absent. This allows users of the backwards-compatible `Delinea-SecretServer` type to enter `N/A` as a dummy value in the Keyfactor Command UI for fields that are not applicable to their authentication flow.

## Compliance Remediation (SOX/SOC2)

- `GetDelineaSecretAsync` now throws `InvalidSecretConfigurationException` when the requested field is not found in the secret, rather than silently returning an empty string. This prevents silent credential resolution failures from going undetected.
- Token endpoint error response body is now truncated to 500 characters before logging to prevent secret metadata exposure in log sinks.
- Added an explicit `LogInformation` audit event for the Windows authentication path recording OS identity, machine name, target URL, and SecretId before the HTTP call is made.
- Added an authentication success `LogInformation` event in `GetAccessToken` recording the caller identity and target URL with a structured `AuthenticationResult=Success` field.
- A `Guid`-based correlation ID is generated at the start of each `GetPassword` invocation and threaded as a trailing structured field through all `LogInformation` and `LogError` calls in `GetDelineaSecretAsync` and `GetAccessToken`, enabling log correlation across a full PAM operation.
- `Stopwatch` instances for the token POST and secret GET HTTP calls are now declared outside their try blocks; catch blocks record elapsed duration and emit a structured `HTTP call failed` log event so network failure timing is preserved in exception paths.
- Removed the duplicate `SecretResponse` class defined inline at the bottom of `SecretServerPam.cs`. The canonical definition in `Models/SecretResponse.cs` (which includes `Id`, `Name`, `SecretTemplateId`, `FolderId`, and `Active` in addition to `Items`) is now the sole definition, resolved via the existing `using Keyfactor.Extensions.Pam.Delinea.Models;` import.
- `Username` and `ClientId` parameters in `integration-manifest.json` changed from `DataType: 2` (secret/masked) to `DataType: 1` (plain text). These are non-secret identifiers and should not be stored or displayed as secrets in the Keyfactor Command UI.
- Added inline comments at each `Environment.UserName` usage site documenting that this value reflects the OS service account identity, not the Keyfactor Command caller identity, since `IPAMProvider` does not expose caller context.

## Improvements

- Enhanced debug logging for token endpoint requests: the obfuscated request body (credentials redacted) and raw response body are now logged on token request failures to aid troubleshooting.
- Added structured audit log event on every `GetPassword` invocation recording caller identity, machine name, target URL, grant type, SecretId, and field name.
- Added response duration logging (ms) for both the OAuth token endpoint and secret retrieval API calls.
- Success and failure log events now include SecretId, field name, grant type, and URL for complete audit trail.
- Auth failure log events now include the target URL, grant type, and caller identity.
- Error responses from Secret Server are truncated to 500 characters before logging to prevent sensitive metadata exposure.
- Removed raw token response body from deserialization failure log path to prevent accidental bearer token exposure.

## Bug Fixes

- Fixed `client_credentials` case in `BuildDelineaConfiguration` where `GrantType` was incorrectly set to `"password"` instead of `"client_credentials"` on the resulting `DelineaConfiguration` object.
- Validation of `SecretFieldName` now rejects whitespace-only values (previously only empty string was rejected).
- Replaced `.Result` with `.GetAwaiter().GetResult()` in `GetPassword` to prevent exception masking on async task failures.

## Testing

- Replaced the manual `TestConsole` project with a proper `xUnit` test project (`delinea-secretserver-pam.Tests`, targeting `net8.0`) covering all four PAM types, all auth flows, and error paths including missing parameters, token failures, field-not-found, and non-success HTTP responses.
- Integration tests skip automatically when `SECRET_SERVER_*` environment variables are not set.

## Maintenance

- Removed dead `IValidatableObject` implementation from `DelineaConfiguration`; validation is enforced in `ValidateServerConfigurationParams`.
- Masked password value in TestConsole output.
- Bumped TestConsole target framework to net10.0 and global SDK pin to 10.0.0.

# v1.2.0

## Features
- Added support for `windows` grant type for Integrated Windows Authentication (IWA). NOTE: IWA is not supported on Secret Server Cloud.

# v1.1.0

## Features
- Added support for `client_credential` grant type for OAuth2 authentication.

# v1.0.0
- Initial release of Delinea SecretServer PAM Provider
