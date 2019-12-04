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

using System.Text;

namespace Bucket.Util
{
    /// <summary>
    /// Glob matches globbing patterns against text.
    /// </summary>
    /// <remarks>Glob implements glob(3) style match.</remarks>
    public static class Glob
    {
        /// <summary>
        /// Returns a regexp which is the equivalent of the glob pattern.
        /// </summary>
        /// <param name="glob">The glob pattern.</param>
        /// <param name="strictLeadingDot">Is it strictly required not to start with a dot.</param>
        /// <param name="strictWildcardSlash">Is it wildcards are strictly required to not allow wildcards slash.</param>
        /// <returns>The regexp.</returns>
        public static string Parse(string glob, bool strictLeadingDot = true, bool strictWildcardSlash = true)
        {
            bool IsIndexChar(string str, int index, char expected)
            {
                if (index >= str.Length)
                {
                    return false;
                }

                return str[index] == expected;
            }

            var firstByte = true;
            var escaping = false;
            var inCurlies = 0;
            var regex = new StringBuilder();
            for (var i = 0; i < glob.Length; ++i)
            {
                var car = glob[i];
                if (firstByte && strictLeadingDot && car != '.')
                {
                    regex.Append(@"(?=[^\.])");
                }

                firstByte = car == '/';
                if (firstByte &&
                    strictWildcardSlash &&
                    IsIndexChar(glob, i + 1, '*') &&
                    IsIndexChar(glob, i + 2, '*') &&
                    ((i + 3) >= glob.Length || IsIndexChar(glob, i + 3, '/')))
                {
                    var str = "[^/]+)+/";
                    if ((i + 3) >= glob.Length)
                    {
                        str += "?";
                    }

                    if (strictLeadingDot)
                    {
                        str = $"(?=[^\\.]){str}";
                    }

                    str = $"/(?:(?:{str})*";

                    i += 2;
                    if ((i + 3) < glob.Length)
                    {
                        i++;
                    }

                    regex.Append(str);
                    escaping = false;
                    continue;
                }

                if (car == '.' || car == '(' || car == ')' || car == '|' || car == '+' || car == '^' || car == '$')
                {
                    regex.Append('\\').Append(car);
                }
                else if (car == '*')
                {
                    if (escaping)
                    {
                        regex.Append("\\*");
                    }
                    else if (strictWildcardSlash)
                    {
                        regex.Append("[^/]*");
                    }
                    else
                    {
                        regex.Append(".*");
                    }
                }
                else if (car == '?')
                {
                    if (escaping)
                    {
                        regex.Append("\\?");
                    }
                    else if (strictWildcardSlash)
                    {
                        regex.Append("[^/]");
                    }
                    else
                    {
                        regex.Append(".");
                    }
                }
                else if (car == '{')
                {
                    regex.Append(escaping ? "\\{" : "(");
                    if (!escaping)
                    {
                        ++inCurlies;
                    }
                }
                else if (car == '}' && inCurlies > 0)
                {
                    regex.Append(escaping ? "}" : ")");
                    if (!escaping)
                    {
                        --inCurlies;
                    }
                }
                else if (car == ',' && inCurlies > 0)
                {
                    regex.Append(escaping ? "," : "|");
                }
                else if (car == '\\')
                {
                    if (escaping)
                    {
                        regex.Append("\\\\");
                        escaping = false;
                    }
                    else
                    {
                        escaping = true;
                    }

                    continue;
                }
                else
                {
                    regex.Append(car);
                }

                escaping = false;
            }

            return regex.ToString();
        }
    }
}
