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
using Bucket.Exception;
using Bucket.Json;
using Bucket.Package.Dumper;
using Bucket.Package.Loader;
using System.Collections.Generic;
using SException = System.Exception;

namespace Bucket.Repository
{
    /// <summary>
    /// Represents a file system repository.
    /// </summary>
    public class RepositoryFileSystem : RepositoryArrayWriteable
    {
        private readonly JsonFile file;
        private readonly DumperPackage dumper;
        private readonly ILoaderPackage loader;

        /// <summary>
        /// Initializes a new instance of the <see cref="RepositoryFileSystem"/> class.
        /// </summary>
        /// <param name="file">The repository json file.</param>
        public RepositoryFileSystem(JsonFile file)
        {
            this.file = file;
            dumper = new DumperPackage();
            loader = new LoaderPackage();
        }

        /// <inheritdoc />
        public override void Reload()
        {
            Clear();
            Initialize();
        }

        /// <inheritdoc />
        public override void Write()
        {
            var data = new List<ConfigInstalledPackage>();
            foreach (var package in GetCanonicalPackages())
            {
                data.Add(dumper.Dump<ConfigInstalledPackage>(package));
            }

            data.Sort((x, y) => string.CompareOrdinal(x.Name, y.Name));
            file.Write(new ConfigInstalled() { Packages = data.ToArray() });
        }

        /// <inheritdoc />
        protected override void Initialize()
        {
            base.Initialize();

            if (!file.Exists())
            {
                return;
            }

            ConfigInstalled configInstalled;
            try
            {
                configInstalled = file.Read<ConfigInstalled>();
                if (configInstalled.Packages == null)
                {
                    throw new UnexpectedException("Could not parse package list from the repository.");
                }
            }
#pragma warning disable CA1031
            catch (SException ex)
#pragma warning restore CA1031
            {
                throw new InvalidRepositoryException(
                    $"Invalid repository data in {file.GetPath()}, packages could not be loaded: [{ex}] {ex.Message}");
            }

            foreach (var package in configInstalled.Packages)
            {
                AddPackage(loader.Load(package));
            }
        }
    }
}
