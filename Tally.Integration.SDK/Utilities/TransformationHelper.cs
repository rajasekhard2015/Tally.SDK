using System;
using System.Globalization;

namespace Tally.Integration.SDK.Utilities
{
    /// <summary>
    /// Applies common value transformations required by Tally XML.
    /// </summary>
    public static class TransformationHelper
    {
        /// <summary>
        /// Formats values for Tally target fields.
        /// </summary>
        /// <param name="targetField">The target Tally field name.</param>
        /// <param name="value">The raw source value.</param>
        /// <returns>Formatted value string.</returns>
        public static string Transform(string targetField, object value)
        {
            if (value == null || value == DBNull.Value)
            {
                return string.Empty;
            }

            var text = Convert.ToString(value, CultureInfo.InvariantCulture);
            if (text == null)
            {
                return string.Empty;
            }

            text = text.Trim();

            if (IsDateField(targetField))
            {
                DateTime date;
                if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
                    || DateTime.TryParse(text, out date))
                {
                    return date.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
                }
            }

            decimal number;
            if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out number))
            {
                return number.ToString("0.00", CultureInfo.InvariantCulture);
            }

            return text;
        }

        private static bool IsDateField(string targetField)
        {
            if (string.IsNullOrWhiteSpace(targetField))
            {
                return false;
            }

            var upper = targetField.ToUpperInvariant();
            return upper.Contains("DATE");
        }
    }
}
