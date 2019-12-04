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

using GameBox.Console;
using GameBox.Console.Exception;
using System;
using System.Runtime.Serialization;

namespace Bucket.Exception
{
    /// <summary>
    /// <see cref="BucketException"/> for all bucket exception.
    /// </summary>
    public class BucketException : System.Exception, IException
    {
        /// <summary>
        /// The exit code.
        /// </summary>
        private readonly int exitCode = -1;

        /// <summary>
        /// Initializes a new instance of the <see cref="BucketException"/> class.
        /// </summary>
        public BucketException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BucketException"/> class.
        /// </summary>
        /// <param name="message">The exception message as a single string.</param>
        public BucketException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BucketException"/> class.
        /// </summary>
        /// <param name="message">The exception message as a single string.</param>
        /// <param name="exitCode">The exit code.</param>
        public BucketException(string message, int exitCode)
            : this(message, exitCode, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BucketException"/> class.
        /// </summary>
        /// <param name="message">The exception message as a single string.</param>
        /// <param name="innerException">The exception as a inner exception.</param>
        public BucketException(string message, System.Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BucketException"/> class.
        /// </summary>
        /// <param name="message">The exception message as a single string.</param>
        /// <param name="exitCode">The exit code.</param>
        /// <param name="innerException">The exception as a inner exception.</param>
        public BucketException(string message, int exitCode, System.Exception innerException)
            : base(message, innerException)
        {
            this.exitCode = exitCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BucketException"/> class.
        /// </summary>
        /// <param name="info">
        /// The System.Runtime.Serialization.SerializationInfo that holds the serialized
        /// object data about the exception being thrown.
        /// </param>
        /// <param name="context">
        /// The System.Runtime.Serialization.StreamingContext that contains contextual
        /// information about the source or destination.
        /// </param>
        protected BucketException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Gets the exit code.
        /// </summary>
        public virtual int ExitCode => exitCode < 0 ? ExitCodes.GeneralException : Math.Min(exitCode, 255);
    }
}
