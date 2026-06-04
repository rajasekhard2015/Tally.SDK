using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;
using System.Text;
using System.Text.Json;
using Tally.Integration.SDK;
using Tally.Integration.SDK.Models;
using Tally.Integration.SDK.Tally;

namespace Tally.Integration.Web.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;

    public IReadOnlyList<string> SupportedVoucherTypes { get; } = new[]
    {
        "Sales",
        "Purchase"
    };

    public string JsonPayloadTemplate =>
        "{\"InvoiceNo\":\"INV-3001\",\"Customer\":\"ABC Traders\",\"InvoiceDate\":\"2026-04-02\",\"Amount\":2500.00}";

    public string XmlPayloadTemplate =>
        "<Invoice><InvoiceNo>INV-3001</InvoiceNo><Customer>ABC Traders</Customer><InvoiceDate>2026-04-02</InvoiceDate><Amount>2500.00</Amount></Invoice>";

    public string MappingTemplatesJson => JsonSerializer.Serialize(BuildMappingTemplates());

    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
        Input = new InputModel();
        Result = new ResultViewModel();
    }

    [BindProperty]
    public InputModel Input { get; set; }

    public ResultViewModel Result { get; set; }

    public void OnGet()
    {
        Input = new InputModel
        {
            InputType = "json",
            VoucherType = "Sales",
            TallyUrl = "http://localhost:9000",
            TimeoutMs = 30000,
            CompanyName = "Team-X",
            EnablePush = true,
            MappingJson = BuildMappingTemplate("Sales"),
            Payload = JsonPayloadTemplate
        };
    }

    public void OnPost()
    {
        Result = new ResultViewModel();

        if (!ModelState.IsValid)
        {
            Result.StatusMessage = "Please fix validation errors and try again.";
            Result.ValidationErrors = GetAllValidationErrors();
            return;
        }

        if (!IsJson(Input.MappingJson))
        {
            ModelState.AddModelError("Input.MappingJson", "Mapping JSON is invalid.");
            Result.StatusMessage = "Please fix validation errors and try again.";
            Result.ValidationErrors = GetAllValidationErrors();
            return;
        }

        if (!IsValidPayloadByInputType(Input.InputType, Input.Payload))
        {
            ModelState.AddModelError("Input.Payload", "Payload format does not match selected input type.");
            Result.StatusMessage = "Please fix validation errors and try again.";
            Result.ValidationErrors = GetAllValidationErrors();
            return;
        }

        var mappingPath = Path.Combine(Path.GetTempPath(), "tally-mapping-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            System.IO.File.WriteAllText(mappingPath, Input.MappingJson, Encoding.UTF8);

            var connector = new TallyConnector(Input.TallyUrl, Input.TimeoutMs);
            var sdk = new TallySdk(connector)
            {
                EnablePushToTally = Input.EnablePush,
                CompanyName = Input.CompanyName ?? string.Empty,
                AutoCreateMissingLedgers = true,
                AutoFallbackVoucherDateOnDateError = true,
                FallbackVoucherDate = DateTime.Today
            };

            var effectiveVoucherType = ResolveVoucherType(Input.VoucherType);

            ImportResult importResult;
            if (string.Equals(Input.InputType, "xml", StringComparison.OrdinalIgnoreCase))
            {
                importResult = sdk.ImportXml(Input.Payload, effectiveVoucherType, mappingPath);
            }
            else
            {
                importResult = sdk.ImportJson(Input.Payload, effectiveVoucherType, mappingPath);
            }

            Result.Success = importResult.Success;
            Result.Created = importResult.Created;
            Result.Altered = importResult.Altered;
            Result.Deleted = importResult.Deleted;
            Result.Errors = importResult.Errors;
            Result.ErrorMessage = importResult.ErrorMessage ?? string.Empty;
            Result.RequestXml = importResult.RequestXml ?? string.Empty;
            Result.ResponseXml = importResult.ResponseXml ?? string.Empty;
            Result.RequestObjectJson = importResult.RequestObject == null
                ? string.Empty
                : JsonSerializer.Serialize(importResult.RequestObject, new JsonSerializerOptions { WriteIndented = true });

            Result.StatusMessage = importResult.Success
                ? (Input.EnablePush ? "Import completed and pushed to Tally." : "Conversion completed. Push is disabled.")
                : "Import/conversion failed. Check details below.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process Tally import request.");
            Result.StatusMessage = "Unexpected error: " + ex.Message;
        }
        finally
        {
            TryDelete(mappingPath);
        }
    }

    private List<string> GetAllValidationErrors()
    {
        return ModelState
            .Where(x => x.Value != null && x.Value.Errors.Count > 0)
            .SelectMany(x => x.Value!.Errors)
            .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Invalid input." : e.ErrorMessage)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static string ResolveVoucherType(string selectedVoucherType)
    {
        return string.IsNullOrWhiteSpace(selectedVoucherType) ? "Sales" : selectedVoucherType.Trim();
    }

    private static bool IsJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsXml(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            return false;
        }

        try
        {
            XDocument.Parse(xml);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidPayloadByInputType(string inputType, string payload)
    {
        return string.Equals(inputType, "xml", StringComparison.OrdinalIgnoreCase)
            ? IsXml(payload)
            : IsJson(payload);
    }

    private static Dictionary<string, string> BuildMappingTemplates()
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Sales", BuildMappingTemplate("Sales") },
            { "Purchase", BuildMappingTemplate("Purchase") }
        };
    }

    private static string BuildMappingTemplate(string voucherType)
    {
        var definition = new
        {
            VoucherType = voucherType,
            Mappings = new Dictionary<string, string>
            {
                { "InvoiceNo", "VOUCHERNUMBER" },
                { "Customer", "PARTYLEDGERNAME" },
                { "InvoiceDate", "DATE" },
                { "Amount", "AMOUNT" }
            }
        };

        return JsonSerializer.Serialize(definition, new JsonSerializerOptions { WriteIndented = true });
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(path) && System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }
        }
        catch
        {
            // Ignore cleanup failures.
        }
    }

    public class InputModel
    {
        [Required]
        public string InputType { get; set; } = string.Empty;

        [Required]
        public string VoucherType { get; set; } = string.Empty;

        [Required]
        public string TallyUrl { get; set; } = string.Empty;

        [Range(1000, 120000)]
        public int TimeoutMs { get; set; }

        public string CompanyName { get; set; } = string.Empty;

        public bool EnablePush { get; set; }

        [Required]
        public string MappingJson { get; set; } = string.Empty;

        [Required]
        public string Payload { get; set; } = string.Empty;
    }

    public class ResultViewModel
    {
        public string StatusMessage { get; set; } = string.Empty;

        public List<string> ValidationErrors { get; set; } = new();

        public bool Success { get; set; }

        public int Created { get; set; }

        public int Altered { get; set; }

        public int Deleted { get; set; }

        public int Errors { get; set; }

        public string ErrorMessage { get; set; } = string.Empty;

        public string RequestObjectJson { get; set; } = string.Empty;

        public string RequestXml { get; set; } = string.Empty;

        public string ResponseXml { get; set; } = string.Empty;
    }
}
