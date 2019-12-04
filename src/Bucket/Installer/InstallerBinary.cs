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
using Bucket.Package;
using Bucket.Util;
using GameBox.Console.Process;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Bucket.Installer
{
    /// <summary>
    /// Utility to handle installation of package "bin".
    /// </summary>
    public class InstallerBinary
    {
        private readonly IIO io;
        private readonly string binDir;
        private readonly string binCompat;
        private readonly IFileSystem fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="InstallerBinary"/> class.
        /// </summary>
        /// <param name="io">The input/output instance.</param>
        /// <param name="binDir">The binary folder.</param>
        /// <param name="binCompat">Indicates that the binary file dynamically generates compatibility.</param>
        /// <param name="fileSystem">The file system instance.</param>
        public InstallerBinary(IIO io, string binDir, string binCompat, IFileSystem fileSystem = null)
        {
            this.io = io;
            this.binDir = binDir;
            this.binCompat = binCompat;
            this.fileSystem = fileSystem ?? new FileSystemLocal();
        }

        /// <summary>
        /// Determine the caller of the binary.
        /// </summary>
        /// <param name="binPath">Absolute path to the binary file.</param>
        /// <param name="fileSystem">If a file system is given, an attempt is made to determine the invoker through the file header.</param>
        /// <param name="defaultCaller">The default caller.</param>
        public static string DetermineBinaryCaller(string binPath, IFileSystem fileSystem = null, string defaultCaller = "dotnet")
        {
            var extension = Path.GetExtension(binPath);
            if (extension == ".exe" || extension == ".bat")
            {
                return "call";
            }

            if (fileSystem == null)
            {
                return defaultCaller;
            }

            var line = fileSystem.Read(binPath)?.ReadLine() ?? string.Empty;
            var match = Regex.Match(line, "^#!/(?:usr/bin/env )?(?:[^/]+/)*(?<caller>.+)$", RegexOptions.Multiline);
            if (match.Success)
            {
                return match.Groups["caller"].Value.Trim();
            }

            return defaultCaller;
        }

        /// <summary>
        /// Installs binaries file.
        /// </summary>
        /// <param name="package">The package instance.</param>
        /// <param name="installPath">The path when package installed.</param>
        /// <param name="warnOnOverwrite">Whether is display warning on overwrite the exists file.</param>
        public virtual void Install(IPackage package, string installPath, bool warnOnOverwrite = true)
        {
            var binaries = GetBinaries(package);
            if (binaries == null || binaries.Length <= 0)
            {
                return;
            }

            foreach (var bin in binaries)
            {
                var binPath = Path.Combine(installPath, bin);

                // in case a custom installer returned a relative path for the
                // package, we can now safely turn it into a absolute path (as we
                // already checked the binary's existence). The following helpers
                // will require absolute paths to work properly.
                binPath = Path.Combine(Environment.CurrentDirectory, binPath);

                if (!fileSystem.Exists(binPath, FileSystemOptions.File))
                {
                    io.WriteError($"    <warning>Skipped installation of bin \"{bin}\" for package \"{package}\": file not found in package.</warning>");
                    continue;
                }

                var link = Path.Combine(GetBinDir(), Path.GetFileName(binPath));
                if (fileSystem.Exists(link))
                {
                    // likely leftover from a previous install, make sure that the
                    // target is still executable in case this is a fresh install
                    // of the vendor.
                    Permission.Chmod(link, 777);

                    if (warnOnOverwrite)
                    {
                        io.WriteError($"    Skipped installation of bin \"{bin}\" for package \"{package}\": name conflicts with an existing file.");
                    }

                    continue;
                }

                if (binCompat == "auto")
                {
                    if (Platform.IsWindows)
                    {
                        // In order to enable cygwin support, we have to
                        // install full under windows.
                        InstallFullBinaries(binPath, link, bin, package);
                    }
                    else
                    {
                        InstallUnixProxyBinaries(binPath, link);
                    }
                }
                else if (binCompat == "full")
                {
                    InstallFullBinaries(binPath, link, bin, package);
                }
                else
                {
                    throw new UnexpectedException("The config bin-compat must be one of the following values: auto, full.");
                }

                // make sure that the bin is still executable.
                Permission.Chmod(binPath, 777);
            }
        }

        /// <summary>
        /// Remove installed binaries file.
        /// </summary>
        /// <param name="package">The package instance.</param>
        public virtual void Remove(IPackage package)
        {
            var binaries = GetBinaries(package);
            if (binaries == null || binaries.Length <= 0)
            {
                return;
            }

            var binDirPath = GetBinDir();
            foreach (var bin in binaries)
            {
                var link = Path.Combine(binDirPath, Path.GetFileName(bin));
                fileSystem.Delete(link);
                fileSystem.Delete($"{link}.bat");
            }

            if (fileSystem.IsEmptyDirectory(binDirPath))
            {
                fileSystem.Delete(binDirPath);
            }
        }

        /// <summary>
        /// Gets an array of the binaries folder.
        /// </summary>
        /// <param name="package">The package instance.</param>
        protected virtual string[] GetBinaries(IPackage package)
        {
            return package.GetBinaries();
        }

        /// <summary>
        /// Get an absolute path to represent a vendor dir.
        /// </summary>
        protected virtual string GetBinDir()
        {
            return Path.Combine(Environment.CurrentDirectory, binDir).TrimEnd('/', '\\');
        }

        /// <summary>
        /// Install binary packages and use full compatibility.
        /// </summary>
        /// <param name="binPath">Absolute path to the binary file.</param>
        /// <param name="link">Absolute path to link to.</param>
        /// <param name="bin">Relative path of the binary file.</param>
        /// <param name="package">The package instance.</param>
        protected virtual void InstallFullBinaries(string binPath, string link, string bin, IPackage package)
        {
            // If it is the end of bat then there is no need to build
            // unix support, because there is no use for the build.
            var extension = Path.GetExtension(binPath);
            if (extension != ".bat")
            {
                InstallUnixProxyBinaries(binPath, link);

                link = $"{link}.bat";
                if (fileSystem.Exists(link))
                {
                    Permission.Chmod(link, 777);
                    io.WriteError($"    Skipped installation of bin \"{bin}.bat\" proxy for package \"{package}\": a .bat proxy was already installed.");
                    return;
                }
            }

            InstallWindowsProxyBinaries(binPath, link);
        }

        /// <summary>
        /// Install binary packages and use unix compatibility.
        /// </summary>
        /// <param name="binPath">Absolute path to the binary file.</param>
        /// <param name="link">Absolute path to link to.</param>
        protected virtual void InstallUnixProxyBinaries(string binPath, string link)
        {
            using (var stream = GenerateUnixProxyCode(binPath, link).ToStream())
            {
                fileSystem.Write(link, stream);
            }

            // make sure that the target is still executable.
            Permission.Chmod(link, 777);
        }

        /// <summary>
        /// Install binary packages and use windows compatibility.
        /// </summary>
        /// <param name="binPath">Absolute path to the binary file.</param>
        /// <param name="link">Absolute path to link to.</param>
        protected virtual void InstallWindowsProxyBinaries(string binPath, string link)
        {
            using (var stream = GenerateWindowsProxyCode(binPath, link).ToStream())
            {
                fileSystem.Write(link, stream);
            }

            Permission.Chmod(link, 777);
        }

        /// <summary>
        /// Generate compatibility files under windows.
        /// </summary>
        /// <param name="binPath">The package's bin file.</param>
        /// <param name="link">The link file.</param>
        /// <returns>Returns the compatible content.</returns>
        protected virtual string GenerateWindowsProxyCode(string binPath, string link)
        {
            var caller = DetermineBinaryCaller(binPath, fileSystem);
            binPath = BaseFileSystem.GetRelativePath(link, binPath);

            return "@ECHO OFF\r\n" +
            "setlocal DISABLEDELAYEDEXPANSION\r\n" +
            $"SET BIN_TARGET=%~dp0/{ProcessExecutor.Escape(binPath).Trim('\'', '"')}\r\n" +
            $"{caller} \"%BIN_TARGET%\" %*\r\n";
        }

        /// <summary>
        /// Generate compatibility files under unix.
        /// </summary>
        /// <returns>Returns the compatible content.</returns>
        protected virtual string GenerateUnixProxyCode(string binPath, string link)
        {
            binPath = BaseFileSystem.GetRelativePath(link, binPath);
            var binDirPath = ProcessExecutor.Escape(Path.GetDirectoryName(binPath));
            var binFile = Path.GetFileName(binPath);

            var proxyCode =
@"#!/usr/bin/env sh

dir=$(cd ""${0%[/\\]*}"" > /dev/null; cd %binDirPath% && pwd)

if [ -d /proc/cygdrive ]; then
    case $(which dotnet) in
        $(readlink -n /proc/cygdrive)/*)
            dir=$(cygpath -m ""$dir"");
            ;;
    esac
fi

""${dir}/%binFile%"" ""$@""
";

            proxyCode = proxyCode.Replace("%binDirPath%", binDirPath);
            proxyCode = proxyCode.Replace("%binFile%", binFile);
            proxyCode = proxyCode.Replace("\r\n", "\n");

            return proxyCode;
        }
    }
}
