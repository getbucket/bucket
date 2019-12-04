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

#pragma warning disable CA1056

using Newtonsoft.Json;

namespace Bucket.Configuration
{
    /// <summary>
    /// Represents a repository configuration.
    /// </summary>
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ConfigRepository
    {
        /// <summary>
        /// Gets or sets the repository type.
        /// </summary>
        [JsonProperty("name", Order = 0)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the repository type.
        /// </summary>
        [JsonProperty("type", Required = Required.Always, Order = 5)]
        public string Type { get; set; }

        public static implicit operator string(ConfigRepository bucket)
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
