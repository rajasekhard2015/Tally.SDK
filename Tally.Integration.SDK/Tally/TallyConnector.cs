using System;
using System.IO;
using System.Net;
using System.Text;

namespace Tally.Integration.SDK.Tally
{
    /// <summary>
    /// Handles HTTP communication with Tally's XML API.
    /// </summary>
    public class TallyConnector
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TallyConnector"/> class.
        /// </summary>
        public TallyConnector()
            : this("http://localhost:9000", 30000)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TallyConnector"/> class with custom settings.
        /// </summary>
        /// <param name="baseUrl">Tally endpoint URL.</param>
        /// <param name="timeoutMs">Request timeout in milliseconds.</param>
        public TallyConnector(string baseUrl, int timeoutMs)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new ArgumentException("Base URL cannot be empty.", "baseUrl");
            }

            if (timeoutMs <= 0)
            {
                throw new ArgumentOutOfRangeException("timeoutMs", "Timeout must be greater than zero.");
            }

            BaseUrl = baseUrl;
            TimeoutMs = timeoutMs;
        }

        /// <summary>
        /// Gets or sets the Tally base URL.
        /// </summary>
        public string BaseUrl { get; set; }

        /// <summary>
        /// Gets or sets the HTTP timeout in milliseconds.
        /// </summary>
        public int TimeoutMs { get; set; }

        /// <summary>
        /// Imports XML payload into Tally.
        /// </summary>
        /// <param name="xml">Tally XML envelope.</param>
        /// <returns>Tally response XML.</returns>
        public string Import(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
            {
                throw new ArgumentException("XML payload cannot be empty.", "xml");
            }

            var request = (HttpWebRequest)WebRequest.Create(BaseUrl);
            request.Method = "POST";
            request.ContentType = "application/xml";
            request.Timeout = TimeoutMs;

            var bytes = Encoding.UTF8.GetBytes(xml);
            request.ContentLength = bytes.Length;

            using (var stream = request.GetRequestStream())
            {
                stream.Write(bytes, 0, bytes.Length);
            }

            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                using (var responseStream = response.GetResponseStream())
                using (var reader = new StreamReader(responseStream))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (WebException ex)
            {
                var serverMessage = ReadWebExceptionBody(ex);
                throw new InvalidOperationException("Tally import failed. " + serverMessage, ex);
            }
        }

        /// <summary>
        /// Checks whether Tally endpoint is reachable.
        /// </summary>
        /// <returns>True when Tally responds successfully.</returns>
        public bool TestConnection()
        {
            var pingEnvelope = "<ENVELOPE><HEADER><TALLYREQUEST>Export Data</TALLYREQUEST></HEADER><BODY><DESC><STATICVARIABLES></STATICVARIABLES><TDL><TDLMESSAGE><COLLECTION NAME='Company Collection'><TYPE>Company</TYPE></COLLECTION></TDLMESSAGE></TDL></DESC></BODY></ENVELOPE>";

            try
            {
                var response = Import(pingEnvelope);
                return !string.IsNullOrWhiteSpace(response);
            }
            catch
            {
                return false;
            }
        }

        private static string ReadWebExceptionBody(WebException ex)
        {
            if (ex == null || ex.Response == null)
            {
                return string.Empty;
            }

            using (var stream = ex.Response.GetResponseStream())
            {
                if (stream == null)
                {
                    return string.Empty;
                }

                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
