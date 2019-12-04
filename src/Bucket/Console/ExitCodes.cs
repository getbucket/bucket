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

namespace Bucket.Console
{
    /// <summary>
    /// Bucket command line process exit code.
    /// Error codes can be reused and limited to 64-113.
    /// </summary>
    public static class ExitCodes
    {
        /// <summary>
        /// The require solving error code.
        /// </summary>
        public const int DependencySolvingException = 2;

        /// <summary>
        /// The bucket file validation warning only when --strict is given.
        /// </summary>
        /// <see cref="CommandValidate"/>
        public const int ValidationWarning = 64;

        /// <summary>
        /// The bucket file validation errors.
        /// </summary>
        /// <see cref="CommandValidate"/>
        public const int ValidationErrors = 65;

        /// <summary>
        /// The bucket file not found exception.
        /// </summary>
        /// <see cref="CommandValidate"/>
        public const int FileNotFoundException = 66;
    }
}
