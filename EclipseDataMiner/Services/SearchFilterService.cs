using System;
using System.Collections.Generic;
using System.Linq;
using EclipseDataMiner.Models;

namespace EclipseDataMiner.Services
{
    /// <summary>
    /// 検索フィルタ条件の評価サービス（フィールド内OR、グローバルAND/OR、除外判定、数値範囲判定、PlanSum判定）
    /// </summary>
    public static class SearchFilterService
    {
        /// <summary>
        /// テキスト値が指定されたフィルタリストおよびマッチモード（部分一致/完全一致/正規表現）に適合するか判定（包含＋除外）
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

            // 1. 除外リストの判定: いずれかに一致した場合は即座に除外（false）
            if (hasExcludes)
            {
                bool matchesExclude = EvaluateTokenList(targetValue, excludeList, mode);
                if (matchesExclude)
                {
                    return false;
                }
            }

            // 2. 包含リストの判定:
            // 包含リストが指定されていない場合（除外のみ指定）、除外をパスしたので一致（true）
            if (!hasIncludes)
            {
                return true;
            }

            // 包含リストのいずれかに一致するか判定
            return EvaluateTokenList(targetValue, includeList, mode);
        }

        /// <summary>
        /// テキスト値が指定されたフィルタリストおよびマッチモードに適合するか判定（後方互換用）
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
        /// 患者IDがフィルタに合致するか判定
        /// </summary>
        public static bool IsPatientMatch(string patientId, SearchFilterCriteria criteria)
        {
            if (criteria == null) return true;
            return IsTextMatch(patientId, criteria.PatientIdFilter, criteria.PatientIdExcludeFilter, criteria.PatientIdMatchMode);
        }

        /// <summary>
        /// コースIDがフィルタに合致するか判定
        /// </summary>
        public static bool IsCourseMatch(string courseId, SearchFilterCriteria criteria)
        {
            if (criteria == null) return true;
            return IsTextMatch(courseId, criteria.CourseIdFilter, criteria.CourseIdExcludeFilter, criteria.CourseIdMatchMode);
        }

        /// <summary>
        /// 承認ステータスが有効な選択肢に含まれるか判定
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

            // いずれの定義済みステータスにも当てはまらない、またはチェックが外れている場合
            return false;
        }

        /// <summary>
        /// 患者を開く前にスキップ可能か判定（ESAPI パフォーマンス最適化）
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

            // AND の場合: 患者IDが一致しなければ、プランが何であれ全体として不一致確定
            if (criteria.GlobalLogicIsAnd)
            {
                return !patientMatches;
            }

            // OR の場合:
            // 患者IDが一致していればスキップしない（この患者自体が条件を満たす）
            if (patientMatches)
            {
                return false;
            }

            // 患者ID不一致の場合でも、他の条件（Course, Plan, Target, Dose等）が指定されていれば、
            // その患者のプランが合致する可能性があるためスキップしてはならない
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
        /// コース単位でスキップ可能かを判定
        /// </summary>
        public static bool ShouldSkipCourse(string courseId, SearchFilterCriteria criteria)
        {
            if (criteria == null) return false;

            bool courseMatches = IsCourseMatch(courseId, criteria);

            // AND の場合: コースIDが一致しなければスキップ
            if (criteria.GlobalLogicIsAnd)
            {
                return !courseMatches;
            }

            // OR の場合:
            if (courseMatches)
            {
                return false;
            }

            // コースID不一致でも、PlanやTargetやDose等の下位条件が指定されていればスキップしてはならない
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
        /// 日付が指定範囲に適合するか判定
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
        /// ビーム一覧が Machine, Energy, Technique の包含・除外条件に適合するか判定
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

            // 1. 除外チェック (!): いずれかのビームが除外条件に合致した場合は即座に除外 (false)
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

            // 2. 包含チェック: 指定されている場合、条件を満たすビームが1門以上存在しなければならない
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
        /// プランの詳細条件（Patient ID, Course ID, Plan ID, Target Volume, 線量, 分割数, 日付, ビームメタデータ）に対する総合判定
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

            // PlanSum を含めるかどうかのチェック
            if (isPlanSum && !criteria.IncludePlanSums)
            {
                return false;
            }

            // 承認状態チェック（PlanSum は承認ステータスを持たないためパス、PlanSetup のみ適用）
            if (!isPlanSum && !IsApprovalStatusMatch(approvalStatus, criteria))
            {
                return false;
            }

            // 線量有無（計算済み／未計算）チェック
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

            // 日付条件チェック
            bool hasDateFilter = criteria.DateFrom.HasValue || criteria.DateTo.HasValue;
            bool? matchDate = hasDateFilter
                ? IsDateMatch(targetDate, criteria.DateFrom, criteria.DateTo)
                : (bool?)null;

            // ビーム条件チェック
            bool hasBeamFilter = (criteria.MachineFilter?.Count > 0) || (criteria.MachineExcludeFilter?.Count > 0) ||
                                 (criteria.EnergyFilter?.Count > 0) || (criteria.EnergyExcludeFilter?.Count > 0) ||
                                 (criteria.TechniqueFilter?.Count > 0) || (criteria.TechniqueExcludeFilter?.Count > 0);
            bool? matchBeam = hasBeamFilter
                ? IsBeamMatch(beams, criteria.MachineFilter, criteria.MachineExcludeFilter, criteria.EnergyFilter, criteria.EnergyExcludeFilter, criteria.TechniqueFilter, criteria.TechniqueExcludeFilter)
                : (bool?)null;

            // 各条件の個別判定（フィルタ未指定時は null）
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

            // 指定されたアクティブな条件リストを収集
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

            // 条件が1つも指定されていない場合は全件一致
            if (activeConditions.Count == 0)
            {
                return true;
            }

            // グローバル論理切替（AND: すべて満たす, OR: いずれかを満たす）
            return criteria.GlobalLogicIsAnd 
                ? activeConditions.All(c => c) 
                : activeConditions.Any(c => c);
        }

        /// <summary>
        /// 後方互換用オーバーロード（日付, ビーム未指定）
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
        /// 後方互換用オーバーロード（Patient ID, Course ID, 日付, ビーム未指定）
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
