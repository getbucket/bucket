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

using System.Runtime.Serialization;

namespace Bucket.Exception
{
    /// <summary>
    /// Indicates that the exception is not within the expected code execution.
    /// </summary>
    public class UnexpectedException : BucketException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnexpectedException"/> class.
        /// </summary>
        public UnexpectedException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnexpectedException"/> class.
        /// </summary>
        /// <param name="message">The exception message as a single string.</param>
        public UnexpectedException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnexpectedException"/> class.
        /// </summary>
        /// <param name="message">The exception message as a single string.</param>
        /// <param name="exitCode">The exit code.</param>
        public UnexpectedException(string message, int exitCode)
            : this(message, exitCode, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnexpectedException"/> class.
        /// </summary>
        /// <param name="message">The exception message as a single string.</param>
        /// <param name="innerException">The exception as a inner exception.</param>
        public UnexpectedException(string message, System.Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnexpectedException"/> class.
        /// </summary>
        /// <param name="message">The exception message as a single string.</param>
        /// <param name="exitCode">The exit code.</param>
        /// <param name="innerException">The exception as a inner exception.</param>
        public UnexpectedException(string message, int exitCode, System.Exception innerException)
            : base(message, exitCode, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnexpectedException"/> class.
        /// </summary>
        /// <param name="info">
        /// The System.Runtime.Serialization.SerializationInfo that holds the serialized
        /// object data about the exception being thrown.
        /// </param>
        /// <param name="context">
        /// The System.Runtime.Serialization.StreamingContext that contains contextual
        /// information about the source or destination.
        /// </param>
        protected UnexpectedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
