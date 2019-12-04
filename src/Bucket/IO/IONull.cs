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

namespace Bucket.IO
{
    /// <summary>
    /// <see cref="IIO"/> that is not interactive and never writes the output.
    /// </summary>
    public class IONull : BaseIO
    {
        /// <summary>
        /// Gets an empty instance.
        /// </summary>
        public static IIO That { get; private set; } = new IONull();

        /// <inheritdoc />
        public override void Write(string message, bool newLine = true, Verbosities verbosity = Verbosities.Normal)
        {
            // ignore.
        }

        /// <inheritdoc />
        public override void WriteError(string message, bool newLine = true, Verbosities verbosity = Verbosities.Normal)
        {
            // ignore.
        }

        /// <inheritdoc />
        public override void Overwrite(string message, bool newLine = true, int size = -1, Verbosities verbosity = Verbosities.Normal)
        {
            // ignore.
        }

        /// <inheritdoc />
        public override void OverwriteError(string message, bool newLine = true, int size = -1, Verbosities verbosity = Verbosities.Normal)
        {
            // ignore.
        }
    }
}
