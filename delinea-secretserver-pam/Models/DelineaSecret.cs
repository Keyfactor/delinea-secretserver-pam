// Copyright 2025 Keyfactor
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions
// and limitations under the License.

using Newtonsoft.Json;

namespace Keyfactor.Extensions.Pam.Delinea.Models
{
    /// <summary>
    ///     Represents an individual field or item within a secret retrieved from Delinea Secret Server.
    /// </summary>
    /// <remarks>
    ///     This class is used to deserialize the JSON response from the Secret Server API at the field level.
    ///     Each instance represents a single item (field) within a secret.
    /// </remarks>
    internal class DelineaSecret
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DelineaSecret" /> class.
        /// </summary>
        public DelineaSecret()
        {
            // Initialize non-nullable string properties
            Name = string.Empty;
            Slug = string.Empty;
            Value = string.Empty;
        }

        /// <summary>
        ///     Gets or sets the unique identifier for this secret item.
        /// </summary>
        /// <remarks>
        ///     Maps to the "itemId" field in the JSON response.
        /// </remarks>
        [JsonProperty("itemId")]
        public int Id { get; set; }

        /// <summary>
        ///     Gets or sets the display name of this secret field.
        /// </summary>
        /// <remarks>
        ///     Maps to the "fieldName" field in the JSON response.
        ///     This is the human-readable name of the field.
        /// </remarks>
        [JsonProperty("fieldName")]
        public string Name { get; set; }

        /// <summary>
        ///     Gets or sets the slug (machine-friendly identifier) of this secret field.
        /// </summary>
        /// <remarks>
        ///     Maps to the "slug" field in the JSON response.
        ///     The slug is a normalized, machine-friendly version of the field name.
        /// </remarks>
        [JsonProperty("slug")]
        public string Slug { get; set; }

        /// <summary>
        ///     Gets or sets the value of this secret field.
        /// </summary>
        /// <remarks>
        ///     Maps to the "itemValue" field in the JSON response.
        ///     This contains the actual secret data.
        /// </remarks>
        [JsonProperty("itemValue")]
        public string Value { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether this field contains password data.
        /// </summary>
        /// <remarks>
        ///     Maps to the "isPassword" field in the JSON response.
        ///     This is used to identify password fields within a secret.
        /// </remarks>
        [JsonProperty("isPassword")]
        public bool IsPassword { get; set; }
    }
}