using System;
using System.Collections.Generic;
using System.IO;
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
                object sourceValue;
                if (!sourceData.TryGetValue(pair.Key, out sourceValue))
                {
                    continue;
                }

                output[pair.Value] = TransformationHelper.Transform(pair.Value, sourceValue);
            }

            return output;
        }
    }
}
