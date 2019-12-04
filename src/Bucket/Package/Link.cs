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

using Bucket.Exception;
using Bucket.Semver.Constraint;
using Bucket.Util;
using System.Text;

#pragma warning disable CA1822

namespace Bucket.Package
{
    /// <summary>
    /// Represents a link between two packages, represented by their names.
    /// </summary>
    public class Link
    {
        private readonly string source;
        private readonly string description;
        private readonly string target;
        private readonly IConstraint constraint;
        private readonly string prettyConstraint;

        /// <summary>
        /// Initializes a new instance of the <see cref="Link"/> class.
        /// </summary>
        /// <param name="source">The source package name.</param>
        /// <param name="target">The target package name.</param>
        /// <param name="constraint">The target constraint.</param>
        /// <param name="description">The description with the link.</param>
        /// <param name="prettyConstraint">The pretty constraint description to display.</param>
        public Link(string source, string target, IConstraint constraint, string description = null, string prettyConstraint = null)
        {
            // Package names are all lowercase operations.
            this.source = source.ToLower();
            this.target = target.ToLower();
            this.description = description ?? "relates to";
            this.constraint = constraint;
            this.prettyConstraint = prettyConstraint;
        }

        /// <summary>
        /// Gets source package name.
        /// </summary>
        /// <returns>Returns the source package name.</returns>
        public virtual string GetSource()
        {
            return source;
        }

        /// <summary>
        /// Gets target package name.
        /// </summary>
        /// <returns>Returns target package name.</returns>
        public virtual string GetTarget()
        {
            return target;
        }

        /// <summary>
        /// Gets the description with link.
        /// </summary>
        /// <returns>Returns the description with link.</returns>
        public virtual string GetDescription()
        {
            return description;
        }

        /// <summary>
        /// Gets the version constraint applying to the target of this link.
        /// </summary>
        /// <returns>Returns the version contraint.</returns>
        public virtual IConstraint GetConstraint()
        {
            return constraint;
        }

        /// <summary>
        /// Gets the pretty constraint description string.
        /// </summary>
        /// <returns>Returns the pretty constraint description string.</returns>
        public virtual string GetPrettyConstraint()
        {
            if (string.IsNullOrEmpty(prettyConstraint))
            {
                throw new UnexpectedException($"Link {ToString()} has been misconfigured and had no {nameof(prettyConstraint)} given.");
            }

            return prettyConstraint;
        }

        /// <summary>
        /// Gets the pretty string to display.
        /// </summary>
        /// <param name="sourcePackage">The source package instance.</param>
        /// <returns>Returns the pretty string.</returns>
        public virtual string GetPrettyString(IPackage sourcePackage)
        {
            var result = new StringBuilder();
            result.Append(sourcePackage?.GetPrettyString() ?? GetSource()).Append(Str.Space);
            result.Append(GetDescription()).Append(Str.Space);
            result.Append(GetTarget()).Append(Str.Space);
            result.Append(GetConstraint().GetPrettyString());
            return result.ToString();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return GetPrettyString(null);
        }
    }
}
