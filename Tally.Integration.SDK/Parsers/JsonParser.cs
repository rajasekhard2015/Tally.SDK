using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tally.Integration.SDK.Parsers
{
    /// <summary>
    /// Parses JSON input into a dictionary.
    /// </summary>
    public class JsonParser
    {
        /// <summary>
        /// Parses a JSON object string into a dictionary.
        /// </summary>
        /// <param name="json">The JSON string.</param>
        /// <returns>Parsed key-value data.</returns>
        public Dictionary<string, object> Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("JSON input cannot be null or empty.", "json");
            }

            var token = JToken.Parse(json);
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            if (token.Type == JTokenType.Object)
            {
                FlattenToken(token, string.Empty, result);
            }
            else
            {
                result["Value"] = token.ToString();
            }

            return result;
        }

        private static void FlattenToken(JToken token, string path, Dictionary<string, object> output)
        {
            if (token == null)
            {
                return;
            }

            if (token.Type == JTokenType.Object)
            {
                var obj = (JObject)token;
                foreach (var prop in obj.Properties())
                {
                    var nextPath = string.IsNullOrWhiteSpace(path) ? prop.Name : path + "/" + prop.Name;
                    FlattenToken(prop.Value, nextPath, output);
                }

                return;
            }

            if (token.Type == JTokenType.Array)
            {
                var arr = (JArray)token;
                for (var i = 0; i < arr.Count; i++)
                {
                    var nextPath = path + "[" + i + "]";
                    FlattenToken(arr[i], nextPath, output);
                }

                return;
            }

            var value = ((JValue)token).Value;
            output[path] = value;

            var leaf = GetLeaf(path);
            if (!output.ContainsKey(leaf))
            {
                output[leaf] = value;
            }
        }

        private static string GetLeaf(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            var normalized = path.Replace('\\', '/').Replace('.', '/');
            var index = normalized.LastIndexOf('/');
            return index < 0 ? normalized : normalized.Substring(index + 1);
        }
    }
}
