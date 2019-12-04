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

#pragma warning disable CA1819
#pragma warning disable CA2227

using Bucket.Json.Converter;
using Bucket.Semver;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace Bucket.Configuration
{
    /// <summary>
    /// Represents an bucket.lock file.
    /// </summary>
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ConfigLocker
    {
        /// <summary>
        /// Gets or sets a readme for the bucket.lock header.
        /// </summary>
        /// <remarks>Don't use comment because some json libraries may not support comment.</remarks>
        [JsonProperty("_readme", Order = 0)]
        public string Readme { get; set; } = "This file is generated automatically";

        /// <summary>
        /// Gets or sets a content hash(md5).
        /// </summary>
        [JsonProperty("content-hash", Order = 5)]
        public string ContentHash { get; set; } = null;

        /// <summary>
        /// Gets or sets an array of packages.
        /// </summary>
        [JsonProperty("packages", Order = 10)]
        public ConfigLockerPackage[] Packages { get; set; } = null;

        /// <summary>
        /// Gets or sets an array of packages for dev mode.
        /// </summary>
        [JsonProperty("packages-dev", Order = 15)]
        public ConfigLockerPackage[] PackagesDev { get; set; } = null;

        /// <summary>
        /// Gets or sets an array of package aliases.
        /// </summary>
        [JsonProperty("aliases", Order = 20)]
        public ConfigAlias[] Aliases { get; set; } = null;

        /// <summary>
        /// Gets or sets minimun stability.
        /// </summary>
        [JsonProperty("minimum-stability", Order = 25)]
        [JsonConverter(typeof(StringEnumConverter))]
        public Stabilities MinimumStability { get; set; } = Stabilities.Stable;

        /// <summary>
        /// Gets or sets the stability flags relationship of the require(include dev) package.
        /// </summary>
        [JsonProperty("stability-flags", Order = 30)]
        [JsonConverter(typeof(ConverterDictionaryEnumValue<Stabilities>))]
        public IDictionary<string, Stabilities> StabilityFlags { get; set; } = new Dictionary<string, Stabilities>();

        /// <summary>
        /// Gets or sets a value indicating whether is prefer stable.
        /// </summary>
        [JsonProperty("prefer-stable", Order = 35)]
        public bool PreferStable { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether is prefer lowest.
        /// </summary>
        [JsonProperty("prefer-lowest", Order = 40)]
        public bool PreferLowest { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating the manually configured platform package information.
        /// </summary>
        [JsonProperty("platform", Order = 45)]
        public IDictionary<string, string> Platforms { get; set; } = null;
    }
}
