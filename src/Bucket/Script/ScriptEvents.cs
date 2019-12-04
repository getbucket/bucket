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

using Bucket.Installer;

namespace Bucket.Script
{
    /// <summary>
    /// <see cref="ScriptEvents"/> lists all script events.
    /// </summary>
    public static class ScriptEvents
    {
        /// <summary>
        /// The <see cref="PreInstallCMD"/> event occurs before the install command is executed.
        /// </summary>
        /// <remarks>The event listener method receives a <see cref="ScriptEventArgs"/> instance.</remarks>
        public const string PreInstallCMD = "pre-install-cmd";

        /// <summary>
        /// The <see cref="PostInstallCMD"/> event occurs after the install command is executed.
        /// </summary>
        /// <remarks>The event listener method receives a <see cref="ScriptEventArgs"/> instance.</remarks>
        public const string PostInstallCMD = "post-install-cmd";

        /// <summary>
        /// The <see cref="PreUpdateCMD"/> event occurs before the update command is executed.
        /// </summary>
        /// <remarks>The event listener method receives a <see cref="ScriptEventArgs"/> instance.</remarks>
        public const string PreUpdateCMD = "pre-update-cmd";

        /// <summary>
        /// The <see cref="PostUpdateCMD"/> event occurs after the update command is executed.
        /// </summary>
        /// <remarks>The event listener method receives a <see cref="ScriptEventArgs"/> instance.</remarks>
        public const string PostUpdateCMD = "post-update-cmd";

        /// <summary>
        /// The <see cref="PrePackageInstall"/> event occurs before a package is installed.
        /// </summary>
        /// <remarks>The event listener method receives a <see cref="PackageEventArgs"/> instance.</remarks>
        public const string PrePackageInstall = "pre-package-install";

        /// <summary>
        /// The <see cref="PostPackageInstall"/> event occurs after a package is installed.
        /// </summary>
        /// <remarks>The event listener method receives a <see cref="PackageEventArgs"/> instance.</remarks>
        public const string PostPackageInstall = "post-package-install";

        /// <summary>
        /// The <see cref="PrePackageUpdate"/> event occurs before a package is updated.
        /// </summary>
        /// <remarks>The event listener method receives a <see cref="PackageEventArgs"/> instance.</remarks>
        public const string PrePackageUpdate = "pre-package-update";

        /// <summary>
        /// The <see cref="PostPackageUpdate"/> event occurs after a package is updated.
        /// </summary>
        /// <remarks>The event listener method receives a <see cref="PackageEventArgs"/> instance.</remarks>
        public const string PostPackageUpdate = "post-package-update";

        /// <summary>
        /// The <see cref="PrePackageUninstall"/> event occurs before a package is uninstalled.
        /// </summary>
        /// <remarks>The event listener method receives a <see cref="PackageEventArgs"/> instance.</remarks>
        public const string PrePackageUninstall = "pre-package-uninstall";

        /// <summary>
        /// The <see cref="PostPackageUninstall"/> event occurs after a package is uninstalled.
        /// </summary>
        /// <remarks>The event listener method receives a <see cref="PackageEventArgs"/> instance.</remarks>
        public const string PostPackageUninstall = "post-package-uninstall";

        /// <summary>
        /// Get an array of all scripts events.
        /// </summary>
        public static string[] GetEvents()
        {
            return new[]
            {
                PreInstallCMD,
                PostInstallCMD,
                PreUpdateCMD,
                PostUpdateCMD,
                PrePackageInstall,
                PostPackageInstall,
                PrePackageUpdate,
                PostPackageUpdate,
                PrePackageUninstall,
                PostPackageUninstall,
            };
        }
    }
}
