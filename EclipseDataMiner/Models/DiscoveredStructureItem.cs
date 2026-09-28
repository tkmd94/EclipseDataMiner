using CommunityToolkit.Mvvm.ComponentModel;

namespace EclipseDataMiner.Models
{
    /// <summary>
    /// Preview model for raw structures discovered during pre-scan and mapping rule application results.
    /// </summary>
    public class DiscoveredStructureItem : ObservableObject
    {
        private string _rawStructureId = string.Empty;
        /// <summary>
        /// Raw structure ID discovered from the database.
        /// </summary>
        public string RawStructureId
        {
            get => _rawStructureId;
            set => SetProperty(ref _rawStructureId, value);
        }

        private int _hitCount = 0;
        /// <summary>
        /// Number of matched plans containing this structure.
        /// </summary>
        public int HitCount
        {
            get => _hitCount;
            set => SetProperty(ref _hitCount, value);
        }

        private string _resolvedAlias = string.Empty;
        /// <summary>
        /// Resolved target alias after mapping rules are applied (original ID if unmapped).
        /// </summary>
        public string ResolvedAlias
        {
            get => _resolvedAlias;
            set => SetProperty(ref _resolvedAlias, value);
        }

        private bool _isExtracted = true;
        /// <summary>
        /// Extraction flag (false if excluded by a rule).
        /// </summary>
        public bool IsExtracted
        {
            get => _isExtracted;
            set => SetProperty(ref _isExtracted, value);
        }

        private string _matchStatus = "Unmapped (Raw)";
        /// <summary>
        /// Match status description (e.g. Mapped (Regex -> PTV), Excluded, Unmapped (Raw)).
        /// </summary>
        public string MatchStatus
        {
            get => _matchStatus;
            set => SetProperty(ref _matchStatus, value);
        }
    }
}
