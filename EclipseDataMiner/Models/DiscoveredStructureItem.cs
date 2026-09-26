using CommunityToolkit.Mvvm.ComponentModel;

namespace EclipseDataMiner.Models
{
    /// <summary>
    /// 事前スキャンで検出された生輪郭とルール適用結果プレビューモデル
    /// </summary>
    public class DiscoveredStructureItem : ObservableObject
    {
        private string _rawStructureId = string.Empty;
        /// <summary>
        /// データベースから検出された生輪郭ID
        /// </summary>
        public string RawStructureId
        {
            get => _rawStructureId;
            set => SetProperty(ref _rawStructureId, value);
        }

        private int _hitCount = 0;
        /// <summary>
        /// 該当輪郭を保持するプランの検出件数
        /// </summary>
        public int HitCount
        {
            get => _hitCount;
            set => SetProperty(ref _hitCount, value);
        }

        private string _resolvedAlias = string.Empty;
        /// <summary>
        /// マッピングルール適用後の解決先 Alias（未マッピング時は元の ID）
        /// </summary>
        public string ResolvedAlias
        {
            get => _resolvedAlias;
            set => SetProperty(ref _resolvedAlias, value);
        }

        private bool _isExtracted = true;
        /// <summary>
        /// 抽出対象フラグ（ルールにより除外されている場合は false）
        /// </summary>
        public bool IsExtracted
        {
            get => _isExtracted;
            set => SetProperty(ref _isExtracted, value);
        }

        private string _matchStatus = "Unmapped (Raw)";
        /// <summary>
        /// ルール適合ステータス表記（例: Mapped (Regex -> PTV), Excluded, Unmapped (Raw)）
        /// </summary>
        public string MatchStatus
        {
            get => _matchStatus;
            set => SetProperty(ref _matchStatus, value);
        }
    }
}
