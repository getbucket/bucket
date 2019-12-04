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
#pragma warning disable CA1819

using Newtonsoft.Json;

namespace Bucket.Configuration
{
    /// <summary>
    /// Represents a package repository configuration.
    /// </summary>
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ConfigRepositoryPackage : ConfigRepository
    {
        /// <summary>
        /// Gets or sets a value indicating whether is secure http request.
        /// </summary>
        [JsonProperty("package", Required = Required.Always, Order = 10)]
        public ConfigPackageInline[] Packages { get; set; }
    }
}
