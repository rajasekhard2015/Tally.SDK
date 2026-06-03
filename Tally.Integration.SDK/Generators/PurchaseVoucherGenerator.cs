namespace Tally.Integration.SDK.Generators
{
    /// <summary>
    /// Generates Tally XML for Purchase vouchers.
    /// </summary>
    public class PurchaseVoucherGenerator : VoucherGeneratorBase
    {
        /// <summary>
        /// Gets the voucher type.
        /// </summary>
        protected override string VoucherType
        {
            get { return "Purchase"; }
        }
    }
}
