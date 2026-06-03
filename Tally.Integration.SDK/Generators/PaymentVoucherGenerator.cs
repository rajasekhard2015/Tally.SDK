namespace Tally.Integration.SDK.Generators
{
    /// <summary>
    /// Generates Tally XML for Payment vouchers.
    /// </summary>
    public class PaymentVoucherGenerator : VoucherGeneratorBase
    {
        /// <summary>
        /// Gets the voucher type.
        /// </summary>
        protected override string VoucherType
        {
            get { return "Payment"; }
        }
    }
}
