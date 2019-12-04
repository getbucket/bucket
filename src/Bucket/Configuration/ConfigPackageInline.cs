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
    /// Represents an inline package.
    /// </summary>
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ConfigPackageInline : ConfigBucketBase
    {
        /// <inheritdoc />
        public override bool ShouldDeserializeSource()
        {
            return true;
        }

        /// <inheritdoc />
        public override bool ShouldDeserializeDist()
        {
            return true;
        }

        /// <inheritdoc />
        public override bool ShouldSerializeSource()
        {
            return true;
        }

        /// <inheritdoc />
        public override bool ShouldSerializeDist()
        {
            return true;
        }
    }
}
