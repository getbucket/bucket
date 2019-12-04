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

using Bucket.IO;
using GameBox.Console.Input;
using GameBox.Console.Tester;
using GameBox.Console.Util;

namespace Bucket.Tester
{
    /// <summary>
    /// Eases the testing of <see cref="IOConsole"/>.
    /// </summary>
    public class TesterIOConsole : AbstractTester
    {
        /// <summary>
        /// An array of strings representing each input passed to the command input stream.
        /// </summary>
        private string[] inputs;

        /// <summary>
        /// Sets an array of strings representing each input passed to the command input stream.
        /// </summary>
        /// <param name="inputs">The array of strings.</param>
        /// <returns>The current instance.</returns>
        public TesterIOConsole SetInputs(string[] inputs)
        {
            this.inputs = inputs;
            return this;
        }

        /// <summary>
        /// Track specified intput/output.
        /// </summary>
        /// <param name="io">The specified intput/output.</param>
        /// <param name="options">The option with <see cref="AbstractTester"/>.</param>
        /// <returns>Tracked intput/output.</returns>
        public IOConsole Track(IOConsole io, params Mixture[] options)
        {
            var input = new InputArgs();
            if (options.TryGet("interactive", out Mixture exists))
            {
                input.SetInteractive(exists);
            }

            if (inputs != null && inputs.Length > 0)
            {
                input.SetInputStream(CreateStream(inputs));
            }

            Initialize(options);
            io.SetInput(input);
            io.SetOutput(Output);

            return io;
        }

        /// <summary>
        /// Mock an new <see cref="IOConsole"/>.
        /// </summary>
        /// <param name="options">The option with <see cref="AbstractTester"/>.</param>
        /// <returns>Mocked intput/output.</returns>
        public IOConsole Mock(params Mixture[] options)
        {
            return Track(new IOConsole(null, null), options);
        }
    }
}
