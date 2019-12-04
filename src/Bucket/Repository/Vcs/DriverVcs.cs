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

using Bucket.Cache;
using Bucket.Configuration;
using Bucket.Downloader.Transport;
using Bucket.IO;
using Bucket.Json;
using Bucket.Util;
using GameBox.Console.Process;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace Bucket.Repository.Vcs
{
    /// <summary>
    /// A driver implementation for driver with authentication interaction.
    /// </summary>
    public abstract class DriverVcs : IDriverVcs
    {
        private readonly IDictionary<string, ConfigBucket> cacheInfomartion;
        private readonly ITransport transport;

        /// <summary>
        /// Initializes a new instance of the <see cref="DriverVcs"/> class.
        /// </summary>
        /// <param name="configRepository">The repository configuration.</param>
        /// <param name="io">The input/output instance.</param>
        /// <param name="config">The config instance.</param>
        /// <param name="transport">The remote transport instance.</param>
        /// <param name="process">The process instance.</param>
        protected DriverVcs(ConfigRepositoryVcs configRepository, IIO io, Config config, ITransport transport = null, IProcessExecutor process = null)
        {
            cacheInfomartion = new Dictionary<string, ConfigBucket>();
            ConfigRepository = configRepository;
            Config = config;
            IO = io;
            Process = process ?? new BucketProcessExecutor(io);
            Uri = configRepository.Uri;
            this.transport = transport ?? new TransportHttp(io, config);
        }

        /// <summary>
        /// Gets or sets indicates the Vcs repository uri.
        /// </summary>
        protected string Uri { get; set; }

        /// <summary>
        /// Gets a value represents a repository configuration.
        /// </summary>
        protected ConfigRepository ConfigRepository { get; private set; }

        /// <summary>
        /// Gets a process executor instance.
        /// </summary>
        protected IProcessExecutor Process { get; private set; }

        /// <summary>
        /// Gets a config.
        /// </summary>
        protected Config Config { get; private set; }

        /// <summary>
        /// Gets a input/output instance.
        /// </summary>
        protected IIO IO { get; private set; }

        /// <inheritdoc />
        public virtual ConfigBucket GetBucketInformation(string identifier)
        {
            if (cacheInfomartion.TryGetValue(identifier, out ConfigBucket configBucket))
            {
                return configBucket;
            }

            var shouldCached = ShouldCache(identifier);

            if (shouldCached && GetCache().TryRead(identifier, out Stream stream))
            {
                configBucket = ParseBucketInformation(stream.ToText());
            }
            else
            {
                configBucket = GetBaseBucketInformation(identifier);
                if (shouldCached)
                {
                    GetCache().Write(identifier, configBucket ?? string.Empty);
                }
            }

            return cacheInfomartion[identifier] = configBucket;
        }

        /// <inheritdoc />
        public abstract void Initialize();

        /// <inheritdoc />
        public abstract string GetRootIdentifier();

        /// <inheritdoc />
        public abstract ConfigResource GetDist(string identifier);

        /// <inheritdoc />
        public abstract ConfigResource GetSource(string identifier);

        /// <inheritdoc />
        public abstract IReadOnlyDictionary<string, string> GetBranches();

        /// <inheritdoc />
        public abstract IReadOnlyDictionary<string, string> GetTags();

        /// <inheritdoc />
        public virtual void Cleanup()
        {
        }

        /// <inheritdoc />
        public bool HasBucketFile(string identifier)
        {
            return GetBucketInformation(identifier) != null;
        }

        /// <summary>
        /// Get the file content.
        /// </summary>
        /// <param name="file">The file name.</param>
        /// <param name="identifier">Any identifier to a specific branch/tag/commit.</param>
        /// <returns>Return the file content.</returns>
        protected internal abstract string GetFileContent(string file, string identifier);

        /// <summary>
        /// Gets the identifier changed date.
        /// </summary>
        /// <param name="identifier">Any identifier to a specific branch/tag/commit.</param>
        /// <returns>Returns the identifier changed date.</returns>
        protected internal abstract DateTime? GetChangeDate(string identifier);

        /// <summary>
        /// Gets the cache system instance.
        /// </summary>
        protected virtual ICache GetCache()
        {
            return null;
        }

        /// <summary>
        /// Get the bucket basic configuration.
        /// </summary>
        /// <param name="identifier">Any identifier to a specific branch/tag/commit.</param>
        /// <returns>Return an configuration instance.</returns>
        protected virtual ConfigBucket GetBaseBucketInformation(string identifier)
        {
            var content = GetFileContent(Factory.DefaultBucketFile, identifier);
            var configBucket = ParseBucketInformation(content);

            if (configBucket != null && configBucket.ReleaseDate == null)
            {
                configBucket.ReleaseDate = GetChangeDate(identifier);
            }

            return configBucket;
        }

        /// <summary>
        /// Parse the bucket info to <see cref="ConfigBucket"/>.
        /// </summary>
        /// <param name="json">The json content.</param>
        /// <returns>Returns the <see cref="ConfigBucket"/> instance.</returns>
        protected virtual ConfigBucket ParseBucketInformation(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            var configBucket = JsonFile.Parse<ConfigBucket>(json);
            return configBucket;
        }

        /// <summary>
        /// Whether the given identifier should be cached or not.
        /// </summary>
        /// <param name="identifier">Any identifier to a specific branch/tag/commit.</param>
        /// <returns>True if the identifier should be cached.</returns>
        protected virtual bool ShouldCache(string identifier)
        {
            return GetCache() != null && Regex.IsMatch(identifier, "[a-f0-9]{40}", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Get the remote content.
        /// </summary>
        protected virtual (string Content, HttpHeaders Headers) GetRemoteContent(string uri)
        {
            var content = transport.GetString(uri, out HttpHeaders headers);
            return (content, headers);
        }

        /// <summary>
        /// Get the transport instance.
        /// </summary>
        protected ITransport GetTransport() => transport;
    }
}
