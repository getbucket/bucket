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

using Bucket.Package;

namespace Bucket.Configuration
{
    /// <summary>
    /// Represents a configuration data source.
    /// </summary>
    /// <remarks>Data source can be used to manipulate Json data.</remarks>
    public interface IConfigSource
    {
        /// <summary>
        /// Add a repository.
        /// </summary>
        /// <param name="configRepository">The repository configuration.</param>
        bool AddRepository(ConfigRepository configRepository);

        /// <summary>
        /// Remove a repository.
        /// </summary>
        /// <param name="name">The name of repository will remove.</param>
        bool RemoveRepository(string name);

        /// <summary>
        /// Add a config setting.
        /// </summary>
        /// <param name="name">The name see:<see cref="Settings"/>.</param>
        bool AddConfigSetting(string name, object value);

        /// <summary>
        /// Remove a config setting.
        /// </summary>
        /// <param name="name">The name see:<see cref="Settings"/>.</param>
        bool RemoveConfigSetting(string name);

        /// <summary>
        /// Add a property.
        /// </summary>
        bool AddProperty(string name, object value);

        /// <summary>
        /// Add a property.
        /// </summary>
        bool RemoveProperty(string name);

        /// <summary>
        /// Add a package link.
        /// </summary>
        bool AddLink(LinkType type, string name, string constraint, bool sortPackages = false);

        /// <summary>
        /// Remove a package link.
        /// </summary>
        bool RemoveLink(LinkType type, string name);

        /// <summary>
        /// Gives a user-friendly name to this source (file path or so).
        /// </summary>
        string GetPrettyName();
    }
}
