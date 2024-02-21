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
        initInfo.Add("Username", Environment.GetEnvironmentVariable("SECRET_SERVER_USERNAME") ?? "pam-tester");
        //Read Password from environment variable
        initInfo.Add("Password", Environment.GetEnvironmentVariable("SECRET_SERVER_PASSWORD") ?? "changeme!");
        //Read SecretId from environment variable
        initInfo.Add("LogSecrets", true.ToString());
        instanceParams.Add("SecretId", Environment.GetEnvironmentVariable("SECRET_SERVER_SECRET_ID") ?? "1");
        instanceParams.Add("SecretFieldName", "username");
        var username = pam.GetPassword(instanceParams, initInfo);
        instanceParams["SecretFieldName"] = "password";
        var password = pam.GetPassword(instanceParams, initInfo);
        Console.WriteLine($"Username: {username}");
        Console.WriteLine($"Password: {password}");
    }
}