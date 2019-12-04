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

namespace Bucket.Downloader.Transport
{
    /// <summary>
    /// Extension method for <see cref="ITransport"/>.
    /// </summary>
    public static class ExtensionITransport
    {
        /// <summary>
        /// Get the content from uri.
        /// </summary>
        /// <param name="transport">The transport instance.</param>
        /// <param name="uri">The remote file uri.</param>
        /// <returns>The content of the remote file.</returns>
        /// <exception cref="TransportException">When the transport has an error.</exception>
        public static string GetString(this ITransport transport, string uri)
        {
            return transport.GetString(uri, out _);
        }
    }
}
