using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Tally.Integration.SDK.Generators;
using Tally.Integration.SDK.Mappings;
using Tally.Integration.SDK.Models;
using Tally.Integration.SDK.Parsers;
using Tally.Integration.SDK.Tally;

namespace Tally.Integration.SDK
{
    /// <summary>
    /// Facade for importing various input formats into Tally.
    /// </summary>
    public class TallySdk
    {
        private readonly JsonParser _jsonParser;
        private readonly XmlParser _xmlParser;
        private readonly ObjectParser _objectParser;
        private readonly DictionaryParser _dictionaryParser;
        private readonly DataTableParser _dataTableParser;
        private readonly MappingEngine _mappingEngine;
        private readonly TallyConnector _tallyConnector;
        private readonly TallyResponseParser _responseParser;

        /// <summary>
        /// Gets or sets a value indicating whether data should be pushed to Tally.
        /// </summary>
        public bool EnablePushToTally { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether missing ledgers should be auto-created and retried once.
        /// </summary>
        public bool AutoCreateMissingLedgers { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether voucher import should retry with fallback date when Tally returns date errors.
        /// </summary>
        public bool AutoFallbackVoucherDateOnDateError { get; set; }

        /// <summary>
        /// Gets or sets the fallback voucher date used during date-error retry.
        /// When null, current system date is used.
        /// </summary>
        public DateTime? FallbackVoucherDate { get; set; }

        /// <summary>
        /// Gets or sets the target company name in Tally.
        /// When empty, currently loaded company context is used.
        /// </summary>
        public string CompanyName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether only Sales and Purchase vouchers are allowed.
        /// </summary>
        public bool RestrictToSalesAndPurchase { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TallySdk"/> class.
        /// </summary>
        public TallySdk()
            : this(new TallyConnector())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TallySdk"/> class with Tally URL and timeout.
        /// </summary>
        /// <param name="tallyUrl">Tally HTTP endpoint URL.</param>
        /// <param name="timeoutMs">HTTP timeout in milliseconds.</param>
        public TallySdk(string tallyUrl, int timeoutMs)
            : this(new TallyConnector(tallyUrl, timeoutMs))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TallySdk"/> class with custom connector.
        /// </summary>
        /// <param name="connector">Tally connector instance.</param>
        public TallySdk(TallyConnector connector)
        {
            _jsonParser = new JsonParser();
            _xmlParser = new XmlParser();
            _objectParser = new ObjectParser();
            _dictionaryParser = new DictionaryParser();
            _dataTableParser = new DataTableParser();
            _mappingEngine = new MappingEngine();
            _responseParser = new TallyResponseParser();
            _tallyConnector = connector ?? new TallyConnector();

            // Default to offline mode so request payload can be verified without a live Tally endpoint.
            EnablePushToTally = false;
            AutoCreateMissingLedgers = true;
            AutoFallbackVoucherDateOnDateError = true;
            FallbackVoucherDate = null;
            CompanyName = string.Empty;
            RestrictToSalesAndPurchase = true;
        }

        /// <summary>
        /// Imports JSON input into Tally.
        /// </summary>
        /// <param name="json">JSON input string.</param>
        /// <param name="voucherType">Voucher type.</param>
        /// <param name="mappingFile">Mapping file path.</param>
        /// <returns>Import result.</returns>
        public ImportResult ImportJson(string json, string voucherType, string mappingFile)
        {
            try
            {
                var data = _jsonParser.Parse(json);
                return ImportDictionaryInternal(data, voucherType, mappingFile);
            }
            catch (Exception ex)
            {
                return ImportResult.Fail(ex);
            }
        }

        /// <summary>
        /// Imports XML input into Tally.
        /// </summary>
        /// <param name="xml">XML input string.</param>
        /// <param name="voucherType">Voucher type.</param>
        /// <param name="mappingFile">Mapping file path.</param>
        /// <returns>Import result.</returns>
        public ImportResult ImportXml(string xml, string voucherType, string mappingFile)
        {
            try
            {
                var data = _xmlParser.Parse(xml);
                return ImportDictionaryInternal(data, voucherType, mappingFile);
            }
            catch (Exception ex)
            {
                return ImportResult.Fail(ex);
            }
        }

        /// <summary>
        /// Imports POCO object input into Tally.
        /// </summary>
        /// <param name="obj">Source object.</param>
        /// <param name="voucherType">Voucher type.</param>
        /// <param name="mappingFile">Mapping file path.</param>
        /// <returns>Import result.</returns>
        public ImportResult ImportObject(object obj, string voucherType, string mappingFile)
        {
            try
            {
                var data = _objectParser.Parse(obj);
                return ImportDictionaryInternal(data, voucherType, mappingFile);
            }
            catch (Exception ex)
            {
                return ImportResult.Fail(ex);
            }
        }

        /// <summary>
        /// Imports dictionary input into Tally.
        /// </summary>
        /// <param name="data">Source dictionary.</param>
        /// <param name="voucherType">Voucher type.</param>
        /// <param name="mappingFile">Mapping file path.</param>
        /// <returns>Import result.</returns>
        public ImportResult ImportDictionary(Dictionary<string, object> data, string voucherType, string mappingFile)
        {
            try
            {
                var normalized = _dictionaryParser.Parse(data);
                return ImportDictionaryInternal(normalized, voucherType, mappingFile);
            }
            catch (Exception ex)
            {
                return ImportResult.Fail(ex);
            }
        }

        /// <summary>
        /// Imports each DataTable row into Tally.
        /// </summary>
        /// <param name="table">Source DataTable.</param>
        /// <param name="voucherType">Voucher type.</param>
        /// <param name="mappingFile">Mapping file path.</param>
        /// <returns>Aggregated import result.</returns>
        public ImportResult ImportDataTable(DataTable table, string voucherType, string mappingFile)
        {
            var aggregate = new ImportResult { Success = true };

            try
            {
                var rows = _dataTableParser.Parse(table);
                foreach (var row in rows)
                {
                    var result = ImportDictionaryInternal(row, voucherType, mappingFile);
                    aggregate.Merge(result);
                }

                if (rows.Count == 0)
                {
                    aggregate.Success = true;
                }

                return aggregate;
            }
            catch (Exception ex)
            {
                return ImportResult.Fail(ex);
            }
        }

        /// <summary>
        /// Tests connectivity with Tally.
        /// </summary>
        /// <returns>True if Tally endpoint is reachable.</returns>
        public bool TestConnection()
        {
            return _tallyConnector.TestConnection();
        }

        private ImportResult ImportDictionaryInternal(Dictionary<string, object> sourceData, string voucherType, string mappingFile)
        {
            var mapping = _mappingEngine.LoadMapping(mappingFile);
            var mappedData = _mappingEngine.Map(sourceData, mapping);

            var selectedVoucherType = string.IsNullOrWhiteSpace(voucherType)
                ? mapping.VoucherType
                : voucherType;

            if (RestrictToSalesAndPurchase && !IsSalesOrPurchase(selectedVoucherType))
            {
                return new ImportResult
                {
                    Success = false,
                    Errors = 1,
                    ErrorMessage = "Unsupported voucher type: " + (selectedVoucherType ?? string.Empty) + ". Currently supported voucher types are Sales and Purchase.",
                    RequestObject = mappedData,
                    PushToTallyAttempted = false
                };
            }

            var templateProfile = ResolveCompanyTemplate(mapping);
            if (templateProfile != null)
            {
                ApplyTemplateDefaults(mappedData, templateProfile);

                if (templateProfile.StrictMode)
                {
                    var missingTargets = ValidateRequiredTargets(mappedData, templateProfile);
                    if (missingTargets.Count > 0)
                    {
                        var guidance = BuildMissingTargetGuidance(missingTargets);
                        return new ImportResult
                        {
                            Success = false,
                            Errors = 1,
                            ErrorMessage = "Company template validation failed. Missing required target fields: "
                                + string.Join(", ", missingTargets)
                                + ". " + guidance,
                            RequestObject = mappedData,
                            PushToTallyAttempted = false
                        };
                    }
                }
            }

            var generator = VoucherGeneratorFactory.Create(selectedVoucherType);
            var xml = generator.Generate(mappedData);

            if (!EnablePushToTally)
            {
                return new ImportResult
                {
                    Success = true,
                    PushToTallyAttempted = false,
                    RequestObject = mappedData,
                    RequestXml = xml
                };
            }

            xml = ApplyCompanyContext(xml);
            var response = _tallyConnector.Import(xml);
            var result = _responseParser.Parse(response);
            result.PushToTallyAttempted = true;
            result.RequestObject = mappedData;
            result.RequestXml = xml;

            if (!result.Success && AutoCreateMissingLedgers)
            {
                var attemptedLedgerCreation = TryCreateMissingLedgers(result.ResponseXml);
                if (attemptedLedgerCreation)
                {
                    var retryResponse = _tallyConnector.Import(xml);
                    result = _responseParser.Parse(retryResponse);
                    result.PushToTallyAttempted = true;
                    result.RequestObject = mappedData;
                    result.RequestXml = xml;
                }
            }

            if (!result.Success && AutoFallbackVoucherDateOnDateError && IsDateRelatedFailure(result.ResponseXml))
            {
                var retryDate = (FallbackVoucherDate ?? DateTime.Today).ToString("yyyyMMdd", CultureInfo.InvariantCulture);
                mappedData["DATE"] = retryDate;

                xml = generator.Generate(mappedData);
                xml = ApplyCompanyContext(xml);
                var retryResponse = _tallyConnector.Import(xml);
                result = _responseParser.Parse(retryResponse);
                result.PushToTallyAttempted = true;
                result.RequestObject = mappedData;
                result.RequestXml = xml;
            }

            if (!result.Success)
            {
                var guidance = BuildTemplateUpdateGuidance(result.ResponseXml, mappedData, templateProfile, selectedVoucherType);
                if (!string.IsNullOrWhiteSpace(guidance))
                {
                    result.ErrorMessage = string.IsNullOrWhiteSpace(result.ErrorMessage)
                        ? guidance
                        : result.ErrorMessage + " | Template Guidance: " + guidance;
                }
            }

            return result;
        }

        private static string BuildMissingTargetGuidance(List<string> missingTargets)
        {
            if (missingTargets == null || missingTargets.Count == 0)
            {
                return string.Empty;
            }

            return "Update mapping JSON and CompanyTemplates.RequiredTargets/Defaults for these fields.";
        }

        private string BuildTemplateUpdateGuidance(
            string responseXml,
            Dictionary<string, object> mappedData,
            CompanyTemplateProfile templateProfile,
            string voucherType)
        {
            var hints = new List<string>();

            if (templateProfile == null)
            {
                hints.Add("Add CompanyTemplates section with StrictMode, Defaults and RequiredTargets.");
            }

            if (!HasRequiredTarget(mappedData, "DATE"))
            {
                hints.Add("Map source date to DATE and include DATE in RequiredTargets.");
            }

            if (!HasRequiredTarget(mappedData, "PARTYLEDGERNAME"))
            {
                hints.Add("Map party field to PARTYLEDGERNAME and include it in RequiredTargets.");
            }

            if (!HasRequiredTarget(mappedData, "ALLLEDGERENTRIES.LIST[*]/AMOUNT") && !CanAutoGenerateLedgerEntries(mappedData))
            {
                hints.Add("Provide explicit ALLLEDGERENTRIES.LIST[*]/LEDGERNAME and /AMOUNT mappings, or map AMOUNT for auto-ledger generation.");
            }

            if ((voucherType ?? string.Empty).Trim().Equals("sales", StringComparison.OrdinalIgnoreCase)
                && !HasRequiredTarget(mappedData, "ALLINVENTORYENTRIES.LIST[*]/STOCKITEMNAME")
                && !HasRequiredTarget(mappedData, "SALESLEDGERNAME"))
            {
                hints.Add("For Sales, map inventory lines to ALLINVENTORYENTRIES.LIST[*]/... or set SALESLEDGERNAME default.");
            }

            if ((voucherType ?? string.Empty).Trim().Equals("purchase", StringComparison.OrdinalIgnoreCase)
                && !HasRequiredTarget(mappedData, "ALLINVENTORYENTRIES.LIST[*]/STOCKITEMNAME")
                && !HasRequiredTarget(mappedData, "PURCHASELEDGERNAME"))
            {
                hints.Add("For Purchase, map inventory lines to ALLINVENTORYENTRIES.LIST[*]/... or set PURCHASELEDGERNAME default.");
            }

            AppendTallyLineErrorHints(responseXml, hints);

            if (hints.Count == 0)
            {
                return string.Empty;
            }

            return string.Join(" ", hints.Distinct(StringComparer.OrdinalIgnoreCase));
        }

        private static void AppendTallyLineErrorHints(string responseXml, List<string> hints)
        {
            if (string.IsNullOrWhiteSpace(responseXml) || hints == null)
            {
                return;
            }

            List<string> lineErrors;
            try
            {
                var doc = XDocument.Parse(responseXml);
                lineErrors = doc.Descendants("LINEERROR").Select(x => x.Value).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            }
            catch
            {
                return;
            }

            if (lineErrors.Count == 0)
            {
                return;
            }

            var all = string.Join(" | ", lineErrors).ToLowerInvariant();

            if (all.Contains("ledger") && all.Contains("does not exist"))
            {
                hints.Add("Ledger master missing: update company template defaults or create ledger masters in Tally.");
            }

            if (all.Contains("stock item") && all.Contains("does not exist"))
            {
                hints.Add("Stock item master missing: map STOCKITEMNAME correctly and create missing stock items in company.");
            }

            if (all.Contains("date") && (all.Contains("missing") || all.Contains("out of range") || all.Contains("period")))
            {
                hints.Add("Date issue: ensure DATE maps to yyyyMMdd and falls inside company financial period.");
            }

            if (all.Contains("gst") || all.Contains("tax"))
            {
                hints.Add("Tax/GST issue: add required GST target mappings/defaults per company configuration.");
            }

            if (all.Contains("amount") && (all.Contains("mismatch") || all.Contains("balance")))
            {
                hints.Add("Amount balancing issue: ensure ledger entry amounts balance and signs match voucher rules.");
            }
        }

        private static bool IsDateRelatedFailure(string responseXml)
        {
            if (string.IsNullOrWhiteSpace(responseXml))
            {
                return false;
            }

            var message = responseXml.ToLowerInvariant();
            return message.Contains("voucher date is missing")
                || message.Contains("date is out of range")
                || message.Contains("invalid date")
                || message.Contains("period") && message.Contains("date");
        }

        private static bool IsSalesOrPurchase(string voucherType)
        {
            if (string.IsNullOrWhiteSpace(voucherType))
            {
                return false;
            }

            var normalized = voucherType.Trim().ToLowerInvariant();
            return normalized == "sales" || normalized == "purchase";
        }

        private CompanyTemplateProfile ResolveCompanyTemplate(MappingDefinition mapping)
        {
            if (mapping == null || mapping.CompanyTemplates == null || mapping.CompanyTemplates.Count == 0)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(CompanyName))
            {
                CompanyTemplateProfile profile;
                if (mapping.CompanyTemplates.TryGetValue(CompanyName.Trim(), out profile))
                {
                    return profile;
                }

                var wanted = NormalizeCompanyKey(CompanyName);
                foreach (var pair in mapping.CompanyTemplates)
                {
                    if (NormalizeCompanyKey(pair.Key).Equals(wanted, StringComparison.OrdinalIgnoreCase))
                    {
                        return pair.Value;
                    }
                }
            }

            CompanyTemplateProfile wildcardProfile;
            if (mapping.CompanyTemplates.TryGetValue("*", out wildcardProfile))
            {
                return wildcardProfile;
            }

            return null;
        }

        private static void ApplyTemplateDefaults(Dictionary<string, object> mappedData, CompanyTemplateProfile templateProfile)
        {
            if (mappedData == null || templateProfile == null || templateProfile.Defaults == null)
            {
                return;
            }

            foreach (var item in templateProfile.Defaults)
            {
                object current;
                if (mappedData.TryGetValue(item.Key, out current))
                {
                    var text = Convert.ToString(current, CultureInfo.InvariantCulture);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        continue;
                    }
                }

                mappedData[item.Key] = item.Value;
            }
        }

        private static List<string> ValidateRequiredTargets(Dictionary<string, object> mappedData, CompanyTemplateProfile templateProfile)
        {
            var missing = new List<string>();
            if (mappedData == null || templateProfile == null || templateProfile.RequiredTargets == null)
            {
                return missing;
            }

            var canAutoGenerateLedgerEntries = CanAutoGenerateLedgerEntries(mappedData);

            foreach (var requiredTarget in templateProfile.RequiredTargets)
            {
                if (string.IsNullOrWhiteSpace(requiredTarget))
                {
                    continue;
                }

                // If explicit ledger entries are not mapped but party+amount is present,
                // generator can auto-create ALLLEDGERENTRIES.LIST nodes.
                if (canAutoGenerateLedgerEntries
                    && requiredTarget.IndexOf("ALLLEDGERENTRIES", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }

                if (!HasRequiredTarget(mappedData, requiredTarget))
                {
                    missing.Add(requiredTarget);
                }
            }

            return missing;
        }

        private static bool HasRequiredTarget(Dictionary<string, object> mappedData, string requiredTarget)
        {
            if (requiredTarget.IndexOf("[*]", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var regexPattern = "^" + Regex.Escape(Canonicalize(requiredTarget)).Replace("\\[\\*\\]", "\\[[0-9]+\\]") + "$";
                var regex = new Regex(regexPattern, RegexOptions.IgnoreCase);

                foreach (var key in mappedData.Keys)
                {
                    if (!regex.IsMatch(Canonicalize(key)))
                    {
                        continue;
                    }

                    var value = Convert.ToString(mappedData[key], CultureInfo.InvariantCulture);
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return true;
                    }
                }

                return false;
            }

            var wanted = Canonicalize(requiredTarget);
            foreach (var key in mappedData.Keys)
            {
                if (!Canonicalize(key).Equals(wanted, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var value = Convert.ToString(mappedData[key], CultureInfo.InvariantCulture);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return true;
                }
            }

            return false;
        }

        private static string Canonicalize(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            var value = key.Trim();
            value = value.Replace('\\', '/').Replace('.', '/').Replace('-', '/');
            while (value.Contains("//"))
            {
                value = value.Replace("//", "/");
            }

            return value.ToLowerInvariant();
        }

        private static string NormalizeCompanyKey(string company)
        {
            if (string.IsNullOrWhiteSpace(company))
            {
                return string.Empty;
            }

            var value = company.Trim().ToLowerInvariant();
            value = value.Replace(" ", string.Empty).Replace("-", string.Empty).Replace("_", string.Empty);
            return value;
        }

        private static bool CanAutoGenerateLedgerEntries(Dictionary<string, object> mappedData)
        {
            if (mappedData == null || mappedData.Count == 0)
            {
                return false;
            }

            var hasParty = HasRequiredTarget(mappedData, "PARTYLEDGERNAME");
            var hasAmount = HasRequiredTarget(mappedData, "AMOUNT")
                || HasRequiredTarget(mappedData, "ALLINVENTORYENTRIES.LIST[*]/AMOUNT");

            var hasExplicitLedgerEntries = false;
            foreach (var key in mappedData.Keys)
            {
                if (key.IndexOf("ALLLEDGERENTRIES", StringComparison.OrdinalIgnoreCase) >= 0
                    || key.IndexOf("LEDGERENTRIES", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var value = Convert.ToString(mappedData[key], CultureInfo.InvariantCulture);
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        hasExplicitLedgerEntries = true;
                        break;
                    }
                }
            }

            return hasParty && hasAmount && !hasExplicitLedgerEntries;
        }

        private bool TryCreateMissingLedgers(string responseXml)
        {
            var ledgers = ExtractMissingLedgers(responseXml);
            if (ledgers.Count == 0)
            {
                return false;
            }

            foreach (var ledgerName in ledgers)
            {
                var parentGroup = ResolveLedgerParentGroup(ledgerName);
                var masterXml = BuildCreateLedgerXml(ledgerName, parentGroup);
                masterXml = ApplyCompanyContext(masterXml);
                _tallyConnector.Import(masterXml);
            }

            return true;
        }

        private static List<string> ExtractMissingLedgers(string responseXml)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(responseXml))
            {
                return list;
            }

            var doc = XDocument.Parse(responseXml);
            var lineErrors = doc.Descendants("LINEERROR").Select(x => x.Value).ToList();
            if (lineErrors.Count == 0)
            {
                return list;
            }

            var regex = new Regex("Ledger\\s+'([^']+)'\\s+does\\s+not\\s+exist", RegexOptions.IgnoreCase);
            foreach (var line in lineErrors)
            {
                var match = regex.Match(line);
                if (!match.Success)
                {
                    continue;
                }

                var ledger = match.Groups[1].Value.Trim();
                if (ledger.Length > 0 && !list.Contains(ledger, StringComparer.OrdinalIgnoreCase))
                {
                    list.Add(ledger);
                }
            }

            return list;
        }

        private static string ResolveLedgerParentGroup(string ledgerName)
        {
            if (string.Equals(ledgerName, "Sales", StringComparison.OrdinalIgnoreCase))
            {
                return "Sales Accounts";
            }

            if (string.Equals(ledgerName, "Sales A/c", StringComparison.OrdinalIgnoreCase))
            {
                return "Sales Accounts";
            }

            if (string.Equals(ledgerName, "Purchase", StringComparison.OrdinalIgnoreCase))
            {
                return "Purchase Accounts";
            }

            if (string.Equals(ledgerName, "Purchase A/c", StringComparison.OrdinalIgnoreCase))
            {
                return "Purchase Accounts";
            }

            if (string.Equals(ledgerName, "Cash", StringComparison.OrdinalIgnoreCase))
            {
                return "Cash-in-Hand";
            }

            return "Sundry Debtors";
        }

        private static string BuildCreateLedgerXml(string ledgerName, string parentGroup)
        {
            var envelope = new XDocument(
                new XElement("ENVELOPE",
                    new XElement("HEADER",
                        new XElement("TALLYREQUEST", "Import Data")),
                    new XElement("BODY",
                        new XElement("IMPORTDATA",
                            new XElement("REQUESTDESC",
                                new XElement("REPORTNAME", "All Masters")),
                            new XElement("REQUESTDATA",
                                new XElement("TALLYMESSAGE",
                                    new XAttribute(XNamespace.Xmlns + "UDF", "TallyUDF"),
                                    new XElement("LEDGER",
                                        new XAttribute("NAME", ledgerName),
                                        new XAttribute("ACTION", "Create"),
                                        new XElement("NAME", ledgerName),
                                        new XElement("PARENT", parentGroup),
                                        new XElement("ISBILLWISEON", "Yes"),
                                        new XElement("AFFECTSSTOCK", "No"),
                                        new XElement("OPENINGBALANCE", 0m.ToString("0.00", CultureInfo.InvariantCulture)))))))));

            return envelope.ToString(SaveOptions.DisableFormatting);
        }

        private string ApplyCompanyContext(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml) || string.IsNullOrWhiteSpace(CompanyName))
            {
                return xml;
            }

            var document = XDocument.Parse(xml);
            var requestDesc = document.Descendants("REQUESTDESC").FirstOrDefault();
            if (requestDesc == null)
            {
                return xml;
            }

            var staticVariables = requestDesc.Element("STATICVARIABLES");
            if (staticVariables == null)
            {
                staticVariables = new XElement("STATICVARIABLES");
                requestDesc.AddFirst(staticVariables);
            }

            var currentCompany = staticVariables.Element("SVCURRENTCOMPANY");
            if (currentCompany == null)
            {
                currentCompany = new XElement("SVCURRENTCOMPANY", CompanyName);
                staticVariables.Add(currentCompany);
            }
            else
            {
                currentCompany.Value = CompanyName;
            }

            return document.ToString(SaveOptions.DisableFormatting);
        }
    }
}
