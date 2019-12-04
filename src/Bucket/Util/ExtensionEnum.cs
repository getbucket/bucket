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

using System;

namespace Bucket.Util
{
    /// <summary>
    /// Provide some extension functions for <see cref="Enum"/>.
    /// </summary>
    public static class ExtensionEnum
    {
        /// <summary>
        /// Gets the enum attribute.
        /// </summary>
        /// <typeparam name="T">The attribute type.</typeparam>
        /// <returns>null if the attribute not found.</returns>
        public static T GetAttribute<T>(this Enum value)
            where T : Attribute
        {
            var type = value.GetType();
            var memberInfo = type.GetMember(value.ToString());
            var attributes = memberInfo[0].GetCustomAttributes(typeof(T), false);
            return attributes.Length > 0 ? (T)attributes[0] : null;
        }
    }
}
