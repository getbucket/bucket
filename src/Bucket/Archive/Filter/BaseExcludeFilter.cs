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

using Bucket.Util;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Bucket.Archive.Filter
{
    /// <summary>
    /// Exclude filters to exclude files that do not match glob mode.
    /// </summary>
    public abstract class BaseExcludeFilter
    {
        /// <summary>
        /// The parser to be parse on each line.
        /// </summary>
        protected delegate string LineParser(string line);

        /// <summary>
        /// Checks the given path against all exclude patterns in this filter.
        /// Negated patterns overwrite exclude decisions of previous filters.
        /// </summary>
        /// <param name="relativePath">The file's path relative to the sourcePath.</param>
        /// <param name="exclude">Whether a previous filter wants to exclude this file.</param>
        /// <returns>True if the file should be excluded.</returns>
        public virtual bool Filter(string relativePath, bool exclude)
        {
            foreach (var (pattern, negate, stripLeadingSlash) in GetExcludePatterns())
            {
                var path = relativePath;
                if (stripLeadingSlash && relativePath.Length >= 1)
                {
                    path = relativePath.Substring(1);
                }

                if (pattern.IsMatch(path))
                {
                    exclude = !negate;
                }
            }

            return exclude;
        }

        /// <summary>
        /// Processes a file containing exclude rules of different formats per line.
        /// </summary>
        /// <param name="lines">A list of lines to be parsed.</param>
        /// <param name="parser">The parser to be used on each line.</param>
        /// <returns>Exclude patterns to be used in <see cref="Filter" />.</returns>
        protected virtual IEnumerable<string> ParseLines(IEnumerable<string> lines, LineParser parser)
        {
            string InternalParser(string line)
            {
                line = line?.Trim();
                if (string.IsNullOrEmpty(line) ||
                    line.StartsWith("#", StringComparison.Ordinal))
                {
                    return null;
                }

                return parser(line);
            }

            return Arr.Filter(Arr.Map(lines, InternalParser), (pattern) => pattern != null);
        }

        /// <summary>
        /// Generates a set of exclude patterns for <see cref="Filter"/> from a rules.
        /// </summary>
        /// <remarks>This function rule applies to gitignore.</remarks>
        /// <param name="rules">A list of exclude rules which should conform to the glob(3) rule.</param>
        /// <returns>Exclude patterns.</returns>
        protected virtual IEnumerable<FilterPattern> GeneratePatterns(IEnumerable<string> rules)
        {
            var collection = new List<FilterPattern>();
            foreach (var rule in rules)
            {
                collection.Add(GeneratePattern(rule));
            }

            return collection;
        }

        /// <summary>
        /// Generates an exclude pattern for <see cref="Filter"/> from a rule.
        /// </summary>
        /// <remarks>This function rule applies to gitignore.</remarks>
        /// <param name="rule">Rule string, which should conform to the glob(3) rule.</param>
        /// <returns>An exclude pattern.</returns>
        protected virtual FilterPattern GeneratePattern(string rule)
        {
            var negate = false;
            var pattern = new StringBuilder();

            if (rule.Length > 0 && rule[0] == '!')
            {
                negate = true;
                rule = rule.Substring(1);
            }

            if (rule.Length > 0 && rule[0] == '/')
            {
                pattern.Append("^/");
                rule = rule.Substring(1);
            }
            else if (rule.Length - 1 == rule.IndexOf('/'))
            {
                pattern.Append("/");
                rule = rule.Substring(0, rule.Length - 1);
            }
            else if (rule.IndexOf('/') == -1)
            {
                pattern.Append("/");
            }

            pattern.Append(Glob.Parse(rule));
            pattern.Append("(?=$|/)");
            var regex = new Regex(pattern.ToString());

            return new FilterPattern(regex, negate, false);
        }

        /// <summary>
        /// Get the matching pattern of the filter.
        /// </summary>
        /// <remarks>Field Negate indicates whether the current mode is reversed.</remarks>
        protected abstract IEnumerable<FilterPattern> GetExcludePatterns();

#pragma warning disable CA1815
        protected struct FilterPattern
#pragma warning restore CA1815
        {
            public FilterPattern(Regex pattern, bool negate, bool stripLeadingSlash)
            {
                Pattern = pattern;
                Negate = negate;
                StripLeadingSlash = stripLeadingSlash;
            }

            public Regex Pattern { get; }

            public bool Negate { get; }

            public bool StripLeadingSlash { get; }

#pragma warning disable S1144
            public void Deconstruct(out Regex pattern, out bool negate, out bool stripLeadingSlash)
#pragma warning restore S1144
            {
                pattern = Pattern;
                negate = Negate;
                stripLeadingSlash = StripLeadingSlash;
            }
        }
    }
}
