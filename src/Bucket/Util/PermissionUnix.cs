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
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Bucket.Util
{
    /// <summary>
    /// Unix permission helper for files or folders.
    /// </summary>
    internal static class PermissionUnix
    {
        [Flags]
        private enum FileAccessPermissions
        {
            None = 0,
            UserRead = 256,
            UserWrite = 128,
            UserExecute = 64,
            GroupRead = 32,
            GroupWrite = 16,
            GroupExecute = 8,
            OtherRead = 4,
            OtherWrite = 2,
            OtherExecute = 1,
            UserReadWriteExecute = UserRead | UserWrite | UserExecute,
            GroupReadWriteExecute = GroupRead | GroupWrite | GroupExecute,
            OtherReadWriteExecute = OtherRead | OtherWrite | OtherExecute,
            AllPermissions = UserReadWriteExecute | GroupReadWriteExecute | OtherReadWriteExecute,
        }

        /// <summary>
        /// Transfer the permissions of the source file(or dir) to the target file(or dir).
        /// </summary>
        /// <remarks>It is not allowed to pass permissions between folders and files, this will return false.</remarks>
        /// <returns>True if transfer successful. false if the <paramref name="destination"/> not exists.</returns>
        public static bool TransferPerms(string source, string destination)
        {
            var process = new BucketProcessExecutor();

            string command;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                command = $"stat -f %A \"{source}\"";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                command = $"stat -c %a \"{source}\"";
            }
            else
            {
                throw new NotSupportedException("Not support the platform.");
            }

            if (process.Execute(command, out string stdout) != 0 || !int.TryParse(stdout, out int perms))
            {
                throw new RuntimeException($"Unable to get permissions: {source}");
            }

            Chmod(destination, perms);
            return true;
        }

        /// <summary>
        /// Attempts to change the mode of the specified file or dir to that given in mode.
        /// </summary>
        /// <param name="path">The specified file or dir.</param>
        /// <param name="mode">The mode for the unix.</param>
        public static void Chmod(string path, int mode)
        {
            var prems = GetFileAccessPermissionsFromMode(mode.ToString());
            if (SysChmod(path, (int)prems) != 0)
            {
                var errcode = Marshal.GetLastWin32Error();
                throw new Win32Exception(errcode);
            }
        }

        private static FileAccessPermissions GetFileAccessPermissionsFromMode(string mode)
        {
            if (mode.Length < 3)
            {
                mode = Str.Pad(3, mode, "0", Str.PadType.Left);
            }
            else
            {
                mode = mode.Substring(mode.Length - 3, 3);
            }

            var userGroupOthers = new[] { mode[0], mode[1], mode[2] };
            FileAccessPermissions GetFileAccessPermissionsFromOctalValue(char value)
            {
                switch (value)
                {
                    case '0': return FileAccessPermissions.None;
                    case '1': return FileAccessPermissions.OtherExecute;
                    case '2': return FileAccessPermissions.OtherWrite;
                    case '3': return FileAccessPermissions.OtherRead;
                    case '4': return FileAccessPermissions.OtherWrite | FileAccessPermissions.OtherExecute;
                    case '5': return FileAccessPermissions.OtherRead | FileAccessPermissions.OtherExecute;
                    case '6': return FileAccessPermissions.OtherRead | FileAccessPermissions.OtherWrite;
                    case '7': return FileAccessPermissions.OtherReadWriteExecute;
                    default:
                        throw new RuntimeException($"Unsupported octal permissions: {value}");
                }
            }

            var perms = (int)GetFileAccessPermissionsFromOctalValue(userGroupOthers[0]) << 6;
            perms |= (int)GetFileAccessPermissionsFromOctalValue(userGroupOthers[1]) << 3;
            perms |= (int)GetFileAccessPermissionsFromOctalValue(userGroupOthers[2]);

            return (FileAccessPermissions)perms;
        }

#pragma warning disable CA2101

        [DllImport("libc", SetLastError = true, EntryPoint = "chmod")]
        private static extern int SysChmod(string pathname, int mode);
    }
}
