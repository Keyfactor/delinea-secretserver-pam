// Copyright 2025 Keyfactor
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions
// and limitations under the License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Threading.Tasks;
using Keyfactor.Extensions.Pam.Delinea.Models;
using Keyfactor.Logging;
using Keyfactor.Platform.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Keyfactor.Extensions.Pam.Delinea
{
    /// <summary>
    ///     Exception thrown when the authentication token for Delinea Secret Server is invalid or cannot be obtained.
    /// </summary>
    public class InvalidTokenException : Exception
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="InvalidTokenException" /> class with a specified error message.
        /// </summary>
        public InvalidTokenException(string message) : base(message)
        {
        }
    }

    /// <summary>
    ///     Exception thrown when the server (initialization) configuration provided to the PAM provider is invalid.
    /// </summary>
    public class InvalidClientConfigurationException : Exception
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="InvalidClientConfigurationException" /> class with a specified error
        ///     message.
        /// </summary>
        public InvalidClientConfigurationException(string message) : base(message)
        {
        }
    }

    /// <summary>
    ///     Exception thrown when the instance (per-secret) configuration provided to the PAM provider is invalid.
    /// </summary>
    public class InvalidSecretConfigurationException : Exception
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="InvalidSecretConfigurationException" /> class with a specified error
        ///     message.
        /// </summary>
        public InvalidSecretConfigurationException(string message) : base(message)
        {
        }
    }

    // ---------------------------------------------------------------------------
    // Abstract base — all shared logic lives here
    // ---------------------------------------------------------------------------

    /// <summary>
    ///     Abstract base class for all Delinea Secret Server PAM providers.
    ///     Encapsulates the shared HTTP, validation, configuration-building, and
    ///     secret-retrieval logic used by every concrete PAM type variant.
    /// </summary>
    public abstract class SecretServerPamBase
    {
        // Subclasses set their own class-specific logger via the protected setter.
        protected ILogger Logger { get; set; }

        // HttpClient is injected so tests can substitute a fake handler without
        // going to the network.  Production constructors build the real client.
        private HttpClient _httpClient;

        /// <summary>
        ///     Production constructor — builds a default <see cref="HttpClient" />.
        ///     Grant type and TLS-skip are not yet known at construction time; they
        ///     are resolved from configuration during <see cref="GetPasswordCore" />.
        /// </summary>
        protected SecretServerPamBase()
        {
            Logger = LogHandler.GetClassLogger(GetType());
            _httpClient = null; // will be built lazily in GetPasswordCore
        }

        /// <summary>
        ///     Test constructor — accepts an injected <see cref="HttpClient" /> and
        ///     <see cref="ILogger" /> so unit tests can control HTTP responses.
        /// </summary>
        internal SecretServerPamBase(HttpClient httpClient, ILogger logger)
        {
            _httpClient = httpClient;
            Logger = logger;
        }

        // ---------------------------------------------------------------------------
        // Core entry point called by every concrete GetPassword implementation
        // ---------------------------------------------------------------------------

        /// <summary>
        ///     Resolves the effective grant type for this provider invocation.
        ///     The base implementation reads it from <paramref name="serverConfigurationParameters" />,
        ///     defaulting to <c>"password"</c> for backwards compatibility.
        ///     Type-specific subclasses override this to return a hardcoded value.
        /// </summary>
        protected virtual string ResolveGrantType(IReadOnlyDictionary<string, string> serverConfigurationParameters)
        {
            if (serverConfigurationParameters.TryGetValue(DelineaConfiguration.GRANT_TYPE, out var grantType) &&
                !string.IsNullOrEmpty(grantType))
                return grantType;

            Logger.LogWarning(
                "'{GrantType}' parameter not provided — defaulting to 'password' grant",
                DelineaConfiguration.GRANT_TYPE);
            return "password";
        }

        /// <summary>
        ///     Core implementation of credential retrieval shared by all concrete types.
        ///     Validates configuration, builds an <see cref="HttpClient" />, and fetches
        ///     the secret from Delinea Secret Server.
        /// </summary>
        protected string GetPasswordCore(
            Dictionary<string, string> instanceParameters,
            Dictionary<string, string> serverConfigurationParameters)
        {
            Logger.MethodEntry();

            instanceParameters.TryGetValue(DelineaConfiguration.SECRET_ID, out var logSecretId);
            instanceParameters.TryGetValue(DelineaConfiguration.SECRET_FIELD_NAME, out var logFieldName);
            serverConfigurationParameters.TryGetValue(DelineaConfiguration.SECRET_SERVER_URL, out var logUrl);
            var logGrantType = ResolveGrantType(serverConfigurationParameters);

            // UserName is the OS service account identity — IPAMProvider does not expose the Keyfactor caller
            Logger.LogInformation(
                "GetPassword invoked | SecretId={SecretId} Field={SecretFieldName} TargetUrl={Url} GrantType={GrantType} CallerIdentity={Identity} Host={Machine}",
                logSecretId, logFieldName, logUrl, logGrantType,
                Environment.UserName, Environment.MachineName);

            var correlationId = Guid.NewGuid().ToString("N");
            Logger.LogInformation("Operation correlation ID | CorrelationId={CorrelationId}", correlationId);
            Logger.LogTrace("instanceParameters: {@InstanceParameters}", instanceParameters);

            var config = BuildDelineaConfiguration(instanceParameters, serverConfigurationParameters);

            // Use the injected client (tests) or build a real one (production)
            var client = _httpClient ?? BuildHttpClient(config.GrantType, config.SkipTlsValidation);
            var ownsClient = _httpClient == null;
            try
            {
                Logger.MethodExit();
                return GetDelineaSecretAsync(client, config, correlationId).GetAwaiter().GetResult();
            }
            finally
            {
                if (ownsClient)
                    client.Dispose();
            }
        }

        // ---------------------------------------------------------------------------
        // Secret retrieval
        // ---------------------------------------------------------------------------

        private async Task<string> GetDelineaSecretAsync(
            HttpClient client,
            DelineaConfiguration configurationInfo,
            string correlationId)
        {
            Logger.MethodEntry();
            HttpResponseMessage response;
            Logger.LogDebug("Attempting to fetch secret from Delinea Secret Server at {SecretServerUrl}",
                configurationInfo.SecretServerUrl);

            var secretUrl = $"{configurationInfo.SecretServerUrl}/api/v1/secrets/{configurationInfo.SecretId}";

            switch (configurationInfo.GrantType)
            {
                case "windows":
                    Logger.LogDebug("Using Windows Authentication");
                    secretUrl =
                        $"{configurationInfo.SecretServerUrl}/winauthwebservices/api/v1/secrets/{configurationInfo.SecretId}";
                    // UserName is the OS service account identity — IPAMProvider does not expose the Keyfactor caller
                    Logger.LogInformation(
                        "Windows authentication attempt | Identity={Identity} Machine={Machine} TargetUrl={TargetUrl} SecretId={SecretId} CorrelationId={CorrelationId}",
                        Environment.UserName, Environment.MachineName, secretUrl, configurationInfo.SecretId,
                        correlationId);
                    break;

                default: // password and client_credentials
                    Logger.LogDebug("Using {GrantType} grant to obtain access token", configurationInfo.GrantType);
                    var bearerToken =
                        await GetAccessToken(client, configurationInfo, correlationId).ConfigureAwait(false);

                    if (string.IsNullOrEmpty(bearerToken))
                    {
                        Logger.LogError(
                            "Authentication failed: empty token received | Url={Url} GrantType={GrantType} Identity={Identity} CorrelationId={CorrelationId}",
                            configurationInfo.SecretServerUrl, configurationInfo.GrantType,
                            string.IsNullOrEmpty(configurationInfo.Username)
                                ? configurationInfo.ClientId
                                : configurationInfo.Username,
                            correlationId);
                        Logger.MethodExit();
                        throw new InvalidTokenException("Unable to obtain access token from Delinea Secret Server");
                    }

                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", bearerToken);
                    client.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json"));
                    break;
            }

            var sw = Stopwatch.StartNew();
            try
            {
                Logger.LogDebug("Secret URL: {SecretUrl}", secretUrl);
                response = await client
                    .GetAsync(new Uri(secretUrl).AbsoluteUri)
                    .ConfigureAwait(false);
                sw.Stop();
                Logger.LogInformation(
                    "Secret Server API call completed | Method=GET StatusCode={StatusCode} DurationMs={DurationMs} SecretId={SecretId} CorrelationId={CorrelationId}",
                    (int)response.StatusCode, sw.ElapsedMilliseconds, configurationInfo.SecretId, correlationId);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var truncated = errorContent?.Length > 500
                        ? errorContent.Substring(0, 500) + "..."
                        : errorContent;
                    Logger.LogError(
                        "Received non-success status code {StatusCode} from Secret Server. Response (truncated): {ResponseContent} CorrelationId={CorrelationId}",
                        (int)response.StatusCode, truncated, correlationId);
                }

                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                sw.Stop();
                Logger.LogError(
                    "HTTP call failed | Method={Method} Url={Url} DurationMs={DurationMs} Error={ExMessage} CorrelationId={CorrelationId}",
                    "GET", secretUrl, sw.ElapsedMilliseconds, ex.Message, correlationId);
                Logger.MethodExit();
                throw;
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                sw.Stop();
                Logger.LogError(
                    "HTTP call failed | Method={Method} Url={Url} DurationMs={DurationMs} Error={ExMessage} CorrelationId={CorrelationId}",
                    "GET", secretUrl, sw.ElapsedMilliseconds, ex.Message, correlationId);
                Logger.MethodExit();
                throw new InvalidClientConfigurationException(
                    "A Windows authentication error occurred while attempting to communicate with Delinea Secret Server. " +
                    "Please ensure the application is running under a user context with access to Secret Server. " +
                    "For more information on windows auth please visit: " +
                    "https://docs.delinea.com/online-help/secret-server/authentication/iwa-webservices/webservice-iwa-powershell/index.htm");
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            Logger.LogDebug("Attempting to deserialize Delinea Secret Server response content from {Url}", secretUrl);
            try
            {
                var secretResponse = JsonConvert.DeserializeObject<SecretResponse>(content);

                Logger.LogTrace("Received '{ItemsCount}' secret items from Delinea Secret Server",
                    secretResponse?.Items.Count ?? 0);
                Logger.LogTrace("Secret field name: {SecretFieldName}", configurationInfo.SecretFieldName);

                var secret = secretResponse?.Items.FirstOrDefault(i =>
                    i.Name == configurationInfo.SecretFieldName ||
                    i.Slug == configurationInfo.SecretFieldName)?.Value;

                if (!string.IsNullOrEmpty(secret))
                {
                    Logger.LogInformation(
                        "Credential retrieval succeeded | SecretId={SecretId} Field={SecretFieldName} GrantType={GrantType} Url={Url} CorrelationId={CorrelationId}",
                        configurationInfo.SecretId, configurationInfo.SecretFieldName,
                        configurationInfo.GrantType, configurationInfo.SecretServerUrl, correlationId);
                    Logger.MethodExit();
                    return secret;
                }
            }
            catch (Exception ex)
            {
                if (content != null && content.Contains("login-message"))
                {
                    Logger.LogError(
                        "Authentication failed when attempting to retrieve secret from Delinea Secret Server — check credentials and configuration. CorrelationId={CorrelationId}",
                        correlationId);
                    Logger.LogTrace("Response content: {Response}", content);
                    Logger.MethodExit();
                    throw new AuthenticationException(
                        "Authentication failed when attempting to retrieve secret from Delinea Secret Server. Please check your credentials and try again");
                }

                Logger.LogError(
                    "An error occurred while attempting to deserialize the Delinea Secret Server response: {ExMessage} CorrelationId={CorrelationId}",
                    ex.Message, correlationId);
                Logger.MethodExit();
                throw;
            }

            Logger.LogError(
                "Credential retrieval failed: field not found in secret | SecretId={SecretId} Field={SecretFieldName} GrantType={GrantType} Url={Url} CorrelationId={CorrelationId}",
                configurationInfo.SecretId, configurationInfo.SecretFieldName,
                configurationInfo.GrantType, configurationInfo.SecretServerUrl, correlationId);
            Logger.MethodExit();
            throw new InvalidSecretConfigurationException(
                $"Field '{configurationInfo.SecretFieldName}' not found in secret {configurationInfo.SecretId}. " +
                "Verify the field name or slug exists on the secret template.");
        }

        // ---------------------------------------------------------------------------
        // Token acquisition
        // ---------------------------------------------------------------------------

        private async Task<string> GetAccessToken(
            HttpClient client,
            DelineaConfiguration configurationInfo,
            string correlationId)
        {
            Logger.MethodEntry();

            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));

            // NOTE: Delinea Secret Server's token endpoint always uses "username"/"password"
            // field names regardless of whether the flow is password or client_credentials.
            // This is a Delinea API constraint — do not change the field names.
            var body = new Dictionary<string, string>
            {
                { "username", configurationInfo.Username },
                { "password", configurationInfo.Password },
                { "grant_type", "password" } // Delinea API always expects grant_type=password
            };

            Logger.LogTrace("Authentication request grant type: {GrantType}", body["grant_type"]);

            var loggableBody = new Dictionary<string, string>(body);
            foreach (var sensitiveKey in new[] { "password", "client_secret" })
                if (loggableBody.ContainsKey(sensitiveKey))
                    loggableBody[sensitiveKey] = "***";
            Logger.LogDebug("Token request body (redacted): {RequestBody}", JsonConvert.SerializeObject(loggableBody));

            HttpResponseMessage response;
            var tokenUrl = $"{configurationInfo.SecretServerUrl}/oauth2/token";
            var sw = Stopwatch.StartNew();

            try
            {
                Logger.LogDebug("Requesting access token from Secret Server at {TokenUrl}", tokenUrl);
                response = await client
                    .PostAsync(new Uri(tokenUrl).AbsoluteUri, new FormUrlEncodedContent(body))
                    .ConfigureAwait(false);
                sw.Stop();
                Logger.LogInformation(
                    "Token endpoint call completed | Method=POST StatusCode={StatusCode} DurationMs={DurationMs} CorrelationId={CorrelationId}",
                    (int)response.StatusCode, sw.ElapsedMilliseconds, correlationId);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var truncatedError = errorBody?.Length > 500 ? errorBody.Substring(0, 500) + "..." : errorBody;
                    Logger.LogError(
                        "Token request failed | StatusCode={StatusCode} ResponseBody={ResponseBody} CorrelationId={CorrelationId}",
                        (int)response.StatusCode, truncatedError, correlationId);
                    response.EnsureSuccessStatusCode();
                }
            }
            catch (HttpRequestException ex)
            {
                sw.Stop();
                Logger.LogError(
                    "HTTP call failed | Method={Method} Url={Url} DurationMs={DurationMs} Error={ExMessage} CorrelationId={CorrelationId}",
                    "POST", tokenUrl, sw.ElapsedMilliseconds, ex.Message, correlationId);
                Logger.MethodExit();
                throw;
            }

            Logger.LogDebug("Access token received, deserializing response");

            try
            {
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                var token = values?["access_token"];
                client.DefaultRequestHeaders.Accept.Clear();

                Logger.LogTrace("Access token parsed successfully");
                if (token != null)
                {
                    Logger.LogInformation(
                        "Authentication succeeded | Identity={Identity} Url={Url} AuthenticationResult=Success CorrelationId={CorrelationId}",
                        string.IsNullOrEmpty(configurationInfo.Username)
                            ? configurationInfo.ClientId
                            : configurationInfo.Username,
                        configurationInfo.SecretServerUrl, correlationId);
                    return token;
                }

                Logger.LogError(
                    "Unable to generate access token from Delinea Secret Server '{Url}'. Please check your credentials and try again. CorrelationId={CorrelationId}",
                    configurationInfo.SecretServerUrl, correlationId);
                Logger.MethodExit();
                throw new InvalidTokenException(
                    $"Unable to generate access token from Delinea Secret Server '{configurationInfo.SecretServerUrl}'. Please check your credentials and try again");
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    "An error occurred while attempting to deserialize the access token response: {ExMessage} CorrelationId={CorrelationId}",
                    ex.Message, correlationId);
                Logger.MethodExit();
                throw;
            }
        }

        // ---------------------------------------------------------------------------
        // Validation
        // ---------------------------------------------------------------------------

        /// <summary>
        ///     Validates instance parameters (SecretId, SecretFieldName).
        ///     Throws <see cref="InvalidSecretConfigurationException" /> on failure.
        /// </summary>
        private bool ValidateInstanceParams(IReadOnlyDictionary<string, string> instanceParameters)
        {
            Logger.MethodEntry();
            Logger.LogDebug("Validating instance parameters");

            if (!instanceParameters.ContainsKey(DelineaConfiguration.SECRET_ID))
            {
                Logger.LogError("Instance parameter '{SecretId}' not found", DelineaConfiguration.SECRET_ID);
                Logger.MethodExit();
                throw new InvalidSecretConfigurationException(
                    $"Instance parameter '{DelineaConfiguration.SECRET_ID}' not found");
            }

            if (!instanceParameters.ContainsKey(DelineaConfiguration.SECRET_FIELD_NAME) ||
                string.IsNullOrWhiteSpace(instanceParameters[DelineaConfiguration.SECRET_FIELD_NAME]))
            {
                Logger.LogError("Instance parameter '{SecretFieldName}' not provided",
                    DelineaConfiguration.SECRET_FIELD_NAME);
                Logger.MethodExit();
                throw new InvalidSecretConfigurationException(
                    $"Instance parameter '{DelineaConfiguration.SECRET_FIELD_NAME}' not provided");
            }

            if (int.TryParse(instanceParameters[DelineaConfiguration.SECRET_ID], out _))
            {
                Logger.LogDebug("Instance parameters are valid");
                Logger.MethodExit();
                return true;
            }

            Logger.LogError("Unable to parse {SecretId} as an integer", DelineaConfiguration.SECRET_ID);
            Logger.MethodExit();
            throw new InvalidSecretConfigurationException(
                $"Unable to parse '{instanceParameters[DelineaConfiguration.SECRET_ID]}' as an integer");
        }

        /// <summary>
        ///     Validates server configuration parameters for the resolved grant type.
        ///     Throws <see cref="InvalidClientConfigurationException" /> on failure.
        /// </summary>
        private bool ValidateServerConfigurationParams(
            IReadOnlyDictionary<string, string> connectionConfiguration,
            string grantType)
        {
            Logger.MethodEntry();
            Logger.LogDebug("Validating server configuration parameters for grant type '{GrantType}'", grantType);

            ValidateRequiredParameter(connectionConfiguration, DelineaConfiguration.SECRET_SERVER_URL,
                "Server configuration parameter");

            switch (grantType)
            {
                case "password":
                    ValidatePasswordGrantCredentials(connectionConfiguration);
                    break;
                case "client_credentials":
                    ValidateClientCredentialsGrantCredentials(connectionConfiguration);
                    break;
                case "windows":
                    Logger.LogDebug("Using Windows Authentication — no credential parameters to validate");
                    break;
                default:
                    Logger.LogError(
                        "Invalid grant type '{GrantType}' specified. Supported values are 'password', 'client_credentials', and 'windows'",
                        grantType);
                    Logger.MethodExit();
                    throw new InvalidClientConfigurationException(
                        $"Invalid grant type '{grantType}' specified. Supported values are 'password', 'client_credentials', and 'windows'");
            }

            Logger.LogInformation("Server configuration parameters are valid");
            Logger.MethodExit();
            return true;
        }

        private void ValidateRequiredParameter(
            IReadOnlyDictionary<string, string> config,
            string paramName,
            string errorPrefix)
        {
            Logger.MethodEntry();
            Logger.LogDebug("Validating parameter '{ParamName}'", paramName);

            if (config.ContainsKey(paramName) && !string.IsNullOrEmpty(config[paramName]))
            {
                Logger.MethodExit();
                return;
            }

            Logger.LogError("{ErrorPrefix} '{ParamName}' not provided", errorPrefix, paramName);
            Logger.MethodExit();
            throw new InvalidClientConfigurationException($"{errorPrefix} '{paramName}' not provided");
        }

        private void ValidatePasswordGrantCredentials(IReadOnlyDictionary<string, string> config)
        {
            Logger.MethodEntry();
            ValidateRequiredParameter(config, DelineaConfiguration.USERNAME, "Client configuration parameter");
            ValidateRequiredParameter(config, DelineaConfiguration.PASSWORD, "Client configuration parameter");
            Logger.MethodExit();
        }

        private void ValidateClientCredentialsGrantCredentials(IReadOnlyDictionary<string, string> config)
        {
            Logger.MethodEntry();
            ValidateRequiredParameter(config, DelineaConfiguration.CLIENT_ID, "Client configuration parameter");
            ValidateRequiredParameter(config, DelineaConfiguration.CLIENT_SECRET, "Client configuration parameter");
            Logger.MethodExit();
        }

        // ---------------------------------------------------------------------------
        // Configuration builder
        // ---------------------------------------------------------------------------

        private DelineaConfiguration BuildDelineaConfiguration(
            IReadOnlyDictionary<string, string> instanceParameters,
            IReadOnlyDictionary<string, string> connectionConfiguration)
        {
            Logger.MethodEntry();
            Logger.LogInformation("Validating Delinea configuration");

            var grantType = ResolveGrantType(connectionConfiguration);

            var validServer = ValidateServerConfigurationParams(connectionConfiguration, grantType);
            var validInstance = ValidateInstanceParams(instanceParameters);

            if (!validServer || !validInstance)
            {
                Logger.LogError("Delinea PAM provider configuration is invalid");
                Logger.MethodExit();
                throw new InvalidClientConfigurationException(
                    "Delinea configuration is invalid, please review server logs.");
            }

            var secretId = int.Parse(instanceParameters[DelineaConfiguration.SECRET_ID]);
            Logger.LogDebug("Secret ID: {SecretId}", secretId);

            connectionConfiguration.TryGetValue(DelineaConfiguration.SKIP_TLS_VALIDATION, out var skipTlsRaw);
            var skipTls = string.Equals(skipTlsRaw, "true", StringComparison.OrdinalIgnoreCase);
            if (skipTls)
                Logger.LogWarning(
                    "TLS certificate validation is disabled — use only in non-production environments");

            Logger.LogDebug("Building Delinea configuration for '{GrantType}' grant type", grantType);

            switch (grantType)
            {
                case "password":
                    Logger.MethodExit();
                    return new DelineaConfiguration
                    {
                        SecretServerUrl = connectionConfiguration[DelineaConfiguration.SECRET_SERVER_URL],
                        Username = connectionConfiguration[DelineaConfiguration.USERNAME],
                        Password = connectionConfiguration[DelineaConfiguration.PASSWORD],
                        SecretId = secretId,
                        SecretFieldName = instanceParameters[DelineaConfiguration.SECRET_FIELD_NAME],
                        GrantType = "password",
                        SkipTlsValidation = skipTls
                    };

                case "client_credentials":
                    Logger.MethodExit();
                    return new DelineaConfiguration
                    {
                        SecretServerUrl = connectionConfiguration[DelineaConfiguration.SECRET_SERVER_URL],
                        // NOTE: For client_credentials the ClientId maps to Username and ClientSecret maps to
                        // Password in the token request body. This is a Delinea API constraint.
                        Username = connectionConfiguration[DelineaConfiguration.CLIENT_ID],
                        Password = connectionConfiguration[DelineaConfiguration.CLIENT_SECRET],
                        ClientId = connectionConfiguration[DelineaConfiguration.CLIENT_ID],
                        ClientSecret = connectionConfiguration[DelineaConfiguration.CLIENT_SECRET],
                        SecretId = secretId,
                        SecretFieldName = instanceParameters[DelineaConfiguration.SECRET_FIELD_NAME],
                        GrantType = "client_credentials",
                        SkipTlsValidation = skipTls
                    };

                case "windows":
                    Logger.MethodExit();
                    return new DelineaConfiguration
                    {
                        SecretServerUrl = connectionConfiguration[DelineaConfiguration.SECRET_SERVER_URL],
                        SecretId = secretId,
                        SecretFieldName = instanceParameters[DelineaConfiguration.SECRET_FIELD_NAME],
                        GrantType = "windows",
                        SkipTlsValidation = skipTls
                    };

                default:
                    Logger.LogError(
                        "Invalid grant type '{GrantType}' — supported values are 'password', 'client_credentials', and 'windows'",
                        grantType);
                    Logger.MethodExit();
                    throw new InvalidClientConfigurationException(
                        $"Invalid grant type '{grantType}' specified. Supported values are 'password', 'client_credentials', and 'windows'");
            }
        }

        // ---------------------------------------------------------------------------
        // HttpClient factory
        // ---------------------------------------------------------------------------

        private static HttpClient BuildHttpClient(string grantType, bool skipTlsValidation = false)
        {
            var handler = new HttpClientHandler();
            if (grantType == "windows")
                handler.UseDefaultCredentials = true;
            if (skipTlsValidation)
                handler.ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true;
            var client = new HttpClient(handler, true);
            client.Timeout = new TimeSpan(0, 0, 60);
            return client;
        }
    }

    // ---------------------------------------------------------------------------
    // Concrete PAM type implementations
    // ---------------------------------------------------------------------------

    /// <summary>
    ///     Backwards-compatible PAM provider for Delinea Secret Server.
    ///     Supports all three authentication flows (password, client_credentials, windows)
    ///     selected at runtime via the <c>GrantType</c> server configuration parameter.
    ///     Prefer the type-specific variants for new installations.
    /// </summary>
    public class SecretServerPam : SecretServerPamBase, IPAMProvider
    {
        /// <summary>Production constructor — no arguments, required by Keyfactor Command.</summary>
        public SecretServerPam()
        {
            Logger = LogHandler.GetClassLogger<SecretServerPam>();
        }

        /// <summary>Test constructor — accepts injected dependencies.</summary>
        internal SecretServerPam(HttpClient httpClient, ILogger logger)
            : base(httpClient, logger)
        {
        }

        /// <inheritdoc />
        public string Name => "Delinea-SecretServer";

        /// <inheritdoc />
        public string GetPassword(
            Dictionary<string, string> instanceParameters,
            Dictionary<string, string> serverConfigurationParameters)
            => GetPasswordCore(instanceParameters, serverConfigurationParameters);
    }

    /// <summary>
    ///     PAM provider for Delinea Secret Server using the <c>password</c> grant type (Username + Password).
    ///     Only the <c>Host</c>, <c>Username</c>, and <c>Password</c> server parameters are required.
    /// </summary>
    public class SecretServerPamPassword : SecretServerPamBase, IPAMProvider
    {
        /// <summary>Production constructor — no arguments, required by Keyfactor Command.</summary>
        public SecretServerPamPassword()
        {
            Logger = LogHandler.GetClassLogger<SecretServerPamPassword>();
        }

        /// <summary>Test constructor — accepts injected dependencies.</summary>
        internal SecretServerPamPassword(HttpClient httpClient, ILogger logger)
            : base(httpClient, logger)
        {
        }

        /// <inheritdoc />
        public string Name => "Delinea-SecretServer-Password";

        /// <summary>Always returns <c>"password"</c> — hardcoded for this type.</summary>
        protected override string ResolveGrantType(IReadOnlyDictionary<string, string> serverConfigurationParameters)
            => "password";

        /// <inheritdoc />
        public string GetPassword(
            Dictionary<string, string> instanceParameters,
            Dictionary<string, string> serverConfigurationParameters)
            => GetPasswordCore(instanceParameters, serverConfigurationParameters);
    }

    /// <summary>
    ///     PAM provider for Delinea Secret Server using the <c>client_credentials</c> OAuth2 flow (ClientId + ClientSecret).
    ///     Only the <c>Host</c>, <c>ClientId</c>, and <c>ClientSecret</c> server parameters are required.
    /// </summary>
    public class SecretServerPamClientCredentials : SecretServerPamBase, IPAMProvider
    {
        /// <summary>Production constructor — no arguments, required by Keyfactor Command.</summary>
        public SecretServerPamClientCredentials()
        {
            Logger = LogHandler.GetClassLogger<SecretServerPamClientCredentials>();
        }

        /// <summary>Test constructor — accepts injected dependencies.</summary>
        internal SecretServerPamClientCredentials(HttpClient httpClient, ILogger logger)
            : base(httpClient, logger)
        {
        }

        /// <inheritdoc />
        public string Name => "Delinea-SecretServer-ClientCredentials";

        /// <summary>Always returns <c>"client_credentials"</c> — hardcoded for this type.</summary>
        protected override string ResolveGrantType(IReadOnlyDictionary<string, string> serverConfigurationParameters)
            => "client_credentials";

        /// <inheritdoc />
        public string GetPassword(
            Dictionary<string, string> instanceParameters,
            Dictionary<string, string> serverConfigurationParameters)
            => GetPasswordCore(instanceParameters, serverConfigurationParameters);
    }

    /// <summary>
    ///     PAM provider for Delinea Secret Server using Integrated Windows Authentication (IWA).
    ///     Only the <c>Host</c> server parameter is required.
    ///     NOTE: IWA is not supported on Secret Server Cloud.
    /// </summary>
    public class SecretServerPamWindows : SecretServerPamBase, IPAMProvider
    {
        /// <summary>Production constructor — no arguments, required by Keyfactor Command.</summary>
        public SecretServerPamWindows()
        {
            Logger = LogHandler.GetClassLogger<SecretServerPamWindows>();
        }

        /// <summary>Test constructor — accepts injected dependencies.</summary>
        internal SecretServerPamWindows(HttpClient httpClient, ILogger logger)
            : base(httpClient, logger)
        {
        }

        /// <inheritdoc />
        public string Name => "Delinea-SecretServer-Windows";

        /// <summary>Always returns <c>"windows"</c> — hardcoded for this type.</summary>
        protected override string ResolveGrantType(IReadOnlyDictionary<string, string> serverConfigurationParameters)
            => "windows";

        /// <inheritdoc />
        public string GetPassword(
            Dictionary<string, string> instanceParameters,
            Dictionary<string, string> serverConfigurationParameters)
            => GetPasswordCore(instanceParameters, serverConfigurationParameters);
    }
}
