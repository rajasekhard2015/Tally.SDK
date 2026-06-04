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

        /// <summary>
        /// Gets or sets company-specific template profiles.
        /// Key is company name; value contains defaults and required target fields.
        /// </summary>
        public Dictionary<string, CompanyTemplateProfile> CompanyTemplates { get; set; }
    }

    /// <summary>
    /// Represents company-level template rules for a voucher mapping.
    /// </summary>
    public class CompanyTemplateProfile
    {
        /// <summary>
        /// Gets or sets fixed default values to inject into mapped target fields when missing.
        /// </summary>
        public Dictionary<string, string> Defaults { get; set; }

        /// <summary>
        /// Gets or sets required mapped target fields for this company template.
        /// Supports exact targets and wildcard forms such as ALLLEDGERENTRIES.LIST[*]/AMOUNT.
        /// </summary>
        public List<string> RequiredTargets { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether required target checks should fail import.
        /// </summary>
        public bool StrictMode { get; set; } = true;
    }
}
