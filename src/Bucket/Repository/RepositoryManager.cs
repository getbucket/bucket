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
using Bucket.Package;
using Bucket.Semver;
using Bucket.Semver.Constraint;
using GameBox.Console.EventDispatcher;
using GameBox.Console.Exception;
using System.Collections.Generic;
using System.Linq;
using BVersionParser = Bucket.Package.Version.VersionParser;

namespace Bucket.Repository
{
    /// <summary>
    /// Different types of repositories can be created through the manager.
    /// </summary>
    public class RepositoryManager
    {
        private readonly IIO io;
        private readonly Config config;
        private readonly IEventDispatcher eventDispatcher;
        private readonly LinkedList<IRepository> repositories;
        private readonly IVersionParser versionParser;
        private readonly IDictionary<string, RepositoryCreater> repositoryCreaters;
        private IRepositoryInstalled localRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="RepositoryManager"/> class.
        /// </summary>
        public RepositoryManager(IIO io, Config config, IEventDispatcher eventDispatcher = null, IVersionParser versionParser = null)
        {
            this.io = io;
            this.config = config;
            this.eventDispatcher = eventDispatcher;
            this.versionParser = versionParser ?? new BVersionParser();
            repositories = new LinkedList<IRepository>();
            repositoryCreaters = new Dictionary<string, RepositoryCreater>();
        }

        /// <summary>
        /// Represents a repository creater.
        /// </summary>
        /// <param name="configRepository">The repository configuration.</param>
        /// <param name="io">The input/output instance.</param>
        /// <param name="config">The global configuration.</param>
        /// <param name="eventDispatcher">The event dispatcher instance.</param>
        /// <param name="versionParser">The version parser instance.</param>
        /// <returns>Return an repository instance.</returns>
        public delegate IRepository RepositoryCreater(ConfigRepository configRepository, IIO io, Config config, IEventDispatcher eventDispatcher, IVersionParser versionParser);

#pragma warning disable CA2225
        public static implicit operator bool(RepositoryManager manager)
#pragma warning restore CA2225
        {
            return manager != null;
        }

        /// <summary>
        /// Searches for a package by it's name and version in managed repositories.
        /// </summary>
        /// <param name="name">The package name.</param>
        /// <param name="constraint">The package version to match against.</param>
        /// <returns>Return the <see cref="IPackage"/> instance. null if package not found.</returns>
        public virtual IPackage FindPackage(string name, string constraint)
        {
            return FindPackage(name, versionParser.ParseConstraints(constraint));
        }

        /// <summary>
        /// Searches for a package by it's name and version in managed repositories.
        /// </summary>
        /// <param name="name">The package name.</param>
        /// <param name="constraint">The package version constraint to match against.</param>
        /// <returns>Return the <see cref="IPackage"/> instance. null if package not found.</returns>
        public virtual IPackage FindPackage(string name, IConstraint constraint)
        {
            foreach (var repository in repositories)
            {
                var package = repository.FindPackage(name, constraint);
                if (package != null)
                {
                    return package;
                }
            }

            return null;
        }

        /// <summary>
        /// Searches for all packages matching a name and optionally a version in managed repositories.
        /// </summary>
        /// <param name="name">The package name.</param>
        /// <param name="constraint">The package version to match against.</param>
        /// <returns>Returns an array represents all packages matching. Empty array if package not found.</returns>
        public virtual IPackage[] FindPackages(string name, string constraint)
        {
            return FindPackages(name, versionParser.ParseConstraints(constraint));
        }

        /// <summary>
        /// Searches for all packages matching a name and optionally a version in managed repositories.
        /// </summary>
        /// <param name="name">The package name.</param>
        /// <param name="constraint">The package version constraint to match against.</param>
        /// <returns>Returns an array represents all packages matching. Empty array if package not found.</returns>
        public virtual IPackage[] FindPackages(string name, IConstraint constraint)
        {
            var packages = new List<IPackage>();

            foreach (var repository in repositories)
            {
                packages.AddRange(repository.FindPackages(name, constraint));
            }

            return packages.ToArray();
        }

        /// <summary>
        /// Adds repository to manager.
        /// </summary>
        /// <param name="repository">The repository will added.</param>
        /// <param name="prepend">Whether is add the repository at link list first.</param>
        public virtual void AddRepository(IRepository repository, bool prepend = false)
        {
            if (prepend)
            {
                repositories.AddFirst(repository);
            }
            else
            {
                repositories.AddLast(repository);
            }
        }

        /// <summary>
        /// Gets all repositories, except local one.
        /// </summary>
        public virtual IRepository[] GetRepositories()
        {
            return repositories.ToArray();
        }

        /// <summary>
        /// Register repository creater for a specific repository type.
        /// </summary>
        /// <param name="type">The specifice repository type.</param>
        /// <param name="creater">The repository creater.</param>
        public virtual void RegisterRepository(string type, RepositoryCreater creater)
        {
            repositoryCreaters[type] = creater;
        }

        /// <summary>
        /// Create a new repository for a specific repository type.
        /// </summary>
        /// <param name="configRepository">The global configuration.</param>
        /// <returns>Retruns a new repository instance.</returns>
        public virtual IRepository CreateRepository(ConfigRepository configRepository)
        {
            if (!repositoryCreaters.TryGetValue(configRepository.Type, out RepositoryCreater creater))
            {
                throw new InvalidArgumentException($"Repository type is not registered: {configRepository.Type} {configRepository.Name ?? string.Empty}");
            }

            return creater(configRepository, io, config, eventDispatcher, versionParser);
        }

        /// <summary>
        /// Sets local installed repository for the project.
        /// </summary>
        public virtual void SetLocalInstalledRepository(IRepositoryInstalled repository)
        {
            localRepository = repository;
        }

        /// <summary>
        /// Gets local installed repository for the project.
        /// </summary>
        public virtual IRepositoryInstalled GetLocalInstalledRepository()
        {
            return localRepository;
        }
    }
}
