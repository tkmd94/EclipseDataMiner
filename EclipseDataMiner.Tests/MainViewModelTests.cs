using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EclipseDataMiner.ViewModels;

namespace EclipseDataMiner.Tests
{
    [TestClass]
    public class MainViewModelTests
    {
        [TestMethod]
        [Description("ViewModel の実行状態 (IsRunning) に応じて Run / PreScan / Cancel コマンドの活性・非活性 (CanExecute) が連動することを検証")]
        public void CommandCanExecute_WhenRunningStateChanges_ShouldToggleProperly()
        {
            // Arrange
            var vm = new MainViewModel();

            // 初期状態: 待機中 (IsRunning == false)
            Assert.IsFalse(vm.IsRunning);
            Assert.IsTrue(vm.IsNotRunning);
            Assert.IsTrue(vm.RunPreScanCommand.CanExecute(null));
            Assert.IsTrue(vm.RunExtractionCommand.CanExecute(null));
            Assert.IsFalse(vm.CancelCommand.CanExecute(null));

            // Act 1: 実行中に変更
            vm.IsRunning = true;

            // Assert 1: 多重実行防止により Run / PreScan は無効化、Cancel が有効化
            Assert.IsFalse(vm.RunPreScanCommand.CanExecute(null));
            Assert.IsFalse(vm.RunExtractionCommand.CanExecute(null));
            Assert.IsTrue(vm.CancelCommand.CanExecute(null));

            // Act 2: 完了・停止状態に復帰
            vm.IsRunning = false;

            // Assert 2: 再度 Run / PreScan が実行可能になる
            Assert.IsTrue(vm.RunPreScanCommand.CanExecute(null));
            Assert.IsTrue(vm.RunExtractionCommand.CanExecute(null));
            Assert.IsFalse(vm.CancelCommand.CanExecute(null));
        }

        [TestMethod]
        [Description("キャンセル要求中 (IsCancelling == true) の場合、二重クリック防止のため CancelCommand が無効化されることを検証")]
        public void CommandCanExecute_WhenCancelling_CancelCommandShouldBeDisabledToPreventDoubleExecution()
        {
            // Arrange
            var vm = new MainViewModel();
            vm.IsRunning = true;
            Assert.IsTrue(vm.CancelCommand.CanExecute(null));

            // Act: キャンセル要求状態に遷移
            vm.IsCancelling = true;

            // Assert: CancelCommand は無効化される（二重キャンセル要求の防止）
            Assert.IsFalse(vm.CancelCommand.CanExecute(null));

            // 完了復帰
            vm.IsRunning = false;
            vm.IsCancelling = false;
            Assert.IsFalse(vm.CancelCommand.CanExecute(null));
            Assert.IsTrue(vm.RunExtractionCommand.CanExecute(null));
        }

        [TestMethod]
        [Description("初期化時にデフォルトの出力先パスが設定され、拡張子が .csv であることを検証")]
        public void OutputFilePath_InitialValue_ShouldBeValidCsvPath()
        {
            // Arrange & Act
            var vm = new MainViewModel();

            // Assert
            Assert.IsFalse(string.IsNullOrWhiteSpace(vm.OutputFilePath));
            Assert.IsTrue(vm.OutputFilePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(vm.OutputFilePath.Contains("DataMiningOutput."));
        }

        [TestMethod]
        [Description("進捗プロパティおよびステータステキストのバインディング更新を検証")]
        public void ProgressProperties_ShouldUpdateCorrectly()
        {
            // Arrange
            var vm = new MainViewModel();

            // Act
            vm.ProgressPercentage = 75;
            vm.ProgressText = "Extracting patient 15/20";

            // Assert
            Assert.AreEqual(75, vm.ProgressPercentage);
            Assert.AreEqual("Extracting patient 15/20", vm.ProgressText);
        }

        [TestMethod]
        [Description("プレビュー行のダブルクリック (AddDiscoveredItemToRules) により、該当輪郭がルール定義に Exact ルールとして追加されプレビューが更新されることを検証")]
        public void AddDiscoveredItemToRules_WhenItemDoubleClicked_ShouldAddRuleAndRefreshPreview()
        {
            // Arrange
            var vm = new MainViewModel();
            var item = new EclipseDataMiner.Models.DiscoveredStructureItem
            {
                RawStructureId = "Parotid_L",
                HitCount = 12
            };
            vm.DiscoveredStructures.Add(item);

            // Act: 該当アイテムをダブルクリックしてルール追加（コマンドまたはメソッド実行）
            vm.AddDiscoveredItemCommand.Execute(item);

            // Assert: ルール一覧に Parotid_L が Exact ルールとして追加され、選択中ルールになっていること
            Assert.AreEqual(1, vm.MappingRules.Count);
            Assert.AreEqual("Parotid_L", vm.SelectedRule.Pattern);
            Assert.AreEqual(EclipseDataMiner.Models.StructureMatchMode.Exact, vm.SelectedRule.MatchMode);
            Assert.AreEqual("Parotid_L", vm.SelectedRule.TargetAlias);

            // プレビューが更新され Mapped になっていること
            Assert.AreEqual("Parotid_L", item.ResolvedAlias);
            Assert.IsTrue(item.MatchStatus.Contains("Mapped (Exact"));
        }

        [TestMethod]
        [Description("既に同名のルールが存在する項目をダブルクリックした場合、重複追加されずに既存ルールが選択されることを検証")]
        public void AddDiscoveredItemToRules_WhenRuleAlreadyExists_ShouldSelectExistingRuleWithoutDuplication()
        {
            // Arrange
            var vm = new MainViewModel();
            var item = new EclipseDataMiner.Models.DiscoveredStructureItem
            {
                RawStructureId = "Bladder",
                HitCount = 20
            };
            vm.DiscoveredStructures.Add(item);

            // 1回目のダブルクリック -> ルール追加 (1件)
            vm.AddDiscoveredItemToRules(item);
            Assert.AreEqual(1, vm.MappingRules.Count);

            // Act: 2回目のダブルクリック
            vm.AddDiscoveredItemToRules(item);

            // Assert: ルール件数は 1 件のまま（重複防止）、SelectedRule が維持される
            Assert.AreEqual(1, vm.MappingRules.Count);
            Assert.IsNotNull(vm.SelectedRule);
            Assert.AreEqual("Bladder", vm.SelectedRule.Pattern);
        }

        [TestMethod]
        [Description("プレビューのテキストフィルタにより、RawStructureId または ResolvedAlias に部分一致する項目のみ絞り込まれることを検証")]
        public void DiscoveredFilter_ByText_ShouldFilterViewCorrectly()
        {
            // Arrange
            var vm = new MainViewModel();
            vm.DiscoveredStructures.Add(new EclipseDataMiner.Models.DiscoveredStructureItem { RawStructureId = "PTV_60", ResolvedAlias = "PTV_Integrated", MatchStatus = "Mapped (Exact)" });
            vm.DiscoveredStructures.Add(new EclipseDataMiner.Models.DiscoveredStructureItem { RawStructureId = "Rectum", ResolvedAlias = "Rectum", MatchStatus = "Unmapped (Raw)" });
            vm.DiscoveredStructures.Add(new EclipseDataMiner.Models.DiscoveredStructureItem { RawStructureId = "Bladder", ResolvedAlias = "Bladder", MatchStatus = "Unmapped (Raw)" });

            Assert.AreEqual(3, vm.FilteredDiscoveredCount);

            // Act 1: "PTV" でフィルタ
            vm.DiscoveredFilterText = "ptv";
            Assert.AreEqual(1, vm.FilteredDiscoveredCount);

            // Act 2: "Integrated"（ResolvedAlias の部分一致）でフィルタ
            vm.DiscoveredFilterText = "Integrated";
            Assert.AreEqual(1, vm.FilteredDiscoveredCount);

            // Act 3: 該当なし
            vm.DiscoveredFilterText = "NonExistent";
            Assert.AreEqual(0, vm.FilteredDiscoveredCount);

            // Act 4: クリア
            vm.ClearDiscoveredFilterCommand.Execute(null);
            Assert.AreEqual(string.Empty, vm.DiscoveredFilterText);
            Assert.AreEqual(3, vm.FilteredDiscoveredCount);
        }

        [TestMethod]
        [Description("プレビューのステータスフィルタ（Unmapped Only / Mapped Only）による絞り込みを検証")]
        public void DiscoveredFilter_ByStatus_ShouldFilterViewCorrectly()
        {
            // Arrange
            var vm = new MainViewModel();
            vm.DiscoveredStructures.Add(new EclipseDataMiner.Models.DiscoveredStructureItem { RawStructureId = "PTV_60", MatchStatus = "Mapped (Exact)" });
            vm.DiscoveredStructures.Add(new EclipseDataMiner.Models.DiscoveredStructureItem { RawStructureId = "Rectum", MatchStatus = "Unmapped (Raw)" });
            vm.DiscoveredStructures.Add(new EclipseDataMiner.Models.DiscoveredStructureItem { RawStructureId = "BODY", MatchStatus = "Excluded (Exact)" });

            Assert.AreEqual(3, vm.FilteredDiscoveredCount);

            // Act 1: Unmapped Only
            vm.DiscoveredFilterStatus = "Unmapped Only";
            Assert.AreEqual(1, vm.FilteredDiscoveredCount);

            // Act 2: Mapped Only
            vm.DiscoveredFilterStatus = "Mapped Only";
            Assert.AreEqual(1, vm.FilteredDiscoveredCount);

            // Act 3: Excluded Only
            vm.DiscoveredFilterStatus = "Excluded Only";
            Assert.AreEqual(1, vm.FilteredDiscoveredCount);

            // Act 4: All
            vm.DiscoveredFilterStatus = "All";
            Assert.AreEqual(3, vm.FilteredDiscoveredCount);
        }

        [TestMethod]
        [Description("ルール削除時に削除行の1つ下の行に自動選択が移動し、連続してDelete実行できることを検証")]
        public void DeleteRuleCommand_ShouldSelectNextRow_AndAllowContinuousDeletion()
        {
            // Arrange
            var vm = new MainViewModel();
            var r1 = new EclipseDataMiner.Models.StructureMappingRule { Pattern = "Rule1", TargetAlias = "Alias1" };
            var r2 = new EclipseDataMiner.Models.StructureMappingRule { Pattern = "Rule2", TargetAlias = "Alias2" };
            var r3 = new EclipseDataMiner.Models.StructureMappingRule { Pattern = "Rule3", TargetAlias = "Alias3" };
            vm.MappingRules.Add(r1);
            vm.MappingRules.Add(r2);
            vm.MappingRules.Add(r3);

            // Act 1: 先頭の r1 を選択して Delete 実行
            vm.SelectedRule = r1;
            Assert.IsTrue(vm.DeleteRuleCommand.CanExecute(null));
            vm.DeleteRuleCommand.Execute(null);

            // Assert 1: r1 が削除され、元の1つ下だった r2 が自動選択され、CanExecute が true のまま
            Assert.AreEqual(2, vm.MappingRules.Count);
            Assert.AreSame(r2, vm.SelectedRule);
            Assert.IsTrue(vm.DeleteRuleCommand.CanExecute(null));

            // Act 2: そのまま連続して Delete 実行 (r2 を削除)
            vm.DeleteRuleCommand.Execute(null);

            // Assert 2: r2 が削除され、元の1つ下だった r3 が自動選択され、CanExecute が true のまま
            Assert.AreEqual(1, vm.MappingRules.Count);
            Assert.AreSame(r3, vm.SelectedRule);
            Assert.IsTrue(vm.DeleteRuleCommand.CanExecute(null));

            // Act 3: 最後の1件を削除
            vm.DeleteRuleCommand.Execute(null);

            // Assert 3: 全件削除され、SelectedRule は null、CanExecute は false
            Assert.AreEqual(0, vm.MappingRules.Count);
            Assert.IsNull(vm.SelectedRule);
            Assert.IsFalse(vm.DeleteRuleCommand.CanExecute(null));
        }

        [TestMethod]
        [Description("末尾のルール削除時に、新しい末尾行（1つ上の行）が自動選択されることを検証")]
        public void DeleteRuleCommand_WhenDeletingLastRow_ShouldSelectNewLastRow()
        {
            // Arrange
            var vm = new MainViewModel();
            var r1 = new EclipseDataMiner.Models.StructureMappingRule { Pattern = "Rule1", TargetAlias = "Alias1" };
            var r2 = new EclipseDataMiner.Models.StructureMappingRule { Pattern = "Rule2", TargetAlias = "Alias2" };
            vm.MappingRules.Add(r1);
            vm.MappingRules.Add(r2);

            // Act: 末尾の r2 を削除
            vm.SelectedRule = r2;
            vm.DeleteRuleCommand.Execute(null);

            // Assert: 新しい末尾である r1 が選択され、CanExecute が true のまま
            Assert.AreEqual(1, vm.MappingRules.Count);
            Assert.AreSame(r1, vm.SelectedRule);
            Assert.IsTrue(vm.DeleteRuleCommand.CanExecute(null));
        }

        [TestMethod]
        [Description("DQP削除時に削除行の1つ下の行に自動選択が移動し、連続してDelete実行できることを検証")]
        public void DeleteDqpCommand_ShouldSelectNextRow_AndAllowContinuousDeletion()
        {
            // Arrange
            var vm = new MainViewModel();
            vm.DQPList.Clear();
            var d1 = new EclipseDataMiner.Models.DQP { structureName = "PTV", DQPvalue = 95.0 };
            var d2 = new EclipseDataMiner.Models.DQP { structureName = "Rectum", DQPvalue = 70.0 };
            var d3 = new EclipseDataMiner.Models.DQP { structureName = "Bladder", DQPvalue = 50.0 };
            vm.DQPList.Add(d1);
            vm.DQPList.Add(d2);
            vm.DQPList.Add(d3);

            // Act 1: 真ん中の d2 を削除
            vm.SelectedDqp = d2;
            Assert.IsTrue(vm.DeleteDqpCommand.CanExecute(null));
            vm.DeleteDqpCommand.Execute(null);

            // Assert 1: d2 が削除され、1つ下だった d3 が自動選択される
            Assert.AreEqual(2, vm.DQPList.Count);
            Assert.AreSame(d3, vm.SelectedDqp);
            Assert.IsTrue(vm.DeleteDqpCommand.CanExecute(null));

            // Act 2: 末尾になった d3 を削除
            vm.DeleteDqpCommand.Execute(null);

            // Assert 2: 新しい末尾 d1 が自動選択される
            Assert.AreEqual(1, vm.DQPList.Count);
            Assert.AreSame(d1, vm.SelectedDqp);
            Assert.IsTrue(vm.DeleteDqpCommand.CanExecute(null));
        }

        [TestMethod]
        [Description("MainWindow のハードコード排除: WindowTitle がアセンブリの ProductVersion (InformationalVersion) から動的に取得されることを検証")]
        public void WindowTitle_ShouldDeriveDynamicallyFromProductVersion()
        {
            // Arrange
            var vm = new MainViewModel();

            // Act
            string productVer = MainViewModel.GetProductVersion();
            string title = vm.WindowTitle;

            // Assert: ProductVersion が 3.0.0 であり、タイトルに含まれていること
            Assert.AreEqual("3.0.0", productVer);
            Assert.IsTrue(title.Contains("v3.0.0"), $"Title '{title}' does not contain 'v3.0.0'");
            Assert.IsTrue(title.StartsWith("EclipseDataMiner"));
            Assert.IsTrue(title.Contains("High-Throughput Clinical ESAPI Data Mining Platform"));
        }

        [TestMethod]
        [Description("Plan Search で選択された計画数に応じて Tab 2 の事前スキャンスコープバッジ (PreScanScopeBadgeText) がリアルタイムに更新されることを検証")]
        public void PreScanScopeBadgeText_ShouldReflectMatchedPlansSelection()
        {
            // Arrange
            var vm = new MainViewModel();

            // 初期状態: 検索前
            Assert.AreEqual("🌐 Target: All Criteria Matching Plans", vm.PreScanScopeBadgeText);

            // Act 1: 検索結果が3件追加（初期状態はすべて IsSelected = true）
            var p1 = new EclipseDataMiner.Models.MatchedPlanItem { PatientId = "PT1", CourseId = "C1", PlanId = "Plan1", IsSelected = true };
            var p2 = new EclipseDataMiner.Models.MatchedPlanItem { PatientId = "PT1", CourseId = "C1", PlanId = "Plan2", IsSelected = true };
            var p3 = new EclipseDataMiner.Models.MatchedPlanItem { PatientId = "PT2", CourseId = "C1", PlanId = "Plan1", IsSelected = true };
            vm.MatchedPlans.Add(p1);
            vm.MatchedPlans.Add(p2);
            vm.MatchedPlans.Add(p3);
            vm.UpdateMatchedPlansSummary();

            // Assert 1: 3/3 選択表示
            Assert.AreEqual("🎯 Target: 3 / 3 Selected Plans", vm.PreScanScopeBadgeText);

            // Act 2: 1件チェック解除
            p2.IsSelected = false;
            vm.UpdateMatchedPlansSummary();

            // Assert 2: 2/3 選択表示
            Assert.AreEqual("🎯 Target: 2 / 3 Selected Plans", vm.PreScanScopeBadgeText);

            // Act 3: 全解除
            vm.ExecuteSelectAllPlans(false);

            // Assert 3: 0/3 選択表示
            Assert.AreEqual("🎯 Target: 0 / 3 Selected Plans", vm.PreScanScopeBadgeText);
        }
    }
}
