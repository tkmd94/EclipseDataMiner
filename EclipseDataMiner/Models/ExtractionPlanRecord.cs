using System;
using System.Collections.Generic;

namespace EclipseDataMiner.Models
{
    /// <summary>
    /// 1つの治療計画（PlanSetup または PlanSum）から抽出された全データを保持するDTO
    /// </summary>
    public class ExtractionPlanRecord
    {
        // 識別情報
        public string PatientId { get; set; } = string.Empty;
        public string CourseId { get; set; } = string.Empty;
        public string PlanId { get; set; } = string.Empty;
        public bool IsPlanSum { get; set; }
        public DateTime? DateOfBirth { get; set; }

        // 処方・計画基本情報
        public string TargetVolumeId { get; set; } = string.Empty;
        public double? DosePerFractionGy { get; set; }
        public int? NumberOfFractions { get; set; }
        public double? TotalDoseGy { get; set; }
        public int NumberOfBeams { get; set; }
        public string ApprovalStatus { get; set; } = string.Empty;
        public string PlanningApprover { get; set; } = string.Empty;
        public string PlanningApprovalDate { get; set; } = string.Empty;

        // 計画メタデータ
        public string CalculationModelPhoton { get; set; } = string.Empty;
        public string CalculationModelElectron { get; set; } = string.Empty;
        public string PlanNormalizationMethod { get; set; } = string.Empty;
        public string ClinicalProtocolSummary { get; set; } = string.Empty;

        // ビーム一覧
        public List<BeamRecord> Beams { get; set; } = new List<BeamRecord>();

        // 最適化設定一覧
        public List<OptimizationObjectiveRecord> OptimizationObjectives { get; set; } = new List<OptimizationObjectiveRecord>();

        // DVH・DQP統計一覧
        public List<DvhMetricResult> DvhMetrics { get; set; } = new List<DvhMetricResult>();

        // 計算ログ
        public List<string> CalculationLogs { get; set; } = new List<string>();

        // プラン複雑性サマリー文字列 (BeamID:MCS,EM,LeafTravel,ArcLength)
        public string PlanComplexitySummary { get; set; } = string.Empty;
    }
}
