// Copyright 2023 Keyfactor
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions
// and limitations under the License.

namespace Keyfactor.Extensions.Pam.Delinea.Models
{
    internal class DelineaConfiguration
    {
        public static string SECRET_SERVER_URL { get; } = "SecretServerUrl";
        public static string USERNAME { get; } = "Username";
        public static string PASSWORD { get; } = "Password";
        public static string SECRET_ID { get; } = "SecretId";
        public static string SECRET_FIELD_NAME { get; } = "SecretFieldName";

        public string SecretServerUrl { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public int SecretId { get; set; }

        public string SecretFieldName { get; set; }
    }
}

