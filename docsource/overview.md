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
> Using more than one Delinea PAM type requires a separate Universal Orchestrator for each.

The release contains one manifest file per PAM type. `manifest.json` registers the generic `Delinea-SecretServer`
provider. To use a type-specific provider, replace `manifest.json` with that type's manifest file (rename it to
`manifest.json` on install) and edit it.

| PAM Type / Name | Manifest shipped in the release | Class (`TypeFullName` / `web.config` `mapTo`) |
| --- | --- | --- |
| `Delinea-SecretServer` | `manifest.json` (default) | `Keyfactor.Extensions.Pam.Delinea.SecretServerPam` |
| `Delinea-SecretServer-Password` | `Password-manifest.json` | `Keyfactor.Extensions.Pam.Delinea.SecretServerPamPassword` |
| `Delinea-SecretServer-ClientCredentials` | `ClientCredentials-manifest.json` | `Keyfactor.Extensions.Pam.Delinea.SecretServerPamClientCredentials` |
| `Delinea-SecretServer-Windows` | `Windows-manifest.json` | `Keyfactor.Extensions.Pam.Delinea.SecretServerPamWindows` |

Each manifest registers its extension under the name of the **PAM provider you created in Keyfactor Command** (the
provider instance name, not the PAM type name): the extension entry key is that name itself, with no `PAMProviders.`
prefix and no `.PAMProvider` suffix, and the first `InitializationInfo` section is named
`Keyfactor:PAMProviders:<provider name>:InitializationInfo`. The shipped placeholder is `YOUR-PROVIDER-NAME`; replace
it in both places before use. A second `InitializationInfo` section, named after the PAM type, is kept as shipped.
An unedited manifest does not register a usable provider.

The type-specific classes use a fixed grant type (`password`, `client_credentials`, `windows`), so no `GrantType`
value is required for them. For Command-hosted (Command 10) `web.config` registrations, use the class above with the
assembly name `delinea-secretserver-pam`, for example:
`mapTo="Keyfactor.Extensions.Pam.Delinea.SecretServerPamClientCredentials, delinea-secretserver-pam"`.

> [!NOTE]
> Each PAM type's page under [docs/](docs/) contains the exact `manifest.json` and `web.config` snippets for that type,
> and the manifest files are shipped in the release. The documentation generator has rendered the per-type install
> steps below incorrectly in the past (the same block shown for every type, or the step text shown as raw text instead
> of formatted steps). If the generated install steps below look wrong for your PAM type, use the snippet from that
> type's page in [docs/](docs/) or the shipped manifest file instead.

> [!WARNING]
> **Upgrading from 1.3.0 or an earlier 1.3.1 pre-release:** the shipped manifests changed key shape (see the
> changelog). Installing 1.3.1 over an edited `manifest.json` overwrites your edits, and an unedited 1.3.1 manifest
> is not usable until `YOUR-PROVIDER-NAME` is replaced with your PAM provider name. If you use a type-specific
> provider, install its manifest file as `manifest.json`, and remove any `GrantType` workaround you added.
