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
using System;

namespace Bucket.Package.Loader
{
    /// <summary>
    /// Represents a package instance loader.
    /// </summary>
    public interface ILoaderPackage
    {
        /// <summary>
        /// Converts a package from <see cref="ConfigBucket"/> instance.
        /// </summary>
        /// <param name="config">The config bucket instance.</param>
        /// <param name="expectedClass">The class of the converted instance.</param>
        IPackage Load(ConfigBucketBase config, Type expectedClass);
    }
}
