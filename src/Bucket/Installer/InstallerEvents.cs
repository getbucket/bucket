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

namespace Bucket.Installer
{
    /// <summary>
    /// <see cref="InstallerEvents"/> lists all installer events.
    /// </summary>
    public static class InstallerEvents
    {
        /// <summary>
        /// The <see cref="PreDependenciesSolving"/> event occurs as a installer begins
        /// resolve operations.
        /// </summary>
        public const string PreDependenciesSolving = "pre-dependencies-solving";

        /// <summary>
        /// The <see cref="PostDependenciesSolving"/> event occurs as a installer after
        /// resolve operations.
        /// </summary>
        public const string PostDependenciesSolving = "post-dependencies-solving";
    }
}
