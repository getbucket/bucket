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

namespace Bucket.Installer
{
    /// <summary>
    /// The suggestion.
    /// </summary>
#pragma warning disable CA1815
    public struct Suggestion : System.IEquatable<Suggestion>
#pragma warning restore CA1815
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Suggestion"/> struct.
        /// </summary>
        public Suggestion(string source, string target, string reason)
        {
            Source = source;
            Target = target;
            Reason = reason;
        }

        /// <summary>
        /// Gets a value represent source package which made the suggestion.
        /// </summary>
        public string Source { get; private set; }

        /// <summary>
        /// Gets the target package to be suggested.
        /// </summary>
        public string Target { get; private set; }

        /// <summary>
        /// Gets reason the target package to be suggested.
        /// </summary>
        public string Reason { get; private set; }

        public static bool operator ==(Suggestion left, Suggestion right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Suggestion left, Suggestion right)
        {
            return !(left == right);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (obj is Suggestion suggestion)
            {
                return Equals(suggestion);
            }

            return false;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return (Source + Target + Reason).GetHashCode();
        }

        /// <inheritdoc />
        public bool Equals(Suggestion other)
        {
            return Target == other.Target && Source == other.Source && Reason == other.Reason;
        }
    }
}
