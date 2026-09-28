using System;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;

namespace EclipseDataMiner.Models
{
    public enum StructureMatchMode
    {
        Exact,
        Contains,
        Regex
    }

    /// <summary>
    /// Pre-mapping and alias rule model for structures.
    /// </summary>
    public class StructureMappingRule : ObservableObject
    {
        private bool _isSelected = true;
        /// <summary>
        /// Extraction flag (false indicates opt-out / exclusion).
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        private string _pattern = string.Empty;
        /// <summary>
        /// Matching pattern condition (Structure ID or Regular Expression).
        /// </summary>
        public string Pattern
        {
            get => _pattern;
            set
            {
                if (SetProperty(ref _pattern, value))
                {
                    ValidateRegex();
                }
            }
        }

        private StructureMatchMode _matchMode = StructureMatchMode.Exact;
        /// <summary>
        /// Matching mode (Exact match, Partial match, Regular Expression).
        /// </summary>
        public StructureMatchMode MatchMode
        {
            get => _matchMode;
            set
            {
                if (SetProperty(ref _matchMode, value))
                {
                    ValidateRegex();
                }
            }
        }

        private bool _isRegexError = false;
        /// <summary>
        /// Indicates whether a regex syntax error exists.
        /// </summary>
        public bool IsRegexError
        {
            get => _isRegexError;
            private set => SetProperty(ref _isRegexError, value);
        }

        private string _regexErrorMessage = string.Empty;
        /// <summary>
        /// Detailed syntax error message for regular expression.
        /// </summary>
        public string RegexErrorMessage
        {
            get => _regexErrorMessage;
            private set => SetProperty(ref _regexErrorMessage, value);
        }

        private string _targetAlias = string.Empty;
        /// <summary>
        /// Target alias name upon extraction. If empty, the original Structure ID is preserved.
        /// </summary>
        public string TargetAlias
        {
            get => _targetAlias;
            set => SetProperty(ref _targetAlias, value);
        }

        private int _matchedCount = 0;
        /// <summary>
        /// Hit count discovered during pre-scan.
        /// </summary>
        public int MatchedCount
        {
            get => _matchedCount;
            set => SetProperty(ref _matchedCount, value);
        }

        /// <summary>
        /// Validates syntax correctness for regular expressions.
        /// </summary>
        public void ValidateRegex()
        {
            if (MatchMode != StructureMatchMode.Regex || string.IsNullOrEmpty(Pattern))
            {
                IsRegexError = false;
                RegexErrorMessage = string.Empty;
                return;
            }

            try
            {
                var _ = new Regex(Pattern, RegexOptions.IgnoreCase);
                IsRegexError = false;
                RegexErrorMessage = string.Empty;
            }
            catch (ArgumentException ex)
            {
                IsRegexError = true;
                RegexErrorMessage = ex.Message;
            }
        }

        /// <summary>
        /// Determines whether the given Structure.Id matches this rule.
        /// </summary>
        public bool IsMatch(string structureId)
        {
            if (string.IsNullOrEmpty(structureId) || string.IsNullOrEmpty(Pattern))
            {
                return false;
            }

            switch (MatchMode)
            {
                case StructureMatchMode.Exact:
                    return string.Equals(structureId, Pattern, StringComparison.OrdinalIgnoreCase);

                case StructureMatchMode.Contains:
                    return structureId.IndexOf(Pattern, StringComparison.OrdinalIgnoreCase) >= 0;

                case StructureMatchMode.Regex:
                    try
                    {
                        return Regex.IsMatch(structureId, Pattern, RegexOptions.IgnoreCase);
                    }
                    catch
                    {
                        return false;
                    }

                default:
                    return false;
            }
        }
    }
}
