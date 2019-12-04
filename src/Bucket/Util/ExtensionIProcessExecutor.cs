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

using GameBox.Console.Process;
using System;

namespace Bucket.Util
{
    /// <summary>
    /// Provide some extension functions for <see cref="IProcessExecutor"/>.
    /// </summary>
    public static class ExtensionIProcessExecutor
    {
        /// <summary>
        /// Executes the given command with command line.
        /// </summary>
        /// <param name="process">The <see cref="IProcessExecutor"/> instance.</param>
        /// <param name="command">The command that will be executed.</param>
        /// <param name="cwd">Specify the execution path of the command.</param>
        /// <returns>The return status of the executed command.</returns>
        public static int Execute(this IProcessExecutor process, string command, string cwd = null)
        {
            return process.Execute(command, out _, out _, cwd);
        }

        /// <summary>
        /// Executes the given command with command line.
        /// </summary>
        /// <param name="process">The <see cref="IProcessExecutor"/> instance.</param>
        /// <param name="command">The command that will be executed.</param>
        /// <param name="stdout">The output string from the command.</param>
        /// <param name="cwd">Specify the execution path of the command.</param>
        /// <returns>The return status of the executed command.</returns>
        public static int Execute(this IProcessExecutor process, string command, out string stdout, string cwd = null)
        {
            return process.Execute(command, out stdout, out _, cwd);
        }

        /// <summary>
        /// Executes the given command with command line.
        /// </summary>
        /// <param name="process">The <see cref="IProcessExecutor"/> instance.</param>
        /// <param name="command">The command that will be executed.</param>
        /// <param name="stdout">The output string from the command.</param>
        /// <param name="cwd">Specify the execution path of the command.</param>
        /// <returns>The return status of the executed command.</returns>
        public static int Execute(this IProcessExecutor process, string command, out string[] stdout, string cwd = null)
        {
            return process.Execute(command, out stdout, out _, cwd);
        }

        /// <summary>
        /// Executes the given command with command line.
        /// </summary>
        /// <param name="process">The <see cref="IProcessExecutor"/> instance.</param>
        /// <param name="command">The command that will be executed.</param>
        /// <param name="stdout">The output string from the command.</param>
        /// <param name="stderr">The error string from the command.</param>
        /// <param name="cwd">Specify the execution path of the command.</param>
        /// <returns>The return status of the executed command.</returns>
        public static int Execute(this IProcessExecutor process, string command, out string stdout, out string stderr, string cwd = null)
        {
            var ret = process.Execute(command, out string[] output, out string[] error, cwd);

            stdout = string.Join(Environment.NewLine, output ?? Array.Empty<string>());
            stderr = string.Join(Environment.NewLine, error ?? Array.Empty<string>());

            return ret;
        }
    }
}
