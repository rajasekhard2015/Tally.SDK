using System.Collections.Generic;

namespace Tally.Integration.SDK.Generators
{
    /// <summary>
    /// Defines XML generation for a Tally voucher.
    /// </summary>
    public interface IVoucherGenerator
    {
        /// <summary>
        /// Generates a complete Tally import envelope.
        /// </summary>
        /// <param name="mappedData">Mapped Tally field data.</param>
        /// <returns>Tally import XML.</returns>
        string Generate(Dictionary<string, object> mappedData);
    }
}
