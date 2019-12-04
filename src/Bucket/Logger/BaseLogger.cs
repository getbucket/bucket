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
using System.Diagnostics.CodeAnalysis;

namespace Bucket.Logger
{
    /// <summary>
    /// BaseLogger is the abstract class implemented by all log output or error output classes.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public abstract class BaseLogger : ILogger
    {
        /// <inheritdoc />
        public void Debug(string message, IDictionary<string, object> context)
        {
            Log(LogLevel.Debug, message, context);
        }

        /// <inheritdoc />
        public void Debug(string message, params object[] context)
        {
            Log(LogLevel.Debug, message, context);
        }

        /// <inheritdoc />
        public void Info(string message, IDictionary<string, object> context)
        {
            Log(LogLevel.Info, message, context);
        }

        /// <inheritdoc />
        public void Info(string message, params object[] context)
        {
            Log(LogLevel.Info, message, context);
        }

        /// <inheritdoc />
        public void Notice(string message, IDictionary<string, object> context)
        {
            Log(LogLevel.Notice, message, context);
        }

        /// <inheritdoc />
        public void Notice(string message, params object[] context)
        {
            Log(LogLevel.Notice, message, context);
        }

        /// <inheritdoc />
        public void Warning(string message, IDictionary<string, object> context)
        {
            Log(LogLevel.Warning, message, context);
        }

        /// <inheritdoc />
        public void Warning(string message, params object[] context)
        {
            Log(LogLevel.Warning, message, context);
        }

        /// <inheritdoc />
        public void Error(string message, IDictionary<string, object> context)
        {
            Log(LogLevel.Error, message, context);
        }

        /// <inheritdoc />
        public void Error(string message, params object[] context)
        {
            Log(LogLevel.Error, message, context);
        }

        /// <inheritdoc />
        public void Critical(string message, IDictionary<string, object> context)
        {
            Log(LogLevel.Critical, message, context);
        }

        /// <inheritdoc />
        public void Critical(string message, params object[] context)
        {
            Log(LogLevel.Critical, message, context);
        }

        /// <inheritdoc />
        public void Alert(string message, IDictionary<string, object> context)
        {
            Log(LogLevel.Alert, message, context);
        }

        /// <inheritdoc />
        public void Alert(string message, params object[] context)
        {
            Log(LogLevel.Alert, message, context);
        }

        /// <inheritdoc />
        public void Emergency(string message, IDictionary<string, object> context)
        {
            Log(LogLevel.Emergency, message, context);
        }

        /// <inheritdoc />
        public void Emergency(string message, params object[] context)
        {
            Log(LogLevel.Emergency, message, context);
        }

        /// <inheritdoc />
        public void Log(LogLevel level, string message, IDictionary<string, object> context)
        {
            Log(level, Interpolate(message, context));
        }

        /// <inheritdoc />
        public abstract void Log(LogLevel level, string message, params object[] context);

        /// <summary>
        /// Interpolates context values into the message placeholders.
        /// </summary>
        /// <param name="message">The message placeholders.</param>
        /// <param name="context">The interpolates context.</param>
        /// <returns>The interpolated message.</returns>
        protected abstract string Interpolate(string message, IDictionary<string, object> context);
    }
}
