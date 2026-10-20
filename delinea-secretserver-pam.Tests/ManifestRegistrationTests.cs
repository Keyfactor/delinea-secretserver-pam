// Copyright 2025 Keyfactor
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions
// and limitations under the License.

using System.Net;
using System.Reflection;
using FluentAssertions;
using Keyfactor.Platform.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Keyfactor.Extensions.Pam.Delinea.Tests;

/// <summary>
///     Guards the shipped <c>manifest.json</c>. The Universal Orchestrator only loads classes that are
///     registered here; an unregistered per-type class silently falls back to the generic provider
///     (which defaults GrantType to "password").
/// </summary>
public class ManifestRegistrationTests
{
    private const string ExtensionPointKey = "Keyfactor.Platform.Extensions.IPAMProvider";
    private const string InitInfoPrefix = "Keyfactor:PAMProviders:";
    private const string InitInfoSuffix = ":InitializationInfo";
    private const string RegistrationPrefix = "PAMProviders.";
    private const string RegistrationSuffix = ".PAMProvider";
    private const string Host = "https://ss.example.com/SecretServer";

    private static readonly Assembly ProviderAssembly = typeof(SecretServerPam).Assembly;

    private static string FindUp(params string[] relative)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(new[] { dir.FullName }.Concat(relative).ToArray());
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        throw new FileNotFoundException("Could not locate " + string.Join("/", relative));
    }

    private static JObject LoadManifest()
        => JObject.Parse(File.ReadAllText(FindUp("delinea-secretserver-pam", "manifest.json")));

    private static List<Type> ProviderTypes()
        => ProviderAssembly.GetTypes()
            .Where(t => typeof(IPAMProvider).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract)
            .ToList();

    private static Dictionary<string, JObject> Registrations()
        => ((JObject)LoadManifest()["extensions"]![ExtensionPointKey]!)
            .Properties()
            .ToDictionary(p => p.Name, p => (JObject)p.Value);

    private static string NameOf(Type t) => ((IPAMProvider)Activator.CreateInstance(t)!).Name;

    private static string RegistrationName(string registrationKey)
    {
        registrationKey.Should().StartWith(RegistrationPrefix).And.EndWith(RegistrationSuffix);
        return registrationKey.Substring(RegistrationPrefix.Length,
            registrationKey.Length - RegistrationPrefix.Length - RegistrationSuffix.Length);
    }

    [Fact]
    public void EveryIPAMProviderClass_IsRegistered()
    {
        var registered = Registrations().Values.Select(r => (string)r["TypeFullName"]!).ToHashSet();

        foreach (var type in ProviderTypes())
            registered.Should().Contain(type.FullName, $"{type.Name} must be registered in manifest.json");

        ProviderTypes().Should().HaveCount(4);
    }

    [Fact]
    public void EveryRegistration_TypeFullNameResolvesToIPAMProvider()
    {
        foreach (var reg in Registrations().Values)
        {
            var type = ProviderAssembly.GetType((string)reg["TypeFullName"]!, false);
            type.Should().NotBeNull($"TypeFullName {reg["TypeFullName"]} must resolve");
            typeof(IPAMProvider).IsAssignableFrom(type).Should().BeTrue();
            ((string)reg["assemblyPath"]!).Should().Be(ProviderAssembly.GetName().Name + ".dll");
        }
    }

    [Fact]
    public void RegistrationName_MatchesProviderName_AndInitializationInfoKey()
    {
        var manifest = LoadManifest();

        foreach (var (key, reg) in Registrations())
        {
            var type = ProviderAssembly.GetType((string)reg["TypeFullName"]!, true)!;
            var name = NameOf(type);

            RegistrationName(key).Should().Be(name, "registration name must equal the provider's Name");
            manifest.Property(InitInfoPrefix + name + InitInfoSuffix).Should()
                .NotBeNull($"InitializationInfo key for '{name}' must exist");
        }
    }

    [Fact]
    public void EveryInitializationInfoKey_HasARegistration()
    {
        var registeredNames = Registrations().Keys.Select(RegistrationName).ToHashSet();

        var initNames = LoadManifest().Properties()
            .Where(p => p.Name.StartsWith(InitInfoPrefix) && p.Name.EndsWith(InitInfoSuffix))
            .Select(p => p.Name.Substring(InitInfoPrefix.Length,
                p.Name.Length - InitInfoPrefix.Length - InitInfoSuffix.Length));

        initNames.Should().BeEquivalentTo(registeredNames);
    }

    [Fact]
    public void ManifestTemplates_DoNotEnableSkipTlsValidation()
    {
        foreach (var p in LoadManifest().Properties().Where(p => p.Name.EndsWith(InitInfoSuffix)))
            ((JObject)p.Value).Property("SkipTlsValidation")?.Value.ToString().Should().NotBe("true");
    }

    [Fact]
    public void IntegrationManifestPamTypes_MatchProviderNames_AndAllSupportSkipTlsValidation()
    {
        var pamTypes = (JObject)JObject.Parse(File.ReadAllText(FindUp("integration-manifest.json")))
            ["about"]!["pam"]!["pam_types"]!;

        pamTypes.Properties().Select(p => (string)p.Value["Name"]!)
            .Should().BeEquivalentTo(ProviderTypes().Select(NameOf));

        foreach (var p in pamTypes.Properties())
            p.Value["Parameters"]!.Select(x => (string)x["Name"]!).Should().Contain("SkipTlsValidation", p.Name);
    }

    // -----------------------------------------------------------------------
    // Per-type classes use their fixed grant type with no GrantType supplied
    // -----------------------------------------------------------------------

    private static (List<HttpRequestMessage> Requests, List<string> Bodies, HttpClient Client) RecordingClient()
    {
        var requests = new List<HttpRequestMessage>();
        var bodies = new List<string>();
        var handler = new Fakes.TestHttpMessageHandler(async (req, _) =>
        {
            requests.Add(req);
            bodies.Add(req.Content == null ? "" : await req.Content.ReadAsStringAsync());
            var body = req.RequestUri!.AbsolutePath.EndsWith("/oauth2/token")
                ? JsonConvert.SerializeObject(new { access_token = "t" })
                : JsonConvert.SerializeObject(new
                {
                    id = 42,
                    items = new[] { new { fieldName = "password", slug = "password", itemValue = "v" } }
                });
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) };
        });
        return (requests, bodies, new HttpClient(handler));
    }

    private static Dictionary<string, string> Instance() => new()
        { { "SecretId", "42" }, { "SecretFieldName", "password" } };

    [Fact]
    public void PasswordType_UsesPasswordGrant_WithoutGrantTypeParam()
    {
        var (requests, bodies, client) = RecordingClient();
        var sut = new SecretServerPamPassword(client, NullLogger.Instance);

        sut.GetPassword(Instance(), new Dictionary<string, string>
            { { "Host", Host }, { "Username", "u-1" }, { "Password", "p-1" } }).Should().Be("v");

        requests.Select(r => r.RequestUri!.AbsolutePath).Should()
            .Equal("/SecretServer/oauth2/token", "/SecretServer/api/v1/secrets/42");
        bodies[0].Should().Contain("username=u-1").And.Contain("grant_type=password");
    }

    [Fact]
    public void ClientCredentialsType_UsesClientIdAndSecret_WithoutGrantTypeParam()
    {
        var (requests, bodies, client) = RecordingClient();
        var sut = new SecretServerPamClientCredentials(client, NullLogger.Instance);

        sut.GetPassword(Instance(), new Dictionary<string, string>
            { { "Host", Host }, { "ClientId", "cid-1" }, { "ClientSecret", "csec-1" } }).Should().Be("v");

        // Delinea quirk: token body is grant_type=password with ClientId/ClientSecret as username/password.
        bodies[0].Should().Contain("username=cid-1").And.Contain("password=csec-1")
            .And.Contain("grant_type=password");
        requests.Last().RequestUri!.AbsolutePath.Should().Be("/SecretServer/api/v1/secrets/42");
    }

    [Fact]
    public void WindowsType_UsesWinAuthEndpoint_WithoutToken_AndWithoutGrantTypeParam()
    {
        var (requests, _, client) = RecordingClient();
        var sut = new SecretServerPamWindows(client, NullLogger.Instance);

        sut.GetPassword(Instance(), new Dictionary<string, string> { { "Host", Host } }).Should().Be("v");

        requests.Select(r => r.RequestUri!.AbsolutePath).Should()
            .Equal("/SecretServer/winauthwebservices/api/v1/secrets/42");
    }

    [Fact]
    public void PerTypeClasses_IgnoreConflictingGrantTypeParam()
    {
        var (requests, _, client) = RecordingClient();
        var sut = new SecretServerPamWindows(client, NullLogger.Instance);

        sut.GetPassword(Instance(), new Dictionary<string, string> { { "Host", Host }, { "GrantType", "password" } })
            .Should().Be("v");

        requests.Should().ContainSingle().Which.RequestUri!.AbsolutePath.Should().Contain("winauthwebservices");
    }
}
