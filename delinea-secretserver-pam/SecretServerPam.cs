// Copyright 2023 Keyfactor
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions
// and limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Keyfactor.Extensions.Pam.Delinea.Models;
using Keyfactor.Logging;
using Keyfactor.Platform.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Keyfactor.Extensions.Pam.Delinea
{
    public class SecretServerPam : IPAMProvider
    {
        private ILogger Logger { get; } = LogHandler.GetClassLogger<SecretServerPam>();
        public string Name => "Delinea-SecretServer";

        public string GetPassword(Dictionary<string, string> instanceParameters,
            Dictionary<string, string> initializationInfo)
        {
            Logger.LogInformation("Starting Delinea Secret Server PAM Provider");
            Logger.LogDebug("Getting password from Delinea Secret Server");
            Logger.LogTrace("instanceParameters: {@InstanceParameters}", instanceParameters);
            // Logger.LogTrace("initializationInfo: {@InitializationInfo}", initializationInfo); // Commented out to avoid logging sensitive information
            using (var client = BuildHttpClient())
            {
                var config = BuildDelineaConfiguration(instanceParameters, initializationInfo);
                return GetDelineaSecretAsync(client, config).Result;
            }
        }

        private async Task<string> GetDelineaSecretAsync(HttpClient client, DelineaConfiguration configurationInfo)
        {
            HttpResponseMessage response;
            var bearerToken = await GetAccessToken(client, configurationInfo).ConfigureAwait(false);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                Logger.LogTrace("Sending request to Secret Server...");
                response = await client
                    .GetAsync(new Uri(
                            $"{configurationInfo.SecretServerUrl}/api/v1/secrets/{configurationInfo.SecretId}")
                        .AbsoluteUri)
                    .ConfigureAwait(false);

                response.EnsureSuccessStatusCode();
            }

            catch (HttpRequestException ex)
            {
                Logger.LogError("An error occurred while attempting to communicate with Delinea Secret Server: {ExMessage}", ex.Message);
                throw;
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            Logger.LogDebug("Attempting to deserialize Delinea Secret Server content into a data model");
            var secretResponse = JsonConvert.DeserializeObject<SecretResponse>(content);

            Logger.LogTrace("Received '{ItemsCount}' secrets from Delinea Secret Server", secretResponse?.Items.Count ?? 0);

            // var secret = secretResponse?.Items.FirstOrDefault(i => i.IsPassword)?.Value;
            var secret = secretResponse?.Items.FirstOrDefault(i =>
                i.Name == configurationInfo.SecretFieldName || i.Slug == configurationInfo.SecretFieldName)?.Value;
            if (!string.IsNullOrEmpty(secret)) return secret;
            Logger.LogError("No secret was found or no items in the secret were of type password");
            return "";
        }

        private async Task<string> GetAccessToken(HttpClient client, DelineaConfiguration configurationInfo)
        {
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));

            var body = new Dictionary<string, string>
            {
                { "username", configurationInfo.Username },
                { "password", configurationInfo.Password },
                { "grant_type", "password" }
            };

            HttpResponseMessage response;
            try
            {
                Logger.LogDebug("Requesting an access token from Secret Server...");
                response = await client
                    .PostAsync(new Uri($"{configurationInfo.SecretServerUrl}/oauth2/token").AbsoluteUri,
                        new FormUrlEncodedContent(body))
                    .ConfigureAwait(false);
                Logger.LogDebug("Request sent");

                response.EnsureSuccessStatusCode();
            }

            catch (HttpRequestException ex)
            {
                Logger.LogError("An error occurred while attempting to fetch an access token from Delinea Secret Server: {ExMessage}", ex.Message);
                throw;
            }

            Logger.LogTrace("Access token received");

            Logger.LogDebug("Deserializing access token response");
            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

            var token = values?["access_token"];
            client.DefaultRequestHeaders.Accept.Clear();

            Logger.LogTrace("Access token parsed");
            if (token != null) return token;
            Logger.LogError("Unable to generate access token from Delinea Secret Server \'{ConfigurationInfoSecretServerUrl}\' as \'{ConfigurationInfoUsername}\'. Please check your credentials and try again", configurationInfo.SecretServerUrl, configurationInfo.Username);
            return "";
        }

        private DelineaConfiguration BuildDelineaConfiguration(
            IReadOnlyDictionary<string, string> instanceParameters,
            IReadOnlyDictionary<string, string> initializationInfo)
        {
            Logger.LogDebug("Building Delinea configuration");
            if (!instanceParameters.ContainsKey(DelineaConfiguration.SECRET_ID))
            {
                Logger.LogError("Instance parameters does not contain the '{SecretId}' key",
                    DelineaConfiguration.SECRET_ID);
                throw new Exception($"Instance level parameters is missing the '{DelineaConfiguration.SECRET_ID}' key");
            }


            if (!initializationInfo.ContainsKey(DelineaConfiguration.SECRET_SERVER_URL))
            {
                Logger.LogError("Initialization parameters does not contain the \'{SecretServerUrl}\' key",
                    DelineaConfiguration.SECRET_SERVER_URL);
                throw new Exception(
                    $"Instance level parameters is missing the '{DelineaConfiguration.SECRET_SERVER_URL}' key");
            }


            if (!initializationInfo.ContainsKey(DelineaConfiguration.USERNAME))
            {
                Logger.LogError("Initialization parameters does not contain the \'{Username}\' key",
                    DelineaConfiguration.USERNAME);
                throw new Exception($"Instance level parameters is missing the '{DelineaConfiguration.USERNAME}' key");
            }


            if (!initializationInfo.ContainsKey(DelineaConfiguration.PASSWORD))
            {
                Logger.LogError("Initialization parameters does not contain the \'{Password}\' key",
                    DelineaConfiguration.PASSWORD);
                throw new Exception($"Instance level parameters is missing the '{DelineaConfiguration.PASSWORD}' key");
            }


            Logger.LogDebug("Parsing secret id from instance parameters");
            if (!int.TryParse(instanceParameters[DelineaConfiguration.SECRET_ID], out var secretId))
            {
                Logger.LogError("Unable to parse {SecretId} as an integer", DelineaConfiguration.SECRET_ID);
                throw new Exception(
                    $"Unable to parse {instanceParameters[DelineaConfiguration.SECRET_ID]} as an integer");
            }


            if (instanceParameters.ContainsKey(DelineaConfiguration.SECRET_FIELD_NAME))
            {
                Logger.LogDebug("Building Delinea configuration");
                return new DelineaConfiguration
                {
                    SecretServerUrl = initializationInfo[DelineaConfiguration.SECRET_SERVER_URL],
                    Username = initializationInfo[DelineaConfiguration.USERNAME],
                    Password = initializationInfo[DelineaConfiguration.PASSWORD],
                    SecretId = secretId,
                    SecretFieldName = instanceParameters[DelineaConfiguration.SECRET_FIELD_NAME]
                };
            }

            Logger.LogError("Instance parameters does not contain the \'{SecretFieldName}\' key",
                DelineaConfiguration.SECRET_FIELD_NAME);
            throw new Exception(
                $"Instance level parameters is missing the '{DelineaConfiguration.SECRET_FIELD_NAME}' key");
        }

        private static HttpClient BuildHttpClient()
        {
            var handler = new HttpClientHandler();
            var client = new HttpClient(handler, true);

            client.Timeout = new TimeSpan(0, 0, 60);

            return client;
        }
    }
}