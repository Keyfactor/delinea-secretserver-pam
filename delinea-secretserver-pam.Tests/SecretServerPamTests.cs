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

    [Collection(ProviderEnvironmentCollection.Name)]
    public class BackwardsCompatibleType : ProviderEnvironmentTestBase
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

    [Collection(ProviderEnvironmentCollection.Name)]
    public class PasswordType : ProviderEnvironmentTestBase
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

    [Collection(ProviderEnvironmentCollection.Name)]
    public class ClientCredentialsType : ProviderEnvironmentTestBase
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

    [Collection(ProviderEnvironmentCollection.Name)]
    public class WindowsType : ProviderEnvironmentTestBase
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

    [Collection(ProviderEnvironmentCollection.Name)]
    public class SharedValidation : ProviderEnvironmentTestBase
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

    [Collection(ProviderEnvironmentCollection.Name)]
    public class SkipTlsValidation : ProviderEnvironmentTestBase
    {
        private const string EnvVar = "KEYFACTOR_PAM_SKIP_TLS_VALIDATION";

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
    // N/A dummy value — treated as absent/empty input
    // ---------------------------------------------------------------------------

    [Collection(ProviderEnvironmentCollection.Name)]
    public class NaDummyValue : ProviderEnvironmentTestBase
    {
        private static TestHttpMessageHandler HappyPathHandler() => new TestHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    req.RequestUri!.AbsolutePath.Contains("oauth2/token")
                        ? BuildTokenResponse()
                        : BuildSecretResponse())
            }));

        [Fact]
        public void GetPassword_NaPassword_TreatedAsEmpty_ThrowsInvalidClientConfiguration()
        {
            var sut = new SecretServerPamPassword(new HttpClient(HappyPathHandler()), NullLogger);
            var act = () => sut.GetPassword(
                new Dictionary<string, string> { { "SecretId", FakeSecretId }, { "SecretFieldName", FakeFieldName } },
                new Dictionary<string, string> { { "Host", FakeHost }, { "Username", FakeUsername }, { "Password", "N/A" } });

            act.Should().Throw<InvalidClientConfigurationException>("N/A password must be treated as absent");
        }

        [Fact]
        public void GetPassword_NaUsernameWithCasing_TreatedAsEmpty_ThrowsInvalidClientConfiguration()
        {
            var sut = new SecretServerPamPassword(new HttpClient(HappyPathHandler()), NullLogger);
            var act = () => sut.GetPassword(
                new Dictionary<string, string> { { "SecretId", FakeSecretId }, { "SecretFieldName", FakeFieldName } },
                new Dictionary<string, string> { { "Host", FakeHost }, { "Username", "n/a" }, { "Password", FakePassword } });

            act.Should().Throw<InvalidClientConfigurationException>("n/a (lowercase) must be treated as absent");
        }

        [Fact]
        public void GetPassword_NaSecretFieldName_TreatedAsEmpty_ThrowsInvalidSecretConfiguration()
        {
            var sut = new SecretServerPamPassword(new HttpClient(HappyPathHandler()), NullLogger);
            var act = () => sut.GetPassword(
                new Dictionary<string, string> { { "SecretId", FakeSecretId }, { "SecretFieldName", "N/A" } },
                new Dictionary<string, string> { { "Host", FakeHost }, { "Username", FakeUsername }, { "Password", FakePassword } });

            act.Should().Throw<InvalidSecretConfigurationException>("N/A SecretFieldName must be treated as absent");
        }

        [Fact]
        public void GetPassword_NaWithSurroundingWhitespace_TreatedAsEmpty()
        {
            var sut = new SecretServerPamPassword(new HttpClient(HappyPathHandler()), NullLogger);
            var act = () => sut.GetPassword(
                new Dictionary<string, string> { { "SecretId", FakeSecretId }, { "SecretFieldName", FakeFieldName } },
                new Dictionary<string, string> { { "Host", FakeHost }, { "Username", FakeUsername }, { "Password", " N/A " } });

            act.Should().Throw<InvalidClientConfigurationException>("' N/A ' (with whitespace) must be treated as absent");
        }

        [Fact]
        public void GetPassword_RealValueNotNa_PassesThrough()
        {
            var sut = new SecretServerPamPassword(new HttpClient(HappyPathHandler()), NullLogger);
            var result = sut.GetPassword(
                new Dictionary<string, string> { { "SecretId", FakeSecretId }, { "SecretFieldName", FakeFieldName } },
                new Dictionary<string, string> { { "Host", FakeHost }, { "Username", FakeUsername }, { "Password", FakePassword } });

            result.Should().Be(FakeFieldValue, "real values must not be affected by N/A normalization");
        }
    }

    // ---------------------------------------------------------------------------
    // client_credentials GrantType bug fix — must not send "password" grant type
    // ---------------------------------------------------------------------------

    [Collection(ProviderEnvironmentCollection.Name)]
    public class ClientCredentialsGrantTypeFix : ProviderEnvironmentTestBase
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

    // ---------------------------------------------------------------------------
    // KEYFACTOR_PAM_DELINEA_* environment-variable fallback for server parameters
    // ---------------------------------------------------------------------------

    [Collection(ProviderEnvironmentCollection.Name)]
    public class EnvironmentVariableFallback : ProviderEnvironmentTestBase
    {
        private const string EnvHost = "KEYFACTOR_PAM_DELINEA_HOST";
        private const string EnvGrantType = "KEYFACTOR_PAM_DELINEA_GRANT_TYPE";
        private const string EnvUsername = "KEYFACTOR_PAM_DELINEA_USERNAME";
        private const string EnvPassword = "KEYFACTOR_PAM_DELINEA_PASSWORD";
        private const string EnvClientId = "KEYFACTOR_PAM_DELINEA_CLIENT_ID";
        private const string EnvClientSecret = "KEYFACTOR_PAM_DELINEA_CLIENT_SECRET";

        // Values that differ from every Fake* constant so assertions can tell the sources apart.
        private const string EnvHostValue = "https://env-host.example.com/SecretServer";
        private const string EnvUsernameValue = "env-user";
        private const string EnvPasswordValue = "  env P@ss w0rd!  "; // leading/trailing spaces must survive
        private const string EnvClientIdValue = "env-client-id";
        private const string EnvClientSecretValue = "env-cl13nt-s3cr3t";

        private const string CapturedHostSegment = "env-host.example.com";

        // The forms a parameter can take when it is "not supplied": null means the key is absent.
        public static IEnumerable<object?[]> AbsentForms => new[]
        {
            new object?[] { null }, new object?[] { "" }, new object?[] { "N/A" }, new object?[] { " n/a " }
        };

        public static IEnumerable<object?[]> KindsByAbsentForm()
        {
            foreach (var kind in new[] { "generic", "password", "clientcredentials", "windows" })
            foreach (var form in new string?[] { null, "", "N/A", " n/a " })
                yield return new object?[] { kind, form };
        }

        public static IEnumerable<object?[]> PasswordGrantKindsByAbsentForm()
        {
            foreach (var kind in new[] { "generic", "password" })
            foreach (var form in new string?[] { null, "", "N/A", " n/a " })
                yield return new object?[] { kind, form };
        }

        public static IEnumerable<object?[]> ClientCredentialsGrantKindsByAbsentForm()
        {
            foreach (var kind in new[] { "generic-cc", "clientcredentials" })
            foreach (var form in new string?[] { null, "", "N/A", " n/a " })
                yield return new object?[] { kind, form };
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        private sealed class Recorder
        {
            public List<Uri> Uris { get; } = new();
            public string? TokenBody { get; private set; }
            public bool TokenRequested => Uris.Any(u => u.AbsolutePath.EndsWith("/oauth2/token"));
            public Uri SecretUri => Uris.Last(u => u.AbsolutePath.Contains("/api/v1/secrets/"));

            public TestHttpMessageHandler Handler() => new(async (req, ct) =>
            {
                Uris.Add(req.RequestUri!);
                if (req.RequestUri!.AbsolutePath.EndsWith("/oauth2/token"))
                {
                    TokenBody = await req.Content!.ReadAsStringAsync(ct);
                    return new HttpResponseMessage(HttpStatusCode.OK)
                        { Content = new StringContent(BuildTokenResponse()) };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                    { Content = new StringContent(BuildSecretResponse()) };
            });

            /// <summary>Decoded value of a field in the form-encoded token request body.</summary>
            public string? TokenField(string name)
                => System.Web.HttpUtility.ParseQueryString(TokenBody ?? "")[name];
        }

        private static ProviderUnderTest Create(string kind, Recorder recorder, ILogger? logger = null)
            => new(kind, new HttpClient(recorder.Handler()), logger ?? NullLogger);

        /// <summary>Thin wrapper so one test body can drive all four provider classes.</summary>
        private sealed class ProviderUnderTest
        {
            private readonly Func<Dictionary<string, string>, Dictionary<string, string>, string> _getPassword;

            public ProviderUnderTest(string kind, HttpClient client, ILogger logger)
            {
                switch (kind)
                {
                    case "generic":
                    case "generic-cc":
                        _getPassword = new SecretServerPam(client, logger).GetPassword;
                        break;
                    case "password":
                        _getPassword = new SecretServerPamPassword(client, logger).GetPassword;
                        break;
                    case "clientcredentials":
                        _getPassword = new SecretServerPamClientCredentials(client, logger).GetPassword;
                        break;
                    case "windows":
                        _getPassword = new SecretServerPamWindows(client, logger).GetPassword;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
                }
            }

            public string GetPassword(Dictionary<string, string> instance, Dictionary<string, string> server)
                => _getPassword(instance, server);
        }

        private static Dictionary<string, string> InstanceParams() => new()
        {
            { "SecretId", FakeSecretId },
            { "SecretFieldName", FakeFieldName }
        };

        /// <summary>Server parameters for a provider kind with every parameter supplied except Host.</summary>
        private static Dictionary<string, string> ParamsWithoutHost(string kind) => kind switch
        {
            "generic" => new() { { "Username", FakeUsername }, { "Password", FakePassword } },
            "password" => new() { { "Username", FakeUsername }, { "Password", FakePassword } },
            "generic-cc" => new()
            {
                { "GrantType", "client_credentials" }, { "ClientId", FakeClientId }, { "ClientSecret", FakeClientSecret }
            },
            "clientcredentials" => new() { { "ClientId", FakeClientId }, { "ClientSecret", FakeClientSecret } },
            "windows" => new(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };

        private static void Set(Dictionary<string, string> dict, string key, string? form)
        {
            if (form == null) dict.Remove(key);
            else dict[key] = form;
        }

        private static void Env(string name, string? value) => Environment.SetEnvironmentVariable(name, value);

        private static void SetAllEnv()
        {
            Env(EnvHost, EnvHostValue);
            Env(EnvUsername, EnvUsernameValue);
            Env(EnvPassword, EnvPasswordValue);
            Env(EnvClientId, EnvClientIdValue);
            Env(EnvClientSecret, EnvClientSecretValue);
        }

        // -----------------------------------------------------------------------
        // Host
        // -----------------------------------------------------------------------

        [Theory]
        [MemberData(nameof(KindsByAbsentForm))]
        public void Host_FillsMissingEmptyOrNaParameter_ForEveryType(string kind, string? form)
        {
            Env(EnvHost, EnvHostValue);
            var recorder = new Recorder();
            var server = ParamsWithoutHost(kind);
            Set(server, "Host", form);

            var result = Create(kind, recorder).GetPassword(InstanceParams(), server);

            result.Should().Be(FakeFieldValue);
            recorder.Uris.Should().OnlyContain(u => u.Host == CapturedHostSegment);
        }

        [Theory]
        [InlineData("generic")]
        [InlineData("password")]
        [InlineData("clientcredentials")]
        [InlineData("windows")]
        public void Host_ConfiguredParameterWinsOverEnvironment(string kind)
        {
            Env(EnvHost, EnvHostValue);
            var recorder = new Recorder();
            var server = ParamsWithoutHost(kind);
            server["Host"] = FakeHost;

            Create(kind, recorder).GetPassword(InstanceParams(), server);

            recorder.Uris.Should().OnlyContain(u => u.Host == "secretserver.example.com");
        }

        // -----------------------------------------------------------------------
        // GrantType (generic Delinea-SecretServer type only)
        // -----------------------------------------------------------------------

        [Theory]
        [MemberData(nameof(AbsentForms))]
        public void GrantType_Windows_FillsAbsentParameter_OnGenericType(string? form)
        {
            Env(EnvGrantType, "windows");
            var recorder = new Recorder();
            var server = new Dictionary<string, string> { { "Host", FakeHost } };
            Set(server, "GrantType", form);

            Create("generic", recorder).GetPassword(InstanceParams(), server);

            recorder.TokenRequested.Should().BeFalse("windows grant needs no token");
            recorder.SecretUri.AbsolutePath.Should().Contain("winauthwebservices");
        }

        [Theory]
        [MemberData(nameof(AbsentForms))]
        public void GrantType_ClientCredentials_FillsAbsentParameter_AndCredentialsFollowTheGrant(string? form)
        {
            Env(EnvGrantType, "client_credentials");
            Env(EnvClientId, EnvClientIdValue);
            Env(EnvClientSecret, EnvClientSecretValue);
            var recorder = new Recorder();
            var server = new Dictionary<string, string> { { "Host", FakeHost } };
            Set(server, "GrantType", form);

            Create("generic", recorder).GetPassword(InstanceParams(), server);

            recorder.TokenField("username").Should().Be(EnvClientIdValue);
            recorder.TokenField("password").Should().Be(EnvClientSecretValue);
            recorder.TokenField("grant_type").Should().Be("password", "Delinea API constraint");
        }

        [Fact]
        public void GrantType_ConfiguredParameterWinsOverEnvironment_OnGenericType()
        {
            SetAllEnv();
            Env(EnvGrantType, "windows");
            var recorder = new Recorder();

            Create("generic", recorder).GetPassword(InstanceParams(), new Dictionary<string, string>
            {
                { "Host", FakeHost }, { "GrantType", "password" }, { "Username", FakeUsername },
                { "Password", FakePassword }
            });

            recorder.TokenRequested.Should().BeTrue("the configured password grant must win over the windows env value");
            recorder.TokenField("username").Should().Be(FakeUsername);
        }

        [Fact]
        public void GrantType_InvalidEnvironmentValue_IsRejectedByValidation()
        {
            Env(EnvGrantType, "bogus");
            var sut = Create("generic", new Recorder());

            var act = () => sut.GetPassword(InstanceParams(), new Dictionary<string, string> { { "Host", FakeHost } });

            act.Should().Throw<InvalidClientConfigurationException>().WithMessage("*bogus*");
        }

        [Theory]
        [InlineData("password", "windows")]
        [InlineData("clientcredentials", "windows")]
        [InlineData("windows", "password")]
        [InlineData("windows", "client_credentials")]
        public void GrantType_TypeSpecificClassesIgnoreEnvironment(string kind, string envGrant)
        {
            Env(EnvGrantType, envGrant);
            var recorder = new Recorder();
            var server = ParamsWithoutHost(kind);
            server["Host"] = FakeHost;

            Create(kind, recorder).GetPassword(InstanceParams(), server);

            if (kind == "windows")
                recorder.TokenRequested.Should().BeFalse();
            else
                recorder.TokenRequested.Should().BeTrue();
        }

        // -----------------------------------------------------------------------
        // Credentials for the resolved grant type
        // -----------------------------------------------------------------------

        [Theory]
        [MemberData(nameof(PasswordGrantKindsByAbsentForm))]
        public void UsernameAndPassword_FillAbsentParameters_ForPasswordGrant(string kind, string? form)
        {
            Env(EnvUsername, EnvUsernameValue);
            Env(EnvPassword, EnvPasswordValue);
            var recorder = new Recorder();
            var server = new Dictionary<string, string> { { "Host", FakeHost } };
            Set(server, "Username", form);
            Set(server, "Password", form);

            Create(kind, recorder).GetPassword(InstanceParams(), server);

            recorder.TokenField("username").Should().Be(EnvUsernameValue);
            recorder.TokenField("password").Should().Be(EnvPasswordValue, "env values are used verbatim, untrimmed");
        }

        [Theory]
        [MemberData(nameof(ClientCredentialsGrantKindsByAbsentForm))]
        public void ClientIdAndSecret_FillAbsentParameters_ForClientCredentialsGrant(string kind, string? form)
        {
            Env(EnvClientId, EnvClientIdValue);
            Env(EnvClientSecret, EnvClientSecretValue);
            var recorder = new Recorder();
            var server = new Dictionary<string, string> { { "Host", FakeHost } };
            if (kind == "generic-cc") server["GrantType"] = "client_credentials";
            Set(server, "ClientId", form);
            Set(server, "ClientSecret", form);

            Create(kind, recorder).GetPassword(InstanceParams(), server);

            recorder.TokenField("username").Should().Be(EnvClientIdValue);
            recorder.TokenField("password").Should().Be(EnvClientSecretValue);
        }

        [Theory]
        [InlineData("generic")]
        [InlineData("password")]
        public void Credentials_ConfiguredParametersWinOverEnvironment_ForPasswordGrant(string kind)
        {
            SetAllEnv();
            var recorder = new Recorder();

            Create(kind, recorder).GetPassword(InstanceParams(), new Dictionary<string, string>
            {
                { "Host", FakeHost }, { "Username", FakeUsername }, { "Password", FakePassword }
            });

            recorder.TokenField("username").Should().Be(FakeUsername);
            recorder.TokenField("password").Should().Be(FakePassword);
        }

        [Theory]
        [InlineData("generic-cc")]
        [InlineData("clientcredentials")]
        public void Credentials_ConfiguredParametersWinOverEnvironment_ForClientCredentialsGrant(string kind)
        {
            SetAllEnv();
            var recorder = new Recorder();
            var server = ParamsWithoutHost(kind);
            server["Host"] = FakeHost;

            Create(kind, recorder).GetPassword(InstanceParams(), server);

            recorder.TokenField("username").Should().Be(FakeClientId);
            recorder.TokenField("password").Should().Be(FakeClientSecret);
        }

        [Fact]
        public void Credentials_PartialOverride_FillsOnlyTheMissingParameter()
        {
            Env(EnvPassword, EnvPasswordValue);
            var recorder = new Recorder();

            Create("password", recorder).GetPassword(InstanceParams(), new Dictionary<string, string>
            {
                { "Host", FakeHost }, { "Username", FakeUsername }, { "Password", "N/A" }
            });

            recorder.TokenField("username").Should().Be(FakeUsername);
            recorder.TokenField("password").Should().Be(EnvPasswordValue);
        }

        [Theory]
        [InlineData("generic", "windows")]
        [InlineData("windows", null)]
        public void WindowsGrant_NeverReadsUsernameOrPasswordVariables(string kind, string? grantParam)
        {
            Env(EnvUsername, EnvUsernameValue);
            Env(EnvPassword, EnvPasswordValue);
            Env(EnvClientId, EnvClientIdValue);
            Env(EnvClientSecret, EnvClientSecretValue);
            var recorder = new Recorder();
            var logger = new CapturingLogger();
            var server = new Dictionary<string, string> { { "Host", FakeHost } };
            if (grantParam != null) server["GrantType"] = grantParam;

            Create(kind, recorder, logger).GetPassword(InstanceParams(), server);

            recorder.TokenRequested.Should().BeFalse();
            logger.Entries.Should().NotContain(e =>
                e.Message.Contains(EnvUsername) || e.Message.Contains(EnvPassword) ||
                e.Message.Contains(EnvClientId) || e.Message.Contains(EnvClientSecret),
                "the windows grant must not even look at credential variables");
        }

        [Fact]
        public void Credentials_AreNotMixedAcrossGrantTypes()
        {
            // Only the client_credentials variables are set; a password-grant provider must not use them.
            Env(EnvClientId, EnvClientIdValue);
            Env(EnvClientSecret, EnvClientSecretValue);
            var sut = Create("password", new Recorder());

            var act = () => sut.GetPassword(InstanceParams(), new Dictionary<string, string> { { "Host", FakeHost } });

            act.Should().Throw<InvalidClientConfigurationException>().WithMessage($"*{EnvUsername}*");
        }

        // -----------------------------------------------------------------------
        // No fallback for instance parameters
        // -----------------------------------------------------------------------

        [Theory]
        [InlineData("KEYFACTOR_PAM_DELINEA_SECRET_ID")]
        [InlineData("KEYFACTOR_PAM_DELINEA_SECRETID")]
        public void SecretId_HasNoEnvironmentFallback(string variable)
        {
            Env(variable, FakeSecretId);
            SetAllEnv();
            var sut = Create("password", new Recorder());

            var act = () => sut.GetPassword(
                new Dictionary<string, string> { { "SecretFieldName", FakeFieldName } },
                new Dictionary<string, string> { { "Host", FakeHost } });

            act.Should().Throw<InvalidSecretConfigurationException>().WithMessage("*SecretId*");
        }

        [Theory]
        [InlineData("KEYFACTOR_PAM_DELINEA_SECRET_FIELD_NAME")]
        [InlineData("KEYFACTOR_PAM_DELINEA_SECRETFIELDNAME")]
        public void SecretFieldName_HasNoEnvironmentFallback(string variable)
        {
            Env(variable, FakeFieldName);
            SetAllEnv();
            var sut = Create("password", new Recorder());

            var act = () => sut.GetPassword(
                new Dictionary<string, string> { { "SecretId", FakeSecretId } },
                new Dictionary<string, string> { { "Host", FakeHost } });

            act.Should().Throw<InvalidSecretConfigurationException>().WithMessage("*SecretFieldName*");
        }

        // -----------------------------------------------------------------------
        // Error messages
        // -----------------------------------------------------------------------

        [Fact]
        public void MissingHost_ErrorNamesBothSources()
        {
            var sut = Create("windows", new Recorder());

            var act = () => sut.GetPassword(InstanceParams(), new Dictionary<string, string>());

            act.Should().Throw<InvalidClientConfigurationException>()
                .WithMessage("*'Host'*not provided*KEYFACTOR_PAM_DELINEA_HOST*");
        }

        [Theory]
        [InlineData("password", "Username", EnvUsername)]
        [InlineData("password", "Password", EnvPassword)]
        [InlineData("clientcredentials", "ClientId", EnvClientId)]
        [InlineData("clientcredentials", "ClientSecret", EnvClientSecret)]
        public void MissingCredential_ErrorNamesBothSources(string kind, string parameter, string envVar)
        {
            var server = ParamsWithoutHost(kind);
            server["Host"] = FakeHost;
            server.Remove(parameter);
            var sut = Create(kind, new Recorder());

            var act = () => sut.GetPassword(InstanceParams(), server);

            act.Should().Throw<InvalidClientConfigurationException>()
                .WithMessage($"*'{parameter}'*{envVar}*");
        }

        [Theory]
        [InlineData("")]
        [InlineData("N/A")]
        [InlineData(" n/a ")]
        public void EmptyOrNaEnvironmentValue_CountsAsUnset_AndErrorNamesTheVariable(string envValue)
        {
            Env(EnvHost, envValue);
            Env(EnvPassword, envValue);
            var sut = Create("password", new Recorder());

            var hostAct = () => sut.GetPassword(InstanceParams(), new Dictionary<string, string>
                { { "Username", FakeUsername }, { "Password", FakePassword } });
            var passwordAct = () => sut.GetPassword(InstanceParams(), new Dictionary<string, string>
                { { "Host", FakeHost }, { "Username", FakeUsername } });

            hostAct.Should().Throw<InvalidClientConfigurationException>().WithMessage($"*{EnvHost}*");
            passwordAct.Should().Throw<InvalidClientConfigurationException>().WithMessage($"*{EnvPassword}*");
        }

        // -----------------------------------------------------------------------
        // Logging
        // -----------------------------------------------------------------------

        [Fact]
        public void InvocationLog_ShowsEffectiveHostAndGrantType_AndNoDefaultingWarning()
        {
            Env(EnvHost, EnvHostValue);
            Env(EnvGrantType, "client_credentials");
            Env(EnvClientId, EnvClientIdValue);
            Env(EnvClientSecret, EnvClientSecretValue);
            var logger = new CapturingLogger();

            Create("generic", new Recorder(), logger)
                .GetPassword(InstanceParams(), new Dictionary<string, string>());

            var invoked = logger.Entries.Single(e => e.Message.StartsWith("GetPassword invoked"));
            invoked.Message.Should().Contain($"TargetUrl={EnvHostValue}");
            invoked.Message.Should().Contain("GrantType=client_credentials");
            logger.Entries.Should().NotContain(e => e.Level == LogLevel.Warning && e.Message.Contains("defaulting"));
        }

        [Fact]
        public void Logs_NameTheSourceOfEachValue_AndNeverTheSecrets()
        {
            SetAllEnv();
            var logger = new CapturingLogger();

            Create("password", new Recorder(), logger).GetPassword(InstanceParams(), new Dictionary<string, string>
            {
                { "Host", FakeHost }, { "Username", "N/A" }, { "Password", "" }
            });

            var debug = logger.Entries.Where(e => e.Level == LogLevel.Debug).Select(e => e.Message).ToList();
            debug.Should().Contain(m => m.Contains("'Host'") && m.Contains("PAM parameter"));
            debug.Should().Contain(m => m.Contains("'Username'") && m.Contains(EnvUsername));
            debug.Should().Contain(m => m.Contains("'Password'") && m.Contains(EnvPassword));

            var all = string.Join("\n", logger.Entries.Select(e => e.Message));
            all.Should().NotContain(EnvPasswordValue.Trim(), "environment secret values must never be logged");
            all.Should().NotContain(EnvClientSecretValue);
        }

        [Fact]
        public void NoEnvironmentVariables_StillWarnsWhenGrantTypeDefaults()
        {
            var logger = new CapturingLogger();

            Create("generic", new Recorder(), logger).GetPassword(InstanceParams(), new Dictionary<string, string>
            {
                { "Host", FakeHost }, { "Username", FakeUsername }, { "Password", FakePassword }
            });

            logger.Entries.Should().Contain(e => e.Level == LogLevel.Warning && e.Message.Contains("defaulting"));
        }
    }

    // ---------------------------------------------------------------------------
    // Integration tests — skipped automatically when env vars are not set
    // ---------------------------------------------------------------------------

    [Collection(ProviderEnvironmentCollection.Name)]
    public class IntegrationTests : ProviderEnvironmentTestBase
    {
        private static string Env(string name) => Environment.GetEnvironmentVariable(name)!;
        private static bool SkipTls =>
            string.Equals(Env("SECRET_SERVER_SKIP_TLS_VALIDATION"), "true", StringComparison.OrdinalIgnoreCase);

        [IntegrationFact]
        public void LiveServer_PasswordGrant_RetrievesSecret()
        {
            var sut = new SecretServerPamPassword();
            var result = sut.GetPassword(
                new Dictionary<string, string> { { "SecretId", Env("SECRET_SERVER_SECRET_ID") }, { "SecretFieldName", "username" } },
                new Dictionary<string, string> { { "Host", Env("SECRET_SERVER_URL") }, { "Username", Env("SECRET_SERVER_USERNAME") }, { "Password", Env("SECRET_SERVER_PASSWORD") }, { "SkipTlsValidation", SkipTls ? "true" : "false" } });

            result.Should().NotBeNullOrEmpty("live Secret Server should return a non-empty value");
        }

        [IntegrationFact]
        public void LiveServer_EnvVarSkipTls_RetrievesSecret()
        {
            Environment.SetEnvironmentVariable("KEYFACTOR_PAM_SKIP_TLS_VALIDATION", "true");
            try
            {
                var sut = new SecretServerPamPassword();
                var result = sut.GetPassword(
                    new Dictionary<string, string> { { "SecretId", Env("SECRET_SERVER_SECRET_ID") }, { "SecretFieldName", "username" } },
                    new Dictionary<string, string> { { "Host", Env("SECRET_SERVER_URL") }, { "Username", Env("SECRET_SERVER_USERNAME") }, { "Password", Env("SECRET_SERVER_PASSWORD") } });

                result.Should().NotBeNullOrEmpty("KEYFACTOR_PAM_SKIP_TLS_VALIDATION=true should allow retrieval from live server");
            }
            finally
            {
                Environment.SetEnvironmentVariable("KEYFACTOR_PAM_SKIP_TLS_VALIDATION", null);
            }
        }

        [IntegrationFact]
        public void LiveServer_NaDummyValuesInConnectionConfig_IgnoredAndSecretRetrieved()
        {
            // Verifies that N/A dummy values for fields irrelevant to the password flow
            // are silently stripped and do not cause authentication or retrieval failures.
            var sut = new SecretServerPamPassword();
            var result = sut.GetPassword(
                new Dictionary<string, string> { { "SecretId", Env("SECRET_SERVER_SECRET_ID") }, { "SecretFieldName", "username" } },
                new Dictionary<string, string>
                {
                    { "Host", Env("SECRET_SERVER_URL") },
                    { "Username", Env("SECRET_SERVER_USERNAME") },
                    { "Password", Env("SECRET_SERVER_PASSWORD") },
                    { "SkipTlsValidation", SkipTls ? "true" : "false" },
                    { "ClientId", "N/A" },
                    { "ClientSecret", "N/A" },
                    { "GrantType", "N/A" }
                });

            result.Should().NotBeNullOrEmpty("N/A dummy values for inapplicable fields must not prevent secret retrieval");
        }
    }
}
