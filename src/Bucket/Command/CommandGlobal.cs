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
using GameBox.Console.Input;
using GameBox.Console.Output;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Bucket.Command
{
    /// <summary>
    /// This command will mark subsequent commands as global execution.
    /// </summary>
    public class CommandGlobal : BaseCommand
    {
        /// <inheritdoc />
        public override bool IsProxyCommand => true;

        /// <inheritdoc />
        public override int Run(IInput input, IOutput output)
        {
            var factory = new Factory();
            var config = factory.CreateConfig();
            string home = config.Get(Settings.Home);

            if (!Directory.Exists(home))
            {
                Directory.CreateDirectory(home);
            }

            Environment.CurrentDirectory = home;

            GetIO().WriteError($"<info>Changed current directory to {home}</info>");

            var regexGlobal = new Regex(@"\bg(?:l(?:o(?:b(?:a(?:l)?)?)?)?)?\b");
            input = new InputString(regexGlobal.Replace(input.ToString(), string.Empty, 1));
            ResetBucket();

            return Application.Run(input, output);
        }

        /// <inheritdoc />
        protected override void Configure()
        {
            SetName("global")
                .SetDescription("Allows running commands in the global bucket dir (BUCKET_HOME).")
                .SetDefinition(new[]
                {
                    new InputArgument("command-name", InputArgumentModes.Required, string.Empty),
                    new InputArgument("args", InputArgumentModes.IsArray | InputArgumentModes.Optional, string.Empty),
                })
                .SetHelp(
@"Use this command as a wrapper to run other Bucket commands
within the global context of BUCKET_HOME.

You can use this to install CLI utilities globally, all you need
is to add the BUCKET_HOME/vendor/bin dir to your PATH env var.

BUCKET_HOME is c:\Users\<user>\AppData\Roaming\Bucket on Windows
and /home/<user>/.bucket on unix systems.

Note: This path may vary depending on customizations to bin-dir in
bucket.json or the environmental variable BUCKET_BIN_DIR.");
        }
    }
}
