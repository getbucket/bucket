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

using Bucket.Configuration;
using Bucket.Downloader;
using Bucket.IO;
using System;
using System.Collections.Generic;

namespace Bucket.Util
{
    /// <summary>
    /// A generic guard representing a configuration class.
    /// </summary>
    internal static class GuardConfig
    {
        private static HashSet<string> warnedHosts;

        /// <summary>
        /// Guard that the passed URL is allowed to be used by current config, or throws an exception.
        /// </summary>
        public static void ProhibitUri(this Guard guard, Config config, string uri, IIO io = null)
        {
            if (string.IsNullOrEmpty(uri))
            {
                return;
            }

            uri = uri.Trim();
            if (uri.StartsWith("git@", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var uriInstance = new Uri(uri);
            var scheme = uriInstance.Scheme;
            if (!Array.Exists(new[] { "http", "git", "ftp", "svn" }, (protocol) => protocol == scheme))
            {
                return;
            }

            if (config.Get(Settings.SecureHttp))
            {
                // todo: exception message link to document.
                throw new TransportException($"Your configuration does not allow connections to {uri}. Authorized by configuration: secure-http.");
            }

            if (io == null)
            {
                return;
            }

            var host = uriInstance.Host;
            if (warnedHosts == null)
            {
                warnedHosts = new HashSet<string>();
            }

            if (warnedHosts.Contains(host))
            {
                return;
            }

            // Warn all unsafe transport protocols.
            io.WriteError($"<warning>Warning: Accessing {host} over {scheme} which is an insecure protocol.</warning>");
            warnedHosts.Add(host);
        }
    }
}
