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

namespace Bucket.Plugin
{
    /// <summary>
    /// The plugin events.
    /// </summary>
    public static class PluginEvents
    {
        /// <summary>
        /// The <see cref="Init"/> event occurs after a Bucket instance is done being initialized.
        /// </summary>
        public const string Init = "init";

        /// <summary>
        /// The <see cref="Command"/> event occurs as a command begins.
        /// </summary>
        public const string Command = "command";

        /// <summary>
        /// The <see cref="PreCommandRun"/> event occurs before a command is executed and lets you modify the input arguments/options.
        /// </summary>
        public const string PreCommandRun = "pre-command-run";

        /// <summary>
        /// The <see cref="PreFileDownload"/> event occurs before the file download start, it allows you to manipulate the <see cref="ITransport"/> object.
        /// </summary>
        public const string PreFileDownload = "pre-file-download";
    }
}
