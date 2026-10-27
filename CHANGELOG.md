# v1.4.0

## Features

- `(config)` Server parameters can now be supplied through environment variables, as a fallback only: a configured PAM parameter always wins, and a variable is used only when the parameter is missing, empty, or `N/A`. New variables: `KEYFACTOR_PAM_DELINEA_HOST` (`Host`, all PAM types), `KEYFACTOR_PAM_DELINEA_GRANT_TYPE` (`GrantType`, generic `Delinea-SecretServer` type only), `KEYFACTOR_PAM_DELINEA_USERNAME` / `KEYFACTOR_PAM_DELINEA_PASSWORD` (`password` grant) and `KEYFACTOR_PAM_DELINEA_CLIENT_ID` / `KEYFACTOR_PAM_DELINEA_CLIENT_SECRET` (`client_credentials` grant). Credential variables are read only for the grant type in effect, so the `windows` grant never reads them, and the type-specific PAM types ignore the grant type variable. `SecretId` and `SecretFieldName` have no environment fallback. `KEYFACTOR_PAM_SKIP_TLS_VALIDATION` is unchanged.

## Improvements

- `(errors)` When a required server parameter is missing, the error message now also names the environment variable that could supply it, for example `Server configuration parameter 'Host' not provided (environment variable KEYFACTOR_PAM_DELINEA_HOST not set)`. The exception types are unchanged.
- `(logging)` At debug level the provider records which source (PAM parameter or environment variable name) supplied each server parameter. Values are never logged. The `GetPassword invoked` record now shows the effective host and grant type.

## Documentation

- `(docs)` Document the environment variables, the precedence rule, the `N/A` tip for the Command UI, and the security considerations: machine-scope variables are readable by other local users, the process environment is readable by administrators and in crash dumps, one set of variables applies to every Delinea PAM provider in the process, and changes need a process restart.
- `(docs)` Correct the TLS Validation section: `KEYFACTOR_PAM_SKIP_TLS_VALIDATION` does not override `SkipTlsValidation`. Validation is skipped if either source enables it, and `false` in one source does not override `true` in the other. The code has always behaved this way; only the documentation was wrong.

## Tests

- `(tests)` Cover the environment fallback for every PAM type and each variable (missing, empty and `N/A` parameters), parameter-over-environment precedence, the `windows` grant ignoring credential variables, type-specific classes ignoring the grant type variable, no fallback for `SecretId` and `SecretFieldName`, empty and `N/A` variables counting as unset, error messages, and log content (source named, secrets never logged). All provider tests now run in one non-parallel collection and clear `KEYFACTOR_PAM_DELINEA_*` before each test, so results no longer depend on the developer shell or on parallel test classes.

# v1.3.1

## Breaking Changes

- `(manifest)` The shipped manifest files no longer register the provider under a fixed, type-based key. The `extensions` entry in each manifest is now keyed by the Command PAM provider name itself, with no `PAMProviders.` prefix and no `.PAMProvider` suffix. The shipped key is the placeholder `YOUR-PROVIDER-NAME` (bracket-free: a bracketed placeholder key made an orchestrator fail to start in the tested environment; observed once, cause not established). For a provider named `Delinea`, the key is `Delinea` and the config section is `Keyfactor:PAMProviders:Delinea:InitializationInfo`. (Fixes #22)

## Upgrade Notes

- An unedited `manifest.json` (or per-type manifest) is no longer usable: replace `YOUR-PROVIDER-NAME` with the name of the PAM provider you created in Keyfactor Command, both in the `extensions` entry key and in the `InitializationInfo` section name. The key shape changed from the 1.3.0 form (`PAMProviders.Delinea.PAMProvider`).
- Installing 1.3.1 over an edited `manifest.json` overwrites it. Re-apply your edits to the new file.
- A Universal Orchestrator supports only one PAM provider registration per `manifest.json` (until UO v26.4). To use a type-specific provider, install its manifest file (`Password-manifest.json`, `ClientCredentials-manifest.json` or `Windows-manifest.json`) as `manifest.json`. Using more than one Delinea PAM type requires a separate Universal Orchestrator for each.
- Remove any `GrantType` workaround you added to `InitializationInfo` for the type-specific providers; the type-specific classes use a fixed grant type.
- Remove any other `Keyfactor:PAMProviders:*:InitializationInfo` sections from `manifest.json` (for example, the four sample sections shipped in 1.3.0). Left in place, their placeholder values reached the plugin configuration in testing.

## Features

- `(manifest)` Ship one manifest file per PAM type, following the other Keyfactor PAM providers: `manifest.json` (`Delinea-SecretServer`, base class `SecretServerPam`), `Password-manifest.json`, `ClientCredentials-manifest.json` and `Windows-manifest.json`. Each registers its own class and contains exactly one `InitializationInfo` section, named after the provider (`Keyfactor:PAMProviders:YOUR-PROVIDER-NAME:InitializationInfo`), with placeholder values only. Previously only the base class was registered, so the type-specific classes (`SecretServerPamPassword`, `SecretServerPamClientCredentials`, `SecretServerPamWindows`) could not be loaded and type-specific PAM types silently used the generic class and defaulted the grant type to `password`. (Fixes #22)

## Bug Fixes

- `(manifest)` Each shipped manifest no longer contains sample `InitializationInfo` sections for other PAM types (1.3.0 shipped four), so placeholder values from unused samples no longer reach the plugin configuration. (Fixes #22)
- `(params)` The generic `Delinea-SecretServer` PAM type in `integration-manifest.json` now includes the `SkipTlsValidation` parameter, consistent with the type-specific types and the code.
- `(docs)` The orchestrator install step now shows the correct manifest for each PAM type (previously the Windows snippet was shown for the base, Password and ClientCredentials types). (Fixes #23)

## Documentation

- `(docs)` Each PAM type's page documents its own install steps (including renaming the per-type manifest to `manifest.json`), the provider naming rule, the one-provider-per-`manifest.json` limitation, and the Command 10 `web.config` registration with the correct class and assembly name (`delinea-secretserver-pam`). (Fixes #23)

## Tests

- `(tests)` Add `ManifestTests`, which validate every shipped manifest against the assembly and the build output: valid JSON and copied to the build output; exactly one `IPAMProvider` entry keyed exactly `YOUR-PROVIDER-NAME`; no angle brackets, `PAMProviders.` prefix or `.PAMProvider` suffix; `TypeFullName` resolves to an `IPAMProvider` class; exactly one `InitializationInfo` section, named `Keyfactor:PAMProviders:YOUR-PROVIDER-NAME:InitializationInfo`, with placeholder values and no `SkipTlsValidation: true`; together the manifests cover all four classes; every PAM type in `integration-manifest.json` matches a provider class and has `SkipTlsValidation`; and each type-specific class uses its fixed grant type when no `GrantType` is supplied.

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
