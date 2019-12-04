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

using Bucket.Console;
using System;

namespace Bucket.CLI
{
    /// <summary>
    /// Bucket package manager cli program entry.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Starup the program.
        /// </summary>
        public static void Main()
        {
            var application = new Application
            {
                Encoding = System.Console.OutputEncoding,
            };

            Environment.Exit(application.Run());
        }
    }
}
