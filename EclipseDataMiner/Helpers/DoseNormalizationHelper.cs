using System;
using VMS.TPS.Common.Model.Types;

namespace EclipseDataMiner.Helpers
{
    /// <summary>
    /// 線量値の単位正規化（Gy統一）ヘルパー
    /// </summary>
    public static class DoseNormalizationHelper
    {
        /// <summary>
        /// ESAPI DoseValue から安全に Gy 値を取得する拡張メソッド
        /// </summary>
        public static double ToGy(this DoseValue doseValue)
        {
            if (doseValue.Unit == DoseValue.DoseUnit.cGy)
            {
                return doseValue.Dose / 100.0;
            }
            if (doseValue.Unit == DoseValue.DoseUnit.Gy)
            {
                return doseValue.Dose;
            }
            // 未定義または特殊単位の場合
            return doseValue.Dose;
        }

        /// <summary>
        /// 数値と単位文字列（"cGy", "Gy"）から安全に Gy 値を取得
        /// </summary>
        public static double ToGy(double dose, string unit)
        {
            if (string.Equals(unit, "cGy", StringComparison.OrdinalIgnoreCase))
            {
                return dose / 100.0;
            }
            return dose;
        }

        /// <summary>
        /// Gy 値を指定した表示単位（Gy または cGy）に換算
        /// </summary>
        public static double FromGy(double doseGy, string targetUnit)
        {
            if (string.Equals(targetUnit, "cGy", StringComparison.OrdinalIgnoreCase))
            {
                return doseGy * 100.0;
            }
            return doseGy;
        }
    }
}
