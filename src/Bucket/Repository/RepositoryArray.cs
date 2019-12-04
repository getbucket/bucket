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
using Bucket.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Bucket.Repository
{
    /// <summary>
    /// A repository implementation that simply stores packages in an array(Means in memory).
    /// </summary>
    public class RepositoryArray : IRepository, IEnumerable<IPackage>
    {
        private readonly LinkedList<IPackage> packages;
        private IPackage[] cached;
        private bool inited;

        /// <summary>
        /// Initializes a new instance of the <see cref="RepositoryArray"/> class.
        /// </summary>
        /// <param name="packages">Initializes package array.</param>
        public RepositoryArray(IEnumerable<IPackage> packages = null)
        {
            this.packages = new LinkedList<IPackage>();
            foreach (var package in packages ?? Array.Empty<IPackage>())
            {
                AddPackage(package);
            }
        }

        /// <inheritdoc />
        public int Count => packages.Count;

        /// <inheritdoc />
        public IEnumerator<IPackage> GetEnumerator()
        {
            foreach (var package in GetPackages())
            {
                yield return package;
            }
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc />
        public virtual IPackage FindPackage(string name, IConstraint constraint)
        {
            name = name.ToLower();
            foreach (var package in GetPackages())
            {
                if (package.GetName() != name)
                {
                    continue;
                }

                var packageConstraint = new Constraint("==", package.GetVersion());
                if (constraint.Matches(packageConstraint))
                {
                    return package;
                }
            }

            return null;
        }

        /// <inheritdoc />
        public virtual IPackage[] FindPackages(string name, IConstraint constraint = null)
        {
            name = name.ToLower();
            var collection = new LinkedList<IPackage>();
            foreach (var package in GetPackages())
            {
                if (package.GetName() != name)
                {
                    continue;
                }

                if (constraint == null || constraint.Matches(new Constraint("==", package.GetVersion())))
                {
                    collection.AddLast(package);
                }
            }

            return collection.ToArray();
        }

        /// <inheritdoc />
        public virtual IPackage[] GetPackages()
        {
            GuardInitialize();

            if (cached == null)
            {
                cached = packages.ToArray();
            }

            return cached;
        }

        /// <inheritdoc />
        public virtual bool HasPackage(IPackage package)
        {
            // don't use packages.Contains(). Because this requires
            // processing the alias package or clone package.
            var packageId = package.GetNameUnique();
            foreach (var repositoryPackage in GetPackages())
            {
                if (packageId == repositoryPackage.GetNameUnique())
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc />
        public virtual SearchResult[] Search(string query, SearchMode mode = SearchMode.Fulltext, string type = null)
        {
            var matchRegex = GenerateMatchRegex(query);
            var searched = new Dictionary<string, IPackage>();
            foreach (var package in GetPackages())
            {
                var name = package.GetName();
                if (searched.ContainsKey(name))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(type) && type != package.GetPackageType())
                {
                    continue;
                }

                if (Regex.IsMatch(name, matchRegex, RegexOptions.IgnoreCase))
                {
                    goto matched;
                }

                if (mode != SearchMode.Fulltext || !(package is IPackageComplete packageComplete))
                {
                    continue;
                }

                var keyword = string.Join(Str.Space, packageComplete.GetKeywords()) + Str.Space + packageComplete.GetDescription();

                if (!Regex.IsMatch(keyword, matchRegex, RegexOptions.IgnoreCase))
                {
                    continue;
                }

            matched:
                searched[name] = package;
            }

            return Arr.Map(searched.Values, CreateSearchResult);
        }

        /// <summary>
        /// Adds a new package to the repository.
        /// </summary>
        /// <param name="package">The added package instance.</param>
        public void AddPackage(IPackage package)
        {
            GuardInitialize();

            package.SetRepository(this);
            packages.AddLast(package);
            if (package is PackageAlias packageAlias)
            {
                var aliasedPackage = packageAlias.GetAliasOf();
                if (aliasedPackage.GetRepository() == null)
                {
                    AddPackage(aliasedPackage);
                }
            }

            cached = null;
        }

        /// <summary>
        /// Removes package from repository.
        /// </summary>
        /// <param name="package">The package instance.</param>
        public void RemovePackage(IPackage package)
        {
            var packageId = package.GetNameUnique();
            foreach (var repositoryPackage in GetPackages())
            {
                if (packageId == repositoryPackage.GetNameUnique())
                {
                    packages.Remove(repositoryPackage);
                }
            }

            cached = null;
        }

        /// <summary>
        /// Create an alias package instance.
        /// </summary>
        /// <param name="package">The package instance.</param>
        /// <param name="version">The alias version.</param>
        /// <param name="versionPretty">The alias version pretty name.</param>
        /// <returns>Returns the alias package isntance.</returns>
        protected static PackageAlias CreatePackageAlias(IPackage package, string version, string versionPretty)
        {
            return new PackageAlias(package is PackageAlias packageAlias ? packageAlias.GetAliasOf() : package, version, versionPretty);
        }

        /// <summary>
        /// Create search result with package instance.
        /// </summary>
        /// <param name="package">The package instance.</param>
        /// <returns>Returns search result instance.</returns>
        protected virtual SearchResult CreateSearchResult(IPackage package)
        {
            return new SearchResult(package);
        }

        /// <summary>
        /// Generate match expression from query.
        /// </summary>
        /// <param name="query">The search query.</param>
        /// <returns>Return the regex match string.</returns>
        protected virtual string GenerateMatchRegex(string query)
        {
            var segments = Regex.Split(query, @"\s+");
            return string.Join("|", segments);
        }

        /// <summary>
        /// Initialize the repository. Mostly meant as an extension point.
        /// </summary>
        protected virtual void Initialize()
        {
        }

        /// <summary>
        /// Clear the repository. This operation does not cause the
        /// initialization state to be reset.
        /// </summary>
        protected virtual void Clear()
        {
            packages.Clear();
            cached = null;
        }

        /// <summary>
        /// The guarantee must be initialized.
        /// </summary>
        private void GuardInitialize()
        {
            if (!inited)
            {
                inited = true;
                Initialize();
            }
        }
    }
}
