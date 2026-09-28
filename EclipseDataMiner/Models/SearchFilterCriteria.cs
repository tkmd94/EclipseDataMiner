using System;
using System.Collections.Generic;
using System.Linq;

namespace EclipseDataMiner.Models
{
    /// <summary>
    /// Text matching mode (Contains, Exact, Regex).
    /// </summary>
    public enum TextMatchMode
    {
        Contains, // Partial match (default)
        Exact,    // Exact match
        Regex     // Regular expression match
    }

    /// <summary>
    /// Target date field for date filtering.
    /// </summary>
    public enum DateFilterTarget
    {
        TreatmentApprovalDate, // Treatment approval date
        PlanningApprovalDate,  // Planning approval date
        CreationDate           // Creation date
    }

    /// <summary>
    /// Filter mode based on dose presence (calculated or uncalculated).
    /// </summary>
    public enum DosePresenceFilter
    {
        All,        // All (ignore dose presence; default)
        HasDose,    // Calculated dose present (TotalDose > 0 and !NaN)
        NoDose      // No calculated dose (TotalDose == null or NaN or <= 0)
    }

    /// <summary>
    /// Search and filtering criteria model.
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

        // Advanced metadata filters (Machine, Energy, Technique, Date Range)
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
        /// Global logical condition toggle (true: AND "match all", false: OR "match any").
        /// </summary>
        public bool GlobalLogicIsAnd { get; set; } = true;

        /// <summary>
        /// Whether to include PlanSums (default false).
        /// </summary>
        public bool IncludePlanSums { get; set; } = false;

        /// <summary>
        /// Parses comma-separated text into a list (for backward compatibility).
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
        /// Parses text input and separates into inclusion terms and exclusion terms (starting with ! or -).
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
