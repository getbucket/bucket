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

#pragma warning disable SA1402

using System.Collections.Generic;

namespace Bucket.Configuration
{
    /// <summary>
    /// Represents a configured authorization data.
    /// </summary>
    /// <remarks>This class is meant to increase the readability of the configuration.</remarks>
    public class ConfigAuth : Dictionary<string, string>
    {
        /// <summary>
        /// Gets represents an empty authorization object.
        /// </summary>
        public static ConfigAuth Empty { get; } = new ConfigAuth();
    }

    /// <summary>
    /// Represents a configured authorization data.
    /// </summary>
    /// <typeparam name="T">Type of the auth detail infomation.</typeparam>
    /// <remarks>This class is meant to increase the readability of the configuration.</remarks>
    public class ConfigAuth<T> : Dictionary<string, T>
        where T : AuthBase, new()
    {
        /// <summary>
        /// Gets represents an empty authorization object.
        /// </summary>
#pragma warning disable CA1000
        public static ConfigAuth<T> Empty => EmptyInstance<ConfigAuth<T>>.Value;
#pragma warning restore CA1000

        private static class EmptyInstance<TInstance>
            where TInstance : new()
        {
            internal static readonly TInstance Value = new TInstance();
        }
    }
}
