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

using System.Collections.Generic;

namespace Bucket.Util
{
    internal static class ExtensionICollection
    {
        public static bool Empty<T>(this ICollection<T> collection)
        {
            return collection == null || collection.Count <= 0;
        }
    }
}
