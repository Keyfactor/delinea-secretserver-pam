// Copyright 2025 Keyfactor
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions
// and limitations under the License.

using System.Net;

namespace Keyfactor.Extensions.Pam.Delinea.Tests.Fakes;

/// <summary>
///     A test-only <see cref="HttpMessageHandler" /> whose behaviour is controlled
///     by a delegate, allowing individual tests to script exactly what the fake
///     HTTP server returns without going to the network.
/// </summary>
public class TestHttpMessageHandler : HttpMessageHandler
{
    public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? HandlerFunc { get; set; }

    public TestHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? handlerFunc = null)
    {
        HandlerFunc = handlerFunc;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
        => HandlerFunc != null
            ? HandlerFunc(request, cancellationToken)
            : Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotImplemented));
}
