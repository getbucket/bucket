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
using System.Linq;
using System.Net.Http.Headers;

namespace Bucket.Util
{
    internal static class ExtensionHttpHeaders
    {
        /// <summary>
        /// Return if a specified header and specified value are stored in the collection.
        /// </summary>
        public static bool TryGetValue(this HttpHeaders headers, string header, out string value)
        {
            if (!headers.TryGetValues(header, out IEnumerable<string> values))
            {
                value = default;
                return false;
            }

            value = values.FirstOrDefault();
            return true;
        }
    }
}
