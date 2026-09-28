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
        [Description("Verifies OR-based partial matching search using comma-separated patient ID lists")]
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
        [Description("Global logical AND: Verifies true only when all specified conditions are satisfied")]
        public void IsPlanMatch_WhenGlobalLogicIsAnd_ShouldRequireAllConditions()
        {
            // Arrange
            var criteria = new SearchFilterCriteria
            {
                GlobalLogicIsAnd = true,
                PlanIdFilter = new List<string> { "VMAT" },
                TotalDoseGy = 60.0
            };

            // Act & Assert - Both match
            Assert.IsTrue(SearchFilterService.IsPlanMatch("VMAT_Prostate", "PTV", 2.0, 30, 60.0, "TreatmentApproved", false, criteria));

            // Plan ID matches but dose does not -> false
            Assert.IsFalse(SearchFilterService.IsPlanMatch("VMAT_Prostate", "PTV", 2.0, 39, 78.0, "TreatmentApproved", false, criteria));

            // Dose matches but Plan ID does not -> false
            Assert.IsFalse(SearchFilterService.IsPlanMatch("IMRT_Prostate", "PTV", 2.0, 30, 60.0, "TreatmentApproved", false, criteria));
        }

        [TestMethod]
        [Description("Global logical OR: Verifies true when any of the specified conditions is met")]
        public void IsPlanMatch_WhenGlobalLogicIsOr_ShouldMatchIfAnyConditionMet()
        {
            // Arrange
            var criteria = new SearchFilterCriteria
            {
                GlobalLogicIsAnd = false,
                PlanIdFilter = new List<string> { "VMAT" },
                TotalDoseGy = 60.0
            };

            // Plan ID only matches -> true
            Assert.IsTrue(SearchFilterService.IsPlanMatch("VMAT_Prostate", "PTV", 2.0, 39, 78.0, "TreatmentApproved", false, criteria));

            // Dose only matches -> true
            Assert.IsTrue(SearchFilterService.IsPlanMatch("IMRT_Prostate", "PTV", 2.0, 30, 60.0, "TreatmentApproved", false, criteria));

            // Neither matches -> false
            Assert.IsFalse(SearchFilterService.IsPlanMatch("CyberKnife", "PTV", 10.0, 4, 40.0, "TreatmentApproved", false, criteria));
        }

        [TestMethod]
        [Description("Verifies that PlanSum is excluded when IncludePlanSums option is false")]
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
        [Description("Verifies exclusion/permission checks based on approval status filter")]
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
        [Description("Verifies that AND/OR logic toggles mutually synchronize in ViewModel")]
        public void GlobalLogicToggle_InViewModel_ShouldMutuallySync()
        {
            // Arrange
            var vm = new EclipseDataMiner.ViewModels.MainViewModel();

            // Initial state: AND is true, OR is false
            Assert.IsTrue(vm.GlobalLogicIsAnd);
            Assert.IsFalse(vm.GlobalLogicIsOr);

            // Act: Set OR to true
            vm.GlobalLogicIsOr = true;

            // Assert: AND is false, OR is true
            Assert.IsFalse(vm.GlobalLogicIsAnd);
            Assert.IsTrue(vm.GlobalLogicIsOr);

            // Act: Set AND to true again
            vm.GlobalLogicIsAnd = true;

            // Assert: AND is true, OR is false
            Assert.IsTrue(vm.GlobalLogicIsAnd);
            Assert.IsFalse(vm.GlobalLogicIsOr);
        }

        [TestMethod]
        [Description("Verifies that comma-separated parser safely trims and ignores consecutive commas and whitespace")]
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
        [Description("Verifies that comma-separated parser returns an empty list when given null or whitespace")]
        public void ParseCommaSeparated_WhenNullOrEmpty_ShouldReturnEmptyList()
        {
            // Act & Assert
            Assert.AreEqual(0, SearchFilterCriteria.ParseCommaSeparated(null).Count);
            Assert.AreEqual(0, SearchFilterCriteria.ParseCommaSeparated("").Count);
            Assert.AreEqual(0, SearchFilterCriteria.ParseCommaSeparated("   　  ").Count);
        }

        [TestMethod]
        [Description("Verifies that inclusion/exclusion option for PlanSum is evaluated accurately")]
        public void IsPlanMatch_WhenPlanSumIncludedAndExcluded_ShouldFilterProperly()
        {
            // Arrange: Criteria excluding PlanSum
            var criteriaExclude = new SearchFilterCriteria { IncludePlanSums = false };
            // Criteria including PlanSum
            var criteriaInclude = new SearchFilterCriteria { IncludePlanSums = true };

            // Act & Assert
            // Regular plan (isPlanSum: false) -> Matches in both
            Assert.IsTrue(SearchFilterService.IsPlanMatch("Plan1", "PTV", 2.0, 30, 60.0, "TreatmentApproved", false, criteriaExclude));
            Assert.IsTrue(SearchFilterService.IsPlanMatch("Plan1", "PTV", 2.0, 30, 60.0, "TreatmentApproved", false, criteriaInclude));

            // PlanSum (isPlanSum: true) -> False when excluded, True when included
            Assert.IsFalse(SearchFilterService.IsPlanMatch("PlanSum_Total", "PTV", null, null, 70.0, "TreatmentApproved", true, criteriaExclude));
            Assert.IsTrue(SearchFilterService.IsPlanMatch("PlanSum_Total", "PTV", null, null, 70.0, "TreatmentApproved", true, criteriaInclude));
        }

        [TestMethod]
        [Description("Global logical AND: Verifies that all specified conditions including Patient ID and Course ID must match")]
        public void IsPlanMatch_WhenPatientAndCourseIncluded_InAndLogic_ShouldRequireAll()
        {
            // Arrange: Specify Patient ID, Course ID, and Plan ID
            var criteria = new SearchFilterCriteria
            {
                GlobalLogicIsAnd = true,
                PatientIdFilter = new List<string> { "PT100" },
                CourseIdFilter = new List<string> { "C1" },
                PlanIdFilter = new List<string> { "VMAT" }
            };

            // All match -> true
            Assert.IsTrue(SearchFilterService.IsPlanMatch("PT100_ABC", "C1_Rad", "VMAT_1", "PTV", 2.0, 30, 60.0, "TreatmentApproved", false, criteria));

            // Patient ID mismatch -> false
            Assert.IsFalse(SearchFilterService.IsPlanMatch("PT200_ABC", "C1_Rad", "VMAT_1", "PTV", 2.0, 30, 60.0, "TreatmentApproved", false, criteria));

            // Course ID mismatch -> false
            Assert.IsFalse(SearchFilterService.IsPlanMatch("PT100_ABC", "C2_Boost", "VMAT_1", "PTV", 2.0, 30, 60.0, "TreatmentApproved", false, criteria));

            // Plan ID mismatch -> false
            Assert.IsFalse(SearchFilterService.IsPlanMatch("PT100_ABC", "C1_Rad", "IMRT_1", "PTV", 2.0, 30, 60.0, "TreatmentApproved", false, criteria));
        }

        [TestMethod]
        [Description("Global logical OR: Verifies true when any of Patient ID, Course ID, Plan ID, or dose matches")]
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

            // Patient ID only matches -> true
            Assert.IsTrue(SearchFilterService.IsPlanMatch("PT100", "C99", "OTHER", "PTV", 2.0, 20, 40.0, "TreatmentApproved", false, criteria));

            // Course ID only matches -> true
            Assert.IsTrue(SearchFilterService.IsPlanMatch("PT999", "C1", "OTHER", "PTV", 2.0, 20, 40.0, "TreatmentApproved", false, criteria));

            // Plan ID only matches -> true
            Assert.IsTrue(SearchFilterService.IsPlanMatch("PT999", "C99", "VMAT_Pros", "PTV", 2.0, 20, 40.0, "TreatmentApproved", false, criteria));

            // Dose only matches -> true
            Assert.IsTrue(SearchFilterService.IsPlanMatch("PT999", "C99", "OTHER", "PTV", 2.0, 30, 60.0, "TreatmentApproved", false, criteria));

            // None match -> false
            Assert.IsFalse(SearchFilterService.IsPlanMatch("PT999", "C99", "OTHER", "PTV", 2.0, 20, 40.0, "TreatmentApproved", false, criteria));
        }

        [TestMethod]
        [Description("ShouldSkipPatient: In AND logic, verifies immediate skip when patient ID does not match")]
        public void ShouldSkipPatient_WhenAndLogic_ShouldSkipIfPatientNotMatched()
        {
            var criteria = new SearchFilterCriteria
            {
                GlobalLogicIsAnd = true,
                PatientIdFilter = new List<string> { "PT100" },
                PlanIdFilter = new List<string> { "VMAT" }
            };

            // Match -> Do not skip (false)
            Assert.IsFalse(SearchFilterService.ShouldSkipPatient("PT100_A", criteria));

            // Mismatch -> Skip (true)
            Assert.IsTrue(SearchFilterService.ShouldSkipPatient("PT200", criteria));
        }

        [TestMethod]
        [Description("ShouldSkipPatient: In OR logic, verifies patient is not skipped on ID mismatch if plan criteria exist")]
        public void ShouldSkipPatient_WhenOrLogic_ShouldNotSkipIfOtherCriteriaSpecified()
        {
            var criteriaWithOther = new SearchFilterCriteria
            {
                GlobalLogicIsAnd = false,
                PatientIdFilter = new List<string> { "PT100" },
                PlanIdFilter = new List<string> { "VMAT" } // Other criteria exist
            };

            // Even if Patient ID mismatches, Plan ID is specified so it must not be skipped
            Assert.IsFalse(SearchFilterService.ShouldSkipPatient("PT200", criteriaWithOther));

            // When no other criteria are specified
            var criteriaPatientOnly = new SearchFilterCriteria
            {
                GlobalLogicIsAnd = false,
                PatientIdFilter = new List<string> { "PT100" }
            };

            // Patient ID matches -> Do not skip
            Assert.IsFalse(SearchFilterService.ShouldSkipPatient("PT100", criteriaPatientOnly));

            // Patient ID mismatches with no other criteria -> Can skip
            Assert.IsTrue(SearchFilterService.ShouldSkipPatient("PT200", criteriaPatientOnly));
        }

        [TestMethod]
        [Description("ShouldSkipCourse: Verifies course skip evaluation behaves properly in AND/OR logic")]
        public void ShouldSkipCourse_WhenAndOrLogic_ShouldBehaveCorrectly()
        {
            var criteriaAnd = new SearchFilterCriteria
            {
                GlobalLogicIsAnd = true,
                CourseIdFilter = new List<string> { "C1" },
                PlanIdFilter = new List<string> { "VMAT" }
            };

            // AND: Immediate skip if course mismatches
            Assert.IsTrue(SearchFilterService.ShouldSkipCourse("C2", criteriaAnd));
            Assert.IsFalse(SearchFilterService.ShouldSkipCourse("C1", criteriaAnd));

            var criteriaOrWithPlan = new SearchFilterCriteria
            {
                GlobalLogicIsAnd = false,
                CourseIdFilter = new List<string> { "C1" },
                PlanIdFilter = new List<string> { "VMAT" }
            };

            // OR: Other criteria (PlanId) exist, so do not skip on course mismatch
            Assert.IsFalse(SearchFilterService.ShouldSkipCourse("C2", criteriaOrWithPlan));

            var criteriaOrCourseOnly = new SearchFilterCriteria
            {
                GlobalLogicIsAnd = false,
                CourseIdFilter = new List<string> { "C1" }
            };

            // OR: Course criteria only; can skip if course mismatches
            Assert.IsTrue(SearchFilterService.ShouldSkipCourse("C2", criteriaOrCourseOnly));
        }

        [TestMethod]
        [Description("IsTextMatch: Exact mode verifies 'a' does not match 'Plan' and matches only exact values")]
        public void IsTextMatch_WhenModeIsExact_ShouldMatchOnlyExactValues()
        {
            var filter = new List<string> { "a" };

            // In partial match "Plan" contains "a", but in Exact mode it mismatches
            Assert.IsFalse(SearchFilterService.IsTextMatch("Plan", filter, TextMatchMode.Exact));

            // Exact match (case insensitive) matches
            Assert.IsTrue(SearchFilterService.IsTextMatch("a", filter, TextMatchMode.Exact));
            Assert.IsTrue(SearchFilterService.IsTextMatch("A", filter, TextMatchMode.Exact));
        }

        [TestMethod]
        [Description("IsTextMatch: Regex mode verifies matches only when fitting regex pattern")]
        public void IsTextMatch_WhenModeIsRegex_ShouldMatchRegexPattern()
        {
            var filter = new List<string> { "^Plan_\\d+$" };

            Assert.IsTrue(SearchFilterService.IsTextMatch("Plan_1", filter, TextMatchMode.Regex));
            Assert.IsTrue(SearchFilterService.IsTextMatch("plan_123", filter, TextMatchMode.Regex));
            Assert.IsFalse(SearchFilterService.IsTextMatch("Boost_Plan_1", filter, TextMatchMode.Regex));
            Assert.IsFalse(SearchFilterService.IsTextMatch("Plan_Boost", filter, TextMatchMode.Regex));
        }

        [TestMethod]
        [Description("IsPlanMatch: When PlanIdMatchMode is Exact, 'a' skips 'Plan' and 'Plan' matches exactly")]
        public void IsPlanMatch_WhenPlanIdMatchModeIsExact_ShouldFilterCorrectly()
        {
            // Set "a" in Plan ID filter and mode to Exact
            var criteriaExactA = new SearchFilterCriteria
            {
                PlanIdFilter = new List<string> { "a" },
                PlanIdMatchMode = TextMatchMode.Exact
            };

            // Plan with Plan ID "Plan" -> false (contains "a" but not exact match)
            Assert.IsFalse(SearchFilterService.IsPlanMatch("PT1", "C1", "Plan", "PTV", 2.0, 30, 60.0, "TreatmentApproved", false, criteriaExactA));

            // Set "Plan" in Plan ID filter and mode to Exact
            var criteriaExactPlan = new SearchFilterCriteria
            {
                PlanIdFilter = new List<string> { "Plan" },
                PlanIdMatchMode = TextMatchMode.Exact
            };

            // Plan with Plan ID "Plan" -> true
            Assert.IsTrue(SearchFilterService.IsPlanMatch("PT1", "C1", "Plan", "PTV", 2.0, 30, 60.0, "TreatmentApproved", false, criteriaExactPlan));

            // Plan with Plan ID "Plan1" -> false
            Assert.IsFalse(SearchFilterService.IsPlanMatch("PT1", "C1", "Plan1", "PTV", 2.0, 30, 60.0, "TreatmentApproved", false, criteriaExactPlan));
        }

        [TestMethod]
        [Description("IsPlanMatch: When PlanIdMatchMode is Regex, verifies accurate filtering by regular expression")]
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
        [Description("ParseTextFilter: Verifies separated parsing of regular tokens and exclusion tokens (! / -)")]
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
        [Description("NumericFilterCriteria.Parse: Verifies parsing and IsMatch evaluation for range syntax (70-80, 70~80)")]
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
        [Description("NumericFilterCriteria.Parse: Verifies parsing and IsMatch evaluation for inequality syntax (>=10, <30)")]
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
        [Description("IsPlanMatch: Verifies NOT exclude filter (!QA, !Test) evaluation")]
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

            // VMAT_Prostate -> Match
            Assert.IsTrue(SearchFilterService.IsPlanMatch("PT1", "C1", "VMAT_Prostate", "PTV", null, null, null, "TreatmentApproved", false, criteria));

            // VMAT_QA -> Contains "QA", so should be excluded
            Assert.IsFalse(SearchFilterService.IsPlanMatch("PT1", "C1", "VMAT_QA", "PTV", null, null, null, "TreatmentApproved", false, criteria));

            // QA_IMRT -> Contains "QA", so excluded
            Assert.IsFalse(SearchFilterService.IsPlanMatch("PT1", "C1", "QA_IMRT", "PTV", null, null, null, "TreatmentApproved", false, criteria));

            // 3DCRT -> Does not match include list
            Assert.IsFalse(SearchFilterService.IsPlanMatch("PT1", "C1", "3DCRT", "PTV", null, null, null, "TreatmentApproved", false, criteria));
        }

        [TestMethod]
        [Description("IsPlanMatch: Verifies that only non-QA plans match when only an exclude filter (!QA) is specified")]
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
        [Description("IsPlanMatch: Verifies composite protocol criteria evaluation (Lung SBRT: >=10Gy/Fr, 4-5Fr, 48-60Gy)")]
        public void IsPlanMatch_LungSbrtCriteria_ShouldMatchCorrectProtocol()
        {
            var criteria = new SearchFilterCriteria
            {
                DosePerFractionCriteria = NumericFilterCriteria.Parse(">= 10"),
                NumberOfFractionsCriteria = NumericFilterCriteria.Parse("4 - 5"),
                TotalDoseCriteria = NumericFilterCriteria.Parse("48 - 60"),
                GlobalLogicIsAnd = true
            };

            // Lung SBRT plan: 12Gy x 4Fr = 48Gy -> Match
            Assert.IsTrue(SearchFilterService.IsPlanMatch("PT1", "C1", "Lung_SBRT", "PTV", 12.0, 4, 48.0, "TreatmentApproved", false, criteria));

            // Lung SBRT plan: 10Gy x 5Fr = 50Gy -> Match
            Assert.IsTrue(SearchFilterService.IsPlanMatch("PT1", "C1", "Lung_SBRT", "PTV", 10.0, 5, 50.0, "TreatmentApproved", false, criteria));

            // Prostate standard fractionation: 2Gy x 39Fr = 78Gy -> Mismatch
            Assert.IsFalse(SearchFilterService.IsPlanMatch("PT2", "C1", "Prostate", "PTV", 2.0, 39, 78.0, "TreatmentApproved", false, criteria));

            // Dose matches but fractions is 30 -> Mismatch
            Assert.IsFalse(SearchFilterService.IsPlanMatch("PT3", "C1", "Other", "PTV", 10.0, 30, 50.0, "TreatmentApproved", false, criteria));
        }

        [TestMethod]
        [Description("SearchPresetService: Verifies individual JSON file save, load, and deletion within presets folder")]
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

                // Save single preset
                service.SavePreset(custom);

                // Verify physical file exists
                string expectedFile = System.IO.Path.Combine(tempDir, "Custom Palliative 30Gy.json");
                Assert.IsTrue(System.IO.File.Exists(expectedFile));

                // Reload
                var reloadedService = new SearchPresetService(tempDir);
                var reloaded = reloadedService.LoadPresets();
                Assert.AreEqual(1, reloaded.Count);

                var reloadedCustom = reloaded.FirstOrDefault(p => p.Name == "Custom Palliative 30Gy");
                Assert.IsNotNull(reloadedCustom);
                Assert.AreEqual("3.0", reloadedCustom.DosePerFractionText);
                Assert.AreEqual("10", reloadedCustom.NumberOfFractionsText);
                Assert.AreEqual("30", reloadedCustom.TotalDoseText);
                Assert.AreEqual(expectedFile, reloadedCustom.FilePath);

                // Delete
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
        [Description("MainViewModel: Verifies criteria construction integration with ApplyPreset and BuildCriteria")]
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

                // Select and apply preset
                vm.SelectedPreset = loaded;

                // Verify ViewModel properties updated
                Assert.AreEqual(">= 10", vm.DosePerFractionText);
                Assert.AreEqual("4 - 5", vm.NumberOfFractionsText);
                Assert.AreEqual("48 - 60", vm.TotalDoseText);

                // Verify BuildCriteria output
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
        [Description("MainViewModel: Verifies saving and deleting presets (no built-in restrictions, arbitrary deletion)")]
        public void MainViewModel_PresetManagement_SaveAndDelete_ShouldFunction()
        {
            string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"presets_manage_{System.Guid.NewGuid()}");
            try
            {
                var service = new SearchPresetService(tempDir);
                var vm = new MainViewModel(service);

                // Initial state is empty
                Assert.AreEqual(0, vm.Presets.Count);
                Assert.IsFalse(vm.DeletePresetCommand.CanExecute(null));

                // Save new preset
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

                // Deletable because it is selected
                Assert.IsTrue(vm.DeletePresetCommand.CanExecute(null));

                // Verify physical file exists
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
        [Description("IsDateMatch: Verifies date range matching (From to To, open-ended, null handling)")]
        public void IsDateMatch_VariousRanges_ShouldFilterCorrectly()
        {
            var date = new System.DateTime(2025, 6, 15, 14, 30, 0);

            // 1. Unspecified -> always true
            Assert.IsTrue(SearchFilterService.IsDateMatch(date, null, null));
            Assert.IsTrue(SearchFilterService.IsDateMatch(null, null, null));

            // 2. Date criteria specified but target date is null -> false
            Assert.IsFalse(SearchFilterService.IsDateMatch(null, new System.DateTime(2025, 1, 1), null));
            Assert.IsFalse(SearchFilterService.IsDateMatch(null, null, new System.DateTime(2025, 12, 31)));

            // 3. From only specified
            Assert.IsTrue(SearchFilterService.IsDateMatch(date, new System.DateTime(2025, 6, 15), null)); // Same day included
            Assert.IsTrue(SearchFilterService.IsDateMatch(date, new System.DateTime(2025, 6, 1), null));
            Assert.IsFalse(SearchFilterService.IsDateMatch(date, new System.DateTime(2025, 6, 16), null));

            // 4. To only specified
            Assert.IsTrue(SearchFilterService.IsDateMatch(date, null, new System.DateTime(2025, 6, 15))); // Includes up to 23:59:59
            Assert.IsTrue(SearchFilterService.IsDateMatch(date, null, new System.DateTime(2025, 7, 1)));
            Assert.IsFalse(SearchFilterService.IsDateMatch(date, null, new System.DateTime(2025, 6, 14)));

            // 5. From to To range specified
            Assert.IsTrue(SearchFilterService.IsDateMatch(date, new System.DateTime(2025, 6, 1), new System.DateTime(2025, 6, 30)));
            Assert.IsFalse(SearchFilterService.IsDateMatch(date, new System.DateTime(2025, 1, 1), new System.DateTime(2025, 5, 31)));
            Assert.IsFalse(SearchFilterService.IsDateMatch(date, new System.DateTime(2025, 7, 1), new System.DateTime(2025, 12, 31)));
        }

        [TestMethod]
        [Description("IsBeamMatch: Verifies composite evaluation of Machine, Energy, Technique inclusion and !exclusion")]
        public void IsBeamMatch_MachineEnergyTechnique_ShouldFilterCorrectly()
        {
            var normalBeams = new List<BeamRecord>
            {
                new BeamRecord { BeamId = "B1", TreatmentUnit = "TrueBeam1", EnergyModeDisplayName = "6X", Technique = "ARC", IsSetupField = false },
                new BeamRecord { BeamId = "B2", TreatmentUnit = "TrueBeam1", EnergyModeDisplayName = "10X", Technique = "ARC", IsSetupField = false }
            };

            // 1. Machine ID inclusion match
            Assert.IsTrue(SearchFilterService.IsBeamMatch(normalBeams,
                machineIncludes: new List<string> { "TrueBeam" }, machineExcludes: null,
                energyIncludes: null, energyExcludes: null,
                techniqueIncludes: null, techniqueExcludes: null));

            // 2. Machine ID mismatch
            Assert.IsFalse(SearchFilterService.IsBeamMatch(normalBeams,
                machineIncludes: new List<string> { "Clinac" }, machineExcludes: null,
                energyIncludes: null, energyExcludes: null,
                techniqueIncludes: null, techniqueExcludes: null));

            // 3. Machine ID exclusion (!TrueBeam1)
            Assert.IsFalse(SearchFilterService.IsBeamMatch(normalBeams,
                machineIncludes: null, machineExcludes: new List<string> { "TrueBeam1" },
                energyIncludes: null, energyExcludes: null,
                techniqueIncludes: null, techniqueExcludes: null));

            // 4. Energy inclusion match (6X)
            Assert.IsTrue(SearchFilterService.IsBeamMatch(normalBeams,
                machineIncludes: null, machineExcludes: null,
                energyIncludes: new List<string> { "6X" }, energyExcludes: null,
                techniqueIncludes: null, techniqueExcludes: null));

            // 5. Energy exclusion (!10X) -> B2 is 10X so it should be excluded
            Assert.IsFalse(SearchFilterService.IsBeamMatch(normalBeams,
                machineIncludes: null, machineExcludes: null,
                energyIncludes: null, energyExcludes: new List<string> { "10X" },
                techniqueIncludes: null, techniqueExcludes: null));

            // 6. Technique inclusion (ARC) & exclusion (!STATIC) -> STATIC absent, matches via ARC
            Assert.IsTrue(SearchFilterService.IsBeamMatch(normalBeams,
                machineIncludes: null, machineExcludes: null,
                energyIncludes: null, energyExcludes: null,
                techniqueIncludes: new List<string> { "ARC" }, techniqueExcludes: new List<string> { "STATIC" }));
        }

        [TestMethod]
        [Description("IsPlanMatch: Verifies comprehensive evaluation including date ranges and beam delivery parameters")]
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

            // All conditions match -> true
            Assert.IsTrue(SearchFilterService.IsPlanMatch("PT1", "C1", "VMAT_Prostate", "PTV", 2.0, 39, 78.0, "TreatmentApproved", false, approvedDate, beams, criteria));

            // Date out of range (2024) -> false
            var oldDate = new System.DateTime(2024, 12, 10);
            Assert.IsFalse(SearchFilterService.IsPlanMatch("PT1", "C1", "VMAT_Prostate", "PTV", 2.0, 39, 78.0, "TreatmentApproved", false, oldDate, beams, criteria));

            // Machine mismatch -> false
            var otherBeams = new List<BeamRecord>
            {
                new BeamRecord { BeamId = "Field1", TreatmentUnit = "Clinac_iX", EnergyModeDisplayName = "6X", Technique = "VMAT_ARC", IsSetupField = false }
            };
            Assert.IsFalse(SearchFilterService.IsPlanMatch("PT1", "C1", "VMAT_Prostate", "PTV", 2.0, 39, 78.0, "TreatmentApproved", false, approvedDate, otherBeams, criteria));
        }

        [TestMethod]
        [Description("MainViewModel: Verifies advanced filter properties, ClearDatesCommand, and preset save/load")]
        public void MainViewModel_AdvancedFiltersAndPresetSync_ShouldWorkCorrectly()
        {
            string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"presets_adv_{System.Guid.NewGuid()}");
            try
            {
                var service = new SearchPresetService(tempDir);
                var vm = new MainViewModel(service);

                // Configure advanced filter properties
                vm.MachineFilterText = "TrueBeam, Clinac, !QA_Linac";
                vm.EnergyFilterText = "6X, 10X, !6FFF";
                vm.TechniqueFilterText = "ARC, !STATIC";
                vm.DateTarget = DateFilterTarget.PlanningApprovalDate;
                vm.DateFrom = new System.DateTime(2025, 4, 1);
                vm.DateTo = new System.DateTime(2025, 9, 30);

                // Verify BuildCriteria
                var criteria = vm.BuildCriteria();
                Assert.AreEqual(2, criteria.MachineFilter.Count);
                Assert.AreEqual(1, criteria.MachineExcludeFilter.Count);
                Assert.AreEqual("QA_Linac", criteria.MachineExcludeFilter[0]);
                Assert.AreEqual(2, criteria.EnergyFilter.Count);
                Assert.AreEqual(1, criteria.EnergyExcludeFilter.Count);
                Assert.AreEqual(DateFilterTarget.PlanningApprovalDate, criteria.DateTarget);
                Assert.AreEqual(new System.DateTime(2025, 4, 1), criteria.DateFrom);
                Assert.AreEqual(new System.DateTime(2025, 9, 30), criteria.DateTo);

                // Verify ClearDatesCommand
                Assert.IsTrue(vm.ClearDatesCommand.CanExecute(null));
                vm.ClearDatesCommand.Execute(null);
                Assert.IsNull(vm.DateFrom);
                Assert.IsNull(vm.DateTo);

                // Save as preset
                vm.DateFrom = new System.DateTime(2025, 4, 1);
                vm.DateTo = new System.DateTime(2025, 9, 30);
                vm.PresetNameInput = "Stereotactic Advanced";
                vm.SavePresetCommand.Execute(null);

                // Reload and apply preset
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
        [Description("XAML integrity test: Verifies that all StaticResources used in MainWindow.xaml are defined in App.xaml (preventing startup crash recurrence)")]
        public void XamlResourceIntegrity_ShouldHaveNoMissingStaticResources()
        {
            // Search for project root path
            string currentDir = System.AppDomain.CurrentDomain.BaseDirectory;
            string solutionDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(currentDir, @"..\..\.."));
            string appXamlPath = System.IO.Path.Combine(solutionDir, @"EclipseDataMiner\App.xaml");
            string mainXamlPath = System.IO.Path.Combine(solutionDir, @"EclipseDataMiner\MainWindow.xaml");

            if (!System.IO.File.Exists(appXamlPath) || !System.IO.File.Exists(mainXamlPath))
            {
                // Fallback search when paths differ depending on test execution environment
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
        [Description("WPF XAML full load test: Initializes App and MainWindow on STA thread, verifying no Style TargetType mismatches or runtime parsing exceptions")]
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
        [Description("MatchedPlanItem: Verifies UniqueKey generation, multiple energy/technique display, and target volume formatting")]
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

            // HasDose formatting (true = ✔, false = —)
            item.HasDose = true;
            Assert.AreEqual("✔", item.FormattedHasDose);
            item.HasDose = false;
            Assert.AreEqual("—", item.FormattedHasDose);

            // Empty formatting
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
        [Description("MainViewModel: Verifies plan selection commands (select all, unselect all, invert, summary update)")]
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

            // Unselect all
            vm.UnselectAllPlansCommand.Execute(null);
            Assert.IsFalse(p1.IsSelected);
            Assert.IsFalse(p2.IsSelected);
            Assert.IsFalse(p3.IsSelected);
            Assert.AreEqual("Selected: 0 / 3 Plans", vm.MatchedPlansSummaryText);

            // Invert
            vm.InvertPlanSelectionCommand.Execute(null);
            Assert.IsTrue(p1.IsSelected);
            Assert.IsTrue(p2.IsSelected);
            Assert.IsTrue(p3.IsSelected);
            Assert.AreEqual("Selected: 3 / 3 Plans", vm.MatchedPlansSummaryText);

            // Manually uncheck 1 item
            p1.IsSelected = false;
            vm.UpdateMatchedPlansSummary();
            Assert.AreEqual("Selected: 2 / 3 Plans", vm.MatchedPlansSummaryText);

            // Select all
            vm.SelectAllPlansCommand.Execute(null);
            Assert.IsTrue(p1.IsSelected);
            Assert.IsTrue(p2.IsSelected);
            Assert.IsTrue(p3.IsSelected);
            Assert.AreEqual("Selected: 3 / 3 Plans", vm.MatchedPlansSummaryText);
        }

        [TestMethod]
        [Description("MainViewModel: Verifies synchronization between SearchPlansCommand.CanExecute and IsRunning")]
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
        [Description("SearchPresetService: Verifies that initial sample presets exist and can be loaded from the default Presets directory")]
        public void SearchPresetService_DefaultPresetsDirectory_ShouldContainValidPresets()
        {
            var service = new SearchPresetService();
            var presets = service.LoadPresets();

            Assert.IsTrue(presets.Count >= 4, $"Expected at least 4 sample presets, but found {presets.Count}");
            Assert.IsTrue(presets.Any(p => p.Name.Contains("Prostate")), "Prostate preset should be present.");
            Assert.IsTrue(presets.Any(p => p.Name.Contains("Lung")), "Lung preset should be present.");
        }

        [TestMethod]
        [Description("NumericFilterCriteria: Verifies that double.NaN or Infinity are properly excluded in range, inequality, and exact value searches")]
        public void NumericFilterCriteria_WhenTargetIsNaNOrInfinity_ShouldAlwaysReturnFalse()
        {
            // Range search (70-80)
            var rangeCriteria = NumericFilterCriteria.Parse("70-80");
            Assert.IsFalse(rangeCriteria.IsMatch(double.NaN), "Range filter should return false for NaN");
            Assert.IsFalse(rangeCriteria.IsMatch(double.PositiveInfinity), "Range filter should return false for PositiveInfinity");
            Assert.IsFalse(rangeCriteria.IsMatch(double.NegativeInfinity), "Range filter should return false for NegativeInfinity");
            Assert.IsTrue(rangeCriteria.IsMatch(75.0), "Range filter should match valid value");

            // Inequality search (>= 10)
            var gteCriteria = NumericFilterCriteria.Parse(">=10");
            Assert.IsFalse(gteCriteria.IsMatch(double.NaN), "GTE filter should return false for NaN");
            Assert.IsFalse(gteCriteria.IsMatch(double.PositiveInfinity), "GTE filter should return false for PositiveInfinity");
            Assert.IsTrue(gteCriteria.IsMatch(10.0), "GTE filter should match boundary value");

            // Inequality search (<= 100)
            var lteCriteria = NumericFilterCriteria.Parse("<=100");
            Assert.IsFalse(lteCriteria.IsMatch(double.NaN), "LTE filter should return false for NaN");
            Assert.IsFalse(lteCriteria.IsMatch(double.NegativeInfinity), "LTE filter should return false for NegativeInfinity");
            Assert.IsTrue(lteCriteria.IsMatch(50.0), "LTE filter should match valid value");

            // Exact value search (78)
            var exactCriteria = NumericFilterCriteria.Parse("78");
            Assert.IsFalse(exactCriteria.IsMatch(double.NaN), "Exact filter should return false for NaN");
            Assert.IsTrue(exactCriteria.IsMatch(78.0), "Exact filter should match exact value");

            // Empty filter matches everything
            var emptyCriteria = NumericFilterCriteria.Parse("");
            Assert.IsTrue(emptyCriteria.IsMatch(double.NaN), "Empty filter should accept anything");
        }

        [TestMethod]
        [Description("SearchFilterService: Verifies that NaN dose plans are excluded by TotalDose and DosePerFraction filters")]
        public void SearchFilterService_WhenPlanDoseIsNaN_ShouldNotMatchNumericFilters()
        {
            var criteria = new SearchFilterCriteria
            {
                GlobalLogicIsAnd = true,
                TotalDoseCriteria = NumericFilterCriteria.Parse("70-80"),
                DosePerFractionCriteria = NumericFilterCriteria.Parse(">=2.0")
            };

            // Dose is NaN -> false
            bool matchWithNaN = SearchFilterService.IsPlanMatch("VMAT1", "PTV", double.NaN, 35, double.NaN, "TreatmentApproved", false, criteria);
            Assert.IsFalse(matchWithNaN, "Plan with NaN dose must not match range or inequality filter");

            // Valid dose -> true
            bool matchWithValid = SearchFilterService.IsPlanMatch("VMAT1", "PTV", 2.0, 37, 74.0, "TreatmentApproved", false, criteria);
            Assert.IsTrue(matchWithValid, "Plan with valid dose should match filter");
        }

        [TestMethod]
        [Description("MainViewModel: Verifies consistency when editing, saving, and re-selecting preset descriptions")]
        public void MainViewModel_PresetDescription_EditAndSave_ShouldPersist()
        {
            string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "EDM_Preset_Desc_Test_" + System.Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(tempDir);
            try
            {
                var service = new SearchPresetService(tempDir);
                var vm = new MainViewModel(service);

                // Create new preset
                string presetName = "Test_Edit_Desc";
                vm.PresetNameInput = presetName;
                vm.PresetDescriptionInput = "Initial description for testing";
                vm.PlanIdText = "VMAT*";
                vm.ExecuteSavePreset();

                // Check Description of the saved preset
                var saved = vm.Presets.FirstOrDefault(p => p.Name == presetName);
                Assert.IsNotNull(saved);
                Assert.AreEqual("Initial description for testing", saved.Description);

                // Edit Description on UI and overwrite save
                vm.SelectedPreset = saved;
                Assert.AreEqual("Initial description for testing", vm.PresetDescriptionInput);

                vm.PresetDescriptionInput = "Updated description text with clinical notes";
                vm.ExecuteSavePreset();

                // Verify update result
                var updated = vm.Presets.FirstOrDefault(p => p.Name == presetName);
                Assert.IsNotNull(updated);
                Assert.AreEqual("Updated description text with clinical notes", updated.Description);

                // Reload from service to verify file persistence
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
        [Description("MainViewModel: Verifies that regular expression hint snippets for plan search are populated and insertable")]
        public void MainViewModel_SearchRegexSnippets_ShouldBePopulatedAndInsertable()
        {
            var vm = new MainViewModel();

            Assert.IsTrue(vm.SearchRegexSnippets.Count >= 5, "Should have at least 5 search regex snippets");
            Assert.IsTrue(vm.SearchRegexSnippets.Any(s => s.Pattern.Contains("VMAT|IMRT")), "Should contain OR pattern snippet");
            Assert.IsTrue(vm.SearchRegexSnippets.Any(s => s.Pattern.Contains("Boost")), "Should contain Boost pattern snippet");

            // Verify execution of insert command
            var snippet = vm.SearchRegexSnippets.First(s => s.Pattern.Contains("VMAT|IMRT"));
            vm.PlanIdText = "";
            vm.InsertSearchRegexSnippetCommand.Execute(snippet.Pattern);

            Assert.AreEqual(snippet.Pattern, vm.PlanIdText);
        }

        [TestMethod]
        [Description("SearchFilterService: Verifies that DosePresenceFilter.HasDose matches only dose-calculated plans, excluding uncalculated, NaN, and 0 Gy plans")]
        public void SearchFilterService_DosePresenceFilter_HasDose_ShouldOnlyMatchCalculatedPlans()
        {
            var criteria = new SearchFilterCriteria
            {
                DosePresence = DosePresenceFilter.HasDose
            };

            // Dose present (calculated) -> true
            Assert.IsTrue(SearchFilterService.IsPlanMatch("VMAT1", "PTV", 2.0, 35, 70.0, "TreatmentApproved", false, criteria));

            // No dose (null) -> false
            Assert.IsFalse(SearchFilterService.IsPlanMatch("VMAT1", "PTV", null, 35, null, "TreatmentApproved", false, criteria));

            // Dose is NaN -> false
            Assert.IsFalse(SearchFilterService.IsPlanMatch("VMAT1", "PTV", double.NaN, 35, double.NaN, "TreatmentApproved", false, criteria));

            // Dose is 0 Gy -> false
            Assert.IsFalse(SearchFilterService.IsPlanMatch("VMAT1", "PTV", 0.0, 35, 0.0, "TreatmentApproved", false, criteria));
        }

        [TestMethod]
        [Description("SearchFilterService: Verifies that DosePresenceFilter.NoDose matches only uncalculated plans and excludes calculated plans")]
        public void SearchFilterService_DosePresenceFilter_NoDose_ShouldOnlyMatchUncalculatedPlans()
        {
            var criteria = new SearchFilterCriteria
            {
                DosePresence = DosePresenceFilter.NoDose
            };

            // Dose present (calculated) -> false
            Assert.IsFalse(SearchFilterService.IsPlanMatch("VMAT1", "PTV", 2.0, 35, 70.0, "TreatmentApproved", false, criteria));

            // No dose (null) -> true
            Assert.IsTrue(SearchFilterService.IsPlanMatch("VMAT1", "PTV", null, 35, null, "TreatmentApproved", false, criteria));

            // Dose is NaN -> true (treated as uncalculated)
            Assert.IsTrue(SearchFilterService.IsPlanMatch("VMAT1", "PTV", double.NaN, 35, double.NaN, "TreatmentApproved", false, criteria));
        }

        [TestMethod]
        [Description("MainViewModel: Verifies that DosePresence filter syncs with preset save/apply and BuildCriteria")]
        public void MainViewModel_DosePresenceFilter_PresetSync_ShouldWorkCorrectly()
        {
            string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "EDM_Preset_Dose_Test_" + System.Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(tempDir);
            try
            {
                var service = new SearchPresetService(tempDir);
                var vm = new MainViewModel(service);

                // Default state is All
                Assert.AreEqual(DosePresenceFilter.All, vm.DosePresence);

                // Set to HasDose and save
                vm.PresetNameInput = "Preset_HasDose";
                vm.DosePresence = DosePresenceFilter.HasDose;
                vm.ExecuteSavePreset();

                // Verify reflection in Criteria
                var criteria = vm.BuildCriteria();
                Assert.AreEqual(DosePresenceFilter.HasDose, criteria.DosePresence);

                // Create NoDose preset as well
                vm.PresetNameInput = "Preset_NoDose";
                vm.DosePresence = DosePresenceFilter.NoDose;
                vm.ExecuteSavePreset();

                // Verify restoration when switching presets
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
        [Description("IsPlanMatch: Verifies that passing beamRecords and targetDate with Advanced Filters (irradiation parameters and date range) correctly matches plans")]
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

            // Act & Assert 1: When both beam and date info are provided -> Match (True)
            bool matchWithAllInfo = SearchFilterService.IsPlanMatch(
                "PT01", "C1", "Plan1", "PTV", 2.0, 30, 60.0, "TreatmentApproved", false,
                validDate, validBeams, criteria);
            Assert.IsTrue(matchWithAllInfo, "Should evaluate to True when both beam information and date match.");

            // Act & Assert 2: When beam or date info is null -> Non-match (False)
            bool matchWithoutBeams = SearchFilterService.IsPlanMatch(
                "PT01", "C1", "Plan1", "PTV", 2.0, 30, 60.0, "TreatmentApproved", false,
                validDate, null, criteria);
            Assert.IsFalse(matchWithoutBeams, "Should be False when beam information is null with Advanced Filter specified.");

            bool matchWithoutDate = SearchFilterService.IsPlanMatch(
                "PT01", "C1", "Plan1", "PTV", 2.0, 30, 60.0, "TreatmentApproved", false,
                null, validBeams, criteria);
            Assert.IsFalse(matchWithoutDate, "Should be False when date information is null with date range filter specified.");

            // Act & Assert 3: Beam information with different machine (Clinac) -> Non-match (False)
            var mismatchBeams = new List<BeamRecord>
            {
                new BeamRecord { BeamId = "B1", TreatmentUnit = "Clinac_iX", EnergyModeDisplayName = "6X", Technique = "ARC" }
            };
            bool matchMismatchBeam = SearchFilterService.IsPlanMatch(
                "PT01", "C1", "Plan1", "PTV", 2.0, 30, 60.0, "TreatmentApproved", false,
                validDate, mismatchBeams, criteria);
            Assert.IsFalse(matchMismatchBeam, "Beams with non-matching machine names should be excluded.");

            // Act & Assert 4: Outside date range (2025) -> Non-match (False)
            var outOfRangeDate = new DateTime(2025, 12, 31);
            bool matchOutOfRangeDate = SearchFilterService.IsPlanMatch(
                "PT01", "C1", "Plan1", "PTV", 2.0, 30, 60.0, "TreatmentApproved", false,
                outOfRangeDate, validBeams, criteria);
            Assert.IsFalse(matchOutOfRangeDate, "Plans outside the date range should be excluded.");
        }

        [TestMethod]
        [Description("ShouldSkipPatient: Verifies that when only NumberOfFractions is specified in OR mode, patients with non-matching IDs are not skipped")]
        public void ShouldSkipPatient_WhenNumberOfFractionsSpecified_InOrLogic_ShouldNotSkip()
        {
            // Arrange: OR mode with PatientId='PT999' and Fractions=30
            var criteria = new SearchFilterCriteria
            {
                GlobalLogicIsAnd = false,
                PatientIdFilter = new List<string> { "PT999" },
                NumberOfFractions = 30
            };

            // Act & Assert: Target patient 'PT001' does not match PatientId, but must not be skipped because fraction condition exists
            bool shouldSkip = SearchFilterService.ShouldSkipPatient("PT001", criteria);
            Assert.IsFalse(shouldSkip, "Non-matching patients must not be skipped when fraction condition is specified in OR mode.");
        }

        [TestMethod]
        [Description("IsPlanMatch: Verifies that DosePresence and date filters work accurately when creation date and dose presence markers are passed for PlanSum")]
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
            double? hasDoseMarker = 1.0; // Marker indicating dose is present

            // Case 1: Dose present + within date range -> Match (True)
            bool match = SearchFilterService.IsPlanMatch(
                "PT01", "C1", "PlanSum1", null, null, null, hasDoseMarker, "PlanSum", true,
                validDate, null, criteria);
            Assert.IsTrue(match, "PlanSum with dose within date range should be True.");

            // Case 2: No dose (null) + DosePresence=HasDose -> Non-match (False)
            bool noDoseMatch = SearchFilterService.IsPlanMatch(
                "PT01", "C1", "PlanSum1", null, null, null, null, "PlanSum", true,
                validDate, null, criteria);
            Assert.IsFalse(noDoseMatch, "PlanSum without dose should be False when DosePresence=HasDose.");

            // Case 3: When changed to DosePresence=NoDose, PlanSum without dose -> Match (True)
            criteria.DosePresence = DosePresenceFilter.NoDose;
            bool noDoseCriteriaMatch = SearchFilterService.IsPlanMatch(
                "PT01", "C1", "PlanSum1", null, null, null, null, "PlanSum", true,
                validDate, null, criteria);
            Assert.IsTrue(noDoseCriteriaMatch, "PlanSum without dose should be True when DosePresence=NoDose.");

            // Case 4: Outside date range (2024) -> Non-match (False)
            var outOfRangeDate = new DateTime(2024, 1, 1);
            bool outOfDateMatch = SearchFilterService.IsPlanMatch(
                "PT01", "C1", "PlanSum1", null, null, null, null, "PlanSum", true,
                outOfRangeDate, null, criteria);
            Assert.IsFalse(outOfDateMatch, "PlanSum outside date range should be excluded.");
        }
    }
}

