using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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

            FlattenElement(document.Root, document.Root.Name.LocalName, result);

            return result;
        }

        private static void FlattenElement(XElement element, string currentPath, Dictionary<string, object> output)
        {
            if (!element.HasElements)
            {
                AddAliases(output, currentPath, element.Name.LocalName, element.Value);
                return;
            }

            var groups = element.Elements()
                .GroupBy(x => x.Name.LocalName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            foreach (var group in groups)
            {
                var siblings = group.Value;
                for (var i = 0; i < siblings.Count; i++)
                {
                    var child = siblings[i];
                    var useIndex = siblings.Count > 1;
                    var segment = useIndex ? child.Name.LocalName + "[" + i + "]" : child.Name.LocalName;
                    var nextPath = currentPath + "/" + segment;
                    FlattenElement(child, nextPath, output);
                }
            }
        }

        private static void AddAliases(Dictionary<string, object> output, string fullPath, string leafName, string value)
        {
            output[fullPath] = value;

            var withoutRoot = RemoveRootSegment(fullPath);
            if (!string.IsNullOrWhiteSpace(withoutRoot))
            {
                output[withoutRoot] = value;
            }

            var noIndexPath = RemoveIndexes(fullPath);
            if (!string.IsNullOrWhiteSpace(noIndexPath))
            {
                output[noIndexPath] = value;
            }

            if (!output.ContainsKey(leafName))
            {
                output[leafName] = value;
            }
        }

        private static string RemoveRootSegment(string path)
        {
            var index = path.IndexOf('/');
            return index < 0 ? path : path.Substring(index + 1);
        }

        private static string RemoveIndexes(string path)
        {
            return Regex.Replace(path, "\\[[0-9]+\\]", string.Empty);
        }
    }
}
