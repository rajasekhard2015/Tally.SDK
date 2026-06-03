using System.Collections.Generic;

namespace Tally.Integration.SDK.Models
{
    /// <summary>
    /// Represents a JSON mapping configuration for a voucher type.
    /// </summary>
    public class MappingDefinition
    {
        /// <summary>
        /// Gets or sets the voucher type.
        /// </summary>
        public string VoucherType { get; set; }

        /// <summary>
        /// Gets or sets source-to-target field mappings.
        /// </summary>
        public Dictionary<string, string> Mappings { get; set; }
    }
}
