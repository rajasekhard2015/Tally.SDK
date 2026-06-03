using System;

namespace Tally.Integration.SDK.Generators
{
    /// <summary>
    /// Generates Tally XML for any dynamic voucher type name.
    /// </summary>
    public class GenericVoucherGenerator : VoucherGeneratorBase
    {
        private readonly string _voucherType;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericVoucherGenerator"/> class.
        /// </summary>
        /// <param name="voucherType">Voucher type name.</param>
        public GenericVoucherGenerator(string voucherType)
        {
            if (string.IsNullOrWhiteSpace(voucherType))
            {
                throw new ArgumentException("Voucher type cannot be empty.", "voucherType");
            }

            _voucherType = voucherType.Trim();
        }

        /// <summary>
        /// Gets the voucher type.
        /// </summary>
        protected override string VoucherType
        {
            get { return _voucherType; }
        }
    }
}