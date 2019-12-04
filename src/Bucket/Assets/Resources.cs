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

using System.IO;

namespace Bucket.Assets
{
    /// <summary>
    /// Represents an assembly embedded resource file.
    /// </summary>
    internal static class Resources
    {
        /// <summary>
        /// Get the file contents.
        /// </summary>
        /// <param name="fileName">The file name with relative path. For example: "Schema/bucket-schema.json".</param>
        /// <returns>The file contents.</returns>
        public static string GetString(string fileName)
        {
            using (var stream = GetStream(fileName))
            {
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// Get the file stream.
        /// </summary>
        /// <param name="fileName">The file name with relative path. For example: "Schema/bucket-schema.json".</param>
        /// <returns>The file stream.</returns>
        public static Stream GetStream(string fileName)
        {
            var path = GetPath(fileName);
            var stream = typeof(Resources).Assembly.GetManifestResourceStream(path);

            if (stream == null)
            {
                var message = $"The embedded resource \"{path}\" was not found.";
                throw new FileNotFoundException(message, path);
            }

            return stream;
        }

        private static string GetPath(string fileName)
        {
            const string pathSeparator = ".";
            return typeof(Resources).Namespace + pathSeparator + fileName.Replace('/', '.');
        }
    }
}
