# ESAPI データマイニング・プログラム 詳細設計仕様書 (v3.0)

[English](DESIGN_SPECIFICATION.en.md) | **日本語**

## 1. システム概要
本システムは、Varian Eclipse Scripting API (ESAPI) を活用し、10,000件規模の放射線治療計画（PlanSetup / PlanSum）からDVH指標およびメタデータを高速かつ安全に抽出するスタンドアロン・アプリケーションである。抽出データはデータクレンジングの負荷を最小化するため、正規化されたCSVおよび機械学習用途に最適なJSON Lines (JSONL) 形式でストリーミング出力される。

## 2. アーキテクチャ設計
* **プラットフォーム**: .NET Framework 4.6.1 (Eclipse v16.1 準拠) / x64ターゲット
  * プロジェクト形式: SDK-style csproj / `<LangVersion>10.0</LangVersion>`
* **UIフレームワーク**: WPF (Windows Presentation Foundation)
* **デザインパターン**: MVVMパターン (`CommunityToolkit.Mvvm` を利用)
* **プロジェクト構成**: 
  * 単一メインプロジェクト (`EclipseDataMiner`) ＋ 単体テストプロジェクト (`EclipseDataMiner.Tests`)
  * レイヤー構成: Models (DTO), ViewModels, Views, Services, Helpers
* **中間データモデル (DTO) 疎結合アーキテクチャ**:
  * ESAPIオブジェクトから `ExtractionPlanRecord` などの独立した抽出DTOモデルへマッピング。
  * CSV/JSONL出力、サニタイズ、集計ロジックをESAPI非依存で実装し、オフライン環境でも100%単体テスト可能とする。
* **非同期・スレッドモデル**: 
  * UIフリーズを防止するため、専用のSTA (Single Threaded Apartment) ワーカースレッドを起動してバックグラウンド処理を実施。
  * ESAPIのSTA制約に従い、ESAPIオブジェクトへのアクセスループは直列（シーケンシャル）で実行する。`Parallel.ForEach` などのマルチスレッド処理は厳禁とする。
* **データ出力アーキテクチャ**: 
  * メモリ肥大化を防ぐため、1患者の処理完了ごとにファイルへ書き出す「ストリーミング出力パイプライン」を採用。
* **パッケージング**:
  * `Costura.Fody` を導入し、依存NuGetライブラリを内包した単一EXE (`EclipseDataMiner.exe`) を生成（※ESAPI DLLは除外設定）。

## 3. 機能要件
### 3.1. 検索・フィルタリング機能
* **階層的AND/OR検索**:
  * **フィールド内OR検索**: カンマ(`,`)区切り入力による複数条件のOR検索 (例: PlanID = `VMAT, IMRT`)
  * **グローバル論理切替**: 異なる条件間の結合を「すべて満たす(AND)」か「いずれかを満たす(OR)」かで切り替え可能。
* **検索対象**: Patient ID, Course ID, Plan ID, 1回線量, 分割回数, 総線量, 承認ステータス。
* **PlanSum（合算計画）対応**:
  * オプションで「PlanSumを含める」チェックボックスを用意（デフォルトOFF）。
  * PlanSum抽出時は、PlanSumに存在しない項目（Beam情報や最適化設定等）に自動で `N/A` を出力。

### 3.2. 事前マッピング（2ペイン構成 ＆ リアルタイムプレビュー）
* **2ペイン分離アーキテクチャ**:
  * **左ペイン（ルール定義辞書）**: 永続化されるマッピングルール（Pattern, MatchMode, TargetAlias, IsSelected）。何度事前スキャンを実行してもロード済み・定義済みのルールは一切消去・上書きされない。`[▲ Up]` / `[▼ Down]` ボタンにより評価順序を直感的に並び替え可能。
  * **右ペイン（スキャン結果＆適用プレビュー）**: 検索条件に合致する対象プラン内の全 `Structure.Id` および出現件数を収集し、左ペインのルールを適用した解決結果（Resolved Alias, Status）をリアルタイムにプレビュー表示。
* **ルール重複・競合時の優先順位（特異度優先 ＋ 先頭優先）**:
  1. **完全一致 (Exact)** を最優先で評価。
  2. 次に **部分一致 (Contains)** を評価。
  3. 最後に **正規表現 (Regex)** を評価。
  4. 同一マッチモード内では、**リストの上にあるルール** が優先される（Up/Down ボタンで順序制御）。
  5. 完全に同一の Pattern および MatchMode を持つルールの重複登録は自動検知・防止。
* **Target Alias（統合名）が重複する場合の挙動**:
  * **意図的な表記揺れ統合（異なる症例間の同一臓器）**: 出力 CSV 上で同一の単一列（例: `PTV-Volume[cc]`, `PTV-Max dose[Gy]`, `PTV-D95%[Gy]`）に集約出力される（本機能の主目的）。
  * **同一プラン内での競合**: 同一プラン内に同一 Target Alias にマッチする輪郭が複数存在する場合、CSV 列には先頭一致した輪郭の値が出力され、JSON Lines 出力には両方の輪郭オブジェクトが階層保持される。
  * **プレビューでの可視化**: 同一 Target Alias に複数の生輪郭が統合される場合、右ペインのステータスに `[統合: N件]` と明記され、ユーザーが意図しない集約を即座に確認可能。
* **設定の永続化**: ルール定義辞書は独立した JSON ファイルとして保存・読み込み可能。
* **スキャン結果からのワンクリック取り込み**: スキャンで見つかった輪郭をワンクリックでルール定義に追加可能。
* **バイパス機能**: 定型業務向けに事前スキャンをスキップし、本検索へ直行するオプションを搭載。未スキャン時は誤操作防止の安全確認ダイアログを表示。

### 3.3. 抽出データ機能
* **基本DVH統計量の自動出力**: 全対象輪郭について、個別の DQP 設定によらず `Volume[cc]`, `Max dose[Gy]`, `Mean dose[Gy]`, `Min dose[Gy]` を自動的に必ず出力する。
* **動的DQP指標 (Dose Quality Parameters)**: 現行 5 列形式 (`structureName, DQPtype, DQPvalue, InputUnit, OutputUnit`) により、`Dose` (D), `Volume` (V), `DoseComplement` (DC), `ComplementVolume` (CV) を任意指定して抽出。
* **相対線量の正規化**: 相対線量（例: V70% 等）の計算基準には、プラン情報の処方総線量 (`PlanSetup.TotalDose`) を使用する。
* **照射パラメータ**: MU, Machine, Energy, Tech., PlanType (1プラン複数ビームは1セルに集約)。
* **計画メタデータ**: Calc Model, Normalization Mode, Clinical Protocol, Optimization Objectives。
* **Calculation Log**: 計算ログを出力可能 (サニタイズ処理必須)。
* **プラン複雑性評価 (Plan Complexity)**: MU/cGyに加え、オプションで文献に準拠した幾何学的照射野複雑度指標（MCS / MCSv [Masi 2013, McNiven 2010], Edge Metric [Younge 2012], Leaf Travel Length, Arc Length）を自動計算。
* **匿名化出力**: 
  * Patient ID を SHA-256 ハッシュ値に置換。
  * 生年月日（DateOfBirth）および計画承認者名（Planning Approver）を空欄または固定文字列（`REDACTED`）にマスク。

## 4. 出力データ仕様
* **線量単位の正規化 (Gy統一)**: 
  * 取得した `DoseValue.Unit` を判定し、`cGy` は `Gy` に自動換算して出力する。CSVヘッダーには `[Gy]` 等の単位を明記する。
* **CSV出力仕様 (1計画 = 1行の正規化フラット形式)**:
  * **1. 基本計画情報カラム (Core: 11列, 常時出力)**:
    * `Patient ID`, `Course ID`, `Date of birth`, `Plan ID`, `Target volume`, `DosePerFraction[Gy]`, `NumberOfFractions`, `TotalDose[Gy]`, `NumberOfBeams`, `ApprovalStatus`, `IsPlanSum`
  * **2. オプション計画メタデータ・照射パラメータカラム (Optional: 最大10列, Tab 4 設定連動)**:
    * `PlanningApprover`, `PlanningApprovalDate`, `MU`, `Machine/Energy/Tech/PlanType`, `CalculationModel`, `CalculationLog`, `PlanNormalizationMethod`, `ClinicalProtocol`, `OptimizationObjectives`, `PlanComplexity` (MCS, EdgeMetric, LeafTravel, ArcLength, AAV, LSV)
  * **3. 輪郭基本統計量カラム (Baseline Structure Stats: 1輪郭あたり4列, 自動出力)**:
    * `<Alias>-Volume[cc]`, `<Alias>-Max dose[Gy]`, `<Alias>-Mean dose[Gy]`, `<Alias>-Min dose[Gy]`
  * **4. 動的線量評価指標 (DQP) カラム (Dynamic DQP Columns: Tab 3 登録指標)**:
    * `<Alias>-D<Val>%[Gy]`, `<Alias>-D<Val>cc[Gy]`, `<Alias>-V<Val>Gy[%]`, `<Alias>-DC<Val>%[Gy]`, `<Alias>-CV<Val>Gy[%]` 等
  * 1対Nのデータ (Beam情報, Optimization Objectives等) は `;` で連結して1つのセルに格納。
  * 改行やカンマを含む文字列 (Calculation Log等) は、改行をスペースに置換し `""` でエスケープした上でダブルクォーテーションで囲む。
  * 欠損データや該当しない項目には明示的に `N/A` を出力する。
  * エンコーディング: Microsoft Excel での直接表示を保証する UTF-8 with BOM。
* **JSONL (JSON Lines) 出力仕様**:
  * 機械学習・AI解析向けに、オプションで `.jsonl` 形式を同時出力。
  * ESAPIの階層構造 (Plan > Beams, Objectives) をリストや辞書型として保持したまま、1プラン = 1行のJSONオブジェクトとしてシリアライズする。

## 5. ESAPI制御・実装仕様
### 5.1. メモリ管理の厳格化
* **患者リソースの解放**: 同時にメモリに展開できる患者は1名のみ。抽出ループ内で1患者の処理が終わるたびに必ず `Application.ClosePatient()` を `finally` ブロックで実行する。
* **ガベージコレクション**: 200件処理するごとに `System.GC.Collect()` を明示的に呼び出し、アンマネージドリソースのメモリリークを防止する。

### 5.2. 拡張メソッド・サニタイズ処理 (コード指針)
* **DoseNormalizationHelper**: `DoseValue` から安全にGy値を取得する拡張メソッド `ToGy()` を実装し、これを経由して線量値を取得する。
* **StringBuilderの利用**: CSVの行構築や1対Nの文字列連結には必ず `StringBuilder` を使用する。
* **フェイルセーフ対応**: 輪郭の未存在、線量未計算、`PlanSum` における最適化パラメータの不在など、Null参照が発生しうる箇所には徹底したNullチェックを実装する。

## 6. UI / デザインシステム設計仕様

### 6.1. デザインシステム & カラートークン
長時間の臨床・研究データ抽出作業における眼精疲労軽減と高い視認性を両立するため、医療系エンタープライズに最適化された **Slate & Ocean Cyan テーマ** を採用。

| トークン名 | カラーコード | 用途 |
| :--- | :--- | :--- |
| **Primary** | `#0284C7` (Sky-600) | アクションバー主要ボタン（Run Extraction）、フォーカスボーダー |
| **Primary Hover** | `#0369A1` (Sky-700) | プライマリボタンのホバー演出 |
| **Dark Header / Console** | `#0F172A` (Slate-900) | アプリケーションタイトルバー、ターミナルログコンソール背景 |
| **Window Background** | `#F8FAFC` (Slate-50) | 全体ウィンドウ背景 |
| **Card / Surface** | `#FFFFFF` | 各セクションのカード背景、DataGrid 背景 |
| **Border / Divider** | `#E2E8F0` (Slate-200) | カード境界線、グリッド区切り線、入力枠 |
| **Danger** | `#EF4444` (Red-500) | 緊急停止ボタン（Cancel）、削除系操作 |
| **Warning / Accent** | `#F59E0B` (Amber-500) | PlanSum 有効時バッジ、注意喚起ノート |
| **Success / Ready** | `#10B981` (Emerald-500) | 稼働ステータスバッジ（READY） |
| **Font Family** | `Segoe UI` / `Consolas` | UI基本フォント（Segoe UI）、ログ/コード等幅フォント（Consolas） |

### 6.2. 主要コンポーネント設計
1. **トップアクションバー**:
   - アプリケーションロゴ、バージョン表示、リアルタイム稼働ステータスバッジ（`READY` / `PRE-SCANNING` / `EXTRACTING`）を常時表示。
   - 実行制御（Run: Primary / Cancel: Danger）と出力先設定（Set Path / Open Folder）を右上に集約。
2. **検索条件カード**:
   - カンマ区切りの複数キーワード入力に対応したテキストボックス。
   - `AND` / `OR` を直感的に切り替えられるセグメントボタンスタイル。
   - 承認状態チェックボックスおよび PlanSum 選択時の警告バッジ。
3. **2ペイン輪郭マッピングタブ**:
   - 左右ペイン間にドラッグ可能な `GridSplitter` を配置。
   - **左ペイン（ルール定義）**: 優先順位入替（▲ Up / ▼ Down）、追加・削除、特異度優先探索（`Exact` > `Contains` > `Regex`）の制御。
   - **右ペイン（事前スキャン & プレビュー）**: 検出輪郭一覧、出現頻度（Hits）、解決先エイリアス（太字強調）、重複集約時の明示注記（`[統合: N件]`）、行ダブルクリックによる即時ルール追加、テキスト＆ステータス別（`All` / `Unmapped Only` / `Mapped Only` / `Excluded Only`）リアルタイム・クイックフィルタバー。
4. **DQP 設定タブ**:
   - 基本DVH統計量（Volume, Max, Mean, Min）自動出力および処方総線量正規化を通知する情報カード。
   - 直感的な行追加（+ Add DQP）および行削除（- Delete Selected）ボタン。
5. **オプションタブ（2カラムカード）**:
   - 左カラム: 計画メタデータおよびビームパラメータ。
   - 右カラム: 複雑性指標計算、個人情報匿名化（プライバシー強調バッジ付き）、JSONL 同時出力、事前スキャンバイパス。
6. **ログコンソール & プログレスバー**:
   - 完全な黒背景（`#090D16`）と視認性の高い等幅テキスト（`#F1F5F9`, `Consolas`）によるターミナルコンソール。
   - ミニツールバー（📋 Copy / 🗑 Clear）を配置。
   - 角丸プログレスバー上に進捗パーセンテージ（`{}{0}%`）とステータステキストを重ねて常時表示。

## 7. テスト・品質保証 (QA) 仕様

自動検証パイプライン（`test.bat`）により、MSBuild x64 Release ビルドおよび MSTest（計 116 件）を 100% 自動実行し、コードの健全性と計算正確性を担保。

### 7.1. テストスイート構成（計 116 件）
1. **単位正規化テスト ([DoseNormalizationTests.cs](file:///g:/Source/Repos/tkmd94/EclipseDataMiner/EclipseDataMiner.Tests/DoseNormalizationTests.cs) - 4件)**:
   - `cGy` / `Gy` 相互変換、文字列オーバーロード、厳密な数値保持。
2. **文字列サニタイズ・機密保護テスト ([StringSanitizerTests.cs](file:///g:/Source/Repos/tkmd94/EclipseDataMiner/EclipseDataMiner.Tests/StringSanitizerTests.cs) - 6件)**:
   - CSV 改行・カンマ・ダブルクォートエスケープ、SHA-256 匿名化ハッシュ、個人情報 `REDACTED` マスク、欠損値 `N/A` 変換。
3. **ストリーミングエクスポートテスト ([StreamingExportTests.cs](file:///g:/Source/Repos/tkmd94/EclipseDataMiner/EclipseDataMiner.Tests/StreamingExportTests.cs) - 8件)**:
   - 1プラン1行のフラット CSV 出力、基本統計量＋動的 DQP 列生成、線量 0 Gy / 特殊文字 / 欠損値を含むフォールトトレランス出力、PlanSum 線量計算ログ出力、階層構造を保持した JSONL 出力および `System.Text.Json` パース検証。
4. **検索フィルタ・パーステスト ([SearchFilterTests.cs](file:///g:/Source/Repos/tkmd94/EclipseDataMiner/EclipseDataMiner.Tests/SearchFilterTests.cs) - 47件)**:
   - 患者 ID の OR 検索、グローバル AND / OR 論理判定、PlanSum 包含・除外判定、承認状態判定、AND/OR トグル連動、カンマ区切りパーサーの連続カンマ・全角半角スペース・null サニタイズ。
   - 不等号（`>=`, `<=`, `>`, `<`）および範囲指定（`70-80`）数値パーサー、テキスト除外フィルタ（`!QA`）、照射パラメータ（Machine, Energy, Technique）フィルタ、日付範囲フィルタ（治療承認日・計画承認日・作成日）。
   - 検索プリセットの保存・適用・削除・説明文永続化、正規表現チートシート挿入、線量有無フィルタ（`HasDose` / `NoDose`）。
   - XAML 静的リソース整合性（`XamlResourceIntegrity_ShouldHaveNoMissingStaticResources`）、STA スレッド上での MainWindow XAML 初期化・スタイル解決（`MainWindow_XamlLoadingAndStyleResolution_ShouldNotThrowException`）。
5. **輪郭マッピング・ルール解決テスト ([StructureMappingTests.cs](file:///g:/Source/Repos/tkmd94/EclipseDataMiner/EclipseDataMiner.Tests/StructureMappingTests.cs) - 24件)**:
   - `Exact` / `Contains` / `Regex` マッピング、特異度優先探索（`Exact` > `Contains` > `Regex`）、重複集約時の自動注記（`[統合: N件]`）、構文エラー正規表現の安全な無視（例外非スロー）、日本語・特殊記号対応、順序入替（▲/▼）、JSON 保存読込、DQP/ログコマンド。
   - 検出輪郭のヒット件数（MatchedCount）集計、ルール変更時のリアルタイムプレビュー自動更新、正規表現エラー状態通知、複合臨床名解決。
6. **幾何アルゴリズム・複雑性テスト ([PlanComplexityTests.cs](file:///g:/Source/Repos/tkmd94/EclipseDataMiner/EclipseDataMiner.Tests/PlanComplexityTests.cs) - 13件)**:
   - Varian HD120 (中央 32 枚 2.5 mm, 外側 28 枚 5.0 mm) 幾何座標計算。
   - Millennium 120 (中央 40 枚 5.0 mm, 外側 20 枚 10.0 mm) 幾何座標計算および非対称性防止。
   - 未知 MLC モデルに対するフェイルセーフ。
   - コントロールポイント間リーフトラベル移動距離（LT）計算、Aperture Area Variability (AAV)、Leaf Sequence Variability (LSV)、Modulation Complexity Score (MCS)、Edge Metric、Arc Length の文献ベンチマーク完全一致検証。
   - 固定多門 IMRT および回転 VMAT 照射野の複雑度解析検証。
7. **ViewModel・状態遷移テスト ([MainViewModelTests.cs](file:///g:/Source/Repos/tkmd94/EclipseDataMiner/EclipseDataMiner.Tests/MainViewModelTests.cs) - 13件)**:
   - `IsRunning` に連動した Run / PreScan / Cancel コマンドの多重実行防止（排他制御 `CanExecute`）。
   - 初期出力先 CSV パス生成、進捗パーセンテージおよびステータステキスト更新。
   - プレビュー項目のダブルクリック追加および既存ルール選択（重複防止）。
   - プレビューのリアルタイム・テキストフィルタおよびステータス別絞り込み（`Unmapped Only` 等）。
   - ルールおよび DQP の連続削除（次行自動選択）、タイトルバーバージョン動的反映、Pre-Scan スコープバッジテキスト連動。
8. **UI レンダリング自動テスト ([UiScreenshotTests.cs](file:///g:/Source/Repos/tkmd94/EclipseDataMiner/EclipseDataMiner.Tests/UiScreenshotTests.cs) - 1件)**:
   - STA スレッド上での実 MainWindow レンダリング、コントロール配置・スタイル検証、およびドキュメント用スクリーンショット自動出力。