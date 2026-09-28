namespace EclipseDataMiner.Models
{
    /// <summary>
    /// Extraction DTO record for optimization parameters and objective functions.
    /// </summary>
    public class OptimizationObjectiveRecord
    {
        public string ObjectiveType { get; set; } = string.Empty;
        public string StructureId { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public double? DoseGy { get; set; }
        public double? Volume { get; set; }
        public double Priority { get; set; }
        public double? ParameterA { get; set; }

        // Normal Tissue Objective (NTO) specific parameters
        public bool IsNTO { get; set; }
        public double? DistanceFromTargetBorderInMM { get; set; }
        public double? StartDosePercentage { get; set; }
        public double? EndDosePercentage { get; set; }
        public double? FallOff { get; set; }
        public bool? IsAutomatic { get; set; }

        /// <summary>
        /// Formatted summary string for single CSV cell entry.
        /// </summary>
        public string ToSummaryString()
        {
            if (IsNTO)
            {
                return $"NTO,Dist:{DistanceFromTargetBorderInMM},StartDose%:{StartDosePercentage},EndDose%:{EndDosePercentage},FallOff:{FallOff},Auto:{IsAutomatic},Prio:{Priority}";
            }
            string str = $"Type:{ObjectiveType},Struct:{StructureId},Op:{Operator},Dose:{DoseGy:F2}Gy,Prio:{Priority}";
            if (Volume.HasValue) str += $",Vol:{Volume.Value:F1}";
            if (ParameterA.HasValue) str += $",ParamA:{ParameterA.Value:F2}";
            return str;
        }
    }
}
