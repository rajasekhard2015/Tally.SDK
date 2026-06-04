# Tally Integration SDK - Client Integration Guide

## 1. Overview

`Tally.Integration.SDK` is a lightweight .NET Framework 4.0 class library for importing data into Tally (Prime/ERP) via Tally HTTP XML API.

It accepts these input formats:

- JSON string
- XML string
- POCO object
- `Dictionary<string, object>`
- `DataTable`

The SDK converts input to mapped Tally fields, generates Tally XML, and imports vouchers.

## 2. Prerequisites

### 2.1 Application prerequisites

- .NET Framework 4.0 compatible application
- Reference to `Tally.Integration.SDK.dll`
- Newtonsoft.Json dependency

### 2.2 Tally prerequisites

- Tally Prime / Tally ERP running
- Correct company opened in Tally
- Tally HTTP port available (default: `http://localhost:9000`)

## 3. Package Contents

- SDK project: `Tally.Integration.SDK`
- Sample app: `Tally.Integration.Sample`
- Sample mapping JSON: `Tally.Integration.Sample/Mappings/Sales.json`

## 4. Core API

Main facade class:

- `TallySdk`

Public methods:

- `ImportJson(string json, string voucherType, string mappingFile)`
- `ImportXml(string xml, string voucherType, string mappingFile)`
- `ImportObject(object obj, string voucherType, string mappingFile)`
- `ImportDictionary(Dictionary<string, object> data, string voucherType, string mappingFile)`
- `ImportDataTable(DataTable table, string voucherType, string mappingFile)`
- `TestConnection()`

## 5. Supported Data Input Types (Client Use)

Use the following methods based on client payload type:

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

6. Connectivity check

```csharp
var isConnected = sdk.TestConnection();
```

## 6. Quick Start

```csharp
using System;
using Tally.Integration.SDK;

class Program
{
    static void Main()
    {
        var sdk = new TallySdk("http://localhost:9000", 30000)
        {
            EnablePushToTally = true,
            CompanyName = "Team-X",
            AutoCreateMissingLedgers = true,
          AutoCreateMissingStockItems = true,
          EnforcePreValidation = true,
            AutoFallbackVoucherDateOnDateError = true,
            FallbackVoucherDate = new DateTime(2026, 4, 2)
        };

        var json = "{\"InvoiceNo\":\"INV-1001\",\"Customer\":\"ABC Traders\",\"InvoiceDate\":\"2026-04-02\",\"Amount\":1500.75}";

        var result = sdk.ImportJson(json, "Sales", @"Mappings\Sales.json");

        if (result.Success)
        {
            Console.WriteLine("Import success. Created: " + result.Created);
        }
        else
        {
            Console.WriteLine("Import failed: " + result.ErrorMessage);
        }
    }
}
```

## 7. Mapping File Format

Mapping is fully dynamic and JSON-driven.

Example:

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

Notes:

- No hardcoded source-to-target field mappings are required in code.
- `voucherType` parameter overrides `VoucherType` in mapping file when provided.

### 7.1 Fully dynamic repeated-node mapping

Use `[*]` in source and target paths for variable-length collections:

```json
{
  "VoucherType": "Sales",
  "Mappings": {
    "Items/Item[*]/StockItem": "ALLINVENTORYENTRIES.LIST[*]/STOCKITEMNAME",
    "Items/Item[*]/Quantity": "ALLINVENTORYENTRIES.LIST[*]/BILLEDQTY",
    "Items/Item[*]/Amount": "ALLINVENTORYENTRIES.LIST[*]/AMOUNT",
    "LedgerEntries/Ledger[*]/Name": "ALLLEDGERENTRIES.LIST[*]/LEDGERNAME",
    "LedgerEntries/Ledger[*]/Amount": "ALLLEDGERENTRIES.LIST[*]/AMOUNT"
  }
}
```

At runtime SDK expands wildcard indexes automatically (`[*]` -> `[0]`, `[1]`, `[2]`, ...).

## 8. Voucher Type Support

### 7.1 Built-in voucher generators

- Sales
- Purchase
- Receipt
- Payment
- Journal
- Credit Note
- Debit Note

### 7.2 Dynamic voucher type support

Any other voucher type string is also supported through generic fallback generator.

Examples:

- Contra
- Stock Journal
- Delivery Note
- Custom voucher type names present in Tally

## 9. Important Runtime Settings

`TallySdk` configurable properties:

- `EnablePushToTally`
  - `false`: offline mode (no HTTP push, payload generated only)
  - `true`: push to Tally
- `CompanyName`
  - Sets Tally company context (`SVCURRENTCOMPANY`) for imports
- `AutoCreateMissingLedgers`
  - Automatically creates missing ledgers and retries once
- `AutoCreateMissingStockItems`
  - Automatically creates missing stock items and retries once
- `EnforcePreValidation`
  - Enables voucher rule-pack checks before XML generation/push
- `AutoFallbackVoucherDateOnDateError`
  - Retries with fallback date if Tally returns date-related errors
- `FallbackVoucherDate`
  - Date used for retry when date errors occur

Sales/Purchase-only mode:

- `RestrictToSalesAndPurchase` (default: `true`)
  - Only `Sales` and `Purchase` voucher types are accepted.
  - Any other voucher type returns a controlled SDK response without pushing to Tally.

Unsupported voucher response contract:

- `Success = false`
- `Errors = 1`
- `PushToTallyAttempted = false`
- `ErrorMessage = "Unsupported voucher type: <type>. Currently supported voucher types are Sales and Purchase."`
- `RequestObject` contains mapped data for diagnostics

Voucher Rule Pack coverage (when `EnforcePreValidation = true`):

- Sales/Purchase: party or ledger entries required; inventory or core sales/purchase ledger checks
- Receipt/Payment/Journal/Credit Note/Debit Note: minimum 2 ledger amounts + balance checks
- All vouchers: DATE required in `yyyyMMdd`

## 10. Import Result Fields

`ImportResult` includes:

- `Success`
- `Created`
- `Altered`
- `Deleted`
- `Errors`
- `ResponseXml`
- `ErrorMessage`
- `RequestObject`
- `RequestXml`
- `PushToTallyAttempted`

## 11. Troubleshooting

### 10.1 No data visible in Tally

- Ensure `EnablePushToTally = true`
- Check `Created/Altered/Deleted` counters
- Inspect `ResponseXml` and `ErrorMessage`
- Verify report date filters in Tally Day Book / Voucher view

### 10.2 "Could not find Company ''"

- Set `sdk.CompanyName` to exact open company name in Tally

### 10.3 "Voucher date is missing" / date-related failures

- Use a date within active company period
- Enable fallback:
  - `AutoFallbackVoucherDateOnDateError = true`
  - Set `FallbackVoucherDate`

### 10.4 "Ledger 'X' does not exist"

- Keep `AutoCreateMissingLedgers = true`
- Or create required ledgers in Tally manually

## 12. Security and Production Notes

- Run SDK in controlled network context; Tally endpoint is typically local.
- Validate/clean incoming payloads before import.
- Log `RequestXml` and `ResponseXml` for audit/debug.
- Use sensible timeout values for production workloads.

## 13. Verification Checklist

Before go-live:

- Tally reachable at configured URL
- Correct company name configured
- Mappings validated for each voucher type
- Required ledgers/groups available (or auto-create enabled)
- Date strategy aligned with company period
- Integration logs enabled for failures

## 14. Support Handover Notes

Share with client:

- SDK DLL and dependencies
- This guide
- Voucher-type-specific mapping JSON files
- Environment-specific settings (URL, timeout, company name)

---

For implementation reference, see sample app flow in `Tally.Integration.Sample/Program.cs`.
