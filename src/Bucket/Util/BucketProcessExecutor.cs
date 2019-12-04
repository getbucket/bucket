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
using GameBox.Console.Process;
using GameBox.Console.Util;
using System.IO;
using System.Text.RegularExpressions;
using STimeout = System.Threading.Timeout;

namespace Bucket.Util
{
    /// <summary>
    /// Represents a bucket default process executor.
    /// </summary>
    public class BucketProcessExecutor : ProcessExecutor
    {
        private static int defaultTimeout;
        private readonly IIO io;
        private string shellPath;
        private int timeout;

        /// <summary>
        /// Initializes static members of the <see cref="BucketProcessExecutor"/> class.
        /// </summary>
#pragma warning disable S3963
        static BucketProcessExecutor()
#pragma warning restore S3963
        {
            string envTimeout = Terminal.GetEnvironmentVariable(EnvironmentVariables.BucketProcessTimeout);
            if (!string.IsNullOrEmpty(envTimeout))
            {
                defaultTimeout = int.Parse(envTimeout);
            }
            else
            {
                defaultTimeout = STimeout.Infinite;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BucketProcessExecutor"/> class.
        /// </summary>
        /// <param name="io">The input/output instance.</param>
        public BucketProcessExecutor(IIO io = null)
        {
            this.io = io ?? IONull.That;
            timeout = STimeout.Infinite;
        }

        /// <summary>
        /// Gets or sets default cwd path.
        /// </summary>
        public string Cwd { get; set; } = null;

        /// <inheritdoc />
        public override int Timeout
        {
            get
            {
                if (timeout == STimeout.Infinite)
                {
                    return defaultTimeout;
                }

                return timeout;
            }

            set
            {
                timeout = value;
            }
        }

        /// <summary>
        /// Set the default timeout with milliseconds.
        /// </summary>
        public static void SetDefaultTimeout(int millisecond)
        {
            defaultTimeout = millisecond;
        }

        /// <summary>
        /// Gets the default timeout with milliseconds.
        /// </summary>
        public static int GetDefaultTimeout()
        {
            return defaultTimeout;
        }

        /// <summary>
        /// Filter commands with sensitive information.
        /// </summary>
        public static string FilterSensitive(string command)
        {
            // We hide sensitive information in the output message.
            const string sensitiveRegex = @"://(?<user>[^:/\s]+):(?<password>[^@\s/]+)@";

            string ReplaceSensitive(Match match)
            {
                if (Regex.IsMatch(match.Groups["user"].Value, "^[a-f0-9]{12,}$"))
                {
                    return "://***:***@";
                }
                else
                {
                    return $"://{match.Groups["user"].Value}:***@";
                }
            }

            return Regex.Replace(command, sensitiveRegex, ReplaceSensitive, RegexOptions.IgnoreCase);
        }

        /// <inheritdoc />
        public override int Execute(string command, out string[] stdout, out string[] stderr, string cwd = null)
        {
            if (io.IsDebug)
            {
                io.WriteError($"Executing command ({cwd ?? "CWD"}): {FilterSensitive(command)}");
            }

            return base.Execute(command, out stdout, out stderr, cwd ?? Cwd);
        }

        /// <inheritdoc />
        protected override string GetShellPath()
        {
            if (!string.IsNullOrEmpty(shellPath))
            {
                return shellPath;
            }

            shellPath = Terminal.GetEnvironmentVariable(EnvironmentVariables.BucketScriptShell);
            if (string.IsNullOrEmpty(shellPath) || !File.Exists(shellPath))
            {
                return shellPath = base.GetShellPath();
            }

            return shellPath;
        }
    }
}
