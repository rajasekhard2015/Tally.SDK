namespace Tally.Integration.SDK.Generators
{
    /// <summary>
    /// Generates Tally XML for Credit Note vouchers.
    /// </summary>
    public class CreditNoteGenerator : VoucherGeneratorBase
    {
        /// <summary>
        /// Gets the voucher type.
        /// </summary>
        protected override string VoucherType
        {
            get { return "Credit Note"; }
        }
    }
}
