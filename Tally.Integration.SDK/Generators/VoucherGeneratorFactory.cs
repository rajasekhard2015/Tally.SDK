using System;

namespace Tally.Integration.SDK.Generators
{
    /// <summary>
    /// Factory for voucher XML generators.
    /// </summary>
    public static class VoucherGeneratorFactory
    {
        /// <summary>
        /// Creates a voucher generator for the specified voucher type.
        /// </summary>
        /// <param name="voucherType">Voucher type input.</param>
        /// <returns>Voucher generator instance.</returns>
        public static IVoucherGenerator Create(string voucherType)
        {
            if (string.IsNullOrWhiteSpace(voucherType))
            {
                throw new ArgumentException("Voucher type cannot be empty.", "voucherType");
            }

            var type = voucherType.Trim().ToLowerInvariant();
            switch (type)
            {
                case "sales":
                    return new SalesVoucherGenerator();
                case "purchase":
                    return new PurchaseVoucherGenerator();
                default:
                    throw new NotSupportedException(
                        "Unsupported voucher type: " + voucherType + ". Currently supported generator types are Sales and Purchase.");
            }
        }
    }
}
