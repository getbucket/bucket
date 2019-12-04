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

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Bucket.Util
{
    /// <summary>
    /// Represents a security utilities.
    /// </summary>
    public static class Security
    {
        /// <summary>
        /// Calculate the md5 value of the content, by default the character is lowercase.
        /// </summary>
        /// <param name="content">The content will calculate md5.</param>
        /// <param name="uppercase">Whether is uppercase.</param>
        public static string Md5(string content, bool uppercase = false, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;
            using (var stream = new MemoryStream(encoding.GetBytes(content)))
            {
                return Md5(stream, uppercase);
            }
        }

        /// <summary>
        /// Calculate the md5 value of the content, by default the character is lowercase.
        /// </summary>
        /// <param name="stream">The stream will calculate md5.</param>
        /// <param name="uppercase">Whether is uppercase.</param>
        public static string Md5(Stream stream, bool uppercase = false)
        {
#pragma warning disable CA5351
            using (var md5 = MD5.Create())
#pragma warning restore CA5351
            {
                return ComputeHash(md5, stream, uppercase);
            }
        }

        /// <summary>
        /// Calculate the sha256 value of the content.
        /// </summary>
        /// <param name="content">The content will calculate sha256.</param>
        /// <param name="uppercase">Whether is uppercase.</param>
        /// <param name="encoding">The encoding of the string.</param>
        public static string Sha256(string content, bool uppercase = false, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;
            using (var stream = new MemoryStream(encoding.GetBytes(content)))
            {
                return Sha256(stream, uppercase);
            }
        }

        /// <summary>
        /// Calculate the sha384 value of the content.
        /// </summary>
        /// <param name="stream">The content will calculate sha384.</param>
        /// <param name="uppercase">Whether is uppercase.</param>
        public static string Sha384(Stream stream, bool uppercase = false)
        {
            using (var sha384Managed = new SHA384Managed())
            {
                return ComputeHash(sha384Managed, stream, uppercase);
            }
        }

        /// <summary>
        /// Calculate the sha384 value of the content.
        /// </summary>
        /// <param name="content">The content will calculate sha384.</param>
        /// <param name="uppercase">Whether is uppercase.</param>
        /// <param name="encoding">The encoding of the string.</param>
        public static string Sha384(string content, bool uppercase = false, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;
            using (var stream = new MemoryStream(encoding.GetBytes(content)))
            {
                return Sha384(stream, uppercase);
            }
        }

        /// <summary>
        /// Calculate the sha256 value of the content.
        /// </summary>
        /// <param name="stream">The content will calculate sha256.</param>
        /// <param name="uppercase">Whether is uppercase.</param>
        public static string Sha256(Stream stream, bool uppercase = false)
        {
            using (var sha256Managed = new SHA256Managed())
            {
                return ComputeHash(sha256Managed, stream, uppercase);
            }
        }

        /// <summary>
        /// Calculate the sha1 value of the local file.
        /// </summary>
        /// <param name="path">The local file path.</param>
        /// <param name="uppercase">Whether is uppercase.</param>
        public static string Sha1File(string path, bool uppercase = false)
        {
            using (var stream = File.OpenRead(path))
            {
                return Sha1(stream, uppercase);
            }
        }

        /// <summary>
        /// Calculate the sha1 value of the content.
        /// </summary>
        /// <param name="content">The content will calculate sha1.</param>
        /// <param name="uppercase">Whether is uppercase.</param>
        /// <param name="encoding">The encoding of the string.</param>
        public static string Sha1(string content, bool uppercase = false, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;
            using (var stream = new MemoryStream(encoding.GetBytes(content)))
            {
                return Sha1(stream, uppercase);
            }
        }

        /// <summary>
        /// Calculate the sha1 value of the content.
        /// </summary>
        /// <param name="stream">The content will calculate sha1.</param>
        /// <param name="uppercase">Whether is uppercase.</param>
        public static string Sha1(Stream stream, bool uppercase = false)
        {
#pragma warning disable CA5350
            using (var sha1Managed = new SHA1Managed())
#pragma warning restore CA5350
            {
                return ComputeHash(sha1Managed, stream, uppercase);
            }
        }

        /// <summary>
        /// Verify data signature.
        /// </summary>
        /// <param name="data">The data used to generate the signature previously.</param>
        /// <param name="signatureBase64">Signature for verification with base64.</param>
        /// <param name="publicPem">A string provides public key.</param>
        /// <param name="algo">
        /// A string representation hash digest algorithm for <paramref name="data"/>.
        /// Must recognize at least "MD5", "SHA1", "SHA256", "SHA384", and "SHA512".
        /// </param>
        /// <returns>True if verify signature successful.</returns>
        public static bool VerifySignature(Stream data, string signatureBase64, string publicPem, string algo = "SHA384")
        {
            return VerifySignature(data, Convert.FromBase64String(signatureBase64), publicPem, algo);
        }

        /// <summary>
        /// Verify data signature.
        /// </summary>
        /// <param name="data">The data used to generate the signature previously.</param>
        /// <param name="signature">Signature for verification.</param>
        /// <param name="publicPem">A string provides public key.</param>
        /// <param name="algo">A string representation hash digest algorithm for <paramref name="data"/>.
        /// Must recognize at least "MD5", "SHA1", "SHA256", "SHA384", and "SHA512".
        /// </param>
        /// <returns>True if verify signature successful.</returns>
        public static bool VerifySignature(Stream data, byte[] signature, string publicPem, string algo = "SHA384")
        {
            try
            {
                using (var rsa = GetRSAProviderFromPublicKey(DecodeOpenSSLPublicKey(publicPem)))
                {
                    return rsa.VerifyData(data, signature, new HashAlgorithmName(algo), RSASignaturePadding.Pkcs1);
                }
            }
            catch (CryptographicException)
            {
                return false;
            }
        }

        private static byte[] DecodeOpenSSLPublicKey(string publicKey)
        {
            const string pemPublicHeader = "-----BEGIN PUBLIC KEY-----";
            const string pemPublicFooter = "-----END PUBLIC KEY-----";

            publicKey = publicKey.Trim();
            if (!publicKey.StartsWith(pemPublicHeader, StringComparison.Ordinal) ||
                !publicKey.EndsWith(pemPublicFooter, StringComparison.Ordinal))
            {
                throw new FormatException("Invalid OpenSSL public key.");
            }

            // remove headers/footers, if present.
            var text = new StringBuilder(publicKey);
            text.Replace(pemPublicHeader, string.Empty);
            text.Replace(pemPublicFooter, string.Empty);

            // get string after removing leading/trailing whitespace
            return Convert.FromBase64String(text.ToString().Trim());
        }

        /// <summary>
        /// Create <see cref="RSACryptoServiceProvider"/> from public key.
        /// </summary>
        /// <param name="x509Key">Byte array represents public key.</param>
        private static RSACryptoServiceProvider GetRSAProviderFromPublicKey(byte[] x509Key)
        {
            // encoded OID sequence for  PKCS #1 rsaEncryption szOID_RSA_RSA = "1.2.840.113549.1.1.1"
            var seqOid = new byte[] { 0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01, 0x05, 0x00 };

            bool CompareByteArrays(byte[] x, byte[] y)
            {
                if (x.Length != y.Length)
                {
                    return false;
                }

                var i = 0;
                foreach (var c in x)
                {
                    if (c != y[i])
                    {
                        return false;
                    }

                    i++;
                }

                return true;
            }

            // Set up stream to read the asn.1 encoded SubjectPublicKeyInfo blob
            using (var memory = new MemoryStream(x509Key))
            using (var reader = new BinaryReader(memory))
            {
                try
                {
                    var twoBytes = reader.ReadUInt16();
                    switch (twoBytes)
                    {
                        case 0x8130:
                            // advance 1 byte.
                            reader.ReadByte();
                            break;
                        case 0x8230:
                            // advance 2 bytes.
                            reader.ReadInt16();
                            break;
                        default:
                            return null;
                    }

                    // Make sure Sequence for OID is correct
                    if (!CompareByteArrays(reader.ReadBytes(15), seqOid))
                    {
                        return null;
                    }

                    // data read as little endian order (actual data order for Bit String is 03 81)
                    twoBytes = reader.ReadUInt16();
                    if (twoBytes == 0x8103)
                    {
                        // advance 1 byte.
                        reader.ReadByte();
                    }
                    else if (twoBytes == 0x8203)
                    {
                        // advance 2 bytes.
                        reader.ReadInt16();
                    }
                    else
                    {
                        return null;
                    }

                    // expect null byte next.
                    if (reader.ReadByte() != 0x00)
                    {
                        return null;
                    }

                    // data read as little endian order (actual data order for Sequence is 30 81)
                    twoBytes = reader.ReadUInt16();
                    if (twoBytes == 0x8130)
                    {
                        // advance 1 byte.
                        reader.ReadByte();
                    }
                    else if (twoBytes == 0x8230)
                    {
                        // advance 2 byte.
                        reader.ReadInt16();
                    }
                    else
                    {
                        return null;
                    }

                    // data read as little endian order (actual data order for Integer is 02 81)
                    byte lowbyte = 0x00;
                    byte highbyte = 0x00;
                    twoBytes = reader.ReadUInt16();
                    if (twoBytes == 0x8102)
                    {
                        // read next bytes which is bytes in modulus.
                        lowbyte = reader.ReadByte();
                    }
                    else if (twoBytes == 0x8202)
                    {
                        // advance 2 bytes.
                        highbyte = reader.ReadByte();
                        lowbyte = reader.ReadByte();
                    }
                    else
                    {
                        return null;
                    }

                    // reverse byte order since asn.1 key uses big endian.
                    var modint = new byte[] { lowbyte, highbyte, 0x00, 0x00 };
                    var modsize = BitConverter.ToInt32(modint, 0);

                    var firstByte = reader.ReadByte();
                    reader.BaseStream.Seek(-1, SeekOrigin.Current);

                    if (firstByte == 0x00)
                    {
                        // if first byte (highest order) of modulus is zero, don't include it.
                        // skip this null byte.
                        reader.ReadByte();

                        // reduce modulus buffer size by 1.
                        modsize -= 1;
                    }

                    // read the modulus bytes.
                    var modulus = reader.ReadBytes(modsize);

                    // expect an Integer for the exponent data.
                    if (reader.ReadByte() != 0x02)
                    {
                        return null;
                    }

                    // should only need one byte for actual exponent data (for all useful values).
                    var exponent = reader.ReadBytes(reader.ReadByte());

                    // create RSACryptoServiceProvider instance and initialize with public key
                    var rsa = new RSACryptoServiceProvider();
                    rsa.ImportParameters(new RSAParameters
                    {
                        Modulus = modulus,
                        Exponent = exponent,
                    });
                    return rsa;
                }
#pragma warning disable CA1031
                catch
#pragma warning restore CA1031
                {
                    return null;
                }
            }
        }

        private static string ComputeHash(HashAlgorithm hashAlgorithm, Stream stream, bool uppercase = false)
        {
            var text = new StringBuilder();
            foreach (var computed in hashAlgorithm.ComputeHash(stream))
            {
                text.Append(computed.ToString("x2"));
            }

            if (uppercase)
            {
                return text.ToString().ToUpper();
            }

            return text.ToString();
        }
    }
}
