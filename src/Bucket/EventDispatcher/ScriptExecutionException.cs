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

using Bucket.Exception;

namespace Bucket.EventDispatcher
{
    /// <summary>
    /// Indicates that the script execution exception.
    /// </summary>
    public class ScriptExecutionException : RuntimeException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptExecutionException"/> class.
        /// </summary>
        /// <param name="message">The exception message as a single string.</param>
        /// <param name="exitCode">The exit code.</param>
        public ScriptExecutionException(string message, int exitCode)
            : base(message, exitCode)
        {
        }
    }
}
