using System;
using System.Collections.Generic;
using System.Reflection;

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

            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var properties = obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);

            foreach (var property in properties)
            {
                if (!property.CanRead)
                {
                    continue;
                }

                result[property.Name] = property.GetValue(obj, null);
            }

            return result;
        }
    }
}
