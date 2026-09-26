using CommunityToolkit.Mvvm.ComponentModel;

namespace EclipseDataMiner.Models
{
    /// <summary>
    /// データマイニングの抽出オプション設定モデル
    /// </summary>
    public class ExtractionOptions : ObservableObject
    {
        // 計画メタデータ
        private bool _exportPlanningApprover = false;
        public bool ExportPlanningApprover
        {
            get => _exportPlanningApprover;
            set => SetProperty(ref _exportPlanningApprover, value);
        }

        private bool _exportPlanningApprovalDate = false;
        public bool ExportPlanningApprovalDate
        {
            get => _exportPlanningApprovalDate;
            set => SetProperty(ref _exportPlanningApprovalDate, value);
        }

        private bool _exportCalculationModel = false;
        public bool ExportCalculationModel
        {
            get => _exportCalculationModel;
            set => SetProperty(ref _exportCalculationModel, value);
        }

        private bool _exportNormalizationMode = false;
        public bool ExportNormalizationMode
        {
            get => _exportNormalizationMode;
            set => SetProperty(ref _exportNormalizationMode, value);
        }

        private bool _exportClinicalProtocol = false;
        public bool ExportClinicalProtocol
        {
            get => _exportClinicalProtocol;
            set => SetProperty(ref _exportClinicalProtocol, value);
        }

        private bool _exportOptimizationObjectives = false;
        public bool ExportOptimizationObjectives
        {
            get => _exportOptimizationObjectives;
            set => SetProperty(ref _exportOptimizationObjectives, value);
        }

        // 照射パラメータ
        private bool _exportBeamMU = false;
        public bool ExportBeamMU
        {
            get => _exportBeamMU;
            set => SetProperty(ref _exportBeamMU, value);
        }

        private bool _exportBeamMachineEnergyTech = false;
        public bool ExportBeamMachineEnergyTech
        {
            get => _exportBeamMachineEnergyTech;
            set => SetProperty(ref _exportBeamMachineEnergyTech, value);
        }

        // 計算ログ
        private bool _exportCalculationLog = false;
        public bool ExportCalculationLog
        {
            get => _exportCalculationLog;
            set => SetProperty(ref _exportCalculationLog, value);
        }

        // 複雑度評価
        private bool _exportPlanComplexity = false;
        public bool ExportPlanComplexity
        {
            get => _exportPlanComplexity;
            set => SetProperty(ref _exportPlanComplexity, value);
        }

        // 匿名化
        private bool _anonymizeOutput = false;
        public bool AnonymizeOutput
        {
            get => _anonymizeOutput;
            set => SetProperty(ref _anonymizeOutput, value);
        }

        // JSONL 出力
        private bool _exportJsonl = false;
        public bool ExportJsonl
        {
            get => _exportJsonl;
            set => SetProperty(ref _exportJsonl, value);
        }

        // DVH曲線CSV出力
        private bool _exportDvhCurves = false;
        public bool ExportDvhCurves
        {
            get => _exportDvhCurves;
            set => SetProperty(ref _exportDvhCurves, value);
        }

        // 事前スキャンバイパス
        private bool _bypassPreScan = false;
        public bool BypassPreScan
        {
            get => _bypassPreScan;
            set => SetProperty(ref _bypassPreScan, value);
        }

        // PlanSum（合算計画）を含める
        private bool _exportPlanSums = false;
        public bool ExportPlanSums
        {
            get => _exportPlanSums;
            set => SetProperty(ref _exportPlanSums, value);
        }
    }
}
