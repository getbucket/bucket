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

namespace Bucket.Package
{
    /// <summary>
    /// Indicates the type of package.
    /// </summary>
    public static class PackageType
    {
        /// <summary>
        /// Representation code base package.
        /// </summary>
        public const string Library = "library";

        /// <summary>
        /// Indicates the plugin package.
        /// </summary>
        public const string Plugin = "plugin";

        /// <summary>
        /// Indicates the project.
        /// </summary>
        public const string Project = "project";
    }
}
