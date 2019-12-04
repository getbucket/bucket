/*
 * This file is part of the Bucket package.
 *
 * (c) LiuSiJia <394754029@qq.com>
 *
 * For the full copyright and license information, please view the LICENSE
 * file that was distributed with this source code.
 *
 * Document: https://github.com/getbucket/bucket/wiki
 */

namespace Bucket.Logger
{
    /// <summary>
    /// Describes log levels.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Detailed debug information.
        /// </summary>
        Debug,

        /// <summary>
        /// Interesting events.
        /// </summary>
        Info,

        /// <summary>
        /// Normal but significant events.
        /// </summary>
        Notice,

        /// <summary>
        /// Exceptional occurrences that are not errors.
        /// </summary>
        Warning,

        /// <summary>
        /// Runtime errors that do not require immediate action but should typically
        /// be logged and monitored.
        /// </summary>
        Error,

        /// <summary>
        /// Critical conditions
        /// </summary>
        Critical,

        /// <summary>
        /// Action must be taken immediately.
        /// </summary>
        Alert,

        /// <summary>
        /// System is unusable.
        /// </summary>
        Emergency,
    }
}
