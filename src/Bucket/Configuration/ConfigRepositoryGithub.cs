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
    /// Represents a github repository configuration.
    /// </summary>
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ConfigRepositoryGithub : ConfigRepositoryVcs
    {
        /// <summary>
        /// Gets or sets a value indicating whether is not use the github api.
        /// </summary>
        [JsonProperty("no-api", Order = 100)]
        public bool NoApi { get; set; } = false;
    }
}
