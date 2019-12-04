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
using System.Collections.Generic;
using System.Linq;

namespace Bucket.DependencyResolver.Policy
{
    /// <summary>
    /// Indicates the default package selection policy.
    /// </summary>
    public class PolicyDefault : IPolicy
    {
        private readonly bool preferStable;
        private readonly bool preferLowest;

        /// <summary>
        /// Initializes a new instance of the <see cref="PolicyDefault"/> class.
        /// </summary>
        /// <param name="preferStable">Whether prefer a stable version.</param>
        /// <param name="preferLowest">Whether prefer the lowest version.</param>
        public PolicyDefault(bool preferStable = false, bool preferLowest = false)
        {
            this.preferStable = preferStable;
            this.preferLowest = preferLowest;
        }

        /// <inheritdoc />
        public virtual IPackage[] FindUpdatePackages(Pool pool, IDictionary<int, IPackage> installedMap, IPackage package)
        {
            return FindUpdatePackages(pool, package, false);
        }

        /// <inheritdoc />
        public virtual int[] SelectPreferredPackages(Pool pool, IDictionary<int, IPackage> installedMap, int[] literals, string requirePackageName = null)
        {
            var packages = GroupLiteralsByNamePreferInstalled(pool, installedMap, literals);

            foreach (var item in packages)
            {
                Array.Sort(item.Value, new CompareByPriorityPreferInstalled(pool, installedMap, requirePackageName, true));
            }

            var selected = new List<int>();
            foreach (var item in packages)
            {
                var pruneLiterals = item.Value;
                pruneLiterals = PruneToHighestPriorityOrInstalled(pool, installedMap, pruneLiterals);
                pruneLiterals = PruneToBestVersion(pool, pruneLiterals);
                pruneLiterals = PruneRemoteAliases(pool, pruneLiterals);
                selected.AddRange(pruneLiterals);
            }

            // now sort the result across all packages to respect replaces across packages.
            selected.Sort(new CompareByPriorityPreferInstalled(pool, installedMap, requirePackageName));

            return selected.ToArray();
        }

        /// <inheritdoc />
        public virtual bool VersionCompare(IPackage left, string operatorString, IPackage right)
        {
            if (preferStable && left.GetStability() != right.GetStability())
            {
                return left.GetStability() < right.GetStability();
            }

            var constraint = new Constraint(operatorString, right.GetVersion());
            var version = new Constraint("==", left.GetVersion());
            return constraint.MatchSpecific(version, true);
        }

        /// <summary>
        /// Grouping literal by package name, prefers installed packages
        /// (Literals representing installed packages will be in front).
        /// </summary>
        /// <param name="pool">A package pool.</param>
        /// <param name="installedMap">Installed package mapping table.</param>
        /// <param name="literals">Package to be selected.(Use literals to represent).</param>
        /// <returns>Returns a mapping representing grouped literals.</returns>
        protected static IDictionary<string, int[]> GroupLiteralsByNamePreferInstalled(Pool pool, IDictionary<int, IPackage> installedMap, int[] literals)
        {
            var groups = new Dictionary<string, LinkedList<int>>();
            foreach (var literal in literals)
            {
                var packageName = pool.GetPackageByLiteral(literal).GetName();
                if (!groups.TryGetValue(packageName, out LinkedList<int> packageLiterals))
                {
                    groups[packageName] = packageLiterals = new LinkedList<int>();
                }

                if (installedMap.ContainsKey(Math.Abs(literal)))
                {
                    packageLiterals.AddFirst(literal);
                }
                else
                {
                    packageLiterals.AddLast(literal);
                }
            }

            var result = new Dictionary<string, int[]>();
            foreach (var group in groups)
            {
                result[group.Key] = group.Value.ToArray();
            }

            return result;
        }

        /// <summary>
        /// Assumes that installed packages come first and then all highest priority packages.
        /// </summary>
        /// <remarks>The installed package will definitely be selected.</remarks>
        /// <param name="pool">A package pool.</param>
        /// <param name="installedMap">Installed package mapping table.</param>
        /// <param name="literals">An array of literals need to prune.</param>
        /// <returns>An array of the prune package ilterals.</returns>
        protected virtual int[] PruneToHighestPriorityOrInstalled(Pool pool, IDictionary<int, IPackage> installedMap, int[] literals)
        {
            var selected = new List<int>();
            var priority = int.MaxValue;
            foreach (var literal in literals)
            {
                var package = pool.GetPackageByLiteral(literal);
                if (installedMap.ContainsKey(package.Id))
                {
                    selected.Add(literal);
                    continue;
                }

                // pool.AddRepository determines the priority of the library,
                // and the value of package.Id is also controlled by this
                // function, so we only need to get the first repository,
                // which must be high priority.
                if (priority == int.MaxValue)
                {
                    priority = pool.GetPriority(package.GetRepository());
                }

                if (pool.GetPriority(package.GetRepository()) != priority)
                {
                    break;
                }

                selected.Add(literal);
            }

            return selected.ToArray();
        }

        /// <summary>
        /// Get the most suitable version. results require on preferences.
        /// </summary>
        /// <param name="pool">A package pool.</param>
        /// <param name="literals">An array of literals need to prune.</param>
        /// <returns>Returns the most suitable version.</returns>
        protected virtual int[] PruneToBestVersion(Pool pool, int[] literals)
        {
            var operatorString = preferLowest ? "<" : ">";
            var bestLiterals = new[] { literals[0] };
            var bestPackage = pool.GetPackageByLiteral(literals[0]);

            for (var i = 1; i < literals.Length; i++)
            {
                var literal = literals[i];
                var package = pool.GetPackageByLiteral(literal);
                if (VersionCompare(package, operatorString, bestPackage))
                {
                    bestPackage = package;
                    bestLiterals = new[] { literal };
                }
                else if (VersionCompare(package, "==", bestPackage))
                {
                    Arr.Push(ref bestLiterals, literal);
                }
            }

            return bestLiterals;
        }

        /// <summary>
        /// Assumes that locally aliased (in root package requires) packages take priority over branch-alias ones.
        /// If no package is a local alias, nothing happens.
        /// </summary>
        /// <param name="pool">A package pool.</param>
        /// <param name="literals">An array of literals need to prune.</param>
        /// <returns>An array of the prune package ilterals.</returns>
        protected virtual int[] PruneRemoteAliases(Pool pool, int[] literals)
        {
            var hasLocalAlias = false;
            foreach (var literal in literals)
            {
                var package = pool.GetPackageByLiteral(literal);
                if (package is PackageAlias packageAlias && packageAlias.IsRootPackageAlias)
                {
                    hasLocalAlias = true;
                    break;
                }
            }

            if (!hasLocalAlias)
            {
                return literals;
            }

            var selected = new List<int>();
            foreach (var literal in literals)
            {
                var package = pool.GetPackageByLiteral(literal);
                if (package is PackageAlias packageAlias && packageAlias.IsRootPackageAlias)
                {
                    selected.Add(literal);
                    break;
                }
            }

            return selected.ToArray();
        }

        /// <summary>
        /// Find packages that the specified package can be updated.
        /// </summary>
        /// <param name="pool">A package pool.</param>
        /// <param name="package">The specified package instance.</param>
        /// <param name="mustMatchName">Whether must match the package name.</param>
        /// <returns>Returns an array of package can be updated.</returns>
        private static IPackage[] FindUpdatePackages(Pool pool, IPackage package, bool mustMatchName)
        {
            var packages = new LinkedList<IPackage>();
            foreach (var candidate in pool.WhatProvides(package.GetName(), null, mustMatchName))
            {
                if (candidate != package)
                {
                    packages.AddLast(candidate);
                }
            }

            return packages.ToArray();
        }

        private class CompareByPriorityPreferInstalled : IComparer<int>
        {
            private readonly Pool pool;
            private readonly IDictionary<int, IPackage> installedMap;
            private readonly string requirePackageName;
            private readonly bool ignoreReplace;

            public CompareByPriorityPreferInstalled(Pool pool, IDictionary<int, IPackage> installedMap, string requirePackageName = null, bool ignoreReplace = false)
            {
                this.pool = pool;
                this.installedMap = installedMap;
                this.requirePackageName = requirePackageName;
                this.ignoreReplace = ignoreReplace;
            }

            public int Compare(int x, int y)
            {
                var left = pool.GetPackageByLiteral(x);
                var right = pool.GetPackageByLiteral(y);

                if (left.GetRepository() != right.GetRepository())
                {
                    if (installedMap.ContainsKey(left.Id))
                    {
                        return -1;
                    }

                    if (installedMap.ContainsKey(right.Id))
                    {
                        return 1;
                    }

                    return pool.GetPriority(left.GetRepository()) > pool.GetPriority(right.GetRepository()) ? -1 : 1;
                }

                // prefer aliases to the original package
                if (left.GetName() == right.GetName())
                {
                    var leftIsAliased = left is PackageAlias;
                    var rightIsAliased = right is PackageAlias;

                    if (leftIsAliased && !rightIsAliased)
                    {
                        return -1;
                    }

                    if (!leftIsAliased && rightIsAliased)
                    {
                        return 1;
                    }
                }

                if (!ignoreReplace)
                {
                    // return original, not replaced
                    if (IsReplaces(left, right))
                    {
                        // use right
                        return 1;
                    }

                    if (IsReplaces(right, left))
                    {
                        // use left
                        return -1;
                    }

                    // for replacers not replacing each other, put a higher prio on replacing
                    // packages with the same vendor as the required package
                    int position;
                    if (!string.IsNullOrEmpty(requirePackageName) && (position = requirePackageName.IndexOf('/')) != -1)
                    {
                        var requireVendor = requirePackageName.Substring(0, position);

                        var leftIsSameVendor = (left.GetName().Length > position) && (left.GetName().Substring(0, position) == requireVendor);
                        var rightIsSameVendor = (right.GetName().Length > position) && (right.GetName().Substring(0, position) == requireVendor);

                        if (leftIsSameVendor != rightIsSameVendor)
                        {
                            return leftIsSameVendor ? -1 : 1;
                        }
                    }
                }

                // priority equal, sort by package id to make reproducible
                return left.Id.CompareTo(right.Id);
            }

            /// <summary>
            /// Checks if source replaces a package with the same name as target.
            /// </summary>
            private bool IsReplaces(IPackage source, IPackage target)
            {
                foreach (var link in source.GetReplaces())
                {
                    if (link.GetTarget() == target.GetName())
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
