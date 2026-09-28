using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using EclipseDataMiner.Helpers;
using EclipseDataMiner.Models;

namespace EclipseDataMiner.Services
{
    /// <summary>
    /// JSON Lines (JSONL) streaming exporter for machine learning and AI applications (1 plan per JSON line).
    /// </summary>
    public class JsonlStreamExporter : IDisposable
    {
        private readonly StreamWriter _writer;
        private readonly ExtractionOptions _options;
        private readonly JsonSerializerOptions _jsonOptions;

        public JsonlStreamExporter(StreamWriter writer, ExtractionOptions options)
        {
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _options = options ?? new ExtractionOptions();

            _jsonOptions = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                WriteIndented = false // Single line JSON
            };
        }

        /// <summary>
        /// Writes a single plan record as a JSONL line.
        /// </summary>
        public void WriteRecord(ExtractionPlanRecord record)
        {
            if (record == null) return;

            // Apply anonymization options
            var target = record;
            if (_options.AnonymizeOutput)
            {
                target = new ExtractionPlanRecord
                {
                    PatientId = StringSanitizer.AnonymizePatientId(record.PatientId),
                    CourseId = record.CourseId,
                    PlanId = record.PlanId,
                    IsPlanSum = record.IsPlanSum,
                    DateOfBirth = null,
                    TargetVolumeId = record.TargetVolumeId,
                    DosePerFractionGy = record.DosePerFractionGy,
                    NumberOfFractions = record.NumberOfFractions,
                    TotalDoseGy = record.TotalDoseGy,
                    NumberOfBeams = record.NumberOfBeams,
                    ApprovalStatus = record.ApprovalStatus,
                    PlanningApprover = StringSanitizer.Redacted,
                    PlanningApprovalDate = record.PlanningApprovalDate,
                    CalculationModelPhoton = record.CalculationModelPhoton,
                    CalculationModelElectron = record.CalculationModelElectron,
                    PlanNormalizationMethod = record.PlanNormalizationMethod,
                    ClinicalProtocolSummary = record.ClinicalProtocolSummary,
                    Beams = record.Beams,
                    OptimizationObjectives = record.OptimizationObjectives,
                    DvhMetrics = record.DvhMetrics,
                    CalculationLogs = record.CalculationLogs,
                    PlanComplexitySummary = record.PlanComplexitySummary
                };
            }

            string jsonLine = JsonSerializer.Serialize(target, _jsonOptions);
            _writer.WriteLine(jsonLine);
            _writer.Flush();
        }

        public void Dispose()
        {
            _writer?.Dispose();
        }
    }
}
