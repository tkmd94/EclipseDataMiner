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
    /// Mapping resolution result.
    /// </summary>
    public class ResolvedStructureMapping
    {
        public bool IsSelected { get; set; } = true;
        public string TargetAlias { get; set; } = string.Empty;

        public ResolvedStructureMapping(bool isSelected, string targetAlias)
        {
            IsSelected = isSelected;
            TargetAlias = targetAlias;
        }
    }

    /// <summary>
    /// Service for evaluating structure alias mappings and saving/loading mapping configuration JSON.
    /// </summary>
    public class StructureMappingService
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            Converters = { new JsonStringEnumConverter() }
        };

        /// <summary>
        /// Evaluates rule list against structure ID, returning alias name and opt-out flag.
        /// Priority: Exact > Contains > Regex (first match wins within each category).
        /// </summary>
        public static ResolvedStructureMapping ResolveMapping(string structureId, IEnumerable<StructureMappingRule> rules)
        {
            if (string.IsNullOrEmpty(structureId))
            {
                return new ResolvedStructureMapping(false, string.Empty);
            }

            var bestRule = FindBestMatchingRule(structureId, rules);
            if (bestRule != null)
            {
                if (!bestRule.IsSelected)
                {
                    return new ResolvedStructureMapping(false, structureId);
                }

                string alias = string.IsNullOrWhiteSpace(bestRule.TargetAlias) 
                    ? structureId 
                    : bestRule.TargetAlias.Trim();

                return new ResolvedStructureMapping(true, alias);
            }

            // Structures not matching any rule remain enabled by default, with alias equal to raw ID
            return new ResolvedStructureMapping(true, structureId);
        }

        /// <summary>
        /// Finds the best matching rule based on specificity priority (Exact > Contains > Regex).
        /// </summary>
        public static StructureMappingRule FindBestMatchingRule(string structureId, IEnumerable<StructureMappingRule> rules)
        {
            if (rules == null || string.IsNullOrEmpty(structureId)) return null;
            var ruleList = rules as IList<StructureMappingRule> ?? rules.ToList();

            // 1. Exact match takes highest priority
            foreach (var r in ruleList)
            {
                if (r.MatchMode == StructureMatchMode.Exact && r.IsMatch(structureId))
                {
                    return r;
                }
            }

            // 2. Partial match (Contains) takes second priority
            foreach (var r in ruleList)
            {
                if (r.MatchMode == StructureMatchMode.Contains && r.IsMatch(structureId))
                {
                    return r;
                }
            }

            // 3. Regular expression (Regex) evaluated last
            foreach (var r in ruleList)
            {
                if (r.MatchMode == StructureMatchMode.Regex && r.IsMatch(structureId))
                {
                    return r;
                }
            }

            return null;
        }

        /// <summary>
        /// Evaluates latest rule list for scanned structures with specificity priority, updating resolved aliases and match statuses.
        /// Appends note if multiple structures are merged into the same Target Alias.
        /// </summary>
        public static void RefreshPreview(IEnumerable<DiscoveredStructureItem> discoveredItems, IEnumerable<StructureMappingRule> rules)
        {
            if (discoveredItems == null) return;

            var itemsList = discoveredItems as IList<DiscoveredStructureItem> ?? discoveredItems.ToList();
            var ruleList = rules?.ToList() ?? new List<StructureMappingRule>();

            // 0. Reset matched counts for all rules
            foreach (var rule in ruleList)
            {
                rule.MatchedCount = 0;
            }

            // 1. Determine resolved alias and rule match status for each structure
            foreach (var item in itemsList)
            {
                var bestRule = FindBestMatchingRule(item.RawStructureId, ruleList);
                if (bestRule != null)
                {
                    bestRule.MatchedCount++;
                    item.IsExtracted = bestRule.IsSelected;
                    item.ResolvedAlias = !string.IsNullOrWhiteSpace(bestRule.TargetAlias)
                        ? bestRule.TargetAlias.Trim()
                        : item.RawStructureId;

                    if (!bestRule.IsSelected)
                    {
                        item.MatchStatus = $"Excluded ({bestRule.MatchMode})";
                    }
                    else
                    {
                        item.MatchStatus = $"Mapped ({bestRule.MatchMode})";
                    }
                }
                else
                {
                    item.IsExtracted = true;
                    item.ResolvedAlias = item.RawStructureId;
                    item.MatchStatus = "Unmapped (Raw)";
                }
            }

            // 2. Count and annotate target alias duplicates (merged structures)
            var aliasCountMap = itemsList
                .Where(d => d.IsExtracted)
                .GroupBy(d => d.ResolvedAlias, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            foreach (var item in itemsList)
            {
                if (item.IsExtracted && aliasCountMap.TryGetValue(item.ResolvedAlias, out int count) && count > 1)
                {
                    item.MatchStatus += $" [Merged: {count}]";
                }
            }
        }

        /// <summary>
        /// Saves mapping rule list to a JSON file.
        /// </summary>
        public static void SaveRulesToFile(string filePath, IEnumerable<StructureMappingRule> rules)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path must be specified.", nameof(filePath));
            }

            string json = JsonSerializer.Serialize(rules.ToList(), JsonOptions);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Loads mapping rule list from a JSON file.
        /// </summary>
        public static List<StructureMappingRule> LoadRulesFromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                throw new FileNotFoundException("Mapping rules file not found.", filePath);
            }

            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<StructureMappingRule>>(json, JsonOptions) ?? new List<StructureMappingRule>();
        }
    }
}
