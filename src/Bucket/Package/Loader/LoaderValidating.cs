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
using Bucket.Semver;
using Bucket.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text.RegularExpressions;
using BVersionParser = Bucket.Package.Version.VersionParser;

namespace Bucket.Package.Loader
{
    /// <summary>
    /// Represents a loader for validating data.
    /// </summary>
    public class LoaderValidating : ILoaderPackage
    {
        private readonly ILoaderPackage loader;
        private readonly IVersionParser versionParser;
        private readonly List<string> warnings;
        private readonly List<string> errors;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoaderValidating"/> class.
        /// </summary>
        /// <param name="loader">The base loader instance.</param>
        /// <param name="versionParser">The version parser instance.</param>
        public LoaderValidating(ILoaderPackage loader, IVersionParser versionParser = null)
        {
            this.loader = loader;
            this.versionParser = versionParser ?? new BVersionParser();
            errors = new List<string>();
            warnings = new List<string>();
        }

        /// <inheritdoc />
        /// <exception cref="InvalidPackageException">Triggered when validation fails.</exception>
        public IPackage Load(ConfigBucketBase config, Type expectedClass)
        {
            warnings.Clear();
            errors.Clear();

            // valid package name.
            if (string.IsNullOrEmpty(config.Name))
            {
                errors.Add("The \"name\" property not allowed to be empty.");
            }
            else
            {
                var warning = GetPackageNamingDeprecationWarnings(config.Name);
                if (!string.IsNullOrEmpty(warning))
                {
                    warnings.Add(warning);
                }
            }

            // valid version.
            if (string.IsNullOrEmpty(config.Version))
            {
                errors.Add("The \"version\" property not allowed to be empty.");
            }
            else
            {
                try
                {
                    versionParser.Normalize(config.Version);
                }
#pragma warning disable CA1031
                catch (System.Exception ex)
#pragma warning restore CA1031
                {
                    errors.Add($"Property \"version\" is invalid value ({config.Version}): {ex.Message}.");
                    config.Version = null;
                    config.VersionNormalized = null;
                }
            }

            // valid type.
            if (!string.IsNullOrEmpty(config.PackageType) && !ValidateRegex(config.PackageType, "type", "[A-Za-z0-9-]+"))
            {
                config.PackageType = null;
            }

            // valid authors.
            config.Authors = Arr.Filter(config.Authors ?? Array.Empty<ConfigAuthor>(), (author) =>
            {
                if (!string.IsNullOrEmpty(author.Email) && !ValidateEmail(author.Email, $"Authors {author.Name}"))
                {
                    author.Email = null;
                }

                return !string.IsNullOrEmpty(author.Name);
            });

            // valid support.
            config.Support = Arr.Filter(config.Support, (support) =>
            {
                var legal = new[] { "email", "issues", "forum", "source", "docs", "wiki" };
                var channel = support.Key;

                if (!Array.Exists(legal, (item) => item == channel))
                {
                    warnings.Add($"Property \"{channel}\" is invalid, please use: {string.Join(", ", legal)}");
                    return false;
                }

                if (channel == "email" && !ValidateEmail(support.Value, "Support"))
                {
                    return false;
                }

                return true;
            }).ToDictionary(item => item.Key, item => item.Value);

            // valid link.
            IDictionary<string, string> ValidateLinks(IDictionary<string, string> collection, string linkType)
            {
                return Arr.Filter(collection, (require) =>
                {
                    return ValidateLink(require.Key, require.Value, linkType);
                }).ToDictionary(item => item.Key, item => item.Value);
            }

            config.Requires = ValidateLinks(config.Requires, "require");
            config.RequiresDev = ValidateLinks(config.RequiresDev, "require-dev");
            config.Replaces = ValidateLinks(config.Replaces, "replace");
            config.Provides = ValidateLinks(config.Provides, "provide");
            config.Conflicts = ValidateLinks(config.Conflicts, "conflict");

            if (errors.Count > 0)
            {
                throw new InvalidPackageException(errors.ToArray(), warnings.ToArray(), config);
            }

            return loader.Load(config, expectedClass);
        }

        /// <summary>
        /// Gets the package name deprecation warnings.
        /// Generally used to warn some change fields when upgrading a big version of Bucket.
        /// </summary>
        /// <param name="packageName">The package name.</param>
        /// <returns>Return null if no warnings.</returns>
        public virtual string GetPackageNamingDeprecationWarnings(string packageName)
        {
            return null;
        }

        /// <summary>
        /// Returns an array of the validating errors.
        /// </summary>
        public string[] GetErrors()
        {
            return errors.ToArray();
        }

        /// <summary>
        /// Returns an array of the validating warnings.
        /// </summary>
        public string[] GetWarnings()
        {
            return warnings.ToArray();
        }

        private bool ValidateRegex(string propertyValue, string property, string regex, bool error = false)
        {
            if (Regex.IsMatch(propertyValue, $"^{regex}$"))
            {
                return true;
            }

            var message = $"Property \"{property}\" : invalid value ({propertyValue}), must match {regex}";

            if (error)
            {
                errors.Add(message);
            }
            else
            {
                warnings.Add(message);
            }

            return false;
        }

        private bool ValidateEmail(string email, string author)
        {
            var result = false;
            try
            {
                return result = new MailAddress(email).Address == email;
            }
#pragma warning disable CA1031
            catch (System.Exception)
#pragma warning restore CA1031
            {
                return result = false;
            }
            finally
            {
                if (!result)
                {
                    warnings.Add($"{author} email : invalid value ({email}), must be a valid email address.");
                }
            }
        }

        private bool ValidateLink(string linkPackage, string linkVersion, string linkType)
        {
            var warning = GetPackageNamingDeprecationWarnings(linkPackage);
            if (!string.IsNullOrEmpty(warning))
            {
                warnings.Add(warning);
            }
            else if (!Regex.IsMatch(linkPackage, $"^{Factory.RegexPackageNameIllegal}$"))
            {
                warnings.Add($"{linkType}.{linkPackage} : invalid key, package names must be strings containing only [{Factory.RegexPackageNameIllegal}]");
            }

            if (linkVersion == BasePackage.SelfVersion)
            {
                return true;
            }

            try
            {
                versionParser.ParseConstraints(linkVersion);
            }
#pragma warning disable CA1031
            catch (System.Exception ex)
#pragma warning restore CA1031
            {
                errors.Add($"{linkType}.{linkPackage} : invalid version constraint ({ex.Message})");
                return false;
            }

            return true;
        }
    }
}
