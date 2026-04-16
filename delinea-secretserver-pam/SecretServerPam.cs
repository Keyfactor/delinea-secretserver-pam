// Copyright 2025 Keyfactor
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions
// and limitations under the License.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
    /// <remarks>
    ///     This exception is typically thrown when authentication credentials are incorrect or the server rejects the auth
    ///     request.
    /// </remarks>
    public class InvalidTokenException : Exception
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="InvalidTokenException" /> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public InvalidTokenException(string message) : base(message)
        {
        }
    }

    public class InvalidClientConfigurationException : Exception
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="InvalidClientConfigurationException" /> class with a specified error
        ///     message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public InvalidClientConfigurationException(string message) : base(message)
        {
        }
    }

    public class InvalidSecretConfigurationException : Exception
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="InvalidSecretConfigurationException" /> class with a specified error
        ///     message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public InvalidSecretConfigurationException(string message) : base(message)
        {
        }
    }

    /// <summary>
    ///     Privileged Access Management (PAM) provider implementation for Delinea Secret Server.
    /// </summary>
    /// <remarks>
    ///     This class implements the IPAMProvider interface to retrieve secrets from Delinea Secret Server.
    ///     It supports authentication via username/password with plans for client credentials support.
    /// </remarks>
    public class SecretServerPam : IPAMProvider
    {
        private ILogger Logger { get; } = LogHandler.GetClassLogger<SecretServerPam>();

        /// <summary>
        ///     Gets the name of this PAM provider.
        /// </summary>
        /// <value>The string "Delinea-SecretServer".</value>
        public string Name => "Delinea-SecretServer";

        /// <summary>
        ///     Retrieves a password from Delinea Secret Server using the provided configuration parameters.
        /// </summary>
        /// <param name="instanceParameters">Dictionary containing instance-specific parameters like SecretId and SecretFieldName.</param>
        /// <param name="serverConfigurationParameters">
        ///     Dictionary containing connection and authentication parameters such as host URL,
        ///     username, and password.
        /// </param>
        /// <returns>The password value retrieved from Secret Server.</returns>
        /// <exception cref="Exception">Thrown when required parameters are missing or invalid.</exception>
        /// <exception cref="InvalidTokenException">Thrown when authentication with Secret Server fails.</exception>
        /// <exception cref="HttpRequestException">Thrown when communication with Secret Server fails.</exception>
        public string GetPassword(Dictionary<string, string> instanceParameters,
            Dictionary<string, string> serverConfigurationParameters)
        {
            Logger.MethodEntry();
            instanceParameters.TryGetValue(DelineaConfiguration.SECRET_ID, out var logSecretId);
            instanceParameters.TryGetValue(DelineaConfiguration.SECRET_FIELD_NAME, out var logFieldName);
            serverConfigurationParameters.TryGetValue(DelineaConfiguration.SECRET_SERVER_URL, out var logUrl);
            serverConfigurationParameters.TryGetValue(DelineaConfiguration.GRANT_TYPE, out var logGrantType);
            Logger.LogInformation(
                "GetPassword invoked | SecretId={SecretId} Field={SecretFieldName} TargetUrl={Url} GrantType={GrantType} CallerIdentity={Identity} Host={Machine}",
                logSecretId, logFieldName, logUrl, logGrantType ?? "password",
                Environment.UserName, Environment.MachineName);
            Logger.LogTrace("instanceParameters: {@InstanceParameters}", instanceParameters);
            var config = BuildDelineaConfiguration(instanceParameters, serverConfigurationParameters);
            using (var client = BuildHttpClient(config.GrantType))
            {
                Logger.MethodExit();
                return GetDelineaSecretAsync(client, config).GetAwaiter().GetResult();
            }
        }

        /// <summary>
        ///     Asynchronously retrieves a secret from Delinea Secret Server.
        /// </summary>
        /// <param name="client">The HTTP client used to communicate with Secret Server.</param>
        /// <param name="configurationInfo">The configuration containing Secret Server connection and request details.</param>
        /// <returns>The value of the requested secret field.</returns>
        /// <exception cref="HttpRequestException">Thrown when the HTTP request to Secret Server fails.</exception>
        /// <exception cref="Exception">Thrown when deserializing the response fails or the requested secret is not found.</exception>
        private async Task<string> GetDelineaSecretAsync(HttpClient client, DelineaConfiguration configurationInfo)
        {
            Logger.MethodEntry();
            HttpResponseMessage response;
            Logger.LogDebug("Attempting to fetch access token from Delinea Secret Server at {SecretServerUrl}",
                configurationInfo.SecretServerUrl);

            var secretUrl = $"{configurationInfo.SecretServerUrl}/api/v1/secrets/{configurationInfo.SecretId}";
            switch (configurationInfo.GrantType)
            {
                case "windows":
                    Logger.LogDebug("Using Windows Authentication to obtain access token");
                    secretUrl = $"{configurationInfo.SecretServerUrl}/winauthwebservices/api/v1/secrets/{configurationInfo.SecretId}";
                    break;
                default: // password and client_credentials
                    Logger.LogDebug("Using {GrantType} grant to obtain access token", configurationInfo.GrantType);
                    var bearerToken = await GetAccessToken(client, configurationInfo).ConfigureAwait(false);

                    if (string.IsNullOrEmpty(bearerToken))
                    {
                        Logger.LogError(
                            "Authentication failed: empty token received | Url={Url} GrantType={GrantType} Identity={Identity}",
                            configurationInfo.SecretServerUrl, configurationInfo.GrantType,
                            string.IsNullOrEmpty(configurationInfo.Username) ? configurationInfo.ClientId : configurationInfo.Username);
                        Logger.MethodExit();
                        throw new InvalidTokenException("Unable to obtain access token from Delinea Secret Server");
                    }

                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    break;
            }
            
            try
            {
                Logger.LogDebug("Secret URL: {SecretUrl}", secretUrl);
                var sw = Stopwatch.StartNew();
                response = await client
                    .GetAsync(new Uri(secretUrl)
                        .AbsoluteUri)
                    .ConfigureAwait(false);
                sw.Stop();
                Logger.LogInformation(
                    "Secret Server API call completed | Method=GET StatusCode={StatusCode} DurationMs={DurationMs} SecretId={SecretId}",
                    (int)response.StatusCode, sw.ElapsedMilliseconds, configurationInfo.SecretId);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var truncated = errorContent?.Length > 500 ? errorContent.Substring(0, 500) + "..." : errorContent;
                    Logger.LogError(
                        "Received non-success status code {StatusCode} from Secret Server. Response (truncated): {ResponseContent}",
                        (int)response.StatusCode, truncated);
                }

                response.EnsureSuccessStatusCode();
            }

            catch (HttpRequestException ex)
            {
                Logger.LogError(
                    "An error occurred while attempting to communicate with Delinea Secret Server: {ExMessage}",
                    ex.Message);
                Logger.MethodExit();
                throw;
            }
            
            catch (System.ComponentModel.Win32Exception ex)
            {
                Logger.LogError(
                    "A Windows authentication error occurred while attempting to communicate with Delinea Secret Server: {ExMessage}",
                    ex.Message);
                Logger.MethodExit();
                throw new InvalidClientConfigurationException(
                    "A Windows authentication error occurred while attempting to communicate with Delinea Secret Server. Please ensure the application is running under a user context with access to Secret Server. For more information on windows auth please visit: https://docs.delinea.com/online-help/secret-server/authentication/iwa-webservices/webservice-iwa-powershell/index.htm");
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            Logger.LogDebug("Attempting to deserialize Delinea Secret Server response content from {Url}", secretUrl);
            try
            {
                var secretResponse = JsonConvert.DeserializeObject<SecretResponse>(content);

                Logger.LogTrace("Received '{ItemsCount}' secrets from Delinea Secret Server",
                    secretResponse?.Items.Count ?? 0);

                Logger.LogTrace("Secret field name: {SecretFieldName}", configurationInfo.SecretFieldName);
                Logger.LogTrace("Secret slug: {SecretSlug}", configurationInfo.SecretFieldName);
                // var secret = secretResponse?.Items.FirstOrDefault(i => i.IsPassword)?.Value;
                var secret = secretResponse?.Items.FirstOrDefault(i =>
                    i.Name == configurationInfo.SecretFieldName || i.Slug == configurationInfo.SecretFieldName)?.Value;
                // Logger.LogDebug("Secret value: {SecretValue}", secret);
                if (!string.IsNullOrEmpty(secret))
                {
                    Logger.LogInformation(
                        "Credential retrieval succeeded | SecretId={SecretId} Field={SecretFieldName} GrantType={GrantType} Url={Url}",
                        configurationInfo.SecretId, configurationInfo.SecretFieldName,
                        configurationInfo.GrantType, configurationInfo.SecretServerUrl);
                    Logger.MethodExit();
                    return secret;
                }
            }
            catch (Exception ex)
            {
                if (content != null && content.Contains("login-message"))
                {
                    Logger.LogError(
                        "Authentication failed when attempting to retrieve secret from Delinea Secret Server, please check your credentials and configuration and try again");
                    Logger.LogTrace("Response content: {Response}", content);
                    Logger.MethodExit();
                    throw new AuthenticationException(
                        "Authentication failed when attempting to retrieve secret from Delinea Secret Server. Please check your credentials and try again");
                }
                Logger.LogError(
                    "An error occurred while attempting to deserialize the Delinea Secret Server response: {ExMessage}",
                    ex.Message);
                Logger.MethodExit();
                throw;
            }

            Logger.LogError(
                "Credential retrieval failed: field not found in secret | SecretId={SecretId} Field={SecretFieldName} GrantType={GrantType} Url={Url}",
                configurationInfo.SecretId, configurationInfo.SecretFieldName,
                configurationInfo.GrantType, configurationInfo.SecretServerUrl);
            Logger.MethodExit();
            return "";
        }

        /// <summary>
        ///     Obtains an OAuth access token from Delinea Secret Server.
        /// </summary>
        /// <param name="client">The HTTP client used to communicate with Secret Server.</param>
        /// <param name="configurationInfo">The configuration containing Secret Server connection and authentication details.</param>
        /// <returns>An OAuth access token string for authenticating subsequent API calls.</returns>
        /// <exception cref="HttpRequestException">Thrown when the HTTP request to the token endpoint fails.</exception>
        /// <exception cref="InvalidTokenException">Thrown when the token cannot be obtained or parsed from the response.</exception>
        /// <exception cref="Exception">Thrown when deserializing the token response fails.</exception>
        /// <remarks>Currently only supports password grant type authentication.</remarks>
        private async Task<string> GetAccessToken(HttpClient client, DelineaConfiguration configurationInfo)
        {
            Logger.MethodEntry();

            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));

            var body = new Dictionary<string, string>
            {
                { "username", configurationInfo.Username },
                { "password", configurationInfo.Password },
                { "grant_type", "password" } // grant type is still "password" as far as the Delinea API is concerned
            };


            Logger.LogTrace("Authentication request grant type ${GrantType}", body["grant_type"]);

            var loggableBody = new Dictionary<string, string>(body);
            foreach (var sensitiveKey in new[] { "password", "client_secret" })
                if (loggableBody.ContainsKey(sensitiveKey))
                    loggableBody[sensitiveKey] = "***";
            Logger.LogDebug("Token request body: {RequestBody}", JsonConvert.SerializeObject(loggableBody));

            HttpResponseMessage response;
            var tokeUrl = $"{configurationInfo.SecretServerUrl}/oauth2/token";

            try
            {
                Logger.LogDebug("Requesting an access token from Secret Server at {TokenUrl}", tokeUrl);
                var sw = Stopwatch.StartNew();
                response = await client
                    .PostAsync(new Uri(tokeUrl).AbsoluteUri,
                        new FormUrlEncodedContent(body))
                    .ConfigureAwait(false);
                sw.Stop();
                Logger.LogInformation(
                    "Token endpoint call completed | Method=POST StatusCode={StatusCode} DurationMs={DurationMs}",
                    (int)response.StatusCode, sw.ElapsedMilliseconds);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    Logger.LogError(
                        "Token request failed with status {StatusCode}. Raw response body: {ResponseBody}",
                        (int)response.StatusCode, errorBody);
                    response.EnsureSuccessStatusCode();
                }
            }

            catch (HttpRequestException ex)
            {
                Logger.LogError(
                    "An error occurred while attempting to fetch an access token from Delinea Secret Server: {ExMessage}",
                    ex.Message);
                Logger.MethodExit();
                throw;
            }

            Logger.LogDebug("Access token received");

            try
            {
                Logger.LogDebug("Deserializing access token response");
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

                var token = values?["access_token"];
                client.DefaultRequestHeaders.Accept.Clear();

                Logger.LogTrace("Access token parsed");
                if (token != null) return token;
                Logger.LogError(
                    "Unable to generate access token from Delinea Secret Server \'{ConfigurationInfoSecretServerUrl}\' as \'{ConfigurationInfoUsername}\'. Please check your credentials and try again",
                    configurationInfo.SecretServerUrl, configurationInfo.Username);
                Logger.MethodExit();
                throw new InvalidTokenException(
                    $"Unable to generate access token from Delinea Secret Server '{configurationInfo.SecretServerUrl}' as '{configurationInfo.Username}'. Please check your credentials and try again");
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    "An error occurred while attempting to deserialize the access token response: {ExMessage}",
                    ex.Message);
                Logger.MethodExit();
                throw;
            }
        }

        /// <summary>
        ///     Validates the instance parameters provided to the PAM provider.
        /// </summary>
        /// <param name="instanceParameters">
        ///     A read-only dictionary containing instance-specific parameters, such as SecretId and SecretFieldName.
        /// </param>
        /// <returns>
        ///     True if the instance parameters are valid; otherwise, throws an <see cref="InvalidSecretConfigurationException" />.
        /// </returns>
        /// <exception cref="InvalidSecretConfigurationException">
        ///     Thrown if required parameters are missing or cannot be parsed as expected.
        /// </exception>
        private bool ValidateInstanceParams(IReadOnlyDictionary<string, string> instanceParameters)
        {
            Logger.MethodEntry();
            Logger.LogDebug("Validating instance parameters");
            Logger.LogDebug("Validating instance parameter '{SecretId}'", DelineaConfiguration.SECRET_ID);
            if (!instanceParameters.ContainsKey(DelineaConfiguration.SECRET_ID))
            {
                Logger.LogError("Instance parameter '{SecretId}' not found", DelineaConfiguration.SECRET_ID);
                Logger.MethodExit();
                throw new InvalidSecretConfigurationException(
                    $"Instance parameter '{DelineaConfiguration.SECRET_ID}' not found");
            }

            if (!instanceParameters.ContainsKey(DelineaConfiguration.SECRET_FIELD_NAME) ||
                instanceParameters[DelineaConfiguration.SECRET_FIELD_NAME] == string.Empty)
            {
                Logger.LogError("Instance parameter '{SecretFieldName}' not provided",
                    DelineaConfiguration.SECRET_FIELD_NAME);
                Logger.MethodExit();
                throw new InvalidSecretConfigurationException(
                    $"Instance parameter '{DelineaConfiguration.SECRET_FIELD_NAME}' not provided");
            }

            Logger.LogDebug("Parsing instance parameter '{SecretId}'", DelineaConfiguration.SECRET_ID);
            if (int.TryParse(instanceParameters[DelineaConfiguration.SECRET_ID], out var secretId))
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
        ///     Validates the server configuration parameters for connecting to Delinea Secret Server.
        /// </summary>
        /// <param name="connectionConfiguration">
        ///     A read-only dictionary containing server configuration parameters such as Secret Server URL, credentials, and grant
        ///     type.
        /// </param>
        /// <param name="grantType">
        ///     The OAuth grant type to validate credentials for. Supported values are "password" and "client_credentials".
        ///     Defaults to "password".
        /// </param>
        /// <returns>
        ///     True if the server configuration parameters are valid; otherwise, throws an
        ///     <see cref="InvalidClientConfigurationException" />.
        /// </returns>
        /// <exception cref="InvalidClientConfigurationException">
        ///     Thrown if required parameters are missing or invalid for the specified grant type.
        /// </exception>
        private bool ValidateServerConfigurationParams(
            IReadOnlyDictionary<string, string> connectionConfiguration)
        {
            Logger.MethodEntry();
            Logger.LogDebug("Validating server configuration parameters");
            
            var grantType = "password";
            if (connectionConfiguration.TryGetValue(DelineaConfiguration.GRANT_TYPE, out var configuredGrantType) &&
                !string.IsNullOrEmpty(configuredGrantType))
            {
                grantType = configuredGrantType;
            }

            // Validate Secret Server URL
            ValidateRequiredParameter(connectionConfiguration,
                DelineaConfiguration.SECRET_SERVER_URL,
                "Server configuration parameter");

            // Validate credentials based on grant type
            switch (grantType)
            {
                case "password":
                    ValidatePasswordGrantCredentials(connectionConfiguration);
                    break;

                case "client_credentials":
                    ValidateClientCredentialsGrantCredentials(connectionConfiguration);
                    break;

                case "windows":
                    Logger.LogDebug("Using Windows Authentication, no credentials to validate");
                    break;
                default:
                    Logger.LogError(
                        "Invalid grant type '{GrantType}' specified. Supported types are 'password' and 'client_credentials'",
                        grantType);
                    Logger.MethodExit();
                    throw new Exception(
                        $"Invalid grant type '{grantType}' specified. Supported types are 'password' and 'client_credentials'");
            }

            Logger.MethodExit();
            Logger.LogInformation("Server configuration parameters are valid");
            return true;
        }

        /// <summary>
        ///     Validates that a required parameter exists and is not null or empty in the provided configuration dictionary.
        /// </summary>
        /// <param name="config">
        ///     The configuration dictionary to validate.
        /// </param>
        /// <param name="paramName">
        ///     The name of the parameter to check for existence and non-empty value.
        /// </param>
        /// <param name="errorPrefix">
        ///     A string prefix to include in the error message if validation fails.
        /// </param>
        /// <exception cref="InvalidClientConfigurationException">
        ///     Thrown if the required parameter is missing or its value is null or empty.
        /// </exception>
        private void ValidateRequiredParameter(
            IReadOnlyDictionary<string, string> config,
            string paramName,
            string errorPrefix)
        {
            Logger.MethodEntry();
            Logger.LogDebug("Validating parameter '{ParamName}'", paramName);

            if (config.ContainsKey(paramName) && !string.IsNullOrEmpty(config[paramName])) return;
            Logger.LogError("{ErrorPrefix} '{ParamName}' not provided", errorPrefix, paramName);
            Logger.MethodExit();
            throw new InvalidClientConfigurationException($"{errorPrefix} '{paramName}' not provided");
        }

        /// <summary>
        ///     Validates that the required username and password parameters exist and are not empty for the password grant type.
        /// </summary>
        /// <param name="config">The configuration dictionary containing client parameters.</param>
        /// <exception cref="InvalidClientConfigurationException">
        ///     Thrown if the username or password parameter is missing or empty.
        /// </exception>
        private void ValidatePasswordGrantCredentials(IReadOnlyDictionary<string, string> config)
        {
            Logger.MethodEntry();
            ValidateRequiredParameter(config, DelineaConfiguration.USERNAME, "Client configuration parameter");
            ValidateRequiredParameter(config, DelineaConfiguration.PASSWORD, "Client configuration parameter");
            Logger.MethodExit();
        }

        /// <summary>
        ///     Validates that the required client ID and client secret parameters exist and are not empty for the client
        ///     credentials grant type.
        /// </summary>
        /// <param name="config">
        ///     The configuration dictionary containing client parameters.
        /// </param>
        /// <exception cref="InvalidClientConfigurationException">
        ///     Thrown if the client ID or client secret parameter is missing or empty.
        /// </exception>
        private void ValidateClientCredentialsGrantCredentials(IReadOnlyDictionary<string, string> config)
        {
            Logger.MethodEntry();
            ValidateRequiredParameter(config, DelineaConfiguration.CLIENT_ID, "Client configuration parameter");
            ValidateRequiredParameter(config, DelineaConfiguration.CLIENT_SECRET, "Client configuration parameter");
            Logger.MethodExit();
        }

        /// <summary>
        ///     Creates a DelineaConfiguration object from the provided parameters.
        /// </summary>
        /// <param name="instanceParameters">
        ///     Dictionary containing instance-specific parameters, including the secret ID and field
        ///     name.
        /// </param>
        /// <param name="connectionConfiguration">Dictionary containing connection and authentication parameters for Secret Server.</param>
        /// <returns>A fully populated DelineaConfiguration object.</returns>
        /// <exception cref="Exception">Thrown when required parameters are missing or invalid.</exception>
        private DelineaConfiguration BuildDelineaConfiguration(
            IReadOnlyDictionary<string, string> instanceParameters,
            IReadOnlyDictionary<string, string> connectionConfiguration)
        {
            Logger.MethodEntry();
            Logger.LogInformation("Validating Delinea configuration");
            var validServer = ValidateServerConfigurationParams(connectionConfiguration);
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

            if (!connectionConfiguration.TryGetValue(DelineaConfiguration.GRANT_TYPE, out var grantType))
            {
                Logger.LogWarning(
                    "\'{GrantType}\' parameter not provided defaulting to 'password' grant",
                    DelineaConfiguration.GRANT_TYPE);
                grantType = "password";
            }

            Logger.LogDebug("Building Delinea configuration");
            switch (grantType)
            {
                case "password":

                    Logger.LogDebug("Building Delinea configuration for password grant type");
                    Logger.MethodExit();
                    return new DelineaConfiguration
                    {
                        SecretServerUrl = connectionConfiguration[DelineaConfiguration.SECRET_SERVER_URL],
                        Username = connectionConfiguration[DelineaConfiguration.USERNAME],
                        Password = connectionConfiguration[DelineaConfiguration.PASSWORD],
                        SecretId = secretId,
                        SecretFieldName = instanceParameters[DelineaConfiguration.SECRET_FIELD_NAME],
                        GrantType = "password"
                    };

                case "client_credentials":
                    Logger.LogDebug("Building Delinea configuration for client credentials grant type");
                    Logger.MethodExit();
                    return new DelineaConfiguration
                    {
                        SecretServerUrl = connectionConfiguration[DelineaConfiguration.SECRET_SERVER_URL],
                        ClientId = connectionConfiguration[DelineaConfiguration.CLIENT_ID],
                        ClientSecret = connectionConfiguration[DelineaConfiguration.CLIENT_SECRET],
                        SecretId = secretId,
                        SecretFieldName = instanceParameters[DelineaConfiguration.SECRET_FIELD_NAME],
                        GrantType = "password"
                    };
                case "windows":
                    Logger.LogDebug("Building Delinea configuration for windows grant type");
                    Logger.MethodExit();
                    return new DelineaConfiguration
                    {
                        SecretServerUrl = connectionConfiguration[DelineaConfiguration.SECRET_SERVER_URL],
                        SecretId = secretId,
                        SecretFieldName = instanceParameters[DelineaConfiguration.SECRET_FIELD_NAME],
                        GrantType = "windows"
                    };

                default:
                    Logger.LogError(
                        "Invalid grant type '{GrantType}' specified. Supported types are 'password' and 'client_credentials'",
                        grantType);
                    Logger.MethodExit();
                    throw new Exception(
                        $"Invalid grant type '{grantType}' specified. Supported types are 'password' and 'client_credentials'");
            }
        }

        /// <summary>
        ///     Creates and configures an HttpClient for communicating with Secret Server.
        /// </summary>
        /// <returns>A configured HttpClient with a 60-second timeout.</returns>
        private static HttpClient BuildHttpClient(string grantType)
        {
            var handler = new HttpClientHandler();
            if (grantType == "windows")
            {
                handler.UseDefaultCredentials = true;
            }
            var client = new HttpClient(handler, true);

            client.Timeout = new TimeSpan(0, 0, 60);
            return client;
        }
    }

    /// <summary>
    ///     Represents the response object from a Secret Server get secret API call.
    /// </summary>
    /// <remarks>
    ///     This class is used to deserialize the JSON response from the Secret Server API.
    /// </remarks>
    internal class SecretResponse
    {
        /// <summary>
        ///     Gets or sets the collection of secret items (fields) in the response.
        /// </summary>
        [JsonProperty("items")]
        public List<DelineaSecret> Items { get; set; } = new List<DelineaSecret>();
    }
}