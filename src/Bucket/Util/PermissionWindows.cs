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
using System.Security.AccessControl;

namespace Bucket.Util
{
    /// <summary>
    /// Windows permission helper for files or folders.
    /// </summary>
    internal static class PermissionWindows
    {
        /// <summary>
        /// Transfer the permissions of the source file(or dir) to the target file(or dir).
        /// </summary>
        /// <remarks>It is not allowed to pass permissions between folders and files, this will return false.</remarks>
        /// <returns>True if transfer successful. false if the <paramref name="destination"/> not exists.</returns>
        public static bool TransferPerms(string source, string destination, bool isDir)
        {
            if (isDir)
            {
                var security = new DirectoryInfo(source).GetAccessControl(AccessControlSections.All);
                new DirectoryInfo(destination).SetAccessControl(security);
            }
            else
            {
                var security = new FileInfo(source).GetAccessControl(AccessControlSections.All);
                new FileInfo(destination).SetAccessControl(security);
            }

            return true;
        }
    }
}
