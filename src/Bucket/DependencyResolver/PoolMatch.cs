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

namespace Bucket.DependencyResolver
{
    internal enum PoolMatch
    {
        /// <summary>
        /// Only matched the name but the version does not match.
        /// </summary>
        Name,

        /// <summary>
        /// Didn't match anything.
        /// </summary>
        None,

        /// <summary>
        /// The name and version matched.
        /// </summary>
        Match,

        /// <summary>
        /// Match by provider.
        /// </summary>
        Provide,

        /// <summary>
        /// Match by replacement.
        /// </summary>
        Replace,

        /// <summary>
        /// Filter matched filter.
        /// </summary>
        Filtered,
    }
}
