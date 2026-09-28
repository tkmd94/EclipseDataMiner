using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EclipseDataMiner.Models;
using EclipseDataMiner.Services;

namespace EclipseDataMiner.Tests
{
    [TestClass]
    public class StreamingExportTests
    {
        private string _tempDir;

        [TestInitialize]
        public void Setup()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "EclipseDataMiner_Tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_tempDir))
            {
                try { Directory.Delete(_tempDir, true); } catch { }
            }
        }

        [TestMethod]
        [Description("Verifies that CsvStreamExporter writes header and data rows properly in flat format")]
        public void CsvStreamExporter_ShouldWriteCorrectFlatRow()
        {
            // Arrange
            string csvPath = Path.Combine(_tempDir, "output.csv");
            var options = new ExtractionOptions
            {
                ExportBeamMU = true,
                ExportBeamMachineEnergyTech = true,
                ExportOptimizationObjectives = true
            };

            var dqpColumns = new List<DqpColumnDefinition>
            {
                new DqpColumnDefinition { StructureIdentifier = "PTV", MetricKey = "D95%", HeaderText = "PTV-D95%[Gy]" }
            };

            var record = new ExtractionPlanRecord
            {
                PatientId = "PT_001",
                CourseId = "C1",
                PlanId = "Plan1",
                DateOfBirth = new DateTime(1965, 5, 20),
                TargetVolumeId = "PTV",
                DosePerFractionGy = 2.0,
                NumberOfFractions = 30,
                TotalDoseGy = 60.0,
                NumberOfBeams = 2,
                ApprovalStatus = "TreatmentApproved",
                IsPlanSum = false,
                Beams = new List<BeamRecord>
                {
                    new BeamRecord { BeamId = "B1", MetersetMU = 150.5, TreatmentUnit = "TrueBeam", EnergyModeDisplayName = "6X", Technique = "ARC", MlcPlanType = "VMAT" },
                    new BeamRecord { BeamId = "B2", MetersetMU = 140.2, TreatmentUnit = "TrueBeam", EnergyModeDisplayName = "6X", Technique = "ARC", MlcPlanType = "VMAT" }
                },
                OptimizationObjectives = new List<OptimizationObjectiveRecord>
                {
                    new OptimizationObjectiveRecord { ObjectiveType = "Point", StructureId = "PTV", DoseGy = 60.0, Volume = 95.0, Priority = 100 }
                },
                DvhMetrics = new List<DvhMetricResult>
                {
                    new DvhMetricResult { TargetAlias = "PTV", MetricKey = "D95%", Value = 59.8, Unit = "Gy" }
                }
            };

            // Act
            using (var stream = new FileStream(csvPath, FileMode.Create, FileAccess.Write))
            using (var writer = new StreamWriter(stream))
            using (var exporter = new CsvStreamExporter(writer, options, dqpColumns))
            {
                exporter.WriteRecord(record);
            }

            // Assert
            string[] lines = File.ReadAllLines(csvPath);
            Assert.AreEqual(2, lines.Length); // Header + 1 Data Row

            string header = lines[0];
            StringAssert.Contains(header, "Patient ID");
            StringAssert.Contains(header, "MU");
            StringAssert.Contains(header, "PTV-D95%[Gy]");

            string dataRow = lines[1];
            StringAssert.Contains(dataRow, "PT_001");
            StringAssert.Contains(dataRow, "60.00");
            StringAssert.Contains(dataRow, "B1:150.5;B2:140.2");
            StringAssert.Contains(dataRow, "TrueBeam:6X:ARC:VMAT;TrueBeam:6X:ARC:VMAT");
            StringAssert.Contains(dataRow, "59.80");
        }

        [TestMethod]
        [Description("Verifies that patient ID is hashed and date of birth is REDACTED when anonymization option is enabled")]
        public void CsvStreamExporter_WhenAnonymizeEnabled_ShouldMaskPersonalData()
        {
            // Arrange
            string csvPath = Path.Combine(_tempDir, "anonymized.csv");
            var options = new ExtractionOptions
            {
                AnonymizeOutput = true,
                ExportPlanningApprover = true
            };

            var record = new ExtractionPlanRecord
            {
                PatientId = "SECRET_ID_999",
                CourseId = "C1",
                PlanId = "Plan1",
                DateOfBirth = new DateTime(1970, 1, 1),
                PlanningApprover = "Dr. Yamada"
            };

            // Act
            using (var stream = new FileStream(csvPath, FileMode.Create, FileAccess.Write))
            using (var writer = new StreamWriter(stream))
            using (var exporter = new CsvStreamExporter(writer, options))
            {
                exporter.WriteRecord(record);
            }

            // Assert
            string[] lines = File.ReadAllLines(csvPath);
            string dataRow = lines[1];

            Assert.IsFalse(dataRow.Contains("SECRET_ID_999"), "Raw patient ID must not be exported");
            Assert.IsFalse(dataRow.Contains("1970-01-01"), "Date of birth must not be exported");
            Assert.IsFalse(dataRow.Contains("Dr. Yamada"), "Approver name must not be exported");
            StringAssert.Contains(dataRow, "REDACTED");
        }

        [TestMethod]
        [Description("Verifies that JsonlStreamExporter writes one plan per line as valid streaming JSON")]
        public void JsonlStreamExporter_ShouldWriteValidJsonLine()
        {
            // Arrange
            string jsonlPath = Path.Combine(_tempDir, "output.jsonl");
            var options = new ExtractionOptions();

            var record = new ExtractionPlanRecord
            {
                PatientId = "PT_JSON_01",
                CourseId = "Course1",
                PlanId = "VMAT_Plan",
                TotalDoseGy = 78.0,
                NumberOfFractions = 39,
                Beams = new List<BeamRecord>
                {
                    new BeamRecord { BeamId = "Beam1", MetersetMU = 250.0 }
                }
            };

            // Act
            using (var stream = new FileStream(jsonlPath, FileMode.Create, FileAccess.Write))
            using (var writer = new StreamWriter(stream))
            using (var exporter = new JsonlStreamExporter(writer, options))
            {
                exporter.WriteRecord(record);
            }

            // Assert
            string[] lines = File.ReadAllLines(jsonlPath);
            Assert.AreEqual(1, lines.Length);

            // Verify JSON parsing
            using (var doc = JsonDocument.Parse(lines[0]))
            {
                var root = doc.RootElement;
                Assert.AreEqual("PT_JSON_01", root.GetProperty("PatientId").GetString());
                Assert.AreEqual(78.0, root.GetProperty("TotalDoseGy").GetDouble(), 1e-4);
                Assert.AreEqual("Beam1", root.GetProperty("Beams")[0].GetProperty("BeamId").GetString());
            }
        }

        [TestMethod]
        [Description("Verifies that basic structure statistics (Volume, Max, Mean, Min) and DQP metrics are properly exported side-by-side")]
        public void CsvStreamExporter_WithBasicStatsAndDqp_ShouldWriteAllColumnsCorrectly()
        {
            // Arrange
            string csvPath = Path.Combine(_tempDir, "stats_and_dqp.csv");
            var options = new ExtractionOptions();

            var dqpColumns = new List<DqpColumnDefinition>
            {
                new DqpColumnDefinition { StructureIdentifier = "PTV", MetricKey = "BASIC_STATS", HeaderText = "PTV-Volume[cc]", ColumnType = DqpColumnType.BasicVolume },
                new DqpColumnDefinition { StructureIdentifier = "PTV", MetricKey = "BASIC_STATS", HeaderText = "PTV-Max dose[Gy]", ColumnType = DqpColumnType.BasicMaxDose },
                new DqpColumnDefinition { StructureIdentifier = "PTV", MetricKey = "BASIC_STATS", HeaderText = "PTV-Mean dose[Gy]", ColumnType = DqpColumnType.BasicMeanDose },
                new DqpColumnDefinition { StructureIdentifier = "PTV", MetricKey = "BASIC_STATS", HeaderText = "PTV-Min dose[Gy]", ColumnType = DqpColumnType.BasicMinDose },
                new DqpColumnDefinition { StructureIdentifier = "PTV", MetricKey = "Dose_95_Relative_Absolute", HeaderText = "PTV-D95%[Gy]", ColumnType = DqpColumnType.DqpMetric }
            };

            var record = new ExtractionPlanRecord
            {
                PatientId = "PT_STAT_01",
                CourseId = "C1",
                PlanId = "P1",
                TotalDoseGy = 70.0,
                NumberOfFractions = 35,
                DvhMetrics = new List<DvhMetricResult>
                {
                    new DvhMetricResult
                    {
                        TargetAlias = "PTV",
                        StructureVolumeCc = 45.67,
                        MaxDoseGy = 74.20,
                        MeanDoseGy = 71.50,
                        MinDoseGy = 65.30,
                        MetricKey = "BASIC_STATS"
                    },
                    new DvhMetricResult
                    {
                        TargetAlias = "PTV",
                        MetricKey = "Dose_95_Relative_Absolute",
                        Value = 68.90,
                        Unit = "Gy"
                    }
                }
            };

            // Act
            using (var stream = new FileStream(csvPath, FileMode.Create, FileAccess.Write))
            using (var writer = new StreamWriter(stream))
            using (var exporter = new CsvStreamExporter(writer, options, dqpColumns))
            {
                exporter.WriteRecord(record);
            }

            // Assert
            string[] lines = File.ReadAllLines(csvPath);
            Assert.AreEqual(2, lines.Length);

            string header = lines[0];
            StringAssert.Contains(header, "PTV-Volume[cc]");
            StringAssert.Contains(header, "PTV-Max dose[Gy]");
            StringAssert.Contains(header, "PTV-Mean dose[Gy]");
            StringAssert.Contains(header, "PTV-Min dose[Gy]");
            StringAssert.Contains(header, "PTV-D95%[Gy]");

            string row = lines[1];
            StringAssert.Contains(row, "45.67");
            StringAssert.Contains(row, "74.20");
            StringAssert.Contains(row, "71.50");
            StringAssert.Contains(row, "65.30");
            StringAssert.Contains(row, "68.90");
        }

        [TestMethod]
        [Description("Verifies that CSV output is not corrupted when zero dose, special characters in structure names, or missing values are present")]
        public void CsvStreamExporter_WhenZeroDoseOrSpecialChars_ShouldFormatSafely()
        {
            // Arrange
            string csvPath = Path.Combine(_tempDir, "zero_and_special.csv");
            var options = new ExtractionOptions();

            var dqpColumns = new List<DqpColumnDefinition>
            {
                new DqpColumnDefinition { StructureIdentifier = "Parotid_L", MetricKey = "Dose_50", HeaderText = "Parotid_L-D50%[Gy]", ColumnType = DqpColumnType.DqpMetric },
                new DqpColumnDefinition { StructureIdentifier = "PTV+5mm", MetricKey = "V_20", HeaderText = "PTV+5mm-V20Gy[%]", ColumnType = DqpColumnType.DqpMetric }
            };

            var record = new ExtractionPlanRecord
            {
                PatientId = "PT_SPECIAL",
                CourseId = "C1",
                PlanId = "P_ZERO",
                TotalDoseGy = 0.0, // Dose 0 Gy
                DosePerFractionGy = 0.0,
                NumberOfFractions = 0,
                CalculationLogs = new List<string> { "Log with, comma \"quotes\" and\nnewline" },
                DvhMetrics = new List<DvhMetricResult>
                {
                    new DvhMetricResult { TargetAlias = "Parotid_L", MetricKey = "Dose_50", Value = null, Unit = "Gy" }, // Missing
                    new DvhMetricResult { TargetAlias = "PTV+5mm", MetricKey = "V_20", Value = 15.5, Unit = "%" }
                }
            };

            // Act
            using (var stream = new FileStream(csvPath, FileMode.Create, FileAccess.Write))
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            using (var exporter = new CsvStreamExporter(writer, options, dqpColumns))
            {
                exporter.WriteRecord(record);
            }

            // Assert
            string[] lines = File.ReadAllLines(csvPath, Encoding.UTF8);
            Assert.AreEqual(2, lines.Length);

            string row = lines[1];
            // Missing values are N/A
            StringAssert.Contains(row, "N/A");
            // Valid values are formatted
            StringAssert.Contains(row, "15.50");
            // 0.00 Gy
            StringAssert.Contains(row, "0.00");
        }

        [TestMethod]
        [Description("Verifies that plan records with special characters and null properties serialize into valid JSONL lines")]
        public void JsonlStreamExporter_WhenSpecialCharsAndNulls_ShouldProduceValidJson()
        {
            // Arrange
            string jsonlPath = Path.Combine(_tempDir, "special.jsonl");
            var options = new ExtractionOptions();

            var record = new ExtractionPlanRecord
            {
                PatientId = "PT_JSON_UNICODE",
                CourseId = "C1",
                PlanId = "Plan<1>",
                CalculationLogs = new List<string> { "line1\r\nline2\t\"quoted\"" },
                Beams = new List<BeamRecord>
                {
                    new BeamRecord { BeamId = "Beam:1", Technique = "VMAT (Arc)", TreatmentUnit = "TrueBeam", MetersetMU = 450.2 }
                }
            };

            // Act
            using (var stream = new FileStream(jsonlPath, FileMode.Create, FileAccess.Write))
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            using (var exporter = new JsonlStreamExporter(writer, options))
            {
                exporter.WriteRecord(record);
            }

            // Assert
            string[] lines = File.ReadAllLines(jsonlPath, Encoding.UTF8);
            Assert.AreEqual(1, lines.Length);

            // Verify parseable by System.Text.Json.JsonDocument
            using (var doc = JsonDocument.Parse(lines[0]))
            {
                var root = doc.RootElement;
                Assert.AreEqual("PT_JSON_UNICODE", root.GetProperty("PatientId").GetString());
                Assert.AreEqual("Plan<1>", root.GetProperty("PlanId").GetString());
                var logs = root.GetProperty("CalculationLogs");
                Assert.IsTrue(logs[0].GetString().Contains("line1"));
                Assert.AreEqual(1, root.GetProperty("Beams").GetArrayLength());
            }
        }

        [TestMethod]
        [Description("Verifies that per-beam calculation logs (#B1#;LOG:0...;#B2#;LOG:0...) are exported to CSV when CalculationLog option is enabled")]
        public void CsvStreamExporter_WhenCalculationLogExportEnabled_ShouldExportAggregatedBeamLogsWithCorrectFormat()
        {
            // Arrange
            string csvPath = Path.Combine(_tempDir, "calclog_export.csv");
            var options = new ExtractionOptions
            {
                ExportCalculationLog = true
            };

            var record = new ExtractionPlanRecord
            {
                PatientId = "PT_CALC_01",
                CourseId = "C1",
                PlanId = "Prostate_VMAT",
                TotalDoseGy = 60.0,
                DosePerFractionGy = 3.0,
                NumberOfFractions = 20,
                NumberOfBeams = 2,
                CalculationLogs = new List<string>
                {
                    "#B1#",
                    "LOG:0Information: Imaging Device: ID=Def_CTScanner",
                    "LOG:1Information: Service: AcurosXB",
                    "#B2#",
                    "LOG:0Information: Imaging Device: ID=Def_CTScanner",
                    "LOG:1Information: Service: Photon_Optimizer"
                }
            };

            // Act
            using (var stream = new FileStream(csvPath, FileMode.Create, FileAccess.Write))
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            using (var exporter = new CsvStreamExporter(writer, options, new List<DqpColumnDefinition>()))
            {
                exporter.WriteRecord(record);
            }

            // Assert
            string[] lines = File.ReadAllLines(csvPath, Encoding.UTF8);
            Assert.AreEqual(2, lines.Length);

            string header = lines[0];
            StringAssert.Contains(header, "CalculationLog");

            string row = lines[1];
            // Beam tags and log indices are joined with semicolons
            StringAssert.Contains(row, "#B1#;LOG:0Information: Imaging Device: ID=Def_CTScanner;LOG:1Information: Service: AcurosXB;#B2#;LOG:0Information: Imaging Device: ID=Def_CTScanner;LOG:1Information: Service: Photon_Optimizer");
        }

        [TestMethod]
        [Description("Verifies that N/A is exported when CalculationLogs is empty and CalculationLog option is enabled")]
        public void CsvStreamExporter_WhenCalculationLogEmpty_ShouldExportNA()
        {
            // Arrange
            string csvPath = Path.Combine(_tempDir, "calclog_empty.csv");
            var options = new ExtractionOptions
            {
                ExportCalculationLog = true
            };

            var record = new ExtractionPlanRecord
            {
                PatientId = "PT_EMPTY_LOG",
                CourseId = "C1",
                PlanId = "EmptyPlan",
                CalculationLogs = new List<string>()
            };

            // Act
            using (var stream = new FileStream(csvPath, FileMode.Create, FileAccess.Write))
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            using (var exporter = new CsvStreamExporter(writer, options, new List<DqpColumnDefinition>()))
            {
                exporter.WriteRecord(record);
            }

            // Assert
            string[] lines = File.ReadAllLines(csvPath, Encoding.UTF8);
            Assert.AreEqual(2, lines.Length);
            string row = lines[1];
            StringAssert.Contains(row, "N/A");
        }
    }
}
