namespace Tally.Integration.SDK.Generators
{
    /// <summary>
    /// Generates Tally XML for Receipt vouchers.
    /// </summary>
    public class ReceiptVoucherGenerator : VoucherGeneratorBase
    {
        /// <summary>
        /// Gets the voucher type.
        /// </summary>
        protected override string VoucherType
        {
            get { return "Receipt"; }
        }
    }
}
