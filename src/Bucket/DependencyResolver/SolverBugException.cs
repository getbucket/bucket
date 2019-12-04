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

using Bucket.Console;
using Bucket.Exception;

namespace Bucket.DependencyResolver
{
    /// <summary>
    /// Indicates a resolver exception.
    /// </summary>
    internal sealed class SolverBugException : RuntimeException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SolverBugException"/> class.
        /// </summary>
        public SolverBugException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SolverBugException"/> class.
        /// </summary>
        /// <param name="message">The exception message as a single string.</param>
        public SolverBugException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SolverBugException"/> class.
        /// </summary>
        /// <param name="message">The exception message as a single string.</param>
        /// <param name="exitCode">The exit code.</param>
        public SolverBugException(string message, int exitCode)
            : this(message, exitCode, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SolverBugException"/> class.
        /// </summary>
        /// <param name="message">The exception message as a single string.</param>
        /// <param name="innerException">The exception as a inner exception.</param>
        public SolverBugException(string message, System.Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SolverBugException"/> class.
        /// </summary>
        /// <param name="message">The exception message as a single string.</param>
        /// <param name="exitCode">The exit code.</param>
        /// <param name="innerException">The exception as a inner exception.</param>
        public SolverBugException(string message, int exitCode, System.Exception innerException)
            : base(message, exitCode, innerException)
        {
        }

        /// <inheritdoc />
        public override int ExitCode => ExitCodes.DependencySolvingException;
    }
}
