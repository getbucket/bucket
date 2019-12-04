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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Bucket.Util
{
    /// <summary>
    /// The str helper.
    /// </summary>
    /// <remarks>This class is extracted from GameBox.Core.</remarks>
    [ExcludeFromCodeCoverage]
    internal static class Arr
    {
        /// <summary>
        /// Combine multiple specified arrays into one array.
        /// </summary>
        /// <typeparam name="T">The type of array.</typeparam>
        /// <param name="sources">The specified array.</param>
        /// <returns>Returns an merged array.</returns>
#pragma warning disable S2368
        public static T[] Merge<T>(params T[][] sources)
#pragma warning restore S2368
        {
            Guard.Requires<ArgumentNullException>(sources != null);
            var length = 0;
            foreach (var source in sources)
            {
                if (source == null || source.Length <= 0)
                {
                    continue;
                }

                length += source.Length;
            }

            if (length <= 0)
            {
                return Array.Empty<T>();
            }

            var merge = new T[length];
            var current = 0;
            foreach (var source in sources)
            {
                if (source == null || source.Length <= 0)
                {
                    continue;
                }

                Array.Copy(source, 0, merge, current, source.Length);
                current += source.Length;
            }

            return merge;
        }

        /// <summary>
        /// Pass the value of the iterator into the callback function, and the
        /// value returned by the custom function as the new array value.
        /// </summary>
        /// <typeparam name="T">The type of array.</typeparam>
        /// <typeparam name="TReturn">The type of return value.</typeparam>
        /// <param name="source">The source iterator.</param>
        /// <param name="callback">The callback.</param>
        /// <returns>Returns an new array.</returns>
        public static TReturn[] Map<T, TReturn>(IEnumerable<T> source, Func<T, TReturn> callback)
        {
            Guard.Requires<ArgumentNullException>(callback != null);

            if (source == null)
            {
                return Array.Empty<TReturn>();
            }

            var requested = new List<TReturn>();
            foreach (var value in source)
            {
                requested.Add(callback.Invoke(value));
            }

            return requested.ToArray();
        }

        /// <summary>
        /// Removed the first element in the array and return the value of the removed element.
        /// </summary>
        /// <typeparam name="T">The type of array.</typeparam>
        /// <param name="source">The specified array.</param>
        /// <returns>Returns the removed value.</returns>
        public static T Shift<T>(ref T[] source)
        {
            Guard.Requires<ArgumentNullException>(source != null);
            Guard.Requires<InvalidOperationException>(source.Length > 0);

            var requested = source[0];
            var newSource = new T[source.Length - 1];

            Array.Copy(source, 1, newSource, 0, source.Length - 1);
            source = newSource;

            return requested;
        }

        /// <summary>
        /// Add one or more elements to the end of the array.
        /// </summary>
        /// <typeparam name="T">The type of array.</typeparam>
        /// <param name="source">The specified array.</param>
        /// <param name="elements">The added elements.</param>
        /// <returns>Returns the length of the new array.</returns>
        public static int Push<T>(ref T[] source, params T[] elements)
        {
            Guard.Requires<ArgumentNullException>(source != null);
            Guard.Requires<InvalidOperationException>(elements != null);

            Array.Resize(ref source, source.Length + elements.Length);
            Array.Copy(elements, 0, source, source.Length - elements.Length, elements.Length);

            return source.Length;
        }

        /// <summary>
        /// Each value in the source array is passed to the callback function.
        /// If the callback function is equal to the <paramref name="expected"/>
        /// value, the current value in the input array is added to the result array.
        /// </summary>
        /// <typeparam name="T">The type of array.</typeparam>
        /// <param name="source">The specified array.</param>
        /// <param name="predicate">The callback.</param>
        /// <param name="expected">The expected value.</param>
        /// <returns>Returns an filtered array.</returns>
        public static T[] Filter<T>(IEnumerable<T> source, Predicate<T> predicate, bool expected = true)
        {
            Guard.Requires<ArgumentNullException>(predicate != null);

            if (source == null)
            {
                return Array.Empty<T>();
            }

            var results = new List<T>();
            foreach (var result in source)
            {
                if (predicate.Invoke(result) == expected)
                {
                    results.Add(result);
                }
            }

            return results.ToArray();
        }

        /// <summary>
        /// Remove and return the array element of the specified index.
        /// <para>If the index is passed a negative number then it will be removed from the end.</para>
        /// </summary>
        /// <typeparam name="T">The type of array.</typeparam>
        /// <param name="source">The specified array.</param>
        /// <param name="index">The index of array.</param>
        /// <returns>Returns removed element.</returns>
        public static T RemoveAt<T>(ref T[] source, int index)
        {
            Guard.Requires<ArgumentNullException>(source != null);
            Guard.Requires<ArgumentNullException>(index < source.Length);

            var result = Splice(ref source, index, 1);
            return result.Length > 0 ? result[0] : default;
        }

        /// <summary>
        /// Removes an element of the specified length from the array. If
        /// the <paramref name="replSource"/> parameter is given, the new
        /// element is inserted from the <paramref name="start"/> position.
        /// </summary>
        /// <typeparam name="T">The type of array.</typeparam>
        /// <param name="source">The specified array.</param>
        /// <param name="start">
        /// Delete the start position of the element.
        /// <para>If the value is set to a positive number, delete it from the beginning of the trip.</para>
        /// <para>If the value is set to a negative number, the <paramref name="start"/> absolute value is taken from the back.</para>
        /// </param>
        /// <param name="length">
        /// Number of deleted elements.
        /// <para>If the value is set to a positive number, then the number of elements is returned。.</para>
        /// <para>If the value is set to a negative number, then remove the <paramref name="length"/> absolute position from the back to the front to delete.</para>
        /// <para>If the value is not set, then all elements from the position set by the <paramref name="start"/> parameter to the end of the array are returned.</para>
        /// </param>
        /// <param name="replSource">An array inserted at the start position.</param>
        /// <returns>An removed array.</returns>
        public static T[] Splice<T>(ref T[] source, int start, int? length = null, T[] replSource = null)
        {
            Guard.Requires<ArgumentNullException>(source != null);

            NormalizationPosition(source.Length, ref start, ref length);

            var requested = new T[length.Value];

            if (length.Value == source.Length)
            {
                Array.Copy(source, requested, source.Length);
                source = replSource ?? Array.Empty<T>();
                return requested;
            }

            Array.Copy(source, start, requested, 0, length.Value);

            if (replSource == null || replSource.Length == 0)
            {
                var newSource = new T[source.Length - length.Value];
                if (start > 0)
                {
                    Array.Copy(source, 0, newSource, 0, start);
                }

                Array.Copy(source, start + length.Value, newSource, start, source.Length - (start + length.Value));
                source = newSource;
            }
            else
            {
                var newSource = new T[source.Length - length.Value + replSource.Length];
                if (start > 0)
                {
                    Array.Copy(source, 0, newSource, 0, start);
                }

                Array.Copy(replSource, 0, newSource, start, replSource.Length);
                Array.Copy(source, start + length.Value, newSource, start + replSource.Length,
                    source.Length - (start + length.Value));
                source = newSource;
            }

            return requested;
        }

        /// <summary>
        /// Take a value from the array according to the condition and return.
        /// </summary>
        /// <typeparam name="T">The type of array.</typeparam>
        /// <param name="source">The specified array.</param>
        /// <param name="start">
        /// Remove the starting position of the element.
        /// <para>If the value is set to a positive number, it will be taken from the beginning of the trip.</para>
        /// <para>If the value is set to a negative number, the <paramref name="start"/> absolute value is taken from the back.</para>
        /// </param>
        /// <param name="length">
        /// Returns the length of the array.
        /// <para>If the value is set to a positive number, then the number of elements is returned。.</para>
        /// <para>If the value is set to a negative number, then remove the <paramref name="length"/> absolute position from the back to the front to delete.</para>
        /// <para>If the value is not set, then all elements from the position set by the <paramref name="start"/> parameter to the end of the array are returned.</para>
        /// </param>
        /// <returns>Returns an new array.</returns>
        public static T[] Slice<T>(T[] source, int start, int? length = null)
        {
            if (source == null)
            {
                return Array.Empty<T>();
            }

            NormalizationPosition(source.Length, ref start, ref length);

            var requested = new T[length.Value];
            Array.Copy(source, start, requested, 0, length.Value);

            return requested;
        }

        /// <summary>
        /// Exclude the specified value in the array.
        /// </summary>
        /// <typeparam name="T">The type of array.</typeparam>
        /// <param name="source">The source array.</param>
        /// <param name="match">An array of exclude value.</param>
        /// <returns>Returns an array of processed.</returns>
        public static T[] Difference<T>(T[] source, params T[] match)
        {
            Guard.Requires<ArgumentNullException>(source != null);
            if (match == null)
            {
                return source;
            }

            return Filter(source, (val) =>
            {
                foreach (var t in match)
                {
                    if (val.Equals(t))
                    {
                        return false;
                    }
                }

                return true;
            });
        }

        /// <summary>
        /// Delete the last element in the array and return the deleted element
        /// as the return value.
        /// </summary>
        /// <typeparam name="T">The type of the array.</typeparam>
        /// <param name="sources">The specified array.</param>
        /// <returns>Returns removed element.</returns>
        public static T Pop<T>(ref T[] sources)
        {
            Guard.Requires<ArgumentNullException>(sources != null, $"{nameof(sources)} should not be null.");
            Guard.Requires<InvalidOperationException>(sources.Length > 0, $"The number of elements needs to be greater than 0.");

            var candidate = sources[sources.Length - 1];
            Array.Resize(ref sources, sources.Length - 1);
            return candidate;
        }

        /// <summary>
        /// Pass the specified array to the callback test.
        /// <para>The function returns false only if all elements pass the checker are false.</para>
        /// </summary>
        /// <typeparam name="T">The type of array.</typeparam>
        /// <param name="sources">The specified array.</param>
        /// <param name="predicate">The callback.</param>
        /// <returns>True if pass the test.</returns>
        public static bool Test<T>(IEnumerable<T> sources, Predicate<T> predicate)
        {
            Guard.Requires<ArgumentNullException>(predicate != null, $"Must set a {predicate}.");

            if (sources == null)
            {
                return false;
            }

            foreach (var source in sources)
            {
                if (predicate(source))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Pass the specified array to the callback test.
        /// <para>The function returns false only if all elements pass the checker are false.</para>
        /// </summary>
        /// <typeparam name="T">The type of array.</typeparam>
        /// <param name="sources">The specified array.</param>
        /// <param name="predicate">The callback.</param>
        /// <returns>True if pass the test.</returns>
        public static bool Test<T>(IEnumerable<T> sources, Predicate<T> predicate, out T match)
        {
            Guard.Requires<ArgumentNullException>(predicate != null, $"Must set a {predicate}.");

            match = default;
            if (sources == null)
            {
                return false;
            }

            foreach (var source in sources)
            {
                if (predicate(source))
                {
                    match = source;
                    return true;
                }
            }

            return false;
        }

        internal static void NormalizationPosition(int sourceLength, ref int start, ref int? length)
        {
            start = (start >= 0) ? Math.Min(start, sourceLength) : Math.Max(sourceLength + start, 0);

            if (length == null)
            {
                length = Math.Max(sourceLength - start, 0);
                return;
            }

            length = (length >= 0)
                    ? Math.Min(length.Value, sourceLength - start)
                    : Math.Max(sourceLength + length.Value - start, 0);
        }
    }
}
