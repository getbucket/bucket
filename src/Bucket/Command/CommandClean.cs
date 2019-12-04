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

using Bucket.Cache;
using Bucket.Configuration;
using Bucket.EventDispatcher;
using Bucket.IO;
using Bucket.Plugin;
using GameBox.Console;
using GameBox.Console.Input;
using GameBox.Console.Output;
using System;
using System.IO;

namespace Bucket.Command
{
    /// <summary>
    /// This command clean the cache file.
    /// </summary>
    public class CommandClean : BaseCommand
    {
        private Config config;
        private IIO io;

        /// <inheritdoc />
        protected override void Configure()
        {
            SetName("clean")
                .SetDescription("Clears bucket's internal package cache.")
                .AddOption("gc", null, InputOptionModes.ValueNone, "Use the GC to clean up the cache, not all")
                .SetHelp(
@"The <info>clean</info> deletes all cached packages from bucket's cache directory.");
        }

        /// <inheritdoc />
        protected override void Initialize(IInput input, IOutput output)
        {
            base.Initialize(input, output);
            config = new Factory().CreateConfig();
            io = GetIO();
        }

        /// <inheritdoc />
        protected override int Execute(IInput input, IOutput output)
        {
            if (input.GetOption("gc"))
            {
                CleanWithGC();
            }
            else
            {
                CleanWithAllCache();
            }

            var exitCode = ExitCodes.Normal;
            var bucket = GetBucket(false, input.GetOption("no-plugins"));
            if (bucket)
            {
                var commandEvent = new CommandEventArgs(PluginEvents.Command, "clean", input, output);
                bucket.GetEventDispatcher().Dispatch(this, commandEvent);
                exitCode = Math.Max(exitCode, commandEvent.ExitCode);
            }

            if (!input.GetOption("gc"))
            {
                io.WriteError("<info>All caches cleared.</info>");
            }

            return exitCode;
        }

        /// <summary>
        /// Get an array representing the list of cache config key.
        /// </summary>
        protected virtual string[] GetCacheConfigKey()
        {
            return new[]
            {
                Settings.CacheVcsDir,
                Settings.CacheRepoDir,
                Settings.CacheFilesDir,
                Settings.CacheDir,
            };
        }

        private void CleanWithAllCache()
        {
            foreach (var key in GetCacheConfigKey())
            {
                var cache = CreateCache(key);
                if (cache == null)
                {
                    continue;
                }

                io.WriteError($"<info>Clearing cache ({key}): {cache.CacheDirectory}</info>");
                cache.Clear();
            }
        }

        private void CleanWithGC()
        {
            var cache = CreateCache(Settings.CacheDir);
            cache?.GC(config.Get(Settings.CacheFilesTTL), config.Get(Settings.CacheFilesMaxSize));
        }

        private CacheFileSystem CreateCache(string cacheConfigKey)
        {
            string cachePath = config.Get(cacheConfigKey);
            if (string.IsNullOrEmpty(cachePath))
            {
                io.WriteError($"<info>Cache directory does not exist ({cacheConfigKey}): {cachePath}</info>");
                return null;
            }

            cachePath = Path.Combine(Environment.CurrentDirectory, cachePath);
            var cache = new CacheFileSystem(cachePath, io);

            if (!cache.Enable)
            {
                io.WriteError($"<info>Cache is not enabled ({cacheConfigKey}): {cachePath}</info>");
                return null;
            }

            return cache;
        }
    }
}
