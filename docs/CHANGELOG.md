# Changelog

All notable changes to this project will be documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [3.0.0] - 2026-09-26

### Added
- **検索結果テーブルに「Dose」列（線量有無）を追加**: Plan Search 結果の DataGrid に線量計算の有無を ✔ / — で表示する「Dose」列を新設。PlanSetup は `TotalDose > 0` で判定、PlanSum は `sum.Dose != null` で判定。
- **リリースバイナリへのバージョン番号付与**: `release/` フォルダに配備される実行ファイルを `EclipseDataMiner_v3.0.0.exe` にリネーム。バージョンの識別を容易に。
- **PDF マニュアルの release/ 自動同期**: `test.bat` の Release Artifacts 同期ステップに PDF マニュアル (`EclipseDataMiner_v3.0.0_Manual.pdf` / `EclipseDataMiner_Manual.pdf`) の自動コピーを追加。

### Changed
- **バージョン 3.0.0 へのメジャーアップデート**: AssemblyVersion、ProductVersion (`AssemblyInformationalVersion("3.0.0")`)、UI タイトル、全ドキュメントのバージョン表記を v3.0.0 に統一。

## [2.4.0] - 2026-09-25

### Added
- **Advanced Filters for Beam Parameters & Date Range (Proposal 3)**:
  - 検索条件カード下部に折りたたみ式パネル **「▾ Advanced Filters (Machine, Energy, Technique, Date Range)」** を新設。
  - **照射パラメータ（Machine ID, Energy, Technique）**:
    - 通常照射ビーム（セットアップビームを除外）を対象とした柔軟なテキスト検索。カンマ区切りによる複数 OR 検索、および `!` や `-` による NOT 除外指定（例: `TrueBeam, Clinac, !QA_Linac`, `6X, 10X, !6FFF`, `ARC, !STATIC`）に対応。
  - **日付範囲フィルタ（Date Target & Date Range）**:
    - 判定基準とする日付を3種から選択可能（`TreatmentApprovalDate` [治療承認日・デフォルト]、`PlanningApprovalDate` [計画承認日]、`CreationDate` [作成日]）。
    - カレンダーピッカー（`DatePicker`）による開始日（From）〜終了日（To）の範囲指定（片側指定対応）、および「✕ Clear」ボタンによる一括リセットを実装。
  - **検索プリセット完全連動**:
    - 上記すべての高度フィルタ設定値を「🔖 Preset」の保存・復元に完全統合。
- **Automated Startup Smoke Testing Pipeline & XAML Resource Integrity Guard**:
  - `test.bat` に第5ステップ **「[5/5] Smoke Testing Release Binary Startup」** を新設。ビルドおよび単体テスト完了後に、配備された実 EXE（`release\EclipseDataMiner.exe`）をサブプロセス起動し、起動クラッシュを起こさず正常稼働し続けることを自動検証。
  - `XamlResourceIntegrity_ShouldHaveNoMissingStaticResources` 単体テストを追加。XAML 内で使用される全 `StaticResource` がリソース定義に存在するかを自動走査し、実行時パース例外を未然に防止。
- **Folder-Based Search Preset System (App-Relative Presets/ Directory)**:
  - 検索カード最上段に `🔖 Preset:` ツールバーを配備。疾患別・プロトコル別の検索条件セットをワンクリックで瞬時に保存・適用・削除可能。
  - **アプリ相対 `Presets/` フォルダ管理**: プリセットの保存先を `%APPDATA%` から **アプリ本体と同じ階層の `Presets/` フォルダ**（`Path.Combine(BaseDirectory, "Presets")`）へ変更。各プリセットを個別の JSON ファイル（例: `Presets/{PresetName}.json`）として管理し、エクスプローラーからの直接追加・バックアップ・共有が容易。
  - **組み込み制限の完全撤廃と自由な削除**: コード組み込みプリセット（`IsBuiltIn`）の強制ロードや削除・上書き保護を完全撤廃。ユーザーが UI 上からすべてのプリセットを自由に削除・上書き可能に改善。
  - **臨床プロトコルサンプル同梱**: `Templates/Presets/` に頻出臨床プロトコル（前立腺78Gy、肺SBRT、頭頸部70Gy、全承認計画）のサンプル JSON を同梱。必要に応じて `Presets/` へコピーして利用可能。
- **Numerical Range & Inequality Filtering (Dose/Fr, Fractions, Total Dose)**:
  - 線量および分割数入力欄において、単一値（`78`, `2.0`）に加え、**範囲指定**（`70-80`, `70~80` 等の両端含む判定）および**不等号**（`>=10`, `>10`, `<=30`, `<30` 等）による柔軟な条件判定エンジン（`NumericFilterCriteria`）を新設。
- **NOT / Exclude Filters for Text Fields (`!QA`, `!Test`)**:
  - Patient ID, Course ID, Plan ID, Target Volume において、カンマ区切りトークンの先頭に `!` または `-` を付与することで除外指定が可能（`VMAT, IMRT, !QA, !Test` 等の包含＋除外の混在、および `!QA` 単独でのQA除外全件抽出に完全対応）。
- **Modern Slate & Ocean Cyan Design System**: 医療・研究現場に最適化された高視認性テーマ（カードスタイル、ボタンスタイル、DataGrid 余白・ヘッダー、角丸プログレスバー）を集中定義。
- **Streamlined Single-Row Action Bar**: 余剰ヘッダーを整理し、抽出実行・中止・出力パス指定を 1 行に集約した実用的アクションバーへ簡素化。
- **Pre-Scan Cancel Support**: 事前スキャン中にも即座に処理を中断できるよう、Tab 1 ツールバーにも「⏹ Cancel」ボタンを配備。
- **2-Pane Structure Pre-Scan & Mapping**: 左右スプリッター（`GridSplitter`）によるルール定義とプレビューの完全分離。特異度優先探索（`Exact` > `Contains` > `Regex`）、順序入替（▲/▼）、Target Alias 重複時の集約件数自動注記（`[統合: N件]`）。
- **DQP Information Card & Button Operations**: 基本統計量自動出力通知カードおよび [+ Add DQP] / [- Delete Selected] 操作ボタン。
- **Dark Terminal Console & True Black Theme**: TextBox の `IsReadOnly` トリガーによる白背景化を根本修正し、完全な黒背景（`#090D16`）とハイコントラスト等幅フォント（`#F1F5F9`, `Consolas`）によるプロフェッショナル・コンソールに刷新。
- **Preview Quick Filter Bar**: プレビュー画面にリアルタイムテキスト検索、ステータス別絞り込み（`All`, `Unmapped Only`, `Mapped Only`, `Excluded Only`）、絞り込み件数バッジ（`Showing: X/Y`）、クリアボタンを追加。
- **Preview Row Double-Click Addition**: プレビュー行のダブルクリックによる即時ルール登録および重複防止選択機能を追加。
- **Clinical Regex Snippets & Real-Time Syntax Validation**:
  - ルール定義ツールバーに「❓ Regex Hints ▾」チートシートポップアップを配備。前方一致（`^PTV.*`）、左右識別（`.*[_-](Rt|Lt)$`）、OR結合（`(Bladder|Rectum)`）、線量表記（`.*_\d+Gy$`）等の臨床頻出パターン8種をワンクリックで挿入可能（Match Mode も自動で Regex へ連動、クリップボードコピー対応）。
  - Pattern 列に入力された正規表現の構文をリアルタイムに自動検証し、閉じ括弧忘れ等の構文エラー時は赤枠ハイライトおよびツールチップで具体的なエラー理由を表示。
- **Multi-Mode Search Filter Criteria (Contains / Exact / Regex)**:
  - 検索条件カード内の各テキスト項目（Patient ID, Course ID, Plan ID, Target Volume）に入致モード選択 ComboBox（`Contains` 部分一致、`Exact` 完全一致、`Regex` 正規表現）を新設。
  - Plan ID を `Exact`（完全一致）に設定することで、「a」で「Plan」等の余剰計画が拾われる現象を解消し、ピンポイントな計画抽出や正規表現（`^Plan_\d+$` 等）による高度な抽出が可能。
- **Expanded Test Suite (78 Tests, 100% Pass)**:
  - `SearchFilterTests`: カンマ区切りパーサーのサニタイズ検証、PlanSum 包含・除外詳細検証、Logic AND/OR（Patient ID / Course ID 含む全条件）の厳格判定検証、ESAPI 最適化スキップ判定（`ShouldSkipPatient` / `ShouldSkipCourse`）の検証、TextMatchMode（Contains / Exact / Regex）の単体検証および Exact/Regex での計画抽出検証、ParseTextFilter 分離検証、NumericFilterCriteria（レンジ・不等号）検証、NOT 除外フィルタ検証、肺SBRT複合判定検証、SearchPresetService デフォルトプロトコルおよび保存読込検証、MainViewModel プリセット連動および組み込み保護検証。
  - `PlanComplexityTests`: Varian HD120 / Millennium 120 のリーフ幾何境界計算およびリーフトラベル移動距離計算。
  - `MainViewModelTests`: コマンド排他制御、キャンセル要求中（`IsCancelling`）の多重押下防止、ダブルクリックルール登録、プレビューのテキスト・ステータス絞り込み、ルール/DQPの連続削除および次行自動選択。
  - `StructureMappingTests`: 不正正規表現（構文エラー）入力時のフォールトトレランス検証、日本語・特殊記号輪郭名検証、特異度厳格優先検証、正規表現構文バリデーション検証（正常・異常・モード切替クリア）、スニペット挿入自動Regex連動検証。
  - `StreamingExportTests`: 線量 0 Gy / 特殊文字 / 欠損値を含むフォールトトレランス CSV 出力および階層 JSONL 出力検証。

### Fixed
- **UI Exception on Startup (FrameworkElement.Style / ToggleButton TargetType Mismatch)**:
  - `MainWindow.xaml` の正規表現チートシート開閉ボタン（`<ToggleButton x:Name="BtnRegexHints">`）に対して、`TargetType="Button"` で定義された `ModernButton` スタイルを適用していたため、XAML 初期化時に `System.InvalidOperationException: 'Button' TargetType does not match type of element 'ToggleButton'` が発生し、`Set property 'System.Windows.FrameworkElement.Style' threw an exception` となる不具合を根本解消。
  - [App.xaml](file:///g:/Source/Repos/tkmd94/EclipseDataMiner/EclipseDataMiner/App.xaml) に専用の `ModernToggleButton` スタイルを新設して適用。
  - 再発防止策として、STA スレッド上で App および MainWindow を実際に初期化する単体テスト [`MainWindow_XamlLoadingAndStyleResolution_ShouldNotThrowException`](file:///g:/Source/Repos/tkmd94/EclipseDataMiner/EclipseDataMiner.Tests/SearchFilterTests.cs) を新設し、さらに `test.bat` のスモークテストでもリリース EXE の UI 完全ロードを自動検証するガードを配備。
- **Fatal Startup Crash (XamlParseException / Missing StaticResource)**:
  - `MainWindow.xaml` 内で参照されていた `BrushText` が `App.xaml` に定義されていなかったため、アプリ起動時に `System.Windows.Markup.XamlParseException` が発生して即座にサイレントクラッシュ（ExitCode: `0xe0434352`）していた不具合を根本解消。
  - `App.xaml` に `BrushText` を定義するとともに、`App.xaml.cs` にグローバル未処理例外ハンドラ（`DispatcherUnhandledException` および `AppDomain.UnhandledException`）を実装。
- **Search Filter Logic (AND/OR) Comprehensive Redesign**:
  - `Logic: OR` 選択時に、Patient ID または Course ID が指定されていると他条件（Plan ID や線量等）に合致する他患者・コースのプランが強制スキップされていた根本的な設計不備を修正。
  - `SearchFilterService.IsPlanMatch` に `patientId` と `courseId` を正式に組み込み、全検索項目（Patient, Course, Plan, Target, Dose/Fr, Fractions, Total Dose）の一元的な AND/OR 評価を実現。
  - ESAPI の重い `app.OpenPatient` を最小限に抑える `ShouldSkipPatient` / `ShouldSkipCourse` 最適化ロジックを実装。AND 時は不一致患者を即時スキップし高速性を維持しつつ、OR 時は他条件が存在する場合に漏れなく探索。
  - XAML RadioButton の `GroupName="GlobalLogicGroup"` を削除し、ViewModel 側の双方向 setter による完全な相互排他同期を確立。
- **Cancel Button Malfunction & Cooperative Cancellation**:
  - `StaEsapiWorkerService` の事前スキャン処理において `OperationCanceledException` が一般例外ブロックで握りつぶされていた構造的バグを修正。
  - コース走査、計画走査、輪郭走査、最適化目標走査、および最も重い **DVH / DQP 計算ループの各反復すべてに `CancellationToken.ThrowIfCancellationRequested()` を全面適用**。重い計算の途中でも即座に安全停止するように改善。
  - `MainViewModel` に `IsCancelling` プロパティおよび `CommandManager.InvalidateRequerySuggested()` を導入し、キャンセル押下時の UI 反映と二重押しを確実に防止。
- **Top Bar Header Redundancy**: 画面上段のスクリプト名、スクリプトバージョン、対応ESAPIバージョン、重複していたステータスバッジを削除し、余白を削減したスマートなレイアウトに改善。
- **Millennium 120 MLC Geometric Boundary Bug**: `PlanComplexityAnalysis.makeLeafBoundArray` において、第50リーフ（インデックス 49）の幅判定条件が `i < 49`（5.0 mm ではなく 10.0 mm が適用）となっていたバグを `i < 50` に修正。最上端座標が 205 mm から設計仕様通りの 200 mm（全長 400 mm、幾何学的対称性）に復元。
- **Command Execution Race Condition**: 実行中（`IsRunning == true`）に Run / PreScan コマンドが無効化（CanExecute = false）され、多重実行を完全に防止。
- **Log Console Background Glitch**: 読み取り専用 TextBox のデフォルトスタイルが白背景で上書きしていた不具合を解消。

## [2.3.0] - 2026-09-25

### Added
- **Streaming Export Pipeline**: 10,000件規模の大量計画走査に対応したストリーミングCSV出力エンジンを実装。
- **Machine Learning JSONL Export**: 機械学習・統計解析向けに 1 プラン = 1 行の JSON Lines (`.jsonl`) 同時出力をサポート。
- **Structure Pre-Scan & Alias Mapping**: 検索前に Structure ID を高速スキャンし、表記揺れ（Exact / Contains / Regex）を単一カラムに集約するエイリアスマッピング機能を追加。
- **JSON Rule Persistence**: 輪郭マッピングルールの JSON ファイル保存・読み込み機能を追加。
- **Hierarchical AND/OR Logic**: フィールド内カンマ区切り OR 検索に加え、条件間を「すべて満たす(AND)」/「いずれかを満たす(OR)」で切り替えるグローバル論理機能を追加。
- **PlanSum (Sum Plan) Support**: オプションで PlanSum（合算計画）の抽出に対応（不在項目は自動的に `N/A` 補完）。
- **Privacy & Anonymization Mode**: 院外研究用に患者 ID を SHA-256 でハッシュ化し、生年月日や承認者名を `REDACTED` にマスクする機能を追加。
- **Dose Normalization**: `ToGy()` 拡張メソッドにより cGy/Gy の単位混在を解消し、Gy 単位に自動正規化。
- **Isolated STA Background Worker**: ESAPI の STA 制約を遵守し、UI スレッドをブロックしない専用ワーカースレッド (`StaEsapiWorkerService`) を導入。
- **Automated Memory Safety**: 1 患者ごとの決定論的 `ClosePatient` および 200 件ごとの定期 `GC.Collect()` によるメモリリーク対策を実装。
- **Single-File Executable**: `Costura.Fody` による依存アセンブリ埋め込みを行い、配布・配備を単一 EXE に一本化。
- **Basic DVH Statistics Auto-Export**: 全対象輪郭に対する `Volume[cc]`, `Max dose[Gy]`, `Mean dose[Gy]`, `Min dose[Gy]` の自動常時出力を実装。
- **Prescription Dose Relative Reference**: 相対線量指標（例: V70% 等）の計算基準をプラン処方総線量 (`TotalDose`) に統一。
- **Clinical DQP Header Notation**: 旧仕様に完全準拠した臨床的ヘッダー出力（例: `Prostate-D95%[Gy]`, `Rectum-V70%[%]`）を実装。
- **Unit Testing Suite**: `MSTest` による多層単体テスト（`DoseNormalizationTests`, `StringSanitizerTests`, `StreamingExportTests`, `SearchFilterTests`, `StructureMappingTests` 計 23 件）を追加。

### Changed
- **WPF MVVM Architecture**: `MainWindow.xaml.cs` の約 1,240 行のコードビハインドを廃止し、`CommunityToolkit.Mvvm` を採用した MVVM パターンに全面リファクタリング。
- **Framework Upgrade**: .NET Framework 4.5 から施設内 Eclipse v16.1 端末に準拠した **.NET Framework 4.6.1 + PackageReference** に刷新。
- **UI Modernization**: 検索パネル、事前マッピング、DQP、オプション、コンソールログ、進捗バーを整理したモダンなタブ型レイアウトに刷新。

---

## [1.0.2] - 2020-06-15

### Changed
- DVH 統計から単位を除外して数値形式に統一。
- README ドキュメントを更新。

---

## [1.0.0] - 2019-10-01

### Added
- 初回リリース：Varian Eclipse ESAPI スタンドアロン型データマイニングスクリプト。
- 基本フィルタ（Patient ID, Course ID, Plan ID, Approval Status, Total Dose 等）。
- DQP（Dose Quality Parameters: D, V, DC, CV）の計算および CSV テンプレート読込機能。
- プラン複雑度指標（MCS, Edge Metric, Leaf Travel Length, Arc Length）の計算。
