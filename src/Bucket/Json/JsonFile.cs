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

using Bucket.Assets;
using Bucket.Exception;
using Bucket.FileSystem;
using Bucket.IO;
using GameBox.Console.Exception;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Bucket.Json
{
    /// <summary>
    /// Reads/writes json files.
    /// </summary>
    public class JsonFile
    {
        private const string BucketSchemaPath = "Schema/bucket-schema.json";
        private readonly string path;
        private readonly IFileSystem fileSystem;
        private readonly IIO io;
        private string cache;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonFile"/> class.
        /// </summary>
        /// <param name="path">The path of the json file.</param>
        /// <param name="fileSystem">The file system for reading json files.</param>
        /// <param name="io">The input/output instance.</param>
        public JsonFile(string path, IFileSystem fileSystem = null, IIO io = null)
        {
            this.path = path;

            if (fileSystem == null && Regex.IsMatch(path, "^https?://", RegexOptions.IgnoreCase))
            {
                throw new InvalidArgumentException($"http urls require a {nameof(IFileSystem)} instance to be passed");
            }

            this.fileSystem = fileSystem ?? new FileSystemLocal();
            this.io = io ?? IONull.That;
        }

        /// <summary>
        /// Gets or sets encoding of the file.
        /// </summary>
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        /// <summary>
        /// Parse the json the specified object.
        /// </summary>
        /// <typeparam name="T">Type of the specified object.</typeparam>
        public static T Parse<T>(string content)
        {
            return JsonConvert.DeserializeObject<T>(content);
        }

        /// <summary>
        /// Parse the json the JObject.
        /// </summary>
        public static JObject Parse(string content)
        {
            return JObject.Parse(content);
        }

        /// <summary>
        /// Gets the path of the file.
        /// </summary>
        /// <returns>Returns the path of the file.</returns>
        public virtual string GetPath()
        {
            if (fileSystem is IReportPath report)
            {
                return report.ApplyRootPath(path);
            }

            return path;
        }

        /// <summary>
        /// Checks whether json file exists.
        /// </summary>
        /// <returns>True if the local json file exists.</returns>
        public virtual bool Exists()
        {
            return fileSystem.Exists(path);
        }

        /// <summary>
        /// Delete the json file.
        /// </summary>
        public virtual void Delete()
        {
            fileSystem.Delete(path);
        }

        /// <summary>
        /// Reads json file.
        /// </summary>
        /// <returns>The deserialized anonymous type from the JSON string.</returns>
        public virtual JObject Read()
        {
            if (string.IsNullOrEmpty(cache))
            {
                io.WriteError($"JsonFile reading: {path}", true, Verbosities.Debug);
                cache = fileSystem.Read(path).ToText(Encoding);
            }

            return JObject.Parse(cache);
        }

        /// <summary>
        /// Reads json file.
        /// </summary>
        /// <typeparam name="T">The anonymous type to deserialize to.</typeparam>
        /// <returns>The deserialized anonymous type from the JSON string.</returns>
        public virtual T Read<T>()
        {
            return Read().ToObject<T>();
        }

        /// <summary>
        /// Write json file.
        /// </summary>
        public virtual void Write(object content)
        {
            if (!(content is JObject data))
            {
                data = JObject.FromObject(content);
            }

            io.WriteError($"JsonFile writing: {path}", true, Verbosities.Debug);

            using (StringWriter streamWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                using (JsonTextWriter writer = new JsonTextWriter(streamWriter)
                {
                    Formatting = Formatting.Indented,
                    Indentation = 4,
                })
                {
                    data.WriteTo(writer);
                    cache = streamWriter.ToString();
                    using (var memoryStream = new MemoryStream(Encoding.GetBytes(cache)))
                    {
                        fileSystem.Write(path, memoryStream);
                    }
                }
            }
        }

        /// <summary>
        /// Verify the Json is valid if valid faild throw exception.
        /// </summary>
        /// <param name="strict">Whether validate is strict.</param>
        /// <param name="schemaJson">The json schema content.</param>
        public virtual void Validate(bool strict = true, string schemaJson = null)
        {
            if (IsValidate(out IList<string> errorMessages, strict, schemaJson))
            {
                return;
            }

            var message = new StringBuilder();
            foreach (var error in errorMessages)
            {
                message.AppendLine($"  - {error}");
            }

            throw new RuntimeException($"Bucket.json file validation failed:{Environment.NewLine}{message.ToString()}");
        }

        /// <summary>
        /// Verify the Json is valid.
        /// </summary>
        /// <param name="errorMessages">A list of messages that failed validation.</param>
        /// <param name="strict">Whether validate is strict.</param>
        /// <param name="schemaJson">The json schema content.</param>
        /// <returns>True if the json is valid.</returns>
        public virtual bool IsValidate(out IList<string> errorMessages, bool strict = true, string schemaJson = null)
        {
            if (string.IsNullOrEmpty(schemaJson))
            {
                schemaJson = Resources.GetString(BucketSchemaPath);
            }

            var schema = JSchema.Parse(schemaJson);

            if (!strict)
            {
                schema.AllowAdditionalProperties = true;
                schema.Required.Clear();
            }

            try
            {
                var jsonObject = Read();
                return jsonObject.IsValid(schema, out errorMessages);
            }
            catch (JsonReaderException ex)
            {
                errorMessages = new List<string>
                {
                    ex.Message,
                };

                return false;
            }
        }
    }
}
