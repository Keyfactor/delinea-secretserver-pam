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
///     Guards the manifests shipped with the plugin (one per PAM type; the chosen one is renamed to
///     <c>manifest.json</c> on install). The manifests are read from the test output directory, where they only
///     appear if the library project copies them to its build output.
/// </summary>
public class ManifestTests
{
    private const string ProviderPlaceholder = "YOUR-PROVIDER-NAME";
    private const string ExtensionsKey = "extensions";
    private const string IPamProviderKey = "Keyfactor.Platform.Extensions.IPAMProvider";
    private const string Host = "https://ss.example.com/SecretServer";

    private static readonly Assembly ProviderAssembly = typeof(SecretServerPam).Assembly;

    public static IEnumerable<object[]> Manifests => new[]
    {
        new object[] { "manifest.json", typeof(SecretServerPam) },
        new object[] { "Password-manifest.json", typeof(SecretServerPamPassword) },
        new object[] { "ClientCredentials-manifest.json", typeof(SecretServerPamClientCredentials) },
        new object[] { "Windows-manifest.json", typeof(SecretServerPamWindows) }
    };

    private static JObject Load(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, fileName);
        File.Exists(path).Should().BeTrue($"{fileName} must be copied to the build output");
        return JObject.Parse(File.ReadAllText(path));
    }

    private static List<JProperty> Sections(JObject manifest)
        => manifest.Properties().Where(p => p.Name != ExtensionsKey).ToList();

    private static string NameOf(Type t) => ((IPAMProvider)Activator.CreateInstance(t)!).Name;

    private static List<Type> ProviderTypes()
        => ProviderAssembly.GetTypes()
            .Where(t => typeof(IPAMProvider).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract)
            .ToList();

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

    // -----------------------------------------------------------------------
    // Shipped manifest shape
    // -----------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(Manifests))]
    public void Manifest_IsValidJson_AndPresentInBuildOutput(string fileName, Type _)
    {
        Load(fileName).Should().NotBeNull();
    }

    [Theory]
    [MemberData(nameof(Manifests))]
    public void Manifest_HasSingleExtensionEntry_KeyedByBracketFreeProviderPlaceholder(string fileName, Type expectedType)
    {
        var entries = ((JObject)Load(fileName)[ExtensionsKey]![IPamProviderKey]!).Properties().ToList();

        entries.Should().HaveCount(1);
        entries[0].Name.Should().Be(ProviderPlaceholder);
        entries[0].Name.Should().NotContain("<").And.NotContain(">");
        entries[0].Name.Should().NotStartWith("PAMProviders.");
        entries[0].Name.Should().NotEndWith(".PAMProvider");
        entries[0].Value["assemblyPath"]!.Value<string>().Should().Be(ProviderAssembly.GetName().Name + ".dll");
        entries[0].Value["TypeFullName"]!.Value<string>().Should().Be(expectedType.FullName);
    }

    [Theory]
    [MemberData(nameof(Manifests))]
    public void Manifest_TypeFullName_ResolvesToIPamProviderInAssembly(string fileName, Type _)
    {
        var typeName = Load(fileName)[ExtensionsKey]![IPamProviderKey]!
            .Children<JProperty>().Single().Value["TypeFullName"]!.Value<string>()!;

        var type = ProviderAssembly.GetType(typeName, throwOnError: false);

        type.Should().NotBeNull();
        typeof(IPAMProvider).IsAssignableFrom(type).Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(Manifests))]
    public void Manifest_HasExactlyTwoConfigSections_ProviderNamedFirst_ThenClassNamed(string fileName, Type expectedType)
    {
        var sections = Sections(Load(fileName)).Select(p => p.Name).ToList();

        sections.Should().Equal(
            $"Keyfactor:PAMProviders:{ProviderPlaceholder}:InitializationInfo",
            $"Keyfactor:PAMProviders:{NameOf(expectedType)}:InitializationInfo");
    }

    [Theory]
    [MemberData(nameof(Manifests))]
    public void Manifest_Sections_HaveIdenticalPlaceholderValues_AndNoSkipTlsValidationTrue(string fileName, Type _)
    {
        var sections = Sections(Load(fileName)).Select(p => (JObject)p.Value).ToList();

        sections.Should().HaveCount(2);
        JToken.DeepEquals(sections[0], sections[1]).Should().BeTrue();

        foreach (var section in sections)
        {
            section.Properties().Should().NotBeEmpty();
            var skip = section["SkipTlsValidation"]?.ToString();
            skip?.Trim().ToLowerInvariant().Should().NotBe("true");
        }
    }

    [Theory]
    [InlineData("Password-manifest.json")]
    [InlineData("ClientCredentials-manifest.json")]
    [InlineData("Windows-manifest.json")]
    public void PerTypeManifests_DoNotContainGrantType(string fileName)
    {
        foreach (var section in Sections(Load(fileName)))
            ((JObject)section.Value).Property("GrantType").Should().BeNull();
    }

    // -----------------------------------------------------------------------
    // Coverage across manifests / catalog manifest
    // -----------------------------------------------------------------------

    [Fact]
    public void Manifests_Together_CoverAllProviderClasses()
    {
        var registered = Manifests
            .Select(m => Load((string)m[0])[ExtensionsKey]![IPamProviderKey]!
                .Children<JProperty>().Single().Value["TypeFullName"]!.Value<string>())
            .ToList();

        registered.Should().OnlyHaveUniqueItems();
        registered.Should().BeEquivalentTo(ProviderTypes().Select(t => t.FullName));
        ProviderTypes().Should().HaveCount(4);
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
