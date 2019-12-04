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

namespace Bucket.IO
{
    /// <summary>
    /// Indicates that the object has the ability to reset the authorization.
    /// </summary>
    public interface IResetAuthentications
    {
        /// <summary>
        /// Reset all authentications.
        /// </summary>
        void ResetAuthentications();
    }
}
