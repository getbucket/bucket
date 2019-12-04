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

using Bucket.IO;
using GameBox.Console.Helper;
using System;

namespace Bucket.Downloader.Transport
{
    /// <summary>
    /// Default progress reporter.
    /// </summary>
    public class ReportDownloadProgress : IProgress<ProgressChanged>
    {
        private readonly IIO io;
        private readonly string prompt;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReportDownloadProgress"/> class.
        /// </summary>
        public ReportDownloadProgress(IIO io, string prompt = null)
        {
            this.io = io;
            this.prompt = prompt;
        }

        public virtual void Report(ProgressChanged value)
        {
            if (value.IsUnknowSize)
            {
                io?.OverwriteError($"{prompt}Downloading ({AbstractHelper.FormatMemory(value.ReceivedSize)})", false);
            }
            else
            {
                io?.OverwriteError($"{prompt}Downloading (<comment>{value}%</comment>)", false);
            }
        }
    }
}
