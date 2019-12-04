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
using Bucket.Repository;
using Bucket.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Bucket.DependencyResolver.Rules
{
    /// <summary>
    /// The rule set generator for dependency resolver.
    /// </summary>
    internal class RuleSetGenerator
    {
        private readonly IDictionary<int, IPackage> addedPackages;
        private readonly IDictionary<string, LinkedList<IPackage>> addedPackagesByNames;
        private readonly Pool pool;
        private RuleSet rules;
        private Job[] jobs;
        private IDictionary<int, IPackage> installedMap;

        /// <summary>
        /// Initializes a new instance of the <see cref="RuleSetGenerator"/> class.
        /// </summary>
        /// <param name="pool">The pool contains repositories that provide packages.</param>
        public RuleSetGenerator(Pool pool)
        {
            this.pool = pool;
            addedPackages = new Dictionary<int, IPackage>();
            addedPackagesByNames = new Dictionary<string, LinkedList<IPackage>>();
        }

        /// <summary>
        /// Gets an array of the rules for jobs.
        /// </summary>
        /// <remarks>
        /// White lists of pools are generated during rule generation.
        /// </remarks>
        /// <param name="jobs">An array of the jobs instance.</param>
        /// <param name="installedMap">Mapping for installed package.</param>
        /// <param name="ignorePlatformRequest">Whether ignore platform request.</param>
        /// <returns>Return a <see cref="RuleSet"/> instance to represent the rule set.</returns>
        public RuleSet GetRulesFor(Job[] jobs, IDictionary<int, IPackage> installedMap, bool ignorePlatformRequest = false)
        {
            rules = new RuleSet();
            this.jobs = jobs;
            this.installedMap = installedMap;
            var installPackages = installedMap.Values.ToArray();
            var whitelist = CreateWhitelistFromPackages(pool, installPackages);
            CreateWhitelistFromJobs(pool, jobs, whitelist);
            pool.SetWhiteList(whitelist);

            AddRulesForPackages(installPackages, ignorePlatformRequest);
            AddRulesForJobs(ignorePlatformRequest);

            AddConflictRules();

            // removed IPackage references.
            addedPackages.Clear();
            addedPackagesByNames.Clear();

            return rules;
        }

        /// <summary>
        /// Creates a new rule for the requires of a package.
        /// </summary>
        /// <remarks>
        /// This rule is of the form (-A|B|C), where B and C are the providers of
        /// one requires of the package A.
        /// </remarks>
        /// <param name="package">The package instance.</param>
        /// <param name="providers">The providers of the requires.</param>
        /// <param name="reason">The reason for generating this rule.</param>
        /// <param name="reasonData">Any data with the reason.</param>
        /// <returns>The generated rule or null if tautological.</returns>
        protected static Rule CreateRuleRequire(IPackage package, IPackage[] providers, Reason reason, object reasonData)
        {
            var literals = new int[providers.Length + 1];
            literals[0] = -package.Id;

            for (var i = 0; i < providers.Length; i++)
            {
                if (package == providers[i])
                {
                    return null;
                }

                literals[i + 1] = providers[i].Id;
            }

            return new RuleGeneric(literals, reason, reasonData);
        }

        /// <summary>
        /// Creates a rule to install at least one of a set of packages.
        /// </summary>
        /// <remarks>
        /// The rule is (A|B|C) with A, B and C different packages. If the given
        /// set of packages is empty an impossible rule is generated.
        /// </remarks>
        /// <param name="packages">An array of packages to choose from.</param>
        /// <param name="reason">The reason for generating this rule.</param>
        /// <param name="job">The job this rule was created from.</param>
        /// <returns>The generated rule.</returns>
        protected static Rule CreateRuleInstallOneOf(IPackage[] packages, Reason reason, Job job)
        {
            var literals = Arr.Map(packages, (package) => package.Id);
            return new RuleGeneric(literals, reason, job.PackageName, job);
        }

        /// <summary>
        /// Creates a rule to uninstall a package.
        /// </summary>
        /// <remarks>
        /// The rule for a package A is (-A).
        /// </remarks>
        /// <param name="package">The package to be uninstall.</param>
        /// <param name="reason">The reason for generating this rule.</param>
        /// <param name="job">The job this rule was created from.</param>
        /// <returns>The generated rule.</returns>
        protected static Rule CreateRuleUninstall(IPackage package, Reason reason, Job job)
        {
            return new RuleGeneric(new[] { -package.Id }, reason, job.PackageName, job);
        }

        /// <summary>
        /// Creates a rule for two conflicting or obsolete packages.
        /// </summary>
        /// <remarks>
        /// The rule for conflicting or obsolete packages A and B is (-A|-B). A is called the issuer
        /// and B the provider. Return null if it conflicts or obsolete with itself.
        /// </remarks>
        /// <param name="issuer">The package declaring the conflict or obsolete.</param>
        /// <param name="provider">The package causing the conflict or obsolete.</param>
        /// <param name="reason">The reason for generating this rule.</param>
        /// <param name="reasonData">Any data with the reason.</param>
        /// <returns>The generated rule or null if it conflicts or obsolete with itself.</returns>
        protected static Rule CreateRule2Literals(IPackage issuer, IPackage provider, Reason reason, object reasonData)
        {
            if (issuer == provider)
            {
                return null;
            }

            return new Rule2Literals(-issuer.Id, -provider.Id, reason, reasonData);
        }

        /// <summary>
        /// Create whitelist from packages. If not given whitelist then one will be created automatically.
        /// </summary>
        /// <param name="pool">The pool of the packages.</param>
        /// <param name="packages">An array of the packages.</param>
        /// <param name="whitelist">The whitelist will be populated into the current collection.</param>
        /// <returns>Returns the <paramref name="whitelist"/> instance.</returns>
        protected static ICollection<int> CreateWhitelistFromPackages(Pool pool, IPackage[] packages, ICollection<int> whitelist = null)
        {
            whitelist = whitelist ?? new HashSet<int>();

            var queue = new Queue<IPackage>();
            foreach (var package in packages)
            {
                queue.Enqueue(package);
                while (queue.Count > 0)
                {
                    var workPackage = queue.Dequeue();
                    if (whitelist.Contains(workPackage.Id))
                    {
                        continue;
                    }

                    whitelist.Add(workPackage.Id);

                    foreach (var link in workPackage.GetRequires())
                    {
                        var possibleRequires = pool.WhatProvides(link.GetTarget(), link.GetConstraint(), true);
                        Array.ForEach(possibleRequires, (requires) => queue.Enqueue(requires));
                    }

                    var obsoleteProviders = pool.WhatProvides(workPackage.GetName(), null, true);
                    foreach (var provider in obsoleteProviders)
                    {
                        if (provider == workPackage)
                        {
                            continue;
                        }

                        if (workPackage is PackageAlias packageAlias
                            && packageAlias.GetAliasOf() == provider)
                        {
                            queue.Enqueue(provider);
                        }
                    }
                }
            }

            return whitelist;
        }

        /// <summary>
        /// Create whitelist from jobs. If not given <paramref name="whitelist"/> then one will be created automatically.
        /// </summary>
        /// <param name="pool">The pool of the packages.</param>
        /// <param name="jobs">An array of the jobs.</param>
        /// <param name="whitelist">The whitelist will be populated into the current collection.</param>
        /// <returns>Returns the <paramref name="whitelist"/> instance.</returns>
        protected static ICollection<int> CreateWhitelistFromJobs(Pool pool, Job[] jobs, ICollection<int> whitelist = null)
        {
            foreach (var job in jobs)
            {
                if (job.Command != JobCommand.Install)
                {
                    continue;
                }

                var packages = pool.WhatProvides(job.PackageName, job.Constraint, true);
                whitelist = CreateWhitelistFromPackages(pool, packages, whitelist);
            }

            return whitelist;
        }

        /// <summary>
        /// Determine if the package is not likely to obsolete.
        /// </summary>
        /// <param name="package">The package instance.</param>
        /// <param name="provider">The provider instance.</param>
        /// <returns>True if the package impossible obsolete.</returns>
        protected static bool IsObsoleteImpossibleForAlias(IPackage package, IPackage provider)
        {
            var packageAlias = package as PackageAlias;
            var providerAlias = provider as PackageAlias;

            // Packages other than themselves may be obsolete.
            var impossible = (packageAlias != null && packageAlias.GetAliasOf() == provider) ||
                    (providerAlias != null && providerAlias.GetAliasOf() == package) ||
                    (packageAlias != null && providerAlias != null && packageAlias.GetAliasOf() == providerAlias.GetAliasOf());

            return impossible;
        }

        /// <summary>
        /// Add conflict rules from added packages.
        /// </summary>
        protected void AddConflictRules()
        {
            foreach (var item in addedPackages)
            {
                var package = item.Value;
                foreach (var link in package.GetConflicts())
                {
                    if (!addedPackagesByNames.ContainsKey(link.GetTarget()))
                    {
                        continue;
                    }

                    foreach (var possibleConflict in addedPackagesByNames[link.GetTarget()])
                    {
                        var conflictMatch = pool.Match(possibleConflict, link.GetTarget(), link.GetConstraint(), true);
                        if (conflictMatch == PoolMatch.Match || conflictMatch == PoolMatch.Replace)
                        {
                            rules.Add(CreateRule2Literals(package, possibleConflict, Reason.PackageConflict, link), RuleType.Package);
                        }
                    }
                }

                // check obsoletes and implicit obsoletes of a package.
                var isInstalled = installedMap.ContainsKey(package.Id);
                foreach (var link in package.GetReplaces())
                {
                    if (!addedPackagesByNames.ContainsKey(link.GetTarget()))
                    {
                        continue;
                    }

                    foreach (var provider in addedPackagesByNames[link.GetTarget()])
                    {
                        if (provider == package)
                        {
                            continue;
                        }

                        if (IsObsoleteImpossibleForAlias(package, provider))
                        {
                            continue;
                        }

                        var reason = isInstalled ? Reason.InstalledPackageObsoletes : Reason.PackageObsoletes;
                        rules.Add(CreateRule2Literals(package, provider, reason, link), RuleType.Package);
                    }
                }
            }
        }

        protected void AddRulesForPackages(IPackage[] packages, bool ignorePlatformRequest)
        {
            var queue = new Queue<IPackage>();
            foreach (var package in packages)
            {
                queue.Enqueue(package);
                while (queue.Count > 0)
                {
                    var workPackage = queue.Dequeue();
                    if (addedPackages.ContainsKey(workPackage.Id))
                    {
                        continue;
                    }

                    addedPackages.Add(workPackage.Id, workPackage);

                    foreach (var name in workPackage.GetNames())
                    {
                        if (!addedPackagesByNames.TryGetValue(name, out LinkedList<IPackage> collection))
                        {
                            collection = new LinkedList<IPackage>();
                            addedPackagesByNames.Add(name, collection);
                        }

                        collection.AddLast(workPackage);
                    }

                    foreach (var link in workPackage.GetRequires())
                    {
                        if (ignorePlatformRequest && Regex.IsMatch(link.GetTarget(), RepositoryPlatform.RegexPlatform))
                        {
                            continue;
                        }

                        var possibleRequires = pool.WhatProvides(link.GetTarget(), link.GetConstraint());
                        rules.Add(CreateRuleRequire(workPackage, possibleRequires, Reason.PackageRequire, link), RuleType.Package);
                        Array.ForEach(possibleRequires, queue.Enqueue);
                    }

                    var packageName = workPackage.GetName();
                    var obsoleteProviders = pool.WhatProvides(packageName);
                    foreach (var provider in obsoleteProviders)
                    {
                        if (provider == workPackage)
                        {
                            continue;
                        }

                        if (workPackage is PackageAlias packageAlias
                            && packageAlias.GetAliasOf() == provider)
                        {
                            rules.Add(CreateRuleRequire(workPackage, new[] { provider }, Reason.PackageAlias, workPackage), RuleType.Package);
                        }
                        else if (!IsObsoleteImpossibleForAlias(workPackage, provider))
                        {
                            var reason = packageName == provider.GetName() ? Reason.PackageSameName : Reason.PackageImplicitObsoletes;
                            rules.Add(CreateRule2Literals(workPackage, provider, reason, workPackage), RuleType.Package);
                        }
                    }
                }
            }
        }

        protected void AddRulesForJobs(bool ignorePlatformRequest)
        {
            foreach (var job in jobs)
            {
                if (job.Command == JobCommand.Install)
                {
                    if (!job.Fixed && ignorePlatformRequest
                        && Regex.IsMatch(job.PackageName, RepositoryPlatform.RegexPlatform))
                    {
                        continue;
                    }

                    var packages = pool.WhatProvides(job.PackageName, job.Constraint);

                    if (packages.Length <= 0)
                    {
                        continue;
                    }

                    // Install requires first.
                    AddRulesForPackages(packages, ignorePlatformRequest);
                    var rule = CreateRuleInstallOneOf(packages, Reason.JobInstall, job);
                    rules.Add(rule, RuleType.Job);
                    continue;
                }

                if (job.Command == JobCommand.Uninstall)
                {
                    // Uninstall all packages with this name including uninstalled
                    // ones to make sure none of them are picked as replacements.
                    var packages = pool.WhatProvides(job.PackageName, job.Constraint);
                    foreach (var package in packages)
                    {
                        var rule = CreateRuleUninstall(package, Reason.JobUninstall, job);
                        rules.Add(rule, RuleType.Job);
                    }
                }
            }
        }
    }
}
