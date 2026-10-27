// Copyright 2025 Keyfactor
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions
// and limitations under the License.

using System.Collections;

namespace Keyfactor.Extensions.Pam.Delinea.Tests;

/// <summary>
///     Process environment variables are global, and xUnit runs test classes in parallel. Every test class that
///     reads or writes the provider's environment variables (or calls <c>GetPassword</c>, which reads them) joins
///     this one collection, which is not parallelized with itself or with anything else.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public class ProviderEnvironmentCollection
{
    public const string Name = "Provider environment variables";
}

/// <summary>
///     Base class for tests that call the provider. Before each test it removes every
///     <c>KEYFACTOR_PAM_DELINEA_*</c> variable and <c>KEYFACTOR_PAM_SKIP_TLS_VALIDATION</c> from the process
///     (so a developer shell that already defines them cannot change the results) and restores the original
///     values afterwards.
/// </summary>
public abstract class ProviderEnvironmentTestBase : IDisposable
{
    private const string DelineaPrefix = "KEYFACTOR_PAM_DELINEA_";
    private const string SkipTlsVariable = "KEYFACTOR_PAM_SKIP_TLS_VALIDATION";

    private readonly Dictionary<string, string> _saved = new();

    protected ProviderEnvironmentTestBase()
    {
        foreach (var name in ProviderVariableNames())
        {
            _saved[name] = Environment.GetEnvironmentVariable(name)!;
            Environment.SetEnvironmentVariable(name, null);
        }
    }

    public virtual void Dispose()
    {
        // Remove anything the test added, then put back what the developer shell had.
        foreach (var name in ProviderVariableNames())
            Environment.SetEnvironmentVariable(name, null);
        foreach (var kvp in _saved)
            Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
        GC.SuppressFinalize(this);
    }

    private static List<string> ProviderVariableNames()
    {
        var names = new List<string> { SkipTlsVariable };
        foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            var name = (string)entry.Key;
            if (name.StartsWith(DelineaPrefix, StringComparison.OrdinalIgnoreCase))
                names.Add(name);
        }

        return names;
    }
}
