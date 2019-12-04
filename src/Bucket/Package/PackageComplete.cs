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
using Bucket.Exception;
using Bucket.Util;
using System;
using System.Collections.Generic;

namespace Bucket.Package
{
    /// <summary>
    /// Package containing additional metadata that is not used by the solver.
    /// </summary>
    public class PackageComplete : Package, IPackageComplete
    {
        private dynamic deprecated;
        private string description;
        private string homepage;
        private string[] licenses;
        private string[] keywords;
        private ConfigAuthor[] authors;
        private IDictionary<string, string> support;
        private IDictionary<string, string> scripts;
        private ConfigRepository[] repositories;

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageComplete"/> class.
        /// </summary>
        /// <param name="name">The package name.</param>
        /// <param name="version">Normalized version.</param>
        /// <param name="versionPretty">The package non-normalized version(Human readable).</param>
        public PackageComplete(string name, string version, string versionPretty)
            : base(name, version, versionPretty)
        {
        }

        /// <inheritdoc />
        public bool IsDeprecated
        {
            get
            {
                if (deprecated is bool)
                {
                    return deprecated;
                }

                if (deprecated is null)
                {
                    return false;
                }

                Guard.Requires<UnexpectedException>(deprecated is string, $"Dynamic type {nameof(deprecated)} must be string type");
                return !string.IsNullOrEmpty(deprecated);
            }
        }

        /// <inheritdoc />
        public ConfigAuthor[] GetAuthors()
        {
            return authors ?? Array.Empty<ConfigAuthor>();
        }

        /// <summary>
        /// Sets an array of the package authors.
        /// </summary>
        /// <param name="authors">An array of the package authors.</param>
        public void SetAuthors(ConfigAuthor[] authors)
        {
            this.authors = authors;
        }

        /// <inheritdoc />
        public string GetDescription()
        {
            return description;
        }

        /// <summary>
        /// Sets the package description.
        /// </summary>
        /// <param name="description">The package description.</param>
        public void SetDescription(string description)
        {
            this.description = description;
        }

        /// <inheritdoc />
        public string GetHomepage()
        {
            return homepage;
        }

        /// <summary>
        /// Sets the package homepage.
        /// </summary>
        /// <param name="homepage">The package homepage.</param>
        public void SetHomepage(string homepage)
        {
            this.homepage = homepage;
        }

        /// <inheritdoc />
        public string[] GetKeywords()
        {
            return keywords ?? Array.Empty<string>();
        }

        /// <summary>
        /// Sets an array of keywords relating to the package.
        /// </summary>
        /// <param name="keywords">An array of keywords.</param>
        public void SetKeyworkds(string[] keywords)
        {
            this.keywords = keywords;
        }

        /// <inheritdoc />
        public string[] GetLicenses()
        {
            return licenses ?? Array.Empty<string>();
        }

        /// <summary>
        /// Sets the package license.
        /// </summary>
        /// <param name="licenses">An array of licenses.</param>
        public void SetLicenses(string[] licenses)
        {
            this.licenses = licenses;
        }

        /// <inheritdoc />
        public IDictionary<string, string> GetSupport()
        {
            return support;
        }

        /// <summary>
        /// Sets an map of the package support information.
        /// </summary>
        public void SetSupport(IDictionary<string, string> support)
        {
            this.support = support;
        }

        /// <inheritdoc />
        public ConfigRepository[] GetRepositories()
        {
            return repositories ?? Array.Empty<ConfigRepository>();
        }

        /// <summary>
        /// Sets an array of repository configurations.
        /// </summary>
        /// <param name="repositories">An array of repository configurations.</param>
        public void SetRepositories(ConfigRepository[] repositories)
        {
            this.repositories = repositories;
        }

        /// <summary>
        /// Set the replaced package and mark it as deprecated.
        /// </summary>
        /// <param name="deprecated">Replacement pakcage used to replace deprecated packages.</param>
        public void SetDeprecated(dynamic deprecated)
        {
            this.deprecated = deprecated;
        }

        /// <inheritdoc />
        public string GetReplacementPackage()
        {
            if (deprecated is bool || deprecated is null)
            {
                return null;
            }

            Guard.Requires<UnexpectedException>(deprecated is string, $"Dynamic type {nameof(deprecated)} must be string type");
            return deprecated;
        }

        /// <inheritdoc />
        public IDictionary<string, string> GetScripts()
        {
            return scripts;
        }

        /// <summary>
        /// Sets the scripts.
        /// </summary>
        public void SetScripts(IDictionary<string, string> scripts)
        {
            this.scripts = scripts;
        }
    }
}
