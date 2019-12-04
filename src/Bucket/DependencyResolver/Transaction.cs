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

using Bucket.DependencyResolver.Operation;
using Bucket.DependencyResolver.Policy;
using Bucket.DependencyResolver.Rules;
using Bucket.Package;
using Bucket.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bucket.DependencyResolver
{
    /// <summary>
    /// Represents a transaction that changes the decision to operate.
    /// </summary>
    internal sealed class Transaction
    {
        private readonly IPolicy policy;
        private readonly Pool pool;
        private readonly IDictionary<int, IPackage> installedMap;
        private readonly Decisions decisions;
        private readonly LinkedList<IOperation> transaction;

        // The relationship between literal and installed map:
        //
        // |----------------------------------------------|
        // |             |   installed   |  un-installed  |
        // |==============================================|
        // |      +      |     keep      |    install     |
        // |----------------------------------------------|
        // |      -      |   uninstall   | don't install  |
        // |----------------------------------------------|

        /// <summary>
        /// Initializes a new instance of the <see cref="Transaction"/> class.
        /// </summary>
        public Transaction(IPolicy policy, Pool pool, IDictionary<int, IPackage> installedMap, Decisions decisions)
        {
            this.policy = policy;
            this.pool = pool;
            this.installedMap = installedMap;
            this.decisions = decisions;
            transaction = new LinkedList<IOperation>();
        }

        private enum OperationType
        {
            Update,
            Install,
            Uninstall,
        }

        /// <summary>
        /// Returns an array indicating what needs to be done.
        /// </summary>
        public IOperation[] GetOperations()
        {
            var installMeansUpdateMap = FindUpdates();

            var ignoreUninstall = new HashSet<int>();
            var updateMap = new Dictionary<int, OperationData>();
            var installMap = new Dictionary<int, OperationData>();
            var uninstallMap = new Dictionary<int, OperationData>();
            foreach (var (literal, reason) in decisions)
            {
                var package = pool.GetPackageByLiteral(literal);

                if ((literal > 0) == installedMap.ContainsKey(package.Id))
                {
                    // keep || dont' install
                    continue;
                }

                if (literal <= 0)
                {
                    // uninstall
                    continue;
                }

                if (installMeansUpdateMap.ContainsKey(Math.Abs(literal)) && !(package is PackageAlias))
                {
                    var source = installMeansUpdateMap[Math.Abs(literal)];
                    updateMap[package.Id] = new OperationData()
                    {
                        Type = OperationType.Update,
                        Package = package,
                        Source = source,
                        Reason = reason,
                    };

                    // avoid updates to one package from multiple origins.
                    installMeansUpdateMap.Remove(Math.Abs(literal));
                    ignoreUninstall.Add(source.Id);
                }
                else
                {
                    installMap[package.Id] = new OperationData()
                    {
                        Type = OperationType.Install,
                        Package = package,
                        Reason = reason,
                    };
                }
            }

            foreach (var (literal, reason) in decisions)
            {
                var package = pool.GetPackageByLiteral(literal);

                if (literal <= 0 && installedMap.ContainsKey(package.Id) && !ignoreUninstall.Contains(package.Id))
                {
                    // uninstall && !ignoreUninstall
                    uninstallMap[package.Id] = new OperationData()
                    {
                        Type = OperationType.Uninstall,
                        Package = package,
                        Reason = reason,
                    };
                }
            }

            TransactionFromMaps(installMap, updateMap, uninstallMap);

            return transaction.ToArray();
        }

        private void TransactionFromMaps(
            IDictionary<int, OperationData> installMap, IDictionary<int, OperationData> updateMap,
            IDictionary<int, OperationData> uninstallMap)
        {
            var stack = new Stack<IPackage>(Arr.Map(
                FindRootPackages(installMap, updateMap),
                (operation) => operation.Value.Package));

            var visited = new HashSet<int>();
            while (stack.Count > 0)
            {
                var package = stack.Pop();
                var packageId = package.Id;

                if (!visited.Contains(packageId))
                {
                    stack.Push(package);
                    visited.Add(package.Id);

                    if (package is PackageAlias packageAlias)
                    {
                        stack.Push(packageAlias.GetAliasOf());
                        continue;
                    }

                    foreach (var link in package.GetRequires())
                    {
                        var possibleRequires = pool.WhatProvides(link.GetTarget(), link.GetConstraint());
                        Array.ForEach(possibleRequires, stack.Push);
                    }

                    continue;
                }

                if (installMap.ContainsKey(packageId))
                {
                    var operation = installMap[packageId];
                    Install(operation.Package, operation.Reason);
                    installMap.Remove(packageId);
                }

                if (updateMap.ContainsKey(packageId))
                {
                    var operation = updateMap[packageId];
                    Update(operation.Source, operation.Package, operation.Reason);
                    updateMap.Remove(packageId);
                }
            }

            foreach (var item in uninstallMap)
            {
                Uninstall(item.Value.Package, item.Value.Reason);
            }
        }

        private IDictionary<int, OperationData> FindRootPackages(IDictionary<int, OperationData> installMap, IDictionary<int, OperationData> updateMap)
        {
            var roots = new Dictionary<int, OperationData>(installMap);

            foreach (var item in updateMap)
            {
                if (!roots.ContainsKey(item.Key))
                {
                    roots.Add(item.Key, item.Value);
                }
            }

            foreach (var item in roots.ToArray())
            {
                var packageId = item.Key;
                var package = item.Value.Package;

                if (!roots.ContainsKey(packageId))
                {
                    continue;
                }

                foreach (var link in package.GetRequires())
                {
                    var possibleRequires = pool.WhatProvides(link.GetTarget(), link.GetConstraint());
                    foreach (var require in possibleRequires)
                    {
                        if (require != package)
                        {
                            roots.Remove(require.Id);
                        }
                    }
                }
            }

            return roots;
        }

        private IDictionary<int, IPackage> FindUpdates()
        {
            var installMeansUpdateMap = new Dictionary<int, IPackage>();

            foreach (var (literal, reason) in decisions)
            {
                var package = pool.GetPackageByLiteral(literal);
                if (package is PackageAlias)
                {
                    continue;
                }

                if (literal > 0 || !installedMap.ContainsKey(package.Id))
                {
                    // keep || install || dont' install
                    continue;
                }

                var updates = policy.FindUpdatePackages(pool, installedMap, package);

                void AddInstallMeansUpdateMap(int updateLiteral)
                {
                    if (literal != updateLiteral)
                    {
                        // Value means the source package. not an upgrade package.
                        installMeansUpdateMap.Add(Math.Abs(updateLiteral), package);
                    }
                }

                AddInstallMeansUpdateMap(package.Id);
                Array.ForEach(updates, (update) => AddInstallMeansUpdateMap(update.Id));
            }

            return installMeansUpdateMap;
        }

        private void Install(IPackage package, Rule reason)
        {
            if (package is PackageAlias packageAlias)
            {
                MarkPackageAliasInstalled(packageAlias, reason);
                return;
            }

            transaction.AddLast(new OperationInstall(package, reason));
        }

        private void Uninstall(IPackage package, Rule reason)
        {
            if (package is PackageAlias packageAlias)
            {
                MarkPackageAliasUninstall(packageAlias, reason);
                return;
            }

            transaction.AddLast(new OperationUninstall(package, reason));
        }

        private void Update(IPackage initial, IPackage target, Rule reason)
        {
            transaction.AddLast(new OperationUpdate(initial, target, reason));
        }

        private void MarkPackageAliasInstalled(PackageAlias package, Rule reason)
        {
            transaction.AddLast(new OperationMarkPackageAliasInstalled(package, reason));
        }

        private void MarkPackageAliasUninstall(PackageAlias package, Rule reason)
        {
            transaction.AddLast(new OperationMarkPackageAliasUninstall(package, reason));
        }

        private class OperationData
        {
            public OperationType Type { get; set; }

            public IPackage Package { get; set; }

            /// <summary>
            /// Gets or sets when <see cref="OperationType.Update"/> indicates the
            /// original package before the update. Other case fields are invalid.
            /// </summary>
            public IPackage Source { get; set; }

            public Rule Reason { get; set; }
        }
    }
}
