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

using Bucket.Plugin.Capability;
using System;
using System.Collections.Generic;

namespace Bucket.Plugin
{
    /// <summary>
    /// Indicates the capabilities provided by the plugin.
    /// </summary>
    public interface ICapable
    {
        /// <summary>
        /// Method by which a Plugin announces its API implementations,
        /// through an array with a special type.
        /// </summary>
        /// <remarks>The type returned must be implemented <see cref="ICapability"/>.</remarks>
        IEnumerable<Type> GetCapabilities();
    }
}
