using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using EclipseDataMiner.Helpers;
using EclipseDataMiner.Models;

namespace EclipseDataMiner.Services
{
    /// <summary>
    /// Types of DQP output columns.
    /// </summary>
    public enum DqpColumnType
    {
        DqpMetric,
        BasicVolume,
        BasicMaxDose,
        BasicMeanDose,
        BasicMinDose
    }

    /// <summary>
    /// Definition of a DQP output column.
    /// </summary>
    public class DqpColumnDefinition
    {
        public string StructureIdentifier { get; set; } = string.Empty;
        public string MetricKey { get; set; } = string.Empty;
        public string HeaderText { get; set; } = string.Empty;
        public DqpColumnType ColumnType { get; set; } = DqpColumnType.DqpMetric;
    }

    /// <summary>
    /// Streaming exporter for normalized CSV (1 plan per row).
    /// </summary>
    public class CsvStreamExporter : IDisposable
    {
        private readonly StreamWriter _writer;
        private readonly ExtractionOptions _options;
        private readonly List<DqpColumnDefinition> _dqpColumns;
        private bool _headerWritten = false;

        public CsvStreamExporter(StreamWriter writer, ExtractionOptions options, List<DqpColumnDefinition> dqpColumns = null)
        {
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _options = options ?? new ExtractionOptions();
            _dqpColumns = dqpColumns ?? new List<DqpColumnDefinition>();
        }

        /// <summary>
        /// Writes the CSV header row.
        /// </summary>
        public void WriteHeader()
        {
            if (_headerWritten) return;

            var sb = new StringBuilder();

            // Core headers
            sb.Append("Patient ID,Course ID,Date of birth,Plan ID,Target volume,DosePerFraction[Gy],NumberOfFractions,TotalDose[Gy],NumberOfBeams,ApprovalStatus,IsPlanSum");

            // Optional headers
            if (_options.ExportPlanningApprover) sb.Append(",PlanningApprover");
            if (_options.ExportPlanningApprovalDate) sb.Append(",PlanningApprovalDate");
            if (_options.ExportBeamMU) sb.Append(",MU");
            if (_options.ExportBeamMachineEnergyTech) sb.Append(",Machine/Energy/Tech/PlanType");
            if (_options.ExportCalculationModel) sb.Append(",CalculationModel");
            if (_options.ExportCalculationLog) sb.Append(",CalculationLog");
            if (_options.ExportNormalizationMode) sb.Append(",PlanNormalizationMethod");
            if (_options.ExportClinicalProtocol) sb.Append(",ClinicalProtocol");
            if (_options.ExportOptimizationObjectives) sb.Append(",OptimizationObjectives");
            if (_options.ExportPlanComplexity) sb.Append(",PlanComplexity");

            // DQP columns
            foreach (var col in _dqpColumns)
            {
                sb.Append($",{StringSanitizer.EscapeCsv(col.HeaderText)}");
            }

            _writer.WriteLine(sb.ToString());
            _writer.Flush();
            _headerWritten = true;
        }

        /// <summary>
        /// Streams a single plan record as a CSV row.
        /// </summary>
        public void WriteRecord(ExtractionPlanRecord record)
        {
            if (!_headerWritten)
            {
                WriteHeader();
            }

            if (record == null) return;

            var sb = new StringBuilder();

            // Anonymization handling
            string patId = _options.AnonymizeOutput 
                ? StringSanitizer.AnonymizePatientId(record.PatientId) 
                : record.PatientId;

            string dob = _options.AnonymizeOutput 
                ? StringSanitizer.Redacted 
                : (record.DateOfBirth?.ToString("yyyy-MM-dd") ?? StringSanitizer.NotApplicable);

            string approver = _options.AnonymizeOutput 
                ? StringSanitizer.Redacted 
                : StringSanitizer.ValueOrNA(record.PlanningApprover);

            // Core columns
            sb.Append(StringSanitizer.EscapeCsv(patId)).Append(",");
            sb.Append(StringSanitizer.EscapeCsv(record.CourseId)).Append(",");
            sb.Append(StringSanitizer.EscapeCsv(dob)).Append(",");
            sb.Append(StringSanitizer.EscapeCsv(record.PlanId)).Append(",");
            sb.Append(StringSanitizer.EscapeCsv(StringSanitizer.ValueOrNA(record.TargetVolumeId))).Append(",");
            sb.Append(StringSanitizer.ValueOrNA(record.DosePerFractionGy, "F2")).Append(",");
            sb.Append(StringSanitizer.ValueOrNA(record.NumberOfFractions)).Append(",");
            sb.Append(StringSanitizer.ValueOrNA(record.TotalDoseGy, "F2")).Append(",");
            sb.Append(record.NumberOfBeams).Append(",");
            sb.Append(StringSanitizer.EscapeCsv(record.ApprovalStatus)).Append(",");
            sb.Append(record.IsPlanSum ? "True" : "False");

            // Optional columns
            if (_options.ExportPlanningApprover)
            {
                sb.Append(",").Append(StringSanitizer.EscapeCsv(approver));
            }
            if (_options.ExportPlanningApprovalDate)
            {
                string appDate = StringSanitizer.ValueOrNA(record.PlanningApprovalDate);
                sb.Append(",").Append(StringSanitizer.EscapeCsv(appDate));
            }
            if (_options.ExportBeamMU)
            {
                string muSummary = record.Beams.Count > 0 
                    ? string.Join(";", record.Beams.Select(b => $"{b.BeamId}:{b.MetersetMU:F1}")) 
                    : StringSanitizer.NotApplicable;
                sb.Append(",").Append(StringSanitizer.EscapeCsv(muSummary));
            }
            if (_options.ExportBeamMachineEnergyTech)
            {
                string beamSummary = record.Beams.Count > 0 
                    ? string.Join(";", record.Beams.Select(b => b.ToMachineEnergySummary())) 
                    : StringSanitizer.NotApplicable;
                sb.Append(",").Append(StringSanitizer.EscapeCsv(beamSummary));
            }
            if (_options.ExportCalculationModel)
            {
                string models = $"{record.CalculationModelPhoton}/{record.CalculationModelElectron}".Trim('/');
                sb.Append(",").Append(StringSanitizer.EscapeCsv(StringSanitizer.ValueOrNA(models)));
            }
            if (_options.ExportCalculationLog)
            {
                string logs = record.CalculationLogs.Count > 0 
                    ? string.Join(";", record.CalculationLogs) 
                    : StringSanitizer.NotApplicable;
                sb.Append(",").Append(StringSanitizer.EscapeCsv(logs));
            }
            if (_options.ExportNormalizationMode)
            {
                sb.Append(",").Append(StringSanitizer.EscapeCsv(StringSanitizer.ValueOrNA(record.PlanNormalizationMethod)));
            }
            if (_options.ExportClinicalProtocol)
            {
                sb.Append(",").Append(StringSanitizer.EscapeCsv(StringSanitizer.ValueOrNA(record.ClinicalProtocolSummary)));
            }
            if (_options.ExportOptimizationObjectives)
            {
                string optSummary = record.OptimizationObjectives.Count > 0 
                    ? string.Join(";", record.OptimizationObjectives.Select(o => o.ToSummaryString())) 
                    : StringSanitizer.NotApplicable;
                sb.Append(",").Append(StringSanitizer.EscapeCsv(optSummary));
            }
            if (_options.ExportPlanComplexity)
            {
                sb.Append(",").Append(StringSanitizer.EscapeCsv(StringSanitizer.ValueOrNA(record.PlanComplexitySummary)));
            }

            // DQP and basic metric columns
            foreach (var col in _dqpColumns)
            {
                var metric = record.DvhMetrics.FirstOrDefault(m => 
                    (string.Equals(m.TargetAlias, col.StructureIdentifier, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(m.OriginalStructureId, col.StructureIdentifier, StringComparison.OrdinalIgnoreCase)) &&
                    (col.ColumnType != DqpColumnType.DqpMetric || string.Equals(m.MetricKey, col.MetricKey, StringComparison.OrdinalIgnoreCase)));

                string valStr = StringSanitizer.NotApplicable;
                if (metric != null)
                {
                    switch (col.ColumnType)
                    {
                        case DqpColumnType.BasicVolume:
                            if (metric.StructureVolumeCc.HasValue)
                                valStr = metric.StructureVolumeCc.Value.ToString("F2");
                            break;
                        case DqpColumnType.BasicMaxDose:
                            if (metric.MaxDoseGy.HasValue)
                                valStr = metric.MaxDoseGy.Value.ToString("F2");
                            break;
                        case DqpColumnType.BasicMeanDose:
                            if (metric.MeanDoseGy.HasValue)
                                valStr = metric.MeanDoseGy.Value.ToString("F2");
                            break;
                        case DqpColumnType.BasicMinDose:
                            if (metric.MinDoseGy.HasValue)
                                valStr = metric.MinDoseGy.Value.ToString("F2");
                            break;
                        case DqpColumnType.DqpMetric:
                            if (metric.Value.HasValue)
                                valStr = metric.Value.Value.ToString("F2");
                            break;
                    }
                }

                sb.Append(",").Append(StringSanitizer.EscapeCsv(valStr));
            }

            _writer.WriteLine(sb.ToString());
            _writer.Flush();
        }

        public void Dispose()
        {
            _writer?.Dispose();
        }
    }
}
