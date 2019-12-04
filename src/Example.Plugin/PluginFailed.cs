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
using Bucket.Plugin;
using GameBox.Console.Exception;

namespace Example.Plugin
{
    /// <summary>
    /// Example plugin failed.
    /// </summary>
    public class PluginFailed : IPlugin
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginFailed"/> class.
        /// </summary>
        public PluginFailed(string failedConstructor)
        {
            // An illegal constructor.
            throw new InvalidArgumentException($"Should not create instance with current constructor. {failedConstructor}");
        }

        /// <inheritdoc />
        public string Name => "failed";

        /// <inheritdoc />
        public void Activate(Bucket.Bucket bucket, IIO io)
        {
            io.WriteError($"Activate failed");
        }

        /// <inheritdoc />
        public void Deactivate(Bucket.Bucket bucket, IIO io)
        {
            io.WriteError("Deactivate failed");
        }

        /// <inheritdoc />
        public void Uninstall(Bucket.Bucket bucket, IIO io)
        {
            io.WriteError("Uninstall failed");
        }
    }
}
