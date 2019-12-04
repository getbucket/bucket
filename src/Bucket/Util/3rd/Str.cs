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

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;

namespace Bucket.Util
{
    /// <summary>
    /// The str helper.
    /// </summary>
    /// <remarks>This class is extracted from GameBox.Core.</remarks>
    [ExcludeFromCodeCoverage]
    internal static class Str
    {
        /// <summary>
        /// The space string.
        /// </summary>
        public const string Space = " ";

        /// <summary>
        /// Fill types.
        /// </summary>
        public enum PadType
        {
            /// <summary>
            /// Fill both sides of the string. If it is not even, the right side gets extra padding.
            /// </summary>
            Both,

            /// <summary>
            /// Fill the left side of the string.
            /// </summary>
            Left,

            /// <summary>
            /// Fill the right side of the string.
            /// </summary>
            Right,
        }

        /// <summary>
        /// Gets the default string encoding.
        /// </summary>
        public static Encoding Encoding { get; } = Encoding.UTF8;

        /// <summary>
        /// Repeat the specified number of times for the space.
        /// </summary>
        /// <param name="num">The number of times.</param>
        /// <returns>The repeated space.</returns>
        public static string Repeat(int num)
        {
            return Repeat(Space, num);
        }

        /// <summary>
        /// Repeat the specified number of times for the string.
        /// </summary>
        /// <param name="str">The string will be repeat.</param>
        /// <param name="num">The number of times.</param>
        /// <returns>The repeated string.</returns>
        public static string Repeat(string str, int num)
        {
            if (num <= 0)
            {
                return string.Empty;
            }

            var requested = new StringBuilder();
            for (var i = 0; i < num; i++)
            {
                requested.Append(str);
            }

            return requested.ToString();
        }

        /// <summary>
        /// Fill the string with the new length.
        /// </summary>
        /// <param name="length">The new string length. If the value is less than the original length of the string, no action is taken.</param>
        /// <param name="str">The string to be filled.</param>
        /// <param name="padStr">A string to be used for padding. The default is blank.</param>
        /// <param name="type">
        /// Fill in which side of the string.
        /// <para><see cref="PadType.Both"/>Fill both sides of the string. If not even, get extra padding on the right side.</para>
        /// <para><see cref="PadType.Left"/>Fill the left side of the string.</para>
        /// <para><see cref="PadType.Right"/>Fill the right side of the string.</para>
        /// </param>
        /// <returns>Returns filled string.</returns>
        public static string Pad(int length, string str = null, string padStr = null, PadType type = PadType.Right)
        {
            str = str ?? string.Empty;

            var needPadding = length - str.Length;
            if (needPadding <= 0)
            {
                return str;
            }

            int rightPadding;
            var leftPadding = rightPadding = 0;

            if (type == PadType.Both)
            {
                leftPadding = needPadding >> 1;
                rightPadding = (needPadding >> 1) + (needPadding % 2 == 0 ? 0 : 1);
            }
            else if (type == PadType.Right)
            {
                rightPadding = needPadding;
            }
            else
            {
                leftPadding = needPadding;
            }

            padStr = padStr ?? Space;
            padStr = padStr.Length <= 0 ? Space : padStr;

            var leftPadCount = (leftPadding / padStr.Length) + (leftPadding % padStr.Length == 0 ? 0 : 1);
            var rightPadCount = (rightPadding / padStr.Length) + (rightPadding % padStr.Length == 0 ? 0 : 1);

            return Repeat(padStr, leftPadCount).Substring(0, leftPadding) + str +
                   Repeat(padStr, rightPadCount).Substring(0, rightPadding);
        }

        /// <summary>
        /// Lowercase and use words to divide words with dashes.
        /// </summary>
        /// <param name="value">The string will formatted.</param>
        /// <returns>Returns formatted string.</returns>
        public static string LowerDashes(string value)
        {
            return Regex.Replace(value, "(?:([a-z])([A-Z])|([A-Z])([A-Z][a-z]))", "${1}${3}-${2}${4}").ToLower();
        }

        /// <summary>
        /// Replace the match in the specified string.
        /// </summary>
        /// <param name="matches">An array of the match string.</param>
        /// <param name="replace">The replacement value.</param>
        /// <param name="str">The specified string.</param>
        /// <returns>Returns the replacement string.</returns>
        public static string Replace(string[] matches, string replace, string str)
        {
            if (matches == null || string.IsNullOrEmpty(str))
            {
                return str;
            }

            replace = replace ?? string.Empty;

            foreach (var match in matches)
            {
                str = str.Replace(match, replace);
            }

            return str;
        }

        /// <summary>
        /// Translate the specified string into an asterisk match expression and test.
        /// </summary>
        /// <param name="pattern">The match pattern.</param>
        /// <param name="value">The. </param>
        /// <returns>True if matches.</returns>
        public static bool Is(string pattern, string value)
        {
            return pattern == value || Regex.IsMatch(value, "^" + AsteriskWildcard(pattern) + "$");
        }

        /// <summary>
        /// Translate the specified string into an asterisk match expression.
        /// </summary>
        /// <param name="pattern">The match pattern.</param>
        /// <returns>Returns processed string.</returns>
        public static string AsteriskWildcard(string pattern)
        {
            pattern = Regex.Escape(pattern);
            pattern = pattern.Replace(@"\*", ".*?");

            return pattern;
        }

        /// <summary>
        /// Calculate the number of times a substring appears in a string.
        /// <para>This function does not count overlapping substrings.</para>
        /// </summary>
        /// <param name="str">The specified string.</param>
        /// <param name="substr">The substring.</param>
        /// <param name="start">The starting position.</param>
        /// <param name="length">The length to calculate.</param>
        /// <param name="comparison">The string comparison.</param>
        /// <returns>Returns the number of times a substring appears. -1 means unable to calculate.</returns>
        public static int SubstringCount(string str, string substr, int start = 0, int? length = null, StringComparison comparison = StringComparison.CurrentCultureIgnoreCase)
        {
            if (string.IsNullOrEmpty(str) || string.IsNullOrEmpty(substr))
            {
                return 0;
            }

            Arr.NormalizationPosition(str.Length, ref start, ref length);

            var count = 0;
            while (length.Value > 0)
            {
                int index;
                if ((index = str.IndexOf(substr, start, length.Value, comparison)) < 0)
                {
                    break;
                }

                count++;
                length -= index + substr.Length - start;
                start = index + substr.Length;
            }

            return count;
        }

        /// <summary>
        /// Calculate Levenshtein distance between two strings.
        /// </summary>
        /// <param name="a">The string 1.</param>
        /// <param name="b">The string 2.</param>
        /// <returns>
        /// This function returns the Levenshtein-Distance between the two argument
        /// strings or -1, if one of the argument strings is longer than the limit
        /// of 255 characters.
        /// </returns>
        public static int Levenshtein(string a, string b)
        {
            if (a == null || b == null)
            {
                return -1;
            }

            var lengthA = a.Length;
            var lengthB = b.Length;

            if (lengthA > 255 || lengthB > 255)
            {
                return -1;
            }

            var pA = new int[lengthB + 1];
            var pB = new int[lengthB + 1];

            for (var i = 0; i <= lengthB; i++)
            {
                pA[i] = i;
            }

            int Min(int num1, int num2, int num3)
            {
                var min = num1;
                if (min > num2)
                {
                    min = num2;
                }

                if (min > num3)
                {
                    min = num3;
                }

                return min;
            }

            for (var i = 0; i < lengthA; i++)
            {
                pB[0] = pA[0] + 1;
                for (var n = 0; n < lengthB; n++)
                {
                    var distance = a[i] == b[n]
                        ? Min(pA[n], pA[n + 1] + 1, pB[n] + 1)
                        : Min(pA[n] + 1, pA[n + 1] + 1, pB[n] + 1);
                    pB[n + 1] = distance;
                }

                var temp = pA;
                pA = pB;
                pB = temp;
            }

            return pA[lengthB];
        }
    }
}
