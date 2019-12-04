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

#pragma warning disable CA2227
#pragma warning disable CA1819

using Bucket.Json.Converter;
using Bucket.Package;
using Bucket.Semver;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace Bucket.Configuration
{
    /// <summary>
    /// Represents a minimum availability configuration file
    /// for bucket.json and bucket.lock.
    /// </summary>
    // https://github.com/getbucket/bucket/wiki/bucket-schema
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public abstract class ConfigBucketBase
    {
        /// <summary>
        /// Indicates the name of the root node package.
        /// </summary>
        public const string RootPackage = "__root__";

        /// <summary>
        /// Gets or sets the package name.
        /// </summary>
        [JsonProperty("name", Order = 0)]
        public string Name { get; set; } = RootPackage;

        /// <summary>
        /// Gets or sets the package version.
        /// </summary>
        [JsonProperty("version", Order = 5)]
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// Gets or sets the package version with normalized.
        /// </summary>
        [JsonProperty("version_normalized", Order = 6)]
        public string VersionNormalized { get; set; } = null;

        /// <summary>
        /// Gets or sets the package description.
        /// </summary>
        [JsonProperty("description", Order = 10)]
        public string Description { get; set; } = null;

        /// <summary>
        /// Gets or sets the package keywords, useful to full search.
        /// </summary>
        [JsonProperty("keywords", Order = 15)]
        public string[] Keywords { get; set; } = null;

        /// <summary>
        /// Gets or sets the package licenses.
        /// </summary>
        [JsonProperty("license", Order = 20)]
        [JsonConverter(typeof(ConverterArray))]
        public string[] Licenses { get; set; } = null;

        /// <summary>
        /// Gets or sets the package type.
        /// </summary>
        [JsonProperty("type", Order = 25)]
        public string PackageType { get; set; } = null;

        /// <summary>
        /// Gets or sets the release date of the version.
        /// </summary>
        [JsonProperty("release-date", Order = 30)]
        public DateTime? ReleaseDate { get; set; } = null;

        /// <summary>
        /// Gets or sets the package homepage.
        /// </summary>
        [JsonProperty("homepage", Order = 35)]
        public string Homepage { get; set; } = null;

        /// <summary>
        /// Gets or sets the notification-uri.
        /// </summary>
        [JsonProperty("notification-url", Order = 40)]
        public string NotificationUri { get; set; } = null;

        /// <summary>
        /// Gets or sets the package support information.
        /// </summary>
        [JsonProperty("support", Order = 45)]
        public IDictionary<string, string> Support { get; set; } = null;

        /// <summary>
        /// Gets or sets the an array of package authors.
        /// </summary>
        [JsonProperty("authors", Order = 50)]
        public ConfigAuthor[] Authors { get; set; } = null;

        /// <summary>
        /// Gets or sets the requires package.
        /// </summary>
        [JsonProperty("require", Order = 55)]
        public IDictionary<string, string> Requires { get; set; } = null;

        /// <summary>
        /// Gets or sets the replace package.
        /// </summary>
        [JsonProperty("replace", Order = 60)]
        public IDictionary<string, string> Replaces { get; set; } = null;

        /// <summary>
        /// Gets or sets the provide package.
        /// </summary>
        [JsonProperty("provide", Order = 65)]
        public IDictionary<string, string> Provides { get; set; } = null;

        /// <summary>
        /// Gets or sets the conflict package.
        /// </summary>
        [JsonProperty("conflict", Order = 70)]
        public IDictionary<string, string> Conflicts { get; set; } = null;

        /// <summary>
        /// Gets or sets the requires package with dev mode.
        /// </summary>
        [JsonProperty("require-dev", Order = 75)]
        public IDictionary<string, string> RequiresDev { get; set; } = null;

        /// <summary>
        /// Gets or sets the package suggests.
        /// </summary>
        [JsonProperty("suggest", Order = 80)]
        public IDictionary<string, string> Suggests { get; set; } = null;

        /// <summary>
        /// Gets or sets the archive path.
        /// </summary>
        [JsonProperty("archive", Order = 85)]
        [JsonConverter(typeof(ConverterArray), false)]
        public string[] Archive { get; set; } = null;

        /// <summary>
        /// Gets or sets the extra data.
        /// </summary>
        [JsonProperty("extra", Order = 90)]
        public dynamic Extra { get; set; } = null;

        /// <summary>
        /// Gets or sets the installation source.
        /// </summary>
        [JsonProperty("installation_source", Order = 95)]
        [JsonConverter(typeof(StringEnumConverter))]
        public InstallationSource? InstallationSource { get; set; } = null;

        /// <summary>
        /// Gets or sets the binaries path.
        /// </summary>
        [JsonProperty("bin", Order = 100)]
        [JsonConverter(typeof(ConverterArray), false)]
        public string[] Binaries { get; set; } = null;

        /// <summary>
        /// Gets or sets the scripts.
        /// </summary>
        [JsonProperty("scripts", Order = 105)]
        public IDictionary<string, string> Scripts { get; set; } = null;

        /// <summary>
        /// Gets or sets a value indicating whether the package is deprecated.
        /// </summary>
        /// <remarks>null value means the packages not deprecated.</remarks>
        [JsonProperty("deprecated", Order = 110)]
        [JsonConverter(typeof(ConverterDeprecated))]
        public dynamic Deprecated { get; set; } = null;

        /// <summary>
        /// Gets or sets the minimum stability.
        /// </summary>
        [JsonProperty("minimum-stability", Order = 115)]
        [JsonConverter(typeof(StringEnumConverter))]
        public Stabilities? MinimumStability { get; set; } = null;

        /// <summary>
        /// Gets or sets a value indicating whether is prefer stable.
        /// </summary>
        [JsonProperty("prefer-stable", Order = 120)]
        public bool? PreferStable { get; set; } = null;

        /// <summary>
        /// Gets or sets a value indicating the manually configured platform package information.
        /// </summary>
        [JsonProperty("platform", Order = 125)]
        public IDictionary<string, string> Platforms { get; set; } = null;

        /// <summary>
        /// Gets or sets the dist uri.
        /// </summary>
        /// <remarks>This is a special field that is not parsed by default.</remarks>
        [JsonProperty("dist", Order = 130)]
        public ConfigResource Dist { get; set; } = null;

        /// <summary>
        /// Gets or sets the source uri.
        /// </summary>
        /// <remarks>This is a special field that is not parsed by default.</remarks>
        [JsonProperty("source", Order = 135)]
        public ConfigResource Source { get; set; } = null;

        /// <summary>
        /// Gets or sets an array of repositories.
        /// </summary>
        /// <remarks>This is a special field that is not parsed by default.</remarks>
        [JsonProperty("repositories", Order = 140)]
        [JsonConverter(typeof(ConverterRepository))]
        public ConfigRepository[] Repositories { get; set; } = null;

        public static implicit operator string(ConfigBucketBase bucket)
        {
            return bucket.ToString();
        }

        /// <summary>
        /// Indicates whether you need to deserialize the <see cref="VersionNormalized"/> property.
        /// </summary>
        /// <remarks>Json.net contract function.</remarks>
        public virtual bool ShouldDeserializeVersionNormalized()
        {
            return false;
        }

        /// <summary>
        /// Indicates whether you need to serialize the <see cref="VersionNormalized"/> property.
        /// </summary>
        /// <remarks>Json.net contract function.</remarks>
        public virtual bool ShouldSerializeVersionNormalized()
        {
            return false;
        }

        /// <summary>
        /// Indicates whether you need to deserialize the <see cref="InstallationSource"/> property.
        /// </summary>
        /// <remarks>Json.net contract function.</remarks>
        public virtual bool ShouldDeserializeInstallationSource()
        {
            return false;
        }

        /// <summary>
        /// Indicates whether you need to serialize the <see cref="InstallationSource"/> property.
        /// </summary>
        /// <remarks>Json.net contract function.</remarks>
        public virtual bool ShouldSerializeInstallationSource()
        {
            return false;
        }

        /// <summary>
        /// Indicates whether you need to deserialize the <see cref="Dist"/> property.
        /// </summary>
        /// <remarks>Json.net contract function.</remarks>
        public virtual bool ShouldDeserializeDist()
        {
            return false;
        }

        /// <summary>
        /// Indicates whether you need to serialize the <see cref="Dist"/> property.
        /// </summary>
        /// <remarks>Json.net contract function.</remarks>
        public virtual bool ShouldSerializeDist()
        {
            return false;
        }

        /// <summary>
        /// Indicates whether you need to deserialize the <see cref="Source"/> property.
        /// </summary>
        /// <remarks>Json.net contract function.</remarks>
        public virtual bool ShouldDeserializeSource()
        {
            return false;
        }

        /// <summary>
        /// Indicates whether you need to serialize the <see cref="Source"/> property.
        /// </summary>
        /// <remarks>Json.net contract function.</remarks>
        public virtual bool ShouldSerializeSource()
        {
            return false;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}
