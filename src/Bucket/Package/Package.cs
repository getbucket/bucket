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

using Bucket.Semver;
using Bucket.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bucket.Package
{
    /// <summary>
    /// Core package definitions that are needed to resolve requires and install packages.
    /// </summary>
    public class Package : BasePackage
    {
        private readonly string version;
        private readonly string versionPretty;
        private readonly Stabilities stability;
        private string packageType;
        private Link[] conflicts;
        private Link[] requires;
        private Link[] requiresDev;
        private Link[] provides;
        private Link[] replaces;
        private string[] archives;
        private dynamic extra;
        private string[] binaries;
        private IDictionary<string, string> suggests;
        private string notificationUrl;
        private InstallationSource? installationSource;
        private DateTime? releaseDate;
        private string sourceType;
        private string sourceReference;
        private string sourceUri;
        private string[] sourceMirrors;
        private string distType;
        private string distReference;
        private string distShasum;
        private string[] distMirrors;
        private string distUri;

        /// <summary>
        /// Initializes a new instance of the <see cref="Package"/> class.
        /// </summary>
        /// <param name="name">The package name.</param>
        /// <param name="version">Normalized version.</param>
        /// <param name="versionPretty">The package non-normalized version(Human readable).</param>
        public Package(string name, string version, string versionPretty)
            : base(name)
        {
            this.version = version;
            this.versionPretty = versionPretty;

            stability = VersionParser.ParseStability(version);
        }

        /// <inheritdoc />
        public override string GetPackageType()
        {
            return packageType;
        }

        /// <summary>
        /// Sets the package type.
        /// </summary>
        /// <param name="packageType">The package type.</param>
        public void SetPackageType(string packageType)
        {
            this.packageType = packageType;
        }

        /// <inheritdoc />
        public override Stabilities GetStability()
        {
            return stability;
        }

        /// <inheritdoc />
        public override string GetVersion()
        {
            return version;
        }

        /// <inheritdoc />
        public override string GetVersionPretty()
        {
            return versionPretty;
        }

        /// <inheritdoc />
        public override Link[] GetConflicts()
        {
            return conflicts ?? Array.Empty<Link>();
        }

        /// <summary>
        /// Set the conflicting packages.
        /// </summary>
        /// <param name="conflicts">An array of package links.</param>
        public void SetConflicts(Link[] conflicts)
        {
            this.conflicts = conflicts;
        }

        /// <inheritdoc />
        public override Link[] GetProvides()
        {
            return provides ?? Array.Empty<Link>();
        }

        /// <summary>
        /// Set the provided virtual packages.
        /// </summary>
        /// <param name="provides">An array of package links.</param>
        public void SetProvides(Link[] provides)
        {
            this.provides = provides;
        }

        /// <inheritdoc />
        public override Link[] GetReplaces()
        {
            return replaces ?? Array.Empty<Link>();
        }

        /// <summary>
        /// Set the packages this one replaces.
        /// </summary>
        /// <param name="replaces">An array of package links.</param>
        public void SetReplaces(Link[] replaces)
        {
            this.replaces = replaces;
        }

        /// <inheritdoc />
        public override Link[] GetRequires()
        {
            return requires ?? Array.Empty<Link>();
        }

        /// <summary>
        /// Set the require packages.
        /// </summary>
        /// <param name="requires">An array of package links.</param>
        public void SetRequires(Link[] requires)
        {
            this.requires = requires;
        }

        /// <inheritdoc />
        public override Link[] GetRequiresDev()
        {
            return requiresDev ?? Array.Empty<Link>();
        }

        /// <summary>
        /// Set the require packages with developer mode.
        /// </summary>
        /// <param name="requiresDev">An array of package links.</param>
        public void SetRequiresDev(Link[] requiresDev)
        {
            this.requiresDev = requiresDev;
        }

        /// <inheritdoc />
        public override string[] GetArchives()
        {
            return archives ?? Array.Empty<string>();
        }

        /// <summary>
        /// Sets an array of patterns to be archives.
        /// </summary>
        /// <param name="archives">An array of patterns to be archives.</param>
        public void SetArchives(string[] archives)
        {
            this.archives = archives;
        }

        /// <inheritdoc />
        public override dynamic GetExtra()
        {
            return extra;
        }

        /// <summary>
        /// Sets the package extra data.
        /// </summary>
        public void SetExtra(dynamic extra)
        {
            this.extra = extra;
        }

        /// <inheritdoc />
        public override string[] GetBinaries()
        {
            return binaries ?? Array.Empty<string>();
        }

        /// <summary>
        /// Sets an array of binaries files.
        /// </summary>
        /// <param name="binaries">An array of binaries files.</param>
        public void SetBinaries(string[] binaries)
        {
            this.binaries = binaries;
        }

        /// <inheritdoc />
        public override string GetNotificationUri()
        {
            return notificationUrl;
        }

        /// <summary>
        /// Sets the notification URI.
        /// </summary>
        /// <param name="notificationUrl">The notification URI.</param>
        public void SetNotificationUri(string notificationUrl)
        {
            this.notificationUrl = notificationUrl;
        }

        /// <inheritdoc />
        public override DateTime? GetReleaseDate()
        {
            return releaseDate;
        }

        /// <summary>
        /// Sets the release date with utc.
        /// </summary>
        /// <param name="releaseDate">The release date with utc.</param>
        public void SetReleaseDate(DateTime? releaseDate)
        {
            this.releaseDate = releaseDate;
        }

        /// <inheritdoc />
        public override string GetDistShasum()
        {
            return distShasum;
        }

        /// <inheritdoc />
        public override void SetDistShasum(string distShasum)
        {
            if (string.IsNullOrEmpty(distShasum))
            {
                this.distShasum = null;
            }
            else
            {
                this.distShasum = distShasum;
            }
        }

        /// <inheritdoc />
        public override string[] GetDistMirrors()
        {
            return distMirrors ?? Array.Empty<string>();
        }

        /// <inheritdoc />
        public override void SetDistMirrors(string[] mirrors)
        {
            distMirrors = mirrors;
        }

        /// <inheritdoc />
        public override string GetDistReference()
        {
            return distReference;
        }

        /// <inheritdoc />
        public override void SetDistReference(string distReference)
        {
            if (string.IsNullOrEmpty(distReference))
            {
                this.distReference = null;
            }
            else
            {
                this.distReference = distReference;
            }
        }

        /// <inheritdoc />
        public override string GetDistType()
        {
            return distType;
        }

        /// <inheritdoc />
        public override void SetDistType(string distType)
        {
            this.distType = distType;
        }

        /// <inheritdoc />
        public override string GetDistUri()
        {
            return distUri;
        }

        /// <inheritdoc />
        public override string[] GetDistUris()
        {
            return GetUrisWithMirrors(GetDistUri(), GetDistMirrors(), GetDistReference(), GetDistType(), InstallationSource.Dist);
        }

        /// <inheritdoc />
        public override void SetDistUri(string distUri)
        {
            this.distUri = distUri;
        }

        /// <inheritdoc />
        public override string GetSourceReference()
        {
            return sourceReference;
        }

        /// <inheritdoc />
        public override void SetSourceReference(string sourceReference)
        {
            if (string.IsNullOrEmpty(sourceReference))
            {
                this.sourceReference = null;
            }
            else
            {
                this.sourceReference = sourceReference;
            }
        }

        /// <inheritdoc />
        public override string GetSourceType()
        {
            return sourceType;
        }

        /// <inheritdoc />
        public override void SetSourceType(string sourceType)
        {
            this.sourceType = sourceType;
        }

        /// <inheritdoc />
        public override string GetSourceUri()
        {
            return sourceUri;
        }

        /// <inheritdoc />
        public override void SetSourceUri(string sourceUri)
        {
            this.sourceUri = sourceUri;
        }

        /// <inheritdoc />
        public override string[] GetSourceUris()
        {
            return GetUrisWithMirrors(GetSourceUri(), GetSourceMirrors(), GetSourceReference(), GetSourceType(), InstallationSource.Source);
        }

        /// <inheritdoc />
        public override string[] GetSourceMirrors()
        {
            return sourceMirrors ?? Array.Empty<string>();
        }

        /// <inheritdoc />
        public override void SetSourceMirrors(string[] mirrors)
        {
            sourceMirrors = mirrors;
        }

        /// <inheritdoc />
        public override IDictionary<string, string> GetSuggests()
        {
            return suggests;
        }

        /// <summary>
        /// Sets an array of package suggests.
        /// </summary>
        /// <param name="suggests">An array of package suggest.</param>
        public void SetSuggests(IDictionary<string, string> suggests)
        {
            this.suggests = suggests;
        }

        /// <inheritdoc />
        public override InstallationSource? GetInstallationSource()
        {
            return installationSource;
        }

        /// <inheritdoc />
        public override void SetInstallationSource(InstallationSource? source)
        {
            installationSource = source;
        }

        /// <summary>
        /// Get the specified Uri address, including the mirror uri.
        /// </summary>
        /// <param name="uri">The base uri.</param>
        /// <param name="mirrors">An array of mirrors.</param>
        /// <param name="reference">The source reference.</param>
        /// <param name="type">The type of repository type or distribution archive.</param>
        /// <param name="installationSource">Source from which this package was installed.</param>
        protected string[] GetUrisWithMirrors(string uri, string[] mirrors, string reference, string type, InstallationSource installationSource)
        {
            if (string.IsNullOrEmpty(uri))
            {
                return Array.Empty<string>();
            }

            mirrors = mirrors ?? Array.Empty<string>();

            var uris = new LinkedList<string>();
            uris.AddLast(uri);

            foreach (var mirror in mirrors)
            {
                if (installationSource == InstallationSource.Dist)
                {
                    uris.AddLast(BucketMirror.ProcessUri(mirror, GetName(), GetVersion(), reference, type));
                }
                else
                {
                    uris.AddLast(BucketMirror.ProcessUriGit(mirror, GetName(), uri, type));
                }
            }

            return uris.ToArray();
        }
    }
}
