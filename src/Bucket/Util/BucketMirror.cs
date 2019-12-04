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

using System.Text.RegularExpressions;

namespace Bucket.Util
{
    /// <summary>
    /// Mirror utilities.
    /// </summary>
    public static class BucketMirror
    {
        /// <summary>
        /// Combined mirror uri.
        /// </summary>
        public static string ProcessUri(string mirrorUri, string packageName, string version, string reference, string type)
        {
            reference = reference ?? string.Empty;

            if (!string.IsNullOrEmpty(reference))
            {
                reference = Regex.IsMatch(reference, "^([a-f0-9]*|%reference%)$") ? reference : Security.Md5(reference);
            }

            version = version.Contains("/") ? version : Security.Md5(version);

            mirrorUri = mirrorUri.Replace("%package%", packageName);
            mirrorUri = mirrorUri.Replace("%version%", version);
            mirrorUri = mirrorUri.Replace("%reference%", reference);
            mirrorUri = mirrorUri.Replace("%type%", type);

            return mirrorUri;
        }

        /// <summary>
        /// Combined mirror uri with git.
        /// </summary>
        public static string ProcessUriGit(string mirrorUri, string packageName, string uri, string type)
        {
            var matched = Regex.Match(uri, "^(?:(?:https?|git)://github\\.com/|git@github\\.com:)(?<username>[^/]+)/(?<repository>.+?)(?:\\.git)?$");
            if (matched.Success)
            {
                uri = $"gh-{matched.Groups["username"].Value}/{matched.Groups["repository"].Value}";
            }
            else
            {
                uri = Regex.Replace(uri.Trim('/'), "[^a-z0-9_.-]", "-", RegexOptions.IgnoreCase);
            }

            mirrorUri = mirrorUri.Replace("%package%", packageName);
            mirrorUri = mirrorUri.Replace("%normalizedUri%", uri);
            mirrorUri = mirrorUri.Replace("%type%", type);

            return mirrorUri;
        }
    }
}
