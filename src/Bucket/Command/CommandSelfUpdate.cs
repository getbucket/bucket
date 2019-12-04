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

using Bucket.Archive;
using Bucket.Assets;
using Bucket.Configuration;
using Bucket.Downloader.Transport;
using Bucket.Exception;
using Bucket.FileSystem;
using Bucket.IO;
using Bucket.Json;
using Bucket.SelfUpdate;
using Bucket.Util;
using GameBox.Console;
using GameBox.Console.Input;
using GameBox.Console.Output;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.AccessControl;
using System.Text.RegularExpressions;
using BApplication = Bucket.Console.Application;
using SException = System.Exception;

namespace Bucket.Command
{
    /// <summary>
    /// Command will upgrade the bucket application.
    /// </summary>
    public class CommandSelfUpdate : BaseCommand
    {
        private const string SignatureSha = "sha384";
        private const string TempDirName = ".temp";
        private const string BackupExtensionName = "-backup.zip";
        private readonly IFileSystem fileSystem;
        private readonly string tempDir;
        private ITransport transport;
        private Config config;
        private string baseUri;
        private string backupDir;
        private string backupFile;
        private string home;
        private string installDir;
        private bool noProgress;

        public CommandSelfUpdate()
        {
            tempDir = Path.Combine(BApplication.ExecutablePath, TempDirName);
            fileSystem = new FileSystemLocal();

            try
            {
                // Delete the last used temporary directory.
                if (fileSystem.Exists(tempDir))
                {
                    fileSystem.Delete(tempDir);
                }
            }
#pragma warning disable CA1031
            catch
#pragma warning restore CA1031
            {
                // ignore.
            }
        }

        /// <inheritdoc />
        protected override void Configure()
        {
            SetName("self-update")
                .SetAlias("selfupdate")
                .SetDescription("Updates bucket application to the latest version.")
                .SetDefinition(new IInputDefinition[]
                {
                    new InputArgument("version", InputArgumentModes.Optional, "The version to update to"),
                    new InputOption("rollback", "r", InputOptionModes.ValueNone, "Revert to an older installation of bucket"),
                    new InputOption("clean-backups", null, InputOptionModes.ValueNone, "Delete old backups during an update. This makes the current version of bucket the only backup available after the update"),
                    new InputOption("no-progress", null, InputOptionModes.ValueNone, "Do not output download progress"),
                    new InputOption("update-keys", null, InputOptionModes.ValueNone, "Update the public key from the server"),
                    new InputOption("stable", null, InputOptionModes.ValueNone, "Force an update to the stable version"),
                    new InputOption("preview", null, InputOptionModes.ValueNone, "Force an update to the preview version"),
                    new InputOption("dev", null, InputOptionModes.ValueNone, "Force an update to the dev(snapshot) version"),
                    new InputOption("set-channel-only", null, InputOptionModes.ValueNone, "Only store the channel as the default one and then exit(Not upgraded)"),
                })
                .SetHelp(
@"The <info>self-update</info> command checks " + BucketVersions.Home + @" for newer
versions of bucket and if found, installs the latest.

<info>bucket {command.name}</info>");
        }

        /// <inheritdoc />
        protected override void Initialize(IInput input, IOutput output)
        {
            // Not obtained by bucket, because the update program does
            // not need to read the local bucket configuration.
            var factory = new Factory();
            var io = GetIO();

            config = factory.CreateConfig();
            transport = factory.CreateTransport(io, config);

            // todo: allow disabled the tls.
            baseUri = $"https://{BucketVersions.Home}";
            backupDir = config.Get(Settings.BackupDir);
            home = config.Get(Settings.Home);
            installDir = BApplication.ExecutablePath;
            noProgress = input.GetOption("no-progress");
            backupFile = $"{backupDir}/{Bucket.GetVersionPretty()}@{Bucket.GetVersion()}{BackupExtensionName}";

            if (fileSystem.Exists(tempDir))
            {
                throw new RuntimeException("The temporary file could not cleared. Please check the file usage or permissions and try again.");
            }
        }

        /// <inheritdoc />
        protected override int Execute(IInput input, IOutput output)
        {
            var io = GetIO();
            var versionUtility = new BucketVersions(io, config, transport);

            Channel? storedChannel = null;
            foreach (var channel in new[] { Channel.Stable, Channel.Preview, Channel.Dev })
            {
                // An unstable version will overwrite a stable version.
                if (input.GetOption(channel.ToString().ToLower()))
                {
                    versionUtility.SetChannel(channel);
                    storedChannel = channel;
                }
            }

            if (input.GetOption("set-channel-only"))
            {
                if (storedChannel != null)
                {
                    io.WriteError($"Channel {storedChannel.ToString().ToLower()} has been saved.");
                }
                else
                {
                    io.WriteError($"Channel not stored.");
                }

                return ExitCodes.Normal;
            }

            if (input.GetOption("update-keys"))
            {
                return FetchKeys(io, config);
            }

            if (input.GetOption("rollback"))
            {
                return Rollback(io, installDir, backupDir);
            }

            var (lastedVersion, remoteFileUri, lastedVersionPretty, _) = versionUtility.GetLatest();
            var updateVersion = input.GetArgument("version") ?? lastedVersion;
            var updatingWithDev = versionUtility.GetChannel() == Channel.Dev;

            // If it is the latest version we end the update.
            if (Bucket.GetVersion() == updateVersion)
            {
                io.WriteError($"<info>You are already using bucket version <comment>{Bucket.GetVersionPretty()}</comment> ({Bucket.GetVersion()},{versionUtility.GetChannel().ToString().ToLower()} channel).</info>");

                CheckCleanupBackups(io, input, backupDir);
                return ExitCodes.Normal;
            }

            if (string.IsNullOrEmpty(remoteFileUri))
            {
                var remoteRelativePath = updatingWithDev ?
                    $"/snapshot/{updateVersion}/bucket.zip" :
                    $"/releases/{updateVersion}/bucket.zip";
                remoteFileUri = $"{baseUri}{remoteRelativePath}";
            }
            else if (!BucketUri.IsAbsoluteUri(remoteFileUri))
            {
                remoteFileUri = new Uri(new Uri(baseUri), remoteFileUri).ToString();
            }

            var tempFile = Path.Combine(tempDir, Path.GetFileName(remoteFileUri));
            var signatureFile = updatingWithDev ? $"{home}/keys.dev.pem" : $"{home}/keys.tags.pem";
            tempFile = DownloadRemoteFile(io, remoteFileUri, tempFile, signatureFile);

            // Clean up the backup first, so that the current version
            // will be the last version.
            CheckCleanupBackups(io, input, backupDir);

            try
            {
                InstalltionBucket(installDir, tempFile, backupFile);
            }
#pragma warning disable CA1031
            catch (SException ex)
#pragma warning restore CA1031
            {
                fileSystem.Delete(tempFile);
                io.WriteError($"<error>The file is corrupted ({ex.Message}).</error>");
                io.WriteError("<error>Please re-run the self-update command to try again.</error>");
                return ExitCodes.GeneralException;
            }

            // The version number may be entered by the user.
            if (updateVersion == lastedVersion)
            {
                io.WriteError($"Bucket has been successfully upgraded to: <comment>{Bucket.ParseVersionPretty(lastedVersionPretty)}</comment> ({lastedVersion})");
            }
            else
            {
                io.WriteError($"Bucket has been successfully upgraded to: <comment>{updateVersion}</comment>");
            }

            if (fileSystem.Exists(backupFile, FileSystemOptions.File))
            {
                io.WriteError($"Use <info>bucket self-update --rollback</info> to return to version <comment>{Bucket.GetVersionPretty()}</comment> ({Bucket.GetVersion()})");
            }
            else
            {
                io.WriteError($"<warning>A backup of the current version could not be written to {backupFile}, no rollback possible</warning>");
            }

            return ExitCodes.Normal;
        }

        protected virtual int FetchKeys(IIO io, Config config)
        {
            // todo: need implement.
            io.WriteError("The function has not yet been implemented waiting for an upgrade.");
            return ExitCodes.GeneralException;
        }

        protected virtual int Rollback(IIO io, string installDir, string backupDir)
        {
            var rollbackVersion = GetLastVersionWithBackup(backupDir);

            if (string.IsNullOrEmpty(rollbackVersion))
            {
                throw new UnexpectedException($"Bucket rollback failed: no installation to rollback to in \"{backupDir}\"");
            }

            var rollbackFile = Path.Combine(backupDir, rollbackVersion + BackupExtensionName);
            if (rollbackVersion == $"{Bucket.GetVersionPretty()}@{Bucket.GetVersion()}")
            {
                fileSystem.Delete(rollbackFile);
                io.WriteError($"<info>You are already using bucket version <comment>{Bucket.GetVersionPretty()}</comment> ({Bucket.GetVersion()}).</info>");
                return ExitCodes.Normal;
            }

            var prettyRollbackVersion = Regex.Replace(rollbackVersion, "^(.*?)@(.*?)$", "${1} (${2})");
            io.WriteError($"Rolling back to version <info>{prettyRollbackVersion}</info>");

            try
            {
                InstalltionBucket(installDir, rollbackFile);
            }
#pragma warning disable CA1031
            catch (SException ex)
#pragma warning restore CA1031
            {
                io.WriteError($"<error>The backup file was corrupted ({ex.Message}). Please restore it manually: {rollbackFile}</error>");
                return ExitCodes.GeneralException;
            }

            try
            {
                fileSystem.Delete(rollbackFile);
            }
#pragma warning disable CA1031
            catch (SException ex)
#pragma warning restore CA1031
            {
                io.WriteError($"<error>Rollback has been successful, but failed to clear backup files ({ex.Message}). Please manually deleted: {rollbackFile}</error>");
            }

            return ExitCodes.Normal;
        }

        protected virtual void CheckCleanupBackups(IIO io, IInput input, string backupDir)
        {
            if (!input.GetOption("clean-backups"))
            {
                return;
            }

            var files = fileSystem.GetContents(backupDir).GetFiles();
            foreach (var file in files)
            {
                // Verifying the suffix prevents erroneous deletion of
                // files in the directory that do not belong to backup.
                if (!file.EndsWith(BackupExtensionName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                io.WriteError($"<info>Removing: {file}</info>");
                fileSystem.Delete(file);
            }
        }

        protected virtual string DownloadRemoteFile(IIO io, string remoteFileUri, string targetPath, string signatureFile = null)
        {
            if (noProgress)
            {
                transport.Copy(remoteFileUri, targetPath);
            }
            else
            {
                io.WriteError("  ", false);
                transport.Copy(remoteFileUri, targetPath, new ReportDownloadProgress(io, "  "));
                io.OverwriteError(string.Empty, false);
            }

            var signatureJson = transport.GetString(remoteFileUri + ".sig");
            if (!fileSystem.Exists(targetPath, FileSystemOptions.File) || string.IsNullOrEmpty(signatureJson))
            {
                throw new RuntimeException("The download of the new bucket version failed. Failed to get the signature file or file failed to download");
            }

            if (string.IsNullOrEmpty(signatureFile))
            {
                io.WriteError("<warning>Skipping signature verification as you have disabled.</warning>");
                return targetPath;
            }

            var signature = JsonFile.Parse<ConfigSignature>(signatureJson);
            if (!signature.TryGetValue(SignatureSha, out string sha))
            {
                throw new RuntimeException($"Does not contain a valid signature field {SignatureSha}.");
            }

            VerifyFile(sha, signatureFile, targetPath);
            return targetPath;
        }

        /// <summary>
        /// Verify that the file matches the signature.
        /// </summary>
        /// <param name="expectedSignatureBase64">Expected signature.</param>
        /// <param name="publicKeyFile">The public key file path.</param>
        /// <param name="verifiedFile">File path to be verified.</param>
        protected virtual void VerifyFile(string expectedSignatureBase64, string publicKeyFile, string verifiedFile)
        {
            if (!fileSystem.Exists(publicKeyFile, FileSystemOptions.File))
            {
                try
                {
                    // Release from embedded file if signature file does not exist locally.
                    using (var embeddedSignature = Resources.GetStream($"Signature/{Path.GetFileName(publicKeyFile)}"))
                    {
                        fileSystem.Write(publicKeyFile, embeddedSignature);
                    }
                }
                catch (FileNotFoundException ex)
                {
                    throw new RuntimeException($"Unable to find signature file from local or embedded resources: {publicKeyFile}", ex);
                }
            }

            var publicKey = fileSystem.ReadString(publicKeyFile);
            using (var verifiedFileStream = fileSystem.Read(verifiedFile))
            {
                if (Security.VerifySignature(verifiedFileStream, expectedSignatureBase64, publicKey))
                {
                    return;
                }

                throw new RuntimeException(
                    "The bucket signature did not match the file you downloaded, this means your public keys are outdated or that the bucket file is corrupt/has been modified");
            }
        }

        /// <summary>
        /// Install a new bucket file to the installation path.
        /// </summary>
        protected virtual void InstalltionBucket(string installDir, string newBucketArchiveFile, string backupFile = null)
        {
            var io = GetIO();

            // Back up the current version of the bucket.
            if (!string.IsNullOrEmpty(backupFile))
            {
                BackupWithZip(installDir, backupFile);
            }

            var tempNewBucketDir = Path.Combine(tempDir, "newer");
            var tempOldBucketDir = Path.Combine(tempDir, "older");

            // Unzip the new bucket to the temporary folder.
            ExtractorZip(newBucketArchiveFile, tempNewBucketDir);

            bool NoBackup()
            {
                if (string.IsNullOrEmpty(backupFile))
                {
                    io.WriteError($"Abnormal. The operation cannot continue.");
                    return true;
                }

                return false;
            }

            // Synchronize permissions to the new bucket.
            AssignPermission(installDir, tempNewBucketDir, io);

            try
            {
                // The bucket is running, we can only move to the temporary
                // file (to be deleted) instead of deleting it immediately.
                MoveContents(installDir, tempOldBucketDir);
            }
            catch when (!NoBackup())
            {
                // The move path needs to lock program data and is rejected
                // by the operating system. This situation can never be rolled
                // back and reverted. Tell developers to manually roll back.
                io.WriteError($"Abnormal. The operation cannot continue. The bucket may be damaged. Please restore it manually: {backupFile}");
                throw;
            }

            try
            {
                MoveContents(tempNewBucketDir, installDir);
            }
            catch when (!NoBackup())
            {
                // Try to restore the changed.
                try
                {
                    MoveContents(installDir, tempNewBucketDir);
                    ExtractorZip(backupFile, installDir);
                    io.WriteError("Abnormal. The change is rollback");
                }
                catch
                {
                    io.WriteError($"Abnormal. The operation cannot continue. rollback failed. Please restore it manually:{backupFile}");
                    throw;
                }

                // Exception trigger should not be placed in the catch,
                // otherwise it will lead to incorrect prompts.
                throw;
            }
        }

        /// <summary>
        /// Back up the contents of the specified folder to the zip file.
        /// </summary>
        protected virtual string BackupWithZip(string backupFromDir, string backupToFile)
        {
            var archiver = new ArchiverZip();

            if (fileSystem.Exists(backupToFile))
            {
                fileSystem.Delete(backupToFile);
            }

            return archiver.Archive(backupFromDir, backupToFile, new[] { $"{TempDirName}/" });
        }

        /// <summary>
        /// Assign the bucket permission in the installation path to the new version of the bucket.
        /// </summary>
        protected virtual void AssignPermission(string installDir, string newBucketDir, IIO io)
        {
            installDir = BaseFileSystem.GetNormalizePath(installDir);
            var tempDirFullPath = BaseFileSystem.GetNormalizePath(Path.Combine(installDir, TempDirName));

            var failed = 0;
            foreach (var path in fileSystem.Walk(installDir))
            {
                if (BaseFileSystem.GetNormalizePath(path).StartsWith(tempDirFullPath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Guard.Requires<UnexpectedException>(
                    path.StartsWith(installDir, StringComparison.OrdinalIgnoreCase),
                    "The starting path must match the installation path.");

                var relativePath = path.Substring(installDir.Length).TrimStart('\\', '/');

                try
                {
                    Permission.TransferPerms(path, Path.Combine(newBucketDir, relativePath));
                }
                catch (PrivilegeNotHeldException)
                {
                    // Permission synchronization under Window is not required.
                    io.WriteError("Bucket does not have permission to adjust the update file permission, permission synchronization aborted.", verbosity: Verbosities.Debug);
                    break;
                }
#pragma warning disable CA1031
                catch (SException ex)
#pragma warning restore CA1031
                {
                    // If the authorization fails, we will intercept the exception, because
                    // the developer can authorize manually, which is not necessary.
                    io.WriteError($"Permission synchronization failed: {BaseFileSystem.GetNormalizePath(relativePath)} ({ex.Message})");
                    failed++;
                }
            }

            if (failed > 0)
            {
                io.WriteError($"There are {failed} files failed to synchronize permissions, you need to synchronize manually. Bucket may not run until permission issues are resolved.");
            }
        }

        /// <summary>
        /// Unzip the file to the specified path.
        /// </summary>
        protected virtual void ExtractorZip(string file, string extractPath)
        {
            var extractor = new ExtractorZip(GetIO(), fileSystem);
            extractor.Extract(file, extractPath);
        }

        protected virtual void MoveContents(string source, string destination)
        {
            // key is target path, value is source path.
            var successful = new Dictionary<string, string>();

            try
            {
                var contents = fileSystem.GetContents(source);
                foreach (var directory in contents.GetDirectories())
                {
                    if (Path.GetFullPath(directory) == tempDir)
                    {
                        continue;
                    }

                    // Get the name of the directory, not the directory path.
                    var target = Path.Combine(destination, Path.GetFileName(directory));
                    fileSystem.Move(directory, target);
                    successful.Add(target, directory);
                }

                foreach (var file in contents.GetFiles())
                {
                    var target = Path.Combine(destination, Path.GetFileName(file));
                    fileSystem.Move(file, target);
                    successful.Add(target, file);
                }
            }
            catch
            {
                // Rollback the change. If an exception occurs, we will not
                // resume it.
                foreach (var successItem in successful)
                {
                    fileSystem.Move(successItem.Key, successItem.Value);
                }

                throw;
            }
        }

        /// <summary>
        /// Get the last available version.
        /// </summary>
        /// <remarks>This means that the highest version that can be rolled back will be obtained.</remarks>
        /// <returns>The backup file name. exclude <see cref="BackupExtensionName"/>.</returns>
        protected virtual string GetLastVersionWithBackup(string backupDir, bool desc = true)
        {
            var direction = desc ? -1 : 1;
            var files = fileSystem.GetContents(backupDir).GetFiles();

            if (files.Length <= 0)
            {
                return null;
            }

            var processedFiles = Arr.Map(files, (file) =>
            {
                if (string.IsNullOrEmpty(file) || !file.EndsWith(BackupExtensionName, StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                return new
                {
                    ModifiedTime = fileSystem.GetMetaData(file).LastModified,
                    File = file,
                };
            });

            processedFiles = Arr.Filter(processedFiles, (processedFile) => processedFile != null);
            if (processedFiles.Length <= 0)
            {
                return null;
            }

            Array.Sort(processedFiles, (x, y) => x.ModifiedTime < y.ModifiedTime ? -direction : direction);

            var filename = Path.GetFileName(processedFiles[0].File);
            return filename.Substring(0, filename.Length - BackupExtensionName.Length);
        }

        private class ConfigSignature : Dictionary<string, string>
        {
        }
    }
}
