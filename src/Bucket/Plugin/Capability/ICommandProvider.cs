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
using System.Collections.Generic;

namespace Bucket.Plugin.Capability
{
    /// <summary>
    /// This capability will receive an array with 'bucket' and 'io' keys
    /// as constructor argument. Those contain Bucket instances and IIO
    /// interface.
    /// </summary>
    public interface ICommandProvider : ICapability
    {
        /// <summary>
        /// Retrieves an array of commands.
        /// </summary>
        IEnumerable<BaseCommand> GetCommands();
    }
}
