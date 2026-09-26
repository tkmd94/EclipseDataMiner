namespace EclipseDataMiner.Models
{
    /// <summary>
    /// DVH統計およびDQP算出結果のDTOレコード
    /// </summary>
    public class DvhMetricResult
    {
        /// <summary>
        /// ESAPI上の元の輪郭ID
        /// </summary>
        public string OriginalStructureId { get; set; } = string.Empty;

        /// <summary>
        /// 抽出時統合名（Target Alias）。未指定時は OriginalStructureId
        /// </summary>
        public string TargetAlias { get; set; } = string.Empty;

        /// <summary>
        /// 輪郭体積 [cc]
        /// </summary>
        public double? StructureVolumeCc { get; set; }

        /// <summary>
        /// 最大線量 [Gy]
        /// </summary>
        public double? MaxDoseGy { get; set; }

        /// <summary>
        /// 平均線量 [Gy]
        /// </summary>
        public double? MeanDoseGy { get; set; }

        /// <summary>
        /// 最小線量 [Gy]
        /// </summary>
        public double? MinDoseGy { get; set; }

        /// <summary>
        /// 指標キー（例: D95%, V20Gy, MeanDose 等）
        /// </summary>
        public string MetricKey { get; set; } = string.Empty;

        /// <summary>
        /// 算出された数値（Gyまたはccまたは%）
        /// </summary>
        public double? Value { get; set; }

        /// <summary>
        /// 単位表示（Gy, cc, % 等）
        /// </summary>
        public string Unit { get; set; } = string.Empty;
    }
}
