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
using Bucket.Util;
using GameBox.Console.Process;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using SException = System.Exception;

namespace Bucket.FileSystem
{
    /// <summary>
    /// Represents a local file system implementation.
    /// </summary>
    public class FileSystemLocal : BaseFileSystem, IFileSystem
    {
        private readonly IProcessExecutor process;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSystemLocal"/> class.
        /// </summary>
        /// <param name="root">The root path. access cannot exceed the root path.</param>
        /// <param name="process">Represents a process executor.</param>
        public FileSystemLocal(string root = null, IProcessExecutor process = null)
        {
            this.process = process ?? new BucketProcessExecutor();

            if (string.IsNullOrEmpty(root))
            {
                return;
            }

            if (!Path.IsPathRooted(root))
            {
                throw new ArgumentException($"Path must be rooted [{root}].", nameof(root));
            }

            SetRootPath(root);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the file system is readonly.
        /// </summary>
        public bool Readonly { get; set; } = false;

        /// <summary>
        /// Whether the path is local path.
        /// </summary>
        /// <param name="path">The tests path.</param>
        /// <returns>True if the path is local path.</returns>
        public static bool IsLocalPath(string path)
        {
            return Regex.IsMatch(path, @"^(file://(?!//)|/(?!/)|/?[a-z]:[\\/]|\.\.[\\/]|[a-z0-9_.-]+[\\/])", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Formatted path information without file://.
        /// </summary>
        /// <param name="path">The path information.</param>
        /// <returns>Returns the formatted the path.</returns>
        public static string GetPlatformPath(string path)
        {
            if (Platform.IsWindows)
            {
                path = Regex.Replace(path, "^(?:file:///([a-z]):?/)", "file://${1}:/", RegexOptions.IgnoreCase);
            }

            return Regex.Replace(path, "^file://", string.Empty, RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Ensure the directory exists and writeable.
        /// </summary>
        public static void EnsureDirectory(string path)
        {
            if (File.Exists(path))
            {
                throw new RuntimeException($"{path} exists and is not a directory.");
            }

            // todo: need set 0777 access permission.
            if (Directory.Exists(path))
            {
                return;
            }

            Directory.CreateDirectory(path);
        }

        /// <summary>
        /// Determine if directory is writable.
        /// </summary>
        /// <param name="path">The directory path.</param>
        public static bool IsWriteable(string path)
        {
            EnsureDirectory(path);

            // todo: implement writeable test method.
            return true;
        }

        /// <inheritdoc />
        public override bool Exists(string path, FileSystemOptions options = FileSystemOptions.Directory | FileSystemOptions.File)
        {
            var location = ApplyRootPath(path);

            if (HasOptions(options, FileSystemOptions.Directory)
                && !HasOptions(options, FileSystemOptions.File))
            {
                return Directory.Exists(location);
            }

            if (!HasOptions(options, FileSystemOptions.Directory)
                && HasOptions(options, FileSystemOptions.File))
            {
                return File.Exists(location);
            }

            return IsSuspectFile(location)
                ? File.Exists(location) || Directory.Exists(location)
                : Directory.Exists(location) || File.Exists(location);
        }

        /// <summary>
        /// Create a new file. If the file already exists, it will be overwritten.
        /// </summary>
        public Stream Create(string path, int bufferSize = 4096)
        {
            AssertReadonly();

            var location = ApplyRootPath(path);
            EnsureDirectory(Path.GetDirectoryName(location));

            return new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize);
        }

        /// <inheritdoc />
        public override void Write(string path, Stream stream, bool append = false)
        {
            AssertReadonly();
            Guard.Requires<ArgumentNullException>(stream != null);

            var location = ApplyRootPath(path);
            EnsureDirectory(Path.GetDirectoryName(location));

            if (Directory.Exists(location))
            {
                throw new FileSystemException("File cannot overwrite folder");
            }

            try
            {
                using (var fileStream = new FileStream(location, append ? FileMode.OpenOrCreate : FileMode.Create,
                    FileAccess.Write, FileShare.None))
                {
                    if (append)
                    {
                        fileStream.Position = fileStream.Length;
                    }

                    stream.AppendTo(fileStream);
                }
            }
            catch
            {
                Delete(location);
                throw;
            }
        }

        /// <inheritdoc />
        public override Stream Read(string path)
        {
            var location = ApplyRootPath(path);

            return File.OpenRead(location);
        }

        /// <inheritdoc />
        public override void Move(string path, string newPath)
        {
            AssertReadonly();

            var location = ApplyRootPath(path);
            var destination = ApplyRootPath(newPath);

            if (location == destination)
            {
                throw new FileSystemException($"The target file and source file path are the same: {destination}");
            }

            EnsureDirectory(Path.GetDirectoryName(destination));

            if (Exists(destination))
            {
                throw new FileSystemException($"File or folder already exists at the target location: {destination}");
            }

            // todo: If the target folder already exists and there are
            // files that should be copied file by file. Copy directly
            // if the directory is clean enough.
            if (IsDirectory(location))
            {
                Directory.Move(location, destination);
            }
            else
            {
                File.Move(location, destination);
            }
        }

        /// <inheritdoc />
        public override void Copy(string path, string newPath, bool overwrite = true)
        {
            AssertReadonly();

            var location = ApplyRootPath(path);
            var destination = ApplyRootPath(newPath);

            if (location == destination)
            {
                throw new FileSystemException($"The target file and source file path are the same [{destination}]");
            }

            EnsureDirectory(Path.GetDirectoryName(destination));

            string AssertCopy(string target)
            {
                if (Directory.Exists(target))
                {
                    throw new FileSystemException($"Unable to overwrite file to folder : {target}");
                }

                if (overwrite || !File.Exists(target))
                {
                    return target;
                }

                throw new FileSystemException($"Unable to overwrite existing files, need to change {nameof(overwrite)} to true.");
            }

            if (IsDirectory(location))
            {
                foreach (var file in Directory.GetFiles(location))
                {
                    File.Copy(file, AssertCopy(Path.Combine(destination, Path.GetFileName(file))), overwrite);
                }

                foreach (var directory in Directory.GetDirectories(location))
                {
                    Copy(directory, Path.Combine(destination, Path.GetDirectoryName(directory)));
                }
            }
            else
            {
                File.Copy(location, AssertCopy(destination), overwrite);
            }
        }

        /// <inheritdoc />
        public override void Delete(string path = null)
        {
            AssertReadonly();

            var location = ApplyRootPath(path);

            void DeleteFile(string fileLocation)
            {
                if (!File.Exists(fileLocation))
                {
                    return;
                }

                File.SetAttributes(fileLocation, FileAttributes.Normal);
                File.Delete(fileLocation);
            }

            void DeleteDirectory(string directoryLocation)
            {
                if (!Directory.Exists(directoryLocation))
                {
                    return;
                }

                var files = Directory.GetFiles(directoryLocation);
                var directories = Directory.GetDirectories(directoryLocation);
                Array.ForEach(files, DeleteFile);
                Array.ForEach(directories, DeleteDirectory);
                Directory.Delete(directoryLocation, false);
            }

            try
            {
                DeleteFile(location);
                DeleteDirectory(location);
            }
#pragma warning disable CA1031
            catch (SException)
#pragma warning restore CA1031
            {
                // Retry after a bit on windows since it tends
                // to be touchy with mass removals.
                if (Platform.IsWindows)
                {
                    Thread.Sleep(350);
                }

                try
                {
                    DeleteFile(location);
                    DeleteDirectory(location);
                }
                catch (SException)
                {
                    string command;
                    if (Platform.IsWindows)
                    {
                        command = $"rmdir /S /Q {ProcessExecutor.Escape(location)}";
                    }
                    else
                    {
                        command = $"rm -rf {ProcessExecutor.Escape(location)}";
                    }

                    if (process.Execute(command) != 0)
                    {
                        throw;
                    }
                }
            }
        }

        /// <inheritdoc />
        public override DirectoryContents GetContents(string path = null)
        {
            var location = ApplyRootPath(path);

            if (Directory.Exists(location))
            {
                var files = Arr.Map(Directory.GetFiles(location), RemoveRootPath);
                var directories = Arr.Map(Directory.GetDirectories(location), RemoveRootPath);
                return new DirectoryContents(directories, files);
            }
            else
            {
                if (File.Exists(location))
                {
                    var meta = GetMetaData(location);
                    return new DirectoryContents(Array.Empty<string>(), new[] { meta.Path });
                }

                return new DirectoryContents(Array.Empty<string>(), Array.Empty<string>());
            }
        }

        /// <inheritdoc />
        public override IMetaData GetMetaData(string path = null)
        {
            var location = ApplyRootPath(path);

            if (IsDirectory(location))
            {
                return new DirectoryMetaData(this, location);
            }

            return new FileMetaData(this, location);
        }

        /// <summary>
        /// Whether is a directory.
        /// </summary>
        /// <param name="path">The specified path.</param>
        /// <returns>True if the path is directory.</returns>
        protected internal static bool IsDirectory(string path)
        {
            return (File.GetAttributes(path) & FileAttributes.Directory) == FileAttributes.Directory;
        }

        /// <summary>
        /// Whether it is a suspected file.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <returns>True if suspected file.</returns>
        protected static bool IsSuspectFile(string location)
        {
            return location.LastIndexOf(Path.AltDirectorySeparatorChar) < location.LastIndexOf('.');
        }

        /// <summary>
        /// Get the size of the file or folder (bytes).
        /// </summary>
        /// <param name="path">The file or folder path.</param>
        /// <returns>Returns the size of the file or folder.</returns>
        protected long GetSize(string path)
        {
            var location = ApplyRootPath(path);

            long size = 0;
            if (IsDirectory(location))
            {
                foreach (var file in Directory.GetFiles(location))
                {
                    size += new FileInfo(file).Length;
                }

                foreach (var info in Directory.GetDirectories(location))
                {
                    size += GetSize(info);
                }
            }
            else
            {
                size += new FileInfo(location).Length;
            }

            return size;
        }

        /// <summary>
        /// Assertion read-only mode.
        /// </summary>
        protected virtual void AssertReadonly()
        {
            if (Readonly)
            {
                throw new NotSupportedException("Access mode is readonly");
            }
        }

        private bool HasOptions(FileSystemOptions options, FileSystemOptions expected)
        {
            return (options & expected) == expected;
        }

        /// <summary>
        /// The base metadata.
        /// </summary>
        private abstract class BaseMetaData : IMetaData
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="BaseMetaData"/> class.
            /// </summary>
            /// <param name="fileSystem">The local file system instance.</param>
            /// <param name="info">The base class for both FileInfo or DirectoryInfo.</param>
            protected BaseMetaData(FileSystemLocal fileSystem, FileSystemInfo info)
            {
                FileSystemInfo = info;
                FileSystem = fileSystem;
            }

            /// <inheritdoc />
            public string Name => FileSystemInfo.Name;

            /// <inheritdoc />
            public string Path => FileSystem.RemoveRootPath(FileSystemInfo.FullName);

            /// <inheritdoc />
            public virtual string ParentDirectory => throw new NotSupportedException();

            /// <inheritdoc />
            public string MimeType => IsDirectory ? string.Empty : MimeTypes.GetMimeType(Name);

            /// <inheritdoc />
            public virtual long Size => throw new NotSupportedException();

            /// <inheritdoc />
            public DateTime LastModified => FileSystemInfo.LastWriteTime;

            /// <inheritdoc />
            public DateTime LastAccessTime
            {
                get => FileSystemInfo.LastAccessTime;
                set => FileSystemInfo.LastAccessTime = value;
            }

            /// <inheritdoc />
            public bool IsDirectory => (FileSystemInfo.Attributes & FileAttributes.Directory) == FileAttributes.Directory;

            /// <summary>
            /// Gets the local file system instance.
            /// </summary>
            protected FileSystemLocal FileSystem { get; }

            /// <summary>
            /// Gets the base class for both FileInfo or DirectoryInfo.
            /// </summary>
            protected FileSystemInfo FileSystemInfo { get; }

            /// <inheritdoc />
            public virtual void Refresh()
            {
                FileSystemInfo.Refresh();
            }
        }

        /// <summary>
        /// Represents the metadata of the file.
        /// </summary>
        private sealed class FileMetaData : BaseMetaData
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="FileMetaData"/> class.
            /// </summary>
            /// <param name="fileSystem">The local file system instance.</param>
            /// <param name="path">The path of the file.</param>
            public FileMetaData(FileSystemLocal fileSystem, string path)
                : base(fileSystem, new FileInfo(path))
            {
            }

            /// <inheritdoc />
            public override string ParentDirectory => FileSystem.RemoveRootPath(FileInfo.DirectoryName);

            /// <inheritdoc />
            public override long Size => FileInfo.Length;

            private FileInfo FileInfo => (FileInfo)FileSystemInfo;
        }

        /// <summary>
        /// Represents the metadata of the folder.
        /// </summary>
        private sealed class DirectoryMetaData : BaseMetaData
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="DirectoryMetaData"/> class.
            /// </summary>
            /// <param name="fileSystem">The local file system instance.</param>
            /// <param name="path">The path of the directory.</param>
            public DirectoryMetaData(FileSystemLocal fileSystem, string path)
                : base(fileSystem, new DirectoryInfo(path))
            {
            }

            /// <inheritdoc />
            public override string ParentDirectory
            {
                get
                {
                    var location = DirectoryInfo.FullName;
                    if (location.Length > 3 && location[location.Length - 1] == System.IO.Path.AltDirectorySeparatorChar)
                    {
                        location = location.Substring(0, location.Length - 1);
                    }

                    return FileSystem.RemoveRootPath(System.IO.Path.GetDirectoryName(location));
                }
            }

            /// <inheritdoc />
            public override long Size => FileSystem.GetSize(FileSystem.RemoveRootPath(DirectoryInfo.FullName));

            private DirectoryInfo DirectoryInfo => (DirectoryInfo)FileSystemInfo;
        }
    }
}
