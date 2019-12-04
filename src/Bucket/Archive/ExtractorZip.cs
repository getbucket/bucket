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

using Bucket.Exception;
using Bucket.FileSystem;
using Bucket.IO;
using Bucket.Util;
using GameBox.Console.Process;
using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.ExceptionServices;
using SException = System.Exception;

namespace Bucket.Archive
{
    /// <summary>
    /// Extract zip file.
    /// </summary>
    public class ExtractorZip
    {
        private readonly IIO io;
        private readonly IFileSystem fileSystem;
        private readonly IProcessExecutor process;
        private bool? hasUnzipCommand;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtractorZip"/> class.
        /// </summary>
        public ExtractorZip(IIO io, IFileSystem fileSystem = null, IProcessExecutor process = null)
        {
            this.io = io;
            this.process = process ?? new BucketProcessExecutor(io);
            this.fileSystem = fileSystem ?? new FileSystemLocal(process: this.process);
        }

        /// <summary>
        /// Extract file to target directory.
        /// </summary>
        /// <param name="file">The extracted file.</param>
        /// <param name="extractPath">Unzip the local saved directory.</param>
        public void Extract(string file, string extractPath)
        {
            // bug: under macos using unzip will result in incorrect permissions
            // Preferred ExtractWithZipArchive before the problem is resolved
            // issues:145
            ExtractWithZipArchive(file, extractPath);
        }

        /// <summary>
        /// extract <paramref name="file"/> to <paramref name="extractPath"/> with unzip command.
        /// </summary>
        protected internal virtual void ExtractWithUnzipCommand(string file, string extractPath, bool isFallback = false)
        {
            // When called after a ZipArchive failed, perhaps
            // there is some files to overwrite
            var overwrite = isFallback ? "-o " : string.Empty;
            var command = $"unzip -qq {overwrite}{ProcessExecutor.Escape(file)} -d {ProcessExecutor.Escape(extractPath)}";

            FileSystemLocal.EnsureDirectory(extractPath);

            SException processException;
            try
            {
                if (process.Execute(command, out _, out string stderr) == 0)
                {
                    return;
                }

                throw new RuntimeException(
                    $"Failed to execute {command} {Environment.NewLine}{Environment.NewLine} {stderr}");
            }
#pragma warning disable CA1031
            catch (SException ex)
#pragma warning restore CA1031
            {
                processException = ex;
            }

            if (isFallback)
            {
                ExceptionDispatchInfo.Capture(processException).Throw();
            }

            io.WriteError($"    {processException.Message}");
            io.WriteError($"    The archive may contain identical file names with different capitalization (which fails on case insensitive filesystems)");
            io.WriteError($"    Unzip with unzip command failed, falling back to {nameof(ZipArchive)}.");

            ExtractWithZipArchive(file, extractPath, true);
        }

        /// <summary>
        /// extract <paramref name="file"/> to <paramref name="extractPath"/> with <see cref="ZipArchive"/>.
        /// </summary>
        protected internal virtual void ExtractWithZipArchive(string file, string extractPath, bool isFallback = false)
        {
            SException processException;
            try
            {
                using (var archiveStream = fileSystem.Read(file))
                using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read))
                {
                    foreach (var entry in archive.Entries)
                    {
                        // skip folder the file system will automatically
                        // create a folder.
                        if (entry.FullName.EndsWith("/", StringComparison.Ordinal))
                        {
                            continue;
                        }

                        var destinationPath = Path.Combine(extractPath, entry.FullName);
                        using (var fileStream = entry.Open())
                        {
                            fileSystem.Write(destinationPath, fileStream);
                        }
                    }
                }

                return;
            }
#pragma warning disable CA1031
            catch (SException ex)
#pragma warning restore CA1031
            {
                processException = ex;
            }

            if (isFallback || !HasUnzipCommand())
            {
                ExceptionDispatchInfo.Capture(processException).Throw();
            }

            io.WriteError($"    {processException.Message}");
            io.WriteError($"    Unzip with {nameof(ZipArchive)} failed, falling back to unzip command");

            ExtractWithUnzipCommand(file, extractPath, true);
        }

        private bool HasUnzipCommand()
        {
            if (hasUnzipCommand != null)
            {
                return hasUnzipCommand.Value;
            }

            hasUnzipCommand = process.Execute("unzip") == 0;
            return hasUnzipCommand.Value;
        }
    }
}
