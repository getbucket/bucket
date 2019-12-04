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
using Bucket.Plugin;
using Bucket.Repository;
using Bucket.Util;
using GameBox.Console;
using GameBox.Console.Input;
using GameBox.Console.Output;
using System.Collections.Generic;

namespace Bucket.Command
{
    /// <summary>
    ///  This command search the exists package.
    /// </summary>
    public class CommandSearch : BaseCommand
    {
        /// <inheritdoc />
        protected override void Configure()
        {
            SetName("search")
                .SetDescription("Searches for packages.")
                .SetDefinition(new IInputDefinition[]
                {
                    new InputArgument("tokens", InputArgumentModes.IsArray | InputArgumentModes.Required, "tokens to search for"),
                    new InputOption("only-name", "N", InputOptionModes.ValueNone, "Search only in name"),
                    new InputOption("type", "t", InputOptionModes.ValueRequired, "Search for a specific package type"),
                })
                .SetHelp(
@"The search command searches for packages by its name
<info>bucket {command.name} foo bar</info>");
        }

        /// <inheritdoc />
        protected override int Execute(IInput input, IOutput output)
        {
            var platformRepository = new RepositoryPlatform();
            var io = GetIO();

            var bucket = GetBucket(false);
            if (!bucket)
            {
                var factory = new Factory();
                bucket = factory.CreateBucket(io, new ConfigBucket(), input.HasRawOption("--no-plugins"));
            }

            var localInstalledRepository = bucket.GetRepositoryManager().GetLocalInstalledRepository();
            var repositoryInstalled = new RepositoryComposite(localInstalledRepository, platformRepository);
            var repositories = new RepositoryComposite(Arr.Merge(new[] { repositoryInstalled }, bucket.GetRepositoryManager().GetRepositories()));

            var commandEvent = new CommandEventArgs(PluginEvents.Command, "search", input, output);
            bucket.GetEventDispatcher().Dispatch(this, commandEvent);

            var flags = input.GetOption("only-name") ? SearchMode.Name : SearchMode.Fulltext;
            var results = repositories.Search(string.Join(Str.Space, input.GetArgument("tokens")), flags, input.GetOption("type"));

            var seed = new HashSet<string>();
            foreach (var result in results)
            {
                if (!seed.Add(result.GetName()))
                {
                    continue;
                }

                io.Write(result.ToString());
            }

            return ExitCodes.Normal;
        }
    }
}
