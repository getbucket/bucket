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

using Bucket.EventDispatcher;
using Bucket.Exception;
using Bucket.Script;
using Bucket.Util;
using GameBox.Console;
using GameBox.Console.Input;
using GameBox.Console.Output;
using System.Collections.Generic;

namespace Bucket.Command
{
    /// <summary>
    /// This command run the scripts.
    /// </summary>
    public class CommandRunScript : BaseCommand
    {
        /// <inheritdoc />
        protected override void Configure()
        {
            SetName("run-script")
                .SetAlias("run")
                .SetDescription("Runs the scripts defined in bucket.json.")
                .SetDefinition(new IInputDefinition[]
                {
                    new InputArgument("script", InputArgumentModes.Optional, "Script name to run."),
                    new InputArgument("args", InputArgumentModes.IsArray | InputArgumentModes.Optional),
                    new InputOption("timeout", null, InputOptionModes.ValueOptional, "Sets script timeout in millisecond, or -1 for never."),
                    new InputOption("dev", null, InputOptionModes.ValueNone, "Sets the dev mode."),
                    new InputOption("no-dev", null, InputOptionModes.ValueNone, "Disables the dev mode."),
                    new InputOption("list", "l", InputOptionModes.ValueNone, "List scripts."),
                })
                .SetHelp(
@"The <info>run-script</info> command runs scripts defined in bucket.json:
<info>bucket {command.name} post-install-cmd</info>");
        }

        /// <inheritdoc />
        protected override int Execute(IInput input, IOutput output)
        {
            if (input.GetOption("list"))
            {
                return ListScripts(output);
            }

            string script = input.GetArgument("script");
            if (string.IsNullOrEmpty(script))
            {
                throw new RuntimeException("Missing required argument \"script\"");
            }

            var defiendEvents = new HashSet<string>(ScriptEvents.GetEvents());
            var disabledScript = script.ToUpper().Replace("-", "_");
            if (!defiendEvents.Contains(script) && defiendEvents.Contains(disabledScript))
            {
                throw new RuntimeException($"Script \"{script}\" cannot be run with this command");
            }

            var bucket = GetBucket();
            var devMode = input.GetOption("dev") || !input.GetOption("no-dev");
            var eventDispatcher = bucket.GetEventDispatcher();
            if (!eventDispatcher.HasListener(script))
            {
                throw new RuntimeException($"Script \"{script}\" is not defined in this package");
            }

            string[] args = input.GetArgument("args");
            string timeout = input.GetOption("timeout");
            if (timeout != null)
            {
                BucketProcessExecutor.SetDefaultTimeout(int.Parse(timeout));
            }

            var eventArgs = new ScriptEventArgs(script, bucket, GetIO(), devMode, args);
            eventDispatcher.Dispatch(this, eventArgs);
            return eventArgs.ExitCode;
        }

        /// <summary>
        /// Show the scripts list.
        /// </summary>
        protected virtual int ListScripts(IOutput output)
        {
            // todo: implement list scripts code.
            GetIO().WriteError("The --list optional is not supported yet. Please wait for the next version.");
            return ExitCodes.Normal;
        }
    }
}
