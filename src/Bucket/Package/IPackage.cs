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

using Bucket.Repository;
using Bucket.Semver;
using System;
using System.Collections.Generic;

namespace Bucket.Package
{
    /// <summary>
    /// Defines the essential information a package has that is used during solving/installation.
    /// </summary>
    public interface IPackage : IEquatable<IPackage>, ICloneable
    {
        /// <summary>
        /// Gets a value indicating whether the package is a development virtual package or a concrete one.
        /// </summary>
        bool IsDev { get; }

        /// <summary>
        /// Gets or sets the solver to set an id for this package to refer to it.
        /// </summary>
        int Id { get; set; }

        /// <summary>
        /// Gets the package's name without version info, thus not a unique identifier.
        /// </summary>
        /// <remarks>This name will be composed of lowercase.</remarks>
        /// <returns>Returns the package's name without version info, thus not a unique identifier.</returns>
        string GetName();

        /// <summary>
        /// Returns a set of names that could refer to this package.
        /// No version or release type information should be included in any of the
        /// names. Provided or replaced package names need to be returned as well.
        /// </summary>
        /// <returns>An array of strings referring to this package.</returns>
        string[] GetNames();

        /// <summary>
        /// Gets package unique name, constructed from name and version.
        /// </summary>
        /// <returns>Returns package unique name, constructed from name and version.</returns>
        string GetNameUnique();

        /// <summary>
        /// Gets the package's pretty (i.e. with proper case) name.
        /// </summary>
        /// <returns>Returns the package's pretty name.</returns>
        string GetNamePretty();

        /// <summary>
        /// Gets the package type.
        /// </summary>
        /// <returns>Return the package type.</returns>
        string GetPackageType();

        /// <summary>
        /// Gets source from which this package was installed.
        /// </summary>
        /// <returns>Returns source from which this package was installed.</returns>
        InstallationSource? GetInstallationSource();

        /// <summary>
        /// Sets source from which this package was installed.
        /// </summary>
        /// <param name="source">The source from which this package was installed.</param>
        void SetInstallationSource(InstallationSource? source);

        /// <summary>
        /// Gets the repository type of this package, e.g. git, svn.
        /// </summary>
        /// <returns>Returns the repository type of this package. null if not set.</returns>
        string GetSourceType();

        /// <summary>
        /// Sets the repository type of this package.
        /// </summary>
        /// <param name="sourceType">The repository type of this package.</param>
        void SetSourceType(string sourceType);

        /// <summary>
        /// Gets the repository uri of this package, e.g. git://github.com/foo/foo.git.
        /// </summary>
        /// <returns>Returns the repository uri of this package.</returns>
        /// <remarks>Don't use <see cref="Uri"/>, maybe ssh.</remarks>
        string GetSourceUri();

        /// <summary>
        /// Sets the repository uri of this package.
        /// </summary>
        /// <param name="sourceUri">The repository uri of this package.</param>
        void SetSourceUri(string sourceUri);

        /// <summary>
        /// Gets the repository uris of this package including mirrors.
        /// </summary>
        string[] GetSourceUris();

        /// <summary>
        /// Gets the repository reference of this package, e.g. branch or a commit hash for git.
        /// </summary>
        /// <remarks>If there is no value, implement need to return null.</remarks>
        string GetSourceReference();

        /// <summary>
        /// Sets the repository reference of this package.
        /// </summary>
        /// <param name="sourceReference">The source reference.</param>
        void SetSourceReference(string sourceReference);

        /// <summary>
        /// Gets an array of the source mirrors.
        /// </summary>
        string[] GetSourceMirrors();

        /// <summary>
        /// Sets the source mirrors.
        /// </summary>
        /// <param name="mirrors">An array of mirrors.</param>
        void SetSourceMirrors(string[] mirrors);

        /// <summary>
        /// Gets the type of the distribution archive of this version, e.g. zip, tar.
        /// </summary>
        /// <returns>Return the type of distribution archive of this version.</returns>
        string GetDistType();

        /// <summary>
        /// Sets the repository type of this package with dist mode.
        /// </summary>
        /// <param name="distType">The dist repository type of this package.</param>
        void SetDistType(string distType);

        /// <summary>
        /// Gets the uri of the distribution archive of this version.
        /// </summary>
        /// <returns>Returns the uri of the distribution archive of this version.</returns>
        string GetDistUri();

        /// <summary>
        /// Sets the uri of the distribution archive of this version.
        /// </summary>
        /// <param name="distUri">The uri of the distribution archive.</param>
        void SetDistUri(string distUri);

        /// <summary>
        /// Gets the repository uris of distribution archive including mirrors.
        /// </summary>
        string[] GetDistUris();

        /// <summary>
        /// Gets the reference of the distribution archive of this version, e.g. branch or a commit hash for git.
        /// </summary>
        /// <returns>Returns the reference of the distribution archive of this version.</returns>
        string GetDistReference();

        /// <summary>
        /// Sets the repository reference of this package with dist mode.
        /// </summary>
        /// <param name="distReference">The dist reference.</param>
        void SetDistReference(string distReference);

        /// <summary>
        /// Gets the shasum for the distribution archive of this version.
        /// </summary>
        string GetDistShasum();

        /// <summary>
        /// Sets the shasum for the distribution archive of this version.
        /// </summary>
        /// <param name="distShasum">The shasum for the distribution.</param>
        void SetDistShasum(string distShasum);

        /// <summary>
        /// Gets an array of the dist mirrors.
        /// </summary>
        string[] GetDistMirrors();

        /// <summary>
        /// Sets the dist mirrors.
        /// </summary>
        /// <param name="mirrors">An array of mirrors.</param>
        void SetDistMirrors(string[] mirrors);

        /// <summary>
        /// Gets the version of this package.
        /// </summary>
        /// <returns>Returns the version of this package.</returns>
        string GetVersion();

        /// <summary>
        /// Gets the pretty (i.e. non-normalized) version string of this package.
        /// </summary>
        /// <returns>Returns the pretty version of this package.</returns>
        string GetVersionPretty();

        /// <summary>
        /// Gets the pretty version string plus a git or hg commit hash of this package.
        /// </summary>
        /// <param name="truncate">If the source reference is a sha1 hash, truncate it.</param>
        /// <returns>Return the pretty version string plus a git or hg commit hash of this package.</returns>
        string GetVersionPrettyFull(bool truncate = true);

        /// <summary>
        /// Gets the release date of the package.
        /// </summary>
        /// <returns>Return the release date of the package.</returns>
        DateTime? GetReleaseDate();

        /// <summary>
        /// Gets the stability of this package.
        /// </summary>
        /// <returns>Returns the stability of this package.</returns>
        Stabilities GetStability();

        /// <summary>
        /// Returns a set of links to packages which need to be installed before
        /// this package can be installed.
        /// </summary>
        /// <returns>An array of package links defining required packages.</returns>
        Link[] GetRequires();

        /// <summary>
        /// Returns a set of links to packages which must not be installed at the
        /// same time as this package.
        /// </summary>
        /// <returns>An array of package links defining conflicting packages.</returns>
        Link[] GetConflicts();

        /// <summary>
        /// Returns a set of links to packages which can alternatively be
        /// satisfied by installing this package.
        /// </summary>
        /// <returns>An array of package links defining replaced packages.</returns>
        Link[] GetReplaces();

        /// <summary>
        /// Returns a set of links to packages which are required to develop
        /// this package. These are installed if in dev mode.
        /// </summary>
        /// <returns>An array of package links defining packages required for development.</returns>
        Link[] GetRequiresDev();

        /// <summary>
        /// Returns a set of links to virtual packages that are provided through
        /// this package.
        /// </summary>
        /// <remarks>For example, the package provides a specified abstract interface.</remarks>
        /// <returns>An array of package links defining provided packages.</returns>
        Link[] GetProvides();

        /// <summary>
        /// Returns a set of package names and reasons why they are useful in
        /// combination with this package.
        /// </summary>
        IDictionary<string, string> GetSuggests();

        /// <summary>
        /// Sets a reference to the repository that owns the package.
        /// </summary>
        /// <param name="repository">The repository that owns the package.</param>
        void SetRepository(IRepository repository);

        /// <summary>
        /// Gets a reference to the repository that owns the package.
        /// </summary>
        /// <returns>Returns a reference to the repository that owns the package.</returns>
        IRepository GetRepository();

        /// <summary>
        /// Gets the package binaries.
        /// </summary>
        /// <returns>An array of the package binaries.</returns>
        string[] GetBinaries();

        /// <summary>
        /// Gets the package notification uri.
        /// </summary>
        /// <returns>Return the package notification uri.</returns>
        string GetNotificationUri();

        /// <summary>
        /// Gets an array of patterns from package archives.
        /// </summary>
        /// <returns>Returns an array of patterns from package archives.</returns>
        string[] GetArchives();

        /// <summary>
        /// Gets the package extra data.
        /// </summary>
        dynamic GetExtra();

        /// <summary>
        /// Gets the pretty string to display.
        /// </summary>
        /// <returns>Returns the pretty string.</returns>
        string GetPrettyString();
    }
}
