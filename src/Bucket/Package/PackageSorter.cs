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

using System;
using System.Collections.Generic;

namespace Bucket.Package
{
    /// <summary>
    /// A sorter that represents a package.
    /// </summary>
    public static class PackageSorter
    {
        /// <summary>
        /// Sorts packages by dependency weight. In other words this method sorts
        /// the mutual reference weights of the Packages in the same array.
        /// </summary>
        /// <remarks>
        /// Packages of equal weight retain the original order. Sorting does not
        /// affect source arrays.
        /// </remarks>
        public static IPackage[] SortPackages(IPackage[] packages, bool asc = false)
        {
            // <require package, source package[]>
            var usage = new Dictionary<string, IList<string>>();
            var computing = new HashSet<string>();
            var computed = new Dictionary<string, int>();

            void AddUsage(IPackage package, Link[] links)
            {
                foreach (var link in links)
                {
                    var target = link.GetTarget();
                    if (!usage.TryGetValue(target, out IList<string> usageCollection))
                    {
                        usage[target] = usageCollection = new List<string>();
                    }

                    usageCollection.Add(package.GetName());
                }
            }

            foreach (var package in packages)
            {
                AddUsage(package, package.GetRequires());
                AddUsage(package, package.GetRequiresDev());
            }

            int ComputeImportance(string name)
            {
                // reusing computed importance.
                if (computed.TryGetValue(name, out int weight))
                {
                    return weight;
                }

                // canceling circular dependency.
                if (computing.Contains(name))
                {
                    return 0;
                }

                computing.Add(name);
                if (usage.TryGetValue(name, out IList<string> usageCollection))
                {
                    foreach (var sourcePackageName in usageCollection)
                    {
                        weight -= 1 - ComputeImportance(sourcePackageName);
                    }
                }

                computing.Remove(name);
                computed[name] = weight;
                return weight;
            }

            var weights = new List<(string PackageName, int Weight)>();
            foreach (var package in packages)
            {
                var name = package.GetName();
                weights.Add((name, ComputeImportance(name)));
            }

            var sortedPackages = (IPackage[])packages.Clone();
            Array.Sort(sortedPackages, new PackageComparer(weights, asc));

            return sortedPackages;
        }

        private class PackageComparer : IComparer<IPackage>
        {
            private readonly IDictionary<string, StableData> stables;
            private readonly bool asc;

            public PackageComparer(IList<(string PackageName, int Weight)> weights, bool asc = false)
            {
                var index = 0;
                stables = new Dictionary<string, StableData>();
                foreach (var (packageName, weight) in weights)
                {
                    stables[packageName] = new StableData() { Weight = weight, Index = index };
                }

                this.asc = asc;
            }

            /// <inheritdoc />
            public int Compare(IPackage x, IPackage y)
            {
                var stableX = stables[x.GetName()];
                var stableY = stables[y.GetName()];

                int ret;
                if (stableX.Weight != stableY.Weight)
                {
                    ret = stableX.Weight.CompareTo(stableY.Weight);
                }
                else
                {
                    ret = stableX.Index.CompareTo(stableY.Index);
                }

                return asc ? ret * -1 : ret;
            }

            private struct StableData
            {
                public int Weight { get; set; }

                public int Index { get; set; }
            }
        }
    }
}
