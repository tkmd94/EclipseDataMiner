using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EclipseDataMiner.Models;
using EclipseDataMiner.Services;

namespace EclipseDataMiner.Tests
{
    [TestClass]
    public class StructureMappingTests
    {
        private string _tempDir;

        [TestInitialize]
        public void Setup()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "StructureMappingTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_tempDir))
            {
                try { Directory.Delete(_tempDir, true); } catch { }
            }
        }

        [TestMethod]
        [Description("Verifies alias resolution using Exact and Contains matching modes")]
        public void ResolveMapping_ExactAndContains_ShouldResolveAlias()
        {
            // Arrange
            var rules = new List<StructureMappingRule>
            {
                new StructureMappingRule { Pattern = "PTV_60", MatchMode = StructureMatchMode.Exact, TargetAlias = "PTV_HIGH" },
                new StructureMappingRule { Pattern = "Rectum", MatchMode = StructureMatchMode.Contains, TargetAlias = "Rectum_Merged" }
            };

            // Act & Assert
            var res1 = StructureMappingService.ResolveMapping("PTV_60", rules);
            Assert.IsTrue(res1.IsSelected);
            Assert.AreEqual("PTV_HIGH", res1.TargetAlias);

            var res2 = StructureMappingService.ResolveMapping("Rectum_Wall", rules);
            Assert.IsTrue(res2.IsSelected);
            Assert.AreEqual("Rectum_Merged", res2.TargetAlias);

            // Structures not matching any rule retain their original ID
            var res3 = StructureMappingService.ResolveMapping("Bladder", rules);
            Assert.IsTrue(res3.IsSelected);
            Assert.AreEqual("Bladder", res3.TargetAlias);
        }

        [TestMethod]
        [Description("Verifies alias resolution using Regex matching (e.g. ^PTV.* -> PTV)")]
        public void ResolveMapping_Regex_ShouldMatchPattern()
        {
            // Arrange
            var rules = new List<StructureMappingRule>
            {
                new StructureMappingRule { Pattern = @"^PTV[-_]?\d+", MatchMode = StructureMatchMode.Regex, TargetAlias = "PTV_Target" }
            };

            // Act & Assert
            var res1 = StructureMappingService.ResolveMapping("PTV-60", rules);
            Assert.IsTrue(res1.IsSelected);
            Assert.AreEqual("PTV_Target", res1.TargetAlias);

            var res2 = StructureMappingService.ResolveMapping("PTV_78Gy", rules);
            Assert.IsTrue(res2.IsSelected);
            Assert.AreEqual("PTV_Target", res2.TargetAlias);

            var res3 = StructureMappingService.ResolveMapping("CTV-60", rules);
            Assert.IsTrue(res3.IsSelected);
            Assert.AreEqual("CTV-60", res3.TargetAlias); // No match
        }

        [TestMethod]
        [Description("Verifies extraction exclusion when opt-out (IsSelected = false) is configured")]
        public void ResolveMapping_WhenOptedOut_ShouldReturnFalse()
        {
            // Arrange
            var rules = new List<StructureMappingRule>
            {
                new StructureMappingRule { Pattern = "BODY", MatchMode = StructureMatchMode.Exact, IsSelected = false }
            };

            // Act
            var res = StructureMappingService.ResolveMapping("BODY", rules);

            // Assert
            Assert.IsFalse(res.IsSelected);
        }

        [TestMethod]
        [Description("Verifies round-trip persistence of mapping rules via JSON save and load")]
        public void SaveAndLoadRules_ShouldPreserveRuleProperties()
        {
            // Arrange
            string jsonPath = Path.Combine(_tempDir, "mapping_rules.json");
            var originalRules = new List<StructureMappingRule>
            {
                new StructureMappingRule { Pattern = "PTV_60", MatchMode = StructureMatchMode.Exact, TargetAlias = "PTV_Unified", IsSelected = true, MatchedCount = 42 },
                new StructureMappingRule { Pattern = "OAR_.*", MatchMode = StructureMatchMode.Regex, TargetAlias = "OAR_All", IsSelected = false, MatchedCount = 10 }
            };

            // Act
            StructureMappingService.SaveRulesToFile(jsonPath, originalRules);
            var loadedRules = StructureMappingService.LoadRulesFromFile(jsonPath);

            // Assert
            Assert.AreEqual(2, loadedRules.Count);
            Assert.AreEqual("PTV_60", loadedRules[0].Pattern);
            Assert.AreEqual(StructureMatchMode.Exact, loadedRules[0].MatchMode);
            Assert.AreEqual("PTV_Unified", loadedRules[0].TargetAlias);
            Assert.IsTrue(loadedRules[0].IsSelected);
            Assert.AreEqual(42, loadedRules[0].MatchedCount);

            Assert.AreEqual("OAR_.*", loadedRules[1].Pattern);
            Assert.AreEqual(StructureMatchMode.Regex, loadedRules[1].MatchMode);
            Assert.IsFalse(loadedRules[1].IsSelected);
        }

        [TestMethod]
        [Description("Verifies loading and alias resolution of sample prostate mapping JSON (StructureMapping_Prostate.json)")]
        public void LoadSampleProstateMappingRules_ShouldResolveCorrectly()
        {
            // Arrange
            string baseDir = Path.GetDirectoryName(typeof(StructureMappingTests).Assembly.Location);
            string samplePath = Path.Combine(baseDir, "Templates", "StructureMapping_Prostate.json");
            Assert.IsTrue(File.Exists(samplePath), $"Sample file not found: {samplePath}");

            // Act
            var rules = StructureMappingService.LoadRulesFromFile(samplePath);

            // Assert
            Assert.IsTrue(rules.Count >= 10, "Rule count should be 10 or greater");

            // PTV alias consolidation test
            var ptvMatch1 = StructureMappingService.ResolveMapping("PTV_60Gy", rules);
            Assert.IsTrue(ptvMatch1.IsSelected);
            Assert.AreEqual("PTV", ptvMatch1.TargetAlias);

            var ptvMatch2 = StructureMappingService.ResolveMapping("ptv-prost", rules);
            Assert.IsTrue(ptvMatch2.IsSelected);
            Assert.AreEqual("PTV", ptvMatch2.TargetAlias);

            // Rectum and bladder tests
            var rectumMatch = StructureMappingService.ResolveMapping("Rectum_Wall", rules);
            Assert.IsTrue(rectumMatch.IsSelected);
            Assert.AreEqual("Rectum", rectumMatch.TargetAlias);

            var bladderMatch = StructureMappingService.ResolveMapping("Bladder", rules);
            Assert.IsTrue(bladderMatch.IsSelected);
            Assert.AreEqual("Bladder", bladderMatch.TargetAlias);

            // Opt-out (excluded) structure tests
            var bodyMatch = StructureMappingService.ResolveMapping("BODY", rules);
            Assert.IsFalse(bodyMatch.IsSelected);

            var couchMatch = StructureMappingService.ResolveMapping("CouchInterior", rules);
            Assert.IsFalse(couchMatch.IsSelected);
        }

        [TestMethod]
        [Description("Verifies that RefreshPreview immediately re-evaluates scan results against updated rules")]
        public void RefreshPreview_ShouldUpdateDiscoveredItemsWithCorrectAliasAndStatus()
        {
            // Arrange
            var discovered = new List<DiscoveredStructureItem>
            {
                new DiscoveredStructureItem { RawStructureId = "PTV_60", HitCount = 50 },
                new DiscoveredStructureItem { RawStructureId = "Rectum", HitCount = 80 },
                new DiscoveredStructureItem { RawStructureId = "BODY", HitCount = 100 },
                new DiscoveredStructureItem { RawStructureId = "UnmappedOAR", HitCount = 10 }
            };

            var rules = new List<StructureMappingRule>
            {
                new StructureMappingRule { Pattern = "PTV.*", MatchMode = StructureMatchMode.Regex, TargetAlias = "PTV_Integrated", IsSelected = true },
                new StructureMappingRule { Pattern = "BODY", MatchMode = StructureMatchMode.Exact, TargetAlias = "", IsSelected = false }
            };

            // Act
            StructureMappingService.RefreshPreview(discovered, rules);

            // Assert
            // PTV_60: Matched and consolidated via Regex
            Assert.IsTrue(discovered[0].IsExtracted);
            Assert.AreEqual("PTV_Integrated", discovered[0].ResolvedAlias);
            Assert.IsTrue(discovered[0].MatchStatus.Contains("Mapped (Regex"));

            // Rectum: No rule -> Kept as raw
            Assert.IsTrue(discovered[1].IsExtracted);
            Assert.AreEqual("Rectum", discovered[1].ResolvedAlias);
            Assert.AreEqual("Unmapped (Raw)", discovered[1].MatchStatus);

            // BODY: Excluded via opt-out rule
            Assert.IsFalse(discovered[2].IsExtracted);
            Assert.IsTrue(discovered[2].MatchStatus.Contains("Excluded (Exact)"));

            // UnmappedOAR: Unmapped
            Assert.IsTrue(discovered[3].IsExtracted);
            Assert.AreEqual("UnmappedOAR", discovered[3].ResolvedAlias);
            Assert.AreEqual("Unmapped (Raw)", discovered[3].MatchStatus);
        }

        [TestMethod]
        [Description("Verifies that rule addition and adding discovered items work properly in 2-pane ViewModel operations")]
        public void MainViewModel_RuleManagement_ShouldFunctionCorrectly()
        {
            // Arrange
            var vm = new EclipseDataMiner.ViewModels.MainViewModel();
            Assert.AreEqual(0, vm.MappingRules.Count);
            Assert.AreEqual(0, vm.DiscoveredStructures.Count);

            // Act 1: Add rule
            vm.AddRuleCommand.Execute(null);
            Assert.AreEqual(1, vm.MappingRules.Count);
            Assert.IsNotNull(vm.SelectedRule);

            // Act 2: Simulate and register discovered item
            var item = new DiscoveredStructureItem { RawStructureId = "NewDiscoveredOrgan", HitCount = 15 };
            vm.DiscoveredStructures.Add(item);
            vm.SelectedDiscoveredItem = item;

            // Act 3: Add discovered item to rules
            vm.AddDiscoveredToRulesCommand.Execute(null);

            // Assert: Rule count becomes 2, with added rule pattern as NewDiscoveredOrgan
            Assert.AreEqual(2, vm.MappingRules.Count);
            Assert.AreEqual("NewDiscoveredOrgan", vm.MappingRules[1].Pattern);

            // Preview is refreshed and status becomes Mapped
            Assert.AreEqual("NewDiscoveredOrgan", item.ResolvedAlias);
            Assert.IsTrue(item.MatchStatus.Contains("Mapped (Exact"));
        }

        [TestMethod]
        [Description("Verifies specificity priority (Exact > Contains > Regex), ensuring Exact matches take precedence regardless of registration order")]
        public void ResolveMapping_ShouldPrioritizeExactOverRegex()
        {
            // Arrange: Register Regex rule first and Exact rule second
            var rules = new List<StructureMappingRule>
            {
                new StructureMappingRule { Pattern = "PTV.*", MatchMode = StructureMatchMode.Regex, TargetAlias = "PTV_Generic", IsSelected = true },
                new StructureMappingRule { Pattern = "PTV_60", MatchMode = StructureMatchMode.Exact, TargetAlias = "PTV_HighDose", IsSelected = true }
            };

            // Act: Evaluate PTV_60
            var res = StructureMappingService.ResolveMapping("PTV_60", rules);

            // Assert: Exact (PTV_HighDose) registered later takes priority
            Assert.AreEqual("PTV_HighDose", res.TargetAlias);

            // Act: Evaluate PTV_54 (matches only Regex, not Exact)
            var resGeneric = StructureMappingService.ResolveMapping("PTV_54", rules);

            // Assert: Regex (PTV_Generic) is applied
            Assert.AreEqual("PTV_Generic", resGeneric.TargetAlias);
        }

        [TestMethod]
        [Description("Verifies that MoveUp and MoveDown update MappingRules order, resolve priority, and refresh CanExecute status immediately")]
        public void MoveUpAndDown_ShouldReorderRulesAndAffectSamePriorityMatching()
        {
            // Arrange
            var vm = new EclipseDataMiner.ViewModels.MainViewModel();
            var rule1 = new StructureMappingRule { Pattern = "Rectum", MatchMode = StructureMatchMode.Contains, TargetAlias = "Alias1" };
            var rule2 = new StructureMappingRule { Pattern = "Rect", MatchMode = StructureMatchMode.Contains, TargetAlias = "Alias2" };
            var rule3 = new StructureMappingRule { Pattern = "Bladder", MatchMode = StructureMatchMode.Contains, TargetAlias = "Alias3" };
            vm.MappingRules.Add(rule1);
            vm.MappingRules.Add(rule2);
            vm.MappingRules.Add(rule3);

            // Initial state: rule1 at top -> Alias1 is applied
            var res1 = StructureMappingService.ResolveMapping("Rectum_Wall", vm.MappingRules);
            Assert.AreEqual("Alias1", res1.TargetAlias);

            // Act: Select rule2 (middle item, index 1)
            vm.SelectedRule = rule2;
            Assert.IsTrue(vm.MoveUpRuleCommand.CanExecute(null));
            Assert.IsTrue(vm.MoveDownRuleCommand.CanExecute(null));

            // Move rule2 Up -> index 0
            vm.MoveUpRuleCommand.Execute(null);
            Assert.AreEqual("Rect", vm.MappingRules[0].Pattern);
            Assert.AreEqual(rule2, vm.SelectedRule);
            // Now at top: CanMoveUp should be false, CanMoveDown should be true
            Assert.IsFalse(vm.MoveUpRuleCommand.CanExecute(null));
            Assert.IsTrue(vm.MoveDownRuleCommand.CanExecute(null));

            // Move rule2 Down -> index 1
            vm.MoveDownRuleCommand.Execute(null);
            Assert.AreEqual("Rect", vm.MappingRules[1].Pattern);
            Assert.AreEqual(rule2, vm.SelectedRule);
            Assert.IsTrue(vm.MoveUpRuleCommand.CanExecute(null));
            Assert.IsTrue(vm.MoveDownRuleCommand.CanExecute(null));

            // Move rule2 Down again -> index 2 (bottom)
            vm.MoveDownRuleCommand.Execute(null);
            Assert.AreEqual("Rect", vm.MappingRules[2].Pattern);
            Assert.AreEqual(rule2, vm.SelectedRule);
            // Now at bottom: CanMoveUp should be true, CanMoveDown should be false
            Assert.IsTrue(vm.MoveUpRuleCommand.CanExecute(null));
            Assert.IsFalse(vm.MoveDownRuleCommand.CanExecute(null));
        }

        [TestMethod]
        [Description("Verifies that DQP items can be reordered up and down, and command CanExecute statuses update accurately")]
        public void MoveUpAndDownDqp_ShouldReorderDqpsAndCommandCanExecuteShouldUpdate()
        {
            var vm = new EclipseDataMiner.ViewModels.MainViewModel();
            vm.DQPList.Clear();

            var dqp1 = new DQP { structureName = "PTV", DQPtype = DQPtype.Dose, DQPvalue = 95.0, InputUnit = IOUnit.Relative, OutputUnit = IOUnit.Absolute };
            var dqp2 = new DQP { structureName = "Rectum", DQPtype = DQPtype.Volume, DQPvalue = 70.0, InputUnit = IOUnit.Absolute, OutputUnit = IOUnit.Relative };
            var dqp3 = new DQP { structureName = "Bladder", DQPtype = DQPtype.Volume, DQPvalue = 50.0, InputUnit = IOUnit.Absolute, OutputUnit = IOUnit.Relative };

            vm.DQPList.Add(dqp1);
            vm.DQPList.Add(dqp2);
            vm.DQPList.Add(dqp3);

            // Select dqp2 (middle)
            vm.SelectedDqp = dqp2;
            Assert.IsTrue(vm.MoveUpDqpCommand.CanExecute(null));
            Assert.IsTrue(vm.MoveDownDqpCommand.CanExecute(null));

            // Move Up -> index 0
            vm.MoveUpDqpCommand.Execute(null);
            Assert.AreEqual("Rectum", vm.DQPList[0].structureName);
            Assert.AreEqual(dqp2, vm.SelectedDqp);
            Assert.IsFalse(vm.MoveUpDqpCommand.CanExecute(null));
            Assert.IsTrue(vm.MoveDownDqpCommand.CanExecute(null));

            // Move Down -> index 1
            vm.MoveDownDqpCommand.Execute(null);
            Assert.AreEqual("Rectum", vm.DQPList[1].structureName);
            Assert.AreEqual(dqp2, vm.SelectedDqp);
            Assert.IsTrue(vm.MoveUpDqpCommand.CanExecute(null));
            Assert.IsTrue(vm.MoveDownDqpCommand.CanExecute(null));

            // Move Down -> index 2 (bottom)
            vm.MoveDownDqpCommand.Execute(null);
            Assert.AreEqual("Rectum", vm.DQPList[2].structureName);
            Assert.AreEqual(dqp2, vm.SelectedDqp);
            Assert.IsTrue(vm.MoveUpDqpCommand.CanExecute(null));
            Assert.IsFalse(vm.MoveDownDqpCommand.CanExecute(null));
        }

        [TestMethod]
        [Description("Verifies that [Merged: N] annotation is added when multiple structures map to the same Target Alias")]
        public void RefreshPreview_WhenTargetAliasDuplicates_ShouldAnnotateIntegratedCount()
        {
            // Arrange
            var discovered = new List<DiscoveredStructureItem>
            {
                new DiscoveredStructureItem { RawStructureId = "PTV_60", HitCount = 50 },
                new DiscoveredStructureItem { RawStructureId = "ptv_boost", HitCount = 30 }
            };

            var rules = new List<StructureMappingRule>
            {
                new StructureMappingRule { Pattern = "PTV.*|ptv.*", MatchMode = StructureMatchMode.Regex, TargetAlias = "PTV", IsSelected = true }
            };

            // Act
            StructureMappingService.RefreshPreview(discovered, rules);

            // Assert: Both items consolidated to PTV, with [Merged: 2] added to status
            Assert.AreEqual("PTV", discovered[0].ResolvedAlias);
            Assert.AreEqual("PTV", discovered[1].ResolvedAlias);
            Assert.IsTrue(discovered[0].MatchStatus.Contains("[Merged: 2]"));
            Assert.IsTrue(discovered[1].MatchStatus.Contains("[Merged: 2]"));
        }

        [TestMethod]
        [Description("Verifies that DQP addition/deletion and log clear/copy commands function properly in ViewModel")]
        public void MainViewModel_DqpManagement_And_LogCommands_ShouldFunctionCorrectly()
        {
            // Arrange
            var vm = new EclipseDataMiner.ViewModels.MainViewModel();
            int initialDqpCount = vm.DQPList.Count;

            // Act 1: Add DQP
            vm.AddDqpCommand.Execute(null);
            Assert.AreEqual(initialDqpCount + 1, vm.DQPList.Count);
            var addedDqp = vm.SelectedDqp;
            Assert.IsNotNull(addedDqp);
            Assert.AreEqual(EclipseDataMiner.Models.DQPtype.Dose, addedDqp.DQPtype);

            // Act 2: Delete DQP
            vm.DeleteDqpCommand.Execute(null);
            Assert.AreEqual(initialDqpCount, vm.DQPList.Count);

            // Act 3: Log operations
            vm.LogText = "2026-09-25 [INFO] Test message";
            Assert.IsFalse(string.IsNullOrEmpty(vm.LogText));

            vm.ClearLogCommand.Execute(null);
            Assert.AreEqual(string.Empty, vm.LogText);
        }

        [TestMethod]
        [Description("Verifies that invalid regex patterns (syntax error) safely return false without crashing")]
        public void IsMatch_WhenInvalidRegex_ShouldNotThrowAndReturnFalse()
        {
            // Arrange: Syntax-invalid regex patterns
            var ruleUnclosedBracket = new StructureMappingRule { Pattern = "PTV_[0-9(", MatchMode = StructureMatchMode.Regex };
            var ruleInvalidModifier = new StructureMappingRule { Pattern = "(?<invalid", MatchMode = StructureMatchMode.Regex };
            var ruleDanglingStar = new StructureMappingRule { Pattern = "*PTV", MatchMode = StructureMatchMode.Regex };

            // Act & Assert: Return false safely without throwing exceptions
            Assert.IsFalse(ruleUnclosedBracket.IsMatch("PTV_60"));
            Assert.IsFalse(ruleInvalidModifier.IsMatch("PTV_60"));
            Assert.IsFalse(ruleDanglingStar.IsMatch("PTV_60"));
        }

        [TestMethod]
        [Description("Verifies accurate mapping evaluation for structure names containing Japanese or special symbols (+, /, -)")]
        public void IsMatch_WithJapaneseAndSpecialCharacters_ShouldMatchCorrectly()
        {
            // Arrange
            var ruleJapanese = new StructureMappingRule { Pattern = "耳下腺_L", MatchMode = StructureMatchMode.Exact, TargetAlias = "Parotid_L" };
            var rulePlus = new StructureMappingRule { Pattern = "PTV+5mm", MatchMode = StructureMatchMode.Exact, TargetAlias = "PTV_Margin" };
            var ruleSlash = new StructureMappingRule { Pattern = "CTV_60/30", MatchMode = StructureMatchMode.Contains, TargetAlias = "CTV_High" };

            // Act & Assert
            Assert.IsTrue(ruleJapanese.IsMatch("耳下腺_L"));
            Assert.IsFalse(ruleJapanese.IsMatch("耳下腺_R"));

            Assert.IsTrue(rulePlus.IsMatch("PTV+5mm"));
            Assert.IsFalse(rulePlus.IsMatch("PTV-5mm"));

            Assert.IsTrue(ruleSlash.IsMatch("PLAN_CTV_60/30_BOOST"));
            Assert.IsFalse(ruleSlash.IsMatch("PLAN_CTV_50/25"));
        }

        [TestMethod]
        [Description("Verifies strict adherence to specificity ordering when Exact, Contains, and Regex all match identical input")]
        public void FindBestMatchingRule_WhenMultipleModesMatch_ShouldStrictlyFollowPriority()
        {
            // Arrange: Register rules in reverse order: Regex, Contains, Exact
            var rules = new List<StructureMappingRule>
            {
                new StructureMappingRule { Pattern = ".*", MatchMode = StructureMatchMode.Regex, TargetAlias = "Regex_CatchAll", IsSelected = true },
                new StructureMappingRule { Pattern = "Bladder", MatchMode = StructureMatchMode.Contains, TargetAlias = "Contains_Bladder", IsSelected = true },
                new StructureMappingRule { Pattern = "Bladder", MatchMode = StructureMatchMode.Exact, TargetAlias = "Exact_Bladder", IsSelected = true }
            };

            // Act: Search for "Bladder"
            var best = StructureMappingService.FindBestMatchingRule("Bladder", rules);

            // Assert: Exact (highest specificity) is chosen instead of Regex at the top
            Assert.IsNotNull(best);
            Assert.AreEqual(StructureMatchMode.Exact, best.MatchMode);
            Assert.AreEqual("Exact_Bladder", best.TargetAlias);

            // Act 2: Search for "Bladder_Wall" (Exact does not match)
            var bestSub = StructureMappingService.FindBestMatchingRule("Bladder_Wall", rules);

            // Assert 2: Contains is chosen instead of Regex
            Assert.IsNotNull(bestSub);
            Assert.AreEqual(StructureMatchMode.Contains, bestSub.MatchMode);
            Assert.AreEqual("Contains_Bladder", bestSub.TargetAlias);
        }

        [TestMethod]
        [Description("Verifies that IsRegexError is false for valid regex patterns")]
        public void ValidateRegex_WhenValidPattern_ShouldSetIsRegexErrorFalse()
        {
            var rule = new StructureMappingRule
            {
                MatchMode = StructureMatchMode.Regex,
                Pattern = "^PTV.*"
            };

            Assert.IsFalse(rule.IsRegexError);
            Assert.IsTrue(string.IsNullOrEmpty(rule.RegexErrorMessage));
        }

        [TestMethod]
        [Description("Verifies that IsRegexError is true and message is populated for syntax-invalid regex")]
        public void ValidateRegex_WhenInvalidPattern_ShouldSetIsRegexErrorTrueAndMessage()
        {
            var rule = new StructureMappingRule
            {
                MatchMode = StructureMatchMode.Regex,
                Pattern = "(PTV" // Invalid: missing closing parenthesis
            };

            Assert.IsTrue(rule.IsRegexError);
            Assert.IsFalse(string.IsNullOrEmpty(rule.RegexErrorMessage));
        }

        [TestMethod]
        [Description("Verifies that regex errors are automatically cleared when switching from Regex to Exact or Contains mode")]
        public void ValidateRegex_WhenSwitchingToNonRegex_ShouldClearError()
        {
            var rule = new StructureMappingRule
            {
                MatchMode = StructureMatchMode.Regex,
                Pattern = "(PTV"
            };
            Assert.IsTrue(rule.IsRegexError);

            // Act: Change to Exact
            rule.MatchMode = StructureMatchMode.Exact;

            // Assert: Error is cleared
            Assert.IsFalse(rule.IsRegexError);
            Assert.IsTrue(string.IsNullOrEmpty(rule.RegexErrorMessage));
        }

        [TestMethod]
        [Description("Verifies that ViewModel regex snippet list includes frequent clinical presets")]
        public void RegexSnippets_InViewModel_ShouldContainClinicalPresets()
        {
            var vm = new EclipseDataMiner.ViewModels.MainViewModel();

            Assert.IsTrue(vm.RegexSnippets.Count >= 7);
            Assert.IsTrue(vm.RegexSnippets.Any(s => s.Pattern == "^PTV.*"));
            Assert.IsTrue(vm.RegexSnippets.Any(s => s.Pattern == ".*[_-](Rt|Lt|R|L)$"));
            Assert.IsTrue(vm.RegexSnippets.Any(s => s.Pattern == "(Bladder|Rectum)"));
        }

        [TestMethod]
        [Description("Verifies that executing InsertRegexSnippetCommand applies pattern to selected rule and switches mode to Regex")]
        public void InsertRegexSnippetCommand_ShouldApplyPatternAndSwitchModeToRegex()
        {
            var vm = new EclipseDataMiner.ViewModels.MainViewModel();
            var rule = new StructureMappingRule
            {
                Pattern = "OldPattern",
                MatchMode = StructureMatchMode.Exact
            };
            vm.MappingRules.Add(rule);
            vm.SelectedRule = rule;

            // Act: Insert snippet
            vm.ExecuteInsertRegexSnippet("^PTV.*");

            // Assert: Pattern updated, mode switched to Regex, no error
            Assert.AreEqual("^PTV.*", vm.SelectedRule.Pattern);
            Assert.AreEqual(StructureMatchMode.Regex, vm.SelectedRule.MatchMode);
            Assert.IsFalse(vm.SelectedRule.IsRegexError);
        }

        [TestMethod]
        [Description("Comprehensively verifies mapping resolution for structure names with clinical special characters (+, #, ( ), [ ], _)")]
        public void ResolveMapping_WithComplexClinicalNames_ShouldResolveProperly()
        {
            var rules = new List<StructureMappingRule>
            {
                new StructureMappingRule { Pattern = "CTV-Prostate+SV", MatchMode = StructureMatchMode.Exact, TargetAlias = "CTV_High" },
                new StructureMappingRule { Pattern = "+SV", MatchMode = StructureMatchMode.Contains, TargetAlias = "SV_Boost" },
                new StructureMappingRule { Pattern = @"PTV_.*#1", MatchMode = StructureMatchMode.Regex, TargetAlias = "PTV_Fraction1" },
                new StructureMappingRule { Pattern = "(OAR)", MatchMode = StructureMatchMode.Contains, TargetAlias = "OAR_Generic" },
                new StructureMappingRule { Pattern = @"Rectum\(OAR\)", MatchMode = StructureMatchMode.Regex, TargetAlias = "Rectum_Risk" },
                new StructureMappingRule { Pattern = "Lung_R[Upper]", MatchMode = StructureMatchMode.Exact, TargetAlias = "Lung_Right_Upper" },
                new StructureMappingRule { Pattern = @"Lung_R\[Upper\]", MatchMode = StructureMatchMode.Regex, TargetAlias = "Lung_RU_Regex" }
            };

            // 1. Exact match: "CTV-Prostate+SV" -> Priority 1 (Exact) maps to "CTV_High"
            var res1 = StructureMappingService.ResolveMapping("CTV-Prostate+SV", rules);
            Assert.IsTrue(res1.IsSelected);
            Assert.AreEqual("CTV_High", res1.TargetAlias);

            // 2. Contains (+): "Node+SV" -> Matches "+SV" to "SV_Boost"
            var res2 = StructureMappingService.ResolveMapping("Node+SV", rules);
            Assert.IsTrue(res2.IsSelected);
            Assert.AreEqual("SV_Boost", res2.TargetAlias);

            // 3. Regex (#): "PTV_70Gy#1" -> Matches "PTV_.*#1" to "PTV_Fraction1"
            var res3 = StructureMappingService.ResolveMapping("PTV_70Gy#1", rules);
            Assert.IsTrue(res3.IsSelected);
            Assert.AreEqual("PTV_Fraction1", res3.TargetAlias);

            // 4. Parentheses regex escaping & priority: "Rectum(OAR)"
            // Priority ordering: Exact(1) -> Contains(2) -> Regex(3)
            // Therefore Contains "(OAR)" takes precedence over Regex, resolving to "OAR_Generic"
            var res4 = StructureMappingService.ResolveMapping("Rectum(OAR)", rules);
            Assert.IsTrue(res4.IsSelected);
            Assert.AreEqual("OAR_Generic", res4.TargetAlias);

            // 5. Square brackets Exact match: "Lung_R[Upper]" -> Exact maps to "Lung_Right_Upper"
            var res5 = StructureMappingService.ResolveMapping("Lung_R[Upper]", rules);
            Assert.IsTrue(res5.IsSelected);
            Assert.AreEqual("Lung_Right_Upper", res5.TargetAlias);
        }

        [TestMethod]
        [Description("Verifies integrity of UniqueKey formatting (pipe-delimited) and patient ID extraction for MatchedPlanItem from Plan Search")]
        public void TargetPlanKey_PipeDelimiter_ShouldExtractPatientIdAndMatchPlan()
        {
            var planItem = new MatchedPlanItem
            {
                PatientId = "12345",
                CourseId = "C1",
                PlanId = "Prostate_VMAT",
                IsSelected = true
            };

            // UniqueKey format (pipe delimiter)
            Assert.AreEqual("12345|C1|Prostate_VMAT", planItem.UniqueKey);

            // PatientId extraction from pipe delimiter
            var patientId = planItem.UniqueKey.Split('|')[0];
            Assert.AreEqual("12345", patientId);

            // Match checking against target keys
            var targetKeys = new HashSet<string> { planItem.UniqueKey };
            string runtimePlanKey = $"12345|C1|Prostate_VMAT";
            Assert.IsTrue(targetKeys.Contains(runtimePlanKey));
        }

        [TestMethod]
        [Description("Verifies that RefreshPreview computes and updates MatchedCount on each rule based on discovered structures")]
        public void RefreshPreview_ShouldComputeMatchedCountsOnRules()
        {
            var rule1 = new StructureMappingRule { Pattern = "PTV_60", MatchMode = StructureMatchMode.Exact, TargetAlias = "PTV_High" };
            var rule2 = new StructureMappingRule { Pattern = "PTV.*", MatchMode = StructureMatchMode.Regex, TargetAlias = "PTV_Other" };
            var rule3 = new StructureMappingRule { Pattern = "SpinalCord", MatchMode = StructureMatchMode.Exact, TargetAlias = "Cord" };
            var rules = new List<StructureMappingRule> { rule1, rule2, rule3 };

            var discovered = new List<DiscoveredStructureItem>
            {
                new DiscoveredStructureItem { RawStructureId = "PTV_60" },
                new DiscoveredStructureItem { RawStructureId = "PTV_50" },
                new DiscoveredStructureItem { RawStructureId = "PTV_40" },
                new DiscoveredStructureItem { RawStructureId = "Bladder" }
            };

            // Act
            StructureMappingService.RefreshPreview(discovered, rules);

            // Assert
            Assert.AreEqual(1, rule1.MatchedCount); // Matched PTV_60
            Assert.AreEqual(2, rule2.MatchedCount); // Matched PTV_50, PTV_40
            Assert.AreEqual(0, rule3.MatchedCount); // Did not match any
        }

        [TestMethod]
        [Description("Verifies that MainViewModel automatically updates preview in real-time when rule properties are modified")]
        public void MainViewModel_RulePropertyChange_ShouldAutomaticallyRefreshPreview()
        {
            var vm = new EclipseDataMiner.ViewModels.MainViewModel();

            // Populate discovered structures
            vm.DiscoveredStructures.Add(new DiscoveredStructureItem { RawStructureId = "Rectum" });
            vm.DiscoveredStructures.Add(new DiscoveredStructureItem { RawStructureId = "Bladder" });

            // Initially unmapped
            vm.RefreshDiscoveredPreview();
            Assert.AreEqual("Unmapped (Raw)", vm.DiscoveredStructures[0].MatchStatus);

            // Add rule
            var rule = new StructureMappingRule
            {
                Pattern = "Rectum",
                MatchMode = StructureMatchMode.Exact,
                TargetAlias = "Rectum_OAR",
                IsSelected = true
            };
            vm.MappingRules.Add(rule);

            // Preview automatically updated via CollectionChanged
            Assert.AreEqual("Mapped (Exact)", vm.DiscoveredStructures[0].MatchStatus);
            Assert.AreEqual("Rectum_OAR", vm.DiscoveredStructures[0].ResolvedAlias);

            // Act: edit TargetAlias on rule directly
            rule.TargetAlias = "Rectum_Modified";

            // Assert: preview automatically updated via PropertyChanged
            Assert.AreEqual("Rectum_Modified", vm.DiscoveredStructures[0].ResolvedAlias);

            // Act: uncheck IsSelected (opt-out)
            rule.IsSelected = false;

            // Assert: status becomes Excluded
            Assert.AreEqual("Excluded (Exact)", vm.DiscoveredStructures[0].MatchStatus);
            Assert.IsFalse(vm.DiscoveredStructures[0].IsExtracted);
        }
    }
}


