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
using Bucket.Script;
using Bucket.Util;
using GameBox.Console;
using GameBox.Console.Input;
using GameBox.Console.Output;
using System;
using System.Collections.Generic;
using System.IO;
using BEventDispatcher = Bucket.EventDispatcher.EventDispatcher;

namespace Bucket.Command
{
    /// <summary>
    /// Exec the bin command.
    /// </summary>
    public class CommandExec : BaseCommand
    {
        /// <inheritdoc />
        protected override void Configure()
        {
            SetName("exec")
                .SetDescription("Executes a vendored binary/script.")
                .SetDefinition(new IInputDefinition[]
                {
                    new InputArgument("binary", InputArgumentModes.Optional, "The binary to run."),
                    new InputArgument(
                        "args",
                        InputArgumentModes.IsArray | InputArgumentModes.Optional,
                        "Arguments to pass to the binary. Use <info>--</info> to separate from bucket arguments"),
                    new InputOption("list", "l", InputOptionModes.ValueNone),
                })
                .SetHelp(
@"Executes a vendored binary/script.");
        }

        /// <inheritdoc />
        protected override int Execute(IInput input, IOutput output)
        {
            var io = GetIO();
            var bucket = GetBucket();
            string binary = input.GetArgument("binary");

            if (input.GetOption("list") || string.IsNullOrEmpty(binary))
            {
                OutputList(bucket, io);
                return ExitCodes.Normal;
            }

            var dispatcher = bucket.GetEventDispatcher();

            if (output.IsNormal)
            {
                output.SetVerbosity(OutputOptions.VerbosityQuiet);
            }

            var scriptEventArgs = new ScriptEventArgs("__exec_command", bucket, io, true, input.GetArgument("args"));
            dispatcher.AddListener("__exec_command", (sender, eventArgs) =>
            {
                if (dispatcher is BEventDispatcher bucketEventDispatcher)
                {
                    bucketEventDispatcher.ExecuteScript(binary, sender, (ScriptEventArgs)eventArgs);
                    return;
                }

                throw new NotSupportedException("The \"exec\" command can be used only when the event system is BucketEventSystem.");
            });

            dispatcher.Dispatch(this, scriptEventArgs);
            return scriptEventArgs.ExitCode;
        }

        private void OutputList(Bucket bucket, IIO io)
        {
            // todo: File path needs to be optimized.
            var binDir = bucket.GetConfig().Get(Settings.BinDir);
            var fullBinDir = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, binDir));

            var bins = Array.Empty<string>();
            if (Directory.Exists(fullBinDir))
            {
                bins = Directory.GetFiles(fullBinDir, "*");
            }

            bins = Arr.Merge(bins, Arr.Map(bucket.GetPackage().GetBinaries(), (bin) => $"{bin} (local)"));

            if (bins.Empty())
            {
                throw new RuntimeException($"No binaries found in bucket.json or in bin-dir ({binDir})");
            }

            io.Write(
@"
<comment>Available binaries:</comment>
");
            var seen = new HashSet<string>();
            foreach (var bin in bins)
            {
                var name = Path.GetFileNameWithoutExtension(bin);
                if (!seen.Add(name))
                {
                    continue;
                }

                io.Write($"<info>- {name}</info>");
            }
        }
    }
}
