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

using System;
using System.Collections.Generic;
using System.Net.Http.Headers;

namespace Bucket.Downloader.Transport
{
    /// <summary>
    /// Provide a higher level file transfer interface.
    /// </summary>
    public interface ITransport
    {
        /// <summary>
        /// Get the content from uri.
        /// </summary>
        /// <param name="uri">The remote file uri.</param>
        /// <param name="httpResponseHeaders">The http response headers.</param>
        /// <param name="additionalOptions">Additional options are used to specify additional parameters in the transmission.</param>
        /// <returns>The content of the remote file.</returns>
        /// <exception cref="TransportException">When the transport has an error.</exception>
        string GetString(string uri, out HttpHeaders httpResponseHeaders, IReadOnlyDictionary<string, object> additionalOptions = null);

        /// <summary>
        /// Copy the remote file to local file.
        /// </summary>
        /// <param name="uri">The remote file uri.</param>
        /// <param name="target">The local saved path.</param>
        /// <param name="progress">Report copy progress.</param>
        /// <param name="additionalOptions">Additional options are used to specify additional parameters in the transmission.</param>
        /// <exception cref="TransportException">When the transport has an error.</exception>
        void Copy(string uri, string target, IProgress<ProgressChanged> progress = null, IReadOnlyDictionary<string, object> additionalOptions = null);
    }
}
