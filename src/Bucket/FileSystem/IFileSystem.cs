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

using System.IO;

namespace Bucket.FileSystem
{
    /// <summary>
    /// Representing an abstract file system.
    /// </summary>
    public interface IFileSystem
    {
        /// <summary>
        /// Whether the file or folder is exists.
        /// </summary>
        /// <param name="path">The file or folder path.</param>
        /// <param name="options">Options indicates how to operate.</param>
        /// <returns>True if the file or folder is exists.</returns>
        bool Exists(string path, FileSystemOptions options = FileSystemOptions.Directory | FileSystemOptions.File);

        /// <summary>
        /// Write data from the stream to the specified file.
        /// <para>If not written in the form of append will overwrite the existing file.</para>
        /// </summary>
        /// <param name="path">The specified file path.</param>
        /// <param name="stream">The date stream.</param>
        /// <param name="append">Whether to write in append form.</param>
        void Write(string path, Stream stream, bool append = false);

        /// <summary>
        /// Read the specified file.
        /// </summary>
        /// <param name="path">The specified file path.</param>
        /// <returns>The file stream.</returns>
        Stream Read(string path);

        /// <summary>
        /// Move a file or folder to a specified path.
        /// </summary>
        /// <param name="path">The old file or folder path.</param>
        /// <param name="newPath">The new file or folder path.</param>
        void Move(string path, string newPath);

        /// <summary>
        /// Copy a file or folder to a specified path.
        /// </summary>
        /// <param name="path">The old file or folder path.</param>
        /// <param name="newPath">The new file or folder path.</param>
        /// <param name="overwrite">Whether is overwrite the exists file.</param>
        void Copy(string path, string newPath, bool overwrite = true);

        /// <summary>
        /// Delete a file or folder to a specified path.
        /// </summary>
        /// <param name="path">The file or folder path.</param>
        void Delete(string path = null);

        /// <summary>
        /// Get a list of files and folders under the specified path.
        /// </summary>
        /// <param name="path">The file or folder path.</param>
        /// <returns>Returns the directory content. if the path is file return only file.</returns>
        DirectoryContents GetContents(string path = null);

        /// <summary>
        /// Get the metadata of the specified path file or folder.
        /// </summary>
        /// <param name="path">The file or folder path.</param>
        /// <returns>Return the metadata.</returns>
        IMetaData GetMetaData(string path = null);
    }
}
