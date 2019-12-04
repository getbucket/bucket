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
using Bucket.FileSystem;
using Bucket.IO;
using Bucket.Package;
using Bucket.Util;
using GameBox.Console.Exception;
using GameBox.Console.Process;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using BVersionParser = Bucket.Package.Version.VersionParser;
using SException = System.Exception;

namespace Bucket.Downloader
{
    /// <summary>
    /// Represents a vcs downloader abstraction.
    /// </summary>
    public abstract class DownloaderVcs : IDownloader
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DownloaderVcs"/> class.
        /// </summary>
        protected DownloaderVcs(IIO io, Config config, IProcessExecutor process = null, IFileSystem fileSystem = null)
        {
            IO = io;
            Config = config;
            Process = process ?? new BucketProcessExecutor();
            FileSystem = fileSystem ?? new FileSystemLocal(null, Process);
        }

        /// <inheritdoc />
        public virtual InstallationSource InstallationSource => InstallationSource.Source;

        /// <summary>
        /// Gets the input/output instance.
        /// </summary>
        protected IIO IO { get; }

        /// <summary>
        /// Gets the config instance.
        /// </summary>
        protected Config Config { get; }

        /// <summary>
        /// Gets the process executor instance.
        /// </summary>
        protected IProcessExecutor Process { get; }

        /// <summary>
        /// Gets the fileSystem instance.
        /// </summary>
        protected IFileSystem FileSystem { get; }

        /// <inheritdoc />
        public Task Download(IPackage package, string cwd)
        {
            return Task.Delay(0);
        }

        /// <inheritdoc />
        public void Install(IPackage package, string cwd)
        {
            GuardSourceReferenece(package);

            IO.WriteError($"  - Installing <info>{package.GetName()}</info> (<comment>{package.GetVersionPrettyFull()}</comment>): ", false);

            ProcessAction(package.GetSourceUris(), (uri) =>
            {
                DoInstall(package, cwd, uri);
            });
        }

        /// <inheritdoc />
        public void Remove(IPackage package, string cwd)
        {
            IO.WriteError($"  - Removing <info>{package.GetName()}</info> (<comment>{package.GetVersionPrettyFull()}</comment>)");
            CleanChanges(package, cwd, false);
            try
            {
                FileSystem.Delete(cwd);
            }
            catch (IOException ex)
            {
                throw new RuntimeException($"Could not completely delete \"{cwd}\", aborting.", ex);
            }
        }

        /// <inheritdoc />
        public void Update(IPackage initial, IPackage target, string cwd)
        {
            GuardSourceReferenece(target);

            var name = target.GetName();
            string from, to;
            if (initial.GetVersionPretty() == target.GetVersionPretty())
            {
                if (target.GetSourceType() == "svn")
                {
                    from = initial.GetSourceReference();
                    to = target.GetSourceReference();
                }
                else
                {
                    // Git's reference, we only need to take the first 7 digits.
                    var fromReference = initial.GetSourceReference();
                    var toReference = target.GetSourceReference();
                    from = fromReference.Length >= 7 ? fromReference.Substring(0, 7) : fromReference;
                    to = toReference.Length >= 7 ? toReference.Substring(0, 7) : toReference;
                }

                name += $" {initial.GetVersionPretty()}";
            }
            else
            {
                from = initial.GetVersionPrettyFull();
                to = target.GetVersionPrettyFull();
            }

            var actionName = BVersionParser.IsUpgrade(initial.GetVersion(), target.GetVersion()) ? "Updating" : "Downgrading";
            IO.WriteError($"  - {actionName} <info>{name}</info> (<comment>{from}</comment> => <comment>{to}</comment>): ", false);

            SException exception = null;
            try
            {
                CleanChanges(initial, cwd, true);

                ProcessAction(target.GetSourceUris(), (uri) =>
                {
                    DoUpdate(initial, target, cwd, uri);
                });
            }
#pragma warning disable CA1031
            catch (SException ex)
#pragma warning restore CA1031
            {
                exception = ex;
            }
            finally
            {
                ReapplyChanges(cwd);
            }

            // check metadata repository because in case of missing metadata
            // code would trigger another exception
            if (exception == null && IO.IsVerbose && HasMetadataRepository(cwd))
            {
                var message = "Pulling in changes:";
                var logs = GetCommitLogs(initial.GetSourceReference(), target.GetSourceReference(), cwd);

                if (string.IsNullOrEmpty(logs))
                {
                    message = "Rolling back changes:";
                    logs = GetCommitLogs(target.GetSourceReference(), initial.GetSourceReference(), cwd);
                }

                if (string.IsNullOrEmpty(logs))
                {
                    return;
                }

                logs = string.Join("\n", Arr.Map(logs.Split('\n'), line => $"      {line}"));

                // escape angle brackets for proper output in the console.
                logs = logs.Replace("<", @"\<");

                IO.WriteError($"    {message}");
                IO.WriteError(logs);
            }
            else if (exception != null)
            {
                ExceptionDispatchInfo.Capture(exception).Throw();
            }
        }

        /// <summary>
        /// Prompt the user to check if changes should be stashed/removed or the operation aborted.
        /// </summary>
        /// <param name="package">The package instance.</param>
        /// <param name="cwd">The specific folder.</param>
        /// <param name="isUpdate">
        /// If true (update) the changes can be stashed and reapplied after an update.
        /// If false (remove) the changes should be assumed to be lost if the operation is not aborted.
        /// </param>
        protected virtual void CleanChanges(IPackage package, string cwd, bool isUpdate)
        {
            if (this is IReportChange report && !string.IsNullOrEmpty(report.GetLocalChanges(package, cwd)))
            {
                throw new RuntimeException($"Source directory \"{cwd}\" has uncommitted changes.");
            }
        }

        /// <summary>
        /// Guarantee that no changes have been made to the local copy.
        /// </summary>
        /// <param name="cwd">The specific folder.</param>
        protected virtual void ReapplyChanges(string cwd)
        {
        }

        /// <summary>
        /// Install specific package into specific folder.
        /// </summary>
        /// <param name="package">The package instance.</param>
        /// <param name="cwd">The specific folder.</param>
        /// <param name="uri">The package uri.</param>
        protected abstract void DoInstall(IPackage package, string cwd, string uri);

        /// <summary>
        /// Updates specific package in specific folder from <paramref name="initial"/> to <paramref name="target"/> version.
        /// </summary>
        /// <param name="initial">The initial package.</param>
        /// <param name="target">The target package.</param>
        /// <param name="cwd">The specific folder.</param>
        /// <param name="uri">The package uri.</param>
        protected abstract void DoUpdate(IPackage initial, IPackage target, string cwd, string uri);

        /// <summary>
        /// Checks if VCS metadata repository has been initialized.
        /// </summary>
        /// <remarks>repository example: .git|.svn.</remarks>
        /// <param name="cwd">The specific folder.</param>
        /// <returns>True if the vcs metadata repository has been initialized.</returns>
        protected abstract bool HasMetadataRepository(string cwd);

        /// <summary>
        /// Fetches the commit logs between two commits.
        /// </summary>
        /// <param name="fromReference">The source reference.</param>
        /// <param name="toReference">The target reference.</param>
        /// <param name="cwd">The specific folder.</param>
        /// <returns>Returns the commit logs between two commits.</returns>
        protected abstract string GetCommitLogs(string fromReference, string toReference, string cwd);

        private static void GuardSourceReferenece(IPackage package)
        {
            if (string.IsNullOrEmpty(package.GetSourceReference()))
            {
                throw new InvalidArgumentException($"Package \"{package.GetNamePretty()}\" is missing reference information.");
            }
        }

        private string ProcessLocalSourceUri(string uri)
        {
            var needle = "file://";
            var isFileProtocol = false;
            if (uri.StartsWith(needle, StringComparison.Ordinal))
            {
                uri = uri.Substring(needle.Length);
                isFileProtocol = true;
            }

            if (uri.Contains("%"))
            {
                uri = Uri.UnescapeDataString(uri);
            }

            uri = Path.GetFullPath(uri)
                      .Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            // The source of protection must exist.
            if (!FileSystem.Exists(uri, FileSystemOptions.Directory))
            {
                return string.Empty;
            }

            return isFileProtocol ? needle + uri : uri;
        }

        private void ProcessAction(string[] sourceUris, Action<string> closure)
        {
            var uris = new Queue<string>(sourceUris);
            Guard.Requires<UnexpectedException>(uris.Count > 0, "The number of valid download addresses must be greater than 0.");

            while (uris.Count > 0)
            {
                var uri = uris.Dequeue();

                try
                {
                    if (FileSystemLocal.IsLocalPath(uri))
                    {
                        uri = ProcessLocalSourceUri(uri);
                    }

                    closure(uri);
                    break;
                }
                catch (SException ex)
                {
                    if (IO.IsDebug)
                    {
                        IO.WriteError($"Failed: [{ex.GetType()}] {ex.Message}");
                    }
                    else if (uris.Count > 0)
                    {
                        IO.WriteError("    Failed, trying the next URI");
                    }

                    if (uris.Count <= 0)
                    {
                        throw;
                    }
                }
            }
        }
    }
}
