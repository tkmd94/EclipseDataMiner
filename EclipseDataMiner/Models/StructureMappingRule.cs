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
    /// 輪郭の事前マッピング・エイリアスルールモデル
    /// </summary>
    public class StructureMappingRule : ObservableObject
    {
        private bool _isSelected = true;
        /// <summary>
        /// 抽出対象フラグ（false の場合はオプトアウト・除外）
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        private string _pattern = string.Empty;
        /// <summary>
        /// 一致条件パターン（Structure ID または 正規表現）
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
        /// マッチング方式（完全一致、部分一致、正規表現）
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
        /// 正規表現の構文エラーが存在するか
        /// </summary>
        public bool IsRegexError
        {
            get => _isRegexError;
            private set => SetProperty(ref _isRegexError, value);
        }

        private string _regexErrorMessage = string.Empty;
        /// <summary>
        /// 正規表現構文エラーの詳細メッセージ
        /// </summary>
        public string RegexErrorMessage
        {
            get => _regexErrorMessage;
            private set => SetProperty(ref _regexErrorMessage, value);
        }

        private string _targetAlias = string.Empty;
        /// <summary>
        /// 抽出時統合名（Target Alias）。空欄の場合は元のStructure IDを使用
        /// </summary>
        public string TargetAlias
        {
            get => _targetAlias;
            set => SetProperty(ref _targetAlias, value);
        }

        private int _matchedCount = 0;
        /// <summary>
        /// 事前スキャンで検出されたヒット件数
        /// </summary>
        public int MatchedCount
        {
            get => _matchedCount;
            set => SetProperty(ref _matchedCount, value);
        }

        /// <summary>
        /// 正規表現の構文妥当性を検証
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
        /// 指定された Structure.Id が本ルールに適合するかを判定
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
