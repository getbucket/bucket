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

namespace Bucket.Repository
{
    /// <summary>
    /// Represents the type of search when searching in the repository.
    /// </summary>
    public enum SearchMode
    {
        /// <summary>
        /// Search by full-text search.
        /// </summary>
        Fulltext = 0,

        /// <summary>
        /// Search by name.
        /// </summary>
        Name = 1,
    }
}
