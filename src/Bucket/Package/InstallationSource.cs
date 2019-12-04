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

namespace Bucket.Package
{
    /// <summary>
    /// Source from which this package was installed.
    /// </summary>
    public enum InstallationSource
    {
        /// <summary>
        /// Published source.
        /// </summary>
        [EnumMember(Value = "dist")]
        Dist = 1,

        /// <summary>
        /// Source of source code.
        /// </summary>
        [EnumMember(Value = "source")]
        Source = 2,

        /// <summary>
        /// Automatic decision source.
        /// </summary>
        [EnumMember(Value = "auto")]
        Auto = 16,
    }
}
