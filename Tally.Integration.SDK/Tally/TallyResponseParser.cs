using System;
using System.Linq;
using System.Xml.Linq;
using Tally.Integration.SDK.Models;

namespace Tally.Integration.SDK.Tally
{
    /// <summary>
    /// Parses Tally import response XML.
    /// </summary>
    public class TallyResponseParser
    {
        /// <summary>
        /// Parses Tally response XML into import result metrics.
        /// </summary>
        /// <param name="responseXml">Raw response XML.</param>
        /// <returns>Parsed import result.</returns>
        public ImportResult Parse(string responseXml)
        {
            if (string.IsNullOrWhiteSpace(responseXml))
            {
                return new ImportResult
                {
                    Success = false,
                    Errors = 1,
                    ErrorMessage = "Empty response from Tally."
                };
            }

            var result = new ImportResult
            {
                ResponseXml = responseXml
            };

            try
            {
                var doc = XDocument.Parse(responseXml);
                result.Created = ReadInt(doc, "CREATED");
                result.Altered = ReadInt(doc, "ALTERED");
                result.Deleted = ReadInt(doc, "DELETED");
                result.Errors = ReadInt(doc, "ERRORS");
                var ignored = ReadInt(doc, "IGNORED");
                var exceptions = ReadInt(doc, "EXCEPTIONS");
                var lineError = ReadString(doc, "LINEERROR");
                result.Success = result.Errors == 0 && ignored == 0 && exceptions == 0 && string.IsNullOrWhiteSpace(lineError);

                if (!result.Success)
                {
                    result.ErrorMessage = !string.IsNullOrWhiteSpace(lineError)
                        ? lineError
                    : "Tally reported ignored/error/exception records during import.";
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Errors = Math.Max(1, result.Errors);
                result.ErrorMessage = "Failed to parse Tally response: " + ex.Message;
            }

            return result;
        }

        private static int ReadInt(XDocument doc, string elementName)
        {
            var element = doc.Root == null ? null : doc.Root.Descendants(elementName).FirstOrDefault();
            if (element == null)
            {
                return 0;
            }

            int value;
            return int.TryParse(element.Value, out value) ? value : 0;
        }

        private static string ReadString(XDocument doc, string elementName)
        {
            var element = doc.Root == null ? null : doc.Root.Descendants(elementName).FirstOrDefault();
            return element == null ? string.Empty : element.Value;
        }
    }
}
