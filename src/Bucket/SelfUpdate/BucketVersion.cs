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

namespace Bucket.SelfUpdate
{
    /// <summary>
    /// Bucket release version.
    /// </summary>
    public sealed class BucketVersion
    {
        /// <summary>
        /// Gets or sets the bucket version.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the bucket product version.
        /// </summary>
        public string VersionPretty { get; set; }

        /// <summary>
        /// Gets or sets the commit sha.
        /// </summary>
        public string Sha { get; set; }

        /// <summary>
        /// Gets or sets default downloaded path. null if use default value.
        /// </summary>
        public string Path { get; set; }

#pragma warning disable S1144
        public void Deconstruct(out string version, out string path, out string versionPretty, out string sha)
#pragma warning restore S1144
        {
            version = Version;
            path = Path;
            sha = Sha;
            versionPretty = VersionPretty;
        }
    }
}
