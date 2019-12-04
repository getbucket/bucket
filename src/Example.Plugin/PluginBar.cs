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
using GameBox.Console.EventDispatcher;
using System;
using System.Collections.Generic;

namespace Example.Plugin
{
    /// <summary>
    /// Example plugin bar.
    /// </summary>
    public class PluginBar : IPlugin, IEventSubscriber
    {
        private IIO io;

        /// <inheritdoc />
        public string Name => "bar";

        /// <inheritdoc />
        public void Activate(Bucket.Bucket bucket, IIO io)
        {
            this.io = io;
            io.WriteError($"Activate bar");
        }

        /// <inheritdoc />
        public void Deactivate(Bucket.Bucket bucket, IIO io)
        {
            io.WriteError("Deactivate bar");
        }

        /// <inheritdoc />
        public void Uninstall(Bucket.Bucket bucket, IIO io)
        {
            io.WriteError("Uninstall bar");
        }

        /// <inheritdoc />
        public IDictionary<string, EventHandler> GetSubscribedEvents()
        {
            return new Dictionary<string, EventHandler>
            {
                { "bar", OnBar },
            };
        }

        private void OnBar(object sender, EventArgs args)
        {
            io.WriteError("Trigger bar event");
        }
    }
}
