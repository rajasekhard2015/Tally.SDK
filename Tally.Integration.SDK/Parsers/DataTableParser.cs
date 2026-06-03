using System;
using System.Collections.Generic;
using System.Data;

namespace Tally.Integration.SDK.Parsers
{
    /// <summary>
    /// Parses DataTable input into dictionaries.
    /// </summary>
    public class DataTableParser
    {
        /// <summary>
        /// Parses each data row into a dictionary.
        /// </summary>
        /// <param name="table">The source DataTable.</param>
        /// <returns>List of row dictionaries.</returns>
        public List<Dictionary<string, object>> Parse(DataTable table)
        {
            if (table == null)
            {
                throw new ArgumentNullException("table");
            }

            var result = new List<Dictionary<string, object>>();

            foreach (DataRow row in table.Rows)
            {
                var rowData = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (DataColumn column in table.Columns)
                {
                    rowData[column.ColumnName] = row[column];
                }

                result.Add(rowData);
            }

            return result;
        }
    }
}
