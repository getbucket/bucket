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

using Bucket.Configuration;
using Bucket.Exception;
using System;

namespace Bucket.Package.Loader
{
    /// <summary>
    /// Indicates an invalid package exception.
    /// </summary>
    public sealed class InvalidPackageException : RuntimeException
    {
        private readonly string[] errors;
        private readonly string[] warnings;
        private readonly ConfigBucketBase config;

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidPackageException"/> class.
        /// </summary>
        /// <param name="errors">An array of the errors.</param>
        /// <param name="warnings">An array of the warnings.</param>
        /// <param name="config">The bucket config instance.</param>
        public InvalidPackageException(string[] errors, string[] warnings, ConfigBucketBase config)
            : base()
        {
            this.errors = errors;
            this.warnings = warnings;
            this.config = config;
        }

        /// <summary>
        /// Returns an array of the errors.
        /// </summary>
        public string[] GetErrors()
        {
            return errors ?? Array.Empty<string>();
        }

        /// <summary>
        /// Returns an array of the warnings.
        /// </summary>
        public string[] GetWarnings()
        {
            return warnings ?? Array.Empty<string>();
        }

        /// <summary>
        /// Returns the bucket config instance.
        /// </summary>
        public ConfigBucketBase GetConfig()
        {
            return config;
        }
    }
}
