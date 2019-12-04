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
using Bucket.IO;
using Bucket.IO.Loader;
using Bucket.Semver;
using GameBox.Console.EventDispatcher;
using GameBox.Console.Exception;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bucket.Repository
{
    /// <summary>
    /// Represents build factory.
    /// </summary>
    public static class RepositoryFactory
    {
        /// <summary>
        /// Create a repository manager.
        /// </summary>
        public static RepositoryManager CreateManager(IIO io, Config config, IEventDispatcher eventDispatcher = null, IVersionParser versionParser = null)
        {
            var manager = new RepositoryManager(io, config, eventDispatcher, versionParser);

            Array.ForEach(
                new[] { "git", "vcs", "github", "gitlab" },
                (type) => manager.RegisterRepository(type, CreationVcs));

            manager.RegisterRepository("bucket", CreationBucket);
            manager.RegisterRepository("package", CreationPackage);

            return manager;
        }

        /// <summary>
        /// Create a default repositories instance from config.
        /// </summary>
        public static IRepository[] CreateDefaultRepository(IIO io = null, Config config = null, RepositoryManager manager = null)
        {
            if (!config)
            {
                config = new Config();
            }

            if (io != null)
            {
                new LoaderIOConfiguration(io).Load(config);
            }

            if (!manager)
            {
                if (io == null)
                {
                    throw new InvalidArgumentException($"This function requires either an {nameof(IIO)} or a {nameof(RepositoryManager)}");
                }

                manager = CreateManager(io, config);
            }

            return CreateRepositories(manager, config.GetRepositories());
        }

        private static IRepository CreationVcs(ConfigRepository configRepository, IIO io, Config config, IEventDispatcher eventDispatcher, IVersionParser versionParser)
        {
            if (!(configRepository is ConfigRepositoryVcs configRepositoryVcs))
            {
                throw new InvalidArgumentException($"The repository configuration parameter is invalid and must be of type {nameof(ConfigRepositoryVcs)}");
            }

            return new RepositoryVcs(configRepositoryVcs, io, config, versionParser: versionParser);
        }

        private static IRepository CreationBucket(ConfigRepository configRepository, IIO io, Config config, IEventDispatcher eventDispatcher, IVersionParser versionParser)
        {
            if (!(configRepository is ConfigRepositoryBucket configRepositoryBucket))
            {
                throw new InvalidArgumentException($"The repository configuration parameter is invalid and must be of type {nameof(ConfigRepositoryBucket)}");
            }

            return new RepositoryBucket(configRepositoryBucket, io, config, null, eventDispatcher, versionParser);
        }

        private static IRepository CreationPackage(ConfigRepository configRepository, IIO io, Config config, IEventDispatcher eventDispatcher, IVersionParser versionParser)
        {
            if (!(configRepository is ConfigRepositoryPackage configRepositoryPackage))
            {
                throw new InvalidArgumentException($"The repository configuration parameter is invalid and must be of type {nameof(ConfigRepositoryPackage)}");
            }

            return new RepositoryPackage(configRepositoryPackage);
        }

        private static IRepository[] CreateRepositories(RepositoryManager manager, ConfigRepository[] configRepositories)
        {
            var repositories = new LinkedList<IRepository>();
            foreach (var configRepository in configRepositories)
            {
                repositories.AddLast(manager.CreateRepository(configRepository));
            }

            return repositories.ToArray();
        }
    }
}
