using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EclipseDataMiner;
using EclipseDataMiner.ViewModels;
using EclipseDataMiner.Models;

namespace EclipseDataMiner.Tests
{
    [TestClass]
    public class UiScreenshotTests
    {
        [TestMethod]
        [Description("実際のWPF MainWindowおよびViewModelを初期化し、4つのタブすべてについて臨床サンプルデータを投入した実機UI画面キャプチャを生成")]
        public void RenderActualMainWindow_ShouldGenerateValidScreenshot()
        {
            string repoRoot = null;
            string current = Path.GetDirectoryName(typeof(UiScreenshotTests).Assembly.Location);
            while (!string.IsNullOrEmpty(current))
            {
                if (File.Exists(Path.Combine(current, "EclipseDataMiner.sln")))
                {
                    repoRoot = current;
                    break;
                }
                current = Path.GetDirectoryName(current);
            }
            if (string.IsNullOrEmpty(repoRoot))
            {
                current = Directory.GetCurrentDirectory();
                while (!string.IsNullOrEmpty(current))
                {
                    if (File.Exists(Path.Combine(current, "EclipseDataMiner.sln")))
                    {
                        repoRoot = current;
                        break;
                    }
                    current = Path.GetDirectoryName(current);
                }
            }
            if (string.IsNullOrEmpty(repoRoot))
            {
                repoRoot = @"g:\Source\Repos\tkmd94\EclipseDataMiner";
            }

            string imgDir = Path.Combine(repoRoot, "img");
            string docsImgDir = Path.Combine(repoRoot, "docs", "img");

            if (!Directory.Exists(imgDir)) Directory.CreateDirectory(imgDir);
            if (!Directory.Exists(docsImgDir)) Directory.CreateDirectory(docsImgDir);

            Exception thrownException = null;

            var thread = new Thread(() =>
            {
                try
                {
                    if (Application.Current == null)
                    {
                        var app = new App();
                        app.InitializeComponent();
                    }

                    var window = new MainWindow();
                    window.Width = 1260;
                    window.Height = 980;
                    window.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                    var vm = window.DataContext as MainViewModel;
                    if (vm != null)
                    {
                        // 1. プリセットおよび検索条件
                        var preset = new SearchPreset
                        {
                            Name = "Prostate VMAT 78Gy (Standard)",
                            Description = "Prostate VMAT 78Gy (Standard)"
                        };
                        vm.Presets.Add(preset);
                        vm.SelectedPreset = preset;

                        vm.PatientIdText = "3315206, 3315207, 3315308, 3315500";
                        vm.CourseIdText = "";
                        vm.PlanIdText = "";
                        vm.DosePerFractionText = "78";
                        vm.NumberOfFractionsText = "5";
                        vm.TotalDoseText = "78";
                        vm.DosePresence = DosePresenceFilter.HasDose;
                        vm.PresetDescriptionInput = "Prostate VMAT 78Gy (Standard)";

                        // 2. 検索結果一覧 (MatchedPlans) にリアルな臨床データを注入
                        vm.MatchedPlans.Clear();
                        vm.MatchedPlans.Add(new MatchedPlanItem {
                            IsSelected = true,
                            PatientId = "3315206",
                            CourseId = "0301",
                            PlanId = "918",
                            DosePerFraction = 78.0,
                            NumberOfFractions = 5,
                            TotalDose = 78.0,
                            Machine = "TMAT 5",
                            Energy = "6X",
                            Technique = "VMAT Plan",
                            ApprovalStatus = "TreatmentApproved",
                            HasDose = true
                        });
                        vm.MatchedPlans.Add(new MatchedPlanItem {
                            IsSelected = true,
                            PatientId = "3315207",
                            CourseId = "0302",
                            PlanId = "919",
                            DosePerFraction = 78.0,
                            NumberOfFractions = 5,
                            TotalDose = 78.0,
                            Machine = "TMAT 6",
                            Energy = "6X",
                            Technique = "VMAT Plan",
                            ApprovalStatus = "TreatmentApproved",
                            HasDose = true
                        });
                        vm.MatchedPlans.Add(new MatchedPlanItem {
                            IsSelected = true,
                            PatientId = "3315308",
                            CourseId = "0302",
                            PlanId = "2900",
                            DosePerFraction = 78.0,
                            NumberOfFractions = 5,
                            TotalDose = 78.0,
                            Machine = "TMAT 5",
                            Energy = "10X",
                            Technique = "VMAT Plan",
                            ApprovalStatus = "TreatmentApproved",
                            HasDose = true
                        });
                        vm.MatchedPlans.Add(new MatchedPlanItem {
                            IsSelected = true,
                            PatientId = "3315500",
                            CourseId = "0304",
                            PlanId = "2901",
                            DosePerFraction = 78.0,
                            NumberOfFractions = 5,
                            TotalDose = 78.0,
                            Machine = "TMAT 6",
                            Energy = "6X",
                            Technique = "VMAT Plan",
                            ApprovalStatus = "TreatmentApproved",
                            HasDose = true
                        });
                        vm.MatchedPlans.Add(new MatchedPlanItem {
                            IsSelected = true,
                            PatientId = "3315501",
                            CourseId = "0403",
                            PlanId = "2902",
                            DosePerFraction = 78.0,
                            NumberOfFractions = 5,
                            TotalDose = 78.0,
                            Machine = "TMAT 6",
                            Energy = "6X",
                            Technique = "VMAT Plan",
                            ApprovalStatus = "TreatmentApproved",
                            HasDose = true
                        });
                        vm.MatchedPlans.Add(new MatchedPlanItem {
                            IsSelected = true,
                            PatientId = "3315522",
                            CourseId = "0403",
                            PlanId = "7003",
                            DosePerFraction = 78.0,
                            NumberOfFractions = 5,
                            TotalDose = 78.0,
                            Machine = "TMAT 8",
                            Energy = "10X",
                            Technique = "VMAT Plan",
                            ApprovalStatus = "Completed",
                            HasDose = false
                        });

                        // 3. ログとステータス
                        vm.LogText = "Ready to extract.\r\n[INFO] Loaded preset: Prostate VMAT 78Gy (Standard)\r\n[INFO] Matched 6 plans matching clinical criteria across 5 patients.\r\n[INFO] Structure Mapping verified (PTV, Rectum, Bladder, FemoralHeads).\r\n[INFO] Ready for execution.";
                        vm.ProgressPercentage = 100;
                        vm.ProgressText = "Idle / Ready (6 plans selected for extraction)";
                        vm.MatchedPlansSummaryText = "6 / 6 plans selected";

                        // 4. Tab 2: Structure Mapping サンプルデータ
                        vm.DiscoveredStructures.Clear();
                        vm.DiscoveredStructures.Add(new DiscoveredStructureItem { RawStructureId = "PTV_78Gy", HitCount = 6, ResolvedAlias = "PTV", MatchStatus = "Mapped (Exact -> PTV)", IsExtracted = true });
                        vm.DiscoveredStructures.Add(new DiscoveredStructureItem { RawStructureId = "ptv78", HitCount = 4, ResolvedAlias = "PTV", MatchStatus = "Mapped (Contains -> PTV)", IsExtracted = true });
                        vm.DiscoveredStructures.Add(new DiscoveredStructureItem { RawStructureId = "Rectum", HitCount = 6, ResolvedAlias = "Rectum", MatchStatus = "Mapped (Regex -> Rectum)", IsExtracted = true });
                        vm.DiscoveredStructures.Add(new DiscoveredStructureItem { RawStructureId = "Bladder", HitCount = 6, ResolvedAlias = "Bladder", MatchStatus = "Mapped (Regex -> Bladder)", IsExtracted = true });
                        vm.DiscoveredStructures.Add(new DiscoveredStructureItem { RawStructureId = "Femur_R", HitCount = 5, ResolvedAlias = "FemoralHead_R", MatchStatus = "Mapped (Contains -> FemoralHead_R)", IsExtracted = true });
                        vm.DiscoveredStructures.Add(new DiscoveredStructureItem { RawStructureId = "Femur_L", HitCount = 5, ResolvedAlias = "FemoralHead_L", MatchStatus = "Mapped (Contains -> FemoralHead_L)", IsExtracted = true });
                        vm.DiscoveredStructures.Add(new DiscoveredStructureItem { RawStructureId = "PenileBulb", HitCount = 3, ResolvedAlias = "PenileBulb", MatchStatus = "Mapped (Exact -> PenileBulb)", IsExtracted = true });
                        vm.DiscoveredStructures.Add(new DiscoveredStructureItem { RawStructureId = "Bowel", HitCount = 2, ResolvedAlias = "", MatchStatus = "Unmapped (Raw)", IsExtracted = false });

                        vm.MappingRules.Clear();
                        vm.MappingRules.Add(new StructureMappingRule { IsSelected = true, MatchMode = StructureMatchMode.Exact, Pattern = "PTV_78Gy", TargetAlias = "PTV" });
                        vm.MappingRules.Add(new StructureMappingRule { IsSelected = true, MatchMode = StructureMatchMode.Contains, Pattern = "PTV", TargetAlias = "PTV" });
                        vm.MappingRules.Add(new StructureMappingRule { IsSelected = true, MatchMode = StructureMatchMode.Regex, Pattern = "(?i)^rectum.*", TargetAlias = "Rectum" });
                        vm.MappingRules.Add(new StructureMappingRule { IsSelected = true, MatchMode = StructureMatchMode.Regex, Pattern = "(?i)^bladder.*", TargetAlias = "Bladder" });
                        vm.MappingRules.Add(new StructureMappingRule { IsSelected = true, MatchMode = StructureMatchMode.Contains, Pattern = "Femur_R", TargetAlias = "FemoralHead_R" });
                        vm.MappingRules.Add(new StructureMappingRule { IsSelected = true, MatchMode = StructureMatchMode.Contains, Pattern = "Femur_L", TargetAlias = "FemoralHead_L" });

                        // 5. Tab 3: DQP サンプルデータ
                        vm.DQPList.Clear();
                        vm.DQPList.Add(new DQP { structureName = "PTV", DQPtype = DQPtype.Dose, DQPvalue = 95.0, InputUnit = IOUnit.Relative, OutputUnit = IOUnit.Relative });
                        vm.DQPList.Add(new DQP { structureName = "PTV", DQPtype = DQPtype.Dose, DQPvalue = 98.0, InputUnit = IOUnit.Relative, OutputUnit = IOUnit.Relative });
                        vm.DQPList.Add(new DQP { structureName = "PTV", DQPtype = DQPtype.Dose, DQPvalue = 2.0, InputUnit = IOUnit.Relative, OutputUnit = IOUnit.Relative });
                        vm.DQPList.Add(new DQP { structureName = "Rectum", DQPtype = DQPtype.Volume, DQPvalue = 70.0, InputUnit = IOUnit.Absolute, OutputUnit = IOUnit.Relative });
                        vm.DQPList.Add(new DQP { structureName = "Rectum", DQPtype = DQPtype.Volume, DQPvalue = 50.0, InputUnit = IOUnit.Absolute, OutputUnit = IOUnit.Relative });
                        vm.DQPList.Add(new DQP { structureName = "Rectum", DQPtype = DQPtype.Volume, DQPvalue = 40.0, InputUnit = IOUnit.Absolute, OutputUnit = IOUnit.Relative });
                        vm.DQPList.Add(new DQP { structureName = "Bladder", DQPtype = DQPtype.Volume, DQPvalue = 70.0, InputUnit = IOUnit.Absolute, OutputUnit = IOUnit.Relative });
                        vm.DQPList.Add(new DQP { structureName = "Bladder", DQPtype = DQPtype.Volume, DQPvalue = 65.0, InputUnit = IOUnit.Absolute, OutputUnit = IOUnit.Relative });
                        vm.DQPList.Add(new DQP { structureName = "FemoralHead_R", DQPtype = DQPtype.Volume, DQPvalue = 50.0, InputUnit = IOUnit.Absolute, OutputUnit = IOUnit.Relative });
                        vm.DQPList.Add(new DQP { structureName = "FemoralHead_L", DQPtype = DQPtype.Volume, DQPvalue = 50.0, InputUnit = IOUnit.Absolute, OutputUnit = IOUnit.Relative });

                        // 6. Tab 4: Options サンプル設定
                        vm.OutputFilePath = @"C:\EclipseDataMiner\Exports\Prostate_VMAT_Cohort_2026.csv";
                        if (vm.Options != null)
                        {
                            vm.Options.ExportPlanningApprover = true;
                            vm.Options.ExportPlanningApprovalDate = true;
                            vm.Options.ExportCalculationModel = true;
                            vm.Options.ExportNormalizationMode = true;
                            vm.Options.ExportClinicalProtocol = true;
                            vm.Options.ExportOptimizationObjectives = true;
                            vm.Options.ExportBeamMU = true;
                            vm.Options.ExportBeamMachineEnergyTech = true;
                            vm.Options.ExportCalculationLog = false;
                            vm.Options.ExportPlanComplexity = true;
                            vm.Options.ExportJsonl = true;
                            vm.Options.AnonymizeOutput = true;
                        }
                    }

                    // ウィンドウを表示してレイアウト・テンプレートを展開
                    window.Show();

                    Action<string> captureTab = (fileName) =>
                    {
                        var frame = new DispatcherFrame();
                        Dispatcher.CurrentDispatcher.BeginInvoke(
                            DispatcherPriority.Background,
                            new Action(() => {
                                Thread.Sleep(250);
                                frame.Continue = false;
                            }));
                        Dispatcher.PushFrame(frame);

                        var contentElement = window.Content as FrameworkElement;
                        if (contentElement != null)
                        {
                            int targetW = 1260;
                            int targetH = 980;

                            contentElement.Width = double.NaN;
                            contentElement.Height = double.NaN;
                            contentElement.Measure(new Size(targetW, targetH));
                            contentElement.Arrange(new Rect(0, 0, targetW, targetH));
                            contentElement.UpdateLayout();

                            var rtbContent = new RenderTargetBitmap(targetW, targetH, 96, 96, PixelFormats.Pbgra32);
                            rtbContent.Render(contentElement);
                            var encContent = new PngBitmapEncoder();
                            encContent.Frames.Add(BitmapFrame.Create(rtbContent));

                            string save1 = Path.Combine(imgDir, fileName);
                            string save2 = Path.Combine(docsImgDir, fileName);

                            using (var fs = File.Create(save1))
                            {
                                encContent.Save(fs);
                            }
                            File.Copy(save1, save2, true);
                        }
                    };

                    // --- 1. Tab 1: Plan Search ---
                    vm.SelectedTabIndex = 0;
                    captureTab("UI.png");
                    captureTab("UI_Tab1_PlanSearch.png");

                    // --- 2. Tab 2: Structure Mapping ---
                    vm.SelectedTabIndex = 1;
                    captureTab("UI_Tab2_StructureMapping.png");

                    // --- 3. Tab 3: Dose Quality Parameters (DQP) ---
                    vm.SelectedTabIndex = 2;
                    captureTab("UI_Tab3_DQP.png");

                    // --- 4. Tab 4: Extraction & Analysis Options ---
                    vm.SelectedTabIndex = 3;
                    captureTab("UI_Tab4_Options.png");

                    window.Close();
                }
                catch (Exception ex)
                {
                    thrownException = ex;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (thrownException != null)
            {
                Assert.Fail($"UI Screenshot generation failed: {thrownException}");
            }

            string[] expectedFiles = new[]
            {
                "UI.png",
                "UI_Tab1_PlanSearch.png",
                "UI_Tab2_StructureMapping.png",
                "UI_Tab3_DQP.png",
                "UI_Tab4_Options.png"
            };

            foreach (var fn in expectedFiles)
            {
                string p1 = Path.Combine(imgDir, fn);
                string p2 = Path.Combine(docsImgDir, fn);
                Assert.IsTrue(File.Exists(p1), $"Image {fn} should exist in img/");
                Assert.IsTrue(File.Exists(p2), $"Image {fn} should exist in docs/img/");
                var fi = new FileInfo(p1);
                Assert.IsTrue(fi.Length > 10000, $"Image {fn} should not be empty. Size: {fi.Length} bytes");
            }
        }
    }
}
