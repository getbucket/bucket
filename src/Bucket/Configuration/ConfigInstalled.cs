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

#pragma warning disable CA1819

namespace Bucket.Configuration
{
    /// <summary>
    /// Represents an bucket.lock file.
    /// </summary>
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ConfigInstalled
    {
        /// <summary>
        /// Gets or sets an array of installed packages.
        /// </summary>
        [JsonProperty("packages", Order = 0)]
        public ConfigInstalledPackage[] Packages { get; set; } = null;
    }
}
