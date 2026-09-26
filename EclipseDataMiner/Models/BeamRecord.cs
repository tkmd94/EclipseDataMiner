using System.Collections.Generic;

namespace EclipseDataMiner.Models
{
    /// <summary>
    /// ビーム（照射野）の抽出DTOレコード
    /// </summary>
    public class BeamRecord
    {
        public string BeamId { get; set; } = string.Empty;
        public bool IsSetupField { get; set; }
        public double MetersetMU { get; set; }
        public string TreatmentUnit { get; set; } = string.Empty;
        public string EnergyModeDisplayName { get; set; } = string.Empty;
        public string Technique { get; set; } = string.Empty;
        public string MlcPlanType { get; set; } = string.Empty;
        public double? ArcLength { get; set; }
        public double? ModulationComplexityScore { get; set; }
        public double? EdgeMetric { get; set; }
        public double? LeafTravelLength { get; set; }
        public List<string> CalculationLogs { get; set; } = new List<string>();

        /// <summary>
        /// 1セル集約用フォーマット文字列 (Unit:Energy:Tech:MLCType)
        /// </summary>
        public string ToMachineEnergySummary()
        {
            return $"{TreatmentUnit}:{EnergyModeDisplayName}:{Technique}:{MlcPlanType}";
        }
    }
}
