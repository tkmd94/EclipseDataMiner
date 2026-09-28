using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using EclipseDataMiner.Models;

namespace EclipseDataMiner.Services
{
    /// <summary>
    /// Pipeline orchestrating streaming export to both CSV and JSONL formats.
    /// </summary>
    public class StreamingExportPipeline : IDisposable
    {
        private CsvStreamExporter _csvExporter;
        private JsonlStreamExporter _jsonlExporter;
        private bool _disposed = false;

        public string CsvFilePath { get; private set; } = string.Empty;
        public string JsonlFilePath { get; private set; } = string.Empty;

        /// <summary>
        /// Initializes the pipeline and opens export files.
        /// </summary>
        public void Initialize(string csvPath, ExtractionOptions options, List<DqpColumnDefinition> dqpColumns = null, string jsonlPath = null)
        {
            if (string.IsNullOrWhiteSpace(csvPath))
            {
                throw new ArgumentException("CSV file path must be specified.", nameof(csvPath));
            }

            CsvFilePath = csvPath;
            string dir = Path.GetDirectoryName(csvPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var csvStream = new FileStream(csvPath, FileMode.Create, FileAccess.Write, FileShare.Read);
            var csvWriter = new StreamWriter(csvStream, Encoding.UTF8);
            _csvExporter = new CsvStreamExporter(csvWriter, options, dqpColumns);
            _csvExporter.WriteHeader();

            if (options.ExportJsonl)
            {
                JsonlFilePath = string.IsNullOrWhiteSpace(jsonlPath) 
                    ? Path.ChangeExtension(csvPath, ".jsonl") 
                    : jsonlPath;

                var jsonlStream = new FileStream(JsonlFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
                var jsonlWriter = new StreamWriter(jsonlStream, Encoding.UTF8);
                _jsonlExporter = new JsonlStreamExporter(jsonlWriter, options);
            }
        }

        /// <summary>
        /// Streams a single plan record to exporters.
        /// </summary>
        public void WritePlanRecord(ExtractionPlanRecord record)
        {
            if (_disposed || record == null) return;

            _csvExporter?.WriteRecord(record);
            _jsonlExporter?.WriteRecord(record);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _csvExporter?.Dispose();
                _jsonlExporter?.Dispose();
                _disposed = true;
            }
        }
    }
}
