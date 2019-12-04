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
using GameBox.Console.Exception;
using GameBox.Console.Process;
using GameBox.Console.Util;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Bucket.Util.SCM
{
    /// <summary>
    /// Represents a Git operation class.
    /// </summary>
    /// <remarks>To use this feature, the device must be installed: (https://git-scm.com/).</remarks>
    public class Git
    {
        private static string version;
        private readonly IIO io;
        private readonly Config config;
        private readonly IProcessExecutor process;
        private readonly IFileSystem fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="Git"/> class.
        /// </summary>
        public Git(IIO io, Config config, IProcessExecutor process, IFileSystem fileSystem)
        {
            this.io = io;
            this.config = config;
            this.process = process;
            this.fileSystem = fileSystem;
        }

        /// <summary>
        /// Clean up environment variables to prevent some unexpected problems.
        /// </summary>
        public static void CleanEnvironment()
        {
            // added in git 1.7.1, prevents prompting the user for username/password.
            // https://git-scm.com/book/en/v2/Git-Internals-Environment-Variables
            var gitAskPass = Terminal.GetEnvironmentVariable("GIT_ASKPASS");
            if (gitAskPass is null || gitAskPass != "echo")
            {
                Terminal.SetEnvironmentVariable("GIT_ASKPASS", "echo");
            }

            // clean up rogue git env vars in case this is running in a git hook.
            var gitDir = Terminal.GetEnvironmentVariable("GIT_DIR");
            if (!(gitDir is null))
            {
                Terminal.RemoveEnvironmentVariable("GIT_DIR");
            }

            var gitWorkTree = Terminal.GetEnvironmentVariable("GIT_WORK_TREE");
            if (!(gitWorkTree is null))
            {
                Terminal.RemoveEnvironmentVariable("GIT_WORK_TREE");
            }
        }

        /// <summary>
        /// Get the regular match of a github domain.
        /// </summary>
        /// <returns>Return an regular match of github domain.</returns>
        public static string GetGithubDomainsRegex(Config config)
        {
            return $"(?<domain>{string.Join("|", Arr.Map((string[])config.Get(Settings.GithubDomains), (domain) => Regex.Escape(domain)))})";
        }

        /// <summary>
        /// Get the regular match of a gitlab domain.
        /// </summary>
        /// <returns>Return an regular match of gitlab domain.</returns>
        public static string GetGitlabDomainsRegex(Config config)
        {
            return $"(?<domain>{string.Join("|", Arr.Map((string[])config.Get(Settings.GitlabDomains), (domain) => Regex.Escape(domain)))})";
        }

        /// <summary>
        /// Synchronize the specified git repository to the specified directory.
        /// </summary>
        /// <param name="uri">The specified git repository. can use ssh.</param>
        /// <param name="path">The specified saved path. this path will be based on the file system.</param>
        /// <returns>True if successful to sync mirror.</returns>
        public bool SyncMirror(string uri, string path = null)
        {
            path = string.IsNullOrEmpty(path) ? GetRepositoryNameFromUri(uri) : path;

            if (fileSystem is IReportPath report)
            {
                path = report.ApplyRootPath(path);
            }

            if (string.IsNullOrEmpty(path))
            {
                throw new FileSystemException("Clone path is invalid, cannot be empty.");
            }

            if (fileSystem.Exists(path, FileSystemOptions.Directory)
               && process.Execute("git rev-parse --git-dir", out string output, path) == 0
               && output.Trim() == ".")
            {
                string Upgrate(string protoUri)
                {
                    return $"git remote set-url origin {protoUri} && git remote update --prune origin";
                }

                try
                {
                    ExecuteCommand(Upgrate, uri, path);
                    return true;
                }
#pragma warning disable CA1031
                catch (System.Exception)
#pragma warning restore CA1031
                {
                    return false;
                }
            }

            fileSystem.Delete(path);

            string Cloneable(string protoUri)
            {
                return $"git clone --mirror {ProcessExecutor.Escape(protoUri)} {ProcessExecutor.Escape(path)}";
            }

            ExecuteCommand(Cloneable, uri, path, true);

            return true;
        }

        /// <summary>
        /// Fetch the specified reference or synchronize the git repository.
        /// </summary>
        /// <param name="uri">The specified git repository. can use ssh.</param>
        /// <param name="reference">The specified git reference.</param>
        /// <param name="path">The specified saved path. this path will be based on the file system.</param>
        /// <returns>True if fetched. false is sync mirror.</returns>
        public bool FetchReferenceOrSyncMirror(string uri, string reference, string path = null)
        {
            var parsedPath = string.IsNullOrEmpty(path) ? GetRepositoryNameFromUri(uri) : path;

            if (fileSystem is IReportPath report)
            {
                parsedPath = report.ApplyRootPath(parsedPath);
            }

            if (string.IsNullOrEmpty(parsedPath))
            {
                throw new FileSystemException("Clone path is invalid, cannot be empty.");
            }

            if (fileSystem.Exists(parsedPath, FileSystemOptions.Directory)
               && process.Execute("git rev-parse --git-dir", out string output, parsedPath) == 0
               && output.Trim() == ".")
            {
                var escapedReference = ProcessExecutor.Escape($"{reference}^{{commit}}");
                if (process.Execute($"git rev-parse --quiet --verify {escapedReference}", parsedPath) == 0)
                {
                    return true;
                }
            }

            SyncMirror(uri, path);

            return false;
        }

        /// <summary>
        /// Execute git command.
        /// </summary>
        /// <remarks>If initializing clone, must give an absolute path in the callable command.</remarks>
        /// <param name="commandCallable">A callback, the callback return value is the command executed.</param>
        /// <param name="uri">The git uri.</param>
        /// <param name="cwd">In which directory the command will be executed.</param>
        /// <param name="initialClone">Whether is initial clone command.</param>
        public void ExecuteCommand(Func<string, string> commandCallable, string uri, string cwd, bool initialClone = false)
        {
            // Ensure we are allowed to use this URL by config
            Guard.That.ProhibitUri(config, uri, io);

            string originCwd = null;
            if (initialClone)
            {
                originCwd = cwd;
                cwd = null;
            }

            if (Regex.IsMatch(uri, "^ssh://[^@]+@[^:]+:[^0-9]+"))
            {
                throw new InvalidArgumentException($"The source URI {uri} is invalid, ssh URI should have a port number after \":\"{Environment.NewLine}Use ssh://git@example.com:22/path or just git@example.com:path if you do not want to provide a password or custom port.");
            }

            Match matched;
            if (!initialClone)
            {
                // capture username/password from URL if there is one
                process.Execute("git remote -v", out string output, cwd);
                matched = Regex.Match(output, @"^(?:bucket|origin)\s+https?://(?<username>.+):(?<password>.+)@(?<repository>[^/]+?)\r?", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                if (matched.Success)
                {
                    io.SetAuthentication(
                        matched.Groups["repository"].Value,
                        matched.Groups["username"].Value,
                        matched.Groups["password"].Value);
                }
            }

            string stderr = null;
            string[] protocols = config.Get(Settings.GithubProtocols);

            // public github, autoswitch protocols
            matched = Regex.Match(uri, $"^(?:https?|git)://{GetGithubDomainsRegex(config)}/(?<path>.*)");
            if (matched.Success)
            {
                var messages = new LinkedList<string>();

                foreach (var protocol in protocols)
                {
                    string protocolUri;
                    if (protocol == "ssh")
                    {
                        protocolUri = $"git@{matched.Groups["domain"].Value}:{matched.Groups["path"].Value}";
                    }
                    else
                    {
                        protocolUri = $"{protocol}://{matched.Groups["domain"].Value}/{matched.Groups["path"].Value}";
                    }

                    if (process.Execute(commandCallable(protocolUri), out _, out stderr, cwd) == 0)
                    {
                        return;
                    }

                    var error = Regex.Replace(stderr, "^", "  ", RegexOptions.Multiline);
                    messages.AddLast($"- {protocolUri}{Environment.NewLine}{error}");

                    if (initialClone)
                    {
                        fileSystem.Delete(originCwd);
                    }
                }

                var errorMessage = new StringBuilder();
                errorMessage.Append("Failed to clone ")
                            .Append(uri)
                            .Append("via")
                            .Append(string.Join(", ", protocols))
                            .Append(" protocols, aborting.")
                            .Append(Environment.NewLine)
                            .Append(Environment.NewLine)
                            .Append(string.Join(Environment.NewLine, messages));

                throw CreateException(errorMessage.ToString(), uri);
            }

            // if we have a private github url and the ssh protocol is disabled
            // then we skip it and directly fallback to https.
            var bypassSSHForGithub = Regex.IsMatch(uri, $"git@{GetGithubDomainsRegex(config)}:(.+?)\\.git$", RegexOptions.IgnoreCase)
                                        && !Array.Exists(protocols, (protocol) => protocol == "ssh");

            var command = commandCallable(uri);
            if (!bypassSSHForGithub && process.Execute(command, out _, out stderr, cwd) == 0)
            {
                return;
            }

            // private github repository without git access, try https with auth
            matched = Regex.Match(uri, $"^git@{GetGithubDomainsRegex(config)}:(?<path>.+?)\\.git$", RegexOptions.IgnoreCase);
            if (matched.Success)
            {
                var domain = matched.Groups["domain"].Value;
                var path = matched.Groups["path"].Value;

                if (!io.HasAuthentication(domain))
                {
                    OAuthWithGithub(domain);
                }

                if (!io.HasAuthentication(domain))
                {
                    goto faild;
                }

                var (username, password) = io.GetAuthentication(domain);
                username = Uri.EscapeDataString(username);
                password = Uri.EscapeDataString(password);

                var authUri = $"https://{username}:{password}@{domain}/{path}.git";
                command = commandCallable(authUri);

                if (process.Execute(command, out _, out stderr, cwd) == 0)
                {
                    return;
                }

                goto faild;
            }

            // gitlab repository.
            matched = Regex.Match(uri, $"^(?<scheme>https?)://{GetGitlabDomainsRegex(config)}/(?<path>.*)");
            if (matched.Success)
            {
                var domain = matched.Groups["domain"].Value;
                var path = matched.Groups["path"].Value;
                var scheme = matched.Groups["scheme"].Value;
                if (!io.HasAuthentication(domain))
                {
                    OAuthWithGitlab(scheme, domain);
                }

                if (!io.HasAuthentication(domain))
                {
                    goto faild;
                }

                var (username, password) = io.GetAuthentication(domain);
                username = Uri.EscapeDataString(username);
                password = Uri.EscapeDataString(password);

                string authUri;
                if (password == "private-token" || password == "oauth2")
                {
                    // swap username and password.
                    authUri = $"{scheme}://{password}:{username}@{domain}/{path}";
                }
                else
                {
                    authUri = $"{scheme}://{username}:{password}@{domain}/{path}";
                }

                command = commandCallable(authUri);

                if (process.Execute(command, out _, out stderr, cwd) == 0)
                {
                    return;
                }

                goto faild;
            }

            if (IsAuthenticationFailure(uri, stderr, out matched))
            {
                var host = matched.Groups["host"].Value;
                var scheme = matched.Groups["scheme"].Value;
                var path = matched.Groups["path"].Value;
                string authentication = null;
                string repository;
                if (host.Contains("@"))
                {
                    var segment = host.Split(new[] { '@' }, 2);
                    authentication = segment[0];
                    repository = segment[1];
                }
                else
                {
                    repository = host;
                }

                string storeAuth = null;
                string username = null, password = null;
                if (io.HasAuthentication(repository))
                {
                    (username, password) = io.GetAuthentication(repository);
                }
                else if (io.IsInteractive)
                {
                    string defaultUsername = null;
                    if (!string.IsNullOrEmpty(authentication))
                    {
                        if (authentication.Contains(":"))
                        {
                            defaultUsername = authentication.Split(new[] { ':' }, 2)[0];
                        }
                        else
                        {
                            defaultUsername = authentication;
                        }
                    }

                    (username, password) = InteractiveAuthorization(host, defaultUsername);
                    storeAuth = config.Get(Settings.StoreAuth);
                }

                if (username == null || password == null)
                {
                    goto faild;
                }

                var authUri = $"{scheme}{username}:{password}@{repository}{path}";
                command = commandCallable(authUri);

                if (process.Execute(command, out _, out stderr, cwd) == 0)
                {
                    io.SetAuthentication(repository, username, password);
                    StoreAuth(repository, storeAuth);
                    return;
                }

                goto faild;
            }

        faild:
            if (initialClone)
            {
                fileSystem.Delete(originCwd);
            }

            throw CreateException($"Failed to execute {command}{Environment.NewLine}{Environment.NewLine}{stderr}", uri);
        }

        /// <summary>
        /// Retrieves the current git version.
        /// </summary>
        /// <returns>The git version number.</returns>
        public string GetVersion()
        {
            if (!string.IsNullOrEmpty(version))
            {
                return version;
            }

            if (process.Execute("git --version", out string output) != 0)
            {
                return null;
            }

            var matched = Regex.Match(output, @"^git version (?<version>\d+(?:\.\d+)+)", RegexOptions.Multiline);
            if (matched.Success)
            {
#pragma warning disable S2696
                return version = matched.Groups["version"].Value;
#pragma warning restore S2696
            }

            return null;
        }

        /// <summary>
        /// Interact with the user to get the username and password.
        /// </summary>
        protected virtual (string Username, string Password) InteractiveAuthorization(string host, string defaultUsername)
        {
            io.WriteError($"    Authentication required (<info>{host}</info>):");
            return (io.Ask("      Username: ", defaultUsername),
                    io.AskPassword("      Password: "));
        }

        /// <summary>
        /// Storage authorization.
        /// </summary>
        /// <param name="origin">Specified origin(repository).</param>
        /// <param name="storeAuth">A string describe how to store.</param>
        protected virtual void StoreAuth(string origin, string storeAuth)
        {
            // todo: implement store auth.
        }

        /// <summary>
        /// Authorize for Github.
        /// </summary>
        protected virtual void OAuthWithGithub(string domain)
        {
            // todo: oauth github.
        }

        /// <summary>
        /// Authorize for Gitlab.
        /// </summary>
        protected virtual void OAuthWithGitlab(string scheme, string domain)
        {
            // todo: oauth gitlab.
        }

        private static bool IsAuthenticationFailure(string uri, string errorMessage, out Match match)
        {
            match = Regex.Match(uri, "^(?<scheme>https?://)(?<host>[^/]+)(?<path>.*)$", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return false;
            }

            var authFailures = new[]
            {
                "fatal: Authentication failed",
                "remote error: Invalid username or password.",
                "error: 401 Unauthorized",
                "fatal: unable to access",
                "fatal: could not read Username",
            };

            if (Array.Exists(authFailures, errorMessage.Contains))
            {
                return true;
            }

            return false;
        }

        private static string GetRepositoryNameFromUri(string uri)
        {
            Match matched;
            if (uri.Contains("@"))
            {
                matched = Regex.Match(uri, "^git@(?:.+?):(?<path>.+?)\\.git$");
            }
            else
            {
                matched = Regex.Match(uri, "^(?:https?|ssh|git|http)://(?:.+?)/(?<path>.*?)(\\.git)?$");
            }

            if (matched.Success)
            {
                var segment = matched.Groups["path"].Value.Split('/');
                return segment[segment.Length - 1];
            }

            return null;
        }

        private System.Exception CreateException(string message, string uri)
        {
            if (process.Execute("git --version", out _, out string stderr) != 0)
            {
                return new RuntimeException(BucketProcessExecutor.FilterSensitive($"Failed to clone {uri}, git was not found, check that it is installed and in your PATH env.{Environment.NewLine}{Environment.NewLine}{stderr}"));
            }

            return new RuntimeException(BucketProcessExecutor.FilterSensitive(message));
        }
    }
}
