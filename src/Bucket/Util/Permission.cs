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

using Bucket.FileSystem;

namespace Bucket.Util
{
    /// <summary>
    /// Cross-platform's permission helper for files or folders.
    /// </summary>
    public static class Permission
    {
        private static readonly FileSystemLocal FileSystem = new FileSystemLocal();

        /// <summary>
        /// Transfer the permissions of the source file(or dir) to the target file(or dir).
        /// </summary>
        /// <remarks>It is not allowed to pass permissions between folders and files, this will return false.</remarks>
        /// <returns>True if transfer successful. false if the <paramref name="destination"/> not exists.</returns>
        public static bool TransferPerms(string source, string destination)
        {
            if (!FileSystem.Exists(source) || !FileSystem.Exists(destination))
            {
                return false;
            }

            var sourceIsDir = FileSystemLocal.IsDirectory(source);
            var destinationIsDir = FileSystemLocal.IsDirectory(destination);

            // Ensure source is same type for the destination.
            if (sourceIsDir != destinationIsDir)
            {
                return false;
            }

            if (Platform.IsWindows)
            {
                return PermissionWindows.TransferPerms(source, destination, sourceIsDir);
            }

            return PermissionUnix.TransferPerms(source, destination);
        }

        /// <summary>
        /// Attempts to change the mode of the specified file or dir to that given in mode.
        /// </summary>
        /// <param name="path">The specified file or dir.</param>
        /// <param name="mode">The mode for the unix.</param>
        public static void Chmod(string path, int mode)
        {
            // Only valid under unix.
            if (Platform.IsWindows || !FileSystem.Exists(path))
            {
                return;
            }

            PermissionUnix.Chmod(path, mode);
        }
    }
}
