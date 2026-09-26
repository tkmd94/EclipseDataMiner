using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EclipseDataMiner.Models;
using EclipseDataMiner.Services;
using EclipseDataMiner.ViewModels;

namespace EclipseDataMiner.Tests
{
    [TestClass]
    public class SearchFilterTests
    {
        [TestMethod]
        [Description("患者IDのカンマ区切りリストによるOR部分一致検索を検証")]
        public void IsPatientMatch_WhenMultipleIdsSpecified_ShouldMatchAny()
        {
            // Arrange
            var criteria = new SearchFilterCriteria
            {
                PatientIdFilter = SearchFilterCriteria.ParseCommaSeparated("PT100, 200, 300")
            };

            // Act & Assert
            Assert.IsTrue(SearchFilterService.IsPatientMatch("PT100_A", criteria));
            Assert.IsTrue(SearchFilterService.IsPatientMatch("SAMPLE_200", criteria));
            Assert.IsTrue(SearchFilterService.IsPatientMatch("300", criteria));
            Assert.IsFalse(SearchFilterService.IsPatientMatch("PT400", criteria));
        }

        [TestMethod]
        [Description("グローバル論理AND: すべての指定条件を満たす場合のみ true になることを検証")]
        public void IsPlanMatch_WhenGlobalLogicIsAnd_ShouldRequireAllConditions()
        {
            // Arrange
            var criteria = new SearchFilterCriteria
            {
                GlobalLogicIsAnd = true,
                PlanIdFilter = new List<string> { "VMAT" },
                TotalDoseGy = 60.0
            };

            // Act & Assert - 両方一致
            Assert.IsTrue(SearchFilterService.IsPlanMatch("VMAT_Prostate", "PTV", 2.0, 30, 60.0, "TreatmentApproved", false, criteria));

            // Plan ID は一致するが線量が不一致 -> false
            Assert.IsFalse(SearchFilterService.IsPlanMatch("VMAT_Prostate", "PTV", 2.0, 39, 78.0, "TreatmentApproved", false, criteria));

            // 線量は一致するが Plan ID が不一致 -> false
            Assert.IsFalse(SearchFilterService.IsPlanMatch("IMRT_Prostate", "PTV", 2.0, 30, 60.0, "TreatmentApproved", false, criteria));
        }

        [TestMethod]
        [Description("グローバル論理OR: 指定条件のいずれかを満たせば true になることを検証")]
        public void IsPlanMatch_WhenGlobalLogicIsOr_ShouldMatchIfAnyConditionMet()
        {
            // Arrange
            var criteria = new SearchFilterCriteria
            {
                GlobalLogicIsAnd = false,
                PlanIdFilter = new List<string> { "VMAT" },
                TotalDoseGy = 60.0
            };

            // Plan ID のみ一致 -> true
            Assert.IsTrue(SearchFilterService.IsPlanMatch("VMAT_Prostate", "PTV", 2.0, 39, 78.0, "TreatmentApproved", false, criteria));

            // 線量のみ一致 -> true
            Assert.IsTrue(SearchFilterService.IsPlanMatch("IMRT_Prostate", "PTV", 2.0, 30, 60.0, "TreatmentApproved", false, criteria));

            // いずれも不一致 -> false
            Assert.IsFalse(SearchFilterService.IsPlanMatch("CyberKnife", "PTV", 10.0, 4, 40.0, "TreatmentApproved", false, criteria));
        }

        [TestMethod]
        [Description("PlanSum は IncludePlanSums オプションが false の場合に除外されることを検証")]
        public void IsPlanMatch_PlanSumHandling_ShouldRespectOption()
        {
            // Arrange
            var criteriaExclude = new SearchFilterCriteria { IncludePlanSums = false };
            var criteriaInclude = new SearchFilterCriteria { IncludePlanSums = true };

            // Act & Assert
            Assert.IsFalse(SearchFilterService.IsPlanMatch("PlanSum1", null, null, null, null, "TreatmentApproved", isPlanSum: true, criteriaExclude));
            Assert.IsTrue(SearchFilterService.IsPlanMatch("PlanSum1", null, null, null, null, "TreatmentApproved", isPlanSum: true, criteriaInclude));
        }

        [TestMethod]
        [Description("承認ステータスフィルタによる除外・許可判定を検証")]
        public void IsApprovalStatusMatch_ShouldFilterProperly()
        {
            // Arrange
            var criteria = new SearchFilterCriteria
            {
                FilterUnapproved = false,
                FilterPlanApproved = true,
                FilterTreatmentApproved = true
            };

            // Act & Assert
            Assert.IsFalse(SearchFilterService.IsApprovalStatusMatch("Unapproved", criteria));
            Assert.IsTrue(SearchFilterService.IsApprovalStatusMatch("PlanningApproved", criteria));
            Assert.IsTrue(SearchFilterService.IsApprovalStatusMatch("TreatmentApproved", criteria));
        }

        [TestMethod]
        [Description("ViewModel での AND/OR 論理切り替えが相互排他的に正しく連動することを検証")]
        public void GlobalLogicToggle_InViewModel_ShouldMutuallySync()
        {
            // Arrange
            var vm = new EclipseDataMiner.ViewModels.MainViewModel();

            // 初期状態は AND が true, OR が false
            Assert.IsTrue(vm.GlobalLogicIsAnd);
            Assert.IsFalse(vm.GlobalLogicIsOr);

            // Act: OR を true に設定
            vm.GlobalLogicIsOr = true;

            // Assert: AND が false, OR が true
            Assert.IsFalse(vm.GlobalLogicIsAnd);
            Assert.IsTrue(vm.GlobalLogicIsOr);

            // Act: 再び AND を true に設定
            vm.GlobalLogicIsAnd = true;

            // Assert: AND が true, OR が false
            Assert.IsTrue(vm.GlobalLogicIsAnd);
            Assert.IsFalse(vm.GlobalLogicIsOr);
        }

        [TestMethod]
        [Description("カンマ区切りパーサーが連続カンマや全角・半角スペースを安全にトリム・除外することを検証")]
        public void ParseCommaSeparated_WithSpacesAndConsecutiveCommas_ShouldSanitize()
        {
            // Arrange
            string rawInput = "  PT100 ,,,　PT200　,  , PT300 , ";

            // Act
            var parsed = SearchFilterCriteria.ParseCommaSeparated(rawInput);

            // Assert
            Assert.AreEqual(3, parsed.Count);
            Assert.AreEqual("PT100", parsed[0]);
            Assert.AreEqual("PT200", parsed[1]);
            Assert.AreEqual("PT300", parsed[2]);
        }

        [TestMethod]
        [Description("カンマ区切りパーサーに null や空白のみを渡した際に空リストを返すことを検証")]
        public void ParseCommaSeparated_WhenNullOrEmpty_ShouldReturnEmptyList()
        {
            // Act & Assert
            Assert.AreEqual(0, SearchFilterCriteria.ParseCommaSeparated(null).Count);
            Assert.AreEqual(0, SearchFilterCriteria.ParseCommaSeparated("").Count);
            Assert.AreEqual(0, SearchFilterCriteria.ParseCommaSeparated("   　  ").Count);
        }

        [TestMethod]
        [Description("PlanSum (合算計画) の包含・除外オプションが正確に判定されることを検証")]
        public void IsPlanMatch_WhenPlanSumIncludedAndExcluded_ShouldFilterProperly()
        {
            // Arrange: PlanSum を除外する設定
            var criteriaExclude = new SearchFilterCriteria { IncludePlanSums = false };
            // PlanSum を包含する設定
            var criteriaInclude = new SearchFilterCriteria { IncludePlanSums = true };

            // Act & Assert
            // 通常プラン (isPlanSum: false) -> どちらでも合致
            Assert.IsTrue(SearchFilterService.IsPlanMatch("Plan1", "PTV", 2.0, 30, 60.0, "TreatmentApproved", false, criteriaExclude));
            Assert.IsTrue(SearchFilterService.IsPlanMatch("Plan1", "PTV", 2.0, 30, 60.0, "TreatmentApproved", false, criteriaInclude));

            // PlanSum (isPlanSum: true) -> 除外設定では false、包含設定では true
            Assert.IsFalse(SearchFilterService.IsPlanMatch("PlanSum_Total", "PTV", null, null, 70.0, "TreatmentApproved", true, criteriaExclude));
            Assert.IsTrue(SearchFilterService.IsPlanMatch("PlanSum_Total", "PTV", null, null, 70.0, "TreatmentApproved", true, criteriaInclude));
        }

        [TestMethod]
        [Description("グローバル論理AND: Patient ID, Course ID を含む全指定条件が一致しなければならないことを検証")]
        public void IsPlanMatch_WhenPatientAndCourseIncluded_InAndLogic_ShouldRequireAll()
        {
            // Arrange: Patient ID, Course ID, Plan ID すべて指定
            var criteria = new SearchFilterCriteria
            {
                GlobalLogicIsAnd = true,
                PatientIdFilter = new List<string> { "PT100" },
                CourseIdFilter = new List<string> { "C1" },
                PlanIdFilter = new List<string> { "VMAT" }
            };

            // 全て一致 -> true
            Assert.IsTrue(SearchFilterService.IsPlanMatch("PT100_ABC", "C1_Rad", "VMAT_1", "PTV", 2.0, 30, 60.0, "TreatmentApproved", false, criteria));

            // Patient ID 不一致 -> false
            Assert.IsFalse(SearchFilterService.IsPlanMatch("PT200_ABC", "C1_Rad", "VMAT_1", "PTV", 2.0, 30, 60.0, "TreatmentApproved", false, criteria));

            // Course ID 不一致 -> false
            Assert.IsFalse(SearchFilterService.IsPlanMatch("PT100_ABC", "C2_Boost", "VMAT_1", "PTV", 2.0, 30, 60.0, "TreatmentApproved", false, criteria));

            // Plan ID 不一致 -> false
            Assert.IsFalse(SearchFilterService.IsPlanMatch("PT100_ABC", "C1_Rad", "IMRT_1", "PTV", 2.0, 30, 60.0, "TreatmentApproved", false, criteria));
        }

        [TestMethod]
        [Description("グローバル論理OR: Patient ID, Course ID, Plan ID, 線量のいずれか1つでも一致すれば true になることを検証")]
        public void IsPlanMatch_WhenPatientAndCourseIncluded_InOrLogic_ShouldMatchIfAny()
        {
            // Arrange
            var criteria = new SearchFilterCriteria
            {
                GlobalLogicIsAnd = false,
                PatientIdFilter = new List<string> { "PT100" },
                CourseIdFilter = new List<string> { "C1" },
                PlanIdFilter = new List<string> { "VMAT" },
                TotalDoseGy = 60.0
            };

            // Patient ID のみ一致 -> true
            Assert.IsTrue(SearchFilterService.IsPlanMatch("PT100", "C99", "OTHER", "PTV", 2.0, 20, 40.0, "TreatmentApproved", false, criteria));

            // Course ID のみ一致 -> true
            Assert.IsTrue(SearchFilterService.IsPlanMatch("PT999", "C1", "OTHER", "PTV", 2.0, 20, 40.0, "TreatmentApproved", false, criteria));

            // Plan ID のみ一致 -> true
            Assert.IsTrue(SearchFilterService.IsPlanMatch("PT999", "C99", "VMAT_Pros", "PTV", 2.0, 20, 40.0, "TreatmentApproved", false, criteria));

            // 線量のみ一致 -> true
            Assert.IsTrue(SearchFilterService.IsPlanMatch("PT999", "C99", "OTHER", "PTV", 2.0, 30, 60.0, "TreatmentApproved", false, criteria));

            // 全て不一致 -> false
            Assert.IsFalse(SearchFilterService.IsPlanMatch("PT999", "C99", "OTHER", "PTV", 2.0, 20, 40.0, "TreatmentApproved", false, criteria));
        }

        [TestMethod]
        [Description("ShouldSkipPatient: ANDロジック時、患者IDが不一致なら即時スキップされることを検証")]
        public void ShouldSkipPatient_WhenAndLogic_ShouldSkipIfPatientNotMatched()
        {
            var criteria = new SearchFilterCriteria
            {
                GlobalLogicIsAnd = true,
                PatientIdFilter = new List<string> { "PT100" },
                PlanIdFilter = new List<string> { "VMAT" }
            };

            // 一致 -> スキップしない (false)
            Assert.IsFalse(SearchFilterService.ShouldSkipPatient("PT100_A", criteria));

            // 不一致 -> スキップする (true)
            Assert.IsTrue(SearchFilterService.ShouldSkipPatient("PT200", criteria));
        }

        [TestMethod]
        [Description("ShouldSkipPatient: ORロジック時、プラン条件等があれば患者ID不一致でもスキップしないことを検証")]
        public void ShouldSkipPatient_WhenOrLogic_ShouldNotSkipIfOtherCriteriaSpecified()
        {
            var criteriaWithOther = new SearchFilterCriteria
            {
                GlobalLogicIsAnd = false,
                PatientIdFilter = new List<string> { "PT100" },
                PlanIdFilter = new List<string> { "VMAT" } // 他条件あり
            };

            // 患者ID不一致でも、Plan IDが指定されているためスキップしてはならない
            Assert.IsFalse(SearchFilterService.ShouldSkipPatient("PT200", criteriaWithOther));

            // 他条件が一切指定されていない場合
            var criteriaPatientOnly = new SearchFilterCriteria
            {
                GlobalLogicIsAnd = false,
                PatientIdFilter = new List<string> { "PT100" }
            };

            // 患者ID一致 -> スキップしない
            Assert.IsFalse(SearchFilterService.ShouldSkipPatient("PT100", criteriaPatientOnly));

            // 患者ID不一致かつ他条件なし -> スキップ可能
            Assert.IsTrue(SearchFilterService.ShouldSkipPatient("PT200", criteriaPatientOnly));
        }

        [TestMethod]
        [Description("ShouldSkipCourse: コーススキップ判定がAND/ORで適切に動作することを検証")]
        public void ShouldSkipCourse_WhenAndOrLogic_ShouldBehaveCorrectly()
        {
            var criteriaAnd = new SearchFilterCriteria
            {
                GlobalLogicIsAnd = true,
                CourseIdFilter = new List<string> { "C1" },
                PlanIdFilter = new List<string> { "VMAT" }
            };

            // AND: コース不一致なら即スキップ
            Assert.IsTrue(SearchFilterService.ShouldSkipCourse("C2", criteriaAnd));
            Assert.IsFalse(SearchFilterService.ShouldSkipCourse("C1", criteriaAnd));

            var criteriaOrWithPlan = new SearchFilterCriteria
            {
                GlobalLogicIsAnd = false,
                CourseIdFilter = new List<string> { "C1" },
                PlanIdFilter = new List<string> { "VMAT" }
            };

            // OR: 他条件 (PlanId) があるため、コース不一致でもスキップしない
            Assert.IsFalse(SearchFilterService.ShouldSkipCourse("C2", criteriaOrWithPlan));

            var criteriaOrCourseOnly = new SearchFilterCriteria
            {
                GlobalLogicIsAnd = false,
                CourseIdFilter = new List<string> { "C1" }
            };

            // OR: コース条件のみの場合、コース不一致ならスキップ可能
            Assert.IsTrue(SearchFilterService.ShouldSkipCourse("C2", criteriaOrCourseOnly));
        }

        [TestMethod]
        [Description("IsTextMatch: Exact (完全一致) モード時、「a」で「Plan」がヒットせず、完全一致のみヒットすることを検証")]
        public void IsTextMatch_WhenModeIsExact_ShouldMatchOnlyExactValues()
        {
            var filter = new List<string> { "a" };

            // 部分一致なら "Plan" に "a" が含まれるが、Exact では不一致
            Assert.IsFalse(SearchFilterService.IsTextMatch("Plan", filter, TextMatchMode.Exact));

            // 完全一致（大文字小文字無視）なら一致
            Assert.IsTrue(SearchFilterService.IsTextMatch("a", filter, TextMatchMode.Exact));
            Assert.IsTrue(SearchFilterService.IsTextMatch("A", filter, TextMatchMode.Exact));
        }

        [TestMethod]
        [Description("IsTextMatch: Regex (正規表現) モード時、パターンに適合する場合のみヒットすることを検証")]
        public void IsTextMatch_WhenModeIsRegex_ShouldMatchRegexPattern()
        {
            var filter = new List<string> { "^Plan_\\d+$" };

            Assert.IsTrue(SearchFilterService.IsTextMatch("Plan_1", filter, TextMatchMode.Regex));
            Assert.IsTrue(SearchFilterService.IsTextMatch("plan_123", filter, TextMatchMode.Regex));
            Assert.IsFalse(SearchFilterService.IsTextMatch("Boost_Plan_1", filter, TextMatchMode.Regex));
            Assert.IsFalse(SearchFilterService.IsTextMatch("Plan_Boost", filter, TextMatchMode.Regex));
        }

        [TestMethod]
        [Description("IsPlanMatch: PlanIdMatchMode が Exact の場合、「a」で「Plan」がスキップされ、「Plan」で完全一致することを検証")]
        public void IsPlanMatch_WhenPlanIdMatchModeIsExact_ShouldFilterCorrectly()
        {
            // Plan ID フィルタに "a" を指定し、モードを Exact に設定
            var criteriaExactA = new SearchFilterCriteria
            {
                PlanIdFilter = new List<string> { "a" },
                PlanIdMatchMode = TextMatchMode.Exact
            };

            // Plan ID が "Plan" の計画 -> false (「a」を含んでいるが完全一致ではないためヒットしない)
            Assert.IsFalse(SearchFilterService.IsPlanMatch("PT1", "C1", "Plan", "PTV", 2.0, 30, 60.0, "TreatmentApproved", false, criteriaExactA));

            // Plan ID フィルタに "Plan" を指定し、モードを Exact に設定
            var criteriaExactPlan = new SearchFilterCriteria
            {
                PlanIdFilter = new List<string> { "Plan" },
                PlanIdMatchMode = TextMatchMode.Exact
            };

            // Plan ID が "Plan" の計画 -> true
            Assert.IsTrue(SearchFilterService.IsPlanMatch("PT1", "C1", "Plan", "PTV", 2.0, 30, 60.0, "TreatmentApproved", false, criteriaExactPlan));

            // Plan ID が "Plan1" の計画 -> false
            Assert.IsFalse(SearchFilterService.IsPlanMatch("PT1", "C1", "Plan1", "PTV", 2.0, 30, 60.0, "TreatmentApproved", false, criteriaExactPlan));
        }

        [TestMethod]
        [Description("IsPlanMatch: PlanIdMatchMode が Regex の場合、正規表現で正しくフィルタされることを検証")]
        public void IsPlanMatch_WhenPlanIdMatchModeIsRegex_ShouldFilterCorrectly()
        {
            var criteriaRegex = new SearchFilterCriteria
            {
                PlanIdFilter = new List<string> { "^(VMAT|IMRT)_Prostate$" },
                PlanIdMatchMode = TextMatchMode.Regex
            };

            Assert.IsTrue(SearchFilterService.IsPlanMatch("PT1", "C1", "VMAT_Prostate", "PTV", 2.0, 30, 60.0, "TreatmentApproved", false, criteriaRegex));
            Assert.IsTrue(SearchFilterService.IsPlanMatch("PT1", "C1", "IMRT_Prostate", "PTV", 2.0, 30, 60.0, "TreatmentApproved", false, criteriaRegex));
            Assert.IsFalse(SearchFilterService.IsPlanMatch("PT1", "C1", "VMAT_Prostate_Boost", "PTV", 2.0, 30, 60.0, "TreatmentApproved", false, criteriaRegex));
            Assert.IsFalse(SearchFilterService.IsPlanMatch("PT1", "C1", "3DCRT_Prostate", "PTV", 2.0, 30, 60.0, "TreatmentApproved", false, criteriaRegex));
        }

        [TestMethod]
        [Description("ParseTextFilter: 通常トークンと除外トークン(! / -)の分離パースを検証")]
        public void ParseTextFilter_WithIncludesAndExcludes_ShouldSeparateProperly()
        {
            SearchFilterCriteria.ParseTextFilter("VMAT, IMRT, !QA, -Test,  !Verify  ", out var inc, out var exc);

            Assert.AreEqual(2, inc.Count);
            Assert.AreEqual("VMAT", inc[0]);
            Assert.AreEqual("IMRT", inc[1]);

            Assert.AreEqual(3, exc.Count);
            Assert.AreEqual("QA", exc[0]);
            Assert.AreEqual("Test", exc[1]);
            Assert.AreEqual("Verify", exc[2]);
        }

        [TestMethod]
        [Description("NumericFilterCriteria.Parse: 範囲指定 (70-80, 70~80) の解析および IsMatch 判定を検証")]
        public void NumericFilterCriteria_RangeSyntax_ShouldParseAndMatchCorrectly()
        {
            var rangeHyphen = NumericFilterCriteria.Parse("70-80");
            Assert.IsFalse(rangeHyphen.IsEmpty);
            Assert.AreEqual(70.0, rangeHyphen.MinValue);
            Assert.AreEqual(80.0, rangeHyphen.MaxValue);
            Assert.IsTrue(rangeHyphen.IsMatch(70.0));
            Assert.IsTrue(rangeHyphen.IsMatch(75.5));
            Assert.IsTrue(rangeHyphen.IsMatch(80.0));
            Assert.IsFalse(rangeHyphen.IsMatch(69.0));
            Assert.IsFalse(rangeHyphen.IsMatch(81.0));

            var rangeTilde = NumericFilterCriteria.Parse(" 4 ~ 5 ");
            Assert.IsTrue(rangeTilde.IsMatchInt(4));
            Assert.IsTrue(rangeTilde.IsMatchInt(5));
            Assert.IsFalse(rangeTilde.IsMatchInt(3));
            Assert.IsFalse(rangeTilde.IsMatchInt(6));
        }

        [TestMethod]
        [Description("NumericFilterCriteria.Parse: 不等号指定 (>=10, <30) の解析および IsMatch 判定を検証")]
        public void NumericFilterCriteria_InequalitySyntax_ShouldParseAndMatchCorrectly()
        {
            var gte = NumericFilterCriteria.Parse(">= 10");
            Assert.IsTrue(gte.MinInclusive);
            Assert.AreEqual(10.0, gte.MinValue);
            Assert.IsTrue(gte.IsMatch(10.0));
            Assert.IsTrue(gte.IsMatch(12.5));
            Assert.IsFalse(gte.IsMatch(9.0));

            var lt = NumericFilterCriteria.Parse("< 30");
            Assert.IsFalse(lt.MaxInclusive);
            Assert.AreEqual(30.0, lt.MaxValue);
            Assert.IsTrue(lt.IsMatch(29.0));
            Assert.IsFalse(lt.IsMatch(30.0));
            Assert.IsFalse(lt.IsMatch(35.0));
        }

        [TestMethod]
        [Description("IsPlanMatch: NOT除外フィルタ (!QA, !Test) の判定を検証")]
        public void IsPlanMatch_WithExcludeFilters_ShouldExcludeMatchedPlans()
        {
            // Plan ID: VMAT, IMRT, !QA
            SearchFilterCriteria.ParseTextFilter("VMAT, IMRT, !QA", out var inc, out var exc);
            var criteria = new SearchFilterCriteria
            {
                PlanIdFilter = inc,
                PlanIdExcludeFilter = exc,
                PlanIdMatchMode = TextMatchMode.Contains
            };

            // VMAT_Prostate -> 一致
            Assert.IsTrue(SearchFilterService.IsPlanMatch("PT1", "C1", "VMAT_Prostate", "PTV", null, null, null, "TreatmentApproved", false, criteria));

            // VMAT_QA -> "QA" を含むため除外されるべき
            Assert.IsFalse(SearchFilterService.IsPlanMatch("PT1", "C1", "VMAT_QA", "PTV", null, null, null, "TreatmentApproved", false, criteria));

            // QA_IMRT -> "QA" を含むため除外
            Assert.IsFalse(SearchFilterService.IsPlanMatch("PT1", "C1", "QA_IMRT", "PTV", null, null, null, "TreatmentApproved", false, criteria));

            // 3DCRT -> 包含リストに不一致
            Assert.IsFalse(SearchFilterService.IsPlanMatch("PT1", "C1", "3DCRT", "PTV", null, null, null, "TreatmentApproved", false, criteria));
        }

        [TestMethod]
        [Description("IsPlanMatch: 除外フィルタ単独指定 (!QA) の場合、QA以外がすべて一致することを検証")]
        public void IsPlanMatch_WithOnlyExcludeFilters_ShouldMatchNonExcludedPlans()
        {
            SearchFilterCriteria.ParseTextFilter("!QA, !Test", out var inc, out var exc);
            var criteria = new SearchFilterCriteria
            {
                PlanIdFilter = inc,
                PlanIdExcludeFilter = exc,
                PlanIdMatchMode = TextMatchMode.Contains
            };

            Assert.IsTrue(SearchFilterService.IsPlanMatch("PT1", "C1", "Prostate_VMAT", "PTV", null, null, null, "TreatmentApproved", false, criteria));
            Assert.IsFalse(SearchFilterService.IsPlanMatch("PT1", "C1", "Plan_QA_Verify", "PTV", null, null, null, "TreatmentApproved", false, criteria));
            Assert.IsFalse(SearchFilterService.IsPlanMatch("PT1", "C1", "Test_1", "PTV", null, null, null, "TreatmentApproved", false, criteria));
        }

        [TestMethod]
        [Description("IsPlanMatch: 臨床プロトコル条件（肺SBRT: >=10Gy/Fr, 4-5Fr, 48-60Gy）の複合判定を検証")]
        public void IsPlanMatch_LungSbrtCriteria_ShouldMatchCorrectProtocol()
        {
            var criteria = new SearchFilterCriteria
            {
                DosePerFractionCriteria = NumericFilterCriteria.Parse(">= 10"),
                NumberOfFractionsCriteria = NumericFilterCriteria.Parse("4 - 5"),
                TotalDoseCriteria = NumericFilterCriteria.Parse("48 - 60"),
                GlobalLogicIsAnd = true
            };

            // 肺SBRTプラン: 12Gy × 4Fr = 48Gy -> 一致
            Assert.IsTrue(SearchFilterService.IsPlanMatch("PT1", "C1", "Lung_SBRT", "PTV", 12.0, 4, 48.0, "TreatmentApproved", false, criteria));

            // 肺SBRTプラン: 10Gy × 5Fr = 50Gy -> 一致
            Assert.IsTrue(SearchFilterService.IsPlanMatch("PT1", "C1", "Lung_SBRT", "PTV", 10.0, 5, 50.0, "TreatmentApproved", false, criteria));

            // 前立腺通常分割: 2Gy × 39Fr = 78Gy -> 不一致
            Assert.IsFalse(SearchFilterService.IsPlanMatch("PT2", "C1", "Prostate", "PTV", 2.0, 39, 78.0, "TreatmentApproved", false, criteria));

            // 線量は合致するが分割数が30回 -> 不一致
            Assert.IsFalse(SearchFilterService.IsPlanMatch("PT3", "C1", "Other", "PTV", 10.0, 30, 50.0, "TreatmentApproved", false, criteria));
        }

        [TestMethod]
        [Description("SearchPresetService: プリセットフォルダ配下の個別JSONファイル保存・読込・削除を検証")]
        public void SearchPresetService_DirectoryBased_SaveAndLoadAndDelete_ShouldPreservePresets()
        {
            string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"presets_test_{System.Guid.NewGuid()}");
            try
            {
                var service = new SearchPresetService(tempDir);
                Assert.IsTrue(System.IO.Directory.Exists(tempDir));
                Assert.AreEqual(0, service.LoadPresets().Count);

                var custom = new SearchPreset
                {
                    Name = "Custom Palliative 30Gy",
                    DosePerFractionText = "3.0",
                    NumberOfFractionsText = "10",
                    TotalDoseText = "30",
                    GlobalLogicIsAnd = true
                };

                // 単一プリセット保存
                service.SavePreset(custom);

                // 物理ファイルが存在することを確認
                string expectedFile = System.IO.Path.Combine(tempDir, "Custom Palliative 30Gy.json");
                Assert.IsTrue(System.IO.File.Exists(expectedFile));

                // 再ロード
                var reloadedService = new SearchPresetService(tempDir);
                var reloaded = reloadedService.LoadPresets();
                Assert.AreEqual(1, reloaded.Count);

                var reloadedCustom = reloaded.FirstOrDefault(p => p.Name == "Custom Palliative 30Gy");
                Assert.IsNotNull(reloadedCustom);
                Assert.AreEqual("3.0", reloadedCustom.DosePerFractionText);
                Assert.AreEqual("10", reloadedCustom.NumberOfFractionsText);
                Assert.AreEqual("30", reloadedCustom.TotalDoseText);
                Assert.AreEqual(expectedFile, reloadedCustom.FilePath);

                // 削除
                service.DeletePreset(reloadedCustom);
                Assert.IsFalse(System.IO.File.Exists(expectedFile));
                Assert.AreEqual(0, service.LoadPresets().Count);
            }
            finally
            {
                if (System.IO.Directory.Exists(tempDir))
                {
                    try { System.IO.Directory.Delete(tempDir, true); } catch { }
                }
            }
        }

        [TestMethod]
        [Description("MainViewModel: ApplyPreset および BuildCriteria による検索条件構築の連動を検証")]
        public void MainViewModel_ApplyPreset_ShouldUpdateCriteriaProperly()
        {
            string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"presets_vm_{System.Guid.NewGuid()}");
            try
            {
                var service = new SearchPresetService(tempDir);
                var lungPreset = new SearchPreset
                {
                    Name = "Lung SBRT Test",
                    DosePerFractionText = ">= 10",
                    NumberOfFractionsText = "4 - 5",
                    TotalDoseText = "48 - 60"
                };
                service.SavePreset(lungPreset);

                var vm = new MainViewModel(service);
                var loaded = vm.Presets.FirstOrDefault(p => p.Name == "Lung SBRT Test");
                Assert.IsNotNull(loaded);

                // プリセットを選択・適用
                vm.SelectedPreset = loaded;

                // ViewModel のプロパティが更新されたことを確認
                Assert.AreEqual(">= 10", vm.DosePerFractionText);
                Assert.AreEqual("4 - 5", vm.NumberOfFractionsText);
                Assert.AreEqual("48 - 60", vm.TotalDoseText);

                // BuildCriteria の結果を検証
                var criteria = vm.BuildCriteria();
                Assert.IsNotNull(criteria.DosePerFractionCriteria);
                Assert.IsTrue(criteria.DosePerFractionCriteria.IsMatch(12.0));
                Assert.IsFalse(criteria.DosePerFractionCriteria.IsMatch(8.0));

                Assert.IsNotNull(criteria.NumberOfFractionsCriteria);
                Assert.IsTrue(criteria.NumberOfFractionsCriteria.IsMatchInt(4));
                Assert.IsFalse(criteria.NumberOfFractionsCriteria.IsMatchInt(10));
            }
            finally
            {
                if (System.IO.Directory.Exists(tempDir))
                {
                    try { System.IO.Directory.Delete(tempDir, true); } catch { }
                }
            }
        }

        [TestMethod]
        [Description("MainViewModel: プリセットの保存および削除（組み込み制限なし・自由削除）の検証")]
        public void MainViewModel_PresetManagement_SaveAndDelete_ShouldFunction()
        {
            string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"presets_manage_{System.Guid.NewGuid()}");
            try
            {
                var service = new SearchPresetService(tempDir);
                var vm = new MainViewModel(service);

                // 初期状態は空
                Assert.AreEqual(0, vm.Presets.Count);
                Assert.IsFalse(vm.DeletePresetCommand.CanExecute(null));

                // 新規プリセットを保存
                vm.PresetNameInput = "My Protocol";
                vm.PlanIdText = "VMAT_New";
                vm.DosePerFractionText = "2.5";
                vm.NumberOfFractionsText = "20";
                vm.TotalDoseText = "50";

                vm.SavePresetCommand.Execute(null);

                var saved = vm.Presets.FirstOrDefault(p => p.Name == "My Protocol");
                Assert.IsNotNull(saved);
                Assert.AreEqual("VMAT_New", saved.PlanIdText);
                Assert.AreEqual(saved, vm.SelectedPreset);

                // 選択されているため削除可能
                Assert.IsTrue(vm.DeletePresetCommand.CanExecute(null));

                // 物理ファイルも存在することを確認
                string expectedFile = System.IO.Path.Combine(tempDir, "My Protocol.json");
                Assert.IsTrue(System.IO.File.Exists(expectedFile));
            }
            finally
            {
                if (System.IO.Directory.Exists(tempDir))
                {
                    try { System.IO.Directory.Delete(tempDir, true); } catch { }
                }
            }
        }

        [TestMethod]
        [Description("IsDateMatch: 日付範囲（From〜To、片側指定、Null対応）の判定を検証")]
        public void IsDateMatch_VariousRanges_ShouldFilterCorrectly()
        {
            var date = new System.DateTime(2025, 6, 15, 14, 30, 0);

            // 1. 指定なし -> 常に true
            Assert.IsTrue(SearchFilterService.IsDateMatch(date, null, null));
            Assert.IsTrue(SearchFilterService.IsDateMatch(null, null, null));

            // 2. 日付指定ありで対象日付が null -> false
            Assert.IsFalse(SearchFilterService.IsDateMatch(null, new System.DateTime(2025, 1, 1), null));
            Assert.IsFalse(SearchFilterService.IsDateMatch(null, null, new System.DateTime(2025, 12, 31)));

            // 3. From のみ指定
            Assert.IsTrue(SearchFilterService.IsDateMatch(date, new System.DateTime(2025, 6, 15), null)); // 当日含む
            Assert.IsTrue(SearchFilterService.IsDateMatch(date, new System.DateTime(2025, 6, 1), null));
            Assert.IsFalse(SearchFilterService.IsDateMatch(date, new System.DateTime(2025, 6, 16), null));

            // 4. To のみ指定
            Assert.IsTrue(SearchFilterService.IsDateMatch(date, null, new System.DateTime(2025, 6, 15))); // 当日23:59:59まで含む
            Assert.IsTrue(SearchFilterService.IsDateMatch(date, null, new System.DateTime(2025, 7, 1)));
            Assert.IsFalse(SearchFilterService.IsDateMatch(date, null, new System.DateTime(2025, 6, 14)));

            // 5. From 〜 To 範囲指定
            Assert.IsTrue(SearchFilterService.IsDateMatch(date, new System.DateTime(2025, 6, 1), new System.DateTime(2025, 6, 30)));
            Assert.IsFalse(SearchFilterService.IsDateMatch(date, new System.DateTime(2025, 1, 1), new System.DateTime(2025, 5, 31)));
            Assert.IsFalse(SearchFilterService.IsDateMatch(date, new System.DateTime(2025, 7, 1), new System.DateTime(2025, 12, 31)));
        }

        [TestMethod]
        [Description("IsBeamMatch: Machine, Energy, Technique の包含・!除外の複合判定を検証")]
        public void IsBeamMatch_MachineEnergyTechnique_ShouldFilterCorrectly()
        {
            var normalBeams = new List<BeamRecord>
            {
                new BeamRecord { BeamId = "B1", TreatmentUnit = "TrueBeam1", EnergyModeDisplayName = "6X", Technique = "ARC", IsSetupField = false },
                new BeamRecord { BeamId = "B2", TreatmentUnit = "TrueBeam1", EnergyModeDisplayName = "10X", Technique = "ARC", IsSetupField = false }
            };

            // 1. Machine ID 包含一致
            Assert.IsTrue(SearchFilterService.IsBeamMatch(normalBeams,
                machineIncludes: new List<string> { "TrueBeam" }, machineExcludes: null,
                energyIncludes: null, energyExcludes: null,
                techniqueIncludes: null, techniqueExcludes: null));

            // 2. Machine ID 不一致
            Assert.IsFalse(SearchFilterService.IsBeamMatch(normalBeams,
                machineIncludes: new List<string> { "Clinac" }, machineExcludes: null,
                energyIncludes: null, energyExcludes: null,
                techniqueIncludes: null, techniqueExcludes: null));

            // 3. Machine ID 除外 (!TrueBeam1)
            Assert.IsFalse(SearchFilterService.IsBeamMatch(normalBeams,
                machineIncludes: null, machineExcludes: new List<string> { "TrueBeam1" },
                energyIncludes: null, energyExcludes: null,
                techniqueIncludes: null, techniqueExcludes: null));

            // 4. Energy 包含一致 (6X)
            Assert.IsTrue(SearchFilterService.IsBeamMatch(normalBeams,
                machineIncludes: null, machineExcludes: null,
                energyIncludes: new List<string> { "6X" }, energyExcludes: null,
                techniqueIncludes: null, techniqueExcludes: null));

            // 5. Energy 除外 (!10X) -> B2が10Xなので除外されるべき
            Assert.IsFalse(SearchFilterService.IsBeamMatch(normalBeams,
                machineIncludes: null, machineExcludes: null,
                energyIncludes: null, energyExcludes: new List<string> { "10X" },
                techniqueIncludes: null, techniqueExcludes: null));

            // 6. Technique 包含 (ARC) & 除外 (!STATIC) -> STATICはないのでARCで一致
            Assert.IsTrue(SearchFilterService.IsBeamMatch(normalBeams,
                machineIncludes: null, machineExcludes: null,
                energyIncludes: null, energyExcludes: null,
                techniqueIncludes: new List<string> { "ARC" }, techniqueExcludes: new List<string> { "STATIC" }));
        }

        [TestMethod]
        [Description("IsPlanMatch: 日付範囲とビーム照射パラメータを含めた総合判定を検証")]
        public void IsPlanMatch_WithDateAndBeamFilters_ShouldEvaluateAccurately()
        {
            var criteria = new SearchFilterCriteria
            {
                GlobalLogicIsAnd = true,
                DateTarget = DateFilterTarget.TreatmentApprovalDate,
                DateFrom = new System.DateTime(2025, 1, 1),
                DateTo = new System.DateTime(2025, 12, 31),
                MachineFilter = new List<string> { "TrueBeam" },
                EnergyFilter = new List<string> { "6X" },
                TechniqueFilter = new List<string> { "ARC" },
                PlanIdFilter = new List<string> { "VMAT" }
            };

            var beams = new List<BeamRecord>
            {
                new BeamRecord { BeamId = "Field1", TreatmentUnit = "TrueBeam_SN100", EnergyModeDisplayName = "6X", Technique = "VMAT_ARC", IsSetupField = false }
            };

            var approvedDate = new System.DateTime(2025, 6, 20);

            // 全条件合致 -> true
            Assert.IsTrue(SearchFilterService.IsPlanMatch("PT1", "C1", "VMAT_Prostate", "PTV", 2.0, 39, 78.0, "TreatmentApproved", false, approvedDate, beams, criteria));

            // 日付が範囲外 (2024年) -> false
            var oldDate = new System.DateTime(2024, 12, 10);
            Assert.IsFalse(SearchFilterService.IsPlanMatch("PT1", "C1", "VMAT_Prostate", "PTV", 2.0, 39, 78.0, "TreatmentApproved", false, oldDate, beams, criteria));

            // マシンが不一致 -> false
            var otherBeams = new List<BeamRecord>
            {
                new BeamRecord { BeamId = "Field1", TreatmentUnit = "Clinac_iX", EnergyModeDisplayName = "6X", Technique = "VMAT_ARC", IsSetupField = false }
            };
            Assert.IsFalse(SearchFilterService.IsPlanMatch("PT1", "C1", "VMAT_Prostate", "PTV", 2.0, 39, 78.0, "TreatmentApproved", false, approvedDate, otherBeams, criteria));
        }

        [TestMethod]
        [Description("MainViewModel: 高度フィルタのプロパティ、ClearDatesCommand、およびプリセット保存復元を検証")]
        public void MainViewModel_AdvancedFiltersAndPresetSync_ShouldWorkCorrectly()
        {
            string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"presets_adv_{System.Guid.NewGuid()}");
            try
            {
                var service = new SearchPresetService(tempDir);
                var vm = new MainViewModel(service);

                // 高度フィルタプロパティを設定
                vm.MachineFilterText = "TrueBeam, Clinac, !QA_Linac";
                vm.EnergyFilterText = "6X, 10X, !6FFF";
                vm.TechniqueFilterText = "ARC, !STATIC";
                vm.DateTarget = DateFilterTarget.PlanningApprovalDate;
                vm.DateFrom = new System.DateTime(2025, 4, 1);
                vm.DateTo = new System.DateTime(2025, 9, 30);

                // BuildCriteria の検証
                var criteria = vm.BuildCriteria();
                Assert.AreEqual(2, criteria.MachineFilter.Count);
                Assert.AreEqual(1, criteria.MachineExcludeFilter.Count);
                Assert.AreEqual("QA_Linac", criteria.MachineExcludeFilter[0]);
                Assert.AreEqual(2, criteria.EnergyFilter.Count);
                Assert.AreEqual(1, criteria.EnergyExcludeFilter.Count);
                Assert.AreEqual(DateFilterTarget.PlanningApprovalDate, criteria.DateTarget);
                Assert.AreEqual(new System.DateTime(2025, 4, 1), criteria.DateFrom);
                Assert.AreEqual(new System.DateTime(2025, 9, 30), criteria.DateTo);

                // ClearDatesCommand の検証
                Assert.IsTrue(vm.ClearDatesCommand.CanExecute(null));
                vm.ClearDatesCommand.Execute(null);
                Assert.IsNull(vm.DateFrom);
                Assert.IsNull(vm.DateTo);

                // プリセットとして保存
                vm.DateFrom = new System.DateTime(2025, 4, 1);
                vm.DateTo = new System.DateTime(2025, 9, 30);
                vm.PresetNameInput = "Stereotactic Advanced";
                vm.SavePresetCommand.Execute(null);

                // 再ロードしてプリセット適用
                var reloadedService = new SearchPresetService(tempDir);
                var vm2 = new MainViewModel(reloadedService);
                var loadedPreset = vm2.Presets.FirstOrDefault(p => p.Name == "Stereotactic Advanced");
                Assert.IsNotNull(loadedPreset);

                vm2.SelectedPreset = loadedPreset;
                Assert.AreEqual("TrueBeam, Clinac, !QA_Linac", vm2.MachineFilterText);
                Assert.AreEqual("6X, 10X, !6FFF", vm2.EnergyFilterText);
                Assert.AreEqual("ARC, !STATIC", vm2.TechniqueFilterText);
                Assert.AreEqual(DateFilterTarget.PlanningApprovalDate, vm2.DateTarget);
                Assert.AreEqual(new System.DateTime(2025, 4, 1), vm2.DateFrom);
                Assert.AreEqual(new System.DateTime(2025, 9, 30), vm2.DateTo);
            }
            finally
            {
                if (System.IO.Directory.Exists(tempDir))
                {
                    try { System.IO.Directory.Delete(tempDir, true); } catch { }
                }
            }
        }

        [TestMethod]
        [Description("XAML整合性テスト: MainWindow.xaml で使用されている全 StaticResource が App.xaml に定義されていることを検証（起動クラッシュ再発防止）")]
        public void XamlResourceIntegrity_ShouldHaveNoMissingStaticResources()
        {
            // プロジェクトのルートパスを探索
            string currentDir = System.AppDomain.CurrentDomain.BaseDirectory;
            string solutionDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(currentDir, @"..\..\.."));
            string appXamlPath = System.IO.Path.Combine(solutionDir, @"EclipseDataMiner\App.xaml");
            string mainXamlPath = System.IO.Path.Combine(solutionDir, @"EclipseDataMiner\MainWindow.xaml");

            if (!System.IO.File.Exists(appXamlPath) || !System.IO.File.Exists(mainXamlPath))
            {
                // テスト実行環境によってはパスが異なる場合のフォールバック探索
                string fallbackAppXaml = System.IO.Path.GetFullPath(@"g:\Source\Repos\tkmd94\EclipseDataMiner\EclipseDataMiner\App.xaml");
                string fallbackMainXaml = System.IO.Path.GetFullPath(@"g:\Source\Repos\tkmd94\EclipseDataMiner\EclipseDataMiner\MainWindow.xaml");
                if (System.IO.File.Exists(fallbackAppXaml))
                {
                    appXamlPath = fallbackAppXaml;
                    mainXamlPath = fallbackMainXaml;
                }
            }

            Assert.IsTrue(System.IO.File.Exists(appXamlPath), $"App.xaml not found at: {appXamlPath}");
            Assert.IsTrue(System.IO.File.Exists(mainXamlPath), $"MainWindow.xaml not found at: {mainXamlPath}");

            string appXaml = System.IO.File.ReadAllText(appXamlPath);
            string mainXaml = System.IO.File.ReadAllText(mainXamlPath);

            var keyRegex = new System.Text.RegularExpressions.Regex(@"x:Key=""([^""]+)""");
            var staticRegex = new System.Text.RegularExpressions.Regex(@"StaticResource\s+([A-Za-z0-9_]+)");

            var definedKeys = new HashSet<string>();
            foreach (System.Text.RegularExpressions.Match m in keyRegex.Matches(appXaml))
            {
                definedKeys.Add(m.Groups[1].Value);
            }
            foreach (System.Text.RegularExpressions.Match m in keyRegex.Matches(mainXaml))
            {
                definedKeys.Add(m.Groups[1].Value);
            }

            var missingKeys = new List<string>();
            foreach (System.Text.RegularExpressions.Match m in staticRegex.Matches(mainXaml))
            {
                string usedKey = m.Groups[1].Value;
                if (!definedKeys.Contains(usedKey) && !missingKeys.Contains(usedKey))
                {
                    missingKeys.Add(usedKey);
                }
            }

            Assert.AreEqual(0, missingKeys.Count,
                $"Missing StaticResource keys found in MainWindow.xaml: {string.Join(", ", missingKeys)}. Every StaticResource must be defined in App.xaml or MainWindow.xaml to avoid runtime XamlParseException crash!");
        }

        [TestMethod]
        [Description("WPF XAML完全ロード検証: STAスレッド上でAppおよびMainWindowを初期化し、Style TargetType不一致や実行時パース例外が一切発生しないことを検証")]
        public void MainWindow_XamlLoadingAndStyleResolution_ShouldNotThrowException()
        {
            System.Exception thrownException = null;

            var thread = new System.Threading.Thread(() =>
            {
                try
                {
                    if (System.Windows.Application.Current == null)
                    {
                        var app = new EclipseDataMiner.App();
                        app.InitializeComponent();
                    }

                    var window = new EclipseDataMiner.MainWindow();
                    Assert.IsNotNull(window);
                }
                catch (System.Exception ex)
                {
                    thrownException = ex;
                }
            });

            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (thrownException != null)
            {
                string msg = thrownException.Message;
                var curr = thrownException.InnerException;
                while (curr != null)
                {
                    msg += $"\n --> [{curr.GetType().Name}] {curr.Message}";
                    curr = curr.InnerException;
                }
                Assert.Fail($"MainWindow XAML initialization failed with exception: {msg}");
            }
        }

        [TestMethod]
        [Description("MatchedPlanItem: UniqueKey 生成、複数エネルギー/手法表示、ターゲット輪郭表示の検証")]
        public void MatchedPlanItem_UniqueKey_AndFormattedValues_ShouldWork()
        {
            var item = new MatchedPlanItem
            {
                PatientId = "PT_1001",
                CourseId = "C1",
                PlanId = "Prostate_VMAT",
                TargetVolumeId = "PTV_78Gy",
                DosePerFraction = 2.0,
                NumberOfFractions = 39,
                TotalDose = 78.0,
                TargetDate = new System.DateTime(2025, 4, 15),
                CreationDate = new System.DateTime(2025, 4, 10),
                PlanningApprovalDate = new System.DateTime(2025, 4, 12),
                TreatmentApprovalDate = new System.DateTime(2025, 4, 15),
                Machine = "TrueBeam, Clinac_iX",
                Energy = "6X, 10X",
                Technique = "ARC, STATIC"
            };

            Assert.AreEqual("PT_1001|C1|Prostate_VMAT", item.UniqueKey);
            Assert.AreEqual("PTV_78Gy", item.FormattedTargetVolume);
            Assert.AreEqual("2.00 Gy", item.FormattedDosePerFraction);
            Assert.AreEqual("39", item.FormattedNumberOfFractions);
            Assert.AreEqual("78.00 Gy", item.FormattedTotalDose);
            Assert.AreEqual("2025-04-15", item.FormattedTargetDate);
            Assert.AreEqual("2025-04-10", item.FormattedCreationDate);
            Assert.AreEqual("2025-04-12", item.FormattedPlanningApprovalDate);
            Assert.AreEqual("2025-04-15", item.FormattedTreatmentApprovalDate);
            Assert.AreEqual("TrueBeam, Clinac_iX", item.Machine);
            Assert.AreEqual("6X, 10X", item.Energy);
            Assert.AreEqual("ARC, STATIC", item.Technique);
            Assert.IsTrue(item.IsSelected);

            // HasDose のフォーマット (true = ✔, false = —)
            item.HasDose = true;
            Assert.AreEqual("✔", item.FormattedHasDose);
            item.HasDose = false;
            Assert.AreEqual("—", item.FormattedHasDose);

            // 空の場合のフォーマット
            var emptyItem = new MatchedPlanItem();
            Assert.AreEqual("-", emptyItem.FormattedTargetVolume);
            Assert.AreEqual("—", emptyItem.FormattedHasDose);
            Assert.AreEqual("-", emptyItem.FormattedDosePerFraction);
            Assert.AreEqual("-", emptyItem.FormattedTotalDose);
            Assert.AreEqual("-", emptyItem.FormattedTargetDate);
            Assert.AreEqual("-", emptyItem.FormattedCreationDate);
            Assert.AreEqual("-", emptyItem.FormattedPlanningApprovalDate);
            Assert.AreEqual("-", emptyItem.FormattedTreatmentApprovalDate);
        }

        [TestMethod]
        [Description("MainViewModel: プラン選択コマンド（全選択・全解除・反転・サマリー更新）の動作検証")]
        public void MainViewModel_PlanSelectionCommands_SelectAll_Unselect_Invert_ShouldUpdateSummary()
        {
            var vm = new MainViewModel();

            var p1 = new MatchedPlanItem { PatientId = "P1", CourseId = "C1", PlanId = "PlanA", IsSelected = true };
            var p2 = new MatchedPlanItem { PatientId = "P2", CourseId = "C1", PlanId = "PlanB", IsSelected = true };
            var p3 = new MatchedPlanItem { PatientId = "P3", CourseId = "C2", PlanId = "PlanC", IsSelected = true };

            vm.MatchedPlans.Add(p1);
            vm.MatchedPlans.Add(p2);
            vm.MatchedPlans.Add(p3);

            vm.UpdateMatchedPlansSummary();
            Assert.AreEqual("Selected: 3 / 3 Plans", vm.MatchedPlansSummaryText);
            Assert.IsTrue(vm.HasMatchedPlans);

            // 全解除
            vm.UnselectAllPlansCommand.Execute(null);
            Assert.IsFalse(p1.IsSelected);
            Assert.IsFalse(p2.IsSelected);
            Assert.IsFalse(p3.IsSelected);
            Assert.AreEqual("Selected: 0 / 3 Plans", vm.MatchedPlansSummaryText);

            // 反転
            vm.InvertPlanSelectionCommand.Execute(null);
            Assert.IsTrue(p1.IsSelected);
            Assert.IsTrue(p2.IsSelected);
            Assert.IsTrue(p3.IsSelected);
            Assert.AreEqual("Selected: 3 / 3 Plans", vm.MatchedPlansSummaryText);

            // 1件手動解除
            p1.IsSelected = false;
            vm.UpdateMatchedPlansSummary();
            Assert.AreEqual("Selected: 2 / 3 Plans", vm.MatchedPlansSummaryText);

            // 全選択
            vm.SelectAllPlansCommand.Execute(null);
            Assert.IsTrue(p1.IsSelected);
            Assert.IsTrue(p2.IsSelected);
            Assert.IsTrue(p3.IsSelected);
            Assert.AreEqual("Selected: 3 / 3 Plans", vm.MatchedPlansSummaryText);
        }

        [TestMethod]
        [Description("MainViewModel: SearchPlansCommand の CanExecute と IsRunning の同期検証")]
        public void MainViewModel_SearchPlansCommand_CanExecute_ShouldSyncWithIsRunning()
        {
            var vm = new MainViewModel();

            Assert.IsTrue(vm.SearchPlansCommand.CanExecute(null));
            Assert.IsTrue(vm.RunExtractionCommand.CanExecute(null));

            vm.IsRunning = true;
            Assert.IsFalse(vm.SearchPlansCommand.CanExecute(null));
            Assert.IsFalse(vm.RunExtractionCommand.CanExecute(null));

            vm.IsRunning = false;
            Assert.IsTrue(vm.SearchPlansCommand.CanExecute(null));
            Assert.IsTrue(vm.RunExtractionCommand.CanExecute(null));
        }

        [TestMethod]
        [Description("SearchPresetService: デフォルトPresetsディレクトリに初期サンプルプリセットが存在・ロード可能であることの整合性検証")]
        public void SearchPresetService_DefaultPresetsDirectory_ShouldContainValidPresets()
        {
            var service = new SearchPresetService();
            var presets = service.LoadPresets();

            Assert.IsTrue(presets.Count >= 4, $"Expected at least 4 sample presets, but found {presets.Count}");
            Assert.IsTrue(presets.Any(p => p.Name.Contains("Prostate")), "Prostate preset should be present.");
            Assert.IsTrue(presets.Any(p => p.Name.Contains("Lung")), "Lung preset should be present.");
        }

        [TestMethod]
        [Description("NumericFilterCriteria: 範囲・不等号・単一値検索で double.NaN や Infinity が渡された場合に確実に除外されることを検証")]
        public void NumericFilterCriteria_WhenTargetIsNaNOrInfinity_ShouldAlwaysReturnFalse()
        {
            // 範囲検索 (70-80)
            var rangeCriteria = NumericFilterCriteria.Parse("70-80");
            Assert.IsFalse(rangeCriteria.IsMatch(double.NaN), "Range filter should return false for NaN");
            Assert.IsFalse(rangeCriteria.IsMatch(double.PositiveInfinity), "Range filter should return false for PositiveInfinity");
            Assert.IsFalse(rangeCriteria.IsMatch(double.NegativeInfinity), "Range filter should return false for NegativeInfinity");
            Assert.IsTrue(rangeCriteria.IsMatch(75.0), "Range filter should match valid value");

            // 不等号検索 (>= 10)
            var gteCriteria = NumericFilterCriteria.Parse(">=10");
            Assert.IsFalse(gteCriteria.IsMatch(double.NaN), "GTE filter should return false for NaN");
            Assert.IsFalse(gteCriteria.IsMatch(double.PositiveInfinity), "GTE filter should return false for PositiveInfinity");
            Assert.IsTrue(gteCriteria.IsMatch(10.0), "GTE filter should match boundary value");

            // 不等号検索 (<= 100)
            var lteCriteria = NumericFilterCriteria.Parse("<=100");
            Assert.IsFalse(lteCriteria.IsMatch(double.NaN), "LTE filter should return false for NaN");
            Assert.IsFalse(lteCriteria.IsMatch(double.NegativeInfinity), "LTE filter should return false for NegativeInfinity");
            Assert.IsTrue(lteCriteria.IsMatch(50.0), "LTE filter should match valid value");

            // 単一値検索 (78)
            var exactCriteria = NumericFilterCriteria.Parse("78");
            Assert.IsFalse(exactCriteria.IsMatch(double.NaN), "Exact filter should return false for NaN");
            Assert.IsTrue(exactCriteria.IsMatch(78.0), "Exact filter should match exact value");

            // 空フィルターは全許可
            var emptyCriteria = NumericFilterCriteria.Parse("");
            Assert.IsTrue(emptyCriteria.IsMatch(double.NaN), "Empty filter should accept anything");
        }

        [TestMethod]
        [Description("SearchFilterService: 線量が NaN の場合に TotalDose / DosePerFraction フィルタで除外されることを検証")]
        public void SearchFilterService_WhenPlanDoseIsNaN_ShouldNotMatchNumericFilters()
        {
            var criteria = new SearchFilterCriteria
            {
                GlobalLogicIsAnd = true,
                TotalDoseCriteria = NumericFilterCriteria.Parse("70-80"),
                DosePerFractionCriteria = NumericFilterCriteria.Parse(">=2.0")
            };

            // 線量が NaN の場合 -> false
            bool matchWithNaN = SearchFilterService.IsPlanMatch("VMAT1", "PTV", double.NaN, 35, double.NaN, "TreatmentApproved", false, criteria);
            Assert.IsFalse(matchWithNaN, "Plan with NaN dose must not match range or inequality filter");

            // 正常値の場合 -> true
            bool matchWithValid = SearchFilterService.IsPlanMatch("VMAT1", "PTV", 2.0, 37, 74.0, "TreatmentApproved", false, criteria);
            Assert.IsTrue(matchWithValid, "Plan with valid dose should match filter");
        }

        [TestMethod]
        [Description("MainViewModel: プリセットの Description 編集・保存・再選択時の整合性を検証")]
        public void MainViewModel_PresetDescription_EditAndSave_ShouldPersist()
        {
            string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "EDM_Preset_Desc_Test_" + System.Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(tempDir);
            try
            {
                var service = new SearchPresetService(tempDir);
                var vm = new MainViewModel(service);

                // 新規プリセット作成
                string presetName = "Test_Edit_Desc";
                vm.PresetNameInput = presetName;
                vm.PresetDescriptionInput = "Initial description for testing";
                vm.PlanIdText = "VMAT*";
                vm.ExecuteSavePreset();

                // 保存されたプリセットの Description を確認
                var saved = vm.Presets.FirstOrDefault(p => p.Name == presetName);
                Assert.IsNotNull(saved);
                Assert.AreEqual("Initial description for testing", saved.Description);

                // 画面上で Description を編集して上書き保存
                vm.SelectedPreset = saved;
                Assert.AreEqual("Initial description for testing", vm.PresetDescriptionInput);

                vm.PresetDescriptionInput = "Updated description text with clinical notes";
                vm.ExecuteSavePreset();

                // 更新結果を確認
                var updated = vm.Presets.FirstOrDefault(p => p.Name == presetName);
                Assert.IsNotNull(updated);
                Assert.AreEqual("Updated description text with clinical notes", updated.Description);

                // 再度サービスからロードしてファイル永続化も検証
                var reloadedService = new SearchPresetService(tempDir);
                var reloadedPresets = reloadedService.LoadPresets();
                var persisted = reloadedPresets.FirstOrDefault(p => p.Name == presetName);
                Assert.IsNotNull(persisted);
                Assert.AreEqual("Updated description text with clinical notes", persisted.Description);
            }
            finally
            {
                if (System.IO.Directory.Exists(tempDir))
                {
                    System.IO.Directory.Delete(tempDir, true);
                }
            }
        }

        [TestMethod]
        [Description("MainViewModel: 計画検索用の正規表現ヒントスニペットが初期化され挿入可能であることを検証")]
        public void MainViewModel_SearchRegexSnippets_ShouldBePopulatedAndInsertable()
        {
            var vm = new MainViewModel();

            Assert.IsTrue(vm.SearchRegexSnippets.Count >= 5, "Should have at least 5 search regex snippets");
            Assert.IsTrue(vm.SearchRegexSnippets.Any(s => s.Pattern.Contains("VMAT|IMRT")), "Should contain OR pattern snippet");
            Assert.IsTrue(vm.SearchRegexSnippets.Any(s => s.Pattern.Contains("Boost")), "Should contain Boost pattern snippet");

            // 挿入コマンドの実行検証
            var snippet = vm.SearchRegexSnippets.First(s => s.Pattern.Contains("VMAT|IMRT"));
            vm.PlanIdText = "";
            vm.InsertSearchRegexSnippetCommand.Execute(snippet.Pattern);

            Assert.AreEqual(snippet.Pattern, vm.PlanIdText);
        }

        [TestMethod]
        [Description("SearchFilterService: DosePresenceFilter.HasDose は線量計算済みプランのみ合致し、未計算・NaN・0Gyを除外することを検証")]
        public void SearchFilterService_DosePresenceFilter_HasDose_ShouldOnlyMatchCalculatedPlans()
        {
            var criteria = new SearchFilterCriteria
            {
                DosePresence = DosePresenceFilter.HasDose
            };

            // 線量あり（計算済み） -> true
            Assert.IsTrue(SearchFilterService.IsPlanMatch("VMAT1", "PTV", 2.0, 35, 70.0, "TreatmentApproved", false, criteria));

            // 線量なし (null) -> false
            Assert.IsFalse(SearchFilterService.IsPlanMatch("VMAT1", "PTV", null, 35, null, "TreatmentApproved", false, criteria));

            // 線量が NaN -> false
            Assert.IsFalse(SearchFilterService.IsPlanMatch("VMAT1", "PTV", double.NaN, 35, double.NaN, "TreatmentApproved", false, criteria));

            // 線量が 0Gy -> false
            Assert.IsFalse(SearchFilterService.IsPlanMatch("VMAT1", "PTV", 0.0, 35, 0.0, "TreatmentApproved", false, criteria));
        }

        [TestMethod]
        [Description("SearchFilterService: DosePresenceFilter.NoDose は線量未計算プランのみ合致し、計算済みプランを除外することを検証")]
        public void SearchFilterService_DosePresenceFilter_NoDose_ShouldOnlyMatchUncalculatedPlans()
        {
            var criteria = new SearchFilterCriteria
            {
                DosePresence = DosePresenceFilter.NoDose
            };

            // 線量あり（計算済み） -> false
            Assert.IsFalse(SearchFilterService.IsPlanMatch("VMAT1", "PTV", 2.0, 35, 70.0, "TreatmentApproved", false, criteria));

            // 線量なし (null) -> true
            Assert.IsTrue(SearchFilterService.IsPlanMatch("VMAT1", "PTV", null, 35, null, "TreatmentApproved", false, criteria));

            // 線量が NaN -> true (未計算として扱う)
            Assert.IsTrue(SearchFilterService.IsPlanMatch("VMAT1", "PTV", double.NaN, 35, double.NaN, "TreatmentApproved", false, criteria));
        }

        [TestMethod]
        [Description("MainViewModel: DosePresence フィルタがプリセット保存・適用および BuildCriteria と同期することを検証")]
        public void MainViewModel_DosePresenceFilter_PresetSync_ShouldWorkCorrectly()
        {
            string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "EDM_Preset_Dose_Test_" + System.Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(tempDir);
            try
            {
                var service = new SearchPresetService(tempDir);
                var vm = new MainViewModel(service);

                // 初期状態は All
                Assert.AreEqual(DosePresenceFilter.All, vm.DosePresence);

                // HasDose に設定して保存
                vm.PresetNameInput = "Preset_HasDose";
                vm.DosePresence = DosePresenceFilter.HasDose;
                vm.ExecuteSavePreset();

                // Criteria にも反映されることを確認
                var criteria = vm.BuildCriteria();
                Assert.AreEqual(DosePresenceFilter.HasDose, criteria.DosePresence);

                // NoDose プリセットも作成
                vm.PresetNameInput = "Preset_NoDose";
                vm.DosePresence = DosePresenceFilter.NoDose;
                vm.ExecuteSavePreset();

                // プリセット切り替えで復元されることを確認
                var presetHasDose = vm.Presets.First(p => p.Name == "Preset_HasDose");
                vm.SelectedPreset = presetHasDose;
                Assert.AreEqual(DosePresenceFilter.HasDose, vm.DosePresence);

                var presetNoDose = vm.Presets.First(p => p.Name == "Preset_NoDose");
                vm.SelectedPreset = presetNoDose;
                Assert.AreEqual(DosePresenceFilter.NoDose, vm.DosePresence);
            }
            finally
            {
                if (System.IO.Directory.Exists(tempDir))
                {
                    System.IO.Directory.Delete(tempDir, true);
                }
            }
        }

        [TestMethod]
        [Description("Advanced Filter (照射パラメータ・日付範囲) 指定時に beamRecords および targetDate が渡されることで IsPlanMatch が正しく一致判定されることを検証 (事前スキャン不具合防止)")]
        public void IsPlanMatch_WithAdvancedFilter_WhenBeamsAndDatesSupplied_ShouldMatchCorrectly()
        {
            // Arrange
            var criteria = new SearchFilterCriteria
            {
                MachineFilter = new List<string> { "TrueBeam" },
                EnergyFilter = new List<string> { "6X" },
                TechniqueFilter = new List<string> { "ARC" },
                DateTarget = DateFilterTarget.TreatmentApprovalDate,
                DateFrom = new DateTime(2026, 1, 1),
                DateTo = new DateTime(2026, 12, 31)
            };

            var validBeams = new List<BeamRecord>
            {
                new BeamRecord { BeamId = "B1", TreatmentUnit = "TrueBeam", EnergyModeDisplayName = "6X", Technique = "ARC" }
            };
            var validDate = new DateTime(2026, 6, 15);

            // Act & Assert 1: ビーム情報と日付情報を両方渡した場合 -> 合致 (True)
            bool matchWithAllInfo = SearchFilterService.IsPlanMatch(
                "PT01", "C1", "Plan1", "PTV", 2.0, 30, 60.0, "TreatmentApproved", false,
                validDate, validBeams, criteria);
            Assert.IsTrue(matchWithAllInfo, "ビーム情報と日付情報が合致する場合は True と判定されるべき");

            // Act & Assert 2: ビーム情報・日付情報が null の場合（旧実装の事前スキャンバグのシミュレーション） -> 不一致 (False)
            bool matchWithoutBeams = SearchFilterService.IsPlanMatch(
                "PT01", "C1", "Plan1", "PTV", 2.0, 30, 60.0, "TreatmentApproved", false,
                validDate, null, criteria);
            Assert.IsFalse(matchWithoutBeams, "ビーム情報が null の場合は Advanced Filter に合致せず False になる");

            bool matchWithoutDate = SearchFilterService.IsPlanMatch(
                "PT01", "C1", "Plan1", "PTV", 2.0, 30, 60.0, "TreatmentApproved", false,
                null, validBeams, criteria);
            Assert.IsFalse(matchWithoutDate, "日付情報が null の場合は日付範囲フィルタに合致せず False になる");

            // Act & Assert 3: 異なる装置（Clinac）のビーム情報 -> 不一致 (False)
            var mismatchBeams = new List<BeamRecord>
            {
                new BeamRecord { BeamId = "B1", TreatmentUnit = "Clinac_iX", EnergyModeDisplayName = "6X", Technique = "ARC" }
            };
            bool matchMismatchBeam = SearchFilterService.IsPlanMatch(
                "PT01", "C1", "Plan1", "PTV", 2.0, 30, 60.0, "TreatmentApproved", false,
                validDate, mismatchBeams, criteria);
            Assert.IsFalse(matchMismatchBeam, "装置名が不一致のビーム情報は除外されるべき");

            // Act & Assert 4: 日付範囲外（2025年） -> 不一致 (False)
            var outOfRangeDate = new DateTime(2025, 12, 31);
            bool matchOutOfRangeDate = SearchFilterService.IsPlanMatch(
                "PT01", "C1", "Plan1", "PTV", 2.0, 30, 60.0, "TreatmentApproved", false,
                outOfRangeDate, validBeams, criteria);
            Assert.IsFalse(matchOutOfRangeDate, "日付範囲外の計画は除外されるべき");
        }

        [TestMethod]
        [Description("ShouldSkipPatient: ORロジック時に NumberOfFractions のみが指定されている場合、患者IDが不一致でもスキップされないことを検証")]
        public void ShouldSkipPatient_WhenNumberOfFractionsSpecified_InOrLogic_ShouldNotSkip()
        {
            // Arrange: OR モードで患者IDは "PT999"、分割数は 30
            var criteria = new SearchFilterCriteria
            {
                GlobalLogicIsAnd = false,
                PatientIdFilter = new List<string> { "PT999" },
                NumberOfFractions = 30
            };

            // Act & Assert: 対象患者 "PT001" は患者ID不一致だが、分割数条件が存在するためスキップしてはならない
            bool shouldSkip = SearchFilterService.ShouldSkipPatient("PT001", criteria);
            Assert.IsFalse(shouldSkip, "ORモードで分割数条件が存在する場合、不一致患者も探索対象としてスキップしてはならない");
        }

        [TestMethod]
        [Description("IsPlanMatch: PlanSum に対して作成日および線量有無マーカーが渡された場合、DosePresence および日付フィルタが正確に機能することを検証")]
        public void IsPlanMatch_PlanSum_WithDosePresenceAndDate_ShouldFilterProperly()
        {
            // Arrange
            var criteria = new SearchFilterCriteria
            {
                IncludePlanSums = true,
                DosePresence = DosePresenceFilter.HasDose,
                DateTarget = DateFilterTarget.TreatmentApprovalDate,
                DateFrom = new DateTime(2026, 1, 1),
                DateTo = new DateTime(2026, 12, 31)
            };

            var validDate = new DateTime(2026, 5, 20);
            double? hasDoseMarker = 1.0; // 線量ありマーカー

            // Case 1: 線量あり + 日付範囲内 -> 一致 (True)
            bool match = SearchFilterService.IsPlanMatch(
                "PT01", "C1", "PlanSum1", null, null, null, hasDoseMarker, "PlanSum", true,
                validDate, null, criteria);
            Assert.IsTrue(match, "線量ありかつ日付範囲内の PlanSum は True になるべき");

            // Case 2: 線量なし (null) + DosePresence=HasDose -> 不一致 (False)
            bool noDoseMatch = SearchFilterService.IsPlanMatch(
                "PT01", "C1", "PlanSum1", null, null, null, null, "PlanSum", true,
                validDate, null, criteria);
            Assert.IsFalse(noDoseMatch, "DosePresence=HasDose の時、線量なし PlanSum は False になるべき");

            // Case 3: DosePresence=NoDose に変更時、線量なし PlanSum は 一致 (True)
            criteria.DosePresence = DosePresenceFilter.NoDose;
            bool noDoseCriteriaMatch = SearchFilterService.IsPlanMatch(
                "PT01", "C1", "PlanSum1", null, null, null, null, "PlanSum", true,
                validDate, null, criteria);
            Assert.IsTrue(noDoseCriteriaMatch, "DosePresence=NoDose の時、線量なし PlanSum は True になるべき");

            // Case 4: 日付範囲外（2024年） -> 不一致 (False)
            var outOfRangeDate = new DateTime(2024, 1, 1);
            bool outOfDateMatch = SearchFilterService.IsPlanMatch(
                "PT01", "C1", "PlanSum1", null, null, null, null, "PlanSum", true,
                outOfRangeDate, null, criteria);
            Assert.IsFalse(outOfDateMatch, "日付範囲外の PlanSum は除外されるべき");
        }
    }
}

