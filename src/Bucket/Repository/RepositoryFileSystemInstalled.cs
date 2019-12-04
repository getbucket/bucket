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

using Bucket.Json;

namespace Bucket.Repository
{
    /// <summary>
    /// Represents installed filesystem repository.
    /// </summary>
    public class RepositoryFileSystemInstalled : RepositoryFileSystem, IRepositoryInstalled
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RepositoryFileSystemInstalled"/> class.
        /// </summary>
        /// <param name="file">The repository json file.</param>
        public RepositoryFileSystemInstalled(JsonFile file)
            : base(file)
        {
        }

        // no code. reserved class.
    }
}
