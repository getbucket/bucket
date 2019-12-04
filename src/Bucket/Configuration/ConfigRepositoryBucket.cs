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
    /// Represents a bucket repository configuration.
    /// </summary>
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ConfigRepositoryBucket : ConfigRepository
    {
        /// <summary>
        /// Gets or sets the repository uri.
        /// </summary>
        /// <remarks>Dont use <see cref="System.Uri"/>, because maybe ssh.</remarks>
        [JsonProperty("url", Required = Required.Always, Order = 10)]
        public string Uri { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether whether is allow ssl download.
        /// </summary>
        [JsonProperty("allow-ssl-downgrade", Order = 20)]
        public bool AllowSSLDowngrade { get; set; } = false;
    }
}
