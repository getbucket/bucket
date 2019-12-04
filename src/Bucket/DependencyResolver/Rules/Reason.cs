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

#pragma warning disable SA1602

namespace Bucket.DependencyResolver.Rules
{
    /// <summary>
    /// The reason constants.
    /// </summary>
    public enum Reason
    {
        Undefined = 0,
        InternalAllowUpdate = 1,
        JobInstall = 2,
        JobUninstall = 3,
        PackageConflict = 6,
        PackageRequire = 7,
        PackageObsoletes = 8,

        /// <summary>
        /// This means that the package can only install one of them.
        /// </summary>
        PackageSameName = 9,
        PackageAlias = 10,
        PackageImplicitObsoletes = 11,
        InstalledPackageObsoletes = 12,
        Learned = 13,
    }
}
