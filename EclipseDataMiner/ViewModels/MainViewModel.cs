using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EclipseDataMiner.Models;
using EclipseDataMiner.Services;
using Microsoft.Win32;

namespace EclipseDataMiner.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly StaEsapiWorkerService _workerService;
        private CancellationTokenSource _cts;

        // Extraction options
        public ExtractionOptions Options { get; } = new ExtractionOptions();

        // Window title (dynamically resolved from ProductVersion of the assembly)
        private string _windowTitle;
        public string WindowTitle
        {
            get
            {
                if (_windowTitle != null) return _windowTitle;
                string productVersion = GetProductVersion();
                string verStr = !string.IsNullOrEmpty(productVersion)
                    ? (productVersion.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? productVersion : $"v{productVersion}")
                    : "v3.0.1";
                return $"EclipseDataMiner {verStr} - High-Throughput Clinical ESAPI Data Mining Platform";
            }
            set => SetProperty(ref _windowTitle, value);
        }

        /// <summary>
        /// Retrieves ProductVersion (AssemblyInformationalVersion) of the assembly.
        /// </summary>
        public static string GetProductVersion()
        {
            try
            {
                var asm = typeof(MainViewModel).Assembly;

                // 1. Retrieve AssemblyInformationalVersionAttribute (highest priority: ProductVersion)
                var infoAttr = (System.Reflection.AssemblyInformationalVersionAttribute)
                    System.Attribute.GetCustomAttribute(asm, typeof(System.Reflection.AssemblyInformationalVersionAttribute));
                if (!string.IsNullOrWhiteSpace(infoAttr?.InformationalVersion))
                {
                    return infoAttr.InformationalVersion.Trim();
                }

                // 2. Retrieve FileVersionInfo.ProductVersion
                if (!string.IsNullOrEmpty(asm.Location) && System.IO.File.Exists(asm.Location))
                {
                    var vi = System.Diagnostics.FileVersionInfo.GetVersionInfo(asm.Location);
                    if (!string.IsNullOrWhiteSpace(vi.ProductVersion))
                    {
                        return vi.ProductVersion.Trim();
                    }
                }

                // 3. Retrieve Assembly.GetName().Version (fallback)
                var v = asm.GetName().Version;
                if (v != null)
                {
                    return $"{v.Major}.{v.Minor}.{v.Build}";
                }
            }
            catch { }

            return "3.0.1";
        }

        // Search criteria
        private string _patientIdText = string.Empty;
        public string PatientIdText
        {
            get => _patientIdText;
            set => SetProperty(ref _patientIdText, value);
        }

        private TextMatchMode _patientIdMatchMode = TextMatchMode.Contains;
        public TextMatchMode PatientIdMatchMode
        {
            get => _patientIdMatchMode;
            set => SetProperty(ref _patientIdMatchMode, value);
        }

        private string _courseIdText = string.Empty;
        public string CourseIdText
        {
            get => _courseIdText;
            set => SetProperty(ref _courseIdText, value);
        }

        private TextMatchMode _courseIdMatchMode = TextMatchMode.Contains;
        public TextMatchMode CourseIdMatchMode
        {
            get => _courseIdMatchMode;
            set => SetProperty(ref _courseIdMatchMode, value);
        }

        private string _planIdText = string.Empty;
        public string PlanIdText
        {
            get => _planIdText;
            set => SetProperty(ref _planIdText, value);
        }

        private TextMatchMode _planIdMatchMode = TextMatchMode.Contains;
        public TextMatchMode PlanIdMatchMode
        {
            get => _planIdMatchMode;
            set => SetProperty(ref _planIdMatchMode, value);
        }

        private string _targetVolumeIdText = string.Empty;
        public string TargetVolumeIdText
        {
            get => _targetVolumeIdText;
            set => SetProperty(ref _targetVolumeIdText, value);
        }

        private TextMatchMode _targetVolumeMatchMode = TextMatchMode.Contains;
        public TextMatchMode TargetVolumeMatchMode
        {
            get => _targetVolumeMatchMode;
            set => SetProperty(ref _targetVolumeMatchMode, value);
        }

        private string _dosePerFractionText = string.Empty;
        public string DosePerFractionText
        {
            get => _dosePerFractionText;
            set => SetProperty(ref _dosePerFractionText, value);
        }

        private string _numberOfFractionsText = string.Empty;
        public string NumberOfFractionsText
        {
            get => _numberOfFractionsText;
            set => SetProperty(ref _numberOfFractionsText, value);
        }

        private string _totalDoseText = string.Empty;
        public string TotalDoseText
        {
            get => _totalDoseText;
            set => SetProperty(ref _totalDoseText, value);
        }

        private bool _filterUnapproved = true;
        public bool FilterUnapproved
        {
            get => _filterUnapproved;
            set => SetProperty(ref _filterUnapproved, value);
        }

        private bool _filterPlanApproved = true;
        public bool FilterPlanApproved
        {
            get => _filterPlanApproved;
            set => SetProperty(ref _filterPlanApproved, value);
        }

        private bool _filterTreatmentApproved = true;
        public bool FilterTreatmentApproved
        {
            get => _filterTreatmentApproved;
            set => SetProperty(ref _filterTreatmentApproved, value);
        }

        private bool _globalLogicIsAnd = true;
        public bool GlobalLogicIsAnd
        {
            get => _globalLogicIsAnd;
            set
            {
                if (SetProperty(ref _globalLogicIsAnd, value))
                {
                    OnPropertyChanged(nameof(GlobalLogicIsOr));
                }
            }
        }

        public bool GlobalLogicIsOr
        {
            get => !_globalLogicIsAnd;
            set
            {
                if (value)
                {
                    GlobalLogicIsAnd = false;
                }
                else
                {
                    GlobalLogicIsAnd = true;
                }
            }
        }

        private DosePresenceFilter _dosePresence = DosePresenceFilter.All;
        public DosePresenceFilter DosePresence
        {
            get => _dosePresence;
            set => SetProperty(ref _dosePresence, value);
        }

        // Advanced metadata filters (Machine, Energy, Technique, Date Range)
        private string _machineFilterText = string.Empty;
        public string MachineFilterText
        {
            get => _machineFilterText;
            set => SetProperty(ref _machineFilterText, value);
        }

        private string _energyFilterText = string.Empty;
        public string EnergyFilterText
        {
            get => _energyFilterText;
            set => SetProperty(ref _energyFilterText, value);
        }

        private string _techniqueFilterText = string.Empty;
        public string TechniqueFilterText
        {
            get => _techniqueFilterText;
            set => SetProperty(ref _techniqueFilterText, value);
        }

        private DateFilterTarget _dateTarget = DateFilterTarget.TreatmentApprovalDate;
        public DateFilterTarget DateTarget
        {
            get => _dateTarget;
            set => SetProperty(ref _dateTarget, value);
        }

        public List<DateFilterTarget> DateTargetOptions { get; } = new List<DateFilterTarget>
        {
            DateFilterTarget.TreatmentApprovalDate,
            DateFilterTarget.PlanningApprovalDate,
            DateFilterTarget.CreationDate
        };

        private DateTime? _dateFrom;
        public DateTime? DateFrom
        {
            get => _dateFrom;
            set => SetProperty(ref _dateFrom, value);
        }

        private DateTime? _dateTo;
        public DateTime? DateTo
        {
            get => _dateTo;
            set => SetProperty(ref _dateTo, value);
        }

        public IRelayCommand ClearDatesCommand { get; }

        // Search presets
        private readonly SearchPresetService _presetService;
        public ObservableCollection<SearchPreset> Presets { get; } = new ObservableCollection<SearchPreset>();

        private SearchPreset _selectedPreset;
        public SearchPreset SelectedPreset
        {
            get => _selectedPreset;
            set
            {
                if (SetProperty(ref _selectedPreset, value))
                {
                    if (value != null)
                    {
                        ApplyPreset(value);
                        PresetNameInput = value.Name;
                        PresetDescriptionInput = value.Description ?? string.Empty;
                    }
                    else
                    {
                        PresetDescriptionInput = string.Empty;
                    }
                    (DeletePresetCommand as IRelayCommand)?.NotifyCanExecuteChanged();
                }
            }
        }

        private string _presetNameInput = string.Empty;
        public string PresetNameInput
        {
            get => _presetNameInput;
            set
            {
                if (SetProperty(ref _presetNameInput, value))
                {
                    (SavePresetCommand as IRelayCommand)?.NotifyCanExecuteChanged();
                }
            }
        }

        private string _presetDescriptionInput = string.Empty;
        public string PresetDescriptionInput
        {
            get => _presetDescriptionInput;
            set => SetProperty(ref _presetDescriptionInput, value);
        }

        public IRelayCommand SavePresetCommand { get; }
        public IRelayCommand DeletePresetCommand { get; }

        // Structure pre-mapping rules list (dictionary and persisted rules)
        public ObservableCollection<StructureMappingRule> MappingRules { get; } = new ObservableCollection<StructureMappingRule>();

        // Discovered raw structures from pre-scan and mapping preview list
        public ObservableCollection<DiscoveredStructureItem> DiscoveredStructures { get; } = new ObservableCollection<DiscoveredStructureItem>();

        private StructureMappingRule _selectedRule;
        public StructureMappingRule SelectedRule
        {
            get => _selectedRule;
            set
            {
                if (SetProperty(ref _selectedRule, value))
                {
                    UpdateRuleCommandsCanExecute();
                }
            }
        }

        /// <summary>
        /// Explicitly notifies CanExecute changes on rule management commands and refreshes UI command bindings.
        /// </summary>
        public void UpdateRuleCommandsCanExecute()
        {
            (DeleteRuleCommand as IRelayCommand)?.NotifyCanExecuteChanged();
            (MoveUpRuleCommand as IRelayCommand)?.NotifyCanExecuteChanged();
            (MoveDownRuleCommand as IRelayCommand)?.NotifyCanExecuteChanged();
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }

        private DiscoveredStructureItem _selectedDiscoveredItem;
        public DiscoveredStructureItem SelectedDiscoveredItem
        {
            get => _selectedDiscoveredItem;
            set => SetProperty(ref _selectedDiscoveredItem, value);
        }

        // Preview filtering properties
        private string _discoveredFilterText = string.Empty;
        public string DiscoveredFilterText
        {
            get => _discoveredFilterText;
            set
            {
                if (SetProperty(ref _discoveredFilterText, value))
                {
                    DiscoveredStructuresView?.Refresh();
                    OnPropertyChanged(nameof(FilteredDiscoveredCount));
                }
            }
        }

        private string _discoveredFilterStatus = "All";
        public string DiscoveredFilterStatus
        {
            get => _discoveredFilterStatus;
            set
            {
                if (SetProperty(ref _discoveredFilterStatus, value))
                {
                    DiscoveredStructuresView?.Refresh();
                    OnPropertyChanged(nameof(FilteredDiscoveredCount));
                }
            }
        }

        public List<string> DiscoveredFilterStatusOptions { get; } = new List<string>
        {
            "All",
            "Unmapped Only",
            "Mapped Only",
            "Excluded Only"
        };

        public ICollectionView DiscoveredStructuresView { get; }

        public int FilteredDiscoveredCount
        {
            get
            {
                if (DiscoveredStructuresView == null) return DiscoveredStructures.Count;
                return DiscoveredStructuresView.Cast<object>().Count();
            }
        }

        public IRelayCommand ClearDiscoveredFilterCommand { get; }

        // DQP list
        public ObservableCollection<DQP> DQPList { get; } = new ObservableCollection<DQP>();

        private DQP _selectedDqp;
        public DQP SelectedDqp
        {
            get => _selectedDqp;
            set
            {
                if (SetProperty(ref _selectedDqp, value))
                {
                    UpdateDqpCommandsCanExecute();
                }
            }
        }

        /// <summary>
        /// Explicitly notifies CanExecute changes on DQP management commands and refreshes UI command bindings.
        /// </summary>
        public void UpdateDqpCommandsCanExecute()
        {
            (DeleteDqpCommand as IRelayCommand)?.NotifyCanExecuteChanged();
            (MoveUpDqpCommand as IRelayCommand)?.NotifyCanExecuteChanged();
            (MoveDownDqpCommand as IRelayCommand)?.NotifyCanExecuteChanged();
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }

        // Output destination
        private string _outputFilePath = string.Empty;
        public string OutputFilePath
        {
            get => _outputFilePath;
            set => SetProperty(ref _outputFilePath, value);
        }

        // Execution status and progress
        private bool _isRunning = false;
        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (SetProperty(ref _isRunning, value))
                {
                    OnPropertyChanged(nameof(IsNotRunning));
                    (RunPreScanCommand as IRelayCommand)?.NotifyCanExecuteChanged();
                    (SearchPlansCommand as IRelayCommand)?.NotifyCanExecuteChanged();
                    (RunExtractionCommand as IRelayCommand)?.NotifyCanExecuteChanged();
                    (CancelCommand as IRelayCommand)?.NotifyCanExecuteChanged();
                    System.Windows.Input.CommandManager.InvalidateRequerySuggested();
                }
            }
        }
        public bool IsNotRunning => !IsRunning;

        private bool _isCancelling = false;
        public bool IsCancelling
        {
            get => _isCancelling;
            set
            {
                if (SetProperty(ref _isCancelling, value))
                {
                    (CancelCommand as IRelayCommand)?.NotifyCanExecuteChanged();
                    System.Windows.Input.CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private int _progressPercentage = 0;
        public int ProgressPercentage
        {
            get => _progressPercentage;
            set => SetProperty(ref _progressPercentage, value);
        }

        private string _progressText = "Ready";
        public string ProgressText
        {
            get => _progressText;
            set => SetProperty(ref _progressText, value);
        }

        private string _logText = string.Empty;
        public string LogText
        {
            get => _logText;
            set => SetProperty(ref _logText, value);
        }

        // Search results plan list and selection state
        public ObservableCollection<MatchedPlanItem> MatchedPlans { get; } = new ObservableCollection<MatchedPlanItem>();

        private string _matchedPlansSummaryText = "No plans searched yet.";
        public string MatchedPlansSummaryText
        {
            get => _matchedPlansSummaryText;
            set => SetProperty(ref _matchedPlansSummaryText, value);
        }

        private bool _hasMatchedPlans = false;
        public bool HasMatchedPlans
        {
            get => _hasMatchedPlans;
            set => SetProperty(ref _hasMatchedPlans, value);
        }

        private int _selectedTabIndex = 0;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set => SetProperty(ref _selectedTabIndex, value);
        }

        public int MatchedPlansCount => MatchedPlans.Count;

        // Pre-scan scope badge text for Tab 2 (Structure Mapping)
        public string PreScanScopeBadgeText
        {
            get
            {
                if (MatchedPlans.Count > 0)
                {
                    int selected = MatchedPlans.Count(p => p.IsSelected);
                    return $"🎯 Target: {selected} / {MatchedPlans.Count} Selected Plans";
                }
                return "🌐 Target: All Criteria Matching Plans";
            }
        }

        // Commands
        public IRelayCommand SearchPlansCommand { get; }
        public IRelayCommand SelectAllPlansCommand { get; }
        public IRelayCommand UnselectAllPlansCommand { get; }
        public IRelayCommand InvertPlanSelectionCommand { get; }
        public IRelayCommand RunPreScanCommand { get; }
        public IRelayCommand RunExtractionCommand { get; }
        public IRelayCommand CancelCommand { get; }
        public IRelayCommand SelectOutputFolderCommand { get; }
        public IRelayCommand OpenOutputFolderCommand { get; }
        public IRelayCommand SaveMappingRulesCommand { get; }
        public IRelayCommand LoadMappingRulesCommand { get; }
        public IRelayCommand AddRuleCommand { get; }
        public IRelayCommand DeleteRuleCommand { get; }
        public IRelayCommand MoveUpRuleCommand { get; }
        public IRelayCommand MoveDownRuleCommand { get; }
        public IRelayCommand AddDiscoveredToRulesCommand { get; }
        public IRelayCommand<DiscoveredStructureItem> AddDiscoveredItemCommand { get; }
        public IRelayCommand RefreshPreviewCommand { get; }
        public IRelayCommand SaveDqpCommand { get; }
        public IRelayCommand LoadDqpCommand { get; }
        public IRelayCommand AddDqpCommand { get; }
        public IRelayCommand DeleteDqpCommand { get; }
        public IRelayCommand MoveUpDqpCommand { get; }
        public IRelayCommand MoveDownDqpCommand { get; }
        public IRelayCommand ClearLogCommand { get; }
        public IRelayCommand CopyLogCommand { get; }

        // Regex cheat sheet snippets collection and commands
        public ObservableCollection<RegexSnippetItem> RegexSnippets { get; } = new ObservableCollection<RegexSnippetItem>();
        public IRelayCommand<string> InsertRegexSnippetCommand { get; }
        public IRelayCommand<string> CopyRegexSnippetCommand { get; }

        // Plan search regex cheat sheet snippets collection and commands
        public ObservableCollection<RegexSnippetItem> SearchRegexSnippets { get; } = new ObservableCollection<RegexSnippetItem>();
        public IRelayCommand<string> InsertSearchRegexSnippetCommand { get; }

        public MainViewModel() : this(null)
        {
        }

        public MainViewModel(SearchPresetService presetService = null)
        {
            _presetService = presetService ?? new SearchPresetService();
            _workerService = new StaEsapiWorkerService();

            // Initial output path configuration
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string datetext = DateTime.Now.ToString("yyyyMMddHHmmss");
            OutputFilePath = Path.Combine(baseDir, $"DataMiningOutput.{datetext}.csv");

            // Default DQP configuration
            DQPList.Add(new DQP
            {
                structureName = "*",
                DQPtype = DQPtype.Dose,
                DQPvalue = 95.0,
                InputUnit = IOUnit.Relative,
                OutputUnit = IOUnit.Absolute
            });

            // Command bindings
            SearchPlansCommand = new RelayCommand(async () => await ExecuteSearchPlansAsync(), () => IsNotRunning);
            SelectAllPlansCommand = new RelayCommand(() => ExecuteSelectAllPlans(true));
            UnselectAllPlansCommand = new RelayCommand(() => ExecuteSelectAllPlans(false));
            InvertPlanSelectionCommand = new RelayCommand(ExecuteInvertPlanSelection);

            RunPreScanCommand = new RelayCommand(async () => await ExecutePreScanAsync(), () => IsNotRunning);
            RunExtractionCommand = new RelayCommand(async () => await ExecuteExtractionAsync(), () => IsNotRunning);
            CancelCommand = new RelayCommand(ExecuteCancel, () => IsRunning && !IsCancelling);

            SelectOutputFolderCommand = new RelayCommand(ExecuteSelectOutputFile);
            OpenOutputFolderCommand = new RelayCommand(ExecuteOpenOutputFolder);
            SaveMappingRulesCommand = new RelayCommand(ExecuteSaveMappingRules);
            LoadMappingRulesCommand = new RelayCommand(ExecuteLoadMappingRules);
            AddRuleCommand = new RelayCommand(ExecuteAddRule);
            DeleteRuleCommand = new RelayCommand(ExecuteDeleteRule, () => SelectedRule != null);
            MoveUpRuleCommand = new RelayCommand(ExecuteMoveUpRule, CanMoveUpRule);
            MoveDownRuleCommand = new RelayCommand(ExecuteMoveDownRule, CanMoveDownRule);
            AddDiscoveredToRulesCommand = new RelayCommand(ExecuteAddDiscoveredToRules);
            AddDiscoveredItemCommand = new RelayCommand<DiscoveredStructureItem>(item => AddDiscoveredItemToRules(item));
            RefreshPreviewCommand = new RelayCommand(RefreshDiscoveredPreview);
            SaveDqpCommand = new RelayCommand(ExecuteSaveDqp);
            LoadDqpCommand = new RelayCommand(ExecuteLoadDqp);
            AddDqpCommand = new RelayCommand(ExecuteAddDqp);
            DeleteDqpCommand = new RelayCommand(ExecuteDeleteDqp, () => SelectedDqp != null);
            MoveUpDqpCommand = new RelayCommand(ExecuteMoveUpDqp, CanMoveUpDqp);
            MoveDownDqpCommand = new RelayCommand(ExecuteMoveDownDqp, CanMoveDownDqp);
            ClearLogCommand = new RelayCommand(() => LogText = string.Empty);
            CopyLogCommand = new RelayCommand(ExecuteCopyLog);
            InsertRegexSnippetCommand = new RelayCommand<string>(ExecuteInsertRegexSnippet);
            CopyRegexSnippetCommand = new RelayCommand<string>(ExecuteCopyRegexSnippet);
            InsertSearchRegexSnippetCommand = new RelayCommand<string>(ExecuteInsertSearchRegexSnippet);

            ClearDatesCommand = new RelayCommand(() =>
            {
                DateFrom = null;
                DateTo = null;
            });

            // Search preset command bindings
            SavePresetCommand = new RelayCommand(ExecuteSavePreset);
            DeletePresetCommand = new RelayCommand(ExecuteDeletePreset, () => SelectedPreset != null);

            // Initial load of search presets
            var loadedPresets = _presetService.LoadPresets();
            if (loadedPresets != null)
            {
                foreach (var p in loadedPresets)
                {
                    Presets.Add(p);
                }
            }

            // Initialize regex cheat sheet snippets
            InitializeRegexSnippets();
            InitializeSearchRegexSnippets();

            // Initialize collection view and filter for preview
            DiscoveredStructuresView = CollectionViewSource.GetDefaultView(DiscoveredStructures);
            if (DiscoveredStructuresView != null)
            {
                DiscoveredStructuresView.Filter = FilterDiscoveredItem;
            }
            ClearDiscoveredFilterCommand = new RelayCommand(() =>
            {
                DiscoveredFilterText = string.Empty;
                DiscoveredFilterStatus = "All";
            });

            // Automatically synchronize mapping rules edits with preview in real-time
            MappingRules.CollectionChanged += MappingRules_CollectionChanged;

            AppendLog("Application initialized.");
        }

        private bool FilterDiscoveredItem(object obj)
        {
            if (!(obj is DiscoveredStructureItem item)) return false;

            // 1. Text search (partial match against RawStructureId or ResolvedAlias)
            if (!string.IsNullOrWhiteSpace(DiscoveredFilterText))
            {
                string search = DiscoveredFilterText.Trim();
                bool matchesRaw = item.RawStructureId != null && item.RawStructureId.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
                bool matchesAlias = item.ResolvedAlias != null && item.ResolvedAlias.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
                if (!matchesRaw && !matchesAlias)
                {
                    return false;
                }
            }

            // 2. Status filter
            if (!string.IsNullOrEmpty(DiscoveredFilterStatus) && DiscoveredFilterStatus != "All")
            {
                string status = item.MatchStatus ?? string.Empty;
                if (DiscoveredFilterStatus == "Unmapped Only" && !status.StartsWith("Unmapped"))
                {
                    return false;
                }
                if (DiscoveredFilterStatus == "Mapped Only" && !status.StartsWith("Mapped"))
                {
                    return false;
                }
                if (DiscoveredFilterStatus == "Excluded Only" && !status.StartsWith("Excluded"))
                {
                    return false;
                }
            }

            return true;
        }

        public void AppendLog(string message)
        {
            LogText += $"[{DateTime.Now:HH:mm:ss}] {message}\n";
        }

        public void ApplyPreset(SearchPreset preset)
        {
            if (preset == null) return;
            PatientIdText = preset.PatientIdText ?? string.Empty;
            PatientIdMatchMode = preset.PatientIdMatchMode;

            CourseIdText = preset.CourseIdText ?? string.Empty;
            CourseIdMatchMode = preset.CourseIdMatchMode;

            PlanIdText = preset.PlanIdText ?? string.Empty;
            PlanIdMatchMode = preset.PlanIdMatchMode;

            TargetVolumeIdText = preset.TargetVolumeIdText ?? string.Empty;
            TargetVolumeMatchMode = preset.TargetVolumeMatchMode;

            DosePerFractionText = preset.DosePerFractionText ?? string.Empty;
            NumberOfFractionsText = preset.NumberOfFractionsText ?? string.Empty;
            TotalDoseText = preset.TotalDoseText ?? string.Empty;

            FilterUnapproved = preset.FilterUnapproved;
            FilterPlanApproved = preset.FilterPlanApproved;
            FilterTreatmentApproved = preset.FilterTreatmentApproved;

            GlobalLogicIsAnd = preset.GlobalLogicIsAnd;
            Options.ExportPlanSums = preset.IncludePlanSums;
            DosePresence = preset.DosePresence;

            MachineFilterText = preset.MachineFilterText ?? string.Empty;
            EnergyFilterText = preset.EnergyFilterText ?? string.Empty;
            TechniqueFilterText = preset.TechniqueFilterText ?? string.Empty;
            DateTarget = preset.DateTarget;
            DateFrom = preset.DateFrom;
            DateTo = preset.DateTo;
            PresetDescriptionInput = preset.Description ?? string.Empty;
        }

        public void ExecuteSavePreset()
        {
            string name = PresetNameInput?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Please enter a name for the preset.", "Save Preset", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var existing = Presets.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            var newPreset = new SearchPreset
            {
                Name = name,
                Description = PresetDescriptionInput?.Trim() ?? string.Empty,
                IsBuiltIn = false,
                PatientIdText = PatientIdText,
                PatientIdMatchMode = PatientIdMatchMode,
                CourseIdText = CourseIdText,
                CourseIdMatchMode = CourseIdMatchMode,
                PlanIdText = PlanIdText,
                PlanIdMatchMode = PlanIdMatchMode,
                TargetVolumeIdText = TargetVolumeIdText,
                TargetVolumeMatchMode = TargetVolumeMatchMode,
                DosePerFractionText = DosePerFractionText,
                NumberOfFractionsText = NumberOfFractionsText,
                TotalDoseText = TotalDoseText,
                FilterUnapproved = FilterUnapproved,
                FilterPlanApproved = FilterPlanApproved,
                FilterTreatmentApproved = FilterTreatmentApproved,
                GlobalLogicIsAnd = GlobalLogicIsAnd,
                IncludePlanSums = Options.ExportPlanSums,
                DosePresence = DosePresence,
                MachineFilterText = MachineFilterText,
                EnergyFilterText = EnergyFilterText,
                TechniqueFilterText = TechniqueFilterText,
                DateTarget = DateTarget,
                DateFrom = DateFrom,
                DateTo = DateTo
            };

            if (existing != null)
            {
                newPreset.FilePath = existing.FilePath;
                int index = Presets.IndexOf(existing);
                Presets[index] = newPreset;
            }
            else
            {
                Presets.Add(newPreset);
            }

            _selectedPreset = newPreset;
            OnPropertyChanged(nameof(SelectedPreset));
            (DeletePresetCommand as IRelayCommand)?.NotifyCanExecuteChanged();

            _presetService.SavePreset(newPreset);
            AppendLog($"Saved search preset: '{name}'");
        }

        public void ExecuteDeletePreset()
        {
            if (SelectedPreset == null) return;

            var result = MessageBox.Show($"Are you sure you want to delete preset '{SelectedPreset.Name}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                var targetPreset = SelectedPreset;
                string deletedName = targetPreset.Name;
                _presetService.DeletePreset(targetPreset);
                Presets.Remove(targetPreset);
                SelectedPreset = Presets.FirstOrDefault();
                AppendLog($"Deleted search preset: '{deletedName}'");
            }
        }

        public SearchFilterCriteria BuildCriteria()
        {
            SearchFilterCriteria.ParseTextFilter(PatientIdText, out var ptIncludes, out var ptExcludes);
            SearchFilterCriteria.ParseTextFilter(CourseIdText, out var crsIncludes, out var crsExcludes);
            SearchFilterCriteria.ParseTextFilter(PlanIdText, out var plnIncludes, out var plnExcludes);
            SearchFilterCriteria.ParseTextFilter(TargetVolumeIdText, out var tgtIncludes, out var tgtExcludes);

            SearchFilterCriteria.ParseTextFilter(MachineFilterText, out var machIncludes, out var machExcludes);
            SearchFilterCriteria.ParseTextFilter(EnergyFilterText, out var nrgIncludes, out var nrgExcludes);
            SearchFilterCriteria.ParseTextFilter(TechniqueFilterText, out var techIncludes, out var techExcludes);

            var dpfCriteria = NumericFilterCriteria.Parse(DosePerFractionText);
            var nofCriteria = NumericFilterCriteria.Parse(NumberOfFractionsText);
            var tdCriteria = NumericFilterCriteria.Parse(TotalDoseText);

            double.TryParse(DosePerFractionText, out double dpf);
            int.TryParse(NumberOfFractionsText, out int nof);
            double.TryParse(TotalDoseText, out double td);

            return new SearchFilterCriteria
            {
                PatientIdFilter = ptIncludes,
                PatientIdExcludeFilter = ptExcludes,
                PatientIdMatchMode = PatientIdMatchMode,

                CourseIdFilter = crsIncludes,
                CourseIdExcludeFilter = crsExcludes,
                CourseIdMatchMode = CourseIdMatchMode,

                PlanIdFilter = plnIncludes,
                PlanIdExcludeFilter = plnExcludes,
                PlanIdMatchMode = PlanIdMatchMode,

                TargetVolumeIdFilter = tgtIncludes,
                TargetVolumeExcludeFilter = tgtExcludes,
                TargetVolumeMatchMode = TargetVolumeMatchMode,

                DosePerFractionCriteria = dpfCriteria,
                NumberOfFractionsCriteria = nofCriteria,
                TotalDoseCriteria = tdCriteria,

                DosePerFractionGy = dpf > 0 ? dpf : (double?)null,
                NumberOfFractions = nof > 0 ? nof : (int?)null,
                TotalDoseGy = td > 0 ? td : (double?)null,

                FilterUnapproved = FilterUnapproved,
                FilterPlanApproved = FilterPlanApproved,
                FilterTreatmentApproved = FilterTreatmentApproved,
                GlobalLogicIsAnd = GlobalLogicIsAnd,
                IncludePlanSums = Options.ExportPlanSums,
                DosePresence = DosePresence,

                MachineFilter = machIncludes,
                MachineExcludeFilter = machExcludes,
                EnergyFilter = nrgIncludes,
                EnergyExcludeFilter = nrgExcludes,
                TechniqueFilter = techIncludes,
                TechniqueExcludeFilter = techExcludes,

                DateTarget = DateTarget,
                DateFrom = DateFrom,
                DateTo = DateTo
            };
        }

        private async Task ExecuteSearchPlansAsync()
        {
            IsRunning = true;
            IsCancelling = false;
            _cts = new CancellationTokenSource();
            ProgressPercentage = 0;
            ProgressText = "Searching plans based on criteria...";
            AppendLog("Starting plan search & filter scan...");

            var criteria = BuildCriteria();
            var progress = new Progress<ExtractionProgressInfo>(p =>
            {
                ProgressPercentage = p.Percentage;
                ProgressText = p.Message;
                if (!string.IsNullOrEmpty(p.Message) && p.Percentage % 10 == 0)
                {
                    AppendLog(p.Message);
                }
            });

            try
            {
                var results = await _workerService.SearchPlansAsync(criteria, progress, _cts.Token);

                // Unregister existing listeners
                foreach (var item in MatchedPlans)
                {
                    item.PropertyChanged -= MatchedPlanItem_PropertyChanged;
                }

                MatchedPlans.Clear();
                foreach (var item in results)
                {
                    item.PropertyChanged += MatchedPlanItem_PropertyChanged;
                    MatchedPlans.Add(item);
                }

                UpdateMatchedPlansSummary();
                SelectedTabIndex = 0; // Focus Plan Search tab

                ProgressText = $"Search completed. {results.Count} plans matched.";
                AppendLog($"Plan search completed: {results.Count} plans found matching filter criteria.");
            }
            catch (OperationCanceledException)
            {
                ProgressText = "Plan search cancelled.";
                AppendLog("Plan search was cancelled by user.");
            }
            catch (Exception ex)
            {
                ProgressText = "Plan search failed.";
                AppendLog($"Plan search error: {ex.Message}");
                MessageBox.Show($"Plan search error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsRunning = false;
                IsCancelling = false;
                _cts?.Dispose();
                _cts = null;
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }

        private void MatchedPlanItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MatchedPlanItem.IsSelected))
            {
                UpdateMatchedPlansSummary();
            }
        }

        public void UpdateMatchedPlansSummary()
        {
            int total = MatchedPlans.Count;
            int selected = MatchedPlans.Count(p => p.IsSelected);
            MatchedPlansSummaryText = $"Selected: {selected} / {total} Plans";
            HasMatchedPlans = total > 0;
            OnPropertyChanged(nameof(MatchedPlansCount));
            OnPropertyChanged(nameof(PreScanScopeBadgeText));
        }

        public void ExecuteSelectAllPlans(bool select)
        {
            foreach (var item in MatchedPlans)
            {
                item.IsSelected = select;
            }
            UpdateMatchedPlansSummary();
        }

        public void ExecuteInvertPlanSelection()
        {
            foreach (var item in MatchedPlans)
            {
                item.IsSelected = !item.IsSelected;
            }
            UpdateMatchedPlansSummary();
        }

        private async Task ExecutePreScanAsync()
        {
            IsRunning = true;
            IsCancelling = false;
            _cts = new CancellationTokenSource();
            ProgressPercentage = 0;
            ProgressText = "Starting pre-scan...";
            AppendLog("Starting structure pre-scan...");

            var criteria = BuildCriteria();
            var progress = new Progress<ExtractionProgressInfo>(p =>
            {
                ProgressPercentage = p.Percentage;
                ProgressText = p.Message;
                if (!string.IsNullOrEmpty(p.Message)) AppendLog(p.Message);
            });

            HashSet<string> targetPlanKeys = null;
            if (MatchedPlans.Count > 0)
            {
                targetPlanKeys = new HashSet<string>(MatchedPlans.Where(p => p.IsSelected).Select(p => p.UniqueKey));
                if (targetPlanKeys.Count == 0)
                {
                    MessageBox.Show(
                        "No treatment plans are selected for extraction in the Plan Search results table.\nPlease check at least one plan or clear the plan table to scan all matching plans.",
                        "No Plans Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                    ProgressText = "Ready";
                    AppendLog("Pre-scan aborted: no plans selected in the table.");
                    IsRunning = false;
                    _cts?.Dispose();
                    _cts = null;
                    return;
                }
                AppendLog($"Pre-scanning structures for {targetPlanKeys.Count} selected plans (from Plan Search table)...");
            }
            else
            {
                AppendLog("Pre-scanning structures for all plans matching search criteria...");
            }

            try
            {
                var discovered = await _workerService.RunPreScanAsync(criteria, MappingRules, progress, _cts.Token, targetPlanKeys);
                DiscoveredStructures.Clear();
                foreach (var d in discovered)
                {
                    DiscoveredStructures.Add(d);
                }
                DiscoveredStructuresView?.Refresh();
                OnPropertyChanged(nameof(FilteredDiscoveredCount));
                string scopeSuffix = targetPlanKeys != null ? $" from {targetPlanKeys.Count} selected plans" : "";
                ProgressText = $"Pre-scan completed. {DiscoveredStructures.Count} structures found{scopeSuffix}.";
                AppendLog($"Pre-scan completed. {DiscoveredStructures.Count} unique structures mapped{scopeSuffix}.");
            }
            catch (OperationCanceledException)
            {
                ProgressText = "Pre-scan cancelled.";
                AppendLog("Pre-scan was cancelled by user.");
            }
            catch (Exception ex)
            {
                ProgressText = "Pre-scan failed.";
                AppendLog($"Pre-scan error: {ex.Message}");
                MessageBox.Show($"Pre-scan error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsRunning = false;
                IsCancelling = false;
                _cts?.Dispose();
                _cts = null;
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }

        private async Task ExecuteExtractionAsync()
        {
            // Safety confirmation when Bypass Structure Pre-Scan is unchecked and mapping rules are empty
            if (!Options.BypassPreScan && MappingRules.Count == 0)
            {
                var confirm = MessageBox.Show(
                    "No structure mapping rules are configured and structure pre-scan has not been executed.\n\n" +
                    "Do you want to proceed directly with data extraction?\n\n" +
                    "• [Yes] : Bypass pre-scan and extract directly with structure names specified in DQP list.\n" +
                    "• [No]  : Abort extraction. (Run 'Pre-Scan Structures' in Tab 2 or enable 'Bypass Structure Pre-Scan' in Tab 4)",
                    "Confirm Unmapped Extraction",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes)
                {
                    AppendLog("Extraction aborted: unmapped extraction cancelled by user.");
                    ProgressText = "Ready";
                    return;
                }
            }

            IsRunning = true;
            IsCancelling = false;
            _cts = new CancellationTokenSource();
            ProgressPercentage = 0;
            ProgressText = "Starting data mining...";
            AppendLog($"Starting extraction -> {OutputFilePath}");

            var criteria = BuildCriteria();
            var progress = new Progress<ExtractionProgressInfo>(p =>
            {
                ProgressPercentage = p.Percentage;
                ProgressText = p.Message;
                if (!string.IsNullOrEmpty(p.Message) && p.Percentage % 10 == 0)
                {
                    AppendLog(p.Message);
                }
            });

            try
            {
                HashSet<string> targetPlanKeys = null;
                if (MatchedPlans.Count > 0)
                {
                    targetPlanKeys = new HashSet<string>(MatchedPlans.Where(p => p.IsSelected).Select(p => p.UniqueKey));
                    if (targetPlanKeys.Count == 0)
                    {
                        MessageBox.Show(
                            "No treatment plans are selected in the search results.\nPlease check target plans in the Plan Search results table.",
                            "No Plans Selected",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        ProgressText = "Ready";
                        AppendLog("Extraction aborted: no plans selected in the table.");
                        return;
                    }
                    AppendLog($"Filtering extraction to {targetPlanKeys.Count} selected plans (out of {MatchedPlans.Count}).");
                }

                int count = await _workerService.RunExtractionAsync(
                    criteria,
                    Options,
                    DQPList,
                    MappingRules,
                    OutputFilePath,
                    progress,
                    _cts.Token,
                    targetPlanKeys);

                ProgressText = $"Completed! {count} plans extracted.";
                AppendLog($"Extraction successfully completed. Total plans: {count}");
                MessageBox.Show($"Data mining completed.\nTotal extracted plans: {count}\nFile: {OutputFilePath}", "Completed", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (OperationCanceledException)
            {
                ProgressText = "Extraction cancelled.";
                AppendLog("Extraction was cancelled by user. Partial data saved.");
            }
            catch (Exception ex)
            {
                ProgressText = "Extraction failed.";
                AppendLog($"Extraction error: {ex.Message}");
                MessageBox.Show($"Extraction error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsRunning = false;
                IsCancelling = false;
                _cts?.Dispose();
                _cts = null;
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }

        private void ExecuteCancel()
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                IsCancelling = true;
                _cts.Cancel();
                AppendLog("Cancellation requested by user. Aborting ESAPI worker operation...");
                ProgressText = "Cancelling...";
                (CancelCommand as IRelayCommand)?.NotifyCanExecuteChanged();
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }

        private void ExecuteSelectOutputFile()
        {
            var sfd = new SaveFileDialog
            {
                FileName = Path.GetFileName(OutputFilePath),
                InitialDirectory = Path.GetDirectoryName(OutputFilePath),
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Select Output CSV Path"
            };

            if (sfd.ShowDialog() == true)
            {
                OutputFilePath = sfd.FileName;
                AppendLog($"Output path changed: {OutputFilePath}");
            }
        }

        private void ExecuteOpenOutputFolder()
        {
            try
            {
                string dir = Path.GetDirectoryName(OutputFilePath);
                if (string.IsNullOrWhiteSpace(dir))
                {
                    dir = AppDomain.CurrentDomain.BaseDirectory;
                }
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = dir,
                    UseShellExecute = true,
                    Verb = "open"
                });
                AppendLog($"[Info] Opened output directory: {dir}");
            }
            catch (Exception ex)
            {
                AppendLog($"[Warning] Failed to open output folder: {ex.Message}");
            }
        }

        private void ExecuteSaveMappingRules()
        {
            var sfd = new SaveFileDialog
            {
                FileName = "StructureMappingRules.json",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                Title = "Save Structure Mapping Rules"
            };

            if (sfd.ShowDialog() == true)
            {
                StructureMappingService.SaveRulesToFile(sfd.FileName, MappingRules);
                AppendLog($"Mapping rules saved: {sfd.FileName}");
                MessageBox.Show("Structure mapping rules saved successfully.", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ExecuteLoadMappingRules()
        {
            var ofd = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                Title = "Load Structure Mapping Rules"
            };

            if (ofd.ShowDialog() == true)
            {
                var rules = StructureMappingService.LoadRulesFromFile(ofd.FileName);
                MappingRules.Clear();
                foreach (var r in rules)
                {
                    MappingRules.Add(r);
                }
                SelectedRule = MappingRules.FirstOrDefault();
                UpdateRuleCommandsCanExecute();
                AppendLog($"Loaded {MappingRules.Count} mapping rules from {ofd.FileName}");
                RefreshDiscoveredPreview();
            }
        }

        private void ExecuteAddRule()
        {
            var newRule = new StructureMappingRule
            {
                Pattern = "StructureName",
                MatchMode = StructureMatchMode.Exact,
                TargetAlias = "StructureName",
                IsSelected = true
            };
            MappingRules.Add(newRule);
            SelectedRule = newRule;
            UpdateRuleCommandsCanExecute();
            RefreshDiscoveredPreview();
        }

        private void ExecuteDeleteRule()
        {
            if (SelectedRule != null)
            {
                int index = MappingRules.IndexOf(SelectedRule);
                MappingRules.Remove(SelectedRule);
                RefreshDiscoveredPreview();

                if (MappingRules.Count > 0)
                {
                    int nextIndex = Math.Min(index, MappingRules.Count - 1);
                    SelectedRule = MappingRules[nextIndex];
                }
                else
                {
                    SelectedRule = null;
                }
                UpdateRuleCommandsCanExecute();
            }
        }

        private bool CanMoveUpRule()
        {
            if (SelectedRule == null) return false;
            int index = MappingRules.IndexOf(SelectedRule);
            return index > 0;
        }

        public void ExecuteMoveUpRule()
        {
            if (!CanMoveUpRule()) return;
            int index = MappingRules.IndexOf(SelectedRule);
            if (index <= 0) return;

            var item = SelectedRule;
            MappingRules.Move(index, index - 1);
            _selectedRule = null;
            SelectedRule = item;
            UpdateRuleCommandsCanExecute();
            RefreshDiscoveredPreview();
        }

        private bool CanMoveDownRule()
        {
            if (SelectedRule == null) return false;
            int index = MappingRules.IndexOf(SelectedRule);
            return index >= 0 && index < MappingRules.Count - 1;
        }

        public void ExecuteMoveDownRule()
        {
            if (!CanMoveDownRule()) return;
            int index = MappingRules.IndexOf(SelectedRule);
            if (index < 0 || index >= MappingRules.Count - 1) return;

            var item = SelectedRule;
            MappingRules.Move(index, index + 1);
            _selectedRule = null;
            SelectedRule = item;
            UpdateRuleCommandsCanExecute();
            RefreshDiscoveredPreview();
        }

        private bool CanMoveUpDqp()
        {
            if (SelectedDqp == null) return false;
            int index = DQPList.IndexOf(SelectedDqp);
            return index > 0;
        }

        public void ExecuteMoveUpDqp()
        {
            if (!CanMoveUpDqp()) return;
            int index = DQPList.IndexOf(SelectedDqp);
            if (index <= 0) return;

            var item = SelectedDqp;
            DQPList.Move(index, index - 1);
            _selectedDqp = null;
            SelectedDqp = item;
            UpdateDqpCommandsCanExecute();
        }

        private bool CanMoveDownDqp()
        {
            if (SelectedDqp == null) return false;
            int index = DQPList.IndexOf(SelectedDqp);
            return index >= 0 && index < DQPList.Count - 1;
        }

        public void ExecuteMoveDownDqp()
        {
            if (!CanMoveDownDqp()) return;
            int index = DQPList.IndexOf(SelectedDqp);
            if (index < 0 || index >= DQPList.Count - 1) return;

            var item = SelectedDqp;
            DQPList.Move(index, index + 1);
            _selectedDqp = null;
            SelectedDqp = item;
            UpdateDqpCommandsCanExecute();
        }

        public void ExecuteInsertRegexSnippet(string pattern)
        {
            if (string.IsNullOrEmpty(pattern)) return;

            if (SelectedRule == null)
            {
                ExecuteAddRule();
            }

            if (SelectedRule != null)
            {
                SelectedRule.Pattern = pattern;
                SelectedRule.MatchMode = StructureMatchMode.Regex;
                SelectedRule.ValidateRegex();
                RefreshDiscoveredPreview();
                AppendLog($"Applied regex snippet: {pattern}");
            }
        }

        public void ExecuteCopyRegexSnippet(string pattern)
        {
            if (!string.IsNullOrEmpty(pattern))
            {
                try
                {
                    Clipboard.SetText(pattern);
                    AppendLog($"Copied regex snippet to clipboard: {pattern}");
                }
                catch { }
            }
        }

        private void InitializeRegexSnippets()
        {
            RegexSnippets.Clear();
            RegexSnippets.Add(new RegexSnippetItem
            {
                Pattern = "^PTV.*",
                Title = "Prefix Match (Prefix)",
                Description = "Matches structure names starting with 'PTV'",
                Example = "PTV_High, PTV70, PTV_boost"
            });
            RegexSnippets.Add(new RegexSnippetItem
            {
                Pattern = ".*_PTV$",
                Title = "Suffix Match (Suffix)",
                Description = "Matches structure names ending with '_PTV'",
                Example = "Boost_PTV, Total_PTV"
            });
            RegexSnippets.Add(new RegexSnippetItem
            {
                Pattern = ".*[_-](Rt|Lt|R|L)$",
                Title = "Laterality Suffix",
                Description = "Matches laterality indicators at the end (_Rt, _Lt, -R, -L)",
                Example = "Parotid_Rt, Kidney-L, Lens_R"
            });
            RegexSnippets.Add(new RegexSnippetItem
            {
                Pattern = "^(Lt|Rt|L|R)[_-].*",
                Title = "Laterality Prefix",
                Description = "Matches laterality indicators at the start (Rt_, Lt_, R_, L_)",
                Example = "Rt_Lung, L_OpticNerve, R_Eye"
            });
            RegexSnippets.Add(new RegexSnippetItem
            {
                Pattern = "(Bladder|Rectum)",
                Title = "Any of Multiple Names",
                Description = "Matches any of the pipe-delimited structure names",
                Example = "Bladder, Rectum"
            });
            RegexSnippets.Add(new RegexSnippetItem
            {
                Pattern = @".*_\d+(Gy)?$",
                Title = "Dose Pattern Suffix",
                Description = "Matches underscore followed by numbers and optional Gy",
                Example = "CTV_60Gy, PTV_50, GTV_70Gy"
            });
            RegexSnippets.Add(new RegexSnippetItem
            {
                Pattern = "(?i)chiasm",
                Title = "Case-Insensitive Match",
                Description = "Prefix with (?i) for case-insensitive matching",
                Example = "chiasm, CHIASM, Chiasm"
            });
            RegexSnippets.Add(new RegexSnippetItem
            {
                Pattern = ".*",
                Title = "Wildcard Any",
                Description = "Matches any characters (all structures)",
                Example = "All structures"
            });
        }

        private void ExecuteInsertSearchRegexSnippet(string pattern)
        {
            if (string.IsNullOrEmpty(pattern)) return;
            PlanIdText = pattern;
            PlanIdMatchMode = TextMatchMode.Regex;
            AppendLog($"Applied regex pattern to Plan ID: {pattern} (Mode: Regex)");
        }

        private void InitializeSearchRegexSnippets()
        {
            SearchRegexSnippets.Clear();
            SearchRegexSnippets.Add(new RegexSnippetItem
            {
                Pattern = "(VMAT|IMRT)",
                Title = "Any of Multiple Techniques",
                Description = "Matches plans containing any pipe-delimited term (e.g., VMAT or IMRT)",
                Example = "Prostate_VMAT, Pelvis_IMRT"
            });
            SearchRegexSnippets.Add(new RegexSnippetItem
            {
                Pattern = @".*_(Boost|Main|Total)$",
                Title = "Plan Suffix",
                Description = "Matches plans ending with _Boost, _Main, or _Total",
                Example = "Breast_Boost, Prostate_Main, Brain_Total"
            });
            SearchRegexSnippets.Add(new RegexSnippetItem
            {
                Pattern = "^(Prostate|Lung|Brain|Head|Pelvis).*",
                Title = "Disease Site Prefix",
                Description = "Matches plans starting with specified disease site",
                Example = "Prostate_78Gy, Lung_SBRT, Brain_SRS"
            });
            SearchRegexSnippets.Add(new RegexSnippetItem
            {
                Pattern = @".*_\d+$",
                Title = "Numbered Plan",
                Description = "Matches plans ending with an underscore and numbers",
                Example = "Plan_1, Prostate_02, QA_1"
            });
            SearchRegexSnippets.Add(new RegexSnippetItem
            {
                Pattern = @"PTV.*(70|78)",
                Title = "Target & Dose",
                Description = "Matches target volume containing PTV and 70 or 78",
                Example = "PTV_78Gy, PTV70, PTV_boost78"
            });
            SearchRegexSnippets.Add(new RegexSnippetItem
            {
                Pattern = @"^((?!QA|Verify|Test).)*$",
                Title = "Exclude Keywords (Negative Lookahead)",
                Description = "Matches clinical plans excluding QA, Verify, or Test",
                Example = "Extract clinical treatment plans only"
            });
            SearchRegexSnippets.Add(new RegexSnippetItem
            {
                Pattern = "(?i)vmat",
                Title = "Case-Insensitive Match",
                Description = "Prefix with (?i) for case-insensitive matching",
                Example = "vmat, VMAT, Vmat"
            });
            SearchRegexSnippets.Add(new RegexSnippetItem
            {
                Pattern = ".*",
                Title = "Wildcard Any",
                Description = "Matches all plan identifiers",
                Example = "All plans"
            });
        }

        private void ExecuteAddDiscoveredToRules()
        {
            if (SelectedDiscoveredItem != null)
            {
                AddDiscoveredItemToRules(SelectedDiscoveredItem);
            }
            else
            {
                // Bulk add unmapped structures
                int addedCount = 0;
                foreach (var item in DiscoveredStructures.Where(d => d.MatchStatus.StartsWith("Unmapped")))
                {
                    if (!MappingRules.Any(r => r.Pattern.Equals(item.RawStructureId, StringComparison.OrdinalIgnoreCase) && r.MatchMode == StructureMatchMode.Exact))
                    {
                        MappingRules.Add(new StructureMappingRule
                        {
                            Pattern = item.RawStructureId,
                            MatchMode = StructureMatchMode.Exact,
                            TargetAlias = item.RawStructureId,
                            IsSelected = true
                        });
                        addedCount++;
                    }
                }
                SelectedRule = MappingRules.LastOrDefault();
                UpdateRuleCommandsCanExecute();
                AppendLog($"Added {addedCount} unmapped structures to mapping rules.");
                RefreshDiscoveredPreview();
            }
        }

        /// <summary>
        /// Adds a preview item (or double-clicked item) to rule definitions in the left pane.
        /// </summary>
        public void AddDiscoveredItemToRules(DiscoveredStructureItem item)
        {
            if (item == null) return;

            string rawId = item.RawStructureId;
            if (string.IsNullOrWhiteSpace(rawId)) return;

            var existing = MappingRules.FirstOrDefault(r => r.Pattern.Equals(rawId, StringComparison.OrdinalIgnoreCase) && r.MatchMode == StructureMatchMode.Exact);
            if (existing != null)
            {
                SelectedRule = existing;
                AppendLog($"[Info] Exact rule for '{rawId}' already exists.");
                return;
            }

            var rule = new StructureMappingRule
            {
                Pattern = rawId,
                MatchMode = StructureMatchMode.Exact,
                TargetAlias = rawId,
                IsSelected = true
            };
            MappingRules.Add(rule);
            SelectedRule = rule;
            AppendLog($"Added rule for '{rule.Pattern}' from scan result.");
            RefreshDiscoveredPreview();
        }

        private readonly HashSet<StructureMappingRule> _subscribedRules = new HashSet<StructureMappingRule>();
        private bool _isRefreshingPreview = false;

        private void MappingRules_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
            {
                foreach (var rule in _subscribedRules)
                {
                    rule.PropertyChanged -= Rule_PropertyChanged;
                }
                _subscribedRules.Clear();
                foreach (var rule in MappingRules)
                {
                    rule.PropertyChanged += Rule_PropertyChanged;
                    _subscribedRules.Add(rule);
                }
            }
            else
            {
                if (e.OldItems != null)
                {
                    foreach (StructureMappingRule rule in e.OldItems)
                    {
                        rule.PropertyChanged -= Rule_PropertyChanged;
                        _subscribedRules.Remove(rule);
                    }
                }
                if (e.NewItems != null)
                {
                    foreach (StructureMappingRule rule in e.NewItems)
                    {
                        if (_subscribedRules.Add(rule))
                        {
                            rule.PropertyChanged += Rule_PropertyChanged;
                        }
                    }
                }
            }
            UpdateRuleCommandsCanExecute();
            RefreshDiscoveredPreview();
        }

        private void Rule_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(StructureMappingRule.Pattern) ||
                e.PropertyName == nameof(StructureMappingRule.MatchMode) ||
                e.PropertyName == nameof(StructureMappingRule.TargetAlias) ||
                e.PropertyName == nameof(StructureMappingRule.IsSelected))
            {
                RefreshDiscoveredPreview();
            }
        }

        public void RefreshDiscoveredPreview()
        {
            if (_isRefreshingPreview) return;
            try
            {
                _isRefreshingPreview = true;
                StructureMappingService.RefreshPreview(DiscoveredStructures, MappingRules);
                DiscoveredStructuresView?.Refresh();
                OnPropertyChanged(nameof(FilteredDiscoveredCount));
            }
            finally
            {
                _isRefreshingPreview = false;
            }
        }

        private void ExecuteSaveDqp()
        {
            var sfd = new SaveFileDialog
            {
                FileName = "DQPlist.csv",
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Save Dose Quality Parameters"
            };

            if (sfd.ShowDialog() == true)
            {
                using (var sw = new StreamWriter(sfd.FileName, false))
                {
                    sw.WriteLine("structureName,DQPtype,DQPvalue,InputUnit,OutputUnit");
                    foreach (var dqp in DQPList)
                    {
                        sw.WriteLine($"{dqp.structureName},{dqp.DQPtype},{dqp.DQPvalue},{dqp.InputUnit},{dqp.OutputUnit}");
                    }
                }
                AppendLog($"Saved DQP list to {sfd.FileName}");
                MessageBox.Show("DQP parameters saved.", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ExecuteLoadDqp()
        {
            var ofd = new OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Load Dose Quality Parameters"
            };

            if (ofd.ShowDialog() == true)
            {
                var lines = File.ReadAllLines(ofd.FileName);
                DQPList.Clear();
                for (int i = 1; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var parts = line.Split(',');
                    if (parts.Length >= 5)
                    {
                        Enum.TryParse(parts[1].Trim(), out DQPtype dqpType);
                        double.TryParse(parts[2].Trim(), out double val);
                        Enum.TryParse(parts[3].Trim(), out IOUnit inUnit);
                        Enum.TryParse(parts[4].Trim(), out IOUnit outUnit);

                        DQPList.Add(new DQP
                        {
                            structureName = parts[0].Trim(),
                            DQPtype = dqpType,
                            DQPvalue = val,
                            InputUnit = inUnit,
                            OutputUnit = outUnit
                        });
                    }
                }
                SelectedDqp = DQPList.FirstOrDefault();
                UpdateDqpCommandsCanExecute();
                AppendLog($"Loaded {DQPList.Count} DQP items from {ofd.FileName}");
            }
        }

        private void ExecuteAddDqp()
        {
            var newDqp = new DQP
            {
                structureName = "TargetStructure",
                DQPtype = DQPtype.Dose,
                DQPvalue = 95.0,
                InputUnit = IOUnit.Relative,
                OutputUnit = IOUnit.Absolute
            };
            DQPList.Add(newDqp);
            SelectedDqp = newDqp;
            UpdateDqpCommandsCanExecute();
        }

        private void ExecuteDeleteDqp()
        {
            if (SelectedDqp != null)
            {
                int index = DQPList.IndexOf(SelectedDqp);
                DQPList.Remove(SelectedDqp);

                if (DQPList.Count > 0)
                {
                    int nextIndex = Math.Min(index, DQPList.Count - 1);
                    SelectedDqp = DQPList[nextIndex];
                }
                else
                {
                    SelectedDqp = null;
                }
                UpdateDqpCommandsCanExecute();
            }
        }

        private void ExecuteCopyLog()
        {
            if (!string.IsNullOrEmpty(LogText))
            {
                try
                {
                    Clipboard.SetText(LogText);
                    AppendLog("[Info] Console log copied to clipboard.");
                }
                catch { }
            }
        }
    }
}
