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

using Bucket.IO;

namespace Bucket.Plugin
{
    /// <summary>
    /// The plugin interface.
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        /// Gets a value indicates the name of the plugin.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Apply plugin modifications to Bucket.
        /// </summary>
        void Activate(Bucket bucket, IIO io);

        /// <summary>
        /// Remove any hooks from Bucket.
        /// </summary>
        void Deactivate(Bucket bucket, IIO io);

        /// <summary>
        /// Prepare the plugin to be uninstalled.
        /// </summary>
        /// <remarks>This will be called after deactivate.</remarks>
        void Uninstall(Bucket bucket, IIO io);
    }
}
