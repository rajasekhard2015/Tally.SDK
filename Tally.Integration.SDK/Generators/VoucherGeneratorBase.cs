using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;

namespace Tally.Integration.SDK.Generators
{
    /// <summary>
    /// Base class for voucher XML generation.
    /// </summary>
    public abstract class VoucherGeneratorBase : IVoucherGenerator
    {
        /// <summary>
        /// Gets the voucher type used in Tally XML.
        /// </summary>
        protected abstract string VoucherType { get; }

        /// <summary>
        /// Generates a complete Tally envelope for the voucher.
        /// </summary>
        /// <param name="mappedData">Mapped Tally field data.</param>
        /// <returns>Tally import XML.</returns>
        public string Generate(Dictionary<string, object> mappedData)
        {
            if (mappedData == null)
            {
                throw new ArgumentNullException("mappedData");
            }

            var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in mappedData)
            {
                normalized[field.Key] = field.Value == null ? string.Empty : Convert.ToString(field.Value, CultureInfo.InvariantCulture);
            }

            var voucherDate = GetOrDefault(normalized, "DATE", DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
            var voucherNumber = GetOrDefault(normalized, "VOUCHERNUMBER", string.Empty);
            var partyLedgerName = GetOrDefault(normalized, "PARTYLEDGERNAME", GetDefaultPartyLedgerName());
            var narration = GetOrDefault(normalized, "NARRATION", string.Empty);
            var amount = GetAmount(normalized);
            var counterLedger = GetCounterLedgerName(normalized);

            var voucher = new XElement("VOUCHER",
                new XAttribute("VCHTYPE", VoucherType),
                new XAttribute("ACTION", "Create"),
                new XAttribute("OBJVIEW", "Accounting Voucher View"),
                new XElement("DATE", voucherDate),
                new XElement("VOUCHERTYPENAME", VoucherType),
                new XElement("PERSISTEDVIEW", "Accounting Voucher View"),
                new XElement("ISINVOICE", "No"),
                new XElement("EFFECTIVEDATE", voucherDate));

            if (!string.IsNullOrWhiteSpace(voucherNumber))
            {
                voucher.Add(new XElement("VOUCHERNUMBER", voucherNumber));
            }

            if (!string.IsNullOrWhiteSpace(partyLedgerName))
            {
                voucher.Add(new XElement("PARTYLEDGERNAME", partyLedgerName));
            }

            if (!string.IsNullOrWhiteSpace(narration))
            {
                voucher.Add(new XElement("NARRATION", narration));
            }

            // Add dynamic fields that are not already part of strongly handled voucher metadata.
            foreach (var field in normalized)
            {
                if (IsReservedField(field.Key))
                {
                    continue;
                }

                var value = field.Value ?? string.Empty;
                voucher.Add(new XElement(field.Key, value));
            }

            AddLedgerEntries(voucher, partyLedgerName, counterLedger, amount);

            var envelope = new XDocument(
                new XElement("ENVELOPE",
                    new XElement("HEADER",
                        new XElement("TALLYREQUEST", "Import Data")),
                    new XElement("BODY",
                        new XElement("IMPORTDATA",
                            new XElement("REQUESTDESC",
                                new XElement("REPORTNAME", "Vouchers")),
                            new XElement("REQUESTDATA",
                                new XElement("TALLYMESSAGE",
                                    new XAttribute(XNamespace.Xmlns + "UDF", "TallyUDF"),
                                    voucher))))));

            return envelope.ToString(SaveOptions.DisableFormatting);
        }

        private static bool IsReservedField(string key)
        {
            return key.Equals("DATE", StringComparison.OrdinalIgnoreCase)
                || key.Equals("VOUCHERTYPENAME", StringComparison.OrdinalIgnoreCase)
                || key.Equals("VOUCHERNUMBER", StringComparison.OrdinalIgnoreCase)
                || key.Equals("PARTYLEDGERNAME", StringComparison.OrdinalIgnoreCase)
                || key.Equals("PERSISTEDVIEW", StringComparison.OrdinalIgnoreCase)
                || key.Equals("ISINVOICE", StringComparison.OrdinalIgnoreCase)
                || key.Equals("EFFECTIVEDATE", StringComparison.OrdinalIgnoreCase)
                || key.Equals("NARRATION", StringComparison.OrdinalIgnoreCase)
                || key.Equals("AMOUNT", StringComparison.OrdinalIgnoreCase)
                || key.Equals("COUNTERLEDGERNAME", StringComparison.OrdinalIgnoreCase)
                || key.Equals("SALESLEDGERNAME", StringComparison.OrdinalIgnoreCase)
                || key.Equals("PURCHASELEDGERNAME", StringComparison.OrdinalIgnoreCase)
                || key.Equals("BANKLEDGERNAME", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetOrDefault(Dictionary<string, string> data, string key, string defaultValue)
        {
            string value;
            return data.TryGetValue(key, out value) && !string.IsNullOrWhiteSpace(value)
                ? value
                : defaultValue;
        }

        private static decimal GetAmount(Dictionary<string, string> data)
        {
            string amountText;
            if (!data.TryGetValue("AMOUNT", out amountText) || string.IsNullOrWhiteSpace(amountText))
            {
                return 0m;
            }

            decimal amount;
            if (decimal.TryParse(amountText, NumberStyles.Any, CultureInfo.InvariantCulture, out amount))
            {
                return Math.Abs(amount);
            }

            return 0m;
        }

        private string GetCounterLedgerName(Dictionary<string, string> data)
        {
            string value;
            if (data.TryGetValue("COUNTERLEDGERNAME", out value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            if (VoucherType.Equals("Sales", StringComparison.OrdinalIgnoreCase))
            {
                if (data.TryGetValue("SALESLEDGERNAME", out value) && !string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }

                return "Sales A/c";
            }

            if (VoucherType.Equals("Purchase", StringComparison.OrdinalIgnoreCase))
            {
                if (data.TryGetValue("PURCHASELEDGERNAME", out value) && !string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }

                return "Purchase A/c";
            }

            if (VoucherType.Equals("Receipt", StringComparison.OrdinalIgnoreCase)
                || VoucherType.Equals("Payment", StringComparison.OrdinalIgnoreCase))
            {
                if (data.TryGetValue("BANKLEDGERNAME", out value) && !string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }

                return "Cash";
            }

            return "Suspense A/c";
        }

        private void AddLedgerEntries(XElement voucher, string partyLedgerName, string counterLedgerName, decimal amount)
        {
            if (string.IsNullOrWhiteSpace(partyLedgerName) || string.IsNullOrWhiteSpace(counterLedgerName) || amount <= 0)
            {
                return;
            }

            var debitFirst = IsDebitFirstVoucher();
            var firstEntryAmount = debitFirst ? -amount : amount;
            var secondEntryAmount = -firstEntryAmount;

            voucher.Add(
                new XElement("ALLLEDGERENTRIES.LIST",
                    new XElement("LEDGERNAME", partyLedgerName),
                    new XElement("ISDEEMEDPOSITIVE", firstEntryAmount < 0 ? "Yes" : "No"),
                    new XElement("AMOUNT", firstEntryAmount.ToString("0.00", CultureInfo.InvariantCulture))));

            voucher.Add(
                new XElement("ALLLEDGERENTRIES.LIST",
                    new XElement("LEDGERNAME", counterLedgerName),
                    new XElement("ISDEEMEDPOSITIVE", secondEntryAmount < 0 ? "Yes" : "No"),
                    new XElement("AMOUNT", secondEntryAmount.ToString("0.00", CultureInfo.InvariantCulture))));
        }

        private bool IsDebitFirstVoucher()
        {
            return VoucherType.Equals("Sales", StringComparison.OrdinalIgnoreCase)
                || VoucherType.Equals("Purchase", StringComparison.OrdinalIgnoreCase)
                || VoucherType.Equals("Receipt", StringComparison.OrdinalIgnoreCase)
                || VoucherType.Equals("Debit Note", StringComparison.OrdinalIgnoreCase);
        }

        private string GetDefaultPartyLedgerName()
        {
            if (VoucherType.Equals("Payment", StringComparison.OrdinalIgnoreCase))
            {
                return "Cash";
            }

            if (VoucherType.Equals("Receipt", StringComparison.OrdinalIgnoreCase))
            {
                return "Cash";
            }

            return string.Empty;
        }
    }
}
