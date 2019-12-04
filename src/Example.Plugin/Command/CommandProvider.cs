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
using Bucket.Plugin.Capability;
using System.Collections.Generic;

namespace Example.Plugin.Command
{
    /// <summary>
    /// Provides commands for demonstration.
    /// </summary>
    public class CommandProvider : ICommandProvider
    {
        /// <inheritdoc />
        public IEnumerable<BaseCommand> GetCommands()
        {
            return new[] { new CommandFoo() };
        }
    }
}
