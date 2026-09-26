using Microsoft.VisualStudio.TestTools.UnitTesting;
using EclipseDataMiner.Helpers;

namespace EclipseDataMiner.Tests
{
    [TestClass]
    public class StringSanitizerTests
    {
        [TestMethod]
        [Description("改行コード(CRLF, LF)が半角スペースに置換されることを検証")]
        public void EscapeCsv_ShouldReplaceNewlinesWithSpace()
        {
            // Arrange
            string log = "Line1\r\nLine2\nLine3\rLine4";

            // Act
            string result = StringSanitizer.EscapeCsv(log);

            // Assert
            Assert.AreEqual("Line1 Line2 Line3 Line4", result);
        }

        [TestMethod]
        [Description("カンマを含む文字列がダブルクォーテーションで囲まれることを検証")]
        public void EscapeCsv_ShouldWrapWithQuotes_WhenCommaPresent()
        {
            // Arrange
            string input = "MachineA,6X,VMAT";

            // Act
            string result = StringSanitizer.EscapeCsv(input);

            // Assert
            Assert.AreEqual("\"MachineA,6X,VMAT\"", result);
        }

        [TestMethod]
        [Description("ダブルクォーテーションを含む文字列がエスケープ（2重化）されることを検証")]
        public void EscapeCsv_ShouldEscapeDoubleQuotes()
        {
            // Arrange
            string input = "Note: \"High Priority\" field";

            // Act
            string result = StringSanitizer.EscapeCsv(input);

            // Assert
            Assert.AreEqual("\"Note: \"\"High Priority\"\" field\"", result);
        }

        [TestMethod]
        [Description("SHA-256 ハッシュ化による患者ID匿名化が同一入力で決定論的であることを検証")]
        public void AnonymizePatientId_ShouldProduceConsistentHash()
        {
            // Arrange
            string patId = "12345678";

            // Act
            string hash1 = StringSanitizer.AnonymizePatientId(patId);
            string hash2 = StringSanitizer.AnonymizePatientId(patId);

            // Assert
            Assert.IsFalse(string.IsNullOrEmpty(hash1));
            Assert.AreEqual(64, hash1.Length); // SHA-256 hex is 64 chars
            Assert.AreEqual(hash1, hash2);
            Assert.AreNotEqual(patId, hash1);
        }

        [TestMethod]
        [Description("個人情報マスキングが REDACTED を返却することを検証")]
        public void MaskPersonalData_ShouldReturnRedacted()
        {
            // Arrange
            string approver = "Dr. Yamada";

            // Act
            string result = StringSanitizer.MaskPersonalData(approver, mask: true);

            // Assert
            Assert.AreEqual("REDACTED", result);
        }

        [TestMethod]
        [Description("欠損値（null / 空白）の場合に N/A が返却されることを検証")]
        public void ValueOrNA_ShouldReturnNA_WhenNullOrEmpty()
        {
            // Act & Assert
            Assert.AreEqual("N/A", StringSanitizer.ValueOrNA(null));
            Assert.AreEqual("N/A", StringSanitizer.ValueOrNA("   "));
            Assert.AreEqual("ExistingValue", StringSanitizer.ValueOrNA("ExistingValue"));

            double? nullDouble = null;
            double? validDouble = 60.5;
            Assert.AreEqual("N/A", StringSanitizer.ValueOrNA(nullDouble));
            Assert.AreEqual("60.50", StringSanitizer.ValueOrNA(validDouble, "F2"));
        }
    }
}
