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

using Bucket.Command;
using Bucket.EventDispatcher;
using Bucket.Exception;
using Bucket.IO;
using Bucket.Plugin.Capability;
using Bucket.Util;
using GameBox.Console.Exception;
using GameBox.Console.Helper;
using GameBox.Console.Input;
using GameBox.Console.Output;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using BaseApplication = GameBox.Console.Application;
using BaseCommand = GameBox.Console.Command.Command;
using GExitCodes = GameBox.Console.ExitCodes;

namespace Bucket.Console
{
    /// <summary>
    /// The console application that handles the commands.
    /// </summary>
    /// <remarks>
    /// Basic console technology is provided by GameBox.Console.
    /// </remarks>
    public class Application : BaseApplication
    {
        private const string Logo =
@"
    ____             __        __ 
   / __ )__  _______/ /_____  / /_
  / __  / / / / ___/ //_/ _ \/ __/
 / /_/ / /_/ / /__/ ,< /  __/ /_  
/_____/\__,_/\___/_/|_|\___/\__/  

";

        private IIO io;
        private bool disablePluginsByDefault;
        private bool hasPluginCommands;
        private Bucket bucket;

        /// <summary>
        /// Initializes a new instance of the <see cref="Application"/> class.
        /// </summary>
        public Application()
            : base("Bucket", Bucket.GetVersionPretty())
        {
            Regex.CacheSize = Math.Max(Regex.CacheSize, 32);
            io = IONull.That;
            disablePluginsByDefault = false;
            hasPluginCommands = false;
        }

        /// <summary>
        /// Gets the execution path of the current program.
        /// </summary>
        public static string ExecutablePath => AppDomain.CurrentDomain.BaseDirectory;

        /// <inheritdoc />
        public override string GetHelp()
        {
            return Logo + GetLongVersion(false);
        }

        /// <inheritdoc />
        public override string GetLongVersion()
        {
            return GetLongVersion(true);
        }

        /// <summary>
        /// Gets helper instance for input and output.
        /// </summary>
        /// <returns>The <see cref="IIO"/> instance.</returns>
        public IIO GetIO()
        {
            return io;
        }

        /// <summary>
        /// Gets the <see cref="Bucket"/> object.
        /// </summary>
        /// <param name="required">Whether the bucket object is required.</param>
        /// <param name="disablePlugins">Whether is disabled plugins.</param>
        public virtual Bucket GetBucket(bool required = true, bool? disablePlugins = null)
        {
            if (disablePlugins == null)
            {
                disablePlugins = disablePluginsByDefault;
            }

            if (bucket)
            {
                return bucket;
            }

            try
            {
                bucket = Factory.Create(io, null, disablePlugins.Value);
            }
            catch (InvalidArgumentException ex)
            {
                if (required)
                {
                    io.WriteError(ex.Message);
                    Environment.Exit(GExitCodes.GeneralException);
                }
            }

            return bucket;
        }

        /// <summary>
        /// Removes the cached bucket instance.
        /// </summary>
        public virtual void ResetBucket()
        {
            bucket = null;
            if (GetIO() != null && GetIO() is IResetAuthentications resetable)
            {
                resetable.ResetAuthentications();
            }
        }

        /// <inheritdoc />
        public override int Run(IInput input = null, IOutput output = null)
        {
            return base.Run(input, output ?? Factory.CreateOutput());
        }

        /// <inheritdoc />
        protected override int DoRun(IInput input, IOutput output)
        {
            disablePluginsByDefault = input.HasRawOption("--no-plugins");
            var ioConsole = new IOConsole(input, output);
            io = ioConsole;

            var commandName = GetCommandName(input);
            TryGetCommand(commandName, out BaseCommand command);

            LoadPluginCommands(command);

            var isProxyCommand = false;
            if (!string.IsNullOrEmpty(commandName) || command != null)
            {
                // Maybe the command is provided by the plugin. If we can't find
                // us again, we will stop intercepting the exception.
                if (command == null)
                {
                    command = Find(commandName);
                }

                isProxyCommand = command is Command.BaseCommand baseCommand && baseCommand.IsProxyCommand;
            }

            if (!isProxyCommand)
            {
                io.WriteError(
                    $"Running {Bucket.GetVersionPretty()} ({Bucket.GetVersion()},{Bucket.GetReleaseDataPretty()}) on {Platform.GetOSInfo()}",
                    verbosity: Verbosities.Debug);

                if (!(command is CommandSelfUpdate) && Bucket.IsDev && (DateTime.Now - Bucket.GetReleaseData()) > new TimeSpan(60, 0, 0, 0, 0))
                {
                    io.WriteError(
                        "<warning>Warning: This development build of bucket is over 60 days old. It is recommended to update it by running \"self-update\" to get the latest version.</warning>");
                }
            }

            Stopwatch stopWatch = null;
            try
            {
                if (input.HasRawOption("--profile"))
                {
                    stopWatch = new Stopwatch();
                    ioConsole.SetDebugging(stopWatch);
                }

                stopWatch?.Start();
                var exitCode = base.DoRun(input, output);
                stopWatch?.Stop();

                if (stopWatch != null)
                {
                    var memoryUsage = AbstractHelper.FormatMemory(Environment.WorkingSet);
                    var timeSpent = stopWatch.Elapsed;
                    io.WriteError(string.Empty);
                    io.WriteError($"<info>Memory usage: {memoryUsage}, total time: {timeSpent.TotalSeconds.ToString("0.00")}s</info>");
                    io.WriteError(string.Empty);
                }

                return exitCode;
            }
            catch (ScriptExecutionException ex)
            {
                return ex.ExitCode;
            }
        }

        /// <summary>
        /// Set the helper instance of the input and output.
        /// </summary>
        /// <param name="io">The <see cref="IIO"/> instance.</param>
        protected void SetIO(IIO io)
        {
            this.io = io;
        }

        /// <inheritdoc />
        protected override BaseCommand[] GetDefaultCommands()
        {
            return base.GetDefaultCommands().Concat(new BaseCommand[]
            {
                new CommandPlugin(),
                new CommandAbout(),
                new CommandValidate(),
                new CommandUpdate(),
                new CommandInstall(),
                new CommandRunScript(),
                new CommandRequire(),
                new CommandRemove(),
                new CommandGlobal(),
                new CommandClean(),
                new CommandSearch(),
                new CommandExec(),
                new CommandSelfUpdate(),
            }).ToArray();
        }

        /// <inheritdoc />
        protected override InputDefinition CreateDefaultInputDefinition()
        {
            var definition = base.CreateDefaultInputDefinition();
            definition.AddOptions(new InputOption("--no-plugins", null, InputOptionModes.ValueNone, "Whether to disable plugins."));
            definition.AddOptions(new InputOption("--profile", null, InputOptionModes.ValueNone, "Display timing and memory usage information"));
            return definition;
        }

        /// <summary>
        /// Loaded the commands from plugin.
        /// </summary>
        /// <param name="realCommand">Resolve the real command name of the alias relationship. null maybe plugin command.</param>
        protected virtual void LoadPluginCommands(BaseCommand realCommand)
        {
            if (disablePluginsByDefault || hasPluginCommands || realCommand is CommandGlobal)
            {
                return;
            }

            foreach (var command in GetPluginCommands())
            {
                if (Has(command.Name))
                {
                    io.WriteError($"<warning>Plugin command {command.Name} ({command}) would override a Bucket command and has been skipped.</warning>");
                    continue;
                }

                Add(command);
            }

            // Prevent proxy commands from causing repeated loads.
            hasPluginCommands = true;
        }

        /// <summary>
        /// Try to get the command instance.
        /// </summary>
        /// <returns>True if the command found.</returns>
        protected virtual bool TryGetCommand(string commandName, out BaseCommand command)
        {
            command = null;
            if (string.IsNullOrEmpty(commandName))
            {
                return false;
            }

            try
            {
                command = Find(commandName);
                return true;
            }
            catch (CommandNotFoundException)
            {
                return false;
            }
            catch (InvalidArgumentException)
            {
                return false;
            }
        }

        /// <summary>
        /// Returns the long version of the application.
        /// </summary>
        /// <param name="verbosity">Whether is dispaly the verboisty infomation.</param>
        protected virtual string GetLongVersion(bool verbosity)
        {
            if (verbosity)
            {
                return $"{base.GetLongVersion()} ({Bucket.GetVersion()}) {Bucket.GetReleaseDataPretty()}";
            }

            return $"{base.GetLongVersion()} {Bucket.GetReleaseDataPretty()}";
        }

        private BaseCommand[] GetPluginCommands()
        {
            var bucketInstance = GetBucket(false, false);
            if (bucketInstance == null)
            {
                bucketInstance = Factory.CreateGlobal(io, false);
            }

            if (bucketInstance == null)
            {
                return Array.Empty<BaseCommand>();
            }

            var commands = new List<BaseCommand>();
            var pluginManager = bucketInstance.GetPluginManager();

            foreach (var capability in pluginManager.GetAllCapabilities<ICommandProvider>(bucketInstance, io))
            {
                var pluginCommands = capability.GetCommands();
                if (pluginCommands.Contains(null))
                {
                    throw new UnexpectedException(
                        $"Plugin capability \"{capability}\" returned an invalid value null. we expected an BaseCommand instance.");
                }

                commands.AddRange(pluginCommands);
            }

            return commands.ToArray();
        }
    }
}
