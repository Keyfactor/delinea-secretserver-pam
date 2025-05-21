- [Delinea Secret Server PAM Provider](#delinea-secret-server-pam-provider)
  - [Integration status: Production - Ready for use in production environments.](#integration-status--production---ready-for-use-in-production-environments)
    * [About the Keyfactor Command PAM Provider](#about-the-keyfactor-command-pam-provider)
    * [Support for Delinea Secret Server PAM Provider](#support-for-delinea-secret-server-pam-provider)
    * [Keyfactor Command Versions Supported](#keyfactor-command-versions-supported)
        + [Initial Configuration of PAM Provider](#initial-configuration-of-pam-provider)
        + [Configuring Parameters](#configuring-parameters)
        + [Initialization Parameters for each defined PAM Provider instance](#initialization-parameters-for-each-defined-pam-provider-instance)
        + [Instance Parameters for each retrieved secret field](#instance-parameters-for-each-retrieved-secret-field)
    * [Configuring for PAM Usage](#configuring-for-pam-usage)
        + [Delinea Secret Server](#delinea-secret-server)
        + [On Keyfactor Universal Orchestrator](#on-keyfactor-universal-orchestrator)
            - [Installation](#installation)
            - [Usage](#usage)
        + [In Keyfactor - PAM Provider](#in-keyfactor---pam-provider)
            - [Installation](#installation-1)
            - [Usage](#usage-1)



## Keyfactor Version Supported

The minimum version of the Keyfactor Universal Orchestrator Framework needed to run this version of the extension is 10.1

| Keyfactor Version | Universal Orchestrator Framework Version | Supported    |
|-------------------|------------------------------------------|--------------|
| 10.4.5            | 10.1, 10.2, 10.4                         | &check;      |
| 10.4.0            | 10.1, 10.2, 10.4                         | &check;      |
| 10.2.1            | 10.1, 10.2, 10.4                         | &check;      |
| 10.1.1            | 10.1, 10.2,                              | &check;      |
| 10.0.0            | 10.1, 10.2                               | &check;      |
| 9.10.1            | Not supported on KF 9.X.X                | x            |
| 9.5.0             | Not supported on KF 9.X.X                | x            |