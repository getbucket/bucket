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

using Newtonsoft.Json;

namespace Bucket.Configuration
{
    /// <summary>
    /// Represents a dist or source resource configuration.
    /// </summary>
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ConfigResource
    {
        /// <summary>
        /// Gets or sets file type (zip|tar|vcs).
        /// </summary>
        [JsonProperty("type", Required = Required.Always, Order = 0)]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets file download uri.
        /// </summary>
        [JsonProperty("url", Required = Required.Always, Order = 5)]
        public string Uri { get; set; }

        /// <summary>
        /// Gets or sets an identifier tag.
        /// </summary>
        [JsonProperty("reference", Order = 10)]
        public string Reference { get; set; } = null;

        /// <summary>
        /// Gets or sets the dist check shasum.
        /// </summary>
        [JsonProperty("shasum", Order = 15)]
        public string Shasum { get; set; } = null;

        /// <summary>
        /// Gets or sets the mirrors.
        /// </summary>
        [JsonProperty("mirrors", Order = 20)]
        public string[] Mirrors { get; set; } = null;

        public static implicit operator string(ConfigResource bucket)
        {
            return bucket.ToString();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}
