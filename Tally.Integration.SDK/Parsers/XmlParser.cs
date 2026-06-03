using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Tally.Integration.SDK.Parsers
{
    /// <summary>
    /// Parses XML input into a dictionary.
    /// </summary>
    public class XmlParser
    {
        /// <summary>
        /// Parses an XML string into a flattened dictionary of leaf element values.
        /// </summary>
        /// <param name="xml">The XML string.</param>
        /// <returns>Parsed key-value data.</returns>
        public Dictionary<string, object> Parse(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
            {
                throw new ArgumentException("XML input cannot be null or empty.", "xml");
            }

            var document = XDocument.Parse(xml);
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            if (document.Root == null)
            {
                return result;
            }

            foreach (var element in document.Root.Descendants())
            {
                if (element.HasElements)
                {
                    continue;
                }

                var key = element.Name.LocalName;
                if (!result.ContainsKey(key))
                {
                    result.Add(key, element.Value);
                }
                else
                {
                    result[key] = element.Value;
                }
            }

            return result;
        }
    }
}
