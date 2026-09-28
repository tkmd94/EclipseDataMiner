using System;
using System.Text.Json.Serialization;

namespace EclipseDataMiner.Models
{
    /// <summary>
    /// Search and filtering criteria preset model.
    /// </summary>
    public class SearchPreset
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsBuiltIn { get; set; } = false;

        /// <summary>
        /// Destination file full path for saving presets (excluded from JSON serialization).
        /// </summary>
        [JsonIgnore]
        public string FilePath { get; set; } = string.Empty;

        public string PatientIdText { get; set; } = string.Empty;
        public TextMatchMode PatientIdMatchMode { get; set; } = TextMatchMode.Contains;

        public string CourseIdText { get; set; } = string.Empty;
        public TextMatchMode CourseIdMatchMode { get; set; } = TextMatchMode.Contains;

        public string PlanIdText { get; set; } = string.Empty;
        public TextMatchMode PlanIdMatchMode { get; set; } = TextMatchMode.Contains;

        public string TargetVolumeIdText { get; set; } = string.Empty;
        public TextMatchMode TargetVolumeMatchMode { get; set; } = TextMatchMode.Contains;

        public string DosePerFractionText { get; set; } = string.Empty;
        public string NumberOfFractionsText { get; set; } = string.Empty;
        public string TotalDoseText { get; set; } = string.Empty;

        // Advanced metadata filters
        public string MachineFilterText { get; set; } = string.Empty;
        public string EnergyFilterText { get; set; } = string.Empty;
        public string TechniqueFilterText { get; set; } = string.Empty;
        public DateFilterTarget DateTarget { get; set; } = DateFilterTarget.TreatmentApprovalDate;
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }

        public bool FilterUnapproved { get; set; } = true;
        public bool FilterPlanApproved { get; set; } = true;
        public bool FilterTreatmentApproved { get; set; } = true;

        public bool GlobalLogicIsAnd { get; set; } = true;
        public bool IncludePlanSums { get; set; } = false;
        public DosePresenceFilter DosePresence { get; set; } = DosePresenceFilter.All;

        public override string ToString()
        {
            return Name;
        }
    }
}
