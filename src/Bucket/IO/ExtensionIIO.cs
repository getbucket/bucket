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

using System.Collections.Generic;

namespace Bucket.IO
{
    /// <summary>
    /// Extension method for <see cref="IIO"/>.
    /// </summary>
#pragma warning disable S101
    public static class ExtensionIIO
#pragma warning restore S101
    {
        /// <summary>
        /// Writes a message to the output.
        /// </summary>
        /// <param name="io">The input/output instance.</param>
        /// <param name="messages">An array of message.</param>
        /// <param name="newLine">Whether to add a newline or not.</param>
        /// <param name="verbosity">Verbosity level from the verbosity * constants.</param>
        public static void Write(this IIO io, IEnumerable<string> messages, bool newLine = true, Verbosities verbosity = Verbosities.Normal)
        {
            foreach (var message in messages)
            {
                io.Write(message, newLine, verbosity);
            }
        }

        /// <summary>
        /// Writes a message to the error output.
        /// </summary>
        /// <param name="io">The input/output instance.</param>
        /// <param name="messages">An array of message.</param>
        /// <param name="newLine">Whether to add a newline or not.</param>
        /// <param name="verbosity">Verbosity level from the verbosity * constants.</param>
        public static void WriteError(this IIO io, IEnumerable<string> messages, bool newLine = true, Verbosities verbosity = Verbosities.Normal)
        {
            foreach (var message in messages)
            {
                io.WriteError(message, newLine, verbosity);
            }
        }
    }
}
