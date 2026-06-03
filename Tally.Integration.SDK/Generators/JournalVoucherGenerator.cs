namespace Tally.Integration.SDK.Generators
{
    /// <summary>
    /// Generates Tally XML for Journal vouchers.
    /// </summary>
    public class JournalVoucherGenerator : VoucherGeneratorBase
    {
        /// <summary>
        /// Gets the voucher type.
        /// </summary>
        protected override string VoucherType
        {
            get { return "Journal"; }
        }
    }
}
