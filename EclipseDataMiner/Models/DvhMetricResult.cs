namespace EclipseDataMiner.Models
{
    /// <summary>
    /// DTO record for DVH metrics and dosimetric quality parameter calculation results.
    /// </summary>
    public class DvhMetricResult
    {
        /// <summary>
        /// Original structure ID in ESAPI.
        /// </summary>
        public string OriginalStructureId { get; set; } = string.Empty;

        /// <summary>
        /// Target alias name upon extraction. Defaults to OriginalStructureId if unspecified.
        /// </summary>
        public string TargetAlias { get; set; } = string.Empty;

        /// <summary>
        /// Structure volume [cc].
        /// </summary>
        public double? StructureVolumeCc { get; set; }

        /// <summary>
        /// Maximum dose [Gy].
        /// </summary>
        public double? MaxDoseGy { get; set; }

        /// <summary>
        /// Mean dose [Gy].
        /// </summary>
        public double? MeanDoseGy { get; set; }

        /// <summary>
        /// Minimum dose [Gy].
        /// </summary>
        public double? MinDoseGy { get; set; }

        /// <summary>
        /// Metric key (e.g. D95%, V20Gy, MeanDose, etc.).
        /// </summary>
        public string MetricKey { get; set; } = string.Empty;

        /// <summary>
        /// Calculated numeric value (Gy, cc, or %).
        /// </summary>
        public double? Value { get; set; }

        /// <summary>
        /// Unit label (Gy, cc, %, etc.).
        /// </summary>
        public string Unit { get; set; } = string.Empty;
    }
}
