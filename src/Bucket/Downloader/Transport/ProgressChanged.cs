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

namespace Bucket.Downloader.Transport
{
    /// <summary>
    /// Indicates data transmission progress.
    /// </summary>
#pragma warning disable CA1815
    public struct ProgressChanged
#pragma warning restore CA1815
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressChanged"/> struct.
        /// </summary>
        public ProgressChanged(long totalSize, long receivdeSize)
        {
            TotalSize = totalSize;
            ReceivedSize = receivdeSize;
        }

        /// <summary>
        /// Gets or sets the content total size.
        /// </summary>
        public long TotalSize { get; set; }

        /// <summary>
        /// Gets or sets the received size.
        /// </summary>
        public long ReceivedSize { get; set; }

        /// <summary>
        /// Gets a value indicating whether is unknow total size.
        /// </summary>
        public bool IsUnknowSize => TotalSize <= 0;

#pragma warning disable CA2225
        public static implicit operator float(ProgressChanged progress)
#pragma warning restore CA2225
        {
            if (progress.IsUnknowSize)
            {
                return 1;
            }

            return (float)((double)progress.ReceivedSize / progress.TotalSize);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Math.Min(100, this * 100).ToString("f2")}";
        }
    }
}
