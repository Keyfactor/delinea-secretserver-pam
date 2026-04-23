// Copyright 2023 Keyfactor
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions
// and limitations under the License.

using Keyfactor.Extensions.Pam.Delinea;

namespace TestConsole;

internal class Program
{
    static string RequireEnv(string name) =>
        Environment.GetEnvironmentVariable(name)
        ?? throw new InvalidOperationException($"Required environment variable '{name}' is not set.");

    private static void Main(string[] args)
    {
        var pam = new SecretServerPam();
        var initInfo = new Dictionary<string, string>();

        var instanceParams = new Dictionary<string, string>();

        //Read SecretServerUrl from environment variable
        initInfo.Add("Host", RequireEnv("SECRET_SERVER_URL"));
        //Read GrantType from environment variable
        initInfo.Add("GrantType", Environment.GetEnvironmentVariable("SECRET_SERVER_GRANT_TYPE") ?? "password");

        switch (initInfo["GrantType"])
        {
            case "password":
                initInfo.Add("Username", RequireEnv("SECRET_SERVER_USERNAME"));
                initInfo.Add("Password", RequireEnv("SECRET_SERVER_PASSWORD"));
                break;
            case "client_credentials":
                initInfo.Add("ClientId", RequireEnv("SECRET_SERVER_CLIENT_ID"));
                initInfo.Add("ClientSecret", RequireEnv("SECRET_SERVER_CLIENT_SECRET"));
                break;
            case "windows":
                break;
            default:
                throw new Exception($"Unsupported Grant Type: {initInfo["GrantType"]}");
        }

        if (string.Equals(Environment.GetEnvironmentVariable("SECRET_SERVER_SKIP_TLS_VALIDATION"), "true", StringComparison.OrdinalIgnoreCase))
            initInfo.Add("SkipTlsValidation", "true");

        //Read SecretId from environment variable
        instanceParams.Add("SecretId", RequireEnv("SECRET_SERVER_SECRET_ID"));
        instanceParams.Add("SecretFieldName", "username");
        var username = pam.GetPassword(instanceParams, initInfo);
        instanceParams["SecretFieldName"] = "password";
        var password = pam.GetPassword(instanceParams, initInfo);
        Console.WriteLine($"ServerUsername: {username}");
        Console.WriteLine($"ServerPassword: {new string('*', password?.Length ?? 0)} (len={password?.Length ?? 0})");
    }
}