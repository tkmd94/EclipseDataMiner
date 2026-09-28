using System;
using System.Security.Cryptography;
using System.Text;

namespace EclipseDataMiner.Helpers
{
    /// <summary>
    /// Helper for string sanitization, CSV escaping, and de-identification.
    /// </summary>
    public static class StringSanitizer
    {
        public const string NotApplicable = "N/A";
        public const string Redacted = "REDACTED";

        /// <summary>
        /// Sanitizes text for CSV cells (replaces newlines with spaces, escapes quotes, wraps in quotes if needed).
        /// </summary>
        public static string EscapeCsv(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            // Replace newlines with spaces
            string sanitized = input.Replace("\r\n", " ")
                                    .Replace("\n", " ")
                                    .Replace("\r", " ");

            bool containsComma = sanitized.IndexOf(',') >= 0;
            bool containsQuote = sanitized.IndexOf('"') >= 0;
            bool containsTab = sanitized.IndexOf('\t') >= 0;

            if (containsQuote)
            {
                sanitized = sanitized.Replace("\"", "\"\"");
            }

            if (containsComma || containsQuote || containsTab)
            {
                sanitized = $"\"{sanitized}\"";
            }

            return sanitized;
        }

        /// <summary>
        /// Generates a SHA-256 hashed de-identified string from a Patient ID.
        /// </summary>
        public static string AnonymizePatientId(string patientId, string salt = "EclipseDataMiner_Anonymizer")
        {
            if (string.IsNullOrEmpty(patientId))
            {
                return string.Empty;
            }

            using (var sha256 = CreateSha256())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(patientId + salt);
                byte[] hash = sha256.ComputeHash(bytes);
                var sb = new StringBuilder();
                for (int i = 0; i < hash.Length; i++)
                {
                    sb.Append(hash[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// Creates a FIPS-compliant SHA-256 instance (SHA256CryptoServiceProvider / SHA256Cng)
        /// with graceful fallback to standard SHA256.Create().
        /// In .NET Framework, SHA256.Create() defaults to SHA256Managed which throws
        /// InvalidOperationException on Windows systems with FIPS enforcement enabled.
        /// </summary>
        public static SHA256 CreateSha256()
        {
            try
            {
                return new SHA256CryptoServiceProvider();
            }
            catch
            {
                try
                {
                    return new SHA256Cng();
                }
                catch
                {
                    return SHA256.Create();
                }
            }
        }

        /// <summary>
        /// Masks personal identifiable information (name, birth date, approver, etc.).
        /// </summary>
        public static string MaskPersonalData(string input, bool mask = true)
        {
            if (!mask) return input ?? string.Empty;
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return Redacted;
        }

        /// <summary>
        /// Returns "N/A" if the string value is null or whitespace.
        /// </summary>
        public static string ValueOrNA(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? NotApplicable : value;
        }

        /// <summary>
        /// Formats nullable value types (returns "N/A" if null).
        /// </summary>
        public static string ValueOrNA<T>(T? value, string format = null) where T : struct, IFormattable
        {
            if (!value.HasValue) return NotApplicable;
            return string.IsNullOrEmpty(format) ? value.Value.ToString() : value.Value.ToString(format, null);
        }
    }
}
