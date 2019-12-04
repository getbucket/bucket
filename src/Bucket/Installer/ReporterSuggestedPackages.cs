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
using Bucket.Package;
using Bucket.Repository;
using Bucket.Util;
using GameBox.Console.Formatter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Bucket.Installer
{
    /// <summary>
    /// Represents a suggestion package reporter.
    /// </summary>
    public class ReporterSuggestedPackages
    {
        private readonly IIO io;
        private readonly LinkedList<Suggestion> packages;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReporterSuggestedPackages"/> class.
        /// </summary>
        /// <param name="io">The input/output instance.</param>
        public ReporterSuggestedPackages(IIO io)
        {
            this.io = io;
            packages = new LinkedList<Suggestion>();
        }

        /// <summary>
        /// Get Suggested packages with source, target and reason.
        /// </summary>
        public virtual Suggestion[] GetSuggestions()
        {
            return packages.ToArray();
        }

        /// <summary>
        /// Add suggested packages to be listed after install.
        /// </summary>
        /// <param name="source">Source package which made the suggestion.</param>
        /// <param name="target">Target package to be suggested.</param>
        /// <param name="reason">Reason the target package to be suggested.</param>
        public virtual ReporterSuggestedPackages AddSuggestion(string source, string target, string reason = null)
        {
            packages.AddLast(new Suggestion(source, target, reason ?? string.Empty));
            return this;
        }

        /// <summary>
        /// Add all suggestions from a package.
        /// </summary>
        public virtual ReporterSuggestedPackages AddSuggestions(IPackage package)
        {
            var source = package.GetNamePretty();
            var suggests = package.GetSuggests();

            if (suggests == null)
            {
                return this;
            }

            foreach (var suggest in suggests)
            {
                AddSuggestion(source, suggest.Key, suggest.Value);
            }

            return this;
        }

        /// <summary>
        /// Output suggested packages.
        /// </summary>
        /// <remarks>Do not list the ones already installed if installed repository provided.</remarks>
        public virtual ReporterSuggestedPackages Display(IRepository repositoryInstalled = null)
        {
            var suggestedPackages = GetSuggestions();
            var installedPackages = new HashSet<string>();
            if (repositoryInstalled != null && suggestedPackages.Length > 0)
            {
                foreach (var package in repositoryInstalled.GetPackages())
                {
                    Array.ForEach(package.GetNames(), (name) => installedPackages.Add(name));
                }
            }

            var count = suggestedPackages.Count((suggestion) => !installedPackages.Contains(suggestion.Target));

            if (count > 0)
            {
                io.WriteError($"Package operations have {count} suggestion:");
            }

            foreach (var suggestion in suggestedPackages)
            {
                if (installedPackages.Contains(suggestion.Target))
                {
                    continue;
                }

                var reason = string.IsNullOrEmpty(suggestion.Reason) ? string.Empty : $" ({suggestion.Reason})";
                io.WriteError($"{suggestion.Source} suggests installing {Escape(suggestion.Target)}{Escape(Process(suggestion, reason))}");
            }

            return this;
        }

        private static string Process(Suggestion suggestion, string reason)
        {
            reason = reason.Replace("%source%", suggestion.Source);
            reason = reason.Replace("%target%", suggestion.Target);
            return reason;
        }

        private static string Escape(string content)
        {
            return OutputFormatter.Escape(RemoveControlCharacters(content));
        }

        private static string RemoveControlCharacters(string content)
        {
            content = content.Replace("\n", Str.Space);
            return Regex.Replace(content, "\\p{C}+", string.Empty);
        }
    }
}
