# Tally.Integration.SDK

Tally.Integration.SDK is a lightweight .NET Framework 4.0 library to import data into Tally Prime / Tally ERP through Tally HTTP XML API.

## Features

- Supports input as JSON, XML, POCO object, Dictionary, and DataTable
- Dynamic field mapping through JSON mapping files
- Wildcard list mapping support using `[*]` for fully dynamic repeated nodes
- Voucher XML generation for common voucher types and dynamic custom voucher types
- Optional offline mode to inspect generated request object and XML
- Tally response parsing with success/error diagnostics
- Optional missing-ledger and missing-stock-item auto-create with retry logic
- Voucher Rule Pack pre-validation for Sales, Purchase, Receipt, Payment, Journal, Credit Note, Debit Note
- Optional date fallback retry logic

## Quick Example

```csharp
var sdk = new TallySdk("http://localhost:9000", 30000)
{
    EnablePushToTally = true,
    CompanyName = "Team-X",
    AutoCreateMissingLedgers = true,
  AutoCreateMissingStockItems = true,
  EnforcePreValidation = true,
    AutoFallbackVoucherDateOnDateError = true
};

var result = sdk.ImportJson(
    "{\"InvoiceNo\":\"INV-1001\",\"Customer\":\"ABC Traders\",\"InvoiceDate\":\"2026-04-02\",\"Amount\":1500.75}",
    "Sales",
    @"Mappings\Sales.json"
);
```

## Mapping File

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

For full integration steps and deployment guidance, see `CLIENT_INTEGRATION_GUIDE.md` in the repository root.

## Dynamic List Mapping (`[*]`)

Use wildcard source and target paths to map repeating collections without fixed indexes.

```json
{
  "VoucherType": "Sales",
  "Mappings": {
    "Items/Item[*]/StockItem": "ALLINVENTORYENTRIES.LIST[*]/STOCKITEMNAME",
    "Items/Item[*]/Quantity": "ALLINVENTORYENTRIES.LIST[*]/BILLEDQTY",
    "Items/Item[*]/Rate": "ALLINVENTORYENTRIES.LIST[*]/RATE",
    "Items/Item[*]/Amount": "ALLINVENTORYENTRIES.LIST[*]/AMOUNT",
    "LedgerEntries/Ledger[*]/Name": "ALLLEDGERENTRIES.LIST[*]/LEDGERNAME",
    "LedgerEntries/Ledger[*]/Amount": "ALLLEDGERENTRIES.LIST[*]/AMOUNT"
  }
}
```

The SDK expands `[*]` to `[0]`, `[1]`, `[2]` based on input data.
