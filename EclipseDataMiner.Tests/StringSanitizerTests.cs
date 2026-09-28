using Microsoft.VisualStudio.TestTools.UnitTesting;
using EclipseDataMiner.Helpers;

namespace EclipseDataMiner.Tests
{
    [TestClass]
    public class StringSanitizerTests
    {
        [TestMethod]
        [Description("Verifies that newlines (CRLF, LF) are replaced with spaces in CSV escaping")]
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
        [Description("Verifies that strings containing commas are wrapped in double quotes")]
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
        [Description("Verifies that double quotes inside strings are escaped by doubling them")]
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
        [Description("Verifies that SHA-256 patient ID anonymization is deterministic for identical inputs")]
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
        [Description("Verifies that personal data masking returns REDACTED")]
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
        [Description("Verifies that missing values (null / whitespace) return N/A")]
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
