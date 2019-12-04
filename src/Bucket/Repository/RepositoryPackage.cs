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

using Bucket.Configuration;
using Bucket.Package.Loader;
using SException = System.Exception;

namespace Bucket.Repository
{
    /// <summary>
    /// A repository implementation that means a simple package.
    /// </summary>
    public class RepositoryPackage : RepositoryArray
    {
        private readonly ConfigRepositoryPackage config;

        /// <summary>
        /// Initializes a new instance of the <see cref="RepositoryPackage"/> class.
        /// </summary>
        public RepositoryPackage(ConfigRepositoryPackage config)
            : base()
        {
            this.config = config;
        }

        /// <inheritdoc />
        protected override void Initialize()
        {
            var loader = new LoaderValidating(new LoaderPackage());
            foreach (var packageConfig in config.Packages)
            {
                try
                {
                    var package = loader.Load(packageConfig);
                    AddPackage(package);
                }
#pragma warning disable CA1031
                catch (SException ex)
#pragma warning restore CA1031
                {
                    throw new InvalidRepositoryException(
                        $"A repository of type \"package\" contains an invalid package definition: {ex.Message}\n\nInvalid package definition:\n{packageConfig}", ex);
                }
            }
        }
    }
}
