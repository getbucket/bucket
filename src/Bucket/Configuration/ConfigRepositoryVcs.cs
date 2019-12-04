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
    /// Represents a vcs repository configuration.
    /// </summary>
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ConfigRepositoryVcs : ConfigRepository
    {
        /// <summary>
        /// Gets or sets the repository uri.
        /// </summary>
        /// <remarks>Dont use <see cref="System.Uri"/>, because maybe ssh.</remarks>
        [JsonProperty("url", Required = Required.Always, Order = 10)]
        public string Uri { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether is secure http request.
        /// </summary>
        [JsonProperty("secure-http", Order = 15)]
        public bool SecureHttp { get; set; } = true;
    }
}
