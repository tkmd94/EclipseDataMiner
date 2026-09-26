using System;
using System.Collections.Generic;
using System.Linq;

namespace EclipseDataMiner.Models
{
    /// <summary>
    /// テキスト一致モード（部分一致、完全一致、正規表現）
    /// </summary>
    public enum TextMatchMode
    {
        Contains, // 部分一致（デフォルト）
        Exact,    // 完全一致
        Regex     // 正規表現
    }

    /// <summary>
    /// 日付フィルタの対象種別
    /// </summary>
    public enum DateFilterTarget
    {
        TreatmentApprovalDate, // 治療承認日
        PlanningApprovalDate,  // 計画承認日
        CreationDate           // 計画作成日
    }

    /// <summary>
    /// 線量有無（計算済み／未計算）によるフィルタ種別
    /// </summary>
    public enum DosePresenceFilter
    {
        All,        // すべて（線量の有無を問わない・デフォルト）
        HasDose,    // 線量あり（線量計算済み: TotalDose > 0 かつ !NaN）
        NoDose      // 線量なし（線量未計算: TotalDose == null または NaN または <= 0）
    }

    /// <summary>
    /// 検索・フィルタリング条件モデル
    /// </summary>
    public class SearchFilterCriteria
    {
        public DosePresenceFilter DosePresence { get; set; } = DosePresenceFilter.All;

        public List<string> PatientIdFilter { get; set; } = new List<string>();
        public List<string> PatientIdExcludeFilter { get; set; } = new List<string>();
        public TextMatchMode PatientIdMatchMode { get; set; } = TextMatchMode.Contains;

        public List<string> CourseIdFilter { get; set; } = new List<string>();
        public List<string> CourseIdExcludeFilter { get; set; } = new List<string>();
        public TextMatchMode CourseIdMatchMode { get; set; } = TextMatchMode.Contains;

        public List<string> PlanIdFilter { get; set; } = new List<string>();
        public List<string> PlanIdExcludeFilter { get; set; } = new List<string>();
        public TextMatchMode PlanIdMatchMode { get; set; } = TextMatchMode.Contains;

        public List<string> TargetVolumeIdFilter { get; set; } = new List<string>();
        public List<string> TargetVolumeExcludeFilter { get; set; } = new List<string>();
        public TextMatchMode TargetVolumeMatchMode { get; set; } = TextMatchMode.Contains;

        public double? DosePerFractionGy { get; set; }
        public int? NumberOfFractions { get; set; }
        public double? TotalDoseGy { get; set; }

        public NumericFilterCriteria DosePerFractionCriteria { get; set; }
        public NumericFilterCriteria NumberOfFractionsCriteria { get; set; }
        public NumericFilterCriteria TotalDoseCriteria { get; set; }

        // 高度メタデータフィルタ (Machine, Energy, Technique, Date Range)
        public List<string> MachineFilter { get; set; } = new List<string>();
        public List<string> MachineExcludeFilter { get; set; } = new List<string>();

        public List<string> EnergyFilter { get; set; } = new List<string>();
        public List<string> EnergyExcludeFilter { get; set; } = new List<string>();

        public List<string> TechniqueFilter { get; set; } = new List<string>();
        public List<string> TechniqueExcludeFilter { get; set; } = new List<string>();

        public DateFilterTarget DateTarget { get; set; } = DateFilterTarget.TreatmentApprovalDate;
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }

        public bool FilterUnapproved { get; set; } = true;
        public bool FilterPlanApproved { get; set; } = true;
        public bool FilterTreatmentApproved { get; set; } = true;

        /// <summary>
        /// グローバル論理切替（true: AND「すべて満たす」, false: OR「いずれかを満たす」）
        /// </summary>
        public bool GlobalLogicIsAnd { get; set; } = true;

        /// <summary>
        /// PlanSum（合算計画）を含めるかどうか（デフォルトfalse）
        /// </summary>
        public bool IncludePlanSums { get; set; } = false;

        /// <summary>
        /// テキスト入力をカンマ区切りでパースしてリスト化（後方互換用）
        /// </summary>
        public static List<string> ParseCommaSeparated(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return new List<string>();
            return input.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();
        }

        /// <summary>
        /// テキスト入力をパースし、通常条件（include）と除外条件（exclude: ! または - で開始）に分離
        /// </summary>
        public static void ParseTextFilter(string input, out List<string> includes, out List<string> excludes)
        {
            includes = new List<string>();
            excludes = new List<string>();

            if (string.IsNullOrWhiteSpace(input)) return;

            var tokens = input.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                              .Select(s => s.Trim())
                              .Where(s => !string.IsNullOrEmpty(s));

            foreach (var token in tokens)
            {
                if (token.StartsWith("!") || token.StartsWith("-"))
                {
                    string rawExclude = token.Substring(1).Trim();
                    if (!string.IsNullOrEmpty(rawExclude))
                    {
                        excludes.Add(rawExclude);
                    }
                }
                else
                {
                    includes.Add(token);
                }
            }
        }
    }
}
