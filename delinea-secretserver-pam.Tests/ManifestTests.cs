// Copyright 2025 Keyfactor
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions
// and limitations under the License.

using FluentAssertions;
using Keyfactor.Platform.Extensions;
using Newtonsoft.Json.Linq;

namespace Keyfactor.Extensions.Pam.Delinea.Tests;

/// <summary>
///     Validates the manifest files shipped with the plugin (one per PAM type). The files are read from the
///     test output directory, where they only appear if the library project copies them to its build output.
/// </summary>
public class ManifestTests
{
    private const string ProviderPlaceholder = "YOUR-PROVIDER-NAME";
    private const string ExtensionsKey = "extensions";
    private const string IPamProviderKey = "Keyfactor.Platform.Extensions.IPAMProvider";

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
        entries[0].Value["assemblyPath"]!.Value<string>().Should().Be("delinea-secretserver-pam.dll");
        entries[0].Value["TypeFullName"]!.Value<string>().Should().Be(expectedType.FullName);
    }

    [Theory]
    [MemberData(nameof(Manifests))]
    public void Manifest_TypeFullName_ResolvesToIPamProviderInAssembly(string fileName, Type _)
    {
        var typeName = Load(fileName)[ExtensionsKey]![IPamProviderKey]!
            .Children<JProperty>().Single().Value["TypeFullName"]!.Value<string>()!;

        var type = typeof(SecretServerPam).Assembly.GetType(typeName, throwOnError: false);

        type.Should().NotBeNull();
        typeof(IPAMProvider).IsAssignableFrom(type).Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(Manifests))]
    public void Manifest_HasExactlyTwoConfigSections_ProviderNamedAndClassNamed(string fileName, Type expectedType)
    {
        var manifest = Load(fileName);
        var provider = (IPAMProvider)Activator.CreateInstance(expectedType)!;

        var sections = manifest.Properties().Where(p => p.Name != ExtensionsKey).Select(p => p.Name).ToList();

        sections.Should().BeEquivalentTo(new[]
        {
            $"Keyfactor:PAMProviders:{ProviderPlaceholder}:InitializationInfo",
            $"Keyfactor:PAMProviders:{provider.Name}:InitializationInfo"
        });
    }

    [Theory]
    [MemberData(nameof(Manifests))]
    public void Manifest_Sections_HaveIdenticalPlaceholderValues_AndNoSkipTlsValidationTrue(string fileName, Type _)
    {
        var sections = Load(fileName).Properties()
            .Where(p => p.Name != ExtensionsKey)
            .Select(p => (JObject)p.Value)
            .ToList();

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
    [MemberData(nameof(Manifests))]
    public void Manifest_TypeNamedSection_Equals_PamClassName(string fileName, Type expectedType)
    {
        var provider = (IPAMProvider)Activator.CreateInstance(expectedType)!;

        Load(fileName).Should().ContainKey($"Keyfactor:PAMProviders:{provider.Name}:InitializationInfo");
    }
}
