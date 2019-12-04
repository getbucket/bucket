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
using Bucket.DependencyResolver;
using Bucket.DependencyResolver.Operation;
using Bucket.DependencyResolver.Policy;
using Bucket.DependencyResolver.Rules;
using Bucket.Downloader;
using Bucket.EventDispatcher;
using Bucket.Exception;
using Bucket.Installer;
using Bucket.IO;
using Bucket.Package;
using Bucket.Package.Dumper;
using Bucket.Package.Loader;
using Bucket.Plugin;
using Bucket.Repository;
using Bucket.Script;
using Bucket.Semver;
using Bucket.Semver.Constraint;
using Bucket.Util;
using GameBox.Console;
using GameBox.Console.EventDispatcher;
using GameBox.Console.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SException = System.Exception;

namespace Bucket
{
    /// <summary>
    /// Bucket installer for installing require pakcages.
    /// </summary>
    public class BucketInstaller
    {
        private readonly Bucket bucket;
        private readonly IIO io;
        private readonly Config config;
        private readonly IPackageRoot packageRoot;
        private readonly DownloadManager downloadManager;
        private readonly RepositoryManager repositoryManager;
        private readonly InstallationManager installationManager;
        private readonly IEventDispatcher eventDispatcher;
        private readonly Locker locker;
        private readonly IDictionary<JobCommand, (string Pre, string Post)> jobMapPackageEvents
            = new Dictionary<JobCommand, (string Pre, string Post)>
        {
            { JobCommand.Install, (ScriptEvents.PrePackageInstall, ScriptEvents.PostPackageInstall) },
            { JobCommand.Update, (ScriptEvents.PrePackageUpdate, ScriptEvents.PostPackageUpdate) },
            { JobCommand.Uninstall, (ScriptEvents.PrePackageUninstall, ScriptEvents.PostPackageUninstall) },
        };

        private IRepository additionalRepositoryInstalled;
        private bool update = false;
        private bool dryRun = false;
        private bool verbose = false;
        private bool devMode = true;
        private bool runScripts = true;
        private bool executeOperations = true;
        private bool writeLock = true;
        private bool skipSuggest = false;
        private bool preferSource = false;
        private bool preferDist = false;
        private bool preferStable = false;
        private bool preferLowest = false;
        private bool ignorePlatformRequirements = false;
        private ReporterSuggestedPackages reporterSuggestedPackages;
        private ISet<string> updateWhitelist;
        private bool whitelistAllDependencies = false;
        private bool whitelistTransitiveDependencies = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="BucketInstaller"/> class.
        /// </summary>
        public BucketInstaller(IIO io, Bucket bucket)
        {
            this.io = io;
            this.bucket = bucket;
            config = bucket.GetConfig();
            packageRoot = bucket.GetPackage();
            downloadManager = bucket.GetDownloadManager();
            repositoryManager = bucket.GetRepositoryManager();
            installationManager = bucket.GetInstallationManager();
            eventDispatcher = bucket.GetEventDispatcher();
            locker = bucket.GetLocker();
        }

        private enum ProcessTask
        {
            ForceUpdate,
            ForceLinks,
        }

        /// <summary>
        /// Run installation (or update).
        /// </summary>
        /// <returns>0 on success or a positive error code on failure.</returns>
        public virtual int Run()
        {
            // Disable GC to save CPU cycles. the GC can spend quite some time
            // walking the tree of references looking for stuff to collect
            // while there is nothing to collect.
            var noGC = GC.TryStartNoGCRegion(128 * 1024 * 1024, true);

            try
            {
                // Force update if there is no lock file present.
                if (!update && !locker.IsLocked())
                {
                    update = true;
                }

                InitializeDryRun();

                if (runScripts)
                {
                    var devModeValue = devMode ? 0 : 1;
                    Terminal.SetEnvironmentVariable(EnvironmentVariables.BucketDevMode, devModeValue);

                    var eventName = update ? ScriptEvents.PreUpdateCMD : ScriptEvents.PreInstallCMD;
                    eventDispatcher.Dispatch(this, new ScriptEventArgs(eventName, bucket, io, devMode));
                }

                downloadManager.SetPreferSource(preferSource)
                               .SetPreferDist(preferDist);

                var repositoryLocal = repositoryManager.GetLocalInstalledRepository();
                var repositoryPlatform = new RepositoryPlatform(
                                            update ? packageRoot.GetPlatforms() : locker.GetPlatforms());
                var repositoryInstalled = CreateRepositoryInstalled(repositoryLocal, repositoryPlatform);
                var rootAliases = GetPackageRootAliases();

                AliasPlatformPackages(repositoryPlatform, rootAliases);

                if (reporterSuggestedPackages == null)
                {
                    reporterSuggestedPackages = new ReporterSuggestedPackages(io);
                }

                IPackage[] devPackages;
                try
                {
                    int returnValue;
                    (returnValue, devPackages) = DoRun(repositoryLocal, repositoryInstalled, repositoryPlatform, rootAliases);
                    if (returnValue != 0)
                    {
                        return returnValue;
                    }
                }
                finally
                {
                    if (executeOperations)
                    {
                        installationManager.Notify(io, config);
                    }
                }

                ReportSuggested(repositoryInstalled);
                WarningDeprecated(repositoryLocal.GetPackages());
                WriteLockFile(repositoryLocal, devPackages, rootAliases);
                EnsureBinariesPresence(repositoryLocal);

                if (runScripts)
                {
                    var eventName = update ? ScriptEvents.PostUpdateCMD : ScriptEvents.PostInstallCMD;
                    eventDispatcher.Dispatch(this, new ScriptEventArgs(eventName, bucket, io, devMode));
                }
            }
            finally
            {
                if (noGC)
                {
                    GC.EndNoGCRegion();
                }
            }

            return ExitCodes.Normal;
        }

        /// <summary>
        /// Set whether is update packages.
        /// </summary>
        public BucketInstaller SetUpdate(bool update = true)
        {
            this.update = update;
            return this;
        }

        /// <summary>
        /// Set whether is dry run.
        /// </summary>
        public BucketInstaller SetDryRun(bool dryRun = true)
        {
            this.dryRun = dryRun;
            return this;
        }

        /// <summary>
        /// Set whether is verbose.
        /// </summary>
        public BucketInstaller SetVerbose(bool verbose = true)
        {
            this.verbose = verbose;
            return this;
        }

        /// <summary>
        /// Set whether is run scripts.
        /// </summary>
        public BucketInstaller SetRunScripts(bool runScripts = true)
        {
            this.runScripts = runScripts;
            return this;
        }

        /// <summary>
        /// Set whether the operations (package install, update and removal) be executed on disk.
        /// </summary>
        public BucketInstaller SetExecuteOperations(bool executeOperations = true)
        {
            this.executeOperations = executeOperations;
            return this;
        }

        /// <summary>
        /// Set whether is write the lock file.
        /// </summary>
        public BucketInstaller SetWriteLock(bool writeLock = true)
        {
            this.writeLock = writeLock;
            return this;
        }

        /// <summary>
        /// Set whether is skip the suggests.
        /// </summary>
        public BucketInstaller SetSkipSuggest(bool skipSuggest = true)
        {
            this.skipSuggest = skipSuggest;
            return this;
        }

        /// <summary>
        /// Set whether enables dev packages.
        /// </summary>
        public BucketInstaller SetDevMode(bool devMode = true)
        {
            this.devMode = devMode;
            return this;
        }

        /// <summary>
        /// Set whether is prefer source installation.
        /// </summary>
        public BucketInstaller SetPreferSource(bool preferSource = true)
        {
            this.preferSource = preferSource;
            return this;
        }

        /// <summary>
        /// Set whether is prefer dist installation.
        /// </summary>
        public BucketInstaller SetPreferDist(bool preferDist = true)
        {
            this.preferDist = preferDist;
            return this;
        }

        /// <summary>
        /// Set whether is prefer stable.
        /// </summary>
        public BucketInstaller SetPreferStable(bool preferStable = true)
        {
            this.preferStable = preferStable;
            return this;
        }

        /// <summary>
        /// Set whether is prefer lowest version.
        /// </summary>
        public BucketInstaller SetPreferLowest(bool preferLowest = true)
        {
            this.preferLowest = preferLowest;
            return this;
        }

        /// <summary>
        /// set ignore Platform Package requirements.
        /// </summary>
        public BucketInstaller SetIgnorePlatformRequirements(bool ignorePlatformRequirements)
        {
            this.ignorePlatformRequirements = ignorePlatformRequirements;
            return this;
        }

        /// <summary>
        /// Set the a reporter to report the suggested packages.
        /// </summary>
        public BucketInstaller SetReporterSuggestedPackages(ReporterSuggestedPackages reporterSuggestedPackages)
        {
            this.reporterSuggestedPackages = reporterSuggestedPackages;
            return this;
        }

        /// <summary>
        /// Set the additional repository in installed repository.
        /// </summary>
        public BucketInstaller SetAdditionalRepositoryInstalled(IRepository additionalRepositoryInstalled)
        {
            this.additionalRepositoryInstalled = additionalRepositoryInstalled;
            return this;
        }

        /// <summary>
        /// Restrict the update operation to a few packages, all other
        /// packages that are already installed will be kept at their
        /// current version.
        /// </summary>
        /// <remarks>A non-null value will mean that the whitelist mechanism is in effect.</remarks>
        public BucketInstaller SetUpdateWhitelist(ISet<string> updateWhitelist)
        {
            this.updateWhitelist = updateWhitelist;
            return this;
        }

        /// <summary>
        /// Should all dependencies of whitelisted packages be updated recursively.
        /// </summary>
        /// <remarks>
        /// This will whitelist any dependencies of the whitelisted packages, including
        /// those defined in the root package.
        /// </remarks>
        public BucketInstaller SetWhitelistAllDependencies(bool whitelistAllDependencies = true)
        {
            this.whitelistAllDependencies = whitelistAllDependencies;
            return this;
        }

        /// <summary>
        /// Should dependencies of whitelisted packages (but not direct dependencies) be updated.
        /// </summary>
        public BucketInstaller SetWhitelistTransitiveDependencies(bool whitelistTransitiveDependencies = true)
        {
            this.whitelistTransitiveDependencies = whitelistTransitiveDependencies;
            return this;
        }

        /// <summary>
        /// Disables plugins.
        /// </summary>
        /// <remarks>
        /// Call this if you want to ensure that third-party code never gets
        /// executed.The default is to automatically install, and execute custom
        /// third-party installers.
        /// </remarks>
        public BucketInstaller DisablePlugins()
        {
            installationManager.DisablePlugins();
            return this;
        }

        /// <summary>
        /// Execute the installer.
        /// </summary>
        /// <returns>With the exit code and an array of dev packages on update, or null on install.</returns>
        protected virtual (int returnValue, IPackage[] devPackages) DoRun(
            IRepositoryInstalled repositoryLocal,
            RepositoryComposite repositoryInstalled,
            RepositoryPlatform repositoryPlatform,
            ConfigAlias[] rootAliases)
        {
            IRepository repositoryLocked = null;
            IOperation[] operations;
            var repositories = Array.Empty<IRepository>();
            var devPackages = Array.Empty<IPackage>();

            // initialize locked repo if we are installing from lock or in a partial update
            // and a lock file is present as we need to force install non-whitelisted lock file
            // packages in that case
            if (!update || (!updateWhitelist.Empty() && locker.IsLocked()))
            {
                try
                {
                    repositoryLocked = locker.GetLockedRepository(devMode);
                }
                catch (RuntimeException) when (packageRoot.GetRequiresDev().Empty())
                {
                    repositoryLocked = locker.GetLockedRepository();
                }
            }

            WhitelistUpdateDependencies(
                repositoryLocked ?? repositoryLocal,
                packageRoot.GetRequires(),
                packageRoot.GetRequiresDev());

            io.WriteError("<info>Loading bucket repositories with package information</info>");

            var policy = CreatePolicy();
            var pool = CreatePool(update ? null : repositoryLocked);

            pool.AddRepository(repositoryInstalled, rootAliases);

            if (update)
            {
                repositories = repositoryManager.GetRepositories();
                foreach (var repository in repositories)
                {
                    pool.AddRepository(repository, rootAliases);
                }
            }

            // Add the locked repository after the others in case we are doing a
            // partial update so missing packages can be found there still.
            // For installs from lock it's the only one added so it is first.
            if (repositoryLocked != null)
            {
                pool.AddRepository(repositoryLocked, rootAliases);
            }

            var request = CreateRequest(packageRoot, repositoryPlatform);

            if (update)
            {
                PrepareUpdateRequest(request, repositoryLocal, repositoryInstalled, pool);
            }
            else
            {
                PrepareInstallRequest(request, repositoryLocked, rootAliases);
            }

            // force dev mode packages to have the latest links if we
            // update or install from a (potentially new) lock.
            ProcessDevPackages(repositoryLocal, pool, policy, repositories, repositoryInstalled, repositoryLocked, ProcessTask.ForceLinks);

            var installerEventArgs = new InstallerEventArgs(InstallerEvents.PreDependenciesSolving, bucket, io, devMode, policy, pool, repositoryInstalled, request, Array.Empty<IOperation>());
            eventDispatcher.Dispatch(this, installerEventArgs);

            var solver = new Solver(policy, pool, repositoryInstalled, io);
            int ruleSetSize;
            try
            {
                operations = solver.Solve(request, ignorePlatformRequirements);
                ruleSetSize = solver.RuleSetSize;
                solver = null;
            }
            catch (SolverProblemsException ex)
            {
                io.WriteError("<error>Your requirements could not be resolved to an installable set of packages.</error>", verbosity: Verbosities.Quiet);
                io.WriteError(ex.Message);

                if (update && !devMode)
                {
                    io.WriteError("<warning>Running update with --no-dev does not mean require-dev is ignored, it just means the packages will not be installed. If dev requirements are blocking the update you have to resolve those problems.</warning>", verbosity: Verbosities.Quiet);
                }

                return (Math.Max(ExitCodes.GeneralException, ex.ExitCode), devPackages);
            }

            // force dev packages to be updated if we update or install from a (potentially new) lock
            operations = ProcessDevPackages(repositoryLocal, pool, policy, repositories, repositoryInstalled, repositoryLocked, ProcessTask.ForceUpdate, operations);

            installerEventArgs = new InstallerEventArgs(InstallerEvents.PostDependenciesSolving, bucket, io, devMode, policy, pool, repositoryInstalled, request, operations);
            eventDispatcher.Dispatch(this, installerEventArgs);

            io.WriteError($"Analyzed {pool.Count} packages to resolve dependencies", verbosity: Verbosities.Verbose);
            io.WriteError($"Analyzed {ruleSetSize} rules to resolve dependencies", verbosity: Verbosities.Verbose);

            if (operations.Length <= 0)
            {
                io.WriteError("Nothing to install or update");
            }

            operations = MovePluginsToFront(operations);
            operations = MoveUninstallsToFront(operations);

            if (update)
            {
                devPackages = ExtractDevPackages(operations, repositoryLocal, repositoryPlatform, rootAliases);
                if (!devMode)
                {
                    operations = FilterDevPackageOperations(devPackages, operations, repositoryLocal);
                }
            }

            DebugOperationInformation(operations);

            ExectureOperations(operations, repositoryLocal, policy, pool, repositoryInstalled, request);

            if (executeOperations)
            {
                // force source/dist urls to be updated for all packages.
                ProcessPackageUrls(pool, policy, repositoryLocal, repositories);
                repositoryLocal.Write();
            }

            return (ExitCodes.Normal, devPackages);
        }

        /// <summary>
        /// Whether it is a website address with clearly known results.
        /// other urls this is ambiguous and could result in bad outcomes.
        /// </summary>
        private static bool IsCategoricUri(string uri)
        {
            return !string.IsNullOrEmpty(uri) && Regex.IsMatch(uri, @"^https?://(?:(api\.)?github\.com|(?:www\.)?gitlab\.com|(?:www\.)?example\.com)/", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Find preferred package literals in similar packages (name/version) from all repositories .
        /// </summary>
        private static int[] FindSimilarPackagePreferredLiterals(IPackage package, Pool pool, IPolicy policy, IRepository[] repositories)
        {
            var matches = pool.WhatProvides(package.GetName(), new Constraint("=", package.GetVersion()));
            var matchedPackageliterals = new List<int>();
            foreach (var match in matches)
            {
                // skip local packages and skip providers/replacers.
                if (!Array.Exists(repositories, (repository) => repository == match.GetRepository()) ||
                    match.GetName() != package.GetName())
                {
                    continue;
                }

                matchedPackageliterals.Add(match.Id);
            }

            if (matchedPackageliterals.Count <= 0)
            {
                return Array.Empty<int>();
            }

            return policy.SelectPreferredPackages(pool, new Dictionary<int, IPackage>(), matchedPackageliterals.ToArray());
        }

        private static void UpdateInstallReferences(IPackage package, string reference)
        {
            if (string.IsNullOrEmpty(reference))
            {
                return;
            }

            package.SetSourceReference(reference);

            if (IsCategoricUri(package.GetDistUri()))
            {
                package.SetDistReference(reference);
                package.SetDistUri(Regex.Replace(package.GetDistUri(), "(?<=/|sha=)[a-f0-9]{40}(?=/|$)", reference, RegexOptions.IgnoreCase));
            }
            else if (!string.IsNullOrEmpty(package.GetDistReference()))
            {
                // update the dist reference if there was one, but if none was provided ignore it.
                package.SetDistReference(reference);
            }
        }

        private void PrepareUpdateRequest(Request request, IRepositoryInstalled repositoryLocal, IRepository repositoryInstalled, Pool pool)
        {
            // remove unstable packages from the repositoryLocal if they
            // don't match the current stability settings.
            var removedUnstablePackages = new HashSet<string>();
            foreach (var package in repositoryLocal.GetPackages())
            {
                if (!pool.IsPackageAcceptable(package.GetStability(), package.GetNames())
                    && installationManager.IsPackageInstalled(repositoryLocal, package))
                {
                    removedUnstablePackages.Add(package.GetName());
                    request.Uninstall(package.GetName(), new Constraint("=", package.GetVersion()));
                }
            }

            io.WriteError($"<info>Updating dependencies{GetDevModePrompt()}</info>");

            request.UpdateAll();

            var links = Arr.Merge(packageRoot.GetRequires(), packageRoot.GetRequiresDev());
            foreach (var link in links)
            {
                request.Install(link.GetTarget(), link.GetConstraint());
            }

            // if the updateWhitelist is enabled, packages not in it are also fixed
            // to the version specified in the lock, or their currently installed version
            if (updateWhitelist == null)
            {
                return;
            }

            var currentPackages = GetCurrentPackages(repositoryInstalled);

            // collect packages to fixate from root requirements as well as installed packages
            var candidates = new HashSet<string>();
            var rootRequires = new Dictionary<string, Link>();
            foreach (var link in links)
            {
                candidates.Add(link.GetTarget());
                rootRequires[link.GetTarget()] = link;
            }

            foreach (var package in currentPackages)
            {
                candidates.Add(package.GetName());
            }

            // fix them to the version in lock (or currently installed) if they are not updateable
            foreach (var candidate in candidates)
            {
                foreach (var currentPackage in currentPackages)
                {
                    if (currentPackage.GetName() != candidate)
                    {
                        continue;
                    }

                    if (IsUpdateable(currentPackage) || removedUnstablePackages.Contains(currentPackage.GetName()))
                    {
                        break;
                    }

                    var constraint = new Constraint("=", currentPackage.GetVersion());
                    var description = locker.IsLocked() ? "(locked at" : "(installed at";
                    var requiredAt = rootRequires.TryGetValue(candidate, out Link link) ?
                        $", required as {link.GetPrettyConstraint()}" : string.Empty;
                    constraint.SetPrettyString($"{description} {currentPackage.GetVersionPretty()}{requiredAt})");
                    request.Install(currentPackage.GetName(), constraint);
                    break;
                }
            }
        }

        private void PrepareInstallRequest(Request request, IRepository repositoryLocked, ConfigAlias[] rootAliases)
        {
            io.WriteError($"<info>Installing dependencies{GetDevModePrompt()} from lock file</info>");

            if (!locker.IsFresh())
            {
                io.WriteError(
                    "<warning>Warning: The lock file is not up to date with the latest changes in bucket.json. You may be getting outdated dependencies. Run update to update them.</warning>",
                    verbosity: Verbosities.Quiet);
            }

            foreach (var package in repositoryLocked.GetPackages())
            {
                var version = package.GetVersion();
                var alias = ConfigAlias.FindAlias(rootAliases, package.GetName(), version);
                if (alias != null)
                {
                    version = alias.AliasNormalized;
                }

                var constraint = new Constraint("=", version);
                constraint.SetPrettyString(package.GetVersionPretty());
                request.Install(package.GetName(), constraint);
            }

            foreach (var link in locker.GetPlatformRequirements())
            {
                request.Install(link.GetTarget(), link.GetConstraint());
            }
        }

        /// <summary>
        /// Processing packages marked by developers.
        /// </summary>
        /// <param name="repositories">An array of all repositories, except local one.</param>
        private IOperation[] ProcessDevPackages(
            IRepositoryWriteable repositoryLocal,
            Pool pool,
            IPolicy policy,
            IRepository[] repositories,
            IRepository repositoryInstalled,
            IRepository repositoryLocked,
            ProcessTask task,
            IList<IOperation> operations = null)
        {
            if (task == ProcessTask.ForceUpdate && operations == null)
            {
                throw new UnexpectedException("Missing operations argument");
            }

            if (task == ProcessTask.ForceLinks)
            {
                operations = new List<IOperation>();
            }
            else
            {
                operations = new List<IOperation>(operations ?? Array.Empty<IOperation>());
            }

            Guard.Requires<UnexpectedException>(operations != null, $"{nameof(operations)} should not be null.");

            IPackage[] currentPackages = null;
            if (update && updateWhitelist != null)
            {
                currentPackages = GetCurrentPackages(repositoryInstalled);
            }

            void ReplaceLinks(IPackage source, IPackage replacement)
            {
                if (!(source is Package.Package package))
                {
                    throw new NotSupportedException("Can only be set for packages based on the Bucket package object.");
                }

                package.SetRequires(replacement.GetRequires());
                package.SetConflicts(replacement.GetConflicts());
                package.SetProvides(replacement.GetProvides());
                package.SetReplaces(replacement.GetReplaces());
            }

            bool IsNotSameReference(IPackage source, IPackage target)
            {
                return (!string.IsNullOrEmpty(target.GetSourceReference()) && source.GetSourceReference() != target.GetSourceReference())
                        || (!string.IsNullOrEmpty(target.GetDistReference()) && source.GetDistReference() != target.GetDistReference());
            }

            bool DoTask(IPackage source, IPackage target)
            {
                if (task == ProcessTask.ForceLinks)
                {
                    ReplaceLinks(source, target);
                    return true;
                }
                else if (task == ProcessTask.ForceUpdate && IsNotSameReference(source, target))
                {
                    operations.Add(new OperationUpdate(source, target));
                    return true;
                }

                return false;
            }

            foreach (var package in repositoryLocal.GetCanonicalPackages())
            {
                // skip no-dev packages.
                if (!package.IsDev)
                {
                    goto skip;
                }

                // skip packages that will be updated/uninstalled
                foreach (var operation in operations)
                {
                    if ((operation is OperationUpdate operationUpdate && operationUpdate.GetInitialPackage().Equals(package))
                         || (operation is OperationUninstall operationUninstall && operationUninstall.GetPackage().Equals(package)))
                    {
                        goto skip;
                    }
                }

                if (update)
                {
                    // skip package if the whitelist is enabled and it is not in it.
                    if (updateWhitelist != null && !IsUpdateable(package))
                    {
                        Guard.Requires<UnexpectedException>(currentPackages != null, $"{nameof(currentPackages)} should not be null.");

                        // check if non-updateable packages are out of date compared to the
                        // lock file to ensure we don't corrupt it.
                        foreach (var currentPackage in currentPackages)
                        {
                            if (!currentPackage.IsDev ||
                                currentPackage.GetName() != package.GetName() ||
                                currentPackage.GetVersion() != package.GetVersion())
                            {
                                continue;
                            }

                            DoTask(package, currentPackage);
                            break;
                        }

                        goto skip;
                    }

                    // select preferred package according to policy rules.
                    var prefferedPackagesLiterals = FindSimilarPackagePreferredLiterals(package, pool, policy, repositories);
                    if (prefferedPackagesLiterals.Length > 0)
                    {
                        var newPackage = pool.GetPackageByLiteral(prefferedPackagesLiterals[0]);
                        if (DoTask(package, newPackage))
                        {
                            goto skip;
                        }
                    }

                    if (task != ProcessTask.ForceUpdate)
                    {
                        goto skip;
                    }

                    // force installed package to update to referenced version
                    // in root package if it does not match the installed version.
                    var references = packageRoot.GetReferences();
                    if (references.TryGetValue(package.GetName(), out string reference) && reference != package.GetSourceReference())
                    {
                        // changing the source ref to update to will be handled in the operations loop.
                        operations.Add(new OperationUpdate(package, (IPackage)package.Clone()));
                    }

                    goto skip;
                }

                // force update to locked version if it does not match the installed version.
                foreach (var lockedPackage in repositoryLocked.FindPackages(package.GetName()))
                {
                    if (!lockedPackage.IsDev || lockedPackage.GetVersion() != package.GetVersion())
                    {
                        continue;
                    }

                    DoTask(package, lockedPackage);
                    break;
                }

            skip:
                {
                    // noop.
                }
            }

            return operations.ToArray();
        }

        private string GetDevModePrompt()
        {
            return devMode ? " (including require-dev)" : string.Empty;
        }

        /// <summary>
        /// Find deprecated packages and warn user.
        /// </summary>
        private void WarningDeprecated(IEnumerable<IPackage> packages)
        {
            foreach (var package in packages)
            {
                if (!(package is IPackageComplete packageComplete) || !packageComplete.IsDeprecated)
                {
                    continue;
                }

                var replacement = "No replacement was suggested";
                if (!string.IsNullOrEmpty(packageComplete.GetReplacementPackage()))
                {
                    replacement = $"Use {packageComplete.GetReplacementPackage()} instead";
                }

                io.WriteError($"<warning>Package {package.GetNamePretty()} is deprecated, you should avoid using it. {replacement}.</warning>");
            }
        }

        /// <summary>
        /// Report the suggested packages.
        /// </summary>
        private void ReportSuggested(IRepository repositoryInstalled)
        {
            // display suggestions if we're in dev mode.
            if (devMode && !skipSuggest)
            {
                reporterSuggestedPackages.Display(repositoryInstalled);
            }
        }

        /// <summary>
        /// Write the lock file.
        /// </summary>
        private void WriteLockFile(IRepositoryInstalled repositoryLocal, IPackage[] devPackages, ConfigAlias[] aliases)
        {
            if (!update || !writeLock)
            {
                return;
            }

            repositoryLocal.Reload();
            var requirePackages = Arr.Difference(repositoryLocal.GetCanonicalPackages(), devPackages);
            var builder = locker.BeginSetLockData(requirePackages)
                              .SetPackagesDev(devPackages)
                              .SetAliases(aliases)
                              .SetMinimumStability(packageRoot.GetMinimumStability())
                              .SetStabilityFlags(packageRoot.GetStabilityFlags())
                              .SetPreferStable(preferStable || (packageRoot?.IsPreferStable ?? false))
                              .SetPreferLowest(preferLowest)
                              .SetPlatforms(packageRoot.GetPlatforms());

            if (builder.Save())
            {
                io.WriteError("<info>Writing lock file</info>");
            }
        }

        /// <summary>
        /// Make sure binaries are installed for all installed package.
        /// </summary>
        private void EnsureBinariesPresence(IRepositoryInstalled repositoryLocal)
        {
            if (!executeOperations)
            {
                return;
            }

            foreach (var package in repositoryLocal.GetPackages())
            {
                installationManager.EnsureBinariesPresence(package);
            }

            // todo: path need optimization.
            var vendorDir = config.Get(Settings.VendorDir);
            vendorDir = Path.Combine(Environment.CurrentDirectory, vendorDir).TrimEnd('/', '\\');
            if (Directory.Exists(vendorDir))
            {
                try
                {
                    Directory.SetLastWriteTimeUtc(vendorDir, DateTime.UtcNow);
                }
#pragma warning disable CA1031
                catch (SException ex)
#pragma warning restore CA1031
                {
                    io.WriteError($"Touch folder \"{vendorDir}\" failed: {ex.Message}");
                }
            }
        }

        private void InitializeDryRun()
        {
            if (!dryRun)
            {
                return;
            }

            SetVerbose();
            SetRunScripts(false);
            SetExecuteOperations(false);
            SetWriteLock(false);
            installationManager.AddInstaller(new InstallerNoop());
            MockLocalInstalledRepositories(repositoryManager);
        }

        /// <summary>
        /// Replace local repositories with <see cref="RepositoryArrayInstalled"/> instances.
        /// </summary>
        /// <remarks>This is to prevent any accidental modification of the existing repos on disk.</remarks>
        private void MockLocalInstalledRepositories(RepositoryManager manager)
        {
            var packages = new Dictionary<string, IPackage>();
            foreach (var package in manager.GetLocalInstalledRepository().GetPackages())
            {
                packages.Add(package.GetNameUnique(), (IPackage)package.Clone());
            }

            foreach (var item in packages.ToArray())
            {
                if (!(item.Value is PackageAlias packageAlias))
                {
                    continue;
                }

                var alias = packageAlias.GetAliasOf().GetNameUnique();
                packages[item.Key] = new PackageAlias(packages[alias], packageRoot.GetVersion(), packageRoot.GetVersionPretty());
            }

            manager.SetLocalInstalledRepository(new RepositoryArrayInstalled(packages.Values));
        }

        private RepositoryComposite CreateRepositoryInstalled(IRepositoryWriteable repositoryInstalled, RepositoryPlatform repositoryPlatform)
        {
            // clone root package to have one in the installed repo that does not require anything
            // we don't want it to be uninstallable, but its requirements should not conflict
            // with the lock file for example
            var packageRootClone = (IPackageRoot)packageRoot.Clone();
            packageRootClone.SetRequires(Array.Empty<Link>());
            packageRootClone.SetRequiresDev(Array.Empty<Link>());

            var repository = new RepositoryComposite(
                repositoryInstalled,
                new RepositoryArrayInstalled(new[] { packageRootClone }),
                repositoryPlatform);

            if (additionalRepositoryInstalled != null)
            {
                repository.AddRepository(additionalRepositoryInstalled);
            }

            return repository;
        }

        private ConfigAlias[] GetPackageRootAliases()
        {
            return update ? packageRoot.GetAliases() : locker.GetAliases();
        }

        private void AliasPlatformPackages(RepositoryPlatform repositoryPlatform, IEnumerable<ConfigAlias> aliases)
        {
            foreach (var alias in aliases)
            {
                var packages = repositoryPlatform.FindPackages(alias.Package, alias.Version);
                foreach (var package in packages)
                {
                    var packageAlias = new PackageAlias(package, alias.AliasNormalized, alias.Alias);
                    packageAlias.SetRootPackageAlias();
                    repositoryPlatform.AddPackage(packageAlias);
                }
            }
        }

        /// <summary>
        /// Adds all dependencies of the update whitelist to the whitelist, too.
        /// </summary>
        /// <param name="localOrLockRepository">
        /// Use the locked repo if available, otherwise installed repo will do As
        /// we want the most accurate package list to work with, and installed.
        /// </param>
        /// <param name="rootRequires">An array of links to packages in require of the root package.</param>
        /// <param name="rootDevRequires">An array of links to packages in require-dev of the root package.</param>
        private void WhitelistUpdateDependencies(IRepository localOrLockRepository, Link[] rootRequires, Link[] rootDevRequires)
        {
            if (updateWhitelist == null)
            {
                return;
            }

            rootRequires = Arr.Merge(rootRequires, rootDevRequires);
            var skipPackages = new HashSet<string>();

            if (!whitelistAllDependencies)
            {
                foreach (var require in rootRequires)
                {
                    skipPackages.Add(require.GetTarget());
                }
            }

            var seen = new HashSet<int>();
            var packageQueue = new Queue<IPackage>();
            var pool = new Pool(Stabilities.Dev);
            pool.AddRepository(localOrLockRepository);

            var rootRequiredPackageNames = Arr.Map(rootRequires, (require) => require.GetTarget());
            foreach (var packageName in updateWhitelist.ToArray())
            {
                Guard.Requires<UnexpectedException>(packageQueue.Count == 0, "Packet queue must be empty.");

                var nameMatchesRequiredPackage = false;
                var requirePackages = new List<IPackage>(pool.WhatProvides(packageName));

                // check if the name is a glob pattern that did not match directly.
                if (requirePackages.Count == 0)
                {
                    var whitelistPatternSearchRegex = BasePackage.PackageNameToRegexPattern(packageName, "^{0}$");
                    foreach (var installedPackage in localOrLockRepository.Search(whitelistPatternSearchRegex))
                    {
                        requirePackages.AddRange(pool.WhatProvides(installedPackage.GetName()));
                    }

                    // add root requirements which match the whitelisted name/pattern.
                    var whitelistPatternRegex = BasePackage.PackageNameToRegexPattern(packageName);
                    foreach (var rootRequiredPackageName in rootRequiredPackageNames)
                    {
                        if (Regex.IsMatch(rootRequiredPackageName, whitelistPatternRegex))
                        {
                            nameMatchesRequiredPackage = true;
                            break;
                        }
                    }
                }

                if (requirePackages.Count <= 0 && !nameMatchesRequiredPackage && !Array.Exists(new[] { "nothing", "lock", "mirror" }, (value) => value == packageName))
                {
                    io.WriteError($"<warning>Package {packageName} listed for update is not installed. Ignoring.</warning>");
                }

                foreach (var requirePackage in requirePackages)
                {
                    packageQueue.Enqueue(requirePackage);
                }

                while (packageQueue.Count > 0)
                {
                    var package = packageQueue.Dequeue();
                    if (!seen.Add(package.Id))
                    {
                        continue;
                    }

                    updateWhitelist.Add(package.GetName());

                    if (!whitelistTransitiveDependencies && !whitelistAllDependencies)
                    {
                        continue;
                    }

                    var requires = package.GetRequires();
                    foreach (var require in requires)
                    {
                        foreach (var requirePackage in pool.WhatProvides(require.GetTarget()))
                        {
                            if (updateWhitelist.Contains(requirePackage.GetName()))
                            {
                                continue;
                            }

                            if (skipPackages.Contains(requirePackage.GetName()) && !Regex.IsMatch(requirePackage.GetName(), BasePackage.PackageNameToRegexPattern(packageName)))
                            {
                                io.WriteError($"<warning>Dependency \"{requirePackage.GetName()}\" is also a root requirement, but is not explicitly whitelisted. Ignoring.</warning>");
                                continue;
                            }

                            packageQueue.Enqueue(requirePackage);
                        }
                    }
                }
            }
        }

        private IPolicy CreatePolicy()
        {
            bool isPreferStable;
            bool isPreferLowest;

            if (!update)
            {
                isPreferStable = locker.GetPreferStable();
                isPreferLowest = locker.GetPreferLowest();
            }
            else
            {
                isPreferStable = preferStable || (packageRoot.IsPreferStable ?? false);
                isPreferLowest = preferLowest;
            }

            return new PolicyDefault(isPreferStable, isPreferLowest);
        }

        private Pool CreatePool(IRepository repositoryLocked = null)
        {
            Stabilities? minimumStability;
            IDictionary<string, Stabilities> stabilityFlags;
            var requires = new Dictionary<string, IConstraint>();

            if (update)
            {
                minimumStability = packageRoot.GetMinimumStability();
                stabilityFlags = packageRoot.GetStabilityFlags();
                foreach (var link in Arr.Merge(packageRoot.GetRequires(), packageRoot.GetRequiresDev()))
                {
                    requires[link.GetTarget()] = link.GetConstraint();
                }
            }
            else
            {
                minimumStability = locker.GetMinimumStability();
                stabilityFlags = locker.GetStabilityFlags();
                foreach (var package in repositoryLocked.GetPackages())
                {
                    var constraint = new Constraint("=", package.GetVersion());
                    constraint.SetPrettyString(package.GetVersionPretty());
                    requires[package.GetName()] = constraint;
                }
            }

            return new Pool(minimumStability ?? Stabilities.Stable, stabilityFlags, requires);
        }

        private Request CreateRequest(IPackageRoot packageRoot, RepositoryPlatform repositoryPlatform)
        {
            var request = new Request();

            var constraint = new Constraint("=", packageRoot.GetVersion());
            constraint.SetPrettyString(packageRoot.GetVersionPretty());
            request.Install(packageRoot.GetName(), constraint);

            var fixedPackages = repositoryPlatform.GetPackages();
            if (additionalRepositoryInstalled != null)
            {
                var additionalFixedPackages = additionalRepositoryInstalled.GetPackages();
                fixedPackages = Arr.Merge(fixedPackages, additionalFixedPackages);
            }

            // fix the version of all platform packages + additionally installed packages
            // to prevent the solver trying to remove or update those
            var provided = new Dictionary<string, IConstraint>();
            foreach (var provide in packageRoot.GetProvides())
            {
                provided[provide.GetTarget()] = provide.GetConstraint();
            }

            foreach (var package in fixedPackages)
            {
                constraint = new Constraint("=", package.GetVersion());
                constraint.SetPrettyString(package.GetVersionPretty());

                // skip platform packages that are provided by the root package
                if (package.GetRepository() != repositoryPlatform
                    || !provided.TryGetValue(package.GetName(), out IConstraint constraintProvide)
                    || !constraintProvide.Matches(constraint))
                {
                    request.Fix(package.GetName(), constraint);
                }
            }

            return request;
        }

        /// <summary>
        /// Loads the most "current" list of packages that are installed
        /// meaning from lock ideally or from installed repo as fallback.
        /// </summary>
        private IPackage[] GetCurrentPackages(IRepository repositoryInstalled)
        {
            if (!locker.IsLocked())
            {
                return repositoryInstalled.GetPackages();
            }

            try
            {
                return locker.GetLockedRepository(true).GetPackages();
            }
            catch (RuntimeException)
            {
                // fetch only non-dev packages from lock if doing a dev
                // update fails due to a previously incomplete lock file.
                return locker.GetLockedRepository().GetPackages();
            }
        }

        /// <summary>
        /// Determine if the package is in the updatable whitelist.
        /// </summary>
        private bool IsUpdateable(IPackage package)
        {
            if (updateWhitelist == null)
            {
                throw new RuntimeException($"{nameof(IsUpdateable)} should only be called when a whitelist is present.");
            }

            foreach (var whiteListedPattern in updateWhitelist)
            {
                var patternRegex = BasePackage.PackageNameToRegexPattern(whiteListedPattern);
                if (Regex.IsMatch(package.GetName(), patternRegex))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Move the plugins package operation to the front.
        /// </summary>
        /// <remarks>
        /// if your packages depend on plugins, we must be sure that those are
        /// installed / updated first; else it would lead to packages being
        /// installed multiple times in different folders, when running Bucket twice.
        /// </remarks>
        private IOperation[] MovePluginsToFront(IOperation[] operations)
        {
            var pluginNoDependencies = new LinkedList<IOperation>();
            var pluginDependencies = new LinkedList<IOperation>();
            var pluginRequires = new List<string>();
            var baseOperations = new LinkedList<IOperation>();

            Array.Reverse(operations);

            foreach (var operation in operations)
            {
                IPackage package;
                if (operation is OperationInstall operationInstall)
                {
                    package = operationInstall.GetPackage();
                }
                else if (operation is OperationUpdate operationUpdate)
                {
                    package = operationUpdate.GetTargetPackage();
                }
                else
                {
                    baseOperations.AddFirst(operation);
                    continue;
                }

                // Is this a plugin or a dependency of a plugin?
                var isPlugin = package.GetPackageType() == PluginManager.PluginType;
                if (isPlugin || package.GetNames().Intersect(pluginRequires).Any())
                {
                    // get the package's requires, but filter out any platform
                    // requirements or 'bucket-plugin-api'.
                    var requires = Arr.Filter(
                        Arr.Map(package.GetRequires(), (require) => require.GetTarget()),
                        (target) =>
                        {
                            return target != PluginManager.PluginRequire && !Regex.IsMatch(target, RepositoryPlatform.RegexPlatform);
                        });

                    // Is this a plugin with no meaningful dependencies.
                    if (isPlugin && requires.Length <= 0)
                    {
                        // plugins with no dependencies go to the very front.
                        pluginNoDependencies.AddFirst(operation);
                    }
                    else
                    {
                        // capture the requirements for this package so those
                        // packages will be moved up as well.
                        pluginRequires.AddRange(requires);

                        // move the operation to the front
                        pluginDependencies.AddFirst(operation);
                    }
                }
                else
                {
                    baseOperations.AddFirst(operation);
                }
            }

            var ret = new List<IOperation>(pluginNoDependencies.Count + pluginDependencies.Count + baseOperations.Count);
            ret.AddRange(pluginNoDependencies);
            ret.AddRange(pluginDependencies);
            ret.AddRange(baseOperations);
            return ret.ToArray();
        }

        /// <summary>
        /// Removals of packages should be executed before installations in case
        /// two packages resolve to the same path (due to custom installers).
        /// </summary>
        private IOperation[] MoveUninstallsToFront(IOperation[] operations)
        {
            var operationsUninstall = new List<IOperation>();
            var operationsOther = new List<IOperation>();

            foreach (var operation in operations)
            {
                if (operation is OperationUninstall)
                {
                    operationsUninstall.Add(operation);
                }
                else
                {
                    operationsOther.Add(operation);
                }
            }

            operationsUninstall.AddRange(operationsOther);
            return operationsUninstall.ToArray();
        }

        /// <summary>
        /// Extracts the dev packages out of the <paramref name="repositoryLocal"/>.
        /// </summary>
        /// <remarks>
        /// This works by faking the operations so we can see what the dev packages
        /// would be at the end of the operation execution. This lets us then remove
        /// the dev packages from the list of operations accordingly if we are in a
        /// --no-dev install or update.
        /// </remarks>
        /// <returns>An array of packages install in the dev mode.</returns>
        private IPackage[] ExtractDevPackages(IOperation[] operations, IRepositoryWriteable repositoryLocal, RepositoryPlatform repositoryPlatform, ConfigAlias[] rootAliases)
        {
            if (packageRoot.GetRequiresDev().Empty())
            {
                return Array.Empty<IPackage>();
            }

            var repositoryLocalTemporary = new RepositoryArrayInstalled(Arr.Map(
                                            repositoryLocal.GetPackages(), package => (IPackage)package.Clone()));
            void AddPackageInLocalTemporary(IPackage package)
            {
                if (!repositoryLocalTemporary.HasPackage(package))
                {
                    repositoryLocalTemporary.AddPackage((IPackage)package.Clone());
                }
            }

            // Faking repository after the operation is completed
            foreach (var operation in operations)
            {
                if (operation is OperationUpdate operationUpdate)
                {
                    repositoryLocalTemporary.RemovePackage(operationUpdate.GetInitialPackage());
                    AddPackageInLocalTemporary(operationUpdate.GetTargetPackage());
                    continue;
                }

                if (!(operation is BaseOperation baseOperation))
                {
                    throw new UnexpectedException($"The operation type must be inherited from {nameof(BaseOperation)} type.");
                }

                if (operation.JobCommand == JobCommand.Install || operation.JobCommand == JobCommand.MarkPackageAliasInstalled)
                {
                    AddPackageInLocalTemporary(baseOperation.GetPackage());
                    continue;
                }

                if (operation.JobCommand == JobCommand.Uninstall || operation.JobCommand == JobCommand.MarkPackageAliasUninstall)
                {
                    repositoryLocalTemporary.RemovePackage(baseOperation.GetPackage());
                    continue;
                }

                throw new UnexpectedException($"Unknown operation type: {operation.JobCommand}");
            }

            // GetCanonicalPackages will strip out the alias package,
            // we need to use loader/dumper to reload it in memory.
            repositoryLocal = new RepositoryArrayInstalled();
            var loader = new LoaderPackage(null);
            var dumper = new DumperPackage();
            foreach (var package in repositoryLocalTemporary.GetCanonicalPackages())
            {
                repositoryLocal.AddPackage(loader.Load(dumper.Dump<ConfigBucket>(package)));
            }

            var policy = CreatePolicy();
            var pool = CreatePool();
            var repositoryInstalled = CreateRepositoryInstalled(repositoryLocal, repositoryPlatform);
            pool.AddRepository(repositoryInstalled, rootAliases);

            // creating requirements request without dev requirements
            var request = CreateRequest(packageRoot, repositoryPlatform);
            request.UpdateAll();
            foreach (var link in packageRoot.GetRequires())
            {
                request.Install(link.GetTarget(), link.GetConstraint());
            }

            // solve deps to see which get removed
            var installerEventArgs = new InstallerEventArgs(InstallerEvents.PreDependenciesSolving, bucket, io, devMode, policy, pool, repositoryInstalled, request, Array.Empty<IOperation>());
            eventDispatcher.Dispatch(this, installerEventArgs);

            var solver = new Solver(policy, pool, repositoryInstalled, io);
            operations = solver.Solve(request, ignorePlatformRequirements);

            installerEventArgs = new InstallerEventArgs(InstallerEvents.PostDependenciesSolving, bucket, io, devMode, policy, pool, repositoryInstalled, request, operations);
            eventDispatcher.Dispatch(this, installerEventArgs);

            // So we can reverse the solution to get the difference
            // in non-dev and dev modes.
            var devPackages = new List<IPackage>();
            foreach (var operation in operations)
            {
                if (operation is OperationUninstall operationUninstall)
                {
                    devPackages.Add(operationUninstall.GetPackage());
                }
            }

            return devPackages.ToArray();
        }

        private IOperation[] FilterDevPackageOperations(IPackage[] devPackages, IOperation[] operations, IRepository repositoryLocal)
        {
            var finalOperations = new List<IOperation>(operations.Length);
            var packagesToSkip = new HashSet<string>();

            // non-dev install removing the installed dev package.
            foreach (var devPackage in devPackages)
            {
                packagesToSkip.Add(devPackage.GetName());
                var installedDevPackage = repositoryLocal.FindPackage(devPackage.GetName(), "*");
                if (installedDevPackage == null)
                {
                    continue;
                }

                if (installedDevPackage is PackageAlias packageAlias)
                {
                    // todo: add uninstall reason for friendly debugging.
                    finalOperations.Add(new OperationMarkPackageAliasUninstall(packageAlias));
                    installedDevPackage = packageAlias.GetAliasOf();
                }

                // todo: add uninstall reason for friendly debugging.
                finalOperations.Add(new OperationUninstall(installedDevPackage));
            }

            // skip operations applied on dev packages.
            foreach (var operation in operations)
            {
                IPackage package;
                if (operation is OperationUpdate operationUpdate)
                {
                    package = operationUpdate.GetTargetPackage();
                }
                else if (operation is BaseOperation baseOperation)
                {
                    package = baseOperation.GetPackage();
                }
                else
                {
                    throw new UnexpectedException($"Unknown operation type: {operation.JobCommand}");
                }

                if (packagesToSkip.Contains(package.GetName()))
                {
                    continue;
                }

                finalOperations.Add(operation);
            }

            return finalOperations.ToArray();
        }

        private void ExectureOperations(
            IOperation[] operations,
            IRepositoryInstalled repositoryLocal,
            IPolicy policy,
            Pool pool,
            RepositoryComposite repositoryInstalled,
            Request request)
        {
            PackageEventArgs CreatePackageEventArgs(string eventName, IOperation operation)
            {
                return new PackageEventArgs(eventName, bucket, io, devMode, policy, pool, repositoryInstalled, request, operations, operation);
            }

            bool IsAliasOperation(IOperation operation)
            {
                return operation.JobCommand == JobCommand.MarkPackageAliasInstalled || operation.JobCommand == JobCommand.MarkPackageAliasUninstall;
            }

            foreach (var operation in operations)
            {
                // collect suggestions.
                var operationInstall = operation as OperationInstall;
                if (operationInstall != null)
                {
                    reporterSuggestedPackages.AddSuggestions(operationInstall.GetPackage());
                }

                // updating, force dev packages' references if they're in root package refs.
                if (update)
                {
                    IPackage package = null;
                    var operationUpdate = operation as OperationUpdate;
                    if (operationUpdate != null)
                    {
                        package = operationUpdate.GetTargetPackage();
                    }
                    else if (operationInstall != null)
                    {
                        package = operationInstall.GetPackage();
                    }

                    if (package != null && package.IsDev)
                    {
                        var references = packageRoot.GetReferences();
                        if (references.TryGetValue(package.GetName(), out string reference))
                        {
                            UpdateInstallReferences(package, reference);
                        }
                    }

                    var targetPackage = operationUpdate?.GetTargetPackage();
                    if (targetPackage != null && targetPackage.IsDev)
                    {
                        var initialPackage = operationUpdate.GetInitialPackage();
                        if (targetPackage.GetVersion() == initialPackage.GetVersion()
                            && (string.IsNullOrEmpty(targetPackage.GetSourceReference()) || targetPackage.GetSourceReference() == initialPackage.GetSourceReference())
                            && (string.IsNullOrEmpty(targetPackage.GetDistReference()) || targetPackage.GetDistReference() == initialPackage.GetDistReference()))
                        {
                            io.WriteError($"  - Skipping update of {targetPackage.GetNamePretty()} to the same reference-locked version.", verbosity: Verbosities.Debug);
                            io.WriteError(string.Empty, verbosity: Verbosities.Debug);
                            continue;
                        }
                    }
                }

                var hasEvent = jobMapPackageEvents.TryGetValue(operation.JobCommand, out (string Pre, string Post) eventData);
                if (hasEvent && runScripts)
                {
                    eventDispatcher.Dispatch(this, CreatePackageEventArgs(eventData.Pre, operation));
                }

                // output non-alias ops when not executing operations (i.e. dry run),
                // output alias ops in debug verbosity.
                if (!executeOperations && !IsAliasOperation(operation))
                {
                    io.WriteError($"  - {operation}");
                }
                else if (io.IsDebug && IsAliasOperation(operation))
                {
                    io.WriteError($"  - {operation}");
                }

                installationManager.Execute(repositoryLocal, operation);

                DebugOperationReason(operation, pool);

                if (executeOperations || writeLock)
                {
                    repositoryLocal.Write();
                }

                if (hasEvent && runScripts)
                {
                    eventDispatcher.Dispatch(this, CreatePackageEventArgs(eventData.Post, operation));
                }
            }
        }

        private void ProcessPackageUrls(Pool pool, IPolicy policy, IRepositoryInstalled repositoryLocal, IRepository[] repositories)
        {
            if (!update)
            {
                return;
            }

            var rootReferences = packageRoot.GetReferences();
            foreach (var package in repositoryLocal.GetCanonicalPackages())
            {
                var prefferedPackagesLiterals = FindSimilarPackagePreferredLiterals(package, pool, policy, repositories);
                if (prefferedPackagesLiterals.Length <= 0)
                {
                    continue;
                }

                var newPackage = pool.GetPackageByLiteral(prefferedPackagesLiterals[0]);

                // update the dist and source URLs
                var newSourceUri = newPackage.GetSourceUri();
                var newReference = newPackage.GetSourceReference();

                if (package.IsDev &&
                    rootReferences.TryGetValue(package.GetName(), out string reference) &&
                    package.GetSourceReference() == reference)
                {
                    newReference = reference;
                }

                UpdatePackageUri(package, newSourceUri, newPackage.GetSourceType(), newReference, newPackage.GetDistUri(), newPackage.GetDistType(), newPackage.GetDistShasum());

                if (package is PackageComplete packageComplete && newPackage is PackageComplete newPackageComplete)
                {
                    if (newPackageComplete.IsDeprecated)
                    {
                        var deprecated = newPackageComplete.GetReplacementPackage();
                        if (string.IsNullOrEmpty(deprecated))
                        {
                            packageComplete.SetDeprecated(true);
                        }
                        else
                        {
                            packageComplete.SetDeprecated(deprecated);
                        }
                    }
                    else
                    {
                        packageComplete.SetDeprecated(null);
                    }
                }

                package.SetDistMirrors(newPackage.GetDistMirrors());
                package.SetSourceMirrors(newPackage.GetSourceMirrors());
            }
        }

        private void UpdatePackageUri(IPackage package, string sourceUri, string sourceType, string sourceReference, string distUri, string distType, string distShasum)
        {
            var oldSourceReference = package.GetSourceReference();
            if (package.GetSourceUri() != sourceUri)
            {
                package.SetSourceUri(sourceUri);
                package.SetSourceType(sourceType);
                package.SetSourceReference(sourceReference);
            }

            // only update dist url for github/gitlab dists as they use a
            // combination of dist url + dist reference to install but for
            // other urls this is ambiguous and could result in bad outcomes.
            if (IsCategoricUri(distUri))
            {
                package.SetDistUri(distUri);
                package.SetDistType(distType);
                package.SetDistShasum(distShasum);
                UpdateInstallReferences(package, sourceReference);
            }

            if (updateWhitelist != null && !IsUpdateable(package))
            {
                UpdateInstallReferences(package, oldSourceReference);
            }
        }

        private void DebugOperationInformation(IOperation[] operations)
        {
            if (operations.Empty())
            {
                return;
            }

            var installs = new List<string>();
            var updates = new List<string>();
            var uninstalls = new List<string>();

            foreach (var operation in operations)
            {
                if (operation is OperationInstall operationInstall)
                {
                    installs.Add($"{operationInstall.GetPackage().GetPrettyString()}:{operationInstall.GetPackage().GetVersionPrettyFull()}");
                }
                else if (operation is OperationUpdate operationUpdate)
                {
                    updates.Add($"{operationUpdate.GetTargetPackage().GetPrettyString()}:{operationUpdate.GetTargetPackage().GetVersionPrettyFull()}");
                }
                else if (operation is OperationUninstall operationUninstall)
                {
                    uninstalls.Add(operationUninstall.GetPackage().GetPrettyString());
                }
            }

            var text = new StringBuilder();
            text.Append("<info>")
                .Append($"Package operations: {installs.Count} install")
                .Append(installs.Count == 1 ? string.Empty : "s")
                .Append($", {updates.Count} update")
                .Append(updates.Count == 1 ? string.Empty : "s")
                .Append($", {uninstalls.Count} removal")
                .Append(uninstalls.Count == 1 ? string.Empty : "s")
                .Append("</info>");

            io.WriteError(text.ToString());

            if (installs.Count > 0)
            {
                io.WriteError($"Installs: {string.Join(", ", installs)}", verbosity: Verbosities.Verbose);
            }

            if (updates.Count > 0)
            {
                io.WriteError($"Updates: {string.Join(", ", updates)}", verbosity: Verbosities.Verbose);
            }

            if (uninstalls.Count > 0)
            {
                io.WriteError($"Removals: {string.Join(", ", uninstalls)}", verbosity: Verbosities.Verbose);
            }
        }

        /// <summary>
        /// Output reasons why the operation was ran, only for install/update operations.
        /// </summary>
        private void DebugOperationReason(IOperation operation, Pool pool)
        {
            if (!verbose || !io.IsVeryVerbose ||
                (operation.JobCommand != JobCommand.Update && operation.JobCommand != JobCommand.Install))
            {
                return;
            }

            var reason = operation.GetReason();
            switch (reason.GetReason())
            {
                case Reason.JobInstall:
                    io.WriteError($"    REASON: Required by the root package: {reason.GetPrettyString(pool)}");
                    io.WriteError(string.Empty);
                    break;
                case Reason.PackageRequire:
                    io.WriteError($"    REASON: {reason.GetPrettyString(pool)}");
                    io.WriteError(string.Empty);
                    break;
                default:
                    return;
            }
        }
    }
}
