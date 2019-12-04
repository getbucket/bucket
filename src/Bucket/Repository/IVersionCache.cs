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

namespace Bucket.Repository
{
    /// <summary>
    /// Represents a version cache object.
    /// </summary>
    public interface IVersionCache
    {
        /// <summary>
        /// Get the config from version cache.
        /// </summary>
        /// <param name="version">The package version.</param>
        /// <param name="identifier">Any identifier to a specific branch/tag/commit.</param>
        /// <param name="skipped">Whether is skip this package.</param>
        /// <returns>Returns object Containing all infos from the bucket.json file.</returns>
        ConfigBucket GetVersionPackage(string version, string identifier, out bool skipped);
    }
}
