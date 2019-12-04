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

using System.Collections.Generic;

namespace Bucket.Logger
{
    /// <summary>
    /// ILogger is the interface implemented by all log output or error output classes.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Logs with an arbitrary level.
        /// </summary>
        /// <param name="level">The arbitrary level.</param>
        /// <param name="message">The log message.</param>
        /// <param name="context">The placehold dictionary.</param>
        void Log(LogLevel level, string message, IDictionary<string, object> context);

        /// <summary>
        /// Logs with an arbitrary level.
        /// </summary>
        /// <param name="level">The arbitrary level.</param>
        /// <param name="message">The log message.</param>
        /// <param name="context">The placehold array.</param>
        void Log(LogLevel level, string message, params object[] context);

        /// <summary>
        /// Detailed debug information.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="context">The placehold dictionary data.</param>
        void Debug(string message, IDictionary<string, object> context);

        /// <summary>
        /// Detailed debug information.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="context">The placehold array data.</param>
        void Debug(string message, params object[] context);

        /// <summary>
        /// Interesting events.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="context">The placehold dictionary data.</param>
        void Info(string message, IDictionary<string, object> context);

        /// <summary>
        /// Interesting events.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="context">The placehold array data.</param>
        void Info(string message, params object[] context);

        /// <summary>
        /// Normal but significant events.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="context">The placehold dictionary data.</param>
        void Notice(string message, IDictionary<string, object> context);

        /// <summary>
        /// Normal but significant events.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="context">The placehold array data.</param>
        void Notice(string message, params object[] context);

        /// <summary>
        /// Exceptional occurrences that are not errors.
        /// </summary>
        /// <remarks>
        /// Use of deprecated APIs, poor use of an API, undesirable things
        /// that are not necessarily wrong.
        /// </remarks>
        /// <param name="message">The log message.</param>
        /// <param name="context">The placehold dictionary data.</param>
        void Warning(string message, IDictionary<string, object> context);

        /// <summary>
        /// Exceptional occurrences that are not errors.
        /// </summary>
        /// <remarks>
        /// Use of deprecated APIs, poor use of an API, undesirable things
        /// that are not necessarily wrong.
        /// </remarks>
        /// <param name="message">The log message.</param>
        /// <param name="context">The placehold array data.</param>
        void Warning(string message, params object[] context);

#pragma warning disable CA1716

        /// <summary>
        /// Runtime errors that do not require immediate action but should typically
        /// be logged and monitored.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="context">The placehold dictionary data.</param>
        void Error(string message, IDictionary<string, object> context);

        /// <summary>
        /// Runtime errors that do not require immediate action but should typically
        /// be logged and monitored.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="context">The placehold array data.</param>
        void Error(string message, params object[] context);
#pragma warning restore CA1716

        /// <summary>
        /// Critical conditions.
        /// </summary>
        /// <remarks>Example: Application component unavailable, unexpected exception.</remarks>
        /// <param name="message">The log message.</param>
        /// <param name="context">The placehold dictionary data.</param>
        void Critical(string message, IDictionary<string, object> context);

        /// <summary>
        /// Critical conditions.
        /// </summary>
        /// <remarks>Example: Application component unavailable, unexpected exception.</remarks>
        /// <param name="message">The log message.</param>
        /// <param name="context">The placehold array data.</param>
        void Critical(string message, params object[] context);

        /// <summary>
        /// Action must be taken immediately.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="context">The placehold dictionary data.</param>
        void Alert(string message, IDictionary<string, object> context);

        /// <summary>
        /// Action must be taken immediately.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="context">placehold array data.</param>
        void Alert(string message, params object[] context);

        /// <summary>
        /// System is unusable.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="context">The placehold dictionary data.</param>
        void Emergency(string message, IDictionary<string, object> context);

        /// <summary>
        /// System is unusable.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="context">The placehold array data.</param>
        void Emergency(string message, params object[] context);
    }
}
