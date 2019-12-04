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
using Example.Plugin.Command;
using GameBox.Console.EventDispatcher;
using System;
using System.Collections.Generic;

namespace Example.Plugin
{
    /// <summary>
    /// Example plugin foo.
    /// </summary>
    public class PluginFoo : IPlugin, IEventSubscriber, ICapable
    {
        private IIO io;

        /// <inheritdoc />
        public string Name => "foo";

        /// <inheritdoc />
        public IEnumerable<Type> GetCapabilities()
        {
            return new[]
            {
                typeof(CommandProvider),
            };
        }

        /// <inheritdoc />
        public void Activate(Bucket.Bucket bucket, IIO io)
        {
            this.io = io;
            io.WriteError($"Activate foo");
        }

        /// <inheritdoc />
        public void Deactivate(Bucket.Bucket bucket, IIO io)
        {
            io.WriteError("Deactivate foo");
        }

        /// <inheritdoc />
        public void Uninstall(Bucket.Bucket bucket, IIO io)
        {
            io.WriteError("Uninstall foo");
        }

        /// <inheritdoc />
        public IDictionary<string, EventHandler> GetSubscribedEvents()
        {
            return new Dictionary<string, EventHandler>
            {
                { "foo", OnFoo },
            };
        }

        private void OnFoo(object sender, EventArgs args)
        {
            io.WriteError("Trigger foo event");
        }
    }
}
