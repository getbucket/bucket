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

using System.Runtime.Serialization;

namespace Bucket.SelfUpdate
{
    /// <summary>
    /// Update channel.
    /// </summary>
    public enum Channel
    {
        /// <summary>
        /// A download channel representing the stable version.
        /// </summary>
        [EnumMember(Value = "stable")]
        Stable,

        /// <summary>
        /// A download channel representing the preview version.
        /// </summary>
        [EnumMember(Value = "preview")]
        Preview,

        /// <summary>
        /// A download channel representing the developer(ci snapshot) version.
        /// </summary>
        [EnumMember(Value = "dev")]
        Dev,
    }
}
