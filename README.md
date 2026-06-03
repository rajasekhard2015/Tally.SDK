# Tally.Integration.SDK NuGet Client Sample

This sample shows how a client application can consume `Tally.Integration.SDK` directly from NuGet.

## 1. Install the package

### Package Manager Console

```powershell
Install-Package Tally.Integration.SDK -Version 1.0.0
```

### .NET CLI

```bash
dotnet add package Tally.Integration.SDK --version 1.0.0
```

## 2. Project file reference

If you want to reference the package manually in a `.csproj` file:

```xml
<ItemGroup>
  <PackageReference Include="Tally.Integration.SDK" Version="1.0.0" />
</ItemGroup>
```

## 3. Example mapping file

Create a mapping JSON file such as `Mappings/Sales.json`:

```json
{
  "VoucherType": "Sales",
  "Mappings": {
    "InvoiceNo": "VOUCHERNUMBER",
    "Customer": "PARTYLEDGERNAME",
    "InvoiceDate": "DATE",
    "Amount": "AMOUNT"
  }
}
```

## 4. Supported data input types

Use these SDK methods for each input type:

1. JSON string

```csharp
var result = sdk.ImportJson(jsonData, "Sales", @"Mappings\Sales.json");
```

2. XML string

```csharp
var result = sdk.ImportXml(xmlData, "Sales", @"Mappings\Sales.json");
```

3. POCO object

```csharp
var result = sdk.ImportObject(invoiceObject, "Sales", @"Mappings\Sales.json");
```

4. Dictionary<string, object>

```csharp
var result = sdk.ImportDictionary(dictionaryData, "Sales", @"Mappings\Sales.json");
```

5. DataTable

```csharp
var result = sdk.ImportDataTable(invoiceTable, "Sales", @"Mappings\Sales.json");
```

6. Connection test

```csharp
var isConnected = sdk.TestConnection();
```

## 5. Example console application

The following sample shows all supported SDK entry points in one client app.

```csharp
using System;
using System.Collections.Generic;
using System.Data;
using Tally.Integration.SDK;
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

          private static void PrintResult(string title, Tally.Integration.SDK.Models.ImportResult result)
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
```

    ## 6. Expected output

When the Tally company is open and the voucher is accepted, the output should look like this:

```text
Success: True
Created: 1
Altered: 0
Deleted: 0
Errors: 0
```

## 7. Notes for client

- Replace `CompanyName` with the exact company name opened in Tally.
- Keep `EnablePushToTally = true` only when you want live posting.
- If testing offline, set `EnablePushToTally = false` and inspect `RequestObject` and `RequestXml`.
- Make sure the mapping file path is correct relative to the client application.

## 8. Support reminder

The SDK supports these common voucher types out of the box:

- Sales
- Purchase
- Receipt
- Payment
- Journal
- Credit Note
- Debit Note

It also supports custom voucher type names through the generic generator fallback.
