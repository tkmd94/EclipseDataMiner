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
    /// マッピング解決結果
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
    /// 輪郭エイリアスマッピングの評価および設定JSONの保存・読み込みサービス
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
        /// 輪郭IDに対してルールリストを評価し、エイリアス名とオプトアウト判定を返却
        /// 優先順位: 完全一致 (Exact) > 部分一致 (Contains) > 正規表現 (Regex)（各カテゴリ内は先頭優先）
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

            // ルールに一致しない輪郭はデフォルトで有効、エイリアスは元のID
            return new ResolvedStructureMapping(true, structureId);
        }

        /// <summary>
        /// 特異度優先（Exact > Contains > Regex）で最適なルールを探索
        /// </summary>
        public static StructureMappingRule FindBestMatchingRule(string structureId, IEnumerable<StructureMappingRule> rules)
        {
            if (rules == null || string.IsNullOrEmpty(structureId)) return null;
            var ruleList = rules as IList<StructureMappingRule> ?? rules.ToList();

            // 1. 完全一致 (Exact) を最優先
            foreach (var r in ruleList)
            {
                if (r.MatchMode == StructureMatchMode.Exact && r.IsMatch(structureId))
                {
                    return r;
                }
            }

            // 2. 部分一致 (Contains) を次に優先
            foreach (var r in ruleList)
            {
                if (r.MatchMode == StructureMatchMode.Contains && r.IsMatch(structureId))
                {
                    return r;
                }
            }

            // 3. 正規表現 (Regex) を最後に評価
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
        /// スキャンされた輪郭一覧に対して、最新のルールリストを特異度優先で評価し、解決先Aliasおよびステータスを一括更新
        /// 同一 Target Alias に複数輪郭が統合されている場合はステータスに注記
        /// </summary>
        public static void RefreshPreview(IEnumerable<DiscoveredStructureItem> discoveredItems, IEnumerable<StructureMappingRule> rules)
        {
            if (discoveredItems == null) return;

            var itemsList = discoveredItems as IList<DiscoveredStructureItem> ?? discoveredItems.ToList();
            var ruleList = rules?.ToList() ?? new List<StructureMappingRule>();

            // 1. 各輪郭の解決先Aliasとルール適合状態を判定
            foreach (var item in itemsList)
            {
                var bestRule = FindBestMatchingRule(item.RawStructureId, ruleList);
                if (bestRule != null)
                {
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

            // 2. Target Alias の重複（複数輪郭の集約）をカウントして注記
            var aliasCountMap = itemsList
                .Where(d => d.IsExtracted)
                .GroupBy(d => d.ResolvedAlias, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            foreach (var item in itemsList)
            {
                if (item.IsExtracted && aliasCountMap.TryGetValue(item.ResolvedAlias, out int count) && count > 1)
                {
                    item.MatchStatus += $" [統合: {count}件]";
                }
            }
        }

        /// <summary>
        /// マッピングルールリストを JSON ファイルに保存
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
        /// JSON ファイルからマッピングルールリストを読み込み
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
