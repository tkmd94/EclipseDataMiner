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
        [Description("完全一致(Exact)および部分一致(Contains)によるエイリアス解決を検証")]
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

            // ルールに一致しない輪郭は元のIDのまま
            var res3 = StructureMappingService.ResolveMapping("Bladder", rules);
            Assert.IsTrue(res3.IsSelected);
            Assert.AreEqual("Bladder", res3.TargetAlias);
        }

        [TestMethod]
        [Description("正規表現(Regex)によるエイリアス解決を検証 (例: ^PTV.* -> PTV)")]
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
            Assert.AreEqual("CTV-60", res3.TargetAlias); // 不一致
        }

        [TestMethod]
        [Description("オプトアウト（IsSelected = false）設定時に抽出除外判定されることを検証")]
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
        [Description("マッピングルールの JSON 保存と読み込みによる双方向永続化を検証")]
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
        [Description("作成した前立腺サンプルマッピングJSON (StructureMapping_Prostate.json) の正常読込と解決動作を検証")]
        public void LoadSampleProstateMappingRules_ShouldResolveCorrectly()
        {
            // Arrange
            string baseDir = Path.GetDirectoryName(typeof(StructureMappingTests).Assembly.Location);
            string samplePath = Path.Combine(baseDir, "Templates", "StructureMapping_Prostate.json");
            Assert.IsTrue(File.Exists(samplePath), $"サンプルファイルが見つかりません: {samplePath}");

            // Act
            var rules = StructureMappingService.LoadRulesFromFile(samplePath);

            // Assert
            Assert.IsTrue(rules.Count >= 10, "ルール数が10以上であること");

            // PTV のエイリアス統合テスト
            var ptvMatch1 = StructureMappingService.ResolveMapping("PTV_60Gy", rules);
            Assert.IsTrue(ptvMatch1.IsSelected);
            Assert.AreEqual("PTV", ptvMatch1.TargetAlias);

            var ptvMatch2 = StructureMappingService.ResolveMapping("ptv-prost", rules);
            Assert.IsTrue(ptvMatch2.IsSelected);
            Assert.AreEqual("PTV", ptvMatch2.TargetAlias);

            // 直腸・膀胱のテスト
            var rectumMatch = StructureMappingService.ResolveMapping("Rectum_Wall", rules);
            Assert.IsTrue(rectumMatch.IsSelected);
            Assert.AreEqual("Rectum", rectumMatch.TargetAlias);

            var bladderMatch = StructureMappingService.ResolveMapping("Bladder", rules);
            Assert.IsTrue(bladderMatch.IsSelected);
            Assert.AreEqual("Bladder", bladderMatch.TargetAlias);

            // オプトアウト（除外）輪郭のテスト
            var bodyMatch = StructureMappingService.ResolveMapping("BODY", rules);
            Assert.IsFalse(bodyMatch.IsSelected);

            var couchMatch = StructureMappingService.ResolveMapping("CouchInterior", rules);
            Assert.IsFalse(couchMatch.IsSelected);
        }

        [TestMethod]
        [Description("RefreshPreview によりスキャン結果が最新ルールで即座に再評価されることを検証")]
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
            // PTV_60: 正規表現でマッチして統合
            Assert.IsTrue(discovered[0].IsExtracted);
            Assert.AreEqual("PTV_Integrated", discovered[0].ResolvedAlias);
            Assert.IsTrue(discovered[0].MatchStatus.Contains("Mapped (Regex"));

            // Rectum: ルールなし -> そのまま
            Assert.IsTrue(discovered[1].IsExtracted);
            Assert.AreEqual("Rectum", discovered[1].ResolvedAlias);
            Assert.AreEqual("Unmapped (Raw)", discovered[1].MatchStatus);

            // BODY: 除外ルールで除外
            Assert.IsFalse(discovered[2].IsExtracted);
            Assert.IsTrue(discovered[2].MatchStatus.Contains("Excluded (Exact)"));

            // UnmappedOAR: 未マッピング
            Assert.IsTrue(discovered[3].IsExtracted);
            Assert.AreEqual("UnmappedOAR", discovered[3].ResolvedAlias);
            Assert.AreEqual("Unmapped (Raw)", discovered[3].MatchStatus);
        }

        [TestMethod]
        [Description("ViewModel の 2ペイン操作において、ルール追加・生データ追加が正常に機能することを検証")]
        public void MainViewModel_RuleManagement_ShouldFunctionCorrectly()
        {
            // Arrange
            var vm = new EclipseDataMiner.ViewModels.MainViewModel();
            Assert.AreEqual(0, vm.MappingRules.Count);
            Assert.AreEqual(0, vm.DiscoveredStructures.Count);

            // Act 1: ルールを追加
            vm.AddRuleCommand.Execute(null);
            Assert.AreEqual(1, vm.MappingRules.Count);
            Assert.IsNotNull(vm.SelectedRule);

            // Act 2: スキャン結果をシミュレートして登録
            var item = new DiscoveredStructureItem { RawStructureId = "NewDiscoveredOrgan", HitCount = 15 };
            vm.DiscoveredStructures.Add(item);
            vm.SelectedDiscoveredItem = item;

            // Act 3: スキャン結果からルールへ追加
            vm.AddDiscoveredToRulesCommand.Execute(null);

            // Assert: ルールが 2 件になり、追加されたルールが NewDiscoveredOrgan であること
            Assert.AreEqual(2, vm.MappingRules.Count);
            Assert.AreEqual("NewDiscoveredOrgan", vm.MappingRules[1].Pattern);

            // プレビューが更新されて Mapped になっていること
            Assert.AreEqual("NewDiscoveredOrgan", item.ResolvedAlias);
            Assert.IsTrue(item.MatchStatus.Contains("Mapped (Exact"));
        }

        [TestMethod]
        [Description("特異度優先 (Exact > Contains > Regex) により、登録順序に関係なく完全一致が優先されることを検証")]
        public void ResolveMapping_ShouldPrioritizeExactOverRegex()
        {
            // Arrange: Regex ルールを先に登録し、Exact ルールを後から登録
            var rules = new List<StructureMappingRule>
            {
                new StructureMappingRule { Pattern = "PTV.*", MatchMode = StructureMatchMode.Regex, TargetAlias = "PTV_Generic", IsSelected = true },
                new StructureMappingRule { Pattern = "PTV_60", MatchMode = StructureMatchMode.Exact, TargetAlias = "PTV_HighDose", IsSelected = true }
            };

            // Act: PTV_60 を評価
            var res = StructureMappingService.ResolveMapping("PTV_60", rules);

            // Assert: 後から登録された Exact (PTV_HighDose) が優先されること
            Assert.AreEqual("PTV_HighDose", res.TargetAlias);

            // Act: PTV_54 を評価 (Exact はなく Regex のみにマッチ)
            var resGeneric = StructureMappingService.ResolveMapping("PTV_54", rules);

            // Assert: Regex (PTV_Generic) が適用されること
            Assert.AreEqual("PTV_Generic", resGeneric.TargetAlias);
        }

        [TestMethod]
        [Description("同一マッチモード内では MoveUp/Down による順序変更で優先度が切り替わることを検証")]
        public void MoveUpAndDown_ShouldReorderRulesAndAffectSamePriorityMatching()
        {
            // Arrange
            var vm = new EclipseDataMiner.ViewModels.MainViewModel();
            var rule1 = new StructureMappingRule { Pattern = "Rectum", MatchMode = StructureMatchMode.Contains, TargetAlias = "Alias1" };
            var rule2 = new StructureMappingRule { Pattern = "Rect", MatchMode = StructureMatchMode.Contains, TargetAlias = "Alias2" };
            vm.MappingRules.Add(rule1);
            vm.MappingRules.Add(rule2);

            // 初期状態: rule1 が先頭 -> Alias1 が適用
            var res1 = StructureMappingService.ResolveMapping("Rectum_Wall", vm.MappingRules);
            Assert.AreEqual("Alias1", res1.TargetAlias);

            // Act: rule2 を選択して上に移動 (MoveUp)
            vm.SelectedRule = rule2;
            Assert.IsTrue(vm.MoveUpRuleCommand.CanExecute(null));
            vm.MoveUpRuleCommand.Execute(null);

            // Assert: rule2 が先頭になったこと
            Assert.AreEqual("Rect", vm.MappingRules[0].Pattern);

            // Act: 再度評価 -> rule2 (Alias2) が優先されること
            var res2 = StructureMappingService.ResolveMapping("Rectum_Wall", vm.MappingRules);
            Assert.AreEqual("Alias2", res2.TargetAlias);
        }

        [TestMethod]
        [Description("同一 Target Alias に複数輪郭がマッピングされた場合に [統合: N件] と注記されることを検証")]
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

            // Assert: 2件とも PTV に集約され、ステータスに [統合: 2件] が付与されること
            Assert.AreEqual("PTV", discovered[0].ResolvedAlias);
            Assert.AreEqual("PTV", discovered[1].ResolvedAlias);
            Assert.IsTrue(discovered[0].MatchStatus.Contains("[統合: 2件]"));
            Assert.IsTrue(discovered[1].MatchStatus.Contains("[統合: 2件]"));
        }

        [TestMethod]
        [Description("ViewModel における DQP 項目追加・削除およびログ消去・コピーが正常に動作することを検証")]
        public void MainViewModel_DqpManagement_And_LogCommands_ShouldFunctionCorrectly()
        {
            // Arrange
            var vm = new EclipseDataMiner.ViewModels.MainViewModel();
            int initialDqpCount = vm.DQPList.Count;

            // Act 1: DQP 追加
            vm.AddDqpCommand.Execute(null);
            Assert.AreEqual(initialDqpCount + 1, vm.DQPList.Count);
            var addedDqp = vm.SelectedDqp;
            Assert.IsNotNull(addedDqp);
            Assert.AreEqual(EclipseDataMiner.Models.DQPtype.Dose, addedDqp.DQPtype);

            // Act 2: DQP 削除
            vm.DeleteDqpCommand.Execute(null);
            Assert.AreEqual(initialDqpCount, vm.DQPList.Count);

            // Act 3: ログ操作
            vm.LogText = "2026-09-25 [INFO] Test message";
            Assert.IsFalse(string.IsNullOrEmpty(vm.LogText));

            vm.ClearLogCommand.Execute(null);
            Assert.AreEqual(string.Empty, vm.LogText);
        }

        [TestMethod]
        [Description("不正な正規表現パターン（構文エラー）が設定された場合でもクラッシュせず安全に false を返すことを検証")]
        public void IsMatch_WhenInvalidRegex_ShouldNotThrowAndReturnFalse()
        {
            // Arrange: 構文エラーとなる正規表現パターン
            var ruleUnclosedBracket = new StructureMappingRule { Pattern = "PTV_[0-9(", MatchMode = StructureMatchMode.Regex };
            var ruleInvalidModifier = new StructureMappingRule { Pattern = "(?<invalid", MatchMode = StructureMatchMode.Regex };
            var ruleDanglingStar = new StructureMappingRule { Pattern = "*PTV", MatchMode = StructureMatchMode.Regex };

            // Act & Assert: 例外をスローせず安全に false を返すこと
            Assert.IsFalse(ruleUnclosedBracket.IsMatch("PTV_60"));
            Assert.IsFalse(ruleInvalidModifier.IsMatch("PTV_60"));
            Assert.IsFalse(ruleDanglingStar.IsMatch("PTV_60"));
        }

        [TestMethod]
        [Description("日本語や特殊記号（+, /, -）を含む輪郭名に対するマッピング判定が正確に動作することを検証")]
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
        [Description("同一入力に対して Exact, Contains, Regex すべてが合致する場合に厳格に特異度順序が守られることを検証")]
        public void FindBestMatchingRule_WhenMultipleModesMatch_ShouldStrictlyFollowPriority()
        {
            // Arrange: 登録順序を Regex, Contains, Exact の逆順で登録
            var rules = new List<StructureMappingRule>
            {
                new StructureMappingRule { Pattern = ".*", MatchMode = StructureMatchMode.Regex, TargetAlias = "Regex_CatchAll", IsSelected = true },
                new StructureMappingRule { Pattern = "Bladder", MatchMode = StructureMatchMode.Contains, TargetAlias = "Contains_Bladder", IsSelected = true },
                new StructureMappingRule { Pattern = "Bladder", MatchMode = StructureMatchMode.Exact, TargetAlias = "Exact_Bladder", IsSelected = true }
            };

            // Act: "Bladder" で検索
            var best = StructureMappingService.FindBestMatchingRule("Bladder", rules);

            // Assert: 最上位の Regex ではなく、最も特異度の高い Exact が選択されること
            Assert.IsNotNull(best);
            Assert.AreEqual(StructureMatchMode.Exact, best.MatchMode);
            Assert.AreEqual("Exact_Bladder", best.TargetAlias);

            // Act 2: "Bladder_Wall" で検索 (Exact は合致しない)
            var bestSub = StructureMappingService.FindBestMatchingRule("Bladder_Wall", rules);

            // Assert 2: Regex ではなく Contains が選択されること
            Assert.IsNotNull(bestSub);
            Assert.AreEqual(StructureMatchMode.Contains, bestSub.MatchMode);
            Assert.AreEqual("Contains_Bladder", bestSub.TargetAlias);
        }

        [TestMethod]
        [Description("有効な正規表現パターンの場合、IsRegexError が false になることを検証")]
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
        [Description("構文不正な正規表現（例: 閉じかっこ不足）の場合、IsRegexError が true かつメッセージが設定されることを検証")]
        public void ValidateRegex_WhenInvalidPattern_ShouldSetIsRegexErrorTrueAndMessage()
        {
            var rule = new StructureMappingRule
            {
                MatchMode = StructureMatchMode.Regex,
                Pattern = "(PTV" // 不正: 閉じかっこがない
            };

            Assert.IsTrue(rule.IsRegexError);
            Assert.IsFalse(string.IsNullOrEmpty(rule.RegexErrorMessage));
        }

        [TestMethod]
        [Description("Regex モードから Exact または Contains に変更した際、エラーが自動クリアされることを検証")]
        public void ValidateRegex_WhenSwitchingToNonRegex_ShouldClearError()
        {
            var rule = new StructureMappingRule
            {
                MatchMode = StructureMatchMode.Regex,
                Pattern = "(PTV"
            };
            Assert.IsTrue(rule.IsRegexError);

            // Act: Exact に変更
            rule.MatchMode = StructureMatchMode.Exact;

            // Assert: エラーが解除される
            Assert.IsFalse(rule.IsRegexError);
            Assert.IsTrue(string.IsNullOrEmpty(rule.RegexErrorMessage));
        }

        [TestMethod]
        [Description("ViewModel の正規表現スニペット一覧に臨床頻出プリセットが含まれていることを検証")]
        public void RegexSnippets_InViewModel_ShouldContainClinicalPresets()
        {
            var vm = new EclipseDataMiner.ViewModels.MainViewModel();

            Assert.IsTrue(vm.RegexSnippets.Count >= 7);
            Assert.IsTrue(vm.RegexSnippets.Any(s => s.Pattern == "^PTV.*"));
            Assert.IsTrue(vm.RegexSnippets.Any(s => s.Pattern == ".*[_-](Rt|Lt|R|L)$"));
            Assert.IsTrue(vm.RegexSnippets.Any(s => s.Pattern == "(Bladder|Rectum)"));
        }

        [TestMethod]
        [Description("InsertRegexSnippetCommand 実行時に選択中ルールのパターンが設定され Regex モードへ自動変更されることを検証")]
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

            // Act: スニペット挿入
            vm.ExecuteInsertRegexSnippet("^PTV.*");

            // Assert: パターンが更新され、Regex モードになりエラーなし
            Assert.AreEqual("^PTV.*", vm.SelectedRule.Pattern);
            Assert.AreEqual(StructureMatchMode.Regex, vm.SelectedRule.MatchMode);
            Assert.IsFalse(vm.SelectedRule.IsRegexError);
        }

        [TestMethod]
        [Description("臨床で頻出する特殊文字（+, #, ( ), [ ], _）を含む輪郭名に対するマッピング解決を網羅的に検証")]
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

            // 1. 完全一致: "CTV-Prostate+SV" -> Priority 1 (Exact) で "CTV_High"
            var res1 = StructureMappingService.ResolveMapping("CTV-Prostate+SV", rules);
            Assert.IsTrue(res1.IsSelected);
            Assert.AreEqual("CTV_High", res1.TargetAlias);

            // 2. 部分一致 (+): "Node+SV" -> "+SV" にマッチして "SV_Boost"
            var res2 = StructureMappingService.ResolveMapping("Node+SV", rules);
            Assert.IsTrue(res2.IsSelected);
            Assert.AreEqual("SV_Boost", res2.TargetAlias);

            // 3. 正規表現 (#): "PTV_70Gy#1" -> "PTV_.*#1" にマッチして "PTV_Fraction1"
            var res3 = StructureMappingService.ResolveMapping("PTV_70Gy#1", rules);
            Assert.IsTrue(res3.IsSelected);
            Assert.AreEqual("PTV_Fraction1", res3.TargetAlias);

            // 4. 丸括弧の正規表現エスケープと優先度: "Rectum(OAR)"
            // Priority順序: Exact(1) -> Contains(2) -> Regex(3)
            // したがって Contains "(OAR)" が Regex より優先されて "OAR_Generic" になる
            var res4 = StructureMappingService.ResolveMapping("Rectum(OAR)", rules);
            Assert.IsTrue(res4.IsSelected);
            Assert.AreEqual("OAR_Generic", res4.TargetAlias);

            // 5. 角括弧の完全一致: "Lung_R[Upper]" -> Exact で "Lung_Right_Upper"
            var res5 = StructureMappingService.ResolveMapping("Lung_R[Upper]", rules);
            Assert.IsTrue(res5.IsSelected);
            Assert.AreEqual("Lung_Right_Upper", res5.TargetAlias);
        }

        [TestMethod]
        [Description("Plan Search から渡される MatchedPlanItem の UniqueKey と患者ID抽出（パイプ区切り）の整合性を検証")]
        public void TargetPlanKey_PipeDelimiter_ShouldExtractPatientIdAndMatchPlan()
        {
            var planItem = new MatchedPlanItem
            {
                PatientId = "12345",
                CourseId = "C1",
                PlanId = "Prostate_VMAT",
                IsSelected = true
            };

            // UniqueKey の書式（パイプ区切り）
            Assert.AreEqual("12345|C1|Prostate_VMAT", planItem.UniqueKey);

            // パイプ区切りからの患者ID抽出
            var patientId = planItem.UniqueKey.Split('|')[0];
            Assert.AreEqual("12345", patientId);

            // ターゲットキーとの一致判定
            var targetKeys = new HashSet<string> { planItem.UniqueKey };
            string runtimePlanKey = $"12345|C1|Prostate_VMAT";
            Assert.IsTrue(targetKeys.Contains(runtimePlanKey));
        }
    }
}


