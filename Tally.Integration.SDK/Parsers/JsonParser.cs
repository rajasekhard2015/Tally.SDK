using System;
using System.Collections.Generic;
using Newtonsoft.Json;

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

            var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            return data ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
