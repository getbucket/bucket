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
using Bucket.FileSystem;
using Bucket.IO;
using Bucket.Json;
using Bucket.Package.Loader;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Bucket.Util
{
    /// <summary>
    /// Verify that a configuration is valid by Json Schema.
    /// </summary>
    public class ValidatorBucket
    {
        private readonly IIO io;
        private readonly IFileSystem fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidatorBucket"/> class.
        /// </summary>
        /// <param name="fileSystem">Configuration content will be obtained from the file system.</param>
        /// <param name="io">The input/output instance.</param>
        public ValidatorBucket(IFileSystem fileSystem = null, IIO io = null)
        {
            this.fileSystem = fileSystem;
            this.io = io;
        }

        /// <summary>
        /// Validates the bucket file, and returns the result.
        /// </summary>
        /// <param name="file">The bucket file.</param>
        /// <returns>A tuple containing the warnings, publish errors and errors.</returns>
        public (string[] Warnings, string[] PublishErrors, string[] Errors) Validate(string file)
        {
            var warnings = new List<string>();
            var publishErrors = new List<string>();
            var errors = new List<string>();
            ConfigBucket manifest;

            try
            {
                var jsonFile = new JsonFile(file, fileSystem, io);
                if (!jsonFile.IsValidate(out IList<string> errorMessages))
                {
                    errors.AddRange(errorMessages);
                    return (warnings.ToArray(), publishErrors.ToArray(), errors.ToArray());
                }

                manifest = jsonFile.Read<ConfigBucket>();
            }
#pragma warning disable CA1031
            catch (System.Exception ex)
#pragma warning restore CA1031
            {
                errors.Add(ex.Message);
                return (warnings.ToArray(), publishErrors.ToArray(), errors.ToArray());
            }

            if (!string.IsNullOrEmpty(manifest.Name))
            {
                var matched = Regex.Match(manifest.Name, Factory.RegexPackageName, RegexOptions.IgnoreCase);
                if (!matched.Success && !Regex.IsMatch(manifest.Name, $"^{Factory.RegexPackageNameIllegal}$"))
                {
                    var illegalChars = Regex.Replace(manifest.Name, Factory.RegexPackageNameIllegal, string.Empty);
                    errors.Add($"The name is invalid. \"{illegalChars}\" is not allowed in package names.");
                }
                else if (!matched.Success)
                {
                    errors.Add($"Names can only begin with a letter, and multiple \"/\" different provide are not allowed.");
                }
                else if (string.IsNullOrEmpty(matched.Groups["provide"].Value))
                {
                    warnings.Add($"It is recommended to add a provider name to the package (e.g. provide-name/package-name).");
                }

                if (manifest.Name != manifest.Name.ToLower())
                {
                    var suggestName = Str.LowerDashes(manifest.Name);
                    publishErrors.Add($"Name \"{manifest.Name}\" does not match the best practice (e.g. lower-cased/with-dashes). We suggest using \"{suggestName}\" instead.");
                }
            }

            if (manifest.Licenses == null || manifest.Licenses.Length <= 0)
            {
                warnings.Add("No license specified, it is recommended to do so. For closed-source software you may use \"proprietary\" as license.");
            }

            if ((manifest.Requires != null && manifest.Requires.Count > 0)
                && (manifest.RequiresDev != null && manifest.RequiresDev.Count > 0))
            {
                var requiresOverrides = manifest.Requires.Keys.Intersect(manifest.RequiresDev.Keys).ToArray();
                var plural = (requiresOverrides.Length > 1) ? "are" : "is";
                var message = string.Join(", ", requiresOverrides);
                warnings.Add($"{message} {plural} required both in require and require-dev, this can lead to unexpected behavior.");
            }

            var iterator = new DictionaryIterator<string, string>(manifest.Requires, manifest.RequiresDev);
            foreach (var item in iterator)
            {
                if (item.Value.IndexOf('#') == -1)
                {
                    continue;
                }

                warnings.Add($"The package \"{item.Key}\" is pointing to a commit-ref, this is bad practice and can cause unforeseen issues.");
            }

            var loader = new LoaderValidating(new LoaderPackage());
            try
            {
                loader.Load(manifest);
            }
            catch (InvalidPackageException ex)
            {
                errors.AddRange(ex.GetErrors());
            }

            warnings.AddRange(loader.GetWarnings());
            return (warnings.ToArray(), publishErrors.ToArray(), errors.ToArray());
        }

        private class DictionaryIterator<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
        {
            private readonly HashSet<TKey> iterated;
            private readonly IDictionary<TKey, TValue>[] collections;

            public DictionaryIterator(params IDictionary<TKey, TValue>[] collections)
            {
                this.collections = collections;
                iterated = new HashSet<TKey>();
            }

            public bool Deduplication { get; set; } = true;

            public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
            {
                for (var i = 0; i < collections.Length; i++)
                {
                    var collection = collections[i];
                    if (collection == null)
                    {
                        continue;
                    }

                    foreach (var item in collection)
                    {
                        if (Deduplication && iterated.Contains(item.Key))
                        {
                            continue;
                        }
                        else if (Deduplication)
                        {
                            iterated.Add(item.Key);
                        }

                        yield return item;
                    }
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
