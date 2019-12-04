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

namespace Bucket
{
    /// <summary>
    /// The environment variables with bucket.
    /// </summary>
    public static class EnvironmentVariables
    {
        /// <summary>
        /// Indicates the name of the bucket configuration file.
        /// </summary>
        public const string Bucket = "BUCKET";

        /// <summary>
        /// Indicates the global directory shared by the bucket project.
        /// </summary>
        public const string BucketHome = Bucket + "_HOME";

        /// <summary>
        /// Indicates the bucket cache directory.
        /// </summary>
        /// <remarks>Which is also configurable via the cache-dir option.</remarks>
        public const string BucketCacheDir = Bucket + "_CACHE_DIR";

        /// <summary>
        /// A Json represents the environment variable for authentication.
        /// </summary>
        public const string BucketAuth = Bucket + "_AUTH";

        /// <summary>
        /// A string(stash,true,false,1,0) indicates whether to discard the changes.
        /// </summary>
        public const string BucketDiscardChanges = Bucket + "_DISCARD_CHANGES";

        /// <summary>
        /// A string (full, auto) determines the compatibility of the binaries to be installed.
        /// </summary>
        public const string BucketBinCompat = Bucket + "_BIN_COMPAT";

        /// <summary>
        /// A string path determines which shell will be used.
        /// </summary>
        public const string BucketScriptShell = Bucket + "_SCRIPT_SHELL";

        /// <summary>
        /// A string (0, 1) determines whether is operations dev packages.
        /// </summary>
        public const string BucketDevMode = Bucket + "_DEV_MODE";

        /// <summary>
        /// A int determines the process timeout.
        /// </summary>
        public const string BucketProcessTimeout = Bucket + "_PROCESS_TIMEOUT";
    }
}
