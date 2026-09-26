using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using EclipseDataMiner.Models;

namespace EclipseDataMiner.Services
{
    /// <summary>
    /// CSVおよびJSONLへのストリーミング出力を一括制御するパイプライン
    /// </summary>
    public class StreamingExportPipeline : IDisposable
    {
        private CsvStreamExporter _csvExporter;
        private JsonlStreamExporter _jsonlExporter;
        private bool _disposed = false;

        public string CsvFilePath { get; private set; } = string.Empty;
        public string JsonlFilePath { get; private set; } = string.Empty;

        /// <summary>
        /// パイプラインを初期化してファイルを開く
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
        /// 1プラン分のレコードをストリーミング書き出し
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
