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
> For each PAM type, the **PAM provider name created in Keyfactor Command**, the **extension registration name** in
> `manifest.json` (`PAMProviders.<name>.PAMProvider`), and the **`InitializationInfo` key**
> (`Keyfactor:PAMProviders:<name>:InitializationInfo`) must all be identical. The release `manifest.json` registers all
> four PAM types, each mapped to its own class:

| PAM Type / Name | Class (`TypeFullName` / `web.config` `mapTo`) |
| --- | --- |
| `Delinea-SecretServer` | `Keyfactor.Extensions.Pam.Delinea.SecretServerPam` |
| `Delinea-SecretServer-Password` | `Keyfactor.Extensions.Pam.Delinea.SecretServerPamPassword` |
| `Delinea-SecretServer-ClientCredentials` | `Keyfactor.Extensions.Pam.Delinea.SecretServerPamClientCredentials` |
| `Delinea-SecretServer-Windows` | `Keyfactor.Extensions.Pam.Delinea.SecretServerPamWindows` |

The type-specific classes use a fixed grant type (`password`, `client_credentials`, `windows`), so no `GrantType`
value is required for them. For Command-hosted (Command 10) `web.config` registrations, use the class above with the
assembly name `delinea-secretserver-pam`, for example:
`mapTo="Keyfactor.Extensions.Pam.Delinea.SecretServerPamClientCredentials, delinea-secretserver-pam"`.

> [!NOTE]
> Each PAM type's page under [docs/](docs/) contains the exact `manifest.json` and `web.config` snippets for that type.
> If the generated install steps below show an `InitializationInfo` block that belongs to a different PAM type, use the
> block from that type's page in [docs/](docs/) instead.
