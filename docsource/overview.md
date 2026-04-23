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

## TLS Validation

All PAM types support skipping TLS certificate validation for non-production environments via either:

- The `SkipTlsValidation` configuration parameter (set to `true` in the PAM provider instance)
- The `KEYFACTOR_PAM_SKIP_TLS_VALIDATION` environment variable (set to `true` or `1` on the host)

The environment variable takes precedence and overrides the configuration parameter.

> [!WARNING]
> Disabling TLS validation should only be used in non-production environments.