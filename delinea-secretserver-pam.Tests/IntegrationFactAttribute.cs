using Xunit.Sdk;

namespace Keyfactor.Extensions.Pam.Delinea.Tests;

/// <summary>
/// Marks a test as an integration test that requires specific environment variables.
/// The test is skipped (not failed) when any of the named variables are absent or empty.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class IntegrationFactAttribute : FactAttribute
{
    private static readonly string[] Required =
    {
        "SECRET_SERVER_URL",
        "SECRET_SERVER_USERNAME",
        "SECRET_SERVER_PASSWORD",
        "SECRET_SERVER_SECRET_ID"
    };

    public IntegrationFactAttribute()
    {
        var missing = Required
            .Where(v => string.IsNullOrEmpty(Environment.GetEnvironmentVariable(v)))
            .ToList();

        if (missing.Count > 0)
            Skip = $"Integration env vars not set: {string.Join(", ", missing)}";
    }
}
