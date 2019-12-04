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

using Bucket.Util;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Bucket.FileSystem
{
    /// <summary>
    /// <see cref="IFileSystem"/> extension function.
    /// </summary>
    public static class ExtensionIFileSystem
    {
        /// <summary>
        /// Check the directory is empty.
        /// </summary>
        /// <param name="fileSystem">The file system instance.</param>
        /// <param name="path">The directory path.</param>
        /// <returns>True if the directory is empty, othewise return false if path is file or not empty.</returns>
        public static bool IsEmptyDirectory(this IFileSystem fileSystem, string path)
        {
            if (!fileSystem.Exists(path, FileSystemOptions.Directory))
            {
                return false;
            }

            var contents = fileSystem.GetContents(path);

            if (!Platform.IsWindows)
            {
                var files = contents.GetFiles();
                if (files.Length == 1 && Path.GetFileName(files[0]) == ".DS_Store")
                {
                    return contents.GetDirectories().Length == 0;
                }
            }

            // todo: bug! there are multiple empty folders.
            return contents.GetFiles().Length == 0 && contents.GetDirectories().Length == 0;
        }

        /// <summary>
        /// Walk specified path all directory and files.
        /// </summary>
        /// <param name="fileSystem">The file system instance.</param>
        /// <param name="path">The specified path.</param>
        public static IEnumerable<string> Walk(this IFileSystem fileSystem, string path)
        {
            var contents = fileSystem.GetContents(path);

            foreach (var directory in contents.GetDirectories())
            {
                yield return directory;
                foreach (var file in fileSystem.Walk(directory))
                {
                    yield return file;
                }
            }

            foreach (var file in contents.GetFiles())
            {
                yield return file;
            }
        }

        /// <inheritdoc cref="IFileSystem.Write(string, Stream, bool)"/>
        /// <param name="content">The content will writed.</param>
        /// <param name="encoding">The content encoding.</param>
        public static void Write(this IFileSystem fileSystem, string path, string content, bool append = false, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;
            using (var stream = content.ToStream(encoding))
            {
                fileSystem.Write(path, stream, append);
            }
        }

        /// <inheritdoc cref="IFileSystem.Read(string)"/>
        /// <param name="encoding">The content encoding.</param>
        public static string ReadString(this IFileSystem fileSystem, string path, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;
            using (var stream = fileSystem.Read(path))
            {
                return stream.ToText(encoding, false);
            }
        }
    }
}
