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

using Bucket.Configuration;
using System.Text.RegularExpressions;

namespace Bucket.IO.Loader
{
    /// <summary>
    /// Load configuration to <see cref="IIO"/> instance.
    /// </summary>
    internal sealed class LoaderIOConfiguration
    {
        private readonly IIO io;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoaderIOConfiguration"/> class.
        /// </summary>
        public LoaderIOConfiguration(IIO io)
        {
            this.io = io;
        }

        /// <summary>
        /// Load configuration.
        /// </summary>
        /// <param name="config">The config instance.</param>
        public void Load(Config config)
        {
            var githubOAuth = config.Get<ConfigAuth>(Settings.GithubOAuth) ?? ConfigAuth.Empty;
            var gitlabOAuth = config.Get<ConfigAuth>(Settings.GitlabOAuth) ?? ConfigAuth.Empty;
            var gitlabToken = config.Get<ConfigAuth>(Settings.GitlabToken) ?? ConfigAuth.Empty;
            var httpBasic = config.Get<ConfigAuth<HttpBasic>>(Settings.HttpBasic) ?? ConfigAuth<HttpBasic>.Empty;

            foreach (var auth in githubOAuth)
            {
                if (!Regex.IsMatch(auth.Value, "^[.a-z0-9]+$"))
                {
                    throw new ConfigException($"Your github oauth token for {auth.Key} contains invalid characters: \"{auth.Value}\"");
                }

                CheckAndSetAuthentication(auth.Key, auth.Value, "x-oauth-basic");
            }

            foreach (var auth in gitlabOAuth)
            {
                CheckAndSetAuthentication(auth.Key, auth.Value, "oauth2");
            }

            foreach (var auth in gitlabToken)
            {
                CheckAndSetAuthentication(auth.Key, auth.Value, "private-token");
            }

            foreach (var auth in httpBasic)
            {
                CheckAndSetAuthentication(auth.Key, auth.Value.Username, auth.Value.Password);
            }

            // todo: setup process timeout from configuration.
        }

        /// <summary>
        /// Check for overwrite and set the authentication information for the repository.
        /// </summary>
        /// <param name="repositoryName">The unique name of repository.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password or oauth protocol.</param>
        private void CheckAndSetAuthentication(string repositoryName, string username, string password = null)
        {
            if (!io.HasAuthentication(repositoryName))
            {
                io.SetAuthentication(repositoryName, username, password);
                return;
            }

            var (oldUsername, oldPassword) = io.GetAuthentication(repositoryName);

            if (oldUsername == username && oldPassword == password)
            {
                return;
            }

            io.WriteError($"<warning>Warning: You should avoid overwriting already defined auth settings for \"{repositoryName}\".</warning>");
        }
    }
}
