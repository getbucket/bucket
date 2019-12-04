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

namespace Bucket.IO
{
    /// <summary>
    /// Indicates verbosity levels.
    /// </summary>
    [Flags]
    public enum Verbosities
    {
        /// <summary>
        /// Silently output any message.
        /// </summary>
        Quiet = 1,

        /// <summary>
        /// Default output message.
        /// </summary>
        Normal = 2,

        /// <summary>
        /// Detailed output message.
        /// </summary>
        Verbose = 4,

        /// <summary>
        /// Very detailed output message.
        /// </summary>
        VeryVerbose = 8,

        /// <summary>
        /// Debug output message.
        /// </summary>
        Debug = 16,
    }
}
