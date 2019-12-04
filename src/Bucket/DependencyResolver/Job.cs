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

#pragma warning disable SA1600

using Bucket.Semver.Constraint;
using Bucket.Util;
using System.Text;

namespace Bucket.DependencyResolver
{
    public sealed class Job : System.IEquatable<Job>
    {
        public JobCommand Command { get; set; }

        public string PackageName { get; set; }

        public IConstraint Constraint { get; set; }

        public bool Fixed { get; set; }

        public static bool operator ==(Job left, Job right)
        {
            if (left is null)
            {
                if (right is null)
                {
                    return true;
                }

                return false;
            }

            return left.Equals(right);
        }

        public static bool operator !=(Job left, Job right)
        {
            return !(left == right);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (!(obj is Job other))
            {
                return false;
            }

            return Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return (Command + PackageName + Fixed).GetHashCode();
        }

        /// <inheritdoc />
        public bool Equals(Job other)
        {
            if (other == null)
            {
                return false;
            }

            return other.Command == Command &&
                    other.PackageName == PackageName &&
                    other.Constraint == Constraint &&
                    other.Fixed == Fixed;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var text = new StringBuilder();
            text.Append(Command);

            if (Fixed)
            {
                text.Append("(fixed)");
            }

            text.Append(Str.Space).Append(PackageName).Append(Str.Space);
            text.Append(Constraint?.GetPrettyString());

            return text.ToString();
        }
    }
}
