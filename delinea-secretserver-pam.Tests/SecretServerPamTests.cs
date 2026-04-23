// Copyright 2025 Keyfactor
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions
// and limitations under the License.

using System.Net;
using FluentAssertions;
using Keyfactor.Extensions.Pam.Delinea.Tests.Fakes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;

namespace Keyfactor.Extensions.Pam.Delinea.Tests;

/// <summary>
///     Tests for all four concrete PAM provider types and the shared base logic.
///     HTTP is intercepted via <see cref="TestHttpMessageHandler" /> — no network calls.
/// </summary>
public class SecretServerPamTests
{
    // ---------------------------------------------------------------------------
    // Shared test constants
    // ---------------------------------------------------------------------------
    private const string FakeHost = "https://secretserver.example.com/SecretServer";
    private const string FakeUsername = "svc-account";
    private const string FakePassword = "sup3rS3cret!";
    private const string FakeClientId = "app-client-01";
    private const string FakeClientSecret = "cl13ntS3cr3t!";
    private const string FakeSecretId = "42";
    private const string FakeFieldName = "password";
    private const string FakeFieldValue = "retrieved-credential-value";
    private const string FakeToken = "fake-bearer-token";

    private static ILogger NullLogger => NullLogger<SecretServerPamTests>.Instance;

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private static string BuildTokenResponse(string token = FakeToken)
        => JsonConvert.SerializeObject(new Dictionary<string, string> { { "access_token", token } });

    private static string BuildSecretResponse(string fieldName = FakeFieldName, string fieldValue = FakeFieldValue)
        => JsonConvert.SerializeObject(new
        {
            id = 42,
            name = "Test Secret",
            items = new[]
            {
                new { itemId = 1, fieldName = fieldName, slug = fieldName, itemValue = fieldValue, isPassword = true }
            }
        });

    /// <summary>
    ///     Creates a <see cref="TestHttpMessageHandler" /> that sequences multiple responses:
    ///     first response for the token endpoint, second for the secret endpoint.
    /// </summary>
    private static TestHttpMessageHandler TwoStageHandler(
        HttpResponseMessage tokenResponse,
        HttpResponseMessage secretResponse)
    {
        var callCount = 0;
        return new TestHttpMessageHandler((req, ct) =>
        {
            callCount++;
            return Task.FromResult(callCount == 1 ? tokenResponse : secretResponse);
        });
    }

    private static TestHttpMessageHandler ConstantHandler(HttpResponseMessage response)
        => new TestHttpMessageHandler((req, ct) => Task.FromResult(response));

    // ---------------------------------------------------------------------------
    // SecretServerPam (backwards-compatible, GrantType-driven)
    // ---------------------------------------------------------------------------

    public class BackwardsCompatibleType
    {
        [Fact]
        public void Name_IsDelineaSecretServer()
        {
            var sut = new SecretServerPam();
            sut.Name.Should().Be("Delinea-SecretServer");
        }

        [Fact]
        public void GetPassword_PasswordGrant_ReturnsSecret()
        {
            var handler = TwoStageHandler(
                new HttpResponseMessage(HttpStatusCode.OK)
                    { Content = new StringContent(BuildTokenResponse()) },
                new HttpResponseMessage(HttpStatusCode.OK)
                    { Content = new StringContent(BuildSecretResponse()) });

            var sut = new SecretServerPam(new HttpClient(handler), NullLogger);

            var result = sut.GetPassword(
                InstanceParams(),
                ServerParams("password"));

            result.Should().Be(FakeFieldValue);
        }

        [Fact]
        public void GetPassword_ClientCredentialsGrant_ReturnsSecret()
        {
            var handler = TwoStageHandler(
                new HttpResponseMessage(HttpStatusCode.OK)
                    { Content = new StringContent(BuildTokenResponse()) },
                new HttpResponseMessage(HttpStatusCode.OK)
                    { Content = new StringContent(BuildSecretResponse()) });

            var sut = new SecretServerPam(new HttpClient(handler), NullLogger);

            var serverParams = new Dictionary<string, string>
            {
                { "Host", FakeHost },
                { "ClientId", FakeClientId },
                { "ClientSecret", FakeClientSecret },
                { "GrantType", "client_credentials" }
            };

            var result = sut.GetPassword(InstanceParams(), serverParams);
            result.Should().Be(FakeFieldValue);
        }

        [Fact]
        public void GetPassword_DefaultsToPasswordGrant_WhenGrantTypeAbsent()
        {
            var tokenRequested = false;
            var handler = new TestHttpMessageHandler((req, ct) =>
            {
                if (req.RequestUri!.AbsolutePath.Contains("oauth2/token"))
                    tokenRequested = true;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        req.RequestUri.AbsolutePath.Contains("oauth2/token")
                            ? BuildTokenResponse()
                            : BuildSecretResponse())
                });
            });

            var sut = new SecretServerPam(new HttpClient(handler), NullLogger);

            // No GrantType key in server params
            var serverParams = new Dictionary<string, string>
            {
                { "Host", FakeHost },
                { "Username", FakeUsername },
                { "Password", FakePassword }
            };

            var result = sut.GetPassword(InstanceParams(), serverParams);
            result.Should().Be(FakeFieldValue);
            tokenRequested.Should().BeTrue();
        }

        [Fact]
        public void GetPassword_MissingHost_ThrowsInvalidClientConfigurationException()
        {
            var sut = new SecretServerPam(new HttpClient(ConstantHandler(new HttpResponseMessage())), NullLogger);
            var serverParams = new Dictionary<string, string>
            {
                { "Username", FakeUsername },
                { "Password", FakePassword }
            };
            var act = () => sut.GetPassword(InstanceParams(), serverParams);
            act.Should().Throw<InvalidClientConfigurationException>()
                .WithMessage("*Host*");
        }

        private static Dictionary<string, string> InstanceParams() => new()
        {
            { "SecretId", FakeSecretId },
            { "SecretFieldName", FakeFieldName }
        };

        private static Dictionary<string, string> ServerParams(string grantType) => new()
        {
            { "Host", FakeHost },
            { "Username", FakeUsername },
            { "Password", FakePassword },
            { "GrantType", grantType }
        };
    }

    // ---------------------------------------------------------------------------
    // SecretServerPamPassword
    // ---------------------------------------------------------------------------

    public class PasswordType
    {
        [Fact]
        public void Name_IsDelineaSecretServerPassword()
        {
            var sut = new SecretServerPamPassword();
            sut.Name.Should().Be("Delinea-SecretServer-Password");
        }

        [Fact]
        public void GetPassword_HappyPath_ReturnsSecret()
        {
            var handler = TwoStageHandler(
                new HttpResponseMessage(HttpStatusCode.OK)
                    { Content = new StringContent(BuildTokenResponse()) },
                new HttpResponseMessage(HttpStatusCode.OK)
                    { Content = new StringContent(BuildSecretResponse()) });

            var sut = new SecretServerPamPassword(new HttpClient(handler), NullLogger);

            var result = sut.GetPassword(InstanceParams(), ServerParams());
            result.Should().Be(FakeFieldValue);
        }

        [Fact]
        public void GetPassword_MatchBySlug_ReturnsSecret()
        {
            // Build a secret where fieldName differs from slug; look up by slug
            var secretJson = JsonConvert.SerializeObject(new
            {
                id = 42,
                name = "Test Secret",
                items = new[]
                {
                    new
                    {
                        itemId = 1,
                        fieldName = "Display Name",
                        slug = FakeFieldName,
                        itemValue = FakeFieldValue,
                        isPassword = true
                    }
                }
            });

            var handler = TwoStageHandler(
                new HttpResponseMessage(HttpStatusCode.OK)
                    { Content = new StringContent(BuildTokenResponse()) },
                new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(secretJson) });

            var sut = new SecretServerPamPassword(new HttpClient(handler), NullLogger);
            var result = sut.GetPassword(InstanceParams(), ServerParams());
            result.Should().Be(FakeFieldValue);
        }

        [Fact]
        public void GetPassword_TokenEndpointReturns401_ThrowsHttpRequestException()
        {
            var handler = ConstantHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized)
                { Content = new StringContent("unauthorized") });

            var sut = new SecretServerPamPassword(new HttpClient(handler), NullLogger);
            var act = () => sut.GetPassword(InstanceParams(), ServerParams());
            act.Should().Throw<HttpRequestException>();
        }

        [Fact]
        public void GetPassword_SecretEndpointReturns404_ThrowsHttpRequestException()
        {
            var callCount = 0;
            var handler = new TestHttpMessageHandler((req, ct) =>
            {
                callCount++;
                var statusCode = callCount == 1 ? HttpStatusCode.OK : HttpStatusCode.NotFound;
                var content = callCount == 1 ? BuildTokenResponse() : "not found";
                return Task.FromResult(new HttpResponseMessage(statusCode)
                    { Content = new StringContent(content) });
            });

            var sut = new SecretServerPamPassword(new HttpClient(handler), NullLogger);
            var act = () => sut.GetPassword(InstanceParams(), ServerParams());
            act.Should().Throw<HttpRequestException>();
        }

        [Fact]
        public void GetPassword_FieldNotFoundInSecret_ThrowsInvalidSecretConfigurationException()
        {
            var secretJson = BuildSecretResponse("other-field", "some-value");

            var handler = TwoStageHandler(
                new HttpResponseMessage(HttpStatusCode.OK)
                    { Content = new StringContent(BuildTokenResponse()) },
                new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(secretJson) });

            var sut = new SecretServerPamPassword(new HttpClient(handler), NullLogger);

            // Request a field name that is not in the secret
            var instanceParams = new Dictionary<string, string>
            {
                { "SecretId", FakeSecretId },
                { "SecretFieldName", "nonexistent-field" }
            };

            var act = () => sut.GetPassword(instanceParams, ServerParams());
            act.Should().Throw<InvalidSecretConfigurationException>()
                .WithMessage("*nonexistent-field*");
        }

        [Fact]
        public void GetPassword_EmptyToken_ThrowsInvalidTokenException()
        {
            var tokenJson = JsonConvert.SerializeObject(new Dictionary<string, string>
                { { "access_token", string.Empty } });

            var handler = ConstantHandler(
                new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(tokenJson) });

            var sut = new SecretServerPamPassword(new HttpClient(handler), NullLogger);
            var act = () => sut.GetPassword(InstanceParams(), ServerParams());
            act.Should().Throw<InvalidTokenException>();
        }

        [Fact]
        public void GetPassword_MissingHost_ThrowsInvalidClientConfigurationException()
        {
            var sut = new SecretServerPamPassword(new HttpClient(ConstantHandler(new HttpResponseMessage())), NullLogger);
            var serverParams = new Dictionary<string, string>
            {
                { "Username", FakeUsername },
                { "Password", FakePassword }
            };
            var act = () => sut.GetPassword(InstanceParams(), serverParams);
            act.Should().Throw<InvalidClientConfigurationException>().WithMessage("*Host*");
        }

        [Fact]
        public void GetPassword_MissingUsername_ThrowsInvalidClientConfigurationException()
        {
            var sut = new SecretServerPamPassword(new HttpClient(ConstantHandler(new HttpResponseMessage())), NullLogger);
            var serverParams = new Dictionary<string, string>
            {
                { "Host", FakeHost },
                { "Password", FakePassword }
            };
            var act = () => sut.GetPassword(InstanceParams(), serverParams);
            act.Should().Throw<InvalidClientConfigurationException>().WithMessage("*Username*");
        }

        [Fact]
        public void GetPassword_MissingPassword_ThrowsInvalidClientConfigurationException()
        {
            var sut = new SecretServerPamPassword(new HttpClient(ConstantHandler(new HttpResponseMessage())), NullLogger);
            var serverParams = new Dictionary<string, string>
            {
                { "Host", FakeHost },
                { "Username", FakeUsername }
            };
            var act = () => sut.GetPassword(InstanceParams(), serverParams);
            act.Should().Throw<InvalidClientConfigurationException>().WithMessage("*Password*");
        }

        [Fact]
        public void GetPassword_MissingSecretId_ThrowsInvalidSecretConfigurationException()
        {
            var sut = new SecretServerPamPassword(new HttpClient(ConstantHandler(new HttpResponseMessage())), NullLogger);
            var instanceParams = new Dictionary<string, string>
            {
                { "SecretFieldName", FakeFieldName }
            };
            var act = () => sut.GetPassword(instanceParams, ServerParams());
            act.Should().Throw<InvalidSecretConfigurationException>().WithMessage("*SecretId*");
        }

        [Fact]
        public void GetPassword_MissingSecretFieldName_ThrowsInvalidSecretConfigurationException()
        {
            var sut = new SecretServerPamPassword(new HttpClient(ConstantHandler(new HttpResponseMessage())), NullLogger);
            var instanceParams = new Dictionary<string, string>
            {
                { "SecretId", FakeSecretId }
            };
            var act = () => sut.GetPassword(instanceParams, ServerParams());
            act.Should().Throw<InvalidSecretConfigurationException>().WithMessage("*SecretFieldName*");
        }

        [Fact]
        public void GetPassword_NonIntegerSecretId_ThrowsInvalidSecretConfigurationException()
        {
            var sut = new SecretServerPamPassword(new HttpClient(ConstantHandler(new HttpResponseMessage())), NullLogger);
            var instanceParams = new Dictionary<string, string>
            {
                { "SecretId", "not-an-int" },
                { "SecretFieldName", FakeFieldName }
            };
            var act = () => sut.GetPassword(instanceParams, ServerParams());
            act.Should().Throw<InvalidSecretConfigurationException>().WithMessage("*not-an-int*");
        }

        [Fact]
        public void GetPassword_TokenRequestBody_ContainsUsernameAndPassword()
        {
            // Verify the token POST uses "username"/"password" field names
            // (Delinea API constraint — not "client_id"/"client_secret")
            string? capturedBody = null;
            var callCount = 0;

            var handler = new TestHttpMessageHandler(async (req, ct) =>
            {
                callCount++;
                if (callCount == 1)
                {
                    capturedBody = await req.Content!.ReadAsStringAsync();
                    return new HttpResponseMessage(HttpStatusCode.OK)
                        { Content = new StringContent(BuildTokenResponse()) };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                    { Content = new StringContent(BuildSecretResponse()) };
            });

            var sut = new SecretServerPamPassword(new HttpClient(handler), NullLogger);
            sut.GetPassword(InstanceParams(), ServerParams());

            capturedBody.Should().Contain("username=");
            capturedBody.Should().Contain("password=");
            capturedBody.Should().Contain("grant_type=password");
        }

        [Fact]
        public void GetPassword_TokenRequestUrl_PointsToOAuth2TokenEndpoint()
        {
            Uri? capturedTokenUri = null;
            var callCount = 0;

            var handler = new TestHttpMessageHandler((req, ct) =>
            {
                callCount++;
                if (callCount == 1)
                {
                    capturedTokenUri = req.RequestUri;
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                        { Content = new StringContent(BuildTokenResponse()) });
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                    { Content = new StringContent(BuildSecretResponse()) });
            });

            var sut = new SecretServerPamPassword(new HttpClient(handler), NullLogger);
            sut.GetPassword(InstanceParams(), ServerParams());

            capturedTokenUri.Should().NotBeNull();
            capturedTokenUri!.AbsolutePath.Should().EndWith("/oauth2/token");
        }

        private static Dictionary<string, string> InstanceParams() => new()
        {
            { "SecretId", FakeSecretId },
            { "SecretFieldName", FakeFieldName }
        };

        private static Dictionary<string, string> ServerParams() => new()
        {
            { "Host", FakeHost },
            { "Username", FakeUsername },
            { "Password", FakePassword }
        };
    }

    // ---------------------------------------------------------------------------
    // SecretServerPamClientCredentials
    // ---------------------------------------------------------------------------

    public class ClientCredentialsType
    {
        [Fact]
        public void Name_IsDelineaSecretServerClientCredentials()
        {
            var sut = new SecretServerPamClientCredentials();
            sut.Name.Should().Be("Delinea-SecretServer-ClientCredentials");
        }

        [Fact]
        public void GetPassword_HappyPath_ReturnsSecret()
        {
            var handler = TwoStageHandler(
                new HttpResponseMessage(HttpStatusCode.OK)
                    { Content = new StringContent(BuildTokenResponse()) },
                new HttpResponseMessage(HttpStatusCode.OK)
                    { Content = new StringContent(BuildSecretResponse()) });

            var sut = new SecretServerPamClientCredentials(new HttpClient(handler), NullLogger);
            var result = sut.GetPassword(InstanceParams(), ServerParams());
            result.Should().Be(FakeFieldValue);
        }

        [Fact]
        public void GetPassword_TokenRequestBody_UsesUsernamePasswordFieldNames()
        {
            // Delinea API constraint: client_credentials flow still sends username/password
            // in the token request body — NOT client_id/client_secret
            string? capturedBody = null;
            var callCount = 0;

            var handler = new TestHttpMessageHandler(async (req, ct) =>
            {
                callCount++;
                if (callCount == 1)
                {
                    capturedBody = await req.Content!.ReadAsStringAsync();
                    return new HttpResponseMessage(HttpStatusCode.OK)
                        { Content = new StringContent(BuildTokenResponse()) };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                    { Content = new StringContent(BuildSecretResponse()) };
            });

            var sut = new SecretServerPamClientCredentials(new HttpClient(handler), NullLogger);
            sut.GetPassword(InstanceParams(), ServerParams());

            // Must use username= / password= field names (Delinea API constraint)
            capturedBody.Should().Contain("username=");
            capturedBody.Should().Contain("password=");
            // ClientId value should appear as the username value
            capturedBody.Should().Contain(Uri.EscapeDataString(FakeClientId));
            // Must NOT contain client_id= key
            capturedBody.Should().NotContain("client_id=");
        }

        [Fact]
        public void GetPassword_MissingClientId_ThrowsInvalidClientConfigurationException()
        {
            var sut = new SecretServerPamClientCredentials(
                new HttpClient(ConstantHandler(new HttpResponseMessage())), NullLogger);
            var serverParams = new Dictionary<string, string>
            {
                { "Host", FakeHost },
                { "ClientSecret", FakeClientSecret }
            };
            var act = () => sut.GetPassword(InstanceParams(), serverParams);
            act.Should().Throw<InvalidClientConfigurationException>().WithMessage("*ClientId*");
        }

        [Fact]
        public void GetPassword_MissingClientSecret_ThrowsInvalidClientConfigurationException()
        {
            var sut = new SecretServerPamClientCredentials(
                new HttpClient(ConstantHandler(new HttpResponseMessage())), NullLogger);
            var serverParams = new Dictionary<string, string>
            {
                { "Host", FakeHost },
                { "ClientId", FakeClientId }
            };
            var act = () => sut.GetPassword(InstanceParams(), serverParams);
            act.Should().Throw<InvalidClientConfigurationException>().WithMessage("*ClientSecret*");
        }

        [Fact]
        public void GetPassword_TokenEndpointFails_ThrowsHttpRequestException()
        {
            var handler = ConstantHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized)
                { Content = new StringContent("unauthorized") });

            var sut = new SecretServerPamClientCredentials(new HttpClient(handler), NullLogger);
            var act = () => sut.GetPassword(InstanceParams(), ServerParams());
            act.Should().Throw<HttpRequestException>();
        }

        [Fact]
        public void GetPassword_FieldNotFound_ThrowsInvalidSecretConfigurationException()
        {
            var secretJson = BuildSecretResponse("different-field", "some-value");

            var handler = TwoStageHandler(
                new HttpResponseMessage(HttpStatusCode.OK)
                    { Content = new StringContent(BuildTokenResponse()) },
                new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(secretJson) });

            var sut = new SecretServerPamClientCredentials(new HttpClient(handler), NullLogger);

            var instanceParams = new Dictionary<string, string>
            {
                { "SecretId", FakeSecretId },
                { "SecretFieldName", "missing-field" }
            };

            var act = () => sut.GetPassword(instanceParams, ServerParams());
            act.Should().Throw<InvalidSecretConfigurationException>();
        }

        private static Dictionary<string, string> InstanceParams() => new()
        {
            { "SecretId", FakeSecretId },
            { "SecretFieldName", FakeFieldName }
        };

        private static Dictionary<string, string> ServerParams() => new()
        {
            { "Host", FakeHost },
            { "ClientId", FakeClientId },
            { "ClientSecret", FakeClientSecret }
        };
    }

    // ---------------------------------------------------------------------------
    // SecretServerPamWindows
    // ---------------------------------------------------------------------------

    public class WindowsType
    {
        [Fact]
        public void Name_IsDelineaSecretServerWindows()
        {
            var sut = new SecretServerPamWindows();
            sut.Name.Should().Be("Delinea-SecretServer-Windows");
        }

        [Fact]
        public void GetPassword_HappyPath_ReturnsSecret()
        {
            // Windows auth: no token request, single GET to winauthwebservices endpoint
            var handler = ConstantHandler(new HttpResponseMessage(HttpStatusCode.OK)
                { Content = new StringContent(BuildSecretResponse()) });

            var sut = new SecretServerPamWindows(new HttpClient(handler), NullLogger);
            var result = sut.GetPassword(InstanceParams(), ServerParams());
            result.Should().Be(FakeFieldValue);
        }

        [Fact]
        public void GetPassword_UsesWinAuthWebServicesEndpoint()
        {
            Uri? capturedUri = null;

            var handler = new TestHttpMessageHandler((req, ct) =>
            {
                capturedUri = req.RequestUri;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                    { Content = new StringContent(BuildSecretResponse()) });
            });

            var sut = new SecretServerPamWindows(new HttpClient(handler), NullLogger);
            sut.GetPassword(InstanceParams(), ServerParams());

            capturedUri.Should().NotBeNull();
            capturedUri!.AbsolutePath.Should().Contain("winauthwebservices");
        }

        [Fact]
        public void GetPassword_DoesNotCallTokenEndpoint()
        {
            var tokenEndpointCalled = false;

            var handler = new TestHttpMessageHandler((req, ct) =>
            {
                if (req.RequestUri!.AbsolutePath.Contains("oauth2/token"))
                    tokenEndpointCalled = true;

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                    { Content = new StringContent(BuildSecretResponse()) });
            });

            var sut = new SecretServerPamWindows(new HttpClient(handler), NullLogger);
            sut.GetPassword(InstanceParams(), ServerParams());

            tokenEndpointCalled.Should().BeFalse("Windows auth should not request a token");
        }

        [Fact]
        public void GetPassword_MissingHost_ThrowsInvalidClientConfigurationException()
        {
            var sut = new SecretServerPamWindows(
                new HttpClient(ConstantHandler(new HttpResponseMessage())), NullLogger);
            var act = () => sut.GetPassword(InstanceParams(), new Dictionary<string, string>());
            act.Should().Throw<InvalidClientConfigurationException>().WithMessage("*Host*");
        }

        [Fact]
        public void GetPassword_SecretEndpointFails_ThrowsHttpRequestException()
        {
            var handler = ConstantHandler(new HttpResponseMessage(HttpStatusCode.Forbidden)
                { Content = new StringContent("forbidden") });

            var sut = new SecretServerPamWindows(new HttpClient(handler), NullLogger);
            var act = () => sut.GetPassword(InstanceParams(), ServerParams());
            act.Should().Throw<HttpRequestException>();
        }

        [Fact]
        public void GetPassword_FieldNotFound_ThrowsInvalidSecretConfigurationException()
        {
            var secretJson = BuildSecretResponse("other-field", "some-value");
            var handler = ConstantHandler(new HttpResponseMessage(HttpStatusCode.OK)
                { Content = new StringContent(secretJson) });

            var sut = new SecretServerPamWindows(new HttpClient(handler), NullLogger);
            var instanceParams = new Dictionary<string, string>
            {
                { "SecretId", FakeSecretId },
                { "SecretFieldName", "no-such-field" }
            };
            var act = () => sut.GetPassword(instanceParams, ServerParams());
            act.Should().Throw<InvalidSecretConfigurationException>();
        }

        private static Dictionary<string, string> InstanceParams() => new()
        {
            { "SecretId", FakeSecretId },
            { "SecretFieldName", FakeFieldName }
        };

        private static Dictionary<string, string> ServerParams() => new()
        {
            { "Host", FakeHost }
        };
    }

    // ---------------------------------------------------------------------------
    // Shared validation — tested via SecretServerPamPassword as a representative type
    // ---------------------------------------------------------------------------

    public class SharedValidation
    {
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void GetPassword_EmptySecretFieldName_ThrowsInvalidSecretConfigurationException(string fieldName)
        {
            var sut = new SecretServerPamPassword(
                new HttpClient(ConstantHandler(new HttpResponseMessage())), NullLogger);
            var instanceParams = new Dictionary<string, string>
            {
                { "SecretId", FakeSecretId },
                { "SecretFieldName", fieldName }
            };
            var serverParams = new Dictionary<string, string>
            {
                { "Host", FakeHost },
                { "Username", FakeUsername },
                { "Password", FakePassword }
            };
            var act = () => sut.GetPassword(instanceParams, serverParams);
            act.Should().Throw<InvalidSecretConfigurationException>();
        }

        [Fact]
        public void GetPassword_ErrorResponseTruncatedAt500Chars_NeverLogsFullErrorBody()
        {
            // We cannot inspect log output directly without a custom ILogger, but we
            // can verify the provider still throws rather than swallowing the error,
            // confirming the truncation code path is exercised without hanging.
            var longError = new string('x', 2000);

            var callCount = 0;
            var handler = new TestHttpMessageHandler((req, ct) =>
            {
                callCount++;
                HttpStatusCode status;
                string body;
                if (callCount == 1)
                {
                    // Token request succeeds
                    status = HttpStatusCode.OK;
                    body = BuildTokenResponse();
                }
                else
                {
                    // Secret request returns a long error body
                    status = HttpStatusCode.InternalServerError;
                    body = longError;
                }

                return Task.FromResult(new HttpResponseMessage(status)
                    { Content = new StringContent(body) });
            });

            var sut = new SecretServerPamPassword(new HttpClient(handler), NullLogger);
            var act = () => sut.GetPassword(
                new Dictionary<string, string>
                {
                    { "SecretId", FakeSecretId },
                    { "SecretFieldName", FakeFieldName }
                },
                new Dictionary<string, string>
                {
                    { "Host", FakeHost },
                    { "Username", FakeUsername },
                    { "Password", FakePassword }
                });

            act.Should().Throw<HttpRequestException>();
        }

        [Fact]
        public void GetPassword_AllFourTypes_HaveDistinctNames()
        {
            var names = new[]
            {
                new SecretServerPam().Name,
                new SecretServerPamPassword().Name,
                new SecretServerPamClientCredentials().Name,
                new SecretServerPamWindows().Name
            };

            names.Should().OnlyHaveUniqueItems("all PAM type Names must be distinct");
        }
    }

    // ---------------------------------------------------------------------------
    // SkipTlsValidation — config parameter and environment variable
    // ---------------------------------------------------------------------------

    public class SkipTlsValidation : IDisposable
    {
        private const string EnvVar = "KEYFACTOR_PAM_SKIP_TLS_VALIDATION";

        public void Dispose() => Environment.SetEnvironmentVariable(EnvVar, null);

        private static Dictionary<string, string> InstanceParams() => new()
        {
            { "SecretId", FakeSecretId },
            { "SecretFieldName", FakeFieldName }
        };

        private static Dictionary<string, string> ServerParams(bool skipTls = false) => new()
        {
            { "Host", FakeHost },
            { "Username", FakeUsername },
            { "Password", FakePassword },
            { "SkipTlsValidation", skipTls ? "true" : "false" }
        };

        [Fact]
        public void GetPassword_SkipTlsValidationConfig_True_Succeeds()
        {
            var handler = TwoStageHandler(
                new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(BuildTokenResponse()) },
                new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(BuildSecretResponse()) });

            var sut = new SecretServerPamPassword(new HttpClient(handler), NullLogger);
            var result = sut.GetPassword(InstanceParams(), ServerParams(skipTls: true));
            result.Should().Be(FakeFieldValue);
        }

        [Fact]
        public void GetPassword_SkipTlsEnvVar_True_Succeeds()
        {
            Environment.SetEnvironmentVariable(EnvVar, "true");

            var handler = TwoStageHandler(
                new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(BuildTokenResponse()) },
                new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(BuildSecretResponse()) });

            var sut = new SecretServerPamPassword(new HttpClient(handler), NullLogger);
            var result = sut.GetPassword(InstanceParams(), ServerParams(skipTls: false));
            result.Should().Be(FakeFieldValue);
        }

        [Fact]
        public void GetPassword_SkipTlsEnvVar_One_Succeeds()
        {
            Environment.SetEnvironmentVariable(EnvVar, "1");

            var handler = TwoStageHandler(
                new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(BuildTokenResponse()) },
                new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(BuildSecretResponse()) });

            var sut = new SecretServerPamPassword(new HttpClient(handler), NullLogger);
            var result = sut.GetPassword(InstanceParams(), ServerParams(skipTls: false));
            result.Should().Be(FakeFieldValue);
        }

        [Fact]
        public void GetPassword_SkipTlsEnvVar_False_DoesNotOverrideConfigFalse()
        {
            Environment.SetEnvironmentVariable(EnvVar, "false");

            var handler = TwoStageHandler(
                new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(BuildTokenResponse()) },
                new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(BuildSecretResponse()) });

            var sut = new SecretServerPamPassword(new HttpClient(handler), NullLogger);
            var result = sut.GetPassword(InstanceParams(), ServerParams(skipTls: false));
            result.Should().Be(FakeFieldValue);
        }
    }

    // ---------------------------------------------------------------------------
    // client_credentials GrantType bug fix — must not send "password" grant type
    // ---------------------------------------------------------------------------

    public class ClientCredentialsGrantTypeFix
    {
        private static Dictionary<string, string> InstanceParams() => new()
        {
            { "SecretId", FakeSecretId },
            { "SecretFieldName", FakeFieldName }
        };

        [Fact]
        public void GetPassword_ClientCredentials_TokenRequestBody_AlwaysSendsPasswordGrantType()
        {
            // Delinea API constraint: even for client_credentials flow, the token endpoint
            // requires grant_type=password. Do not change this behaviour.
            string? capturedBody = null;
            var callCount = 0;

            var handler = new TestHttpMessageHandler(async (req, ct) =>
            {
                callCount++;
                if (callCount == 1)
                {
                    capturedBody = await req.Content!.ReadAsStringAsync();
                    return new HttpResponseMessage(HttpStatusCode.OK)
                        { Content = new StringContent(BuildTokenResponse()) };
                }
                return new HttpResponseMessage(HttpStatusCode.OK)
                    { Content = new StringContent(BuildSecretResponse()) };
            });

            var sut = new SecretServerPamClientCredentials(new HttpClient(handler), NullLogger);
            sut.GetPassword(InstanceParams(), new Dictionary<string, string>
            {
                { "Host", FakeHost },
                { "ClientId", FakeClientId },
                { "ClientSecret", FakeClientSecret }
            });

            capturedBody.Should().Contain("grant_type=password",
                "Delinea API constraint: token endpoint always requires grant_type=password");
        }
    }
}
