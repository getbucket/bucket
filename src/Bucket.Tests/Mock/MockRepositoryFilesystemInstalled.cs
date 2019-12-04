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
using Bucket.Repository;

namespace Bucket.Tests.Mock
{
    public class MockRepositoryFilesystemInstalled : RepositoryFileSystemInstalled
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MockRepositoryFilesystemInstalled"/> class.
        /// </summary>
        /// <param name="file">The repository json file.</param>
        public MockRepositoryFilesystemInstalled(JsonFile file)
            : base(file)
        {
        }

        public override void Write()
        {
            // noop.
        }

        public override void Reload()
        {
            // noop.
        }
    }
}
