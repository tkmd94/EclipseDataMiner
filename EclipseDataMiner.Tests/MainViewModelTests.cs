using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EclipseDataMiner.ViewModels;

namespace EclipseDataMiner.Tests
{
    [TestClass]
    public class MainViewModelTests
    {
        [TestMethod]
        [Description("Verifies that Run / PreScan / Cancel commands toggle CanExecute in response to ViewModel running state (IsRunning)")]
        public void CommandCanExecute_WhenRunningStateChanges_ShouldToggleProperly()
        {
            // Arrange
            var vm = new MainViewModel();

            // Initial state: Idle (IsRunning == false)
            Assert.IsFalse(vm.IsRunning);
            Assert.IsTrue(vm.IsNotRunning);
            Assert.IsTrue(vm.RunPreScanCommand.CanExecute(null));
            Assert.IsTrue(vm.RunExtractionCommand.CanExecute(null));
            Assert.IsFalse(vm.CancelCommand.CanExecute(null));

            // Act 1: Transition to running state
            vm.IsRunning = true;

            // Assert 1: Run / PreScan are disabled to prevent duplicate execution, Cancel is enabled
            Assert.IsFalse(vm.RunPreScanCommand.CanExecute(null));
            Assert.IsFalse(vm.RunExtractionCommand.CanExecute(null));
            Assert.IsTrue(vm.CancelCommand.CanExecute(null));

            // Act 2: Return to idle / stopped state
            vm.IsRunning = false;

            // Assert 2: Run / PreScan become executable again
            Assert.IsTrue(vm.RunPreScanCommand.CanExecute(null));
            Assert.IsTrue(vm.RunExtractionCommand.CanExecute(null));
            Assert.IsFalse(vm.CancelCommand.CanExecute(null));
        }

        [TestMethod]
        [Description("Verifies that CancelCommand is disabled while cancellation is in progress (IsCancelling == true) to prevent double clicks")]
        public void CommandCanExecute_WhenCancelling_CancelCommandShouldBeDisabledToPreventDoubleExecution()
        {
            // Arrange
            var vm = new MainViewModel();
            vm.IsRunning = true;
            Assert.IsTrue(vm.CancelCommand.CanExecute(null));

            // Act: Transition to cancelling state
            vm.IsCancelling = true;

            // Assert: CancelCommand is disabled (preventing redundant cancellation requests)
            Assert.IsFalse(vm.CancelCommand.CanExecute(null));

            // Return to completed state
            vm.IsRunning = false;
            vm.IsCancelling = false;
            Assert.IsFalse(vm.CancelCommand.CanExecute(null));
            Assert.IsTrue(vm.RunExtractionCommand.CanExecute(null));
        }

        [TestMethod]
        [Description("Verifies that a default output path is configured on initialization and has a .csv extension")]
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
        [Description("Verifies binding updates for progress percentage and status text properties")]
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
        [Description("Verifies that double-clicking a preview row (AddDiscoveredItemToRules) adds the structure as an Exact rule and refreshes preview")]
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

            // Act: Double-click item to add rule (command or method execution)
            vm.AddDiscoveredItemCommand.Execute(item);

            // Assert: Parotid_L is added as Exact rule to mapping rules and selected
            Assert.AreEqual(1, vm.MappingRules.Count);
            Assert.AreEqual("Parotid_L", vm.SelectedRule.Pattern);
            Assert.AreEqual(EclipseDataMiner.Models.StructureMatchMode.Exact, vm.SelectedRule.MatchMode);
            Assert.AreEqual("Parotid_L", vm.SelectedRule.TargetAlias);

            // Preview is refreshed and status becomes Mapped
            Assert.AreEqual("Parotid_L", item.ResolvedAlias);
            Assert.IsTrue(item.MatchStatus.Contains("Mapped (Exact"));
        }

        [TestMethod]
        [Description("Verifies that double-clicking an item with an existing rule selects the existing rule without creating duplicates")]
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

            // 1st double click -> adds rule (1 count)
            vm.AddDiscoveredItemToRules(item);
            Assert.AreEqual(1, vm.MappingRules.Count);

            // Act: 2nd double click
            vm.AddDiscoveredItemToRules(item);

            // Assert: Rule count remains 1 (preventing duplication), SelectedRule is preserved
            Assert.AreEqual(1, vm.MappingRules.Count);
            Assert.IsNotNull(vm.SelectedRule);
            Assert.AreEqual("Bladder", vm.SelectedRule.Pattern);
        }

        [TestMethod]
        [Description("Verifies that preview text filter only includes items partially matching RawStructureId or ResolvedAlias")]
        public void DiscoveredFilter_ByText_ShouldFilterViewCorrectly()
        {
            // Arrange
            var vm = new MainViewModel();
            vm.DiscoveredStructures.Add(new EclipseDataMiner.Models.DiscoveredStructureItem { RawStructureId = "PTV_60", ResolvedAlias = "PTV_Integrated", MatchStatus = "Mapped (Exact)" });
            vm.DiscoveredStructures.Add(new EclipseDataMiner.Models.DiscoveredStructureItem { RawStructureId = "Rectum", ResolvedAlias = "Rectum", MatchStatus = "Unmapped (Raw)" });
            vm.DiscoveredStructures.Add(new EclipseDataMiner.Models.DiscoveredStructureItem { RawStructureId = "Bladder", ResolvedAlias = "Bladder", MatchStatus = "Unmapped (Raw)" });

            Assert.AreEqual(3, vm.FilteredDiscoveredCount);

            // Act 1: Filter by "PTV"
            vm.DiscoveredFilterText = "ptv";
            Assert.AreEqual(1, vm.FilteredDiscoveredCount);

            // Act 2: Filter by "Integrated" (partial match on ResolvedAlias)
            vm.DiscoveredFilterText = "Integrated";
            Assert.AreEqual(1, vm.FilteredDiscoveredCount);

            // Act 3: No match
            vm.DiscoveredFilterText = "NonExistent";
            Assert.AreEqual(0, vm.FilteredDiscoveredCount);

            // Act 4: Clear filter
            vm.ClearDiscoveredFilterCommand.Execute(null);
            Assert.AreEqual(string.Empty, vm.DiscoveredFilterText);
            Assert.AreEqual(3, vm.FilteredDiscoveredCount);
        }

        [TestMethod]
        [Description("Verifies preview status filter (Unmapped Only / Mapped Only / Excluded Only)")]
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
        [Description("Verifies that deleting a rule automatically selects the row below it, enabling continuous Delete execution")]
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

            // Act 1: Select first row r1 and execute Delete
            vm.SelectedRule = r1;
            Assert.IsTrue(vm.DeleteRuleCommand.CanExecute(null));
            vm.DeleteRuleCommand.Execute(null);

            // Assert 1: r1 is deleted, r2 below it is automatically selected, CanExecute remains true
            Assert.AreEqual(2, vm.MappingRules.Count);
            Assert.AreSame(r2, vm.SelectedRule);
            Assert.IsTrue(vm.DeleteRuleCommand.CanExecute(null));

            // Act 2: Continuously execute Delete (deleting r2)
            vm.DeleteRuleCommand.Execute(null);

            // Assert 2: r2 is deleted, r3 below it is automatically selected, CanExecute remains true
            Assert.AreEqual(1, vm.MappingRules.Count);
            Assert.AreSame(r3, vm.SelectedRule);
            Assert.IsTrue(vm.DeleteRuleCommand.CanExecute(null));

            // Act 3: Delete the last item
            vm.DeleteRuleCommand.Execute(null);

            // Assert 3: All deleted, SelectedRule is null, CanExecute is false
            Assert.AreEqual(0, vm.MappingRules.Count);
            Assert.IsNull(vm.SelectedRule);
            Assert.IsFalse(vm.DeleteRuleCommand.CanExecute(null));
        }

        [TestMethod]
        [Description("Verifies that deleting the last rule automatically selects the new last row (row above)")]
        public void DeleteRuleCommand_WhenDeletingLastRow_ShouldSelectNewLastRow()
        {
            // Arrange
            var vm = new MainViewModel();
            var r1 = new EclipseDataMiner.Models.StructureMappingRule { Pattern = "Rule1", TargetAlias = "Alias1" };
            var r2 = new EclipseDataMiner.Models.StructureMappingRule { Pattern = "Rule2", TargetAlias = "Alias2" };
            vm.MappingRules.Add(r1);
            vm.MappingRules.Add(r2);

            // Act: Delete last item r2
            vm.SelectedRule = r2;
            vm.DeleteRuleCommand.Execute(null);

            // Assert: New last item r1 is selected, CanExecute remains true
            Assert.AreEqual(1, vm.MappingRules.Count);
            Assert.AreSame(r1, vm.SelectedRule);
            Assert.IsTrue(vm.DeleteRuleCommand.CanExecute(null));
        }

        [TestMethod]
        [Description("Verifies that deleting a DQP automatically selects the row below it, enabling continuous Delete execution")]
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

            // Act 1: Delete middle item d2
            vm.SelectedDqp = d2;
            Assert.IsTrue(vm.DeleteDqpCommand.CanExecute(null));
            vm.DeleteDqpCommand.Execute(null);

            // Assert 1: d2 is deleted, d3 below it is automatically selected
            Assert.AreEqual(2, vm.DQPList.Count);
            Assert.AreSame(d3, vm.SelectedDqp);
            Assert.IsTrue(vm.DeleteDqpCommand.CanExecute(null));

            // Act 2: Delete d3 which became the last item
            vm.DeleteDqpCommand.Execute(null);

            // Assert 2: New last item d1 is automatically selected
            Assert.AreEqual(1, vm.DQPList.Count);
            Assert.AreSame(d1, vm.SelectedDqp);
            Assert.IsTrue(vm.DeleteDqpCommand.CanExecute(null));
        }

        [TestMethod]
        [Description("Eliminates MainWindow hardcoding: Verifies that WindowTitle is derived dynamically from assembly ProductVersion (InformationalVersion)")]
        public void WindowTitle_ShouldDeriveDynamicallyFromProductVersion()
        {
            // Arrange
            var vm = new MainViewModel();

            // Act
            string productVer = MainViewModel.GetProductVersion();
            string title = vm.WindowTitle;

            // Assert: ProductVersion is 3.0.0 and contained in the title
            Assert.AreEqual("3.0.0", productVer);
            Assert.IsTrue(title.Contains("v3.0.0"), $"Title '{title}' does not contain 'v3.0.0'");
            Assert.IsTrue(title.StartsWith("EclipseDataMiner"));
            Assert.IsTrue(title.Contains("High-Throughput Clinical ESAPI Data Mining Platform"));
        }

        [TestMethod]
        [Description("Verifies that Tab 2 PreScanScopeBadgeText updates in real time based on selected plans in Plan Search")]
        public void PreScanScopeBadgeText_ShouldReflectMatchedPlansSelection()
        {
            // Arrange
            var vm = new MainViewModel();

            // Initial state: Prior to search
            Assert.AreEqual("🌐 Target: All Criteria Matching Plans", vm.PreScanScopeBadgeText);

            // Act 1: 3 search results added (initially all IsSelected = true)
            var p1 = new EclipseDataMiner.Models.MatchedPlanItem { PatientId = "PT1", CourseId = "C1", PlanId = "Plan1", IsSelected = true };
            var p2 = new EclipseDataMiner.Models.MatchedPlanItem { PatientId = "PT1", CourseId = "C1", PlanId = "Plan2", IsSelected = true };
            var p3 = new EclipseDataMiner.Models.MatchedPlanItem { PatientId = "PT2", CourseId = "C1", PlanId = "Plan1", IsSelected = true };
            vm.MatchedPlans.Add(p1);
            vm.MatchedPlans.Add(p2);
            vm.MatchedPlans.Add(p3);
            vm.UpdateMatchedPlansSummary();

            // Assert 1: Display shows 3 / 3 selected
            Assert.AreEqual("🎯 Target: 3 / 3 Selected Plans", vm.PreScanScopeBadgeText);

            // Act 2: Uncheck 1 item
            p2.IsSelected = false;
            vm.UpdateMatchedPlansSummary();

            // Assert 2: Display shows 2 / 3 selected
            Assert.AreEqual("🎯 Target: 2 / 3 Selected Plans", vm.PreScanScopeBadgeText);

            // Act 3: Deselect all
            vm.ExecuteSelectAllPlans(false);

            // Assert 3: Display shows 0 / 3 selected
            Assert.AreEqual("🎯 Target: 0 / 3 Selected Plans", vm.PreScanScopeBadgeText);
        }
    }
}
