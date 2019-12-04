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

using Bucket.Configuration;
using Bucket.EventDispatcher;
using Bucket.Exception;
using Bucket.IO;
using Bucket.Plugin;
using GameBox.Console.Input;
using GameBox.Console.Output;
using BucketApplication = Bucket.Console.Application;
using FrameworkCommand = GameBox.Console.Command.Command;

namespace Bucket.Command
{
    /// <summary>
    /// Base class for Bucket commands.
    /// </summary>
    public abstract class BaseCommand : FrameworkCommand
    {
        private IIO io;
        private Bucket bucket;

        /// <summary>
        /// Gets a value indicating whether or not this command is meant to call another command.
        /// </summary>
        /// <remarks>This is mainly needed to avoid duplicated warnings messages.</remarks>
        public virtual bool IsProxyCommand => false;

        /// <summary>
        /// Gets helper instance for input and output.
        /// </summary>
        /// <returns>The <see cref="IIO"/> instance.</returns>
        public IIO GetIO()
        {
            if (io != null)
            {
                return io;
            }

            if (Application is BucketApplication bucketApplication)
            {
                return io = bucketApplication.GetIO();
            }

            return io = IONull.That;
        }

        /// <summary>
        /// Set the helper instance of the input and output.
        /// </summary>
        /// <param name="io">The <see cref="IIO"/> instance.</param>
        public void SetIO(IIO io)
        {
            this.io = io;
        }

        /// <summary>
        /// Gets the <see cref="Bucket"/> object.
        /// </summary>
        /// <param name="required">Whether the bucket object is required.</param>
        /// <param name="disablePlugins">Whether is disabled plugins.</param>
        public Bucket GetBucket(bool required = true, bool? disablePlugins = null)
        {
            if (bucket)
            {
                return bucket;
            }

            if (Application is BucketApplication bucketApplication)
            {
                bucket = bucketApplication.GetBucket(required, disablePlugins);
            }
            else if (required)
            {
                throw new RuntimeException("Could not create a Bucket instance, you must inject." +
                    $"one if this command is not used with a \"{typeof(BucketApplication).FullName}\" instance.");
            }

            return bucket;
        }

        /// <summary>
        /// Removes the cached bucket instance.
        /// </summary>
        public void ResetBucket()
        {
            bucket = null;
            if (Application is BucketApplication bucketApplication)
            {
                bucketApplication.ResetBucket();
            }
        }

        /// <inheritdoc />
        protected override void Initialize(IInput input, IOutput output)
        {
            if (GetIO() == IONull.That)
            {
                // In the absence of a given Application maybe
                // in debug mode so we give an IOConsole.
                io = new IOConsole(input, output);
            }

            var disablePlugins = input.HasRawOption("--no-plugins");
#pragma warning disable S1117
            var bucket = GetBucket(false, disablePlugins);
#pragma warning restore S1117
            if (!bucket)
            {
                bucket = Factory.CreateGlobal(GetIO(), disablePlugins);
            }

            if (bucket)
            {
                var commandEvent = new PreCommandRunEventArgs(PluginEvents.PreCommandRun, input, Name);
                bucket.GetEventDispatcher().Dispatch(this, commandEvent);
            }

            if (input.HasRawOption("--no-ansi") && input.HasOption("no-progress"))
            {
                input.SetOption("no-progress", true);
            }

            base.Initialize(input, output);
        }

        /// <summary>
        /// Returns preferSource and preferDist values based on the configuration..
        /// </summary>
        protected virtual (bool PreferSource, bool PreferDist) GetPreferredInstallOptions(Config config, IInput input, bool keepVcsRequiresPreferSource = false)
        {
            var preferSource = false;
            var preferDist = false;

            try
            {
                string preferredInstall = config.Get(Settings.PreferredInstall);
                switch (preferredInstall)
                {
                    case "dist":
                        preferDist = true;
                        break;
                    case "source":
                        preferSource = true;
                        break;
                    case "auto":
                    default:
                        // noop.
                        break;
                }
            }
            catch (ConfigException)
            {
                // noop.
            }

            var optionPreferSource = input.GetOption("prefer-source");
            var optionPreferDist = input.GetOption("prefer-dist");
            if (optionPreferSource || optionPreferDist || (keepVcsRequiresPreferSource && input.HasOption("keep-vcs") && input.GetOption("keep-vcs")))
            {
                preferSource = optionPreferSource || (keepVcsRequiresPreferSource && input.HasOption("keep-vcs") && input.GetOption("keep-vcs"));
                preferDist = optionPreferDist;
            }

            return (preferSource, preferDist);
        }
    }
}
