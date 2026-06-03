using System;
using System.Collections.Generic;
using System.Data;
using Tally.Integration.SDK;
using Tally.Integration.SDK.Models;
using Tally.Integration.SDK.Tally;

namespace ClientApp
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var sdk = new TallySdk(new TallyConnector("http://localhost:9000", 30000));
            sdk.EnablePushToTally = true;
            sdk.CompanyName = "Team-X";
            sdk.AutoCreateMissingLedgers = true;
            sdk.AutoFallbackVoucherDateOnDateError = true;
            sdk.FallbackVoucherDate = new DateTime(2026, 4, 2);

            RunImportJsonSample(sdk);
            RunImportXmlSample(sdk);
            RunImportObjectSample(sdk);
            RunImportDictionarySample(sdk);
            RunImportDataTableSample(sdk);

            Console.WriteLine("TestConnection: " + sdk.TestConnection());
        }

        private static void RunImportJsonSample(TallySdk sdk)
        {
            var jsonData = "{\"InvoiceNo\":\"INV-2001\",\"Customer\":\"ABC Traders\",\"InvoiceDate\":\"2026-04-02\",\"Amount\":2500.00}";
            var result = sdk.ImportJson(jsonData, "Sales", @"Mappings\Sales.json");
            PrintResult("ImportJson", result);
        }

        private static void RunImportXmlSample(TallySdk sdk)
        {
            var xmlData = "<Invoice><InvoiceNo>INV-2002</InvoiceNo><Customer>XYZ Retail</Customer><InvoiceDate>2026-04-02</InvoiceDate><Amount>1800.00</Amount></Invoice>";
            var result = sdk.ImportXml(xmlData, "Sales", @"Mappings\Sales.json");
            PrintResult("ImportXml", result);
        }

        private static void RunImportObjectSample(TallySdk sdk)
        {
            var invoice = new InvoiceInput
            {
                InvoiceNo = "INV-2003",
                Customer = "Delta Stores",
                InvoiceDate = new DateTime(2026, 4, 2),
                Amount = 3200.00m
            };

            var result = sdk.ImportObject(invoice, "Sales", @"Mappings\Sales.json");
            PrintResult("ImportObject", result);
        }

        private static void RunImportDictionarySample(TallySdk sdk)
        {
            var dictionaryData = new Dictionary<string, object>
            {
                { "InvoiceNo", "INV-2004" },
                { "Customer", "Prime Mart" },
                { "InvoiceDate", "2026-04-02" },
                { "Amount", 4100.90m }
            };

            var result = sdk.ImportDictionary(dictionaryData, "Sales", @"Mappings\Sales.json");
            PrintResult("ImportDictionary", result);
        }

        private static void RunImportDataTableSample(TallySdk sdk)
        {
            var table = new DataTable("Invoices");
            table.Columns.Add("InvoiceNo", typeof(string));
            table.Columns.Add("Customer", typeof(string));
            table.Columns.Add("InvoiceDate", typeof(string));
            table.Columns.Add("Amount", typeof(decimal));

            table.Rows.Add("INV-2005", "A One Traders", "2026-04-02", 1200.00m);
            table.Rows.Add("INV-2006", "B Two Traders", "2026-04-02", 1800.25m);

            var result = sdk.ImportDataTable(table, "Sales", @"Mappings\Sales.json");
            PrintResult("ImportDataTable", result);
        }

        private static void PrintResult(string title, ImportResult result)
        {
            Console.WriteLine();
            Console.WriteLine("=== " + title + " ===");
            Console.WriteLine("Success: " + result.Success);
            Console.WriteLine("Created: " + result.Created);
            Console.WriteLine("Altered: " + result.Altered);
            Console.WriteLine("Deleted: " + result.Deleted);
            Console.WriteLine("Errors: " + result.Errors);
            Console.WriteLine("ErrorMessage: " + result.ErrorMessage);
            Console.WriteLine();
            Console.WriteLine("Request Object:");

            if (result.RequestObject != null)
            {
                foreach (var item in result.RequestObject)
                {
                    Console.WriteLine(item.Key + " = " + item.Value);
                }
            }

            Console.WriteLine();
            Console.WriteLine("Generated Tally XML:");
            Console.WriteLine(result.RequestXml);
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
