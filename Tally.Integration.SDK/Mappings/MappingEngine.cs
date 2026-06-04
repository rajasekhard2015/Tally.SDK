using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Tally.Integration.SDK.Models;
using Tally.Integration.SDK.Utilities;

namespace Tally.Integration.SDK.Mappings
{
    /// <summary>
    /// Maps source fields into Tally fields based on JSON mapping configuration.
    /// </summary>
    public class MappingEngine
    {
        /// <summary>
        /// Loads a mapping definition from a JSON file.
        /// </summary>
        /// <param name="mappingFile">The path to the mapping file.</param>
        /// <returns>Mapping configuration.</returns>
        public MappingDefinition LoadMapping(string mappingFile)
        {
            if (string.IsNullOrWhiteSpace(mappingFile))
            {
                throw new ArgumentException("Mapping file path cannot be empty.", "mappingFile");
            }

            if (!File.Exists(mappingFile))
            {
                throw new FileNotFoundException("Mapping file not found.", mappingFile);
            }

            var json = File.ReadAllText(mappingFile);
            var definition = JsonConvert.DeserializeObject<MappingDefinition>(json);
            if (definition == null)
            {
                throw new InvalidOperationException("Invalid mapping file content.");
            }

            if (definition.Mappings == null)
            {
                definition.Mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                definition.Mappings = new Dictionary<string, string>(definition.Mappings, StringComparer.OrdinalIgnoreCase);
            }

            if (definition.CompanyTemplates == null)
            {
                definition.CompanyTemplates = new Dictionary<string, CompanyTemplateProfile>(StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                definition.CompanyTemplates = new Dictionary<string, CompanyTemplateProfile>(definition.CompanyTemplates, StringComparer.OrdinalIgnoreCase);
            }

            return definition;
        }

        /// <summary>
        /// Maps input values from source fields to Tally fields.
        /// </summary>
        /// <param name="sourceData">Input source data.</param>
        /// <param name="mapping">Mapping definition.</param>
        /// <returns>Mapped data using Tally field names.</returns>
        public Dictionary<string, object> Map(Dictionary<string, object> sourceData, MappingDefinition mapping)
        {
            if (sourceData == null)
            {
                throw new ArgumentNullException("sourceData");
            }

            if (mapping == null)
            {
                throw new ArgumentNullException("mapping");
            }

            var output = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            foreach (var pair in mapping.Mappings)
            {
                if (pair.Key.IndexOf("[*]", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    MapWildcardPair(sourceData, output, pair.Key, pair.Value);
                    continue;
                }

                object sourceValue;
                if (!TryResolveSourceValue(sourceData, pair.Key, out sourceValue))
                {
                    continue;
                }

                output[pair.Value] = TransformationHelper.Transform(pair.Value, sourceValue);
            }

            return output;
        }

        private static void MapWildcardPair(
            Dictionary<string, object> sourceData,
            Dictionary<string, object> output,
            string sourcePattern,
            string targetPattern)
        {
            var matches = ResolveWildcardSourceValues(sourceData, sourcePattern);
            if (matches.Count == 0)
            {
                return;
            }

            foreach (var match in matches)
            {
                var targetField = ApplyIndexToTarget(targetPattern, match.Index);
                output[targetField] = TransformationHelper.Transform(targetField, match.Value);
            }
        }

        private static List<WildcardSourceValue> ResolveWildcardSourceValues(Dictionary<string, object> sourceData, string sourcePattern)
        {
            var list = new List<WildcardSourceValue>();
            var canonicalPattern = CanonicalizeKey(sourcePattern);
            var regexPattern = BuildWildcardRegex(canonicalPattern);
            var regex = new Regex(regexPattern, RegexOptions.IgnoreCase);

            foreach (var key in sourceData.Keys)
            {
                var canonicalKey = CanonicalizeKey(key);
                var match = regex.Match(canonicalKey);
                if (!match.Success)
                {
                    continue;
                }

                var idxGroup = match.Groups["idx"];
                var idx = 0;
                if (idxGroup != null && idxGroup.Success)
                {
                    int parsed;
                    if (int.TryParse(idxGroup.Value, out parsed) && parsed >= 0)
                    {
                        idx = parsed;
                    }
                }

                list.Add(new WildcardSourceValue
                {
                    SourceKey = key,
                    Index = idx,
                    Value = sourceData[key]
                });
            }

            return list
                .OrderBy(x => x.Index)
                .ThenBy(x => x.SourceKey, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string BuildWildcardRegex(string canonicalPattern)
        {
            var escaped = Regex.Escape(canonicalPattern);
            escaped = escaped.Replace("\\[\\*\\]", "\\[(?<idx>[0-9]+)\\]");
            return "^" + escaped + "$";
        }

        private static string ApplyIndexToTarget(string targetPattern, int index)
        {
            if (string.IsNullOrWhiteSpace(targetPattern))
            {
                return targetPattern;
            }

            return targetPattern.Replace("[*]", "[" + index.ToString() + "]")
                .Replace("{index}", index.ToString());
        }

        private sealed class WildcardSourceValue
        {
            public string SourceKey { get; set; }

            public int Index { get; set; }

            public object Value { get; set; }
        }

        private static bool TryResolveSourceValue(Dictionary<string, object> sourceData, string sourceKey, out object sourceValue)
        {
            if (sourceData.TryGetValue(sourceKey, out sourceValue))
            {
                return true;
            }

            var canonicalSource = CanonicalizeKey(sourceKey);
            foreach (var key in sourceData.Keys)
            {
                if (CanonicalizeKey(key).Equals(canonicalSource, StringComparison.OrdinalIgnoreCase))
                {
                    sourceValue = sourceData[key];
                    return true;
                }
            }

            var wantedLeaf = GetLeaf(sourceKey);
            foreach (var key in sourceData.Keys)
            {
                if (GetLeaf(key).Equals(wantedLeaf, StringComparison.OrdinalIgnoreCase))
                {
                    sourceValue = sourceData[key];
                    return true;
                }
            }

            sourceValue = null;
            return false;
        }

        private static string CanonicalizeKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            var k = key.Trim();
            k = Regex.Replace(k, "\\[[0-9]+\\]", string.Empty);
            k = k.Replace('\\', '/').Replace('.', '/').Replace('-', '/').Replace(' ', '/');
            while (k.Contains("//"))
            {
                k = k.Replace("//", "/");
            }

            return k.ToLowerInvariant();
        }

        private static string GetLeaf(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            var normalized = key.Replace('\\', '/').Replace('.', '/');
            var parts = normalized.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 0 ? normalized : parts[parts.Length - 1];
        }
    }
}
