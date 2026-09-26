using System;
using System.Security.Cryptography;
using System.Text;

namespace EclipseDataMiner.Helpers
{
    /// <summary>
    /// 文字列サニタイズ・CSVエスケープ・匿名化ヘルパー
    /// </summary>
    public static class StringSanitizer
    {
        public const string NotApplicable = "N/A";
        public const string Redacted = "REDACTED";

        /// <summary>
        /// CSVセル用にサニタイズ（改行をスペース置換、引用符エスケープ、カンマ含有時の囲み）
        /// </summary>
        public static string EscapeCsv(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            // 改行を半角スペースに置換
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
        /// 患者IDを SHA-256 でハッシュ化して匿名化文字列を生成
        /// </summary>
        public static string AnonymizePatientId(string patientId, string salt = "EclipseDataMiner_Anonymizer")
        {
            if (string.IsNullOrEmpty(patientId))
            {
                return string.Empty;
            }

            using (var sha256 = SHA256.Create())
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
        /// 個人情報（氏名、生年月日、承認者名等）のマスキング
        /// </summary>
        public static string MaskPersonalData(string input, bool mask = true)
        {
            if (!mask) return input ?? string.Empty;
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return Redacted;
        }

        /// <summary>
        /// 欠損値（null / 空白）の場合に "N/A" を返却
        /// </summary>
        public static string ValueOrNA(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? NotApplicable : value;
        }

        /// <summary>
        /// Nullable数値のフォーマット（null の場合は "N/A"）
        /// </summary>
        public static string ValueOrNA<T>(T? value, string format = null) where T : struct, IFormattable
        {
            if (!value.HasValue) return NotApplicable;
            return string.IsNullOrEmpty(format) ? value.Value.ToString() : value.Value.ToString(format, null);
        }
    }
}
