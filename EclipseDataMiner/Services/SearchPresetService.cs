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
    /// Service for managing, saving, and loading search/filtering presets to/from JSON files (in Presets directory).
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
        /// Full path to the directory where presets are saved.
        /// </summary>
        public string PresetsDirectory => _presetsDirectory;

        /// <summary>
        /// Gets the Presets folder path located in the same directory as the application.
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

                // If default Presets directory (under application folder) is empty, auto-seed default presets
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
                    Description = "All TreatmentApproved plans (excluding QA and verification plans)",
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
                    Description = "Prostate standard VMAT prescription (2Gy x 39fx = 78Gy, excluding QA/test plans)",
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
                    Description = "Lung SBRT hypofractionation (>=10Gy/fx, 4-5 fx, total 48-60Gy)",
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
                    Description = "Head & Neck definitive (33-35 fx, total 70Gy, excluding QA)",
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
        /// Generates a sanitized file name from preset name.
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
        /// Loads presets from all JSON files in the Presets folder.
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
                            // Single preset per file
                            var preset = JsonSerializer.Deserialize<SearchPreset>(json, JsonOptions);
                            if (preset != null && !string.IsNullOrWhiteSpace(preset.Name))
                            {
                                preset.FilePath = file;
                                preset.IsBuiltIn = false;
                                presets.Add(preset);
                            }
                            else
                            {
                                // Fallback for list array format (List<SearchPreset>)
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
        /// Saves a single preset as an individual JSON file.
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
        /// Deletes the physical JSON file for a preset.
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
        /// Batch saves a list of presets (for backward compatibility).
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
