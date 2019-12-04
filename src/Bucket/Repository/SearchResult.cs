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
using System;

namespace Bucket.Repository
{
    /// <summary>
    /// Represents a search result.
    /// </summary>
    public class SearchResult : IEquatable<SearchResult>
    {
        private readonly IPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchResult"/> class.
        /// </summary>
        /// <param name="package">The package instance.</param>
        public SearchResult(IPackage package)
        {
            this.package = package;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchResult"/> class.
        /// </summary>
        public SearchResult(string name)
        {
            package = new PackageComplete(name, "1.0.0.0", "1.0.0");
        }

        /// <summary>
        /// Gets a value indicating whether the package is deprecated.
        /// </summary>
        public virtual bool IsDeprecated
        {
            get
            {
                if (package is IPackageComplete packageComplete)
                {
                    return packageComplete.IsDeprecated;
                }

                return false;
            }
        }

        public static bool operator ==(SearchResult left, SearchResult right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SearchResult left, SearchResult right)
        {
            return !(left == right);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return ReferenceEquals(package, obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return package.GetHashCode();
        }

        /// <inheritdoc />
        public virtual bool Equals(SearchResult other)
        {
            return package.Equals(other);
        }

        /// <summary>
        /// Gets the package instance.
        /// </summary>
        /// <returns>Returns the package instance.</returns>
        public IPackage GetPackage()
        {
            return package;
        }

        /// <summary>
        /// Gets the package name.
        /// </summary>
        /// <remarks>The implementer only needs to return a human readable name.</remarks>
        /// <returns>Returns the package name.</returns>
        public virtual string GetName()
        {
            return package.GetNamePretty();
        }

        /// <summary>
        /// Gets the package description.
        /// </summary>
        /// <returns>Returns null if no package description.</returns>
        public virtual string GetDescription()
        {
            return package is IPackageComplete packageComplete ? packageComplete.GetDescription() : null;
        }

        /// <summary>
        /// Gets a value indicating the replacement package if the package is deprecated.
        /// </summary>
        public virtual string GetReplacementPackage()
        {
            if (package is IPackageComplete packageComplete)
            {
                return packageComplete.GetReplacementPackage();
            }

            return null;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var description = GetDescription();
            if (!string.IsNullOrEmpty(description))
            {
                description = $" {description}";
            }

            return $"{GetName()}{description}";
        }
    }
}
