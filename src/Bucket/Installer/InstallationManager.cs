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
using Bucket.Downloader.Transport;
using Bucket.Exception;
using Bucket.IO;
using Bucket.Package;
using Bucket.Repository;
using GameBox.Console.Exception;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using SException = System.Exception;

namespace Bucket.Installer
{
    /// <summary>
    /// Package installation manager.
    /// </summary>
    public class InstallationManager
    {
        private readonly LinkedList<IInstaller> installers;
        private readonly IDictionary<string, IInstaller> cache;
        private readonly IDictionary<JobCommand, ExecuteJobCommand> methods;
        private readonly IDictionary<string, IList<IPackage>> notifiablePackages;

        /// <summary>
        /// Initializes a new instance of the <see cref="InstallationManager"/> class.
        /// </summary>
        public InstallationManager()
        {
            installers = new LinkedList<IInstaller>();
            cache = new Dictionary<string, IInstaller>();
            notifiablePackages = new Dictionary<string, IList<IPackage>>();
            methods = new Dictionary<JobCommand, ExecuteJobCommand>()
            {
                { JobCommand.Install, Install },
                { JobCommand.Update, Update },
                { JobCommand.Uninstall, Uninstall },
                { JobCommand.MarkPackageAliasInstalled, MarkAliasInstalled },
                { JobCommand.MarkPackageAliasUninstall, MarkAliasUninstalled },
            };
        }

        /// <summary>
        /// Indicates the operation execution method.
        /// </summary>
        protected delegate void ExecuteJobCommand(IRepositoryInstalled repository, IOperation operation);

        /// <summary>
        /// Add a installer into installation manager.
        /// </summary>
        /// <param name="installer">The installer instance.</param>
        public virtual void AddInstaller(IInstaller installer)
        {
            installers.AddFirst(installer);
            cache.Clear();
        }

        /// <summary>
        /// Removes a installer into installation manager.
        /// </summary>
        /// <param name="installer">The installer instance.</param>
        public virtual void RemoveInstaller(IInstaller installer)
        {
            installers.Remove(installer);
            cache.Clear();
        }

        /// <summary>
        /// Returns installer for a specific package type.
        /// </summary>
        /// <param name="type">The package type.</param>
        /// <returns>The installer instance.</returns>
        /// <exception cref="InvalidArgumentException">If installer for provided type is not registered.</exception>
        public virtual IInstaller GetInstaller(string type)
        {
            type = type ?? string.Empty;
            type = type.ToLower();
            if (cache.TryGetValue(type, out IInstaller ret))
            {
                return ret;
            }

            foreach (var installer in installers)
            {
                if (installer.IsSupports(type))
                {
                    return cache[type] = installer;
                }
            }

            if (string.IsNullOrEmpty(type))
            {
                throw new InvalidArgumentException($"The default installer is not registered cannot be installed.");
            }
            else
            {
                throw new InvalidArgumentException($"Unknown installer type: {type}.");
            }
        }

        /// <summary>
        /// Checks whether provided package is installed in one of the registered installers.
        /// </summary>
        /// <param name="installedRepository">The repository in which to check.</param>
        /// <param name="package">The package instance.</param>
        /// <returns>True if the package is installed.</returns>
        public virtual bool IsPackageInstalled(IRepositoryInstalled installedRepository, IPackage package)
        {
            if (package is PackageAlias packageAlias)
            {
                return installedRepository.HasPackage(package) && IsPackageInstalled(installedRepository, packageAlias.GetAliasOf());
            }

            return GetInstaller(package.GetPackageType()).IsInstalled(installedRepository, package);
        }

        /// <summary>
        /// Executes solver operation.
        /// </summary>
        /// <param name="installedRepository">The repository in which to check.</param>
        /// <param name="operation">The operation instance.</param>
        public virtual void Execute(IRepositoryInstalled installedRepository, IOperation operation)
        {
            void WaitDownload(IPackage package, IPackage previousPackage = null)
            {
                var type = previousPackage != null ? previousPackage.GetPackageType() : package.GetPackageType();
                var installer = GetInstaller(type);
                installer.Download(package, previousPackage)?.Wait();
            }

            var method = GetExecuteMethod(operation.JobCommand);
            if (operation.JobCommand == JobCommand.Install)
            {
                if (!(operation is OperationInstall operationInstall))
                {
                    throw new UnexpectedException($"The Install operation must be {nameof(OperationInstall)} instance.");
                }

                WaitDownload(operationInstall.GetPackage());
            }
            else if (operation.JobCommand == JobCommand.Update)
            {
                if (!(operation is OperationUpdate operationUpdate))
                {
                    throw new UnexpectedException($"The update operation must be {nameof(OperationUpdate)} instance.");
                }

                WaitDownload(operationUpdate.GetTargetPackage(), operationUpdate.GetInitialPackage());
            }

            method(installedRepository, operation);
        }

        /// <summary>
        /// Gets the installation path of a package.
        /// </summary>
        /// <param name="package">The package instance.</param>
        public virtual string GetInstalledPath(IPackage package)
        {
            var installer = GetInstaller(package.GetPackageType());
            return installer.GetInstallPath(package);
        }

        /// <summary>
        /// Make sure binaries are installed for a given package.
        /// </summary>
        /// <param name="package">The package instance.</param>
        public virtual void EnsureBinariesPresence(IPackage package)
        {
            try
            {
                var installer = GetInstaller(package.GetPackageType());
                if (installer is IBinaryPresence binaryPresence)
                {
                    binaryPresence.EnsureBinariesPresence(package);
                }
            }
            catch (InvalidArgumentException)
            {
                // noop.
            }
        }

        /// <summary>
        /// Trigger notification.
        /// </summary>
        /// <param name="io">The input/output instance.</param>
        /// <param name="config">The config instance.</param>
        public virtual void Notify(IIO io, Config config)
        {
            var seen = new HashSet<string>();
            foreach (var item in notifiablePackages)
            {
                if (!Uri.TryCreate(item.Key, UriKind.RelativeOrAbsolute, out Uri uri))
                {
                    continue;
                }

                var authHeader = string.Empty;
                if (io.HasAuthentication(uri.Host))
                {
                    var (username, password) = io.GetAuthentication(uri.Host);
                    var authBytes = Encoding.UTF8.GetBytes($"{username}:{password}");
                    authHeader = $"Basic {Convert.ToBase64String(authBytes)}";
                }

                seen.Clear();
                var postData = new PostNotifyData();
                foreach (var package in item.Value)
                {
                    if (!seen.Add(package.GetName()))
                    {
                        continue;
                    }

                    postData.Downloads.Add((package.GetNamePretty(), package.GetVersion()));
                }

                using (var httpClient = HttpClientFactory.CreateHttpClient(config))
                {
                    var content = new StringContent(postData, Encoding.UTF8, "application/json");
                    if (!string.IsNullOrEmpty(authHeader))
                    {
                        content.Headers.Add("Authorization", authHeader);
                    }

                    try
                    {
                        using (var response = httpClient.PostAsync(uri.ToString(), content).Result)
                        {
                            response.EnsureSuccessStatusCode();
                        }
                    }
#pragma warning disable CA1031
                    catch (SException ex)
#pragma warning restore CA1031
                    {
                        io.WriteError($"Notify {uri.ToString()} failed: {ex.Message}", true, Verbosities.Debug);
                    }
                    finally
                    {
                        httpClient.CancelPendingRequests();
                    }
                }
            }
        }

        /// <summary>
        /// Disables plugins.
        /// </summary>
        /// <remarks>
        /// We prevent any plugins from being instantiated by simply
        /// deactivating the installer for them.This ensure that no
        /// third-party code is ever executed.
        /// </remarks>
        public virtual void DisablePlugins()
        {
            foreach (var installer in installers.ToArray())
            {
                if (installer is InstallerPlugin)
                {
                    installers.Remove(installer);
                }
            }
        }

        /// <summary>
        /// Get the specified job command's execute method.
        /// </summary>
        /// <exception cref="UnexpectedException">When job command's execute method not found.</exception>
        protected virtual ExecuteJobCommand GetExecuteMethod(JobCommand jobCommand)
        {
            if (!methods.TryGetValue(jobCommand, out ExecuteJobCommand method))
            {
                throw new UnexpectedException($"The operation is not found exectuer with type {jobCommand}.");
            }

            return method;
        }

        /// <summary>
        /// Executes install operation.
        /// </summary>
        /// <param name="installedRepository">The repository in which to check.</param>
        /// <param name="operation">The operation instance.</param>
        protected virtual void Install(IRepositoryInstalled installedRepository, IOperation operation)
        {
            if (!(operation is OperationInstall operationInstall))
            {
                throw new UnexpectedException($"The Install operation must be {nameof(OperationInstall)} instance.");
            }

            var package = operationInstall.GetPackage();
            var installer = GetInstaller(package.GetPackageType());
            installer.Install(installedRepository, package);
            MarkForNotification(package);
        }

        /// <summary>
        /// Executes update operation.
        /// </summary>
        /// <param name="installedRepository">The repository in which to check.</param>
        /// <param name="operation">The operation instance.</param>
        protected virtual void Update(IRepositoryInstalled installedRepository, IOperation operation)
        {
            if (!(operation is OperationUpdate operationUpdate))
            {
                throw new UnexpectedException($"The update operation must be {nameof(OperationUpdate)} instance.");
            }

            var initial = operationUpdate.GetInitialPackage();
            var target = operationUpdate.GetTargetPackage();

            var initialType = initial.GetPackageType();
            var targetType = target.GetPackageType();

            if (initialType == targetType)
            {
                var installer = GetInstaller(initialType);
                installer.Update(installedRepository, initial, target);
                MarkForNotification(target);
            }
            else
            {
                var installer = GetInstaller(initialType);
                installer.Uninstall(installedRepository, initial);

                installer = GetInstaller(targetType);
                installer.Install(installedRepository, target);
            }
        }

        /// <summary>
        /// Executes uninstall operation.
        /// </summary>
        /// <param name="installedRepository">The repository in which to check.</param>
        /// <param name="operation">The operation instance.</param>
        protected virtual void Uninstall(IRepositoryInstalled installedRepository, IOperation operation)
        {
            if (!(operation is OperationUninstall operationUninstall))
            {
                throw new UnexpectedException($"The uninstall operation must be {nameof(OperationUninstall)} instance.");
            }

            var package = operationUninstall.GetPackage();
            var installer = GetInstaller(package.GetPackageType());
            installer.Uninstall(installedRepository, package);
        }

        /// <summary>
        /// Executes marke alias installed operation.
        /// </summary>
        /// <param name="installedRepository">The repository in which to check.</param>
        /// <param name="operation">The operation instance.</param>
        protected virtual void MarkAliasInstalled(IRepositoryInstalled installedRepository, IOperation operation)
        {
            if (!(operation is OperationMarkPackageAliasInstalled operationAliasInstall))
            {
                throw new UnexpectedException($"The mark package aliased installed operation must be {nameof(OperationMarkPackageAliasInstalled)} instance.");
            }

            var package = operationAliasInstall.GetPackage();

            if (!installedRepository.HasPackage(package))
            {
                installedRepository.AddPackage((IPackage)package.Clone());
            }
        }

        /// <summary>
        /// Executes marke alias uninstall operation.
        /// </summary>
        /// <param name="installedRepository">The repository in which to check.</param>
        /// <param name="operation">The operation instance.</param>
        protected virtual void MarkAliasUninstalled(IRepositoryInstalled installedRepository, IOperation operation)
        {
            if (!(operation is OperationMarkPackageAliasUninstall operationAliasUninstall))
            {
                throw new UnexpectedException($"The mark package aliased uninstalled operation must be {nameof(OperationMarkPackageAliasUninstall)} instance.");
            }

            var package = operationAliasUninstall.GetPackage();
            installedRepository.RemovePackage(package);
        }

        private void MarkForNotification(IPackage package)
        {
            if (string.IsNullOrEmpty(package.GetNotificationUri()))
            {
                return;
            }

            if (!notifiablePackages.TryGetValue(package.GetNotificationUri(), out IList<IPackage> packages))
            {
                notifiablePackages[package.GetNotificationUri()] = packages = new List<IPackage>();
            }

            packages.Add(package);
        }

        private class PostNotifyData
        {
            [JsonProperty("downloads")]
            public IList<NotifyDownload> Downloads { get; set; } = new List<NotifyDownload>();

            public static implicit operator string(PostNotifyData data)
            {
                return data.ToString();
            }

            /// <inheritdoc />
            public override string ToString()
            {
                return JsonConvert.SerializeObject(this, Formatting.Indented);
            }
        }

        private class NotifyDownload
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("version")]
            public string Version { get; set; }

            public static implicit operator NotifyDownload((string Name, string Version) tuple)
            {
                return new NotifyDownload
                {
                    Name = tuple.Name,
                    Version = tuple.Version,
                };
            }
        }
    }
}
