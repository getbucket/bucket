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

using Bucket.Cache;
using Bucket.Configuration;
using Bucket.Exception;
using Bucket.FileSystem;
using Bucket.IO;
using Bucket.Package;
using Bucket.Semver;
using Bucket.Util;
using Bucket.Util.SCM;
using GameBox.Console.Process;
using System;
using System.Text.RegularExpressions;

namespace Bucket.Downloader
{
    /// <summary>
    /// Represents a git downloader implement.
    /// </summary>
    public class DownloaderGit : DownloaderVcs, IReportChange, IReportDiff
    {
        private readonly Git git;
        private bool hasDiscardedChanges;
        private bool hasStashedChanges;

        /// <summary>
        /// Initializes a new instance of the <see cref="DownloaderGit"/> class.
        /// </summary>
        public DownloaderGit(IIO io, Config config, IProcessExecutor process = null, IFileSystem fileSystem = null)
            : base(io, config, process, fileSystem)
        {
            git = new Git(io, config, process, fileSystem);
        }

        /// <inheritdoc />
        public virtual string GetLocalChanges(IPackage package, string cwd)
        {
            Git.CleanEnvironment();

            if (!HasMetadataRepository(cwd))
            {
                return null;
            }

            var command = "git status --porcelain --untracked-files=no";
            if (Process.Execute(command, out string stdout, out string stderr, cwd) != 0)
            {
                throw new RuntimeException($"Failed to execute \"{command}\"{Environment.NewLine}{Environment.NewLine}{stderr}");
            }

            stdout = stdout.Trim();
            return string.IsNullOrEmpty(stdout) ? null : stdout;
        }

        /// <inheritdoc />
        public virtual string GetUnpushedChanges(IPackage package, string cwd)
        {
            Git.CleanEnvironment();
            cwd = NormalizePath(cwd);

            if (!HasMetadataRepository(cwd))
            {
                return null;
            }

            var command = "git show-ref --head -d";
            if (Process.Execute(command, out string stdout, out string stderr, cwd) != 0)
            {
                throw new RuntimeException($"Failed to execute {command}{Environment.NewLine}{Environment.NewLine}{stderr}");
            }

            const RegexOptions mi = RegexOptions.IgnoreCase | RegexOptions.Multiline;
            var refs = stdout.Trim();
            var match = Regex.Match(refs, "^(?<reference>[a-f0-9]+) HEAD\r?$", mi);
            if (!match.Success)
            {
                // could not match the HEAD for some reason.
                return null;
            }

            var headRef = match.Groups["reference"].Value;
            var matches = Regex.Matches(refs, $"^{headRef} refs/heads/(?<branch>.+?)\r?$", mi);
            if (matches.Count <= 0 || !matches[0].Success)
            {
                // not on a branch, we are either on a not-modified tag or some sort of detached head, so skip this.
                return null;
            }

            // use the first match as branch name for now.
            var branch = matches[0].Groups["branch"].Value;
            string unpushedChanges = null;

            // do two passes, as if we find anything we want to fetch and then re-try.
            for (var i = 0; i <= 1; i++)
            {
                string remoteBranch = null;

                // try to find the a matching branch name in the bucket remote
                foreach (Match matched in matches)
                {
                    var candidate = matched.Groups["branch"].Value;
                    var remoteMatched = Regex.Match(refs, $"^[a-f0-9]+ refs/remotes/(?<branch>(?:bucket|origin)/{Regex.Escape(candidate)})\r?$", mi);
                    if (!remoteMatched.Success)
                    {
                        continue;
                    }

                    branch = candidate;
                    remoteBranch = remoteMatched.Groups["branch"].Value;
                }

                // if it doesn't exist, then we assume it is an unpushed branch
                // this is bad as we have no reference point to do a diff so we
                // just bail listing the branch as being unpushed
                if (string.IsNullOrEmpty(remoteBranch))
                {
                    unpushedChanges = $"Branch {branch} could not be found on the origin remote and appears to be unpushed.";
                }
                else
                {
                    command = $"git diff --name-status {remoteBranch}...{branch} --";
                    if (Process.Execute(command, out stdout, out stderr, cwd) != 0)
                    {
                        throw new RuntimeException($"Failed to execute {command}{Environment.NewLine}{Environment.NewLine}{stderr}");
                    }

                    unpushedChanges = stdout.Trim();
                    unpushedChanges = string.IsNullOrEmpty(unpushedChanges) ? null : unpushedChanges;
                }

                // first pass and we found unpushed changes, fetch from both
                // remotes to make sure we have up to date remotes and then
                // try again as outdated remotes can sometimes cause false-positives
                if (!string.IsNullOrEmpty(unpushedChanges) && i == 0)
                {
                    Process.Execute("git fetch origin && git fetch bucket", cwd);
                }

                // abort after first pass if we didn't find anything
                if (string.IsNullOrEmpty(unpushedChanges))
                {
                    break;
                }
            }

            return unpushedChanges;
        }

        /// <inheritdoc />
        protected override void DoInstall(IPackage package, string cwd, string uri)
        {
            Git.CleanEnvironment();
            cwd = NormalizePath(cwd);
            var cachePath = Config.Get(Settings.CacheVcsDir) + $"/{CacheFileSystem.FormatCacheFolder(uri)}/";
            var reference = package.GetSourceReference();
            var flag = Platform.IsWindows ? "/D " : string.Empty;

            // --dissociate option is only available since git 2.3.0-rc0
            var gitVersion = git.GetVersion();
            var message = $"Cloning {GetShortHash(reference)}";

            var command = $"git clone --no-checkout %uri% %path% && cd {flag}%path% && git remote add bucket %uri% && git fetch bucket";
            if (!string.IsNullOrEmpty(gitVersion) &&
                Comparator.GreaterThanOrEqual(gitVersion, "2.3.0-rc0") &&
                CacheFileSystem.IsUsable(cachePath))
            {
                IO.WriteError(string.Empty, true, Verbosities.Debug);
                IO.WriteError($"    Cloning to cache at {ProcessExecutor.Escape(cachePath)}", true, Verbosities.Debug);

                try
                {
                    git.FetchReferenceOrSyncMirror(uri, reference, cachePath);
                    if (FileSystem.Exists(cachePath, FileSystemOptions.Directory))
                    {
                        command = "git clone --no-checkout %cachePath% %path% --dissociate --reference %cachePath%"
                                + $" && cd {flag}%path%"
                                + " && git remote set-url origin %uri% && git remote add bucket %uri%";
                        message = $"Cloning {GetShortHash(reference)} from cache.";
                    }
                }
                catch (RuntimeException)
                {
                    // ignore runtime exception because this is an optimization solution.
                }
            }

            IO.WriteError(message);

            string CommandCallable(string authUri)
            {
                var template = command;
                template = template.Replace("%uri%", ProcessExecutor.Escape(authUri));
                template = template.Replace("%path%", ProcessExecutor.Escape(cwd));
                template = template.Replace("%cachePath%", ProcessExecutor.Escape(cachePath));
                return template;
            }

            git.ExecuteCommand(CommandCallable, uri, cwd, true);

            if (uri != package.GetSourceUri())
            {
                UpdateOriginUri(package.GetSourceUri(), cwd);
            }
            else
            {
                SetPushUri(uri, cwd);
            }

            ExecuteUpdate(cwd, reference, package);
        }

        /// <inheritdoc />
        protected override void DoUpdate(IPackage initial, IPackage target, string cwd, string uri)
        {
            Git.CleanEnvironment();
            cwd = NormalizePath(cwd);

            if (!HasMetadataRepository(cwd))
            {
                throw new RuntimeException($"The .git directory is missing from \"{cwd}\"");
            }

            var updateOriginUri = false;
            if (Process.Execute("git remote -v", out string stdout, cwd) == 0)
            {
                var originMatch = Regex.Match(stdout, @"^origin\s+(?<uri>\S+?)\r?", RegexOptions.Multiline);
                var bucketMatch = Regex.Match(stdout, @"^bucket\s+(?<uri>\S+?)\r?", RegexOptions.Multiline);

                if (originMatch.Success && bucketMatch.Success &&
                    originMatch.Groups["uri"].Value == bucketMatch.Groups["uri"].Value &&
                    bucketMatch.Groups["uri"].Value != target.GetSourceUri())
                {
                    updateOriginUri = true;
                }
            }

            var reference = target.GetSourceReference();
            IO.WriteError($" Checking out {GetShortHash(reference)}");

            string CommandCallable(string authUri)
            {
                var escapedAuthUri = ProcessExecutor.Escape(authUri);
                var escapedReference = ProcessExecutor.Escape($"{reference}^{{commit}}");
                return $"git remote set-url bucket {escapedAuthUri} && git rev-parse --quiet --verify {escapedReference} || (git fetch bucket && git fetch --tags bucket)";
            }

            git.ExecuteCommand(CommandCallable, uri, cwd);

            ExecuteUpdate(cwd, reference, target);

            if (updateOriginUri)
            {
                UpdateOriginUri(target.GetSourceUri(), cwd);
            }
        }

        /// <inheritdoc />
        protected override string GetCommitLogs(string fromReference, string toReference, string cwd)
        {
            cwd = NormalizePath(cwd);
            var escapedFrom = ProcessExecutor.Escape(fromReference);
            var escapedTo = ProcessExecutor.Escape(toReference);

            var command = $"git log {escapedFrom}..{escapedTo} --pretty=format:\"%h - %an: %s\"";

            if (Process.Execute(command, out string stdout, out string stderr, cwd) != 0)
            {
                throw new RuntimeException($"Failed to execute {command}{Environment.NewLine}{Environment.NewLine}{stderr}");
            }

            return stdout.Trim();
        }

        /// <inheritdoc />
        protected override bool HasMetadataRepository(string cwd)
        {
            cwd = NormalizePath(cwd);
            return FileSystem.Exists(cwd + "/.git", FileSystemOptions.Directory);
        }

        /// <inheritdoc />
        protected override void CleanChanges(IPackage package, string cwd, bool isUpdate)
        {
            Git.CleanEnvironment();
            cwd = NormalizePath(cwd);

            var unpushed = GetUnpushedChanges(package, cwd);
            var discardChanges = Config.Get(Settings.DiscardChanges);

            // For unpushed changes, we throw an exception unless
            // it is not interactive and is configured to discard
            // the changes.
            if (!string.IsNullOrEmpty(unpushed) && (IO.IsInteractive || discardChanges != "true"))
            {
                throw new RuntimeException(
                    $"Source directory \"{cwd}\" has unpushed changes on the current branch: {Environment.NewLine}{unpushed}");
            }

            var changes = GetLocalChanges(package, cwd);
            if (string.IsNullOrEmpty(changes))
            {
                return;
            }

            if (!IO.IsInteractive)
            {
                if (discardChanges == "true")
                {
                    DiscardChanges(cwd);
                    return;
                }

                if (discardChanges == "stash")
                {
                    if (!isUpdate)
                    {
                        base.CleanChanges(package, cwd, isUpdate);
                        return;
                    }

                    StashChanges(cwd);
                    return;
                }

                base.CleanChanges(package, cwd, isUpdate);
                return;
            }

            var changeLines = Arr.Map(Regex.Split(changes, @"\s*\r?\n\s*"), (line) => $"    {line}");

            IO.WriteError("    <error>The package has modified files:</error>");
            IO.WriteError(Arr.Slice(changeLines, 0, 10));

            if (changeLines.Length > 10)
            {
                IO.WriteError($"    <info>{changeLines.Length - 10} more files modified, choose \"v\" to view the full list.</info>");
            }

            while (true)
            {
                string answer = IO.Ask($"    <info>Discard changes [y,n,v,d,{(isUpdate ? "s," : string.Empty)}?,h]?", "?");
                switch (answer.Trim())
                {
                    case "y":
                        DiscardChanges(cwd);
                        return;
                    case "s":
                        if (!isUpdate)
                        {
                            goto help;
                        }

                        StashChanges(cwd);
                        return;
                    case "n":
                        throw new RuntimeException("Update aborted. because the user refused to operate.");
                    case "v":
                        IO.WriteError(changeLines);
                        break;
                    case "d":
                        ViewDiff(cwd);
                        break;
                    default:
                    help:
                        var action = isUpdate ? "update" : "uninstall";
                        IO.WriteError(new[]
                        {
                            $"    y - discard changes and apply the {action}.",
                            $"    n - abort the {action} and let you manually clean things up.",
                            "    v - view modified files.",
                            "    d - view local modifications (diff).",
                        });

                        if (isUpdate)
                        {
                            IO.WriteError("    s - stash changes and try to reapply them after the update.");
                        }

                        IO.WriteError("    ? - print help(h).");
                        break;
                }
            }
        }

        /// <summary>
        /// Discard file changes.
        /// </summary>
        protected virtual void DiscardChanges(string cwd)
        {
            cwd = NormalizePath(cwd);

            if (Process.Execute("git reset --hard", out _, out string stderr, cwd) != 0)
            {
                throw new RuntimeException(
                    $"Could not reset changes{Environment.NewLine}{Environment.NewLine}:{stderr}");
            }

            hasDiscardedChanges = true;
        }

        /// <inheritdoc />
        protected override void ReapplyChanges(string cwd)
        {
            cwd = NormalizePath(cwd);

            if (!hasStashedChanges)
            {
                return;
            }

            hasStashedChanges = false;
            IO.WriteError("    <info>Re-applying stashed changes</info>");

            if (Process.Execute("git stash pop", out _, out string stderr, cwd) != 0)
            {
                throw new RuntimeException($"Failed to apply stashed changes:{Environment.NewLine}{Environment.NewLine}{stderr}");
            }

            hasDiscardedChanges = false;
        }

        /// <summary>
        /// Stash file changes.
        /// </summary>
        protected virtual void StashChanges(string cwd)
        {
            cwd = NormalizePath(cwd);

            if (Process.Execute("git stash --include-untracked", out _, out string stderr, cwd) != 0)
            {
                throw new RuntimeException($"Could not stash changes:{Environment.NewLine}{Environment.NewLine}{stderr}");
            }

            hasStashedChanges = true;
        }

        /// <summary>
        /// View diff files.
        /// </summary>
        protected virtual void ViewDiff(string cwd)
        {
            cwd = NormalizePath(cwd);

            if (Process.Execute("git diff HEAD", out string stdout, out string stderr, cwd) != 0)
            {
                throw new RuntimeException($"Could not view diff:{Environment.NewLine}{Environment.NewLine}{stderr}");
            }

            IO.WriteError(stdout);
        }

        /// <summary>
        /// Updates the given path to the given commit ref.
        /// </summary>
        /// <param name="cwd">The given path.</param>
        /// <param name="reference">Checkout to specified reference(commit ref, tag, branch).</param>
        /// <param name="branch">The name of the branch to use when checking out.</param>
        /// <returns>If a string is returned, it is the commit reference that was checked out if the original could not be found.</returns>
        protected virtual string UpdateToCommit(string cwd, string reference, string branch, DateTime? releaseDate)
        {
            Process.Execute("git branch -r", out string branches, cwd);

            bool IsGitHash(string hash)
            {
                return Regex.IsMatch(hash, "^[a-f0-9]{40}$");
            }

            bool IsBranchesHasRemote(string remote)
            {
                return Regex.IsMatch(branches, $"^\\s+bucket/{Regex.Escape(remote)}\r?$", RegexOptions.Multiline);
            }

            string command;
            var force = hasDiscardedChanges || hasStashedChanges ? "-f " : string.Empty;
            branch = Regex.Replace(branch ?? string.Empty, @"(?:^dev-|(?:\.x)?-dev$)", string.Empty, RegexOptions.IgnoreCase);

            // check whether non-commitish are branches or tags, and fetch branches
            // with the remote name. Use branch(in the context may be the version)
            // as the new branch name.
            if (!IsGitHash(reference) &&
                !string.IsNullOrEmpty(branches) &&
                IsBranchesHasRemote(reference))
            {
                var escapedBranch = ProcessExecutor.Escape(branch);
                var escapedReference = ProcessExecutor.Escape($"bucket/{reference}");
                command = $"git checkout {force}-B {escapedBranch} {escapedReference} -- && git reset --hard {escapedReference} --";
                if (Process.Execute(command, cwd) == 0)
                {
                    return null;
                }
            }

            // try to checkout branch by name and then reset it so it's on the proper branch name.
            if (IsGitHash(reference))
            {
                // add 'v' in front of the branch if it was stripped when generating the pretty name.
                if (!IsBranchesHasRemote(branch) && IsBranchesHasRemote($"v{branch}"))
                {
                    branch = $"v{branch}";
                }

                var escapedBranch = ProcessExecutor.Escape(branch);
                command = $"git checkout {escapedBranch} --";
                var fallbackCommand = $"git checkout {force}-B {escapedBranch} {ProcessExecutor.Escape($"bucket/{branch}")} --";
                if (Process.Execute(command, cwd) == 0 || Process.Execute(fallbackCommand, cwd) == 0)
                {
                    command = $"git reset --hard {ProcessExecutor.Escape(reference)} --";
                    if (Process.Execute(command, cwd) == 0)
                    {
                        return null;
                    }
                }
            }

            // This uses the "--" sequence to separate branch from file parameters.
            //
            // Otherwise git tries the branch name as well as file name.
            // If the non-existent branch is actually the name of a file, the file
            // is checked out.
            var escapedGitReference = ProcessExecutor.Escape(reference);
            command = $"git checkout {force}{escapedGitReference} -- && git reset --hard {escapedGitReference} --";
            if (Process.Execute(command, out _, out string stderr, cwd) == 0)
            {
                return null;
            }

            // reference was not found (prints "fatal: reference is not a tree: $ref").
            if (stderr.Contains(reference))
            {
                IO.WriteError($"    <warning>{reference} is gone (history was rewritten?)</warning>");
            }

            stderr = BucketProcessExecutor.FilterSensitive($"Failed to execute \"{command}\" {Environment.NewLine}{Environment.NewLine}{stderr}");
            throw new RuntimeException(stderr);
        }

        /// <summary>
        /// Normalize the path.
        /// </summary>
        protected virtual string NormalizePath(string cwd)
        {
            if (FileSystem is IReportPath report)
            {
                return report.ApplyRootPath(cwd);
            }

            // todo: normalize if not support ApplyRootPath.
            throw new RuntimeException("File system need implement not support report path.");
        }

        /// <summary>
        /// Get a short hash. if is verbose then skipped.
        /// </summary>
        protected virtual string GetShortHash(string reference)
        {
            if (!IO.IsVerbose && Regex.IsMatch(reference, "^[0-9a-f]{40}$"))
            {
                return reference.Substring(0, 10);
            }

            return reference;
        }

        /// <summary>
        /// Update the origin uri.
        /// </summary>
        protected virtual void UpdateOriginUri(string uri, string cwd)
        {
            Process.Execute($"git remote set-url origin {ProcessExecutor.Escape(uri)}", cwd);
            SetPushUri(uri, cwd);
        }

        /// <summary>
        /// Set the origin push uri.
        /// </summary>
        protected virtual void SetPushUri(string uri, string cwd)
        {
            // set push url for github projects.
            var match = Regex.Match(uri, $"^(?:https?|git)://{Git.GetGithubDomainsRegex(Config)}/(?<username>[^/]+)/(?<repository>[^/]+?)(?:\\.git)?$");
            if (!match.Success)
            {
                return;
            }

            string[] protocols = Config.Get(Settings.GithubProtocols);
            var pushUri = $"git@{match.Groups["domain"].Value}:{match.Groups["username"].Value}/{match.Groups["repository"].Value}.git";

            if (!Array.Exists(protocols, protocol => protocol == "ssh"))
            {
                pushUri = $"https://{match.Groups["domain"].Value}/{match.Groups["username"].Value}/{match.Groups["repository"].Value}.git";
            }

            Process.Execute($"git remote set-url --push origin {ProcessExecutor.Escape(pushUri)}", cwd);
        }

        private void ExecuteUpdate(string cwd, string reference, IPackage target)
        {
            var newReference = UpdateToCommit(cwd, reference, target.GetVersionPretty(), target.GetReleaseDate());
            if (string.IsNullOrEmpty(newReference) ||
                !(target is Package.Package originPackage))
            {
                return;
            }

            if (target.GetDistReference() == target.GetSourceReference())
            {
                originPackage.SetDistReference(newReference);
            }

            originPackage.SetSourceReference(newReference);
        }
    }
}
