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

namespace Bucket.Util
{
    internal static class ExtensionArray
    {
        public static T[] ZeroAsNull<T>(this T[] element)
        {
            return element == null || element.Length <= 0 ? null : element;
        }
    }
}
