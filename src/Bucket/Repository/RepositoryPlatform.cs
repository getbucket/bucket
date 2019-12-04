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

using Bucket.Package;
using Bucket.Plugin;
using System.Collections.Generic;
using BVersionParser = Bucket.Package.Version.VersionParser;

namespace Bucket.Repository
{
    /// <summary>
    /// Represents a platform repository.
    /// </summary>
    public class RepositoryPlatform : RepositoryArray
    {
        /// <summary>
        /// A patten is used to determine if the package name is a platform.
        /// </summary>
        public static readonly string RegexPlatform = $"^(?:{PluginManager.PluginRequire})$";

        private readonly IDictionary<string, string> platforms;
        private BVersionParser versionParser;

        /// <summary>
        /// Initializes a new instance of the <see cref="RepositoryPlatform"/> class.
        /// </summary>
        public RepositoryPlatform(IDictionary<string, string> platforms = null)
        {
            this.platforms = platforms ?? new Dictionary<string, string>();
        }

        /// <inheritdoc />
        protected override void Initialize()
        {
            versionParser = new BVersionParser();
            AddPackage(CreatePluginApiPackage());

            foreach (var platform in platforms)
            {
                var version = versionParser.Normalize(platform.Value);
                var package = new PackageComplete(platform.Key, version, platform.Value);
                package.SetDescription("Package manually configured.");
                AddPackage(package);
            }
        }

        /// <summary>
        /// Create a plugin package api object that is currently supported by Bucket.
        /// </summary>
        protected virtual IPackage CreatePluginApiPackage()
        {
            var versionPretty = PluginConst.PluginApiVersion;
            var version = versionParser.Normalize(versionPretty);
            var bucketPluginApi = new PackageComplete(PluginManager.PluginRequire, version, versionPretty);
            bucketPluginApi.SetDescription("The Bucket Plugin API");
            return bucketPluginApi;
        }
    }
}
