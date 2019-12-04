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

namespace Bucket.Configuration
{
    /// <summary>
    /// Represents a configuration file for bucket.lock.
    /// </summary>
    public class ConfigInstalledPackage : ConfigBucketBase
    {
        /// <inheritdoc />
        public override bool ShouldDeserializeVersionNormalized()
        {
            return true;
        }

        /// <inheritdoc />
        public override bool ShouldSerializeVersionNormalized()
        {
            return true;
        }

        /// <inheritdoc />
        public override bool ShouldDeserializeInstallationSource()
        {
            return true;
        }

        /// <inheritdoc />
        public override bool ShouldSerializeInstallationSource()
        {
            return true;
        }

        /// <inheritdoc />
        public override bool ShouldDeserializeDist()
        {
            return true;
        }

        /// <inheritdoc />
        public override bool ShouldSerializeDist()
        {
            return true;
        }

        /// <inheritdoc />
        public override bool ShouldDeserializeSource()
        {
            return true;
        }

        /// <inheritdoc />
        public override bool ShouldSerializeSource()
        {
            return true;
        }
    }
}
