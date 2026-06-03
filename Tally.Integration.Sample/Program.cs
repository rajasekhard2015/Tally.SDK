using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Tally.Integration.SDK.Models;
using Tally.Integration.SDK;
using Tally.Integration.SDK.Tally;

namespace Tally.Integration.Sample
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var sdk = new TallySdk(new TallyConnector("http://localhost:9000", 30000));
            sdk.EnablePushToTally = false;
            sdk.AutoFallbackVoucherDateOnDateError = true;
            sdk.FallbackVoucherDate = new DateTime(2026, 4, 2);
            sdk.CompanyName = "Team-X";

            // Later, enable this when Tally URL is available.
             sdk.EnablePushToTally = true;

            var mappingPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mappings", "Sales.json");

            Console.WriteLine("===== ImportJson Sample =====");
            var jsonResult = RunImportJsonSample(sdk, mappingPath);
            PrintResult(jsonResult);

            Console.WriteLine();
            Console.WriteLine("===== ImportXml Sample =====");
            var xmlResult = RunImportXmlSample(sdk, mappingPath);
            PrintResult(xmlResult);

            Console.WriteLine();
            Console.WriteLine("===== ImportObject Sample =====");
            var objectResult = RunImportObjectSample(sdk, mappingPath);
            PrintResult(objectResult);

            Console.WriteLine();
            Console.WriteLine("===== ImportDictionary Sample =====");
            var dictionaryResult = RunImportDictionarySample(sdk, mappingPath);
            PrintResult(dictionaryResult);

            Console.WriteLine();
            Console.WriteLine("===== ImportDataTable Sample =====");
            var dataTableResult = RunImportDataTableSample(sdk, mappingPath);
            PrintResult(dataTableResult);

            Console.WriteLine();
            Console.WriteLine("===== TestConnection Sample =====");
            var isConnected = sdk.TestConnection();
            Console.WriteLine("TestConnection: " + isConnected);
        }

        private static ImportResult RunImportJsonSample(TallySdk sdk, string mappingPath)
        {
            var jsonData = "{\"InvoiceNo\":\"INV-1001\",\"Customer\":\"ABC Traders\",\"InvoiceDate\":\"2026-04-02\",\"Amount\":1500.75}";

            return sdk.ImportJson(
                jsonData,
                "Sales",
                mappingPath);
        }

        private static ImportResult RunImportXmlSample(TallySdk sdk, string mappingPath)
        {
            var xmlData = "<Invoice><InvoiceNo>INV-1002</InvoiceNo><Customer>XYZ Retail</Customer><InvoiceDate>2026-04-02</InvoiceDate><Amount>2500.50</Amount></Invoice>";

            return sdk.ImportXml(
                xmlData,
                "Sales",
                mappingPath);
        }

        private static ImportResult RunImportObjectSample(TallySdk sdk, string mappingPath)
        {
            var invoice = new InvoiceInput
            {
                InvoiceNo = "INV-1003",
                Customer = "Delta Stores",
                InvoiceDate = new DateTime(2026, 4, 2),
                Amount = 3200.00m
            };

            return sdk.ImportObject(
                invoice,
                "Sales",
                mappingPath);
        }

        private static ImportResult RunImportDictionarySample(TallySdk sdk, string mappingPath)
        {
            var dictionaryData = new Dictionary<string, object>
            {
                { "InvoiceNo", "INV-1004" },
                { "Customer", "Prime Mart" },
                { "InvoiceDate", "2026-04-02" },
                { "Amount", 4100.90m }
            };

            return sdk.ImportDictionary(
                dictionaryData,
                "Sales",
                mappingPath);
        }

        private static ImportResult RunImportDataTableSample(TallySdk sdk, string mappingPath)
        {
            var table = new DataTable("Invoices");
            table.Columns.Add("InvoiceNo", typeof(string));
            table.Columns.Add("Customer", typeof(string));
            table.Columns.Add("InvoiceDate", typeof(string));
            table.Columns.Add("Amount", typeof(decimal));

            table.Rows.Add("INV-1005", "A One Traders", "2026-04-02", 1200.00m);
            table.Rows.Add("INV-1006", "B Two Traders", "2026-04-02", 1800.25m);

            return sdk.ImportDataTable(
                table,
                "Sales",
                mappingPath);
        }

        private static void PrintResult(ImportResult result)
        {
            Console.WriteLine("Final request object:");
            if (result.RequestObject != null)
            {
                foreach (var item in result.RequestObject)
                {
                    Console.WriteLine(item.Key + " = " + item.Value);
                }
            }
            else
            {
                Console.WriteLine("(No request object available)");
            }

            Console.WriteLine();
            Console.WriteLine("Final Tally XML:");
            Console.WriteLine(result.RequestXml ?? "(No XML generated)");
            Console.WriteLine();

            Console.WriteLine("Tally Response XML:");
            Console.WriteLine(result.ResponseXml ?? "(No response XML)");
            Console.WriteLine();

            if (result.Success)
            {
                Console.WriteLine(result.PushToTallyAttempted
                    ? "Imported successfully."
                    : "Push skipped (offline mode). Final payload printed above.");
            }
            else
            {
                Console.WriteLine("Import failed: " + result.ErrorMessage);
            }

            Console.WriteLine("Created: " + result.Created);
            Console.WriteLine("Altered: " + result.Altered);
            Console.WriteLine("Deleted: " + result.Deleted);
            Console.WriteLine("Errors: " + result.Errors);

            if (result.Created == 0 && result.Altered == 0 && result.Deleted == 0)
            {
                Console.WriteLine("Warning: Tally did not create/alter/delete any voucher. Check Response XML for IGNORED/LINEERROR details.");
            }
        }

        private class InvoiceInput
        {
            public string InvoiceNo { get; set; }

            public string Customer { get; set; }

            public DateTime InvoiceDate { get; set; }

            public decimal Amount { get; set; }
        }
    }
}
