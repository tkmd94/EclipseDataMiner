using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EclipseDataMiner.Models
{
    /// <summary>
    /// Search & Filter で検索・合致したプラン情報項目（選択可能プレビュー用）
    /// </summary>
    public class MatchedPlanItem : INotifyPropertyChanged
    {
        private bool _isSelected = true;

        /// <summary>
        /// データ抽出の対象とするかどうかのチェック状態
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public string PatientId { get; set; } = string.Empty;
        public string CourseId { get; set; } = string.Empty;
        public string PlanId { get; set; } = string.Empty;

        /// <summary>
        /// PlanSetup または PlanSum
        /// </summary>
        public string PlanType { get; set; } = "PlanSetup";

        /// <summary>
        /// Approval Status (Approved, Completed, UnApproved, PlanSum など)
        /// </summary>
        public string ApprovalStatus { get; set; } = string.Empty;

        public double? DosePerFraction { get; set; }
        public int? NumberOfFractions { get; set; }
        public double? TotalDose { get; set; }
        private string _machine = string.Empty;
        public string Machine
        {
            get => _machine;
            set => _machine = value ?? string.Empty;
        }

        public string PrimaryMachine
        {
            get => _machine;
            set => _machine = value ?? string.Empty;
        }

        private string _energy = string.Empty;
        public string Energy
        {
            get => _energy;
            set => _energy = value ?? string.Empty;
        }

        public string PrimaryEnergy
        {
            get => _energy;
            set => _energy = value ?? string.Empty;
        }

        private string _technique = string.Empty;
        public string Technique
        {
            get => _technique;
            set => _technique = value ?? string.Empty;
        }

        public string PrimaryTechnique
        {
            get => _technique;
            set => _technique = value ?? string.Empty;
        }

        public DateTime? TargetDate { get; set; }
        public string DateTargetLabel { get; set; } = string.Empty;

        /// <summary>
        /// 計画作成日 (CreationDate)
        /// </summary>
        public DateTime? CreationDate { get; set; }

        /// <summary>
        /// 計画承認日 (PlanningApprovalDate)
        /// </summary>
        public DateTime? PlanningApprovalDate { get; set; }

        /// <summary>
        /// 治療承認日 (TreatmentApprovalDate)
        /// </summary>
        public DateTime? TreatmentApprovalDate { get; set; }

        public string TargetVolumeId { get; set; } = string.Empty;

        /// <summary>
        /// 線量計算の有無 (true = 線量あり, false = 線量なし/未計算)
        /// </summary>
        public bool HasDose { get; set; }

        /// <summary>
        /// 患者ID、コースID、プランIDを一意に識別するキー (例: "12345|Course1|Prostate_VMAT")
        /// </summary>
        public string UniqueKey => $"{PatientId}|{CourseId}|{PlanId}";

        #region Formatted Display Properties

        public string FormattedTargetVolume => string.IsNullOrWhiteSpace(TargetVolumeId) ? "-" : TargetVolumeId;
        public string FormattedHasDose => HasDose ? "✔" : "—";
        public string FormattedDosePerFraction => DosePerFraction.HasValue && !double.IsNaN(DosePerFraction.Value) && !double.IsInfinity(DosePerFraction.Value) ? $"{DosePerFraction.Value:F2} Gy" : "-";
        public string FormattedNumberOfFractions => NumberOfFractions.HasValue && NumberOfFractions.Value > 0 ? NumberOfFractions.Value.ToString() : "-";
        public string FormattedTotalDose => TotalDose.HasValue && !double.IsNaN(TotalDose.Value) && !double.IsInfinity(TotalDose.Value) ? $"{TotalDose.Value:F2} Gy" : "-";
        public string FormattedTargetDate => TargetDate.HasValue ? TargetDate.Value.ToString("yyyy-MM-dd") : "-";
        public string FormattedCreationDate => CreationDate.HasValue ? CreationDate.Value.ToString("yyyy-MM-dd") : "-";
        public string FormattedPlanningApprovalDate => PlanningApprovalDate.HasValue ? PlanningApprovalDate.Value.ToString("yyyy-MM-dd") : "-";
        public string FormattedTreatmentApprovalDate => TreatmentApprovalDate.HasValue ? TreatmentApprovalDate.Value.ToString("yyyy-MM-dd") : "-";

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
