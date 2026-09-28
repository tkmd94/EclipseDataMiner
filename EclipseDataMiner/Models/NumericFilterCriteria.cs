using System;

namespace EclipseDataMiner.Models
{
    /// <summary>
    /// Numeric filter criteria (single value, range specifications 70-80 / 70~80, inequalities >=10 / <30).
    /// </summary>
    public class NumericFilterCriteria
    {
        public string RawText { get; set; } = string.Empty;
        public double? ExactValue { get; set; }
        public double? MinValue { get; set; }
        public bool MinInclusive { get; set; } = true;
        public double? MaxValue { get; set; }
        public bool MaxInclusive { get; set; } = true;

        /// <summary>
        /// Indicates whether no criteria are specified.
        /// </summary>
        public bool IsEmpty => !ExactValue.HasValue && !MinValue.HasValue && !MaxValue.HasValue;

        /// <summary>
        /// Parses numeric filter criteria from a string.
        /// </summary>
        public static NumericFilterCriteria Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return new NumericFilterCriteria();

            text = text.Trim();
            var criteria = new NumericFilterCriteria { RawText = text };

            // 1. Inequality checks (>=, <=, >, <)
            if (text.StartsWith(">="))
            {
                if (double.TryParse(text.Substring(2).Trim(), out double min))
                {
                    criteria.MinValue = min;
                    criteria.MinInclusive = true;
                    return criteria;
                }
            }
            else if (text.StartsWith("<="))
            {
                if (double.TryParse(text.Substring(2).Trim(), out double max))
                {
                    criteria.MaxValue = max;
                    criteria.MaxInclusive = true;
                    return criteria;
                }
            }
            else if (text.StartsWith(">"))
            {
                if (double.TryParse(text.Substring(1).Trim(), out double min))
                {
                    criteria.MinValue = min;
                    criteria.MinInclusive = false;
                    return criteria;
                }
            }
            else if (text.StartsWith("<"))
            {
                if (double.TryParse(text.Substring(1).Trim(), out double max))
                {
                    criteria.MaxValue = max;
                    criteria.MaxInclusive = false;
                    return criteria;
                }
            }

            // 2. Range specification (~ or -)
            int sepIndex = text.IndexOf('~');
            if (sepIndex < 0)
            {
                // Find '-' occurring after the first character
                sepIndex = text.IndexOf('-', 1);
            }

            if (sepIndex > 0)
            {
                string left = text.Substring(0, sepIndex).Trim();
                string right = text.Substring(sepIndex + 1).Trim();
                if (double.TryParse(left, out double min) && double.TryParse(right, out double max))
                {
                    criteria.MinValue = Math.Min(min, max);
                    criteria.MaxValue = Math.Max(min, max);
                    criteria.MinInclusive = true;
                    criteria.MaxInclusive = true;
                    return criteria;
                }
            }

            // 3. Single value (e.g. "78", "2.0")
            if (double.TryParse(text, out double val))
            {
                criteria.ExactValue = val;
                return criteria;
            }

            return criteria;
        }

        /// <summary>
        /// Matches against a double value (such as dose).
        /// </summary>
        public bool IsMatch(double? targetValue, double tolerance = 0.05)
        {
            if (IsEmpty) return true;
            if (!targetValue.HasValue) return false;

            double val = targetValue.Value;

            // Exclude NaN or Infinity (uncalculated/undefined dose values)
            if (double.IsNaN(val) || double.IsInfinity(val)) return false;

            if (ExactValue.HasValue)
            {
                return Math.Abs(val - ExactValue.Value) <= tolerance;
            }

            if (MinValue.HasValue)
            {
                if (MinInclusive)
                {
                    if (val < MinValue.Value - tolerance) return false;
                }
                else
                {
                    if (val <= MinValue.Value + tolerance) return false;
                }
            }

            if (MaxValue.HasValue)
            {
                if (MaxInclusive)
                {
                    if (val > MaxValue.Value + tolerance) return false;
                }
                else
                {
                    if (val >= MaxValue.Value - tolerance) return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Matches against an integer value (such as fraction count).
        /// </summary>
        public bool IsMatchInt(int? targetValue)
        {
            if (IsEmpty) return true;
            if (!targetValue.HasValue) return false;

            int val = targetValue.Value;
            if (val <= 0) return false;

            if (ExactValue.HasValue)
            {
                return val == (int)Math.Round(ExactValue.Value);
            }

            if (MinValue.HasValue)
            {
                if (MinInclusive)
                {
                    if (val < MinValue.Value) return false;
                }
                else
                {
                    if (val <= MinValue.Value) return false;
                }
            }

            if (MaxValue.HasValue)
            {
                if (MaxInclusive)
                {
                    if (val > MaxValue.Value) return false;
                }
                else
                {
                    if (val >= MaxValue.Value) return false;
                }
            }

            return true;
        }
    }
}
