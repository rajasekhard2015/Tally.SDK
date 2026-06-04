using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Tally.Integration.SDK.Parsers
{
    /// <summary>
    /// Parses POCO objects into a dictionary.
    /// </summary>
    public class ObjectParser
    {
        /// <summary>
        /// Parses a POCO object using public readable properties.
        /// </summary>
        /// <param name="obj">The source object.</param>
        /// <returns>Parsed key-value data.</returns>
        public Dictionary<string, object> Parse(object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            var json = JsonConvert.SerializeObject(obj);
            return new JsonParser().Parse(json);
        }
    }
}
