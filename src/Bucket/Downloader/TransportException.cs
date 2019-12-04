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

using Bucket.Exception;
using System.Net;
using System.Net.Http.Headers;

namespace Bucket.Downloader
{
    /// <summary>
    /// Indicates that a data transfer is abnormal.
    /// </summary>
    public class TransportException : RuntimeException
    {
        private readonly HttpHeaders headers;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransportException"/> class.
        /// </summary>
        public TransportException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransportException"/> class.
        /// </summary>
        /// <param name="message">The exception message as a single string.</param>
        public TransportException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransportException"/> class.
        /// </summary>
        /// <param name="message">The exception message as a single string.</param>
        /// <param name="innerException">The exception as a inner exception.</param>
        public TransportException(string message, System.Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransportException"/> class.
        /// </summary>
        /// <param name="message">The exception message as a single string.</param>
        /// <param name="httpStatusCode">The http status code.</param>
        /// <param name="innerException">The exception as a inner exception.</param>
        public TransportException(string message, HttpStatusCode httpStatusCode, System.Exception innerException)
            : this(message, httpStatusCode, null, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransportException"/> class.
        /// </summary>
        /// <param name="message">The exception message as a single string.</param>
        /// <param name="httpStatusCode">The http status code.</param>
        /// <param name="headers">The http response headers.</param>
        /// <param name="innerException">The exception as a inner exception.</param>
        public TransportException(string message, HttpStatusCode httpStatusCode, HttpHeaders headers, System.Exception innerException)
            : base(message, innerException)
        {
            HttpStatusCode = httpStatusCode;
            this.headers = headers;
        }

        /// <summary>
        /// Gets the error status code.
        /// </summary>
        public HttpStatusCode HttpStatusCode { get; } = 0;

        /// <summary>
        /// Gets the http response headers.
        /// </summary>
        public HttpHeaders GetHeaders() => headers;
    }
}
