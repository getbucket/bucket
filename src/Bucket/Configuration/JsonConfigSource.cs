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
using Bucket.Json;
using Bucket.Package;
using Bucket.Util;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Bucket.Configuration
{
    /// <summary>
    /// Represents a Json configuration source file.
    /// </summary>
    public sealed class JsonConfigSource : IConfigSource
    {
        private readonly JsonFile file;
        private readonly bool isAuthFile;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonConfigSource"/> class.
        /// </summary>
        /// <param name="file">The json file instance.</param>
        /// <param name="isAuthFile">Whether is auth file.</param>
        public JsonConfigSource(JsonFile file, bool isAuthFile = false)
        {
            this.file = file;
            this.isAuthFile = isAuthFile;
        }

        /// <inheritdoc />
        public bool AddConfigSetting(string name, object value)
        {
            // todo: need refactoring.
            return Manipulate((config) =>
            {
                JObject additional;
                if (!Regex.IsMatch(name, $"^({string.Join("|", Settings.GetSecretKeys())})\\."))
                {
                    additional = new JObject()
                    {
                        ["config"] = new JObject()
                        {
                            [name] = JToken.FromObject(value),
                        },
                    };

                    config.Merge(additional);
                    return;
                }

                var segment = name.Split(new[] { '.' }, 2);
                Guard.Requires<UnexpectedException>(segment.Length == 2, $"Failed to parse configuration depth settings: {name}");

                var key = segment[0];
                var host = segment[1];

                if (isAuthFile)
                {
                    additional = new JObject()
                    {
                        [key] = new JObject()
                        {
                            [host] = JToken.FromObject(value),
                        },
                    };
                }
                else
                {
                    additional = new JObject()
                    {
                        ["config"] = new JObject()
                        {
                            [key] = new JObject()
                            {
                                [host] = JToken.FromObject(value),
                            },
                        },
                    };
                }

                config.Merge(additional);
            });
        }

        /// <inheritdoc />
        public bool RemoveConfigSetting(string name)
        {
            // todo: need refactoring.
            return Manipulate((config) =>
            {
                JObject configProperty = null;
                try
                {
                    if (!Regex.IsMatch(name, $"^({string.Join("|", Settings.GetSecretKeys())})\\."))
                    {
                        configProperty = (JObject)config["config"];
                        configProperty.Remove(name);
                        return;
                    }

                    var segment = name.Split(new[] { '.' }, 2);
                    Guard.Requires<UnexpectedException>(segment.Length == 2, $"Failed to parse configuration depth settings: {name}");

                    var key = segment[0];
                    var host = segment[1];

                    void RemoveConfig(JObject collection)
                    {
                        if (!collection.TryGetValue(key, out JToken token))
                        {
                            return;
                        }

                        var keyProperty = (JObject)token;
                        keyProperty.Remove(host);
                        if (keyProperty.Count <= 0)
                        {
                            collection.Remove(key);
                        }
                    }

                    if (isAuthFile)
                    {
                        RemoveConfig(config);
                    }
                    else
                    {
                        if (!config.TryGetValue("config", out JToken token))
                        {
                            return;
                        }

                        configProperty = (JObject)token;
                        RemoveConfig(configProperty);
                    }
                }
                finally
                {
                    if (configProperty != null && configProperty.Count <= 0)
                    {
                        config.Remove("config");
                    }
                }
            });
        }

        /// <inheritdoc />
        public bool AddLink(LinkType type, string name, string constraint, bool sortPackages = false)
        {
            // todo: sort the packages
            var link = Str.LowerDashes(type.ToString());
            return Manipulate((config) =>
            {
                if (!config.TryGetValue(link, out JToken collection))
                {
                    config[link] = collection = new JObject();
                }

                collection[name] = constraint;
            });
        }

        /// <inheritdoc />
        public bool RemoveLink(LinkType type, string name)
        {
            var link = Str.LowerDashes(type.ToString());
            return Manipulate((config) =>
            {
                if (!config.TryGetValue(link, out JToken token))
                {
                    return;
                }

                var collection = (JObject)token;
                collection.Remove(name);

                if (collection.Count <= 0)
                {
                    config.Remove(link);
                }
            });
        }

        /// <inheritdoc />
        public bool AddProperty(string name, object value)
        {
            return Manipulate((config) =>
            {
                config[name] = JToken.FromObject(value);
            });
        }

        /// <inheritdoc />
        public bool AddRepository(ConfigRepository configRepository)
        {
            return Manipulate((config) =>
            {
                if (!config.TryGetValue("repositories", out JToken token))
                {
                    config["repositories"] = token = new JArray();
                }

                var repositories = (JArray)token;
                var oldRepository = repositories.Select((repo, index) => new { index, repo })
                          .FirstOrDefault((item) => item.repo["name"]?.Value<string>() == configRepository.Name);
                var addedRepository = JObject.FromObject(configRepository);

                if (oldRepository == null)
                {
                    repositories.Add(addedRepository);
                }
                else
                {
                    repositories[oldRepository.index] = addedRepository;
                }
            });
        }

        /// <inheritdoc />
        public string GetPrettyName()
        {
            return file.GetPath();
        }

        /// <inheritdoc />
        public bool RemoveProperty(string name)
        {
            return Manipulate((config) =>
            {
                config.Remove(name);
            });
        }

        /// <inheritdoc />
        public bool RemoveRepository(string name)
        {
            return Manipulate((config) =>
            {
                if (!config.TryGetValue("repositories", out JToken token))
                {
                    return;
                }

                var repositories = (JArray)token;
                var oldRepository = repositories.Select((repo) => repo)
                          .FirstOrDefault((repo) => repo["name"]?.Value<string>() == name);

                if (oldRepository == null)
                {
                    return;
                }

                repositories.Remove(oldRepository);

                if (repositories.Count <= 0)
                {
                    config.Remove("repositories");
                }
            });
        }

        private bool Manipulate(Action<JObject> closure)
        {
            var config = file.Exists() ? file.Read() : new JObject();
            closure(config);
            file.Write(config);
            return true;
        }
    }
}
