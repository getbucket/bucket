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

using Newtonsoft.Json;

namespace Bucket.Configuration
{
    /// <summary>
    /// Represents an package alias.
    /// </summary>
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ConfigAlias
    {
        /// <summary>
        /// Gets or sets a package alias name.
        /// </summary>
        [JsonProperty("alias", Order = 0)]
        public string Alias { get; set; }

        /// <summary>
        /// Gets or sets a package alias.
        /// </summary>
        [JsonProperty("alias-normalized", Order = 5)]
        public string AliasNormalized { get; set; }

        /// <summary>
        /// Gets or sets a package version.
        /// </summary>
        [JsonProperty("version", Order = 10)]
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets a package name(lower name).
        /// </summary>
        [JsonProperty("package", Order = 15)]
        public string Package { get; set; }

        /// <summary>
        /// Find the specified alias, null if not found.
        /// </summary>
        public static ConfigAlias FindAlias(ConfigAlias[] aliases, string packageName, string version)
        {
            foreach (var alias in aliases)
            {
                if (alias.Package == packageName && alias.Version == version)
                {
                    return alias;
                }
            }

            return null;
        }
    }
}
