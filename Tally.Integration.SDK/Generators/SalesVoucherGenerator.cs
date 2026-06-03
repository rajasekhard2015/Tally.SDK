namespace Tally.Integration.SDK.Generators
{
    /// <summary>
    /// Generates Tally XML for Sales vouchers.
    /// </summary>
    public class SalesVoucherGenerator : VoucherGeneratorBase
    {
        /// <summary>
        /// Gets the voucher type.
        /// </summary>
        protected override string VoucherType
        {
            get { return "Sales"; }
        }
    }
}
