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

namespace Bucket.Package.Loader
{
    /// <summary>
    /// Extension function for <see cref="ILoaderPackage"/>.
    /// </summary>
    public static class ExtensionILoaderPackage
    {
        /// <summary>
        /// Converts a package from <see cref="ConfigBucket"/> instance.
        /// </summary>
        /// <param name="loader">The loader instance.</param>
        /// <param name="config">The config bucket instance.</param>
        /// <returns>Returns package instance.</returns>
        public static IPackageComplete Load(this ILoaderPackage loader, ConfigBucketBase config)
        {
            return (IPackageComplete)loader.Load(config, typeof(IPackageComplete));
        }

        /// <summary>
        /// Converts a package from <see cref="ConfigBucket"/> instance.
        /// </summary>
        /// <typeparam name="T">The type which package will loaded.</typeparam>
        /// <param name="loader">The loader instance.</param>
        /// <param name="config">The config bucket instance.</param>
        /// <returns>Returns package instance.</returns>
        public static T Load<T>(this ILoaderPackage loader, ConfigBucketBase config)
            where T : class, IPackage
        {
            return (T)loader.Load(config, typeof(T));
        }
    }
}
