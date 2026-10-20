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
///     Guards the shipped manifests. A UO supports only ONE PAM provider registration per manifest.json
///     (until UO v26.4), so <c>manifest.json</c> registers the generic provider and the per-type samples
///     (<c>manifest.password.json</c> etc.) each register exactly one class, to be copied over
///     <c>manifest.json</c> by the user.
/// </summary>
public class ManifestRegistrationTests
{
    private const string ExtensionPointKey = "Keyfactor.Platform.Extensions.IPAMProvider";
    private const string InitInfoPrefix = "Keyfactor:PAMProviders:";
    private const string InitInfoSuffix = ":InitializationInfo";
    private const string RegistrationPrefix = "PAMProviders.";
    private const string RegistrationSuffix = ".PAMProvider";
    private const string Host = "https://ss.example.com/SecretServer";

    private static readonly string[] ManifestFiles =
    {
        "manifest.json",
        "samples/manifest.password.json",
        "samples/manifest.clientcredentials.json",
        "samples/manifest.windows.json"
    };

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

    private static JObject LoadManifest(string file)
        => JObject.Parse(File.ReadAllText(FindUp("delinea-secretserver-pam", file)));

    private static List<Type> ProviderTypes()
        => ProviderAssembly.GetTypes()
            .Where(t => typeof(IPAMProvider).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract)
            .ToList();

    private static string NameOf(Type t) => ((IPAMProvider)Activator.CreateInstance(t)!).Name;

    private static List<JProperty> Registrations(JObject manifest)
        => ((JObject)manifest["extensions"]![ExtensionPointKey]!).Properties().ToList();

    private static List<JProperty> InitInfos(JObject manifest)
        => manifest.Properties()
            .Where(p => p.Name.StartsWith(InitInfoPrefix) && p.Name.EndsWith(InitInfoSuffix))
            .ToList();

    private static string Between(string s, string prefix, string suffix)
    {
        s.Should().StartWith(prefix).And.EndWith(suffix);
        return s.Substring(prefix.Length, s.Length - prefix.Length - suffix.Length);
    }

    public static IEnumerable<object[]> Files() => ManifestFiles.Select(f => new object[] { f });

    [Theory]
    [MemberData(nameof(Files))]
    public void Manifest_HasExactlyOneRegistration_AndOneInitializationInfo(string file)
    {
        var manifest = LoadManifest(file);
        Registrations(manifest).Should().ContainSingle();
        InitInfos(manifest).Should().ContainSingle();
    }

    [Theory]
    [MemberData(nameof(Files))]
    public void Manifest_TypeFullNameResolves_AndNamesMatch(string file)
    {
        var manifest = LoadManifest(file);
        var reg = Registrations(manifest).Single();
        var body = (JObject)reg.Value;

        var type = ProviderAssembly.GetType((string)body["TypeFullName"]!, false);
        type.Should().NotBeNull($"TypeFullName in {file} must resolve");
        typeof(IPAMProvider).IsAssignableFrom(type).Should().BeTrue();
        ((string)body["assemblyPath"]!).Should().Be(ProviderAssembly.GetName().Name + ".dll");

        var name = NameOf(type!);
        Between(reg.Name, RegistrationPrefix, RegistrationSuffix).Should().Be(name);
        Between(InitInfos(manifest).Single().Name, InitInfoPrefix, InitInfoSuffix).Should().Be(name);
    }

    [Fact]
    public void Manifests_Together_CoverAllProviderClasses()
    {
        var registered = ManifestFiles
            .Select(f => (string)((JObject)Registrations(LoadManifest(f)).Single().Value)["TypeFullName"]!)
            .ToList();

        registered.Should().OnlyHaveUniqueItems();
        registered.Should().BeEquivalentTo(ProviderTypes().Select(t => t.FullName));
        ProviderTypes().Should().HaveCount(4);
    }

    [Fact]
    public void ManifestJson_RegistersGenericProvider()
    {
        var reg = Registrations(LoadManifest("manifest.json")).Single();
        ((string)reg.Value["TypeFullName"]!).Should().Be(typeof(SecretServerPam).FullName);
    }

    [Theory]
    [MemberData(nameof(Files))]
    public void ManifestTemplates_DoNotEnableSkipTlsValidation(string file)
    {
        foreach (var p in InitInfos(LoadManifest(file)))
            ((JObject)p.Value).Property("SkipTlsValidation")?.Value.ToString().Should().NotBe("true");
    }

    [Theory]
    [InlineData("samples/manifest.password.json")]
    [InlineData("samples/manifest.clientcredentials.json")]
    [InlineData("samples/manifest.windows.json")]
    public void PerTypeSamples_DoNotContainGrantType(string file)
        => ((JObject)InitInfos(LoadManifest(file)).Single().Value).Property("GrantType").Should().BeNull();

    [Fact]
    public void OnlyManifestJson_SitsAtExtensionRoot_SamplesAreUnderSamplesFolder()
    {
        // The UO must not pick up any other *.json in the extension folder.
        var root = Path.GetDirectoryName(FindUp("delinea-secretserver-pam", "manifest.json"))!;
        Directory.GetFiles(root, "*.json").Select(Path.GetFileName).Should().Equal("manifest.json");
        Directory.GetFiles(Path.Combine(root, "samples"), "*.json").Select(Path.GetFileName)
            .Should().BeEquivalentTo("manifest.password.json", "manifest.clientcredentials.json", "manifest.windows.json");
    }

    [Fact]
    public void ProviderCsproj_CopiesAllManifestsToOutput()
    {
        var csproj = File.ReadAllText(FindUp("delinea-secretserver-pam", "delinea-secretserver-pam.csproj"));
        foreach (var f in ManifestFiles)
            csproj.Should().Contain($"Update=\"{f}\"");
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
