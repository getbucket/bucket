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
using Bucket.EventDispatcher;

namespace Bucket.Plugin
{
    /// <summary>
    /// The pre file download event.
    /// </summary>
    public class PreFileDownloadEventArgs : BucketEventArgs
    {
        private readonly ITransport transport;

        /// <summary>
        /// Initializes a new instance of the <see cref="PreFileDownloadEventArgs"/> class.
        /// </summary>
        public PreFileDownloadEventArgs(string eventName, ITransport transport, string processedUri)
            : base(eventName)
        {
            this.transport = transport;
            ProcessedUri = processedUri;
        }

        /// <summary>
        /// Gets a value indicate the processed uri.
        /// </summary>
        public string ProcessedUri { get; }

        /// <summary>
        /// Returns the http helper instance.
        /// </summary>
        public ITransport GetTransport() => transport;
    }
}
