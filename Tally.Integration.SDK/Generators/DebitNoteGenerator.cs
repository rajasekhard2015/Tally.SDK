namespace Tally.Integration.SDK.Generators
{
    /// <summary>
    /// Generates Tally XML for Debit Note vouchers.
    /// </summary>
    public class DebitNoteGenerator : VoucherGeneratorBase
    {
        /// <summary>
        /// Gets the voucher type.
        /// </summary>
        protected override string VoucherType
        {
            get { return "Debit Note"; }
        }
    }
}
