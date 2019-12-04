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

namespace Bucket.Configuration
{
    /// <summary>
    /// Represents a configuration collection.
    /// </summary>
    public static class Settings
    {
        /// <summary>
        /// Current user's home directory.
        /// </summary>
        /// <remarks>Environmental variable:<see cref="EnvironmentVariables.BucketHome"/>.</remarks>
        public const string Home = "home";

        /// <summary>
        /// Represents a root cache directory.
        /// </summary>
        /// <remarks>Environmental variable:<see cref="EnvironmentVariables.BucketCacheDir"/>.</remarks>
        public const string CacheDir = "cache-dir";

        /// <summary>
        /// Stores repository metadata for the bucket type and the VCS repos of type github.
        /// </summary>
        public const string CacheRepoDir = "cache-repo-dir";

        /// <summary>
        /// Stores VCS clones for loading VCS repository metadata for the git types and to speed up installs.
        /// </summary>
        public const string CacheVcsDir = "cache-vcs-dir";

        /// <summary>
        /// Requires will be installed into the vendor directory.
        /// </summary>
        /// <remarks>You can install requires into a different directory if you want to.</remarks>
        public const string VendorDir = "vendor-dir";

        /// <summary>
        /// If a project includes binaries, they will be symlinked into this directory.
        /// </summary>
        /// <remarks>Defaults to vender/bin.</remarks>
        public const string BinDir = "bin-dir";

        /// <summary>
        /// It is only used for storing past bucket application to be able to rollback to older versions.
        /// </summary>
        public const string BackupDir = "backup-dir";

        /// <summary>
        /// Determines the compatibility of the binaries to be installed.
        /// </summary>
        /// <remarks>
        /// Defaults to auto, If it is auto then bucket only installs .bat proxy files
        /// when on Windows. If set to full then both .bat files for Windows and scripts
        /// for Unix-based operating systems will be installed for each binary.
        /// </remarks>
        public const string BinCompat = "bin-compat";

        /// <summary>
        /// A bool value indicates whether it must be run with an encryption protocol.
        /// </summary>
        public const string SecureHttp = "secure-http";

        /// <summary>
        /// An array listing all the protocols supported by public Github website.
        /// </summary>
        public const string GithubProtocols = "github-protocols";

        /// <summary>
        /// An array representing the available github domains.
        /// </summary>
        public const string GithubDomains = "github-domains";

        /// <summary>
        /// An array representing the available github domains.
        /// </summary>
        public const string GitlabDomains = "gitlab-domains";

        /// <summary>
        /// A string (prompt, true) indicating whether to save the authorization.
        /// </summary>
        public const string StoreAuth = "store-auths";

        /// <summary>
        /// Indicates an github oauth authentication information.
        /// </summary>
        /// <remarks>Complex configuration structure is <see cref="ConfigAuth"/>.</remarks>
        public const string GithubOAuth = "github-oauth";

        /// <summary>
        /// Indicates an gitlab oauth authentication information.
        /// </summary>
        /// <remarks>Complex configuration structure is <see cref="ConfigAuth"/>.</remarks>
        public const string GitlabOAuth = "gitlab-oauth";

        /// <summary>
        /// Indicates an gitlab token authentication information.
        /// </summary>
        /// <remarks>Complex configuration structure is <see cref="ConfigAuth"/>.</remarks>
        public const string GitlabToken = "gitlab-token";

        /// <summary>
        /// Indicates an http basic authentication information.
        /// </summary>
        /// <remarks>Complex configuration structure is <see cref="ConfigAuth{HttpBasic}"/>.</remarks>
        public const string HttpBasic = "http-basic";

        /// <summary>
        /// A string (stash,true,false,1,0) indicating is discard unpushed changes.
        /// </summary>
        /// <remarks>Environmental variable:<see cref="EnvironmentVariables.BucketDiscardChanges"/>.</remarks>
        public const string DiscardChanges = "discard-changes";

        /// <summary>
        /// A string or object indicating the install preferred.
        /// </summary>
        public const string PreferredInstall = "preferred-install";

        /// <summary>
        /// Whether is auto sort the packages.
        /// </summary>
        public const string SortPackages = "sort-packages";

        /// <summary>
        /// The default cache time-to-live.
        /// </summary>
        public const string CacheTTL = "cache-ttl";

        /// <summary>
        /// The lifetime of the cache file, in seconds.
        /// </summary>
        public const string CacheFilesTTL = "cache-files-ttl";

        /// <summary>
        /// The maximum size allowed for the cache file.
        /// </summary>
        /// <remarks>Allows: g,m,k,gib,mib,kib To indicate size.</remarks>
        public const string CacheFilesMaxSize = "cache-files-maxsize";

        /// <summary>
        /// Stores the zip archives of packages.
        /// </summary>
        public const string CacheFilesDir = "cache-files-dir";

        /// <summary>
        /// Get an array of all setting keys.
        /// </summary>
        public static string[] GetKeys()
        {
            return new[]
            {
                Home,
                CacheDir,
                CacheVcsDir,
                VendorDir,
                BinDir,
                BackupDir,
                BinCompat,
                SecureHttp,
                GithubProtocols,
                GithubDomains,
                GitlabDomains,
                StoreAuth,
                GithubOAuth,
                GitlabOAuth,
                GitlabToken,
                HttpBasic,
                DiscardChanges,
                PreferredInstall,
                SortPackages,
                CacheTTL,
                CacheFilesTTL,
                CacheFilesMaxSize,
                CacheFilesDir,
            };
        }

        /// <summary>
        /// Get an array of all secret keys.
        /// </summary>
        public static string[] GetSecretKeys()
        {
            return new[]
            {
                GithubOAuth,
                GitlabOAuth,
                GitlabToken,
                HttpBasic,
            };
        }
    }
}
