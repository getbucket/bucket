﻿/*
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

namespace Bucket.Repository
{
    /// <summary>
    /// Indicates an invalid repository exception.
    /// </summary>
    public class InvalidRepositoryException : RuntimeException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidRepositoryException"/> class.
        /// </summary>
        public InvalidRepositoryException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidRepositoryException"/> class.
        /// </summary>
        /// <param name="message">The exception message as a single string.</param>
        public InvalidRepositoryException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidRepositoryException"/> class.
        /// </summary>
        /// <param name="message">The exception message as a single string.</param>
        /// <param name="exitCode">The exit code.</param>
        public InvalidRepositoryException(string message, int exitCode)
            : this(message, exitCode, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidRepositoryException"/> class.
        /// </summary>
        /// <param name="message">The exception message as a single string.</param>
        /// <param name="innerException">The exception as a inner exception.</param>
        public InvalidRepositoryException(string message, System.Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidRepositoryException"/> class.
        /// </summary>
        /// <param name="message">The exception message as a single string.</param>
        /// <param name="exitCode">The exit code.</param>
        /// <param name="innerException">The exception as a inner exception.</param>
        public InvalidRepositoryException(string message, int exitCode, System.Exception innerException)
            : base(message, exitCode, innerException)
        {
        }
    }
}
