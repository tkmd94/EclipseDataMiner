using System;
using VMS.TPS.Common.Model.Types;

namespace EclipseDataMiner.Helpers
{
    /// <summary>
    /// Helper for dose unit normalization (standardizing to Gy).
    /// </summary>
    public static class DoseNormalizationHelper
    {
        /// <summary>
        /// Extension method to safely retrieve dose value in Gy from an ESAPI DoseValue object.
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
            // Fallback for undefined or custom units
            return doseValue.Dose;
        }

        /// <summary>
        /// Safely converts numeric dose and unit string ("cGy", "Gy") to Gy.
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
        /// Converts a Gy value to the specified target display unit (Gy or cGy).
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
