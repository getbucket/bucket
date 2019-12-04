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

using GameBox.Console.Util;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

namespace Bucket.Util
{
    /// <summary>
    /// Platform helper for uniform platform-specific tests.
    /// </summary>
    public static class Platform
    {
        /// <summary>
        /// Gets a value indicating whether is windows platform.
        /// </summary>
        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        /// <summary>
        /// Parses tildes and environment variables in paths.
        /// </summary>
        /// <param name="path">The path to expand.</param>
        /// <returns>Returns parsed the path.</returns>
        public static string ExpandPath(string path)
        {
            if (Regex.IsMatch(path, "^~[\\/]"))
            {
                return GetUserDirectory() + path.Substring(1);
            }

            // Match and replace: %VARIABLE%, $VARIABLE
            return Regex.Replace(path, @"^(\$|(?<percent>%))(?<var>\w+)(?(percent)%)(?<path>.*)", (matched) =>
            {
                // Guaranteed to use HOME in windows can also correctly parse.
                if (IsWindows && matched.Groups["var"].Value == "HOME")
                {
                    return (Terminal.GetEnvironmentVariable("HOME") ?? Terminal.GetEnvironmentVariable("USERPROFILE") ?? string.Empty)
                             + matched.Groups["path"].Value;
                }

                return (Terminal.GetEnvironmentVariable(matched.Groups["var"].Value) ?? string.Empty) + matched.Groups["path"].Value;
            });
        }

        /// <summary>
        /// Get user profile directory.
        /// </summary>
        /// <returns>Returns user profile directory.</returns>
        public static string GetUserDirectory()
        {
            var home = Terminal.GetEnvironmentVariable("HOME");
            if (!(home is null))
            {
                return home;
            }

            if (IsWindows)
            {
                home = Terminal.GetEnvironmentVariable("USERPROFILE");
                if (!(home is null))
                {
                    return home;
                }
            }

            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        /// <summary>
        /// Gets the os information.
        /// </summary>
        public static string GetOSInfo()
        {
            return Environment.OSVersion.VersionString;
        }

        /// <summary>
        /// Gets the runtime infomation.
        /// </summary>
        public static string GetRuntimeInfo()
        {
            return Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;
        }
    }
}
