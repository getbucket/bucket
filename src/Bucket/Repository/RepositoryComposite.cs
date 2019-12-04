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

using Bucket.Package;
using Bucket.Semver.Constraint;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bucket.Repository
{
    /// <summary>
    /// Represents a composite repository that allows multiple repositories to be treated as one.
    /// </summary>
    public class RepositoryComposite : IRepository
    {
        private readonly List<IRepository> repositories;

        /// <summary>
        /// Initializes a new instance of the <see cref="RepositoryComposite"/> class.
        /// </summary>
        public RepositoryComposite(params IRepository[] repositories)
        {
            this.repositories = new List<IRepository>();
            Array.ForEach(repositories, AddRepository);
        }

        /// <inheritdoc />
        public int Count => repositories.Sum((repository) => repository.Count);

        /// <summary>
        /// Gets an array of all repositories.
        /// </summary>
        /// <returns>Returns an array of all repositories.</returns>
        public IRepository[] GetRepositories()
        {
            return repositories.ToArray();
        }

        /// <inheritdoc />
        public IPackage FindPackage(string name, IConstraint constraint)
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

        /// <inheritdoc />
        public IPackage[] FindPackages(string name, IConstraint constraint = null)
        {
            return repositories.SelectMany((repository) => repository.FindPackages(name, constraint)).ToArray();
        }

        /// <inheritdoc />
        public IPackage[] GetPackages()
        {
            return repositories.SelectMany((repository) => repository.GetPackages()).ToArray();
        }

        /// <inheritdoc />
        public bool HasPackage(IPackage package)
        {
            return repositories.Exists((repository) => repository.HasPackage(package));
        }

        /// <inheritdoc />
        public SearchResult[] Search(string query, SearchMode mode = SearchMode.Fulltext, string type = null)
        {
            return repositories.SelectMany((repository) => repository.Search(query, mode, type)).ToArray();
        }

        /// <summary>
        /// Add a repository.
        /// If the <paramref name="repository"/> is <see cref="RepositoryComposite"/> will automatically dumped.
        /// </summary>
        public void AddRepository(IRepository repository)
        {
            if (repository is RepositoryComposite repositoryComposite)
            {
                Array.ForEach(repositoryComposite.GetRepositories(), AddRepository);
            }
            else
            {
                repositories.Add(repository);
            }
        }
    }
}
