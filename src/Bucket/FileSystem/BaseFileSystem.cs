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
using System.IO;
using System.Text.RegularExpressions;

namespace Bucket.FileSystem
{
    /// <summary>
    /// The basic file system implements some common functions.
    /// Provide file path restriction.
    /// </summary>
    public abstract class BaseFileSystem : IFileSystem, IReportPath
    {
        /// <summary>
        /// Gets a value represents the root path of the file system.
        /// </summary>
        public string Root { get; private set; }

        /// <summary>
        /// Returns a relative path from one path to another.
        /// If the given path is a relative path, it will be converted
        /// to the absolute path based on the current path.
        /// </summary>
        /// <remarks>
        /// Alternative to: Path.GetRelativePath in .net standard 2.1.
        /// https://docs.microsoft.com/en-us/dotnet/api/system.io.path.getrelativepath?view=netstandard-2.1 .
        /// </remarks>
        /// <param name="from">The source path the result should be relative to. This path is always considered to be a directory.</param>
        /// <param name="to">The destination path.</param>
        /// <param name="isDirectory">Whether the path is directory.</param>
        /// <returns>The relative path, or <paramref name="to"/> path if the paths don't share the same root.</returns>
        public static string GetRelativePath(string from, string to, bool isDirectory = false)
        {
            string Normalize(string path)
            {
                path = Path.Combine(Environment.CurrentDirectory, path);
                path = GetNormalizePath(path);
                return char.ToLowerInvariant(path[0]) + path.Substring(1);
            }

            from = Normalize(from);
            to = Normalize(to);

            if (isDirectory)
            {
                from = $"{from.TrimEnd('/')}/dummy_file";
            }

            if (Path.GetDirectoryName(from) == Path.GetDirectoryName(to))
            {
                return $"./{Path.GetFileName(to)}";
            }

            var commonPath = to;
            while (!$"{from}/".StartsWith($"{commonPath}/", StringComparison.Ordinal) &&
                commonPath != "/" &&
                !Regex.IsMatch(commonPath, "^[a-z]:/?$", RegexOptions.IgnoreCase))
            {
                commonPath = Path.GetDirectoryName(commonPath).Replace("\\", "/");
            }

            if (from.IndexOf(commonPath, StringComparison.Ordinal) != 0 || commonPath == "/")
            {
                return to;
            }

            commonPath = commonPath.TrimEnd('/') + "/";

            var sourcePathDepth = Str.SubstringCount(from, "/", commonPath.Length);
            var commonPathCode = Str.Repeat("../", sourcePathDepth);

            var resultPath = commonPathCode + ((to.Length >= commonPath.Length) ? to.Substring(commonPath.Length) : string.Empty);
            return string.IsNullOrEmpty(resultPath) ? "./" : resultPath;
        }

        /// <summary>
        /// Normalize path this replaces backslashes with slashes,
        /// removes ending slash and collapses redundant separators
        /// and up-level references.
        /// </summary>
        /// <param name="path">The specified path.</param>
        /// <returns>Returns the normalized the path.</returns>
        public static string GetNormalizePath(string path)
        {
            path = path.Replace("\\", "/");
            var parts = new LinkedList<string>();
            var prefix = string.Empty;
            var absolute = false;

            var match = Regex.Match(path, "^( [0-9a-z]{2,}: (?: // (?: [a-z]: )? )? | [a-z]: )",
                RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
            if (match.Success)
            {
                prefix = match.Groups[1].Value;
                path = path.Substring(prefix.Length);
            }

            if (path[0] == '/')
            {
                absolute = true;
                path = path.Substring(1);
            }

            var up = false;
            foreach (var chunk in path.Split('/'))
            {
                if (chunk == ".." && (absolute || up))
                {
                    if (parts.Count > 0)
                    {
                        parts.RemoveLast();
                    }

                    up = !(parts.Count == 0 || parts.Last.Value == "..");
                }
                else if (!string.IsNullOrEmpty(chunk) && chunk != ".")
                {
                    parts.AddLast(chunk);
                    up = chunk != "..";
                }
            }

            return prefix + (absolute ? "/" : string.Empty) + string.Join("/", parts);
        }

        /// <inheritdoc />
        public virtual string ApplyRootPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return Root ?? string.Empty;
            }

            if (string.IsNullOrEmpty(Root))
            {
                return GetFullPath(path);
            }

            string location;
            if (Path.IsPathRooted(path))
            {
                location = GetFullPath(path);
            }
            else
            {
                location = GetFullPath(Path.Combine(Root, path));
            }

            AssertRootPath(location);
            return location;
        }

        /// <inheritdoc />
        public virtual string RemoveRootPath(string path)
        {
            if (string.IsNullOrEmpty(Root))
            {
                return path;
            }

            if (path.IndexOf(Path.DirectorySeparatorChar) != -1)
            {
                path = GetFullPath(path);
            }

            if (path.StartsWith(Root, StringComparison.Ordinal))
            {
                return path.Length <= Root.Length
                    ? string.Empty
                    : path.Substring(Root.Length).TrimStart(Path.AltDirectorySeparatorChar);
            }

            return path;
        }

        /// <inheritdoc />
        public abstract bool Exists(string path, FileSystemOptions options = FileSystemOptions.File | FileSystemOptions.Directory);

        /// <inheritdoc />
        public abstract void Write(string path, Stream stream, bool append = false);

        /// <inheritdoc />
        public abstract Stream Read(string path);

        /// <inheritdoc />
        public abstract void Move(string path, string newPath);

        /// <inheritdoc />
        public abstract void Copy(string path, string newPath, bool overwrite = true);

        /// <inheritdoc />
        public abstract void Delete(string path = null);

        /// <inheritdoc />
        public abstract DirectoryContents GetContents(string path = null);

        /// <inheritdoc />
        public abstract IMetaData GetMetaData(string path = null);

        /// <summary>
        /// Set the root path.
        /// </summary>
        /// <param name="root">The root path.</param>
        protected internal void SetRootPath(string root)
        {
            if (string.IsNullOrEmpty(root))
            {
                Root = string.Empty;
                return;
            }

            Root = GetFullPath(root);
        }

        /// <summary>
        /// Returns the absolute path of the specified path string.
        /// </summary>
        /// <param name="path">The specified path.</param>
        /// <returns>Returns the absolute path.</returns>
        protected virtual string GetFullPath(string path)
        {
            path = Path.Combine(Environment.CurrentDirectory, path).Replace("\\", "/");

            // If the path has a tail / then we respect the original uri.
            return GetNormalizePath(path) +
                (path.EndsWith("/", StringComparison.Ordinal) && !Regex.IsMatch(path, "://?$") ? "/" : string.Empty);
        }

        /// <summary>
        /// Assert whether you have a root path.
        /// </summary>
        /// <param name="path">The path to assert.</param>
        private void AssertRootPath(string path)
        {
            if (!path.StartsWith(Root, StringComparison.Ordinal))
            {
                throw new UnauthorizedAccessException(
                    $"The path range is beyond root path. root path [{Root}], your path [{path}].");
            }
        }
    }
}
