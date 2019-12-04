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

using System.IO;
using System.Text;

namespace Bucket.Util
{
    /// <summary>
    /// <see cref="string"/> extension function.
    /// </summary>
    public static class ExtensionString
    {
        /// <summary>
        /// Convert the specified string to a stream.
        /// </summary>
        /// <param name="str">The specified string.</param>
        /// <param name="encoding">The string encoding.</param>
        /// <returns>The stream instance.</returns>
        public static Stream ToStream(this string str, Encoding encoding = null)
        {
            return new MemoryStream((encoding ?? Encoding.Default).GetBytes(str));
        }
    }
}
