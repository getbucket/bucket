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

namespace Bucket.Repository
{
    /// <summary>
    /// Raised when a repository has a security issue.
    /// </summary>
    public class RepositorySecurityException : BucketException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RepositorySecurityException"/> class.
        /// </summary>
        public RepositorySecurityException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RepositorySecurityException"/> class.
        /// </summary>
        /// <param name="message">The exception message as a single string.</param>
        public RepositorySecurityException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RepositorySecurityException"/> class.
        /// </summary>
        /// <param name="message">The exception message as a single string.</param>
        /// <param name="exitCode">The exit code.</param>
        public RepositorySecurityException(string message, int exitCode)
            : this(message, exitCode, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RepositorySecurityException"/> class.
        /// </summary>
        /// <param name="message">The exception message as a single string.</param>
        /// <param name="innerException">The exception as a inner exception.</param>
        public RepositorySecurityException(string message, System.Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RepositorySecurityException"/> class.
        /// </summary>
        /// <param name="message">The exception message as a single string.</param>
        /// <param name="exitCode">The exit code.</param>
        /// <param name="innerException">The exception as a inner exception.</param>
        public RepositorySecurityException(string message, int exitCode, System.Exception innerException)
            : base(message, exitCode, innerException)
        {
        }
    }
}
