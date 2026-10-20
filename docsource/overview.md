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

## Dummy Values

When using the backwards-compatible `Delinea-SecretServer` type, the Keyfactor Command UI requires all declared
fields to be populated. For fields that are not applicable to the chosen authentication flow, enter `N/A` as the
value. The provider treats `N/A` (case-insensitive) as equivalent to an empty value and will not attempt to use it.

> [!TIP]
> For new installations, use the type-specific PAM types (`Delinea-SecretServer-Password`,
> `Delinea-SecretServer-ClientCredentials`, `Delinea-SecretServer-Windows`) to avoid this entirely — they only
> expose the fields relevant to the chosen authentication flow.

## TLS Validation

All PAM types support skipping TLS certificate validation for non-production environments via either:

- The `SkipTlsValidation` configuration parameter (set to `true` in the PAM provider instance)
- The `KEYFACTOR_PAM_SKIP_TLS_VALIDATION` environment variable (set to `true` or `1` on the host)

The environment variable takes precedence and overrides the configuration parameter.

> [!WARNING]
> Disabling TLS validation should only be used in non-production environments.

## Registration and Naming

> [!IMPORTANT]
> A Universal Orchestrator supports only **one** PAM provider registration per `manifest.json` (until UO v26.4).
> The `manifest.json` shipped in the release registers the generic `Delinea-SecretServer` provider only. To use a
> type-specific provider, copy its sample manifest over `manifest.json` and fill in the values. Using more than one
> Delinea PAM type requires a separate Universal Orchestrator for each.

The sample manifests are shipped in a `samples/` subfolder of the extension (not next to `manifest.json`), so the
Universal Orchestrator never picks them up by accident.

| PAM Type / Name | Manifest shipped in the release | Class (`TypeFullName` / `web.config` `mapTo`) |
| --- | --- | --- |
| `Delinea-SecretServer` | `manifest.json` (default) | `Keyfactor.Extensions.Pam.Delinea.SecretServerPam` |
| `Delinea-SecretServer-Password` | `samples/manifest.password.json` | `Keyfactor.Extensions.Pam.Delinea.SecretServerPamPassword` |
| `Delinea-SecretServer-ClientCredentials` | `samples/manifest.clientcredentials.json` | `Keyfactor.Extensions.Pam.Delinea.SecretServerPamClientCredentials` |
| `Delinea-SecretServer-Windows` | `samples/manifest.windows.json` | `Keyfactor.Extensions.Pam.Delinea.SecretServerPamWindows` |

The extension registration name (`PAMProviders.<name>.PAMProvider`) and the `InitializationInfo` key
(`Keyfactor:PAMProviders:<name>:InitializationInfo`) must both match the **PAM provider name you created in Keyfactor
Command** (by default the PAM type name). Any name works as long as all three match.

The type-specific classes use a fixed grant type (`password`, `client_credentials`, `windows`), so no `GrantType`
value is required for them. For Command-hosted (Command 10) `web.config` registrations, use the class above with the
assembly name `delinea-secretserver-pam`, for example:
`mapTo="Keyfactor.Extensions.Pam.Delinea.SecretServerPamClientCredentials, delinea-secretserver-pam"`.

> [!NOTE]
> Each PAM type's page under [docs/](docs/) contains the exact `manifest.json` and `web.config` snippets for that type.
> If the generated install steps below show an `InitializationInfo` block that belongs to a different PAM type, use the
> block from that type's page in [docs/](docs/) instead.

> [!WARNING]
> **Upgrading from 1.3.0:** if you hand-edited the registration name or `InitializationInfo` key in `manifest.json`
> to match a custom PAM provider name, those edits are lost when you replace `manifest.json` with the 1.3.1 version.
> Re-apply your naming (or rename the provider in Command to match the shipped names). If you use a type-specific
> provider, copy its sample manifest over `manifest.json`, and remove any `GrantType` workaround you added.
