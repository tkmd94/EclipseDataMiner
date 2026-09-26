using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EclipseDataMiner.Helpers;
using EclipseDataMiner.Models;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace EclipseDataMiner.Services
{
    /// <summary>
    /// 進捗通知用データ
    /// </summary>
    public class ExtractionProgressInfo
    {
        public int Percentage { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ProcessedPatients { get; set; }
        public int TotalPatients { get; set; }
        public int ExtractedPlans { get; set; }
    }

    /// <summary>
    /// 専用 STA スレッド上で ESAPI 走査・抽出・メモリ管理を実行するサービス
    /// </summary>
    public class StaEsapiWorkerService
    {
        private const int GarbageCollectionInterval = 200;

        /// <summary>
        /// 検索条件に合致するプランの輪郭IDを高速スキャン（事前マッピング用）
        /// </summary>
        public Task<List<DiscoveredStructureItem>> RunPreScanAsync(
            SearchFilterCriteria criteria,
            IList<StructureMappingRule> mappingRules,
            IProgress<ExtractionProgressInfo> progress,
            CancellationToken cancellationToken,
            HashSet<string> targetPlanKeys = null)
        {
            var tcs = new TaskCompletionSource<List<DiscoveredStructureItem>>();

            var thread = new Thread(() =>
            {
                VMS.TPS.Common.Model.API.Application app = null;
                try
                {
                    app = VMS.TPS.Common.Model.API.Application.CreateApplication();
                    var structureCountMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                    var patientSummaries = app.PatientSummaries.ToList();
                    int total = patientSummaries.Count;
                    int count = 0;

                    HashSet<string> targetPatientIds = null;
                    if (targetPlanKeys != null && targetPlanKeys.Count > 0)
                    {
                        targetPatientIds = new HashSet<string>(
                            targetPlanKeys.Select(k => k.Split('|')[0]),
                            StringComparer.OrdinalIgnoreCase);
                    }

                    foreach (var patsum in patientSummaries)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        // 選択されたプラン一覧がある場合、その対象患者でなければ高速スキップ
                        if (targetPatientIds != null && !targetPatientIds.Contains(patsum.Id))
                        {
                            count++;
                            continue;
                        }

                        // 選択患者であるか、または検索条件に合致する患者のみオープン
                        if (targetPatientIds != null || !SearchFilterService.ShouldSkipPatient(patsum.Id, criteria))
                        {
                            Patient pat = null;
                            try
                            {
                                pat = app.OpenPatient(patsum);
                                if (pat != null)
                                {
                                    ScanPatientStructures(pat, criteria, structureCountMap, cancellationToken, targetPlanKeys);
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                throw;
                            }
                            catch (Exception)
                            {
                                // 患者オープン失敗時は安全にスキップ
                            }
                            finally
                            {
                                if (pat != null)
                                {
                                    try { app.ClosePatient(); } catch { }
                                }
                            }
                        }

                        count++;
                        if (count % 20 == 0 || count == total)
                        {
                            int pct = (int)((double)count / total * 100.0);
                            progress?.Report(new ExtractionProgressInfo
                            {
                                Percentage = pct,
                                Message = $"Pre-scanning patient {count}/{total} ({structureCountMap.Count} unique structures found)...",
                                ProcessedPatients = count,
                                TotalPatients = total
                            });
                        }

                        if (count % GarbageCollectionInterval == 0)
                        {
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                        }
                    }

                    var discoveredList = structureCountMap.Select(kv => new DiscoveredStructureItem
                    {
                        RawStructureId = kv.Key,
                        HitCount = kv.Value,
                        ResolvedAlias = kv.Key,
                        IsExtracted = true,
                        MatchStatus = "Unmapped (Raw)"
                    }).OrderByDescending(d => d.HitCount).ToList();

                    // 既存のルールを適用してプレビュー状態を解決
                    StructureMappingService.RefreshPreview(discoveredList, mappingRules);

                    tcs.SetResult(discoveredList);
                }
                catch (OperationCanceledException)
                {
                    tcs.SetCanceled();
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
                finally
                {
                    try { app?.Dispose(); } catch { }
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();

            return tcs.Task;
        }

        /// <summary>
        /// 検索条件に合致するプラン一覧を軽量走査（メタデータのみ読み取り、線量計算は行わない）
        /// </summary>
        public Task<List<MatchedPlanItem>> SearchPlansAsync(
            SearchFilterCriteria criteria,
            IProgress<ExtractionProgressInfo> progress,
            CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<List<MatchedPlanItem>>();

            var thread = new Thread(() =>
            {
                VMS.TPS.Common.Model.API.Application app = null;
                try
                {
                    app = VMS.TPS.Common.Model.API.Application.CreateApplication();
                    var matchedList = new List<MatchedPlanItem>();
                    var patientSummaries = app.PatientSummaries.ToList();
                    int total = patientSummaries.Count;
                    int count = 0;

                    foreach (var patsum in patientSummaries)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (!SearchFilterService.ShouldSkipPatient(patsum.Id, criteria))
                        {
                            Patient pat = null;
                            try
                            {
                                pat = app.OpenPatient(patsum);
                                if (pat != null)
                                {
                                    ScanPatientPlansForSearch(pat, criteria, matchedList, cancellationToken);
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                throw;
                            }
                            catch (Exception)
                            {
                                // 患者オープン失敗時は安全にスキップ
                            }
                            finally
                            {
                                if (pat != null)
                                {
                                    try { app.ClosePatient(); } catch { }
                                }
                            }
                        }

                        count++;
                        if (count % 20 == 0 || count == total)
                        {
                            int pct = (int)((double)count / total * 100.0);
                            progress?.Report(new ExtractionProgressInfo
                            {
                                Percentage = pct,
                                Message = $"Searching patient {count}/{total} (Found {matchedList.Count} plans)...",
                                ProcessedPatients = count,
                                TotalPatients = total,
                                ExtractedPlans = matchedList.Count
                            });
                        }

                        if (count % GarbageCollectionInterval == 0)
                        {
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                        }
                    }

                    tcs.SetResult(matchedList);
                }
                catch (OperationCanceledException)
                {
                    tcs.SetCanceled();
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
                finally
                {
                    try { app?.Dispose(); } catch { }
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();

            return tcs.Task;
        }

        /// <summary>
        /// データマイニング本抽出処理（ストリーミング出力）
        /// </summary>
        public Task<int> RunExtractionAsync(
            SearchFilterCriteria criteria,
            ExtractionOptions options,
            IList<DQP> dqpDefinitions,
            IList<StructureMappingRule> mappingRules,
            string outputCsvPath,
            IProgress<ExtractionProgressInfo> progress,
            CancellationToken cancellationToken,
            HashSet<string> targetPlanKeys = null)
        {
            var tcs = new TaskCompletionSource<int>();

            var thread = new Thread(() =>
            {
                VMS.TPS.Common.Model.API.Application app = null;
                StreamingExportPipeline pipeline = null;

                try
                {
                    // DQP出力ヘッダーの生成
                    var dqpHeaders = BuildDqpColumns(dqpDefinitions, mappingRules);

                    // パイプラインの初期化
                    pipeline = new StreamingExportPipeline();
                    pipeline.Initialize(outputCsvPath, options, dqpHeaders);

                    app = VMS.TPS.Common.Model.API.Application.CreateApplication();
                    var patientSummaries = app.PatientSummaries.ToList();
                    int total = patientSummaries.Count;
                    int processedCount = 0;
                    int extractedPlanCount = 0;

                    HashSet<string> targetPatientIds = null;
                    if (targetPlanKeys != null && targetPlanKeys.Count > 0)
                    {
                        targetPatientIds = new HashSet<string>(
                            targetPlanKeys.Select(k => k.Split('|')[0]),
                            StringComparer.OrdinalIgnoreCase);
                    }

                    foreach (var patsum in patientSummaries)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (targetPatientIds != null && !targetPatientIds.Contains(patsum.Id))
                        {
                            processedCount++;
                            continue;
                        }

                        if (targetPatientIds != null || !SearchFilterService.ShouldSkipPatient(patsum.Id, criteria))
                        {
                            Patient pat = null;
                            try
                            {
                                pat = app.OpenPatient(patsum);
                                if (pat != null)
                                {
                                    extractedPlanCount += ExtractPatientPlans(pat, criteria, options, dqpDefinitions, mappingRules, pipeline, cancellationToken, targetPlanKeys);
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                throw;
                            }
                            catch (Exception ex)
                            {
                                progress?.Report(new ExtractionProgressInfo
                                {
                                    Message = $"[Warning] Failed to process patient {patsum.Id}: {ex.Message}"
                                });
                            }
                            finally
                            {
                                if (pat != null)
                                {
                                    try { app.ClosePatient(); } catch { }
                                }
                            }
                        }

                        processedCount++;
                        if (processedCount % 10 == 0 || processedCount == total)
                        {
                            int pct = (int)((double)processedCount / total * 100.0);
                            progress?.Report(new ExtractionProgressInfo
                            {
                                Percentage = pct,
                                Message = $"Processing patient {processedCount}/{total} (Extracted plans: {extractedPlanCount})...",
                                ProcessedPatients = processedCount,
                                TotalPatients = total,
                                ExtractedPlans = extractedPlanCount
                            });
                        }

                        // 定期的なGCによるアンマネージドリソース解放
                        if (processedCount % GarbageCollectionInterval == 0)
                        {
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                        }
                    }

                    tcs.SetResult(extractedPlanCount);
                }
                catch (OperationCanceledException)
                {
                    tcs.SetCanceled();
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
                finally
                {
                    try { pipeline?.Dispose(); } catch { }
                    try { app?.Dispose(); } catch { }
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();

            return tcs.Task;
        }

        #region Helper Methods

        private void ScanPatientStructures(
            Patient patient,
            SearchFilterCriteria criteria,
            Dictionary<string, int> structureCountMap,
            CancellationToken cancellationToken,
            HashSet<string> targetPlanKeys = null)
        {
            foreach (Course course in patient.Courses)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (SearchFilterService.ShouldSkipCourse(course.Id, criteria)) continue;

                // PlanSetup のスキャン
                foreach (PlanSetup plan in course.PlanSetups)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string planKey = $"{patient.Id}|{course.Id}|{plan.Id}";
                    if (targetPlanKeys != null && targetPlanKeys.Count > 0 && !targetPlanKeys.Contains(planKey))
                    {
                        continue;
                    }

                    string targetId = plan.TargetVolumeID;
                    double? dosePerFr = GetSafePlanDose(plan.DosePerFraction);
                    int? numFr = plan.NumberOfFractions;
                    double? totalDose = GetSafePlanDose(plan.TotalDose);
                    string status = plan.ApprovalStatus.ToString();

                    var allDates = ResolveAllPlanDates(plan);
                    DateTime? targetDate = ResolvePlanDate(allDates.CreationDate, allDates.PlanningApprovalDate, allDates.TreatmentApprovalDate, criteria.DateTarget);
                    var beamRecords = ExtractBeamRecords(plan);

                    bool planMatches = (targetPlanKeys != null && targetPlanKeys.Count > 0)
                        ? targetPlanKeys.Contains(planKey)
                        : SearchFilterService.IsPlanMatch(patient.Id, course.Id, plan.Id, targetId, dosePerFr, numFr, totalDose, status, false, targetDate, beamRecords, criteria);

                    if (planMatches)
                    {
                        if (plan.StructureSet != null)
                        {
                            foreach (Structure s in plan.StructureSet.Structures)
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                if (string.IsNullOrEmpty(s.Id)) continue;
                                if (!structureCountMap.ContainsKey(s.Id))
                                {
                                    structureCountMap[s.Id] = 0;
                                }
                                structureCountMap[s.Id]++;
                            }
                        }
                    }
                }

                // PlanSum のスキャン
                if (criteria.IncludePlanSums)
                {
                    foreach (PlanSum sum in course.PlanSums)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        string sumKey = $"{patient.Id}|{course.Id}|{sum.Id}";
                        if (targetPlanKeys != null && targetPlanKeys.Count > 0 && !targetPlanKeys.Contains(sumKey))
                        {
                            continue;
                        }

                        string status = "PlanSum";
                        DateTime? sumCreation = null;
                        try { sumCreation = sum.CreationDateTime; } catch { }
                        bool sumHasDose = false;
                        try { sumHasDose = sum.Dose != null; } catch { }
                        double? sumDoseMarker = sumHasDose ? 1.0 : (double?)null;

                        bool sumMatches = (targetPlanKeys != null && targetPlanKeys.Count > 0)
                            ? targetPlanKeys.Contains(sumKey)
                            : SearchFilterService.IsPlanMatch(patient.Id, course.Id, sum.Id, null, null, null, sumDoseMarker, status, true, sumCreation, null, criteria);

                        if (sumMatches)
                        {
                            if (sum.StructureSet != null)
                            {
                                foreach (Structure s in sum.StructureSet.Structures)
                                {
                                    cancellationToken.ThrowIfCancellationRequested();
                                    if (string.IsNullOrEmpty(s.Id)) continue;
                                    if (!structureCountMap.ContainsKey(s.Id))
                                    {
                                        structureCountMap[s.Id] = 0;
                                    }
                                    structureCountMap[s.Id]++;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void ScanPatientPlansForSearch(
            Patient patient,
            SearchFilterCriteria criteria,
            List<MatchedPlanItem> matchedList,
            CancellationToken cancellationToken)
        {
            foreach (Course course in patient.Courses)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (SearchFilterService.ShouldSkipCourse(course.Id, criteria)) continue;

                foreach (PlanSetup plan in course.PlanSetups)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string targetId = plan.TargetVolumeID;
                    double? dosePerFr = GetSafePlanDose(plan.DosePerFraction);
                    int? numFr = plan.NumberOfFractions;
                    double? totalDose = GetSafePlanDose(plan.TotalDose);
                    string status = plan.ApprovalStatus.ToString();

                    var allDates = ResolveAllPlanDates(plan);
                    DateTime? targetDate = ResolvePlanDate(allDates.CreationDate, allDates.PlanningApprovalDate, allDates.TreatmentApprovalDate, criteria.DateTarget);
                    var beamRecords = ExtractBeamRecords(plan);

                    if (SearchFilterService.IsPlanMatch(patient.Id, course.Id, plan.Id, targetId, dosePerFr, numFr, totalDose, status, false, targetDate, beamRecords, criteria))
                    {
                        // 複数ビームが存在する場合でもすべての装置・エネルギー・照射手法を重複なくカンマ区切りで集約
                        string machines = "-";
                        string energies = "-";
                        string techniques = "-";

                        if (beamRecords != null && beamRecords.Count > 0)
                        {
                            var machList = beamRecords.Select(b => b.TreatmentUnit).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
                            if (machList.Count > 0) machines = string.Join(", ", machList);

                            var nrgList = beamRecords.Select(b => b.EnergyModeDisplayName).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
                            if (nrgList.Count > 0) energies = string.Join(", ", nrgList);

                            var techList = beamRecords.Select(b => b.Technique).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
                            if (techList.Count > 0) techniques = string.Join(", ", techList);
                        }

                        matchedList.Add(new MatchedPlanItem
                        {
                            IsSelected = true,
                            PatientId = patient.Id,
                            CourseId = course.Id,
                            PlanId = plan.Id,
                            PlanType = "PlanSetup",
                            ApprovalStatus = status,
                            DosePerFraction = dosePerFr,
                            NumberOfFractions = numFr,
                            TotalDose = totalDose,
                            Machine = machines,
                            Energy = energies,
                            Technique = techniques,
                            CreationDate = allDates.CreationDate,
                            PlanningApprovalDate = allDates.PlanningApprovalDate,
                            TreatmentApprovalDate = allDates.TreatmentApprovalDate,
                            TargetDate = targetDate,
                            DateTargetLabel = criteria.DateTarget.ToString(),
                            TargetVolumeId = targetId ?? string.Empty,
                            HasDose = totalDose.HasValue && totalDose.Value > 0
                        });
                    }
                }

                if (criteria.IncludePlanSums)
                {
                    foreach (PlanSum sum in course.PlanSums)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        string status = "PlanSum";
                        DateTime? sumCreation = null;
                        try { sumCreation = sum.CreationDateTime; } catch { }
                        bool sumHasDose = false;
                        try { sumHasDose = sum.Dose != null; } catch { }
                        double? sumDoseMarker = sumHasDose ? 1.0 : (double?)null;

                        if (SearchFilterService.IsPlanMatch(patient.Id, course.Id, sum.Id, null, null, null, sumDoseMarker, status, true, sumCreation, null, criteria))
                        {
                            matchedList.Add(new MatchedPlanItem
                            {
                                IsSelected = true,
                                PatientId = patient.Id,
                                CourseId = course.Id,
                                PlanId = sum.Id,
                                PlanType = "PlanSum",
                                ApprovalStatus = status,
                                DosePerFraction = null,
                                NumberOfFractions = null,
                                TotalDose = null,
                                Machine = "-",
                                Energy = "-",
                                Technique = "-",
                                CreationDate = sumCreation,
                                PlanningApprovalDate = null,
                                TreatmentApprovalDate = null,
                                TargetDate = sumCreation,
                                DateTargetLabel = criteria.DateTarget.ToString(),
                                TargetVolumeId = "-",
                                HasDose = sumHasDose
                            });
                        }
                    }
                }
            }
        }

        private int ExtractPatientPlans(
            Patient patient,
            SearchFilterCriteria criteria,
            ExtractionOptions options,
            IList<DQP> dqpDefinitions,
            IList<StructureMappingRule> mappingRules,
            StreamingExportPipeline pipeline,
            CancellationToken cancellationToken,
            HashSet<string> targetPlanKeys = null)
        {
            int count = 0;

            foreach (Course course in patient.Courses)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (SearchFilterService.ShouldSkipCourse(course.Id, criteria)) continue;

                // PlanSetup の抽出
                foreach (PlanSetup plan in course.PlanSetups)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string planKey = $"{patient.Id}|{course.Id}|{plan.Id}";
                    if (targetPlanKeys != null && targetPlanKeys.Count > 0 && !targetPlanKeys.Contains(planKey))
                    {
                        continue;
                    }

                    string targetId = plan.TargetVolumeID;
                    double? dosePerFr = GetSafePlanDose(plan.DosePerFraction);
                    int? numFr = plan.NumberOfFractions;
                    double? totalDose = GetSafePlanDose(plan.TotalDose);
                    string status = plan.ApprovalStatus.ToString();

                    DateTime? targetDate = ResolvePlanDate(plan, criteria.DateTarget);
                    var beamRecords = ExtractBeamRecords(plan);

                    bool planMatches = (targetPlanKeys != null && targetPlanKeys.Count > 0)
                        ? targetPlanKeys.Contains(planKey)
                        : SearchFilterService.IsPlanMatch(patient.Id, course.Id, plan.Id, targetId, dosePerFr, numFr, totalDose, status, false, targetDate, beamRecords, criteria);

                    if (planMatches)
                    {
                        var record = BuildPlanSetupRecord(patient, course, plan, options, dqpDefinitions, mappingRules, cancellationToken);
                        pipeline.WritePlanRecord(record);
                        count++;
                    }
                }

                // PlanSum の抽出（オプション有効時のみ）
                if (criteria.IncludePlanSums)
                {
                    foreach (PlanSum sum in course.PlanSums)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        string sumKey = $"{patient.Id}|{course.Id}|{sum.Id}";
                        if (targetPlanKeys != null && targetPlanKeys.Count > 0 && !targetPlanKeys.Contains(sumKey))
                        {
                            continue;
                        }

                        string status = "PlanSum";
                        DateTime? sumCreation = null;
                        try { sumCreation = sum.CreationDateTime; } catch { }
                        bool sumHasDose = false;
                        try { sumHasDose = sum.Dose != null; } catch { }
                        double? sumDoseMarker = sumHasDose ? 1.0 : (double?)null;

                        bool sumMatches = (targetPlanKeys != null && targetPlanKeys.Count > 0)
                            ? targetPlanKeys.Contains(sumKey)
                            : SearchFilterService.IsPlanMatch(patient.Id, course.Id, sum.Id, null, null, null, sumDoseMarker, status, true, sumCreation, null, criteria);

                        if (sumMatches)
                        {
                            var record = BuildPlanSumRecord(patient, course, sum, options, dqpDefinitions, mappingRules, cancellationToken);
                            pipeline.WritePlanRecord(record);
                            count++;
                        }
                    }
                }
            }

            return count;
        }

        private static double? GetSafePlanDose(DoseValue doseValue)
        {
            try
            {
                double val = doseValue.ToGy();
                if (double.IsNaN(val) || double.IsInfinity(val)) return null;
                return val;
            }
            catch
            {
                return null;
            }
        }

        private struct PlanDates
        {
            public DateTime? CreationDate;
            public DateTime? PlanningApprovalDate;
            public DateTime? TreatmentApprovalDate;
        }

        private static PlanDates ResolveAllPlanDates(PlanSetup plan)
        {
            DateTime? creationDate = null;
            try { creationDate = plan.CreationDateTime; } catch { }

            DateTime? planningApprovalDate = null;
            try
            {
                if (DateTime.TryParse(plan.PlanningApprovalDate, out DateTime pad))
                {
                    planningApprovalDate = pad;
                }
            }
            catch { }

            DateTime? treatmentApprovalDate = null;
            try
            {
                var prop = plan.GetType().GetProperty("TreatmentApprovalDate");
                if (prop != null)
                {
                    var val = prop.GetValue(plan, null);
                    if (val is DateTime dt) treatmentApprovalDate = dt;
                    else if (val is string str && DateTime.TryParse(str, out DateTime pdt)) treatmentApprovalDate = pdt;
                }
            }
            catch { }

            if (!treatmentApprovalDate.HasValue && planningApprovalDate.HasValue)
            {
                treatmentApprovalDate = planningApprovalDate;
            }

            return new PlanDates
            {
                CreationDate = creationDate,
                PlanningApprovalDate = planningApprovalDate,
                TreatmentApprovalDate = treatmentApprovalDate
            };
        }

        private static DateTime? ResolvePlanDate(DateTime? creationDate, DateTime? planningApprovalDate, DateTime? treatmentApprovalDate, DateFilterTarget dateTarget)
        {
            switch (dateTarget)
            {
                case DateFilterTarget.PlanningApprovalDate:
                    return planningApprovalDate;
                case DateFilterTarget.CreationDate:
                    return creationDate;
                case DateFilterTarget.TreatmentApprovalDate:
                default:
                    return treatmentApprovalDate;
            }
        }

        private static DateTime? ResolvePlanDate(PlanSetup plan, DateFilterTarget dateTarget)
        {
            var dates = ResolveAllPlanDates(plan);
            return ResolvePlanDate(dates.CreationDate, dates.PlanningApprovalDate, dates.TreatmentApprovalDate, dateTarget);
        }

        private static List<BeamRecord> ExtractBeamRecords(PlanSetup plan)
        {
            if (plan.Beams == null) return null;
            var beamRecords = new List<BeamRecord>();
            foreach (Beam b in plan.Beams)
            {
                if (b.IsSetupField) continue;
                beamRecords.Add(new BeamRecord
                {
                    BeamId = b.Id,
                    IsSetupField = b.IsSetupField,
                    TreatmentUnit = b.TreatmentUnit?.Id ?? string.Empty,
                    EnergyModeDisplayName = b.EnergyModeDisplayName ?? string.Empty,
                    Technique = b.Technique?.Id ?? string.Empty,
                    MlcPlanType = b.MLCPlanType.ToString()
                });
            }
            return beamRecords;
        }

        private ExtractionPlanRecord BuildPlanSetupRecord(
            Patient patient,
            Course course,
            PlanSetup plan,
            ExtractionOptions options,
            IList<DQP> dqpDefinitions,
            IList<StructureMappingRule> mappingRules,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var record = new ExtractionPlanRecord
            {
                PatientId = patient.Id,
                CourseId = course.Id,
                PlanId = plan.Id,
                IsPlanSum = false,
                DateOfBirth = patient.DateOfBirth,
                TargetVolumeId = plan.TargetVolumeID,
                DosePerFractionGy = plan.DosePerFraction.ToGy(),
                NumberOfFractions = plan.NumberOfFractions,
                TotalDoseGy = plan.TotalDose.ToGy(),
                ApprovalStatus = plan.ApprovalStatus.ToString(),
                PlanningApprover = plan.PlanningApprover,
                PlanningApprovalDate = plan.PlanningApprovalDate
            };

            // ビーム情報の収集
            int beamCount = 0;
            foreach (Beam b in plan.Beams)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (b.IsSetupField) continue;
                beamCount++;

                var beamRecord = new BeamRecord
                {
                    BeamId = b.Id,
                    IsSetupField = b.IsSetupField,
                    MetersetMU = b.Meterset.Value,
                    TreatmentUnit = b.TreatmentUnit?.Id ?? string.Empty,
                    EnergyModeDisplayName = b.EnergyModeDisplayName ?? string.Empty,
                    Technique = b.Technique?.Id ?? string.Empty,
                    MlcPlanType = b.MLCPlanType.ToString(),
                    ArcLength = b.ArcLength
                };

                if (options.ExportCalculationLog)
                {
                    record.CalculationLogs.Add($"#B{beamCount}#");

                    if (b.CalculationLogs != null)
                    {
                        foreach (var log in b.CalculationLogs)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            if (log.MessageLines == null) continue;

                            int lineIdx = 0;
                            foreach (var line in log.MessageLines)
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                string cleanedLine = line?.Replace("\r\n", " ")
                                                          .Replace("\n", " ")
                                                          .Replace("\r", " ") ?? string.Empty;
                                string logEntry = $"LOG:{lineIdx}{cleanedLine}";
                                beamRecord.CalculationLogs.Add(logEntry);
                                record.CalculationLogs.Add(logEntry);
                                lineIdx++;
                            }
                        }
                    }
                }

                record.Beams.Add(beamRecord);
            }
            record.NumberOfBeams = beamCount;

            // 計算モデル
            record.CalculationModelPhoton = plan.PhotonCalculationModel ?? string.Empty;
            record.CalculationModelElectron = plan.ElectronCalculationModel ?? string.Empty;
            record.PlanNormalizationMethod = plan.PlanNormalizationMethod ?? string.Empty;

            // 臨床プロトコル
            if (options.ExportClinicalProtocol)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    record.ClinicalProtocolSummary = GetClinicalProtocolParameters.GetParameters(patient, plan);
                }
                catch (OperationCanceledException) { throw; }
                catch { }
            }

            // 最適化設定
            if (options.ExportOptimizationObjectives && plan.OptimizationSetup != null)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    ExtractOptimizationObjectives(plan.OptimizationSetup, record, cancellationToken);
                }
                catch (OperationCanceledException) { throw; }
                catch { }
            }

            // プラン複雑性
            if (options.ExportPlanComplexity)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    record.PlanComplexitySummary = PlanComplexityAnalysis.Proccess(plan);
                }
                catch (OperationCanceledException) { throw; }
                catch { }
            }

            // DVH統計およびDQP指標の計算
            if (plan.StructureSet != null && plan.Dose != null)
            {
                ExtractDvhAndDqp(plan, plan.StructureSet, dqpDefinitions, mappingRules, record, cancellationToken);
            }

            return record;
        }

        private ExtractionPlanRecord BuildPlanSumRecord(
            Patient patient,
            Course course,
            PlanSum sum,
            ExtractionOptions options,
            IList<DQP> dqpDefinitions,
            IList<StructureMappingRule> mappingRules,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var record = new ExtractionPlanRecord
            {
                PatientId = patient.Id,
                CourseId = course.Id,
                PlanId = sum.Id,
                IsPlanSum = true,
                DateOfBirth = patient.DateOfBirth,
                TargetVolumeId = StringSanitizer.NotApplicable,
                DosePerFractionGy = null, // PlanSum には1回線量の単一概念がないため null
                NumberOfFractions = null,
                TotalDoseGy = null,
                NumberOfBeams = 0,
                ApprovalStatus = "PlanSum",
                PlanningApprover = StringSanitizer.NotApplicable,
                PlanningApprovalDate = StringSanitizer.NotApplicable,
                CalculationModelPhoton = StringSanitizer.NotApplicable,
                CalculationModelElectron = StringSanitizer.NotApplicable,
                PlanNormalizationMethod = StringSanitizer.NotApplicable,
                ClinicalProtocolSummary = StringSanitizer.NotApplicable,
                PlanComplexitySummary = StringSanitizer.NotApplicable
            };

            // DVH統計およびDQP指標の計算 (PlanSum)
            if (sum.StructureSet != null && sum.Dose != null)
            {
                ExtractDvhAndDqp(sum, sum.StructureSet, dqpDefinitions, mappingRules, record, cancellationToken);
            }

            return record;
        }

        private void ExtractOptimizationObjectives(OptimizationSetup optSetup, ExtractionPlanRecord record, CancellationToken cancellationToken)
        {
            if (optSetup.Parameters != null)
            {
                foreach (var param in optSetup.Parameters.OfType<OptimizationNormalTissueParameter>())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    record.OptimizationObjectives.Add(new OptimizationObjectiveRecord
                    {
                        IsNTO = true,
                        ObjectiveType = "NTO",
                        DistanceFromTargetBorderInMM = param.DistanceFromTargetBorderInMM,
                        EndDosePercentage = param.EndDosePercentage,
                        StartDosePercentage = param.StartDosePercentage,
                        FallOff = param.FallOff,
                        IsAutomatic = param.IsAutomatic,
                        Priority = param.Priority
                    });
                }
            }

            if (optSetup.Objectives != null)
            {
                foreach (var obj in optSetup.Objectives.OfType<OptimizationPointObjective>())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    record.OptimizationObjectives.Add(new OptimizationObjectiveRecord
                    {
                        ObjectiveType = "Point",
                        StructureId = obj.Structure?.Id ?? string.Empty,
                        Operator = obj.Operator.ToString(),
                        DoseGy = obj.Dose.ToGy(),
                        Volume = obj.Volume,
                        Priority = obj.Priority
                    });
                }
                foreach (var obj in optSetup.Objectives.OfType<OptimizationEUDObjective>())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    record.OptimizationObjectives.Add(new OptimizationObjectiveRecord
                    {
                        ObjectiveType = "EUD",
                        StructureId = obj.Structure?.Id ?? string.Empty,
                        Operator = obj.Operator.ToString(),
                        DoseGy = obj.Dose.ToGy(),
                        ParameterA = obj.ParameterA,
                        Priority = obj.Priority
                    });
                }
                foreach (var obj in optSetup.Objectives.OfType<OptimizationMeanDoseObjective>())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    record.OptimizationObjectives.Add(new OptimizationObjectiveRecord
                    {
                        ObjectiveType = "MeanDose",
                        StructureId = obj.Structure?.Id ?? string.Empty,
                        Operator = obj.Operator.ToString(),
                        DoseGy = obj.Dose.ToGy(),
                        Priority = obj.Priority
                    });
                }
            }
        }

        private void ExtractDvhAndDqp(
            PlanningItem planItem,
            StructureSet structureSet,
            IList<DQP> dqpDefinitions,
            IList<StructureMappingRule> mappingRules,
            ExtractionPlanRecord record,
            CancellationToken cancellationToken)
        {
            if (structureSet == null || structureSet.Structures == null) return;

            // 処方総線量（PlanSetupの場合は TotalDose）
            DoseValue prescriptionTotalDose = DoseValue.UndefinedDose();
            if (planItem is PlanSetup ps)
            {
                prescriptionTotalDose = ps.TotalDose;
            }

            // 1. 基本統計量（Volume, Max, Mean, Min）を計算
            // 対象: マッピングルールで選択された輪郭および DQP リストに指定されている輪郭（エイリアス含む）
            var distinctStructNames = GetTargetStructureNames(dqpDefinitions, mappingRules);

            foreach (var sName in distinctStructNames)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var matchedStructures = structureSet.Structures
                    .Where(s => string.Equals(s.Id, sName, StringComparison.OrdinalIgnoreCase) ||
                                StructureMappingService.ResolveMapping(s.Id, mappingRules).TargetAlias.Equals(sName, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var structure in matchedStructures)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var resolved = StructureMappingService.ResolveMapping(structure.Id, mappingRules);
                    if (!resolved.IsSelected) continue;

                    try
                    {
                        var dvh = planItem.GetDVHCumulativeData(structure, DoseValuePresentation.Absolute, VolumePresentation.AbsoluteCm3, 0.1);
                        if (dvh == null) continue;

                        record.DvhMetrics.Add(new DvhMetricResult
                        {
                            OriginalStructureId = structure.Id,
                            TargetAlias = resolved.TargetAlias,
                            StructureVolumeCc = structure.Volume,
                            MaxDoseGy = dvh.MaxDose.ToGy(),
                            MeanDoseGy = dvh.MeanDose.ToGy(),
                            MinDoseGy = dvh.MinDose.ToGy(),
                            MetricKey = "BASIC_STATS",
                            Value = null,
                            Unit = "Gy"
                        });
                    }
                    catch (OperationCanceledException) { throw; }
                    catch { }
                }
            }

            // 2. DQP指標の計算
            if (dqpDefinitions == null || dqpDefinitions.Count == 0) return;

            foreach (var dqp in dqpDefinitions)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(dqp.structureName)) continue;

                var matchedStructures = structureSet.Structures
                    .Where(s => string.Equals(s.Id, dqp.structureName, StringComparison.OrdinalIgnoreCase) ||
                                StructureMappingService.ResolveMapping(s.Id, mappingRules).TargetAlias.Equals(dqp.structureName, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var structure in matchedStructures)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var resolved = StructureMappingService.ResolveMapping(structure.Id, mappingRules);
                    if (!resolved.IsSelected) continue;

                    try
                    {
                        var dvh = planItem.GetDVHCumulativeData(structure, DoseValuePresentation.Absolute, VolumePresentation.AbsoluteCm3, 0.1);
                        if (dvh == null) continue;

                        string metricKey = $"{dqp.DQPtype}_{dqp.DQPvalue}_{dqp.InputUnit}_{dqp.OutputUnit}";
                        double? calculatedVal = null;
                        string unit = "";

                        var volPresIn = dqp.InputUnit == IOUnit.Relative ? VolumePresentation.Relative : VolumePresentation.AbsoluteCm3;
                        var dosePresOut = dqp.OutputUnit == IOUnit.Relative ? DoseValuePresentation.Relative : DoseValuePresentation.Absolute;
                        var volPresOut = dqp.OutputUnit == IOUnit.Relative ? VolumePresentation.Relative : VolumePresentation.AbsoluteCm3;

                        // 相対線量の基準線量（プラン処方総線量、未設定時は dvh.MaxDose にフォールバック）
                        DoseValue refDose = (!prescriptionTotalDose.IsUndefined() && prescriptionTotalDose.Dose > 0)
                            ? prescriptionTotalDose
                            : dvh.MaxDose;

                        DoseValue targetDose = dqp.InputUnit == IOUnit.Relative
                            ? new DoseValue(refDose.Dose * (dqp.DQPvalue * 0.01), refDose.Unit)
                            : new DoseValue(dqp.DQPvalue, DoseValue.DoseUnit.Gy);

                        if (dqp.DQPtype == DQPtype.Dose)
                        {
                            var doseVal = planItem.GetDoseAtVolume(structure, dqp.DQPvalue, volPresIn, dosePresOut);
                            calculatedVal = dqp.OutputUnit == IOUnit.Relative ? doseVal.Dose : doseVal.ToGy();
                            unit = dqp.OutputUnit == IOUnit.Relative ? "%" : "Gy";
                        }
                        else if (dqp.DQPtype == DQPtype.Volume)
                        {
                            calculatedVal = planItem.GetVolumeAtDose(structure, targetDose, volPresOut);
                            unit = dqp.OutputUnit == IOUnit.Relative ? "%" : "cc";
                        }
                        else if (dqp.DQPtype == DQPtype.DoseComplement)
                        {
                            double subVolume = dqp.InputUnit == IOUnit.Relative
                                ? (100.0 - dqp.DQPvalue)
                                : (structure.Volume - dqp.DQPvalue);

                            if (subVolume > 0)
                            {
                                var doseVal = planItem.GetDoseAtVolume(structure, subVolume, volPresIn, dosePresOut);
                                calculatedVal = dqp.OutputUnit == IOUnit.Relative ? doseVal.Dose : doseVal.ToGy();
                            }
                            unit = dqp.OutputUnit == IOUnit.Relative ? "%" : "Gy";
                        }
                        else if (dqp.DQPtype == DQPtype.ComplementVolume)
                        {
                            double volCc = planItem.GetVolumeAtDose(structure, targetDose, VolumePresentation.AbsoluteCm3);
                            double cvCc = structure.Volume - volCc;
                            if (cvCc < 0) cvCc = 0;

                            calculatedVal = dqp.OutputUnit == IOUnit.Relative
                                ? (cvCc / structure.Volume * 100.0)
                                : cvCc;

                            unit = dqp.OutputUnit == IOUnit.Relative ? "%" : "cc";
                        }

                        record.DvhMetrics.Add(new DvhMetricResult
                        {
                            OriginalStructureId = structure.Id,
                            TargetAlias = resolved.TargetAlias,
                            StructureVolumeCc = structure.Volume,
                            MaxDoseGy = dvh.MaxDose.ToGy(),
                            MeanDoseGy = dvh.MeanDose.ToGy(),
                            MinDoseGy = dvh.MinDose.ToGy(),
                            MetricKey = metricKey,
                            Value = calculatedVal,
                            Unit = unit
                        });
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception)
                    {
                        // DVH計算エラー時はスキップ
                    }
                }
            }
        }

        private List<DqpColumnDefinition> BuildDqpColumns(IList<DQP> dqpDefinitions, IList<StructureMappingRule> mappingRules)
        {
            var list = new List<DqpColumnDefinition>();

            // 1. 各輪郭の基本統計量（Volume, Max, Mean, Min）列
            var distinctStructNames = GetTargetStructureNames(dqpDefinitions, mappingRules);

            foreach (var sName in distinctStructNames)
            {
                list.Add(new DqpColumnDefinition
                {
                    StructureIdentifier = sName,
                    MetricKey = "BASIC_STATS",
                    HeaderText = $"{sName}-Volume[cc]",
                    ColumnType = DqpColumnType.BasicVolume
                });
                list.Add(new DqpColumnDefinition
                {
                    StructureIdentifier = sName,
                    MetricKey = "BASIC_STATS",
                    HeaderText = $"{sName}-Max dose[Gy]",
                    ColumnType = DqpColumnType.BasicMaxDose
                });
                list.Add(new DqpColumnDefinition
                {
                    StructureIdentifier = sName,
                    MetricKey = "BASIC_STATS",
                    HeaderText = $"{sName}-Mean dose[Gy]",
                    ColumnType = DqpColumnType.BasicMeanDose
                });
                list.Add(new DqpColumnDefinition
                {
                    StructureIdentifier = sName,
                    MetricKey = "BASIC_STATS",
                    HeaderText = $"{sName}-Min dose[Gy]",
                    ColumnType = DqpColumnType.BasicMinDose
                });
            }

            // 2. DQP 指標列（旧仕様と完全一致する臨床的ヘッダー記法）
            if (dqpDefinitions != null)
            {
                foreach (var dqp in dqpDefinitions)
                {
                    if (string.IsNullOrWhiteSpace(dqp.structureName)) continue;

                string metricKey = $"{dqp.DQPtype}_{dqp.DQPvalue}_{dqp.InputUnit}_{dqp.OutputUnit}";

                // プレフィックス
                string typePrefix = dqp.DQPtype switch
                {
                    DQPtype.Dose => "-D",
                    DQPtype.Volume => "-V",
                    DQPtype.DoseComplement => "-DC",
                    DQPtype.ComplementVolume => "-CV",
                    _ => $"-{dqp.DQPtype}"
                };

                // 入力単位表記
                string inUnitStr = dqp.InputUnit == IOUnit.Absolute
                    ? (dqp.DQPtype == DQPtype.Dose || dqp.DQPtype == DQPtype.DoseComplement ? "cc" : "Gy")
                    : "%";

                // 出力単位表記
                string outUnitStr = dqp.OutputUnit == IOUnit.Absolute
                    ? (dqp.DQPtype == DQPtype.Dose || dqp.DQPtype == DQPtype.DoseComplement ? "[Gy]" : "[cc]")
                    : "[%]";

                string header = $"{dqp.structureName}{typePrefix}{dqp.DQPvalue}{inUnitStr}{outUnitStr}";

                list.Add(new DqpColumnDefinition
                {
                    StructureIdentifier = dqp.structureName,
                    MetricKey = metricKey,
                    HeaderText = header,
                    ColumnType = DqpColumnType.DqpMetric
                });
                }
            }

            return list;
        }

        /// <summary>
        /// マッピングルールおよびDQP定義から、抽出対象となる輪郭（またはエイリアス）名リストを集約
        /// </summary>
        private static List<string> GetTargetStructureNames(IList<DQP> dqpDefinitions, IList<StructureMappingRule> mappingRules)
        {
            var targetStructNames = new List<string>();

            // 1. マッピングルールが存在する場合は、選択されている輪郭（TargetAlias または Pattern）
            if (mappingRules != null)
            {
                foreach (var r in mappingRules.Where(r => r.IsSelected))
                {
                    string name = !string.IsNullOrWhiteSpace(r.TargetAlias) ? r.TargetAlias.Trim() : r.Pattern.Trim();
                    if (!string.IsNullOrEmpty(name) && !name.Contains("*") && !name.Contains("?") && !name.Contains("|"))
                    {
                        if (!targetStructNames.Contains(name, StringComparer.OrdinalIgnoreCase))
                        {
                            targetStructNames.Add(name);
                        }
                    }
                }
            }

            // 2. DQP リストに指定されている輪郭名を追加
            if (dqpDefinitions != null)
            {
                foreach (var d in dqpDefinitions.Where(d => !string.IsNullOrWhiteSpace(d.structureName)))
                {
                    string name = d.structureName.Trim();
                    if (!targetStructNames.Contains(name, StringComparer.OrdinalIgnoreCase))
                    {
                        targetStructNames.Add(name);
                    }
                }
            }

            return targetStructNames;
        }

        #endregion
    }
}
