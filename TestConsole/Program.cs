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
    private static void Main(string[] args)
    {
        var pam = new SecretServerPam();
        var initInfo = new Dictionary<string, string>();

        var instanceParams = new Dictionary<string, string>();

        //Read SecretServerUrl from environment variable
        initInfo.Add("Host",
            Environment.GetEnvironmentVariable("SECRET_SERVER_URL") ?? "https://keyfactor.secretservercloud.com");
        //Read Username from environment variable
        initInfo.Add("GrantType", Environment.GetEnvironmentVariable("SECRET_SERVER_GRANT_TYPE") ?? "password");

        switch (initInfo["GrantType"])
        {
            case "password":
                initInfo.Add("Username", Environment.GetEnvironmentVariable("SECRET_SERVER_USERNAME") ?? "pam-tester");
                initInfo.Add("Password", Environment.GetEnvironmentVariable("SECRET_SERVER_PASSWORD") ?? "changeme!");
                break;
            case "client_credentials":
                initInfo.Add("ClientId", Environment.GetEnvironmentVariable("SECRET_SERVER_CLIENT_ID") ?? "pam-tester");
                initInfo.Add("ClientSecret",
                    Environment.GetEnvironmentVariable("SECRET_SERVER_CLIENT_SECRET") ?? "changeme!");
                break;
            default:
                throw new Exception($"Unsupported Grant Type: {initInfo["GrantType"]}");
        }

        //Read SecretId from environment variable
        instanceParams.Add("SecretId", Environment.GetEnvironmentVariable("SECRET_SERVER_SECRET_ID") ?? "1");
        instanceParams.Add("SecretFieldName", "username");
        var username = pam.GetPassword(instanceParams, initInfo);
        instanceParams["SecretFieldName"] = "password";
        var password = pam.GetPassword(instanceParams, initInfo);
        Console.WriteLine($"ServerUsername: {username}");
        Console.WriteLine($"ServerPassword: {password}");
    }
}