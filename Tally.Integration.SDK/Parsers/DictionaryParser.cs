using System;
using System.Collections.Generic;

namespace Tally.Integration.SDK.Parsers
{
    /// <summary>
    /// Normalizes dictionary input.
    /// </summary>
    public class DictionaryParser
    {
        /// <summary>
        /// Copies dictionary input into a case-insensitive dictionary.
        /// </summary>
        /// <param name="data">The source dictionary.</param>
        /// <returns>Normalized key-value data.</returns>
        public Dictionary<string, object> Parse(Dictionary<string, object> data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in data)
            {
                result[item.Key] = item.Value;
            }

            return result;
        }
    }
}
