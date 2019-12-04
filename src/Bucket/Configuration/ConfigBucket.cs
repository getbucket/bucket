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
    /// Represents a configuration file for Bucket.json.
    /// </summary>
    // https://github.com/getbucket/bucket/wiki/bucket-schema
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ConfigBucket : ConfigBucketBase
    {
#pragma warning disable CA2225
        public static implicit operator string(ConfigBucket bucket)
#pragma warning restore CA2225
        {
            return bucket.ToString();
        }
    }
}
