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
using Bucket.Package;
using Bucket.Repository;
using Bucket.Semver;
using Bucket.Semver.Constraint;
using Bucket.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Bucket.DependencyResolver
{
    /// <summary>
    /// A package pool contains repositories that provide packages.
    /// </summary>
    public class Pool
    {
        private readonly IDictionary<string, Stabilities> stabilityFlags;
        private readonly IDictionary<string, IConstraint> filterRequires;
        private readonly IList<IPackage> packages;
        private readonly IDictionary<string, IList<IPackage>> packageByName;
        private readonly IDictionary<string, IDictionary<int, IPackage>> packageByNameExact;
        private readonly IList<IRepository> repositories;
        private readonly ICollection<Stabilities> acceptableStabilities;
        private readonly IDictionary<string, IPackage[]> providerCache;
        private readonly IList<ILazyload> lazyloadRepositories;
        private ICollection<int> whitelist;
        private int id = 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="Pool"/> class.
        /// </summary>
        /// <param name="minimumStability">
        /// Indicates the minimum stability version(Packages below this stability
        /// will not be added to the pool).
        /// </param>
        /// <param name="stabilityFlags">Indicates the stability flag of the specified package.</param>
        /// <param name="filterRequires">The packets in the map will be filtered.</param>
        public Pool(
            Stabilities minimumStability = Stabilities.Stable,
            IDictionary<string, Stabilities> stabilityFlags = null,
            IDictionary<string, IConstraint> filterRequires = null)
        {
            this.stabilityFlags = stabilityFlags ?? new Dictionary<string, Stabilities>();
            this.filterRequires = filterRequires ?? new Dictionary<string, IConstraint>();
            repositories = new List<IRepository>();
            packages = new List<IPackage>();
            acceptableStabilities = new HashSet<Stabilities>();
            packageByName = new Dictionary<string, IList<IPackage>>();
            packageByNameExact = new Dictionary<string, IDictionary<int, IPackage>>();
            providerCache = new Dictionary<string, IPackage[]>();
            lazyloadRepositories = new List<ILazyload>();

            foreach (Stabilities stability in Enum.GetValues(typeof(Stabilities)))
            {
                if (stability <= minimumStability)
                {
                    acceptableStabilities.Add(stability);
                }
            }

            foreach (var name in this.filterRequires.Keys.ToArray())
            {
                if (Regex.IsMatch(name, RepositoryPlatform.RegexPlatform))
                {
                    this.filterRequires.Remove(name);
                }
            }
        }

        /// <summary>
        /// Gets the registered package count.
        /// </summary>
        public int Count => packages.Count;

        /// <summary>
        /// Sets the white list to compute provide.
        /// Only packages in the whitelist can be computed.
        /// </summary>
        /// <param name="whitelist">The white list.</param>
        public void SetWhiteList(ICollection<int> whitelist)
        {
            // todo: Whitelist mechanism confuses code, consider replacing.
            this.whitelist = whitelist;
            providerCache.Clear();
        }

        /// <summary>
        /// Adds a repository and its packages to this package pool.
        /// </summary>
        /// <param name="repository">A package repository.</param>
        /// <param name="rootAliases">An array of the root required aliases.</param>
        public void AddRepository(IRepository repository, ConfigAlias[] rootAliases = null)
        {
            IRepository[] repos;
            if (repository is RepositoryComposite repositoryComposite)
            {
                repos = repositoryComposite.GetRepositories();
            }
            else
            {
                repos = new[] { repository };
            }

            rootAliases = rootAliases ?? Array.Empty<ConfigAlias>();
            void AddPackageByNameExact(IPackage package)
            {
                if (!packageByNameExact.TryGetValue(
                        package.GetName(),
                        out IDictionary<int, IPackage> mapping))
                {
                    packageByNameExact[package.GetName()] = mapping = new Dictionary<int, IPackage>();
                }

                mapping[package.Id] = package;
            }

            void AddPackageByName(string name, IPackage package)
            {
                if (!packageByName.TryGetValue(name, out IList<IPackage> list))
                {
                    packageByName[name] = list = new List<IPackage>();
                }

                list.Add(package);
            }

            foreach (var repo in repos)
            {
                repositories.Add(repo);
                var exempt = repo is RepositoryPlatform || repo is IRepositoryInstalled;

                if (repo is ILazyload repositoryLazyload && repositoryLazyload.IsLazyLoad)
                {
                    lazyloadRepositories.Add(repositoryLazyload);
                    continue;
                }

                foreach (var package in repo.GetPackages())
                {
                    var names = package.GetNames();
                    var stability = package.GetStability();
                    if (!(exempt || IsPackageAcceptable(stability, names)))
                    {
                        continue;
                    }

                    package.Id = id++;
                    packages.Add(package);
                    AddPackageByNameExact(package);

                    foreach (var name in names)
                    {
                        AddPackageByName(name, package);
                    }

                    var packageName = package.GetName();
                    var alias = ConfigAlias.FindAlias(rootAliases, packageName, package.GetVersion());
                    if (alias == null)
                    {
                        continue;
                    }

                    var originalPackage = package;
                    if (originalPackage is PackageAlias packageAlias)
                    {
                        originalPackage = packageAlias.GetAliasOf();
                    }

                    packageAlias = new PackageAlias(originalPackage, alias.AliasNormalized, alias.Alias);
                    packageAlias.SetRootPackageAlias(true);
                    packageAlias.Id = id++;

                    if (originalPackage.GetRepository() is RepositoryArray repositoryArray)
                    {
                        repositoryArray.AddPackage(packageAlias);
                    }
                    else if (originalPackage.GetRepository() is IRepositoryWriteable repositoryWriteable)
                    {
                        repositoryWriteable.AddPackage(packageAlias);
                    }
                    else
                    {
                        throw new RuntimeException($"The repository is must be {nameof(RepositoryArray)} or {nameof(IRepositoryWriteable)} in package {originalPackage}");
                    }

                    packages.Add(packageAlias);
                    AddPackageByNameExact(packageAlias);
                    foreach (var name in packageAlias.GetNames())
                    {
                        AddPackageByName(name, packageAlias);
                    }
                }
            }
        }

        /// <summary>
        /// Get the priority of the specified repository.
        /// </summary>
        /// <param name="repository">The specified repository.</param>
        /// <returns>A negative number to indicate priority.</returns>
        public int GetPriority(IRepository repository)
        {
            var priority = repositories.IndexOf(repository);
            if (priority == -1)
            {
                throw new RuntimeException("Could not determine repository priority. The repository was not registered in the pool.");
            }

            return -priority;
        }

        /// <summary>
        /// Retrieves the package object for a given package id.
        /// </summary>
        /// <param name="id">The package id.</param>
        /// <returns>Returns an package instance.</returns>
        public IPackage GetPackageById(int id)
        {
            return packages[id - 1];
        }

        /// <summary>
        /// Retrieves the package object for a given literal.
        /// </summary>
        /// <param name="literal">The literal.</param>
        /// <returns>Returns an package instance.</returns>
        public IPackage GetPackageByLiteral(int literal)
        {
            return GetPackageById(Math.Abs(literal));
        }

        /// <summary>
        /// Convert literal to a pretty string.
        /// </summary>
        /// <param name="literal">The literal.</param>
        /// <param name="installedMap">Installed package mapping table.</param>
        /// <returns>Returns the pretty string.</returns>
        public string LiteralToPrettyString(int literal, IDictionary<int, IPackage> installedMap)
        {
            var package = GetPackageByLiteral(literal);

            string prefix;
            if (installedMap != null && installedMap.ContainsKey(package.Id))
            {
                prefix = literal > 0 ? "keep" : "uninstall";
            }
            else
            {
                prefix = literal > 0 ? "install" : "don't install";
            }

            return prefix + Str.Space + package.GetPrettyString();
        }

        /// <summary>
        /// Whether the package is acceptable.
        /// </summary>
        /// <param name="stability">The package stability.</param>
        /// <param name="names">An array of package name. Matches one to true.</param>
        /// <returns>True if the package is acceptable.</returns>
        public bool IsPackageAcceptable(Stabilities stability, params string[] names)
        {
            foreach (var name in names)
            {
                var flagExist = stabilityFlags.TryGetValue(name, out Stabilities flagStability);

                // allow if package matches the global stability requirement.
                if (!flagExist && acceptableStabilities.Contains(stability))
                {
                    return true;
                }

                // allow if package matches the package-specific stability flag
                if (flagExist && stability <= flagStability)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Searches all packages providing the given package name and match the constraint.
        /// </summary>
        /// <param name="name">The package name to be searched for.</param>
        /// <param name="constraint">A constraint that all returned packages must match.</param>
        /// <param name="mustMatchName">Whether the name of returned packages must match the given name.</param>
        /// <param name="bypassFilters">If enabled, filterRequires and stability matching is ignored.</param>
        /// <returns>An array of match packages.</returns>
        public virtual IPackage[] WhatProvides(string name, IConstraint constraint = null, bool mustMatchName = false, bool bypassFilters = false)
        {
            if (bypassFilters)
            {
                return ComputeWhatProvides(name, constraint, mustMatchName, bypassFilters);
            }

            var key = name + mustMatchName + constraint?.ToString();
            if (providerCache.TryGetValue(key, out IPackage[] cache))
            {
                return cache;
            }

            return providerCache[key] = ComputeWhatProvides(name, constraint, mustMatchName, bypassFilters);
        }

        /// <summary>
        /// Checks if the package matches the given constraint
        /// directly or through provided or replaced packages.
        /// </summary>
        /// <param name="candidate">Candidate package instance.</param>
        /// <param name="name">Name of the package to be matched.</param>
        /// <param name="constraint">The constraint to verify.</param>
        /// <param name="bypassFilters">If enabled, filterRequires and stability matching is ignored.</param>
        /// <returns>Type of package match.</returns>
        internal PoolMatch Match(IPackage candidate, string name, IConstraint constraint = null, bool bypassFilters = false)
        {
            var candidateName = candidate.GetName();
            var candidateVersion = candidate.GetVersion();
            var isDev = candidate.IsDev;
            var isAlias = candidate is PackageAlias;

            if (!(!bypassFilters && !isDev && !isAlias && filterRequires.TryGetValue(name, out IConstraint requireFilter)))
            {
                requireFilter = new ConstraintNone();
            }

            if (candidateName == name)
            {
                var packageConstraint = new Constraint("==", candidateVersion);
                if (constraint == null || constraint.Matches(packageConstraint))
                {
                    return requireFilter.Matches(packageConstraint) ? PoolMatch.Match : PoolMatch.Filtered;
                }

                return PoolMatch.Name;
            }

            var provides = candidate.GetProvides();
            var replaces = candidate.GetReplaces();

            foreach (var link in provides)
            {
                if (link.GetTarget() == name && (constraint == null || constraint.Matches(link.GetConstraint())))
                {
                    return requireFilter.Matches(link.GetConstraint()) ? PoolMatch.Provide : PoolMatch.Filtered;
                }
            }

            foreach (var link in replaces)
            {
                if (link.GetTarget() == name && (constraint == null || constraint.Matches(link.GetConstraint())))
                {
                    return requireFilter.Matches(link.GetConstraint()) ? PoolMatch.Replace : PoolMatch.Filtered;
                }
            }

            return PoolMatch.None;
        }

        private IPackage[] ComputeWhatProvides(string name, IConstraint constraint = null, bool mustMatchName = false, bool bypassFilters = false)
        {
            var candidates = new List<IPackage>();

            // todo: bad smell maybe add a pool builder. The pool should only deal
            // with package relationships and should not involve other logic.
            foreach (var repository in lazyloadRepositories)
            {
                foreach (var candidate in repository.WhatProvides(name, IsPackageAcceptable))
                {
                    candidates.Add(candidate);
                    if (candidate.Id < 1)
                    {
                        candidate.Id = id++;
                        packages.Add(candidate);
                    }
                }
            }

            if (mustMatchName)
            {
                if (packageByNameExact.TryGetValue(name, out IDictionary<int, IPackage> packageExact))
                {
                    candidates.AddRange(packageExact.Values);
                }
            }
            else if (packageByName.TryGetValue(name, out IList<IPackage> packages))
            {
                candidates.AddRange(packages);
            }

            var nameMatch = false;
            var matches = new LinkedList<IPackage>();
            var provide = new LinkedList<IPackage>();
            foreach (var candidate in candidates)
            {
                if (whitelist != null && !bypassFilters)
                {
                    if (candidate is PackageAlias packageAlias)
                    {
                        if (!whitelist.Contains(packageAlias.GetAliasOf().Id))
                        {
                            continue;
                        }
                    }
                    else if (!whitelist.Contains(candidate.Id))
                    {
                        continue;
                    }
                }

                switch (Match(candidate, name, constraint, bypassFilters))
                {
                    case PoolMatch.None:
                    case PoolMatch.Filtered:
                        break;
                    case PoolMatch.Name:
                        nameMatch = true;
                        break;
                    case PoolMatch.Match:
                        nameMatch = true;
                        matches.AddLast(candidate);
                        break;
                    case PoolMatch.Provide:
                        provide.AddLast(candidate);
                        break;
                    case PoolMatch.Replace:
                        matches.AddLast(candidate);
                        break;
                    default:
                        throw new UnexpectedException("Unexpected match type.");
                }
            }

            // if a package with the required name exists, we ignore providers.
            if (nameMatch)
            {
                return matches.ToArray();
            }

            return matches.Concat(provide).ToArray();
        }
    }
}
