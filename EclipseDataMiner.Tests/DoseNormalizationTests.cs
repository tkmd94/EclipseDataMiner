using Microsoft.VisualStudio.TestTools.UnitTesting;
using EclipseDataMiner.Helpers;
using VMS.TPS.Common.Model.Types;

namespace EclipseDataMiner.Tests
{
    [TestClass]
    public class DoseNormalizationTests
    {
        [TestMethod]
        [Description("Verifies that DoseValue in cGy is automatically converted to Gy (e.g. 6000 cGy -> 60.0 Gy)")]
        public void ToGy_WhenUnitIsCGy_ShouldConvertToGy()
        {
            // Arrange
            var doseValue = new DoseValue(6000.0, DoseValue.DoseUnit.cGy);

            // Act
            double result = doseValue.ToGy();

            // Assert
            Assert.AreEqual(60.0, result, 1e-6);
        }

        [TestMethod]
        [Description("Verifies that DoseValue in Gy keeps its original value (e.g. 70.0 Gy -> 70.0 Gy)")]
        public void ToGy_WhenUnitIsGy_ShouldKeepSameValue()
        {
            // Arrange
            var doseValue = new DoseValue(70.0, DoseValue.DoseUnit.Gy);

            // Act
            double result = doseValue.ToGy();

            // Assert
            Assert.AreEqual(70.0, result, 1e-6);
        }

        [TestMethod]
        [Description("Verifies string-based unit conversion logic in ToGy(double, string)")]
        public void ToGy_StringOverload_ShouldConvertCorrectly()
        {
            // Arrange & Act
            double fromCgy = DoseNormalizationHelper.ToGy(200.0, "cGy");
            double fromGy = DoseNormalizationHelper.ToGy(2.0, "Gy");

            // Assert
            Assert.AreEqual(2.0, fromCgy, 1e-6);
            Assert.AreEqual(2.0, fromGy, 1e-6);
        }

        [TestMethod]
        [Description("Verifies inverse conversion from Gy to target unit (FromGy)")]
        public void FromGy_WhenTargetIsCGy_ShouldMultiplyBy100()
        {
            // Arrange & Act
            double cgy = DoseNormalizationHelper.FromGy(60.0, "cGy");
            double gy = DoseNormalizationHelper.FromGy(60.0, "Gy");

            // Assert
            Assert.AreEqual(6000.0, cgy, 1e-6);
            Assert.AreEqual(60.0, gy, 1e-6);
        }
    }
}
