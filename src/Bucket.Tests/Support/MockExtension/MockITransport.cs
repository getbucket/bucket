/*
 * This file is part of the Bucket package.
 *
 * (c) Yu Meng Han <menghanyu1994@gmail.com>
 *
 * For the full copyright and license information, please view the LICENSE
 * file that was distributed with this source code.
 *
 * Document: https://github.com/getbucket/bucket/wiki
 */

using Bucket.Downloader.Transport;
using Moq;
using Moq.Language.Flow;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;

namespace Bucket.Tests.Support.MockExtension
{
    public static class MockITransport
    {
        private delegate string GetString(string uri, out HttpHeaders httpResponseHeaders, IReadOnlyDictionary<string, object> options);

        public static IReturnsResult<ITransport> SetupGetString(this Mock<ITransport> transport, string expectedUri, HttpHeaders expectedHeaders, Func<string> returnValue = null)
        {
            return transport.Setup((o) =>
                o.GetString(It.IsIn(expectedUri), out It.Ref<HttpHeaders>.IsAny, It.IsAny<IReadOnlyDictionary<string, object>>()))
                .Returns(new GetString((string uri, out HttpHeaders httpResponseHeaders, IReadOnlyDictionary<string, object> options) =>
                {
                    httpResponseHeaders = expectedHeaders;
                    return returnValue?.Invoke() ?? string.Empty;
                }));
        }

        public static ISetup<ITransport, string> SetupGetString(this Mock<ITransport> transport, string expectedUri)
        {
            return transport.Setup((o) =>
                o.GetString(It.IsIn(expectedUri), out It.Ref<HttpHeaders>.IsAny, It.IsAny<IReadOnlyDictionary<string, object>>()));
        }
    }
}
