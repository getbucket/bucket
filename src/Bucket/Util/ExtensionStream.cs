/*
* This file is part of the Bucket package , borrowed from CatLib(catlib.io) and modified.
*
* (c) MouGuangYi <muguangyi@hotmail.com> , Yu Meng Han <menghanyu1994@gmail.com>
*
* For the full copyright and license information, please view the LICENSE
* file that was distributed with this source code.
*
* Document: https://catlib.io
*/

using Bucket.Util;
using GameBox.Console.Exception;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace Bucket
{
    /// <summary>
    /// The stream extension helper.
    /// </summary>
    /// <remarks>This class is extracted from GameBox.Core.</remarks>
    [ExcludeFromCodeCoverage]
    internal static class ExtensionStream
    {
        [ThreadStatic]
        private static byte[] buffer;

        private static byte[] Buffer
        {
            get
            {
                if (buffer == null)
                {
                    buffer = new byte[4096];
                }

                return buffer;
            }
        }

        /// <summary>
        /// Append the current stream to the target stream.
        /// </summary>
        /// <param name="source">The source stream.</param>
        /// <param name="destination">The target stream.</param>
        /// <returns>How much data was transferred in total.</returns>
        public static long AppendTo(this Stream source, Stream destination)
        {
            return source.AppendTo(destination, Buffer);
        }

        /// <summary>
        /// Append the current stream to the target stream..
        /// </summary>
        /// <param name="source">The source stream.</param>
        /// <param name="destination">The target stream.</param>
        /// <param name="buffer">The used buffer.</param>
        /// <returns>How much data was transferred in total.</returns>
        public static long AppendTo(this Stream source, Stream destination, byte[] buffer)
        {
            Guard.Requires<NullReferenceException>(source != null);
            Guard.Requires<NullReferenceException>(destination != null);

            long result = 0;
            int read;
            while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
            {
                destination.Write(buffer, 0, read);
                result += read;
            }

            return result;
        }

        /// <summary>
        /// Conversion stream to a string.
        /// </summary>
        /// <param name="source">The source stream.</param>
        /// <param name="encoding">The string encoding.</param>
        /// <param name="closed">Whether closed the stream.</param>
        /// <returns>The converted string.</returns>
        public static string ToText(this Stream source, Encoding encoding = null, bool closed = true)
        {
            Guard.Requires<ArgumentNullException>(source != null);
            try
            {
                if (!source.CanRead)
                {
                    throw new ConsoleException($"Can not read stream, {nameof(source.CanRead)} == false");
                }

                encoding = encoding ?? Str.Encoding;
                if (source is MemoryStream memoryStream)
                {
                    byte[] innerBuffer;
                    try
                    {
                        innerBuffer = memoryStream.GetBuffer();
                    }
                    catch (UnauthorizedAccessException)
                    {
                        innerBuffer = memoryStream.ToArray();
                    }

                    return encoding.GetString(innerBuffer, 0, (int)memoryStream.Length);
                }

                var length = 0;
                try
                {
                    length = (int)source.Length;
                }
                catch (NotSupportedException)
                {
                    // ignore
                }

                MemoryStream targetStream;
                if (length > 0 && length <= Buffer.Length)
                {
                    targetStream = new MemoryStream(Buffer, 0, Buffer.Length, true, true);
                }
                else
                {
                    targetStream = new MemoryStream(length);
                }

                using (targetStream)
                {
                    var read = source.AppendTo(targetStream);
                    return encoding.GetString(targetStream.GetBuffer(), 0, (int)read);
                }
            }
            finally
            {
                if (closed)
                {
                    source.Dispose();
                }
            }
        }

        /// <summary>
        /// Read a line of files.
        /// </summary>
        /// <param name="source">The source stream.</param>
        /// <param name="encoding">The string encoding.</param>
        /// <param name="closed">Whether closed the stream.</param>
        public static string ReadLine(this Stream source, Encoding encoding = null, bool closed = true)
        {
            Guard.Requires<ArgumentNullException>(source != null);
            try
            {
                if (!source.CanRead)
                {
                    throw new ConsoleException($"Can not read stream, {nameof(source.CanRead)} == false");
                }

                encoding = encoding ?? Str.Encoding;
                using (var reader = new StreamReader(source, encoding))
                {
                    return reader.ReadLine();
                }
            }
            finally
            {
                if (closed)
                {
                    source.Dispose();
                }
            }
        }
    }
}
