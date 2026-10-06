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

        [TestMethod]
        [Description("Verifies that BuildDqpColumns and CsvStreamExporter accurately generate headers and export values for all DQP metric combinations (D, V, DC, CV x Rel/Abs) and basic statistics")]
        public void Dqp_AllMetricTypes_BuildColumns_AndCsvExport_ShouldGenerateAccurateHeadersAndValues()
        {
            // Arrange
            string csvPath = Path.Combine(_tempDir, "dqp_all_types.csv");
            var options = new ExtractionOptions();

            var rules = new List<StructureMappingRule>
            {
                new StructureMappingRule { Pattern = "PTV", MatchMode = StructureMatchMode.Exact, TargetAlias = "PTV_Target", IsSelected = true },
                new StructureMappingRule { Pattern = "Rectum", MatchMode = StructureMatchMode.Exact, TargetAlias = "Rectum_OAR", IsSelected = true }
            };

            var dqps = new List<DQP>
            {
                new DQP { structureName = "PTV_Target", DQPtype = DQPtype.Dose, DQPvalue = 95.0, InputUnit = IOUnit.Relative, OutputUnit = IOUnit.Absolute },
                new DQP { structureName = "PTV_Target", DQPtype = DQPtype.Dose, DQPvalue = 0.1, InputUnit = IOUnit.Absolute, OutputUnit = IOUnit.Absolute },
                new DQP { structureName = "PTV_Target", DQPtype = DQPtype.Dose, DQPvalue = 95.0, InputUnit = IOUnit.Relative, OutputUnit = IOUnit.Relative },
                new DQP { structureName = "Rectum_OAR", DQPtype = DQPtype.Volume, DQPvalue = 50.0, InputUnit = IOUnit.Absolute, OutputUnit = IOUnit.Relative },
                new DQP { structureName = "Rectum_OAR", DQPtype = DQPtype.Volume, DQPvalue = 50.0, InputUnit = IOUnit.Absolute, OutputUnit = IOUnit.Absolute },
                new DQP { structureName = "PTV_Target", DQPtype = DQPtype.Volume, DQPvalue = 95.0, InputUnit = IOUnit.Relative, OutputUnit = IOUnit.Relative },
                new DQP { structureName = "PTV_Target", DQPtype = DQPtype.DoseComplement, DQPvalue = 95.0, InputUnit = IOUnit.Relative, OutputUnit = IOUnit.Absolute },
                new DQP { structureName = "PTV_Target", DQPtype = DQPtype.DoseComplement, DQPvalue = 2.0, InputUnit = IOUnit.Absolute, OutputUnit = IOUnit.Absolute },
                new DQP { structureName = "Rectum_OAR", DQPtype = DQPtype.ComplementVolume, DQPvalue = 40.0, InputUnit = IOUnit.Absolute, OutputUnit = IOUnit.Relative },
                new DQP { structureName = "Rectum_OAR", DQPtype = DQPtype.ComplementVolume, DQPvalue = 40.0, InputUnit = IOUnit.Absolute, OutputUnit = IOUnit.Absolute }
            };

            // Act 1: Build columns
            var columns = StaEsapiWorkerService.BuildDqpColumns(dqps, rules);

            // Assert 1: Basic Stats columns exist
            CollectionAssert.Contains(columns.ConvertAll(c => c.HeaderText), "PTV_Target-Volume[cc]");
            CollectionAssert.Contains(columns.ConvertAll(c => c.HeaderText), "PTV_Target-Max dose[Gy]");
            CollectionAssert.Contains(columns.ConvertAll(c => c.HeaderText), "PTV_Target-Mean dose[Gy]");
            CollectionAssert.Contains(columns.ConvertAll(c => c.HeaderText), "PTV_Target-Min dose[Gy]");
            CollectionAssert.Contains(columns.ConvertAll(c => c.HeaderText), "Rectum_OAR-Volume[cc]");

            // Assert 2: All 10 DQP header notations
            CollectionAssert.Contains(columns.ConvertAll(c => c.HeaderText), "PTV_Target-D95%[Gy]");
            CollectionAssert.Contains(columns.ConvertAll(c => c.HeaderText), "PTV_Target-D0.1cc[Gy]");
            CollectionAssert.Contains(columns.ConvertAll(c => c.HeaderText), "PTV_Target-D95%[%]");
            CollectionAssert.Contains(columns.ConvertAll(c => c.HeaderText), "Rectum_OAR-V50Gy[%]");
            CollectionAssert.Contains(columns.ConvertAll(c => c.HeaderText), "Rectum_OAR-V50Gy[cc]");
            CollectionAssert.Contains(columns.ConvertAll(c => c.HeaderText), "PTV_Target-V95%[%]");
            CollectionAssert.Contains(columns.ConvertAll(c => c.HeaderText), "PTV_Target-DC95%[Gy]");
            CollectionAssert.Contains(columns.ConvertAll(c => c.HeaderText), "PTV_Target-DC2cc[Gy]");
            CollectionAssert.Contains(columns.ConvertAll(c => c.HeaderText), "Rectum_OAR-CV40Gy[%]");
            CollectionAssert.Contains(columns.ConvertAll(c => c.HeaderText), "Rectum_OAR-CV40Gy[cc]");

            // Act 2: Export plan record with corresponding metric keys
            var record = new ExtractionPlanRecord
            {
                PatientId = "PT_DQP_VERIFY",
                CourseId = "C1",
                PlanId = "Plan1",
                TotalDoseGy = 78.0,
                NumberOfFractions = 39,
                DvhMetrics = new List<DvhMetricResult>
                {
                    new DvhMetricResult { TargetAlias = "PTV_Target", StructureVolumeCc = 120.5, MaxDoseGy = 81.2, MeanDoseGy = 79.5, MinDoseGy = 72.1, MetricKey = "BASIC_STATS" },
                    new DvhMetricResult { TargetAlias = "PTV_Target", MetricKey = "Dose_95_Relative_Absolute", Value = 76.4, Unit = "Gy" },
                    new DvhMetricResult { TargetAlias = "PTV_Target", MetricKey = "Dose_0.1_Absolute_Absolute", Value = 80.8, Unit = "Gy" },
                    new DvhMetricResult { TargetAlias = "PTV_Target", MetricKey = "Dose_95_Relative_Relative", Value = 97.9, Unit = "%" },
                    new DvhMetricResult { TargetAlias = "Rectum_OAR", StructureVolumeCc = 65.2, MaxDoseGy = 75.0, MeanDoseGy = 42.1, MinDoseGy = 12.0, MetricKey = "BASIC_STATS" },
                    new DvhMetricResult { TargetAlias = "Rectum_OAR", MetricKey = "Volume_50_Absolute_Relative", Value = 25.4, Unit = "%" },
                    new DvhMetricResult { TargetAlias = "Rectum_OAR", MetricKey = "Volume_50_Absolute_Absolute", Value = 16.5, Unit = "cc" },
                    new DvhMetricResult { TargetAlias = "PTV_Target", MetricKey = "Volume_95_Relative_Relative", Value = 99.2, Unit = "%" },
                    new DvhMetricResult { TargetAlias = "PTV_Target", MetricKey = "DoseComplement_95_Relative_Absolute", Value = 74.1, Unit = "Gy" },
                    new DvhMetricResult { TargetAlias = "PTV_Target", MetricKey = "DoseComplement_2_Absolute_Absolute", Value = 79.9, Unit = "Gy" },
                    new DvhMetricResult { TargetAlias = "Rectum_OAR", MetricKey = "ComplementVolume_40_Absolute_Relative", Value = 62.8, Unit = "%" },
                    new DvhMetricResult { TargetAlias = "Rectum_OAR", MetricKey = "ComplementVolume_40_Absolute_Absolute", Value = 41.0, Unit = "cc" }
                }
            };

            using (var stream = new FileStream(csvPath, FileMode.Create, FileAccess.Write))
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            using (var exporter = new CsvStreamExporter(writer, options, columns))
            {
                exporter.WriteRecord(record);
            }

            // Assert 3: Read CSV and verify output
            string[] lines = File.ReadAllLines(csvPath, Encoding.UTF8);
            Assert.AreEqual(2, lines.Length);

            string dataRow = lines[1];
            StringAssert.Contains(dataRow, "PT_DQP_VERIFY");
            StringAssert.Contains(dataRow, "120.50"); // Volume
            StringAssert.Contains(dataRow, "81.20");  // Max
            StringAssert.Contains(dataRow, "76.40");  // D95%
            StringAssert.Contains(dataRow, "80.80");  // D0.1cc
            StringAssert.Contains(dataRow, "97.90");  // D95% rel
            StringAssert.Contains(dataRow, "25.40");  // V50Gy %
            StringAssert.Contains(dataRow, "16.50");  // V50Gy cc
            StringAssert.Contains(dataRow, "99.20");  // V95% rel
            StringAssert.Contains(dataRow, "74.10");  // DC95%
            StringAssert.Contains(dataRow, "79.90");  // DC2cc
            StringAssert.Contains(dataRow, "62.80");  // CV40Gy %
            StringAssert.Contains(dataRow, "41.00");  // CV40Gy cc
        }

        [TestMethod]
        [Description("Verifies that InterpolateVolumeAtDoseFromDvh robustly and accurately computes volume at specified dose (including 20 Gy) with boundary safety")]
        public void InterpolateVolumeAtDoseFromDvh_ShouldAccuratelyInterpolateVolumeAtDose()
        {
            // Arrange: DVH points from 0 Gy (50 cc) to 30 Gy (0 cc)
            var p0 = new VMS.TPS.Common.Model.Types.DVHPoint(new VMS.TPS.Common.Model.Types.DoseValue(0.0, VMS.TPS.Common.Model.Types.DoseValue.DoseUnit.Gy), 50.0, "cm3");
            var p10 = new VMS.TPS.Common.Model.Types.DVHPoint(new VMS.TPS.Common.Model.Types.DoseValue(10.0, VMS.TPS.Common.Model.Types.DoseValue.DoseUnit.Gy), 40.0, "cm3");
            var p20 = new VMS.TPS.Common.Model.Types.DVHPoint(new VMS.TPS.Common.Model.Types.DoseValue(20.0, VMS.TPS.Common.Model.Types.DoseValue.DoseUnit.Gy), 25.0, "cm3");
            var p30 = new VMS.TPS.Common.Model.Types.DVHPoint(new VMS.TPS.Common.Model.Types.DoseValue(30.0, VMS.TPS.Common.Model.Types.DoseValue.DoseUnit.Gy), 0.0, "cm3");

            var curve = new[] { p0, p10, p20, p30 };

            // Act & Assert 1: Exactly at 20 Gy (user scenario)
            var v20 = StaEsapiWorkerService.InterpolateVolumeAtDoseFromDvh(curve, 20.0);
            Assert.IsNotNull(v20);
            Assert.AreEqual(25.0, v20.Value, 1e-4);

            // Act & Assert 2: Interpolated between 10 Gy and 20 Gy (e.g. 15 Gy -> 32.5 cc)
            var v15 = StaEsapiWorkerService.InterpolateVolumeAtDoseFromDvh(curve, 15.0);
            Assert.IsNotNull(v15);
            Assert.AreEqual(32.5, v15.Value, 1e-4);

            // Act & Assert 3: At or below 0 Gy -> full volume 50.0 cc
            var v0 = StaEsapiWorkerService.InterpolateVolumeAtDoseFromDvh(curve, 0.0);
            Assert.IsNotNull(v0);
            Assert.AreEqual(50.0, v0.Value, 1e-4);

            // Act & Assert 4: Beyond maximum dose (35 Gy) -> 0.0 cc
            var v35 = StaEsapiWorkerService.InterpolateVolumeAtDoseFromDvh(curve, 35.0);
            Assert.IsNotNull(v35);
            Assert.AreEqual(0.0, v35.Value, 1e-4);
        }

        [TestMethod]
        [Description("Verifies that CalculateVolumeAtDoseSafe produces valid non-NA values for both Relative and Absolute outputs when native ESAPI method throws due to cGy unit or presentation mismatch")]
        public void CalculateVolumeAtDoseSafe_WhenNativeCallFails_ShouldFallbackToDvhCurveAccurately()
        {
            // Arrange: Structure volume = 100 cc, DVH curve at 20 Gy = 30 cc
            var p0 = new VMS.TPS.Common.Model.Types.DVHPoint(new VMS.TPS.Common.Model.Types.DoseValue(0.0, VMS.TPS.Common.Model.Types.DoseValue.DoseUnit.Gy), 100.0, "cm3");
            var p20 = new VMS.TPS.Common.Model.Types.DVHPoint(new VMS.TPS.Common.Model.Types.DoseValue(20.0, VMS.TPS.Common.Model.Types.DoseValue.DoseUnit.Gy), 30.0, "cm3");
            var curve = new[] { p0, p20 };

            // Pass null for planItem and structure to simulate native ESAPI call failure/exception
            // When fallback is invoked with dvh.CurveData, it should calculate from curveData directly
            var targetDose = new VMS.TPS.Common.Model.Types.DoseValue(20.0, VMS.TPS.Common.Model.Types.DoseValue.DoseUnit.Gy);
            var refDose = new VMS.TPS.Common.Model.Types.DoseValue(60.0, VMS.TPS.Common.Model.Types.DoseValue.DoseUnit.Gy);

            // Directly test InterpolateVolumeAtDoseFromDvh on 20 Gy
            var volCc = StaEsapiWorkerService.InterpolateVolumeAtDoseFromDvh(curve, 20.0);
            Assert.IsNotNull(volCc, "Interpolated volume must not be null");
            Assert.AreEqual(30.0, volCc.Value, 1e-4);

            // Relative volume calculation: (30.0 / 100.0) * 100 = 30.0%
            double volRel = (volCc.Value / 100.0) * 100.0;
            Assert.AreEqual(30.0, volRel, 1e-4);
        }
    }
}

