using System;
using System.Collections.Generic;

namespace Tally.Integration.SDK.Models
{
    /// <summary>
    /// Represents the result of an import operation to Tally.
    /// </summary>
    public class ImportResult
    {
        /// <summary>
        /// Gets or sets whether the import operation succeeded.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the number of records created by Tally.
        /// </summary>
        public int Created { get; set; }

        /// <summary>
        /// Gets or sets the number of records altered by Tally.
        /// </summary>
        public int Altered { get; set; }

        /// <summary>
        /// Gets or sets the number of records deleted by Tally.
        /// </summary>
        public int Deleted { get; set; }

        /// <summary>
        /// Gets or sets the number of errors returned by Tally.
        /// </summary>
        public int Errors { get; set; }

        /// <summary>
        /// Gets or sets the raw response XML from Tally.
        /// </summary>
        public string ResponseXml { get; set; }

        /// <summary>
        /// Gets or sets an error message when an exception occurs.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the final mapped object that is prepared for Tally.
        /// </summary>
        public Dictionary<string, object> RequestObject { get; set; }

        /// <summary>
        /// Gets or sets the final Tally XML request envelope.
        /// </summary>
        public string RequestXml { get; set; }

        /// <summary>
        /// Gets or sets whether the SDK attempted to push data to Tally.
        /// </summary>
        public bool PushToTallyAttempted { get; set; }

        /// <summary>
        /// Merges another import result into the current result.
        /// </summary>
        /// <param name="other">The result to merge.</param>
        public void Merge(ImportResult other)
        {
            if (other == null)
            {
                return;
            }

            Created += other.Created;
            Altered += other.Altered;
            Deleted += other.Deleted;
            Errors += other.Errors;
            Success = Success && other.Success;

            if (!string.IsNullOrWhiteSpace(other.ErrorMessage))
            {
                ErrorMessage = string.IsNullOrWhiteSpace(ErrorMessage)
                    ? other.ErrorMessage
                    : ErrorMessage + Environment.NewLine + other.ErrorMessage;
            }

            if (!string.IsNullOrWhiteSpace(other.ResponseXml))
            {
                ResponseXml = other.ResponseXml;
            }

            if (!string.IsNullOrWhiteSpace(other.RequestXml))
            {
                RequestXml = other.RequestXml;
            }

            if (other.RequestObject != null)
            {
                RequestObject = other.RequestObject;
            }

            PushToTallyAttempted = PushToTallyAttempted || other.PushToTallyAttempted;
        }

        /// <summary>
        /// Creates a failed result from an exception.
        /// </summary>
        /// <param name="ex">The exception.</param>
        /// <returns>A failed import result.</returns>
        public static ImportResult Fail(Exception ex)
        {
            return new ImportResult
            {
                Success = false,
                ErrorMessage = ex == null ? "Unknown import error." : ex.Message,
                Errors = 1
            };
        }
    }
}
