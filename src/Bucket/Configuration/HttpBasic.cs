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
    /// Represents an http basic authorization data.
    /// </summary>
    [JsonObject]
    public class HttpBasic : AuthBase
    {
        /// <summary>
        /// Gets or sets the http basic username.
        /// </summary>
        [JsonProperty("username", Required = Required.Always, Order = 0)]
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the http basic password.
        /// </summary>
        [JsonProperty("password", Required = Required.Always, Order = 0)]
        public string Password { get; set; }
    }
}
