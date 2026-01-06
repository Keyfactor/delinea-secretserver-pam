// Copyright 2025 Keyfactor
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions
// and limitations under the License.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Keyfactor.Extensions.Pam.Delinea.Models
{
    /// <summary>
    ///     Configuration class for connecting to and retrieving secrets from Delinea Secret Server.
    ///     Supports authentication via username/password or client credentials.
    /// </summary>
    internal class DelineaConfiguration : IValidatableObject
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DelineaConfiguration" /> class with empty strings.
        /// </summary>
        /// <remarks>
        ///     This constructor initializes required string properties with empty strings to satisfy nullable requirements.
        ///     Validation logic in the Validate method ensures actual values are provided when needed.
        /// </remarks>
        public DelineaConfiguration()
        {
            // Initialize non-nullable string properties to satisfy compiler
            SecretServerUrl = string.Empty;
            Username = string.Empty;
            Password = string.Empty;
            ClientId = string.Empty;
            ClientSecret = string.Empty;
            SecretFieldName = string.Empty;
            GrantType = "password"; // Default value is already set in property declaration
        }

        /// <summary>
        ///     The configuration key for the Secret Server URL.
        /// </summary>
        public static string SECRET_SERVER_URL => "Host";

        /// <summary>
        ///     The configuration key for the username used in password grant authentication.
        /// </summary>
        public static string USERNAME => "Username";

        /// <summary>
        ///     The configuration key for the client ID used in client credentials authentication.
        /// </summary>
        public static string CLIENT_ID => "ClientId";

        /// <summary>
        ///     The configuration key for the client secret used in client credentials authentication.
        /// </summary>
        public static string CLIENT_SECRET => "ClientSecret";

        /// <summary>
        ///     The configuration key for the password used in password grant authentication.
        /// </summary>
        public static string PASSWORD => "Password";

        /// <summary>
        ///     The configuration key for the OAuth grant type.
        /// </summary>
        public static string GRANT_TYPE => "GrantType";

        /// <summary>
        ///     The configuration key for the secret ID to retrieve from Secret Server.
        /// </summary>
        public static string SECRET_ID => "SecretId";

        /// <summary>
        ///     The configuration key for the field name within the secret to retrieve.
        /// </summary>
        public static string SECRET_FIELD_NAME => "SecretFieldName";

        /// <summary>
        ///     The base URL of the Delinea Secret Server.
        /// </summary>
        [Required(ErrorMessage = "The SecretServerUrl field is required.")]
        public string SecretServerUrl { get; set; }

        /// <summary>
        ///     The username for password grant authentication with Secret Server.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        ///     The password for password grant authentication with Secret Server.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        ///     The client ID for client credentials authentication with Secret Server.
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        ///     The client secret for client credentials authentication with Secret Server.
        /// </summary>
        public string ClientSecret { get; set; }

        /// <summary>
        ///     The ID of the secret to retrieve from Secret Server.
        /// </summary>
        [Required(ErrorMessage = "The SecretId field is required.")]
        public int SecretId { get; set; }

        /// <summary>
        ///     The name of the field within the secret to retrieve.
        ///     This can be either the field name or slug.
        /// </summary>
        [Required(ErrorMessage = "The SecretFieldName field is required.")]
        public string SecretFieldName { get; set; }

        /// <summary>
        ///     The OAuth grant type to use for authentication.
        ///     Must be either 'password' or 'client_credentials'.
        ///     Defaults to 'password'.
        /// </summary>
        [RegularExpression("^(password|client_credentials|windows)$",
            ErrorMessage = "GrantType must be 'password', 'client_credentials' or `windows`.")]
        public string GrantType { get; set; } = "password";

        /// <summary>
        ///     Validates that the configuration has either username/password or client credentials for authentication.
        /// </summary>
        /// <param name="validationContext">The validation context.</param>
        /// <returns>A collection of validation results.</returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var hasUserPass = !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
            var hasClientCreds = !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);

            switch (GrantType)
            {
                case "windows":
                    if (hasUserPass || hasClientCreds)
                        yield return new ValidationResult(
                            "No credentials should be provided for 'windows' grant type.",
                            new[] { nameof(Username), nameof(Password), nameof(ClientId), nameof(ClientSecret) });
                    break;

                case "password":
                    if (!hasUserPass)
                        yield return new ValidationResult(
                            "Username and Password must be provided for 'password' grant type.",
                            new[] { nameof(Username), nameof(Password) });
                    break;

                case "client_credentials":
                    if (!hasClientCreds)
                        yield return new ValidationResult(
                            "ClientId and ClientSecret must be provided for 'client_credentials' grant type.",
                            new[] { nameof(ClientId), nameof(ClientSecret) });
                    break;
                default:
                    yield return new ValidationResult(
                        "Invalid GrantType specified.",
                        new[] { nameof(GrantType) });
                    break;
            }
        }
    }
}