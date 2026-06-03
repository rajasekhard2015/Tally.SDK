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

            return result;
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
