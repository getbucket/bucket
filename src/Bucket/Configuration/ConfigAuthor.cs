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
    /// Represents an author information.
    /// </summary>
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public sealed class ConfigAuthor
    {
        /// <summary>
        /// Gets or sets the author name.
        /// </summary>
        [JsonProperty("name", Required = Required.Always, Order = 0)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the author email.
        /// </summary>
        [JsonProperty("email", Order = 5)]
        public string Email { get; set; } = null;

        /// <summary>
        /// Gets or sets the author homepage.
        /// </summary>
        [JsonProperty("homepage", Order = 10)]
        public string Homepage { get; set; } = null;

        /// <summary>
        /// Gets or sets the author's role in the project.
        /// </summary>
        [JsonProperty("role", Order = 15)]
        public string Role { get; set; } = null;

        public static implicit operator string(ConfigAuthor bucket)
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
