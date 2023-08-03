// Copyright 2023 Keyfactor
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions
// and limitations under the License.

using Newtonsoft.Json;

namespace Keyfactor.Extensions.Pam.Delinea.Models
{
    internal class DelineaSecret
    {
        [JsonProperty("itemId")] public int Id { get; set; }

        [JsonProperty("fieldName")] public string Name { get; set; }

        [JsonProperty("slug")] public string Slug { get; set; }

        [JsonProperty("itemValue")] public string Value { get; set; }

        [JsonProperty("isPassword")] public bool IsPassword { get; set; }
    }    
}

