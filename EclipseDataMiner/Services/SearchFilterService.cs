using System;
using System.Collections.Generic;
using System.Linq;
using EclipseDataMiner.Models;

namespace EclipseDataMiner.Services
{
    /// <summary>
    /// Search filter criteria evaluation service (in-field OR, global AND/OR, exclusion logic, numeric ranges, PlanSum logic).
    /// </summary>
    public static class SearchFilterService
    {
        /// <summary>
        /// Evaluates whether a text value matches the specified filter lists and match mode (Contains, Exact, Regex) with inclusions and exclusions.
        /// </summary>
        public static bool IsTextMatch(string targetValue, IList<string> includeList, IList<string> excludeList, TextMatchMode mode)
        {
            bool hasIncludes = includeList != null && includeList.Count > 0;
            bool hasExcludes = excludeList != null && excludeList.Count > 0;

            if (!hasIncludes && !hasExcludes)
            {
                return true;
            }

            if (string.IsNullOrEmpty(targetValue))
            {
                return false;
            }

            // 1. Exclusion list check: if matched by any exclusion rule, immediately exclude (false)
            if (hasExcludes)
            {
                bool matchesExclude = EvaluateTokenList(targetValue, excludeList, mode);
                if (matchesExclude)
                {
                    return false;
                }
            }

            // 2. Inclusion list check:
            // If no inclusions specified (only exclusions), having passed exclusions means match (true)
            if (!hasIncludes)
            {
                return true;
            }

            // Check if matches any token in the inclusion list
            return EvaluateTokenList(targetValue, includeList, mode);
        }

        /// <summary>
        /// Evaluates whether a text value matches the specified filter list and match mode (for backward compatibility).
        /// </summary>
        public static bool IsTextMatch(string targetValue, IList<string> filterList, TextMatchMode mode)
        {
            return IsTextMatch(targetValue, filterList, null, mode);
        }

        private static bool EvaluateTokenList(string targetValue, IList<string> tokens, TextMatchMode mode)
        {
            if (tokens == null || tokens.Count == 0) return false;

            switch (mode)
            {
                case TextMatchMode.Exact:
                    return tokens.Any(f => string.Equals(targetValue, f, StringComparison.OrdinalIgnoreCase));

                case TextMatchMode.Regex:
                    return tokens.Any(f =>
                    {
                        try
                        {
                            return System.Text.RegularExpressions.Regex.IsMatch(targetValue, f, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        }
                        catch
                        {
                            return false;
                        }
                    });

                case TextMatchMode.Contains:
                default:
                    return tokens.Any(f => targetValue.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0);
            }
        }

        /// <summary>
        /// Determines whether patient ID matches the criteria.
        /// </summary>
        public static bool IsPatientMatch(string patientId, SearchFilterCriteria criteria)
        {
            if (criteria == null) return true;
            return IsTextMatch(patientId, criteria.PatientIdFilter, criteria.PatientIdExcludeFilter, criteria.PatientIdMatchMode);
        }

        /// <summary>
        /// Determines whether course ID matches the criteria.
        /// </summary>
        public static bool IsCourseMatch(string courseId, SearchFilterCriteria criteria)
        {
            if (criteria == null) return true;
            return IsTextMatch(courseId, criteria.CourseIdFilter, criteria.CourseIdExcludeFilter, criteria.CourseIdMatchMode);
        }

        /// <summary>
        /// Determines whether approval status is included in the enabled selection.
        /// </summary>
        public static bool IsApprovalStatusMatch(string approvalStatus, SearchFilterCriteria criteria)
        {
            if (criteria == null) return true;

            bool isUnapproved = string.Equals(approvalStatus, "Unapproved", StringComparison.OrdinalIgnoreCase);
            bool isPlanApproved = string.Equals(approvalStatus, "PlanningApproved", StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(approvalStatus, "Plan approved", StringComparison.OrdinalIgnoreCase);
            bool isTrtApproved = string.Equals(approvalStatus, "TreatmentApproved", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(approvalStatus, "TRT approved", StringComparison.OrdinalIgnoreCase);

            if (isUnapproved && criteria.FilterUnapproved) return true;
            if (isPlanApproved && criteria.FilterPlanApproved) return true;
            if (isTrtApproved && criteria.FilterTreatmentApproved) return true;

            // If none of the defined statuses match or status is unchecked
            return false;
        }

        /// <summary>
        /// Determines whether a patient can be skipped prior to opening (ESAPI performance optimization).
        /// </summary>
        public static bool ShouldSkipPatient(string patientId, SearchFilterCriteria criteria)
        {
            if (criteria == null) return false;

            bool hasPatientFilter = (criteria.PatientIdFilter?.Count > 0) || (criteria.PatientIdExcludeFilter?.Count > 0);
            if (!hasPatientFilter)
            {
                return false;
            }

            bool patientMatches = IsPatientMatch(patientId, criteria);

            // In AND mode: if patient ID does not match, overall match is impossible regardless of plan details
            if (criteria.GlobalLogicIsAnd)
            {
                return !patientMatches;
            }

            // In OR mode:
            // If patient ID matches, do not skip (the patient criteria is satisfied)
            if (patientMatches)
            {
                return false;
            }

            // In OR mode when patient ID does not match, if other criteria (Course, Plan, Target, Dose, etc.) are present,
            // plans under this patient may still match, so do not skip
            bool hasOtherCriteria = (criteria.CourseIdFilter?.Count > 0) ||
                                    (criteria.CourseIdExcludeFilter?.Count > 0) ||
                                    (criteria.PlanIdFilter?.Count > 0) ||
                                    (criteria.PlanIdExcludeFilter?.Count > 0) ||
                                    (criteria.TargetVolumeIdFilter?.Count > 0) ||
                                    (criteria.TargetVolumeExcludeFilter?.Count > 0) ||
                                    (criteria.DosePerFractionCriteria != null && !criteria.DosePerFractionCriteria.IsEmpty) ||
                                    (criteria.NumberOfFractionsCriteria != null && !criteria.NumberOfFractionsCriteria.IsEmpty) ||
                                    (criteria.TotalDoseCriteria != null && !criteria.TotalDoseCriteria.IsEmpty) ||
                                    (criteria.DosePerFractionGy.HasValue && criteria.DosePerFractionGy.Value > 0) ||
                                    (criteria.NumberOfFractions.HasValue && criteria.NumberOfFractions.Value > 0) ||
                                    (criteria.TotalDoseGy.HasValue && criteria.TotalDoseGy.Value > 0) ||
                                    (criteria.MachineFilter?.Count > 0) || (criteria.MachineExcludeFilter?.Count > 0) ||
                                    (criteria.EnergyFilter?.Count > 0) || (criteria.EnergyExcludeFilter?.Count > 0) ||
                                    (criteria.TechniqueFilter?.Count > 0) || (criteria.TechniqueExcludeFilter?.Count > 0) ||
                                    criteria.DosePresence != DosePresenceFilter.All ||
                                    criteria.DateFrom.HasValue || criteria.DateTo.HasValue;

            return !hasOtherCriteria;
        }

        /// <summary>
        /// Determines whether an entire course can be skipped.
        /// </summary>
        public static bool ShouldSkipCourse(string courseId, SearchFilterCriteria criteria)
        {
            if (criteria == null) return false;

            bool courseMatches = IsCourseMatch(courseId, criteria);

            // In AND mode: skip if course ID does not match
            if (criteria.GlobalLogicIsAnd)
            {
                return !courseMatches;
            }

            // In OR mode:
            if (courseMatches)
            {
                return false;
            }

            // In OR mode: if course ID does not match, but plan-level criteria exist, do not skip
            bool hasPlanLevelCriteria = (criteria.PlanIdFilter?.Count > 0) ||
                                        (criteria.PlanIdExcludeFilter?.Count > 0) ||
                                        (criteria.TargetVolumeIdFilter?.Count > 0) ||
                                        (criteria.TargetVolumeExcludeFilter?.Count > 0) ||
                                        (criteria.DosePerFractionCriteria != null && !criteria.DosePerFractionCriteria.IsEmpty) ||
                                        (criteria.NumberOfFractionsCriteria != null && !criteria.NumberOfFractionsCriteria.IsEmpty) ||
                                        (criteria.TotalDoseCriteria != null && !criteria.TotalDoseCriteria.IsEmpty) ||
                                        (criteria.DosePerFractionGy.HasValue && criteria.DosePerFractionGy.Value > 0) ||
                                        (criteria.NumberOfFractions.HasValue && criteria.NumberOfFractions.Value > 0) ||
                                        (criteria.TotalDoseGy.HasValue && criteria.TotalDoseGy.Value > 0) ||
                                        (criteria.MachineFilter?.Count > 0) || (criteria.MachineExcludeFilter?.Count > 0) ||
                                        (criteria.EnergyFilter?.Count > 0) || (criteria.EnergyExcludeFilter?.Count > 0) ||
                                        (criteria.TechniqueFilter?.Count > 0) || (criteria.TechniqueExcludeFilter?.Count > 0) ||
                                        criteria.DosePresence != DosePresenceFilter.All ||
                                        criteria.DateFrom.HasValue || criteria.DateTo.HasValue;

            return !hasPlanLevelCriteria;
        }

        /// <summary>
        /// Determines whether date falls within the specified range.
        /// </summary>
        public static bool IsDateMatch(DateTime? targetDate, DateTime? dateFrom, DateTime? dateTo)
        {
            if (!dateFrom.HasValue && !dateTo.HasValue)
            {
                return true;
            }

            if (!targetDate.HasValue)
            {
                return false;
            }

            DateTime d = targetDate.Value.Date;
            if (dateFrom.HasValue && d < dateFrom.Value.Date)
            {
                return false;
            }
            if (dateTo.HasValue && d > dateTo.Value.Date)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Determines whether beam list satisfies Machine, Energy, Technique inclusion and exclusion criteria.
        /// </summary>
        public static bool IsBeamMatch(
            IList<BeamRecord> beams,
            IList<string> machineIncludes,
            IList<string> machineExcludes,
            IList<string> energyIncludes,
            IList<string> energyExcludes,
            IList<string> techniqueIncludes,
            IList<string> techniqueExcludes)
        {
            bool hasMachine = (machineIncludes?.Count > 0) || (machineExcludes?.Count > 0);
            bool hasEnergy = (energyIncludes?.Count > 0) || (energyExcludes?.Count > 0);
            bool hasTech = (techniqueIncludes?.Count > 0) || (techniqueExcludes?.Count > 0);

            if (!hasMachine && !hasEnergy && !hasTech)
            {
                return true;
            }

            var txBeams = beams?.Where(b => !b.IsSetupField).ToList();
            if (txBeams == null || txBeams.Count == 0)
            {
                return false;
            }

            // 1. Exclusion check (!): if any beam matches an exclusion criterion, immediately exclude (false)
            if (machineExcludes?.Count > 0 && txBeams.Any(b => EvaluateTokenList(b.TreatmentUnit, machineExcludes, TextMatchMode.Contains)))
            {
                return false;
            }
            if (energyExcludes?.Count > 0 && txBeams.Any(b => EvaluateTokenList(b.EnergyModeDisplayName, energyExcludes, TextMatchMode.Contains)))
            {
                return false;
            }
            if (techniqueExcludes?.Count > 0 && txBeams.Any(b => EvaluateTokenList(b.Technique, techniqueExcludes, TextMatchMode.Contains) ||
                                                                EvaluateTokenList(b.MlcPlanType, techniqueExcludes, TextMatchMode.Contains)))
            {
                return false;
            }

            // 2. Inclusion check: if specified, at least one beam must satisfy each inclusion criterion
            if (machineIncludes?.Count > 0 && !txBeams.Any(b => EvaluateTokenList(b.TreatmentUnit, machineIncludes, TextMatchMode.Contains)))
            {
                return false;
            }
            if (energyIncludes?.Count > 0 && !txBeams.Any(b => EvaluateTokenList(b.EnergyModeDisplayName, energyIncludes, TextMatchMode.Contains)))
            {
                return false;
            }
            if (techniqueIncludes?.Count > 0 && !txBeams.Any(b => EvaluateTokenList(b.Technique, techniqueIncludes, TextMatchMode.Contains) ||
                                                                 EvaluateTokenList(b.MlcPlanType, techniqueIncludes, TextMatchMode.Contains)))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Evaluates comprehensive plan match against Patient ID, Course ID, Plan ID, Target Volume, Dose, Fractions, Date, and Beam metadata.
        /// </summary>
        public static bool IsPlanMatch(
            string patientId,
            string courseId,
            string planId,
            string targetVolumeId,
            double? dosePerFractionGy,
            int? numberOfFractions,
            double? totalDoseGy,
            string approvalStatus,
            bool isPlanSum,
            DateTime? targetDate,
            IList<BeamRecord> beams,
            SearchFilterCriteria criteria)
        {
            if (criteria == null) return true;

            // Check whether to include PlanSums
            if (isPlanSum && !criteria.IncludePlanSums)
            {
                return false;
            }

            // Check approval status (PlanSum has no approval status, so it passes; applied only to PlanSetup)
            if (!isPlanSum && !IsApprovalStatusMatch(approvalStatus, criteria))
            {
                return false;
            }

            // Check dose presence (calculated or uncalculated)
            if (criteria.DosePresence != DosePresenceFilter.All)
            {
                bool hasCalculatedDose = (totalDoseGy.HasValue && !double.IsNaN(totalDoseGy.Value) && !double.IsInfinity(totalDoseGy.Value) && totalDoseGy.Value > 0) ||
                                         (dosePerFractionGy.HasValue && !double.IsNaN(dosePerFractionGy.Value) && !double.IsInfinity(dosePerFractionGy.Value) && dosePerFractionGy.Value > 0);

                if (criteria.DosePresence == DosePresenceFilter.HasDose && !hasCalculatedDose)
                {
                    return false;
                }
                if (criteria.DosePresence == DosePresenceFilter.NoDose && hasCalculatedDose)
                {
                    return false;
                }
            }

            // Check date criteria
            bool hasDateFilter = criteria.DateFrom.HasValue || criteria.DateTo.HasValue;
            bool? matchDate = hasDateFilter
                ? IsDateMatch(targetDate, criteria.DateFrom, criteria.DateTo)
                : (bool?)null;

            // Check beam criteria
            bool hasBeamFilter = (criteria.MachineFilter?.Count > 0) || (criteria.MachineExcludeFilter?.Count > 0) ||
                                 (criteria.EnergyFilter?.Count > 0) || (criteria.EnergyExcludeFilter?.Count > 0) ||
                                 (criteria.TechniqueFilter?.Count > 0) || (criteria.TechniqueExcludeFilter?.Count > 0);
            bool? matchBeam = hasBeamFilter
                ? IsBeamMatch(beams, criteria.MachineFilter, criteria.MachineExcludeFilter, criteria.EnergyFilter, criteria.EnergyExcludeFilter, criteria.TechniqueFilter, criteria.TechniqueExcludeFilter)
                : (bool?)null;

            // Individual criteria evaluation (null if filter not specified)
            bool hasPatientFilter = (criteria.PatientIdFilter?.Count > 0) || (criteria.PatientIdExcludeFilter?.Count > 0);
            bool? matchPatientId = hasPatientFilter
                ? IsTextMatch(patientId, criteria.PatientIdFilter, criteria.PatientIdExcludeFilter, criteria.PatientIdMatchMode)
                : (bool?)null;

            bool hasCourseFilter = (criteria.CourseIdFilter?.Count > 0) || (criteria.CourseIdExcludeFilter?.Count > 0);
            bool? matchCourseId = hasCourseFilter
                ? IsTextMatch(courseId, criteria.CourseIdFilter, criteria.CourseIdExcludeFilter, criteria.CourseIdMatchMode)
                : (bool?)null;

            bool hasPlanFilter = (criteria.PlanIdFilter?.Count > 0) || (criteria.PlanIdExcludeFilter?.Count > 0);
            bool? matchPlanId = hasPlanFilter
                ? IsTextMatch(planId, criteria.PlanIdFilter, criteria.PlanIdExcludeFilter, criteria.PlanIdMatchMode)
                : (bool?)null;

            bool hasTargetFilter = (criteria.TargetVolumeIdFilter?.Count > 0) || (criteria.TargetVolumeExcludeFilter?.Count > 0);
            bool? matchTarget = hasTargetFilter
                ? IsTextMatch(targetVolumeId, criteria.TargetVolumeIdFilter, criteria.TargetVolumeExcludeFilter, criteria.TargetVolumeMatchMode)
                : (bool?)null;

            bool? matchDosePerFr = null;
            if (criteria.DosePerFractionCriteria != null && !criteria.DosePerFractionCriteria.IsEmpty)
            {
                matchDosePerFr = criteria.DosePerFractionCriteria.IsMatch(dosePerFractionGy);
            }
            else if (criteria.DosePerFractionGy.HasValue && criteria.DosePerFractionGy.Value > 0)
            {
                matchDosePerFr = dosePerFractionGy.HasValue && Math.Abs(dosePerFractionGy.Value - criteria.DosePerFractionGy.Value) < 0.05;
            }

            bool? matchFractionCount = null;
            if (criteria.NumberOfFractionsCriteria != null && !criteria.NumberOfFractionsCriteria.IsEmpty)
            {
                matchFractionCount = criteria.NumberOfFractionsCriteria.IsMatchInt(numberOfFractions);
            }
            else if (criteria.NumberOfFractions.HasValue && criteria.NumberOfFractions.Value > 0)
            {
                matchFractionCount = numberOfFractions.HasValue && numberOfFractions.Value == criteria.NumberOfFractions.Value;
            }

            bool? matchTotalDose = null;
            if (criteria.TotalDoseCriteria != null && !criteria.TotalDoseCriteria.IsEmpty)
            {
                matchTotalDose = criteria.TotalDoseCriteria.IsMatch(totalDoseGy);
            }
            else if (criteria.TotalDoseGy.HasValue && criteria.TotalDoseGy.Value > 0)
            {
                matchTotalDose = totalDoseGy.HasValue && Math.Abs(totalDoseGy.Value - criteria.TotalDoseGy.Value) < 0.05;
            }

            // Collect active condition results
            var activeConditions = new List<bool>();
            if (matchPatientId.HasValue) activeConditions.Add(matchPatientId.Value);
            if (matchCourseId.HasValue) activeConditions.Add(matchCourseId.Value);
            if (matchPlanId.HasValue) activeConditions.Add(matchPlanId.Value);
            if (matchTarget.HasValue) activeConditions.Add(matchTarget.Value);
            if (matchDosePerFr.HasValue) activeConditions.Add(matchDosePerFr.Value);
            if (matchFractionCount.HasValue) activeConditions.Add(matchFractionCount.Value);
            if (matchTotalDose.HasValue) activeConditions.Add(matchTotalDose.Value);
            if (matchDate.HasValue) activeConditions.Add(matchDate.Value);
            if (matchBeam.HasValue) activeConditions.Add(matchBeam.Value);

            // If no criteria specified, match all
            if (activeConditions.Count == 0)
            {
                return true;
            }

            // Global logic toggle (AND: match all, OR: match any)
            return criteria.GlobalLogicIsAnd 
                ? activeConditions.All(c => c) 
                : activeConditions.Any(c => c);
        }

        /// <summary>
        /// Backward compatibility overload (without date and beam parameters).
        /// </summary>
        public static bool IsPlanMatch(
            string patientId,
            string courseId,
            string planId,
            string targetVolumeId,
            double? dosePerFractionGy,
            int? numberOfFractions,
            double? totalDoseGy,
            string approvalStatus,
            bool isPlanSum,
            SearchFilterCriteria criteria)
        {
            return IsPlanMatch(patientId, courseId, planId, targetVolumeId, dosePerFractionGy, numberOfFractions, totalDoseGy, approvalStatus, isPlanSum, null, null, criteria);
        }

        /// <summary>
        /// Backward compatibility overload (without patient ID, course ID, date, and beam parameters).
        /// </summary>
        public static bool IsPlanMatch(
            string planId,
            string targetVolumeId,
            double? dosePerFractionGy,
            int? numberOfFractions,
            double? totalDoseGy,
            string approvalStatus,
            bool isPlanSum,
            SearchFilterCriteria criteria)
        {
            return IsPlanMatch(null, null, planId, targetVolumeId, dosePerFractionGy, numberOfFractions, totalDoseGy, approvalStatus, isPlanSum, null, null, criteria);
        }
    }
}
