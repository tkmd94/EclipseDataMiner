using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using EclipseDataMiner.Models;

namespace EclipseDataMiner.Services
{
    /// <summary>
    /// 検索・フィルタリングプリセットの管理・JSON保存読込サービス（Presetsフォルダ管理）
    /// </summary>
    public class SearchPresetService
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly string _presetsDirectory;

        /// <summary>
        /// プリセット保存先フォルダのフルパス
        /// </summary>
        public string PresetsDirectory => _presetsDirectory;

        /// <summary>
        /// アプリ本体と同じフォルダにある Presets フォルダパスを取得
        /// </summary>
        public static string GetDefaultPresetsDirectory()
        {
            string baseDir = null;
            try
            {
                var assembly = typeof(SearchPresetService).Assembly;
                if (!string.IsNullOrEmpty(assembly.Location))
                {
                    baseDir = Path.GetDirectoryName(assembly.Location);
                }
            }
            catch { }

            if (string.IsNullOrEmpty(baseDir))
            {
                baseDir = AppDomain.CurrentDomain.BaseDirectory;
            }

            return Path.Combine(baseDir, "Presets");
        }

        public SearchPresetService(string customDirectory = null)
        {
            if (!string.IsNullOrEmpty(customDirectory))
            {
                _presetsDirectory = customDirectory;
            }
            else
            {
                _presetsDirectory = GetDefaultPresetsDirectory();
            }

            EnsureDirectoryExists();
        }

        private void EnsureDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(_presetsDirectory))
                {
                    Directory.CreateDirectory(_presetsDirectory);
                }

                // デフォルトの Presets ディレクトリ（アプリ直下）が空の場合、Templates/Presets から初期プリセットを自動シード
                if (string.Equals(_presetsDirectory, GetDefaultPresetsDirectory(), StringComparison.OrdinalIgnoreCase))
                {
                    SeedDefaultPresetsIfEmpty();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create presets directory: {ex.Message}");
            }
        }

        private void SeedDefaultPresetsIfEmpty()
        {
            try
            {
                if (!Directory.Exists(_presetsDirectory)) return;

                var existing = Directory.GetFiles(_presetsDirectory, "*.json");
                if (existing.Length == 0)
                {
                    CreateDefaultPresets();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to seed default presets: {ex.Message}");
            }
        }

        private void CreateDefaultPresets()
        {
            var defaults = new List<SearchPreset>
            {
                new SearchPreset
                {
                    Name = "All Treatment Approved (Exclude QA)",
                    Description = "治療承認済み（TreatmentApproved）全計画（QA・検証計画を除外）",
                    PlanIdText = "!QA, !Verify, !Test",
                    PlanIdMatchMode = TextMatchMode.Contains,
                    FilterTreatmentApproved = true,
                    FilterPlanApproved = false,
                    FilterUnapproved = false,
                    DateTarget = DateFilterTarget.TreatmentApprovalDate,
                    DosePresence = DosePresenceFilter.All,
                    GlobalLogicIsAnd = true
                },
                new SearchPreset
                {
                    Name = "Prostate VMAT 78Gy (Standard)",
                    Description = "前立腺VMAT標準処方（2Gy×39回=78Gy、QA/テスト計画除外）",
                    PlanIdText = "VMAT, !QA, !Test",
                    PlanIdMatchMode = TextMatchMode.Contains,
                    TargetVolumeIdText = "PTV",
                    TargetVolumeMatchMode = TextMatchMode.Contains,
                    DosePerFractionText = "2.0",
                    NumberOfFractionsText = "39",
                    TotalDoseText = "78",
                    FilterTreatmentApproved = true,
                    FilterPlanApproved = true,
                    FilterUnapproved = false,
                    DateTarget = DateFilterTarget.TreatmentApprovalDate,
                    DosePresence = DosePresenceFilter.HasDose,
                    GlobalLogicIsAnd = true
                },
                new SearchPreset
                {
                    Name = "Lung SBRT 4-5Fr (>=10Gy/Fr)",
                    Description = "肺SBRT大線量分割処方（1回10Gy以上、4〜5分割、総線量48〜60Gy）",
                    PlanIdText = "!QA, !Test",
                    PlanIdMatchMode = TextMatchMode.Contains,
                    DosePerFractionText = ">= 10",
                    NumberOfFractionsText = "4 - 5",
                    TotalDoseText = "48 - 60",
                    FilterTreatmentApproved = true,
                    FilterPlanApproved = true,
                    FilterUnapproved = false,
                    DateTarget = DateFilterTarget.TreatmentApprovalDate,
                    DosePresence = DosePresenceFilter.HasDose,
                    GlobalLogicIsAnd = true
                },
                new SearchPreset
                {
                    Name = "Head & Neck 70Gy (33-35Fr)",
                    Description = "頭頸部根治照射（33〜35分割、総線量70Gy、QA除外）",
                    PlanIdText = "!QA, !Test",
                    PlanIdMatchMode = TextMatchMode.Contains,
                    NumberOfFractionsText = "33 - 35",
                    TotalDoseText = "70",
                    FilterTreatmentApproved = true,
                    FilterPlanApproved = true,
                    FilterUnapproved = false,
                    DateTarget = DateFilterTarget.TreatmentApprovalDate,
                    DosePresence = DosePresenceFilter.HasDose,
                    GlobalLogicIsAnd = true
                }
            };

            foreach (var preset in defaults)
            {
                SavePreset(preset);
            }
        }

        /// <summary>
        /// プリセット名から安全なファイル名を生成
        /// </summary>
        public static string GetSafeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Preset.json";
            var invalidChars = Path.GetInvalidFileNameChars();
            var clean = new string(name.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray()).Trim();
            if (string.IsNullOrEmpty(clean)) clean = "Preset";
            return clean + ".json";
        }

        /// <summary>
        /// Presets フォルダ内の全 JSON ファイルからプリセット一覧をロード（組み込みプリセットは非表示・非強制）
        /// </summary>
        public List<SearchPreset> LoadPresets()
        {
            EnsureDirectoryExists();
            var presets = new List<SearchPreset>();

            try
            {
                if (Directory.Exists(_presetsDirectory))
                {
                    var files = Directory.GetFiles(_presetsDirectory, "*.json");
                    foreach (var file in files)
                    {
                        try
                        {
                            string json = File.ReadAllText(file);
                            // 1ファイルに1つのプリセット
                            var preset = JsonSerializer.Deserialize<SearchPreset>(json, JsonOptions);
                            if (preset != null && !string.IsNullOrWhiteSpace(preset.Name))
                            {
                                preset.FilePath = file;
                                preset.IsBuiltIn = false;
                                presets.Add(preset);
                            }
                            else
                            {
                                // 配列形式 (List<SearchPreset>) もフォールバックで対応
                                var list = JsonSerializer.Deserialize<List<SearchPreset>>(json, JsonOptions);
                                if (list != null)
                                {
                                    foreach (var p in list)
                                    {
                                        if (!string.IsNullOrWhiteSpace(p.Name))
                                        {
                                            p.FilePath = file;
                                            p.IsBuiltIn = false;
                                            presets.Add(p);
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to load preset from file {file}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to enumerate presets in {_presetsDirectory}: {ex.Message}");
            }

            return presets;
        }

        /// <summary>
        /// 単一プリセットを個別 JSON ファイルとして保存
        /// </summary>
        public void SavePreset(SearchPreset preset)
        {
            if (preset == null || string.IsNullOrWhiteSpace(preset.Name)) return;
            EnsureDirectoryExists();

            try
            {
                string targetPath;
                if (!string.IsNullOrEmpty(preset.FilePath) && File.Exists(preset.FilePath))
                {
                    string currentFileName = Path.GetFileName(preset.FilePath);
                    string expectedFileName = GetSafeFileName(preset.Name);
                    if (!currentFileName.Equals(expectedFileName, StringComparison.OrdinalIgnoreCase))
                    {
                        try { File.Delete(preset.FilePath); } catch { }
                        targetPath = Path.Combine(_presetsDirectory, expectedFileName);
                    }
                    else
                    {
                        targetPath = preset.FilePath;
                    }
                }
                else
                {
                    targetPath = Path.Combine(_presetsDirectory, GetSafeFileName(preset.Name));
                }

                preset.FilePath = targetPath;
                preset.IsBuiltIn = false;
                string json = JsonSerializer.Serialize(preset, JsonOptions);
                File.WriteAllText(targetPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save preset '{preset.Name}': {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// プリセットの物理 JSON ファイルを削除
        /// </summary>
        public void DeletePreset(SearchPreset preset)
        {
            if (preset == null) return;

            try
            {
                string targetPath = preset.FilePath;
                if (string.IsNullOrEmpty(targetPath) || !File.Exists(targetPath))
                {
                    targetPath = Path.Combine(_presetsDirectory, GetSafeFileName(preset.Name));
                }

                if (File.Exists(targetPath))
                {
                    File.Delete(targetPath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to delete preset '{preset.Name}': {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// プリセット一覧を一括保存（後方互換用）
        /// </summary>
        public void SavePresets(IEnumerable<SearchPreset> presets)
        {
            if (presets == null) return;
            foreach (var p in presets)
            {
                SavePreset(p);
            }
        }
    }
}
