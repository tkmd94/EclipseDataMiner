using System;
using System.Collections.Generic;

namespace EclipseDataMiner.Models
{
    /// <summary>
    /// DTO holding all data extracted from a single treatment plan (PlanSetup or PlanSum).
    /// </summary>
    public class ExtractionPlanRecord
    {
        // Identification information
        public string PatientId { get; set; } = string.Empty;
        public string CourseId { get; set; } = string.Empty;
        public string PlanId { get; set; } = string.Empty;
        public bool IsPlanSum { get; set; }
        public DateTime? DateOfBirth { get; set; }

        // Prescription and basic plan details
        public string TargetVolumeId { get; set; } = string.Empty;
        public double? DosePerFractionGy { get; set; }
        public int? NumberOfFractions { get; set; }
        public double? TotalDoseGy { get; set; }
        public int NumberOfBeams { get; set; }
        public string ApprovalStatus { get; set; } = string.Empty;
        public string PlanningApprover { get; set; } = string.Empty;
        public string PlanningApprovalDate { get; set; } = string.Empty;

        // Plan metadata
        public string CalculationModelPhoton { get; set; } = string.Empty;
        public string CalculationModelElectron { get; set; } = string.Empty;
        public string PlanNormalizationMethod { get; set; } = string.Empty;
        public string ClinicalProtocolSummary { get; set; } = string.Empty;

        // Beam list
        public List<BeamRecord> Beams { get; set; } = new List<BeamRecord>();

        // Optimization objective list
        public List<OptimizationObjectiveRecord> OptimizationObjectives { get; set; } = new List<OptimizationObjectiveRecord>();

        // DVH and DQP metric list
        public List<DvhMetricResult> DvhMetrics { get; set; } = new List<DvhMetricResult>();

        // Calculation logs
        public List<string> CalculationLogs { get; set; } = new List<string>();

        // Plan complexity summary string (BeamID:MCS,EM,LeafTravel,ArcLength)
        public string PlanComplexitySummary { get; set; } = string.Empty;
    }
}
