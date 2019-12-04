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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Bucket.Tests
{
    public static class AssertExtension
    {
        public static System.Exception ThrowsException(this Assert assert, Type expected, Action action)
        {
            try
            {
                action();
            }
            catch (System.Exception ex)
            {
                if (!expected.Equals(ex.GetType()))
                {
                    throw;
                }

                return ex;
            }

            return null;
        }
    }
}
