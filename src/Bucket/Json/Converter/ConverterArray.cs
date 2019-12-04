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

using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Bucket.Json.Converter
{
    /// <summary>
    /// Convert non-array objects to array objects.
    /// </summary>
    /// <remarks>This converter can only be used for item conversion.</remarks>
    internal class ConverterArray : JsonConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConverterArray"/> class.
        /// </summary>
        public ConverterArray()
           : this(true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConverterArray"/> class.
        /// </summary>
        /// <param name="beautify">Whether is beautify the writer.</param>
        public ConverterArray(bool beautify)
        {
            Beautify = beautify;
        }

        /// <summary>
        /// Gets or sets a value indicating whether is beautify the writer.
        /// </summary>
        public bool Beautify { get; set; }

        /// <inheritdoc />
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string[])
                    || objectType == typeof(int[]);
        }

        /// <inheritdoc />
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            var items = ReadObject(reader);

            if (objectType == typeof(string[]))
            {
                return Array.ConvertAll(items, (item) => item.ToString());
            }

            if (objectType == typeof(int[]))
            {
                return Array.ConvertAll(items, (item) => (int)item);
            }

            return existingValue;
        }

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var items = (Array)value;

            if (Beautify && items.Length == 1)
            {
                writer.WriteValue(items.GetValue(0));
            }
            else
            {
                writer.WriteStartArray();
                foreach (var item in items)
                {
                    writer.WriteValue(item);
                }

                writer.WriteEndArray();
            }
        }

        private object[] ReadObject(JsonReader reader)
        {
            if (reader.TokenType != JsonToken.StartArray)
            {
                return new object[] { reader.Value };
            }

            var collection = new List<object>();

            reader.Read();
            while (reader.TokenType != JsonToken.EndArray)
            {
                collection.Add(reader.Value);
                reader.Read();
            }

            return collection.ToArray();
        }
    }
}
