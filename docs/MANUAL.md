# EclipseDataMiner 操作マニュアル (v3.0.0)

[English](MANUAL.en.md) | **日本語**

本ドキュメントは、**EclipseDataMiner v3.0.0** の全機能、画面操作手順、および臨床データマイニングにおける活用方法を解説する公式マニュアルです。  
※本ソフトウェアのユーザーインターフェース (UI) は英語標準表記となっております。本マニュアルでは UI の英語表記と対応させて解説します。

---

## 1. 概要と画面構成

EclipseDataMiner は、Varian Eclipse (ESAPI) データベースから指定条件に合致する治療計画を横断探索し、線量指標 (DQP)、照射パラメータ、プラン複雑度指標 (MCS / Edge Metric) などを高速・安全に抽出するスタンドアロンアプリケーションです。

メイン画面は **上部に番号付きの 4 つのタブ**、**中段にログ出力コンソール**、**最下部に進捗バーおよび実行コントロールバー** で構成されています：

```
+-------------------------------------------------------------------+
| [1- Plan Search] [2- Structure Mapping] [3- DQP] [4- Options]     |
|                                                                   |
| << Tab 1: 1- Plan Search >>                                       |
| - CARD: 計画検索・絞り込み条件 (Plan Search & Filter)             |
|   - Patient / Course / Plan / Target / Dose / Status / Advanced   |
|   - [ 設定条件で走査し、プラン一覧に表示   [Search Plans] ]       |
| - TABLE: 検索プラン一覧 (全検索項目・全日付集約)                  |
|   - [Select All] [Unselect All] [Invert] (抽出対象をチェック)     |
|                                                                   |
| << Tab 4: 4- Extraction & Analysis Options >>                     |
| - CARD: Export Destination ( CSV Path ... [Browse] [Open Folder] )|
| - CARD: Metadata / Complexity / Output & Privacy Formats          |
+-------------------------------------------------------------------+
| CARD: Console Output  [Copy] [Clear]                              |
| [ Dark Slate ターミナルコンソール (自動最下行追従, Consolas) ]    |
+-------------------------------------------------------------------+
| [▶ Run Extraction] [⏹ Cancel]  [======= 42% =======]            |
| Status: Processing patient 42/100 (Extracted plans: 84)...        |
+-------------------------------------------------------------------+
```

![EclipseDataMiner UI メイン画面](img/UI.png)

---

## 2. 4つのメインタブの概要

| タブ番号と名称 | 主な役割 |
| :--- | :--- |
| **1- 📋 Plan Search** | 患者ID、計画名、線量、承認状態、照射装置、日付範囲などの条件指定・プリセット管理、および合致プランの高速検索・抽出対象選択。 |
| **2- 📐 Structure Mapping** | 輪郭名の表記揺れ（例: `PTV_60`, `ptv60`, `PTV-60Gy`）を単一カラム（Target Alias）に統合するための事前スキャンおよびルール辞書管理。 |
| **3- 📊 Dose Quality Parameters (DQP)** | D95%, V20Gy などの線量・体積指標の追加・編集（CSV テンプレートの読込・保存対応）。※各輪郭の基本統計量（Volume, Min, Mean, Max）は自動出力されます。 |
| **4- ⚙️ Extraction & Analysis Options** | 出力先 CSV ファイルパスの指定、照射パラメータ、プラン複雑度指標 (MCS / Edge Metric)、患者ID匿名化 (SHA-256)、JSONL 同時出力などのオプション設定。 |

---

## 3. 基本操作手順

操作は画面上部のタブ番号（**Tab 1 → Tab 2 → Tab 3 → Tab 4 → 最下部実行**）に沿って直線的に進めることができます。

```
[Tab 1: Plan Search]       ->  [Tab 2: Structure Mapping]
 条件指定 ＆ プラン検索         輪郭事前スキャン ＆ ルール定義
 (Exact/Contains, 選択)        (選択プランの輪郭を高速収集)
         ↓                               ↓
[Tab 3: DQP 設定]          ->  [Tab 4: Options ＆ 実行]
 線量評価指標 (D95%, V20Gy)     出力先・複雑度・匿名化設定
                                [▶ Run Extraction] 本番抽出
```

---

### ステップ 1: 計画検索条件の指定とプラン事前検索・選択（Tab 1: 1- 📋 Plan Search）

![Tab 1: 計画検索・絞り込みと合致プラン一覧 (Plan Search)](img/UI_Tab1_PlanSearch.png)

1. **計画検索・絞り込み条件の指定**:
   - **検索プリセット機能 (Presets) & 説明文の編集**:
     - カード最上段の **「🔖 Preset」** バーから、設定した検索条件セットをワンクリックで瞬時に保存・呼び出し・削除できます。
     - **画面上での Description（説明文）直接編集**: プリセットを選択すると説明文が表示され、画面上でそのまま臨床プロトコルの詳細や注意点を編集・追記し、**「💾 Save」** で同時に保存できます。
     - **保存場所（アプリ本体と同じ階層の `Presets/` フォルダ）**: プリセットは個別 JSON ファイル（例: `Presets/Prostate VMAT 78Gy.json`）として自動保存され、ファイル共有により他端末へ容易に展開できます。`Templates/Presets/` に臨床頻出プロトコルのサンプルも同梱されています。
   - **Patient ID / Course ID / Plan ID / Target Volume**:
     - 複数キーワードをカンマ (`,`) 区切りで入力可能です（例: `1001, 1002, 2005` または `VMAT, IMRT`）。
     - **NOT / Exclude 除外検索 (`!` または `-`)**: 単語の先頭に `!` または `-` を付与すると、その文字列を含む計画を除外します（例: `VMAT, IMRT, !QA, !Test`）。
     - **一致モード選択（Contains / Exact / Regex）**:
       - `Contains`（デフォルト）: **部分一致**。指定文字列が含まれる計画を抽出（大文字小文字無視）。
       - `Exact`: **完全一致**。指定した名称と完全に一致する計画のみを抽出（例: Plan ID に `Exact` で `Plan` を指定すると、`Plan1` や `MyPlan` はヒットせず `Plan` のみが合致）。
       - `Regex`: **正規表現**。柔軟なパターン照合（例: `^Plan_\d+$` や `(VMAT|IMRT)_Prostate`）。
     - **計画検索用 正規表現ヒント (❓ Regex Hints ▾)**:
       - プリセットバー右端のヒントボタンから、疾患接頭辞 (`^(HN|Lung)_.*`）、末尾指定 (`.*_Boost$`）、複数手法OR (`(VMAT|IMRT)`）などのスニペットをワンクリックで挿入できます。
   - **線量・分割数の範囲・不等号指定 (Dose/Fr, Fractions, Total Dose)**:
     - 範囲指定 (`70-80` や `70 ~ 80`、分割数 `33-35`）および不等号 (`>= 10`、`< 30`、`<= 5`）に完全対応。
     - 最適化前や線量未計算の計画で生じる `NaN` や `Infinity` は厳密に除外されます。
   - **線量計算の有無フィルタ (Dose Presence: All / HasDose / NoDose)**:
     - `All`: 全計画を対象（デフォルト）。
     - `HasDose`: **線量計算が完了している計画のみ** を抽出。
     - `NoDose`: **線量が未計算（未定義・NaN）の計画のみ** を抽出。
   - **Logic 切替 (AND / OR)**:
     - `AND`: 全指定条件（Patient, Course, Plan, Target, Dose等）を **すべて満たす** 計画のみ抽出。
     - `OR`: 指定条件の **いずれか 1 つでも満たす** 計画を抽出。
   - **Approval Status / Include PlanSum**:
     - `Unapproved`（未承認）、`Plan approved`（計画承認）、`TRT approved`（治療承認）の対象を選択。合算計画も含める場合は「Include PlanSum」をチェック。
   - **高度フィルタ (Advanced Filters)**:
     - 照射パラメータ（Machine ID, Energy, Technique）の包含・除外指定、および日付範囲（Treatment Approval / Planning Approval / Creation Date）による絞り込み。

2. **プラン事前検索（🔍 Search Plans）の実行**:
   - 条件入力カード最下部の **「🔍 Search Plans」** ボタンをクリックします。
   - ESAPI データベースから合致する計画のメタデータを高速走査し、直下の一覧テーブルに表示されます。
   - テーブルには、複数ビームの集約表示（Machine, Energy, Technique）、全3種の日付項目、線量計算有無（Dose 列）などが網羅されます。

3. **一覧テーブルでの抽出対象プランの選択（`Extract` 列チェック）**:
   - テーブル第1列の **`Extract`（チェックボックス）** で、後続の輪郭スキャンや本番データ抽出の対象とする計画を選択・除外します。
   - テーブル上部の「Select All」「Unselect All」「Invert」ボタンで一括切り替えが可能です。
   - **重要（選択情報の連動）**:
     - **この `Extract` 列で選択された計画群の情報は、後続の「Tab 2: Structure Mapping（輪郭事前スキャン）」および「▶ Run Extraction（本番データ抽出）」にダイレクトに反映・引き継がれます。**
     - これにより、全データベースを無駄に走査することなく、ピンポイントに対象プランのみを効率的に処理できます。

---

### ステップ 2: 輪郭事前スキャンと表記揺れマッピング（Tab 2: 2- 📐 Structure Mapping）

輪郭名の表記揺れ（例: `PTV_60`, `ptv60`, `PTV-60Gy`）を単一カラム（Target Alias）に集約・統合します。左右を仕切る GridSplitter を備えた 2 ペイン構成です。

![Tab 2: 輪郭マッピングルールと事前スキャンプレビュー (Structure Mapping)](img/UI_Tab2_StructureMapping.png)

1. **Tab 2: 2- 📐 Structure Mapping** を開きます。
2. **右ペイン: 事前スキャンの実行（🔍 Pre-Scan）**:
   - 右ペイン上部の **「🔍 Pre-Scan」** ボタンをクリックします。
   - **Plan Search 選択情報の自動反映**:
     - **Tab 1 の一覧テーブルで選択（`Extract` チェック）された計画のみをピンポイントで走査** します。
     - ツールバー右側のスコープバッジ（例: `🎯 Target: 3 / 3 Selected Plans`）に対象プラン数がリアルタイムに明示されます。
     - ※ Tab 1 でプラン検索を実行していない場合は、Tab 1 の検索条件に合致する全計画が走査対象となります（`🌐 Target: All Criteria Matching Plans`）。
     - スキャンを実行しても、**左ペインのロード済みルールは一切消去・上書きされません**。
   - スキャン完了後、検出された全輪郭 ID、出現頻度（Count）、現在のルールによる解決先（Resolved Alias）、判定ステータス（Status）が即座に一覧表示されます。
3. **左ペイン: マッピングルールの定義と編集**:
   - **📂 Load JSON / 💾 Save JSON**: 定義済みルールファイルを読み込み・保存します（サンプル: `Templates/StructureMapping_Prostate.json`）。
   - **+ Add Rule / - Delete**: ルールの新規追加や削除（Delete キー連打での連続削除対応）。
   - **▲ Up / ▼ Down**: ルールの適用優先順位を上下に入れ替え（キーボードショートカット `Alt+Up/Down` または `Ctrl+Up/Down` にも対応）。
   - **Target Alias / Match Mode**:
     - `Exact`（完全一致）、`Contains`（部分一致）、`Regex`（正規表現）を選択。
     - **特異度優先探索 (Exact > Contains > Regex)**: `Exact` が最優先され、次に `Contains`、最後に `Regex` が評価されるため、包括的な正規表現ルールと個別例外ルールが安全に両立します。
     - **リアルタイム構文バリデーション**: `Regex` 入力中に構文エラーがある場合はセル枠線が警告レッドに変化しツールチップで原因を表示。
     - **❓ Regex Hints ▾**: 臨床現場で頻出の正規表現スニペットをワンクリックで挿入可能。
   - **Extract チェックボックス**: 抽出不要な輪郭はチェックを外して除外。
4. **プレビューの確認とルールの追加**:
   - 右ペインのクイックフィルタ（テキスト検索、ステータス別絞り込み `All` / `Unmapped Only` / `Mapped Only` / `Excluded Only`）や **「↻ Refresh」** ボタンで、ルールの再評価・絞り込み結果を即座に確認できます。
   - 見つかった輪郭行を **ダブルクリック** するか、選択して **「+ Add to Rules」** を押すと、即座に左ペインのルール定義へ追加され、プレビューが自動再計算されます。
   - 同一の Target Alias に複数輪郭が集約される場合は `[統合: 2件]` のように集約数が明記されます。
5. **事前スキャンバイパス（スキップ）**:
   - 既存のルール辞書で即座に抽出を開始したい場合は、Tab 4 の **「Bypass Structure Pre-Scan」** をチェックすることで確認ダイアログをスキップして本番抽出へ進めます。

---

### ステップ 3: 線量品質パラメータ (DQP) の設定（Tab 3: 3- 📊 Dose Quality Parameters (DQP)）

![Tab 3: 線量品質パラメータ (DQP) 設定一覧 (Dose Quality Parameters)](img/UI_Tab3_DQP.png)

1. **Tab 3: 3- 📊 Dose Quality Parameters (DQP)** を開きます。
2. **自動出力指標の確認と正規化基準**:
   - **各輪郭の基本統計量（Volume, Max, Mean, Min dose）は登録不要で自動出力されます**。
   - **通常の計画（`PlanSetup`）の相対線量正規化基準**:
     - 相対線量（%）の基準線量（100%）は計画の「処方総線量 (`TotalDose`)」です。
   - **合算計画（`PlanSum`）における相対線量の計算挙動**:
     - `PlanSum` は複数計画の合算であるため、ESAPI の仕様上、単一の処方総線量（`TotalDose`）が存在しません。
     - **入力が相対線量の場合（`InputUnit = Relative [%]`、例: `V70%`）**: 処方線量の代わりに **「該当輪郭の最大線量（`Max dose`）」** が 100% 基準線量として自動的にフォールバック採用されます（例: `V50%` は輪郭内最大線量の 50% を受ける体積）。
     - **出力が相対線量の場合（`OutputUnit = Relative [%]`、例: `D95%[%]`）**: Eclipse 上で PlanSum に合算正規化線量が定義されていない場合、安全のため **`N/A`（欠損値）** が出力されます。
     - **体積の相対割合（%）**: 幾何本体積に対する割合として計算されるため、PlanSum でも PlanSetup と同様に正常計算されます（例: `D95%[Gy]`, `V50Gy[%]`）。
     - **臨床推奨**: `PlanSum` を対象に含む抽出では、一貫性・再現性を確保するため、線量指定に **絶対線量 [Gy]**（例: `D95%[Gy]`, `V50Gy[%]`, `D0.1cc[Gy]`）を使用することを推奨します。
3. **評価指標の追加・編集・削除**:
   - **+ Add DQP**: 新しい評価指標行を追加。
   - **- Delete Selected**: 選択行の削除（Delete キー連打での連続削除対応）。
   - **▲ Up / ▼ Down**: 指標行の並び順を上下に入れ替え（キーボードショートカット `Alt+Up/Down` または `Ctrl+Up/Down` にも対応）。
   - **Structure Name / Alias**: 評価対象の輪郭名、またはステップ 2 で定義した Target Alias。
   - **DQP Type**: 
     - `Dose` (D): 指定体積が受ける線量（例: D95%）
     - `Volume` (V): 指定線量を受ける体積（例: V20Gy, V70%）
     - `DoseComplement` (DC): 指定体積を除く部分の線量
     - `ComplementVolume` (CV): 指定線量未満の体積
   - **Value / Input Unit / Output Unit**: 指標値、入力単位（Relative [%] / Absolute [Gy / cc]）、出力単位を設定。
4. **📂 Load CSV / 💾 Save CSV**: 疾患別テンプレートを CSV 形式で保存・再利用できます（ヘッダー: `structureName,DQPtype,DQPvalue,InputUnit,OutputUnit`）。

---

### ステップ 4: 出力先設定と抽出・解析オプション（Tab 4: 4- ⚙️ Extraction & Analysis Options）

![Tab 4: 出力先ファイル設定と抽出・解析オプション (Extraction & Analysis Options)](img/UI_Tab4_Options.png)

1. **Tab 4: 4- ⚙️ Extraction & Analysis Options** を開きます。
2. **出力先ファイル設定 (Export File Destination)**:
   - **Browse...**: 保存先フォルダと CSV ファイル名を指定（デフォルト: アプリと同じフォルダの `DataMiningOutput.<日時>.csv`）。
   - **Open Folder**: 指定した保存先フォルダを Windows エクスプローラーで開く。
3. **カード 1: Plan Metadata & Beam Parameters**:
   - 承認者情報、承認日、線量計算モデル、処方正規化モード、臨床プロトコル名、最適化条件。
   - ビーム MU、装置/エネルギー/照射手法、計算ログ出力（改行エスケープ済み）。
4. **カード 2: Complexity Analysis, Privacy & Output Formats**:
   - **Plan Complexity Analysis**: Modulation Complexity Score (MCS), Edge Metric, Leaf Travel Length, Arc Length を自動計算。
   - **Anonymize Patient ID & Redact Personal Info**: 院外研究用に患者 ID を SHA-256 でハッシュ化し、生年月日や承認者名を `REDACTED` にマスク。
   - **Simultaneously Export JSON Lines (.jsonl)**: 機械学習・AI 解析用に `.jsonl` 形式を同時出力。
   - **Bypass Structure Pre-Scan**: 事前輪郭スキャンの確認をバイパス。

---

### ステップ 5: データマイニング実行と進捗監視（画面最下部 ＆ ログコンソール）

1. 画面最下部のコントロールバーにある **「▶ Run Extraction」** ボタン（鮮やかなオーシャンブルー）をクリックします。
   - **Tab 1 でプランを事前検索・選択している場合**: 一覧テーブルで `Extract` にチェックが入っている選択計画のみをピンポイントで本番抽出（DVH・DQP・幾何指標計算）します。
   - **事前検索をスキップした場合**: 設定された検索条件に合致する全計画を直接走査して抽出します。
2. 稼働ステータスバッジが `EXTRACTING` に切り替わり、進捗バーがリアルタイムに更新されます。
3. **ダークターミナルログコンソール**:
   - 等幅フォント（`Consolas`）による黒背景ターミナルで処理状況（開いた患者、計算中プラン、DVH サンプリング等）をリアルタイム監視。
   - **📋 Copy**: 全ログをクリップボードにコピー。
   - **🗑 Clear**: ログ表示をクリア。
4. **安全・即時キャンセル機能 (⏹ Cancel)**:
   - 処理を中断したい場合は、最下部の赤色 **「⏹ Cancel」** ボタンを押します。
   - ミリ秒単位でバックグラウンドループへ安全な協調キャンセルが伝播し、部分書き出し済み CSV の破損を防ぎながら速やかに終了します（そこまでに抽出されたデータはファイルに安全に保存されます）。
5. 処理完了後、完了ダイアログが表示され、指定パスにデータが出力されます。

---

## 4. 出力ファイルの仕様と全カラム詳細解説

EclipseDataMiner は、データマイニングおよび機械学習パイプラインへの投入を円滑化するため、**1計画 = 1行の正規化フラット CSV**（UTF-8 with BOM）および階層構造を保持した **JSON Lines (`.jsonl`)** を出力します。

---

### 4.1 CSV 出力 (フラット形式) の全カラム解説

CSV の各行は、以下のカテゴリ順で構成されます：
1. **基本計画情報カラム**（常に必ず出力される 11 項目）
2. **オプション計画メタデータ・照射パラメータカラム**（Tab 4 のチェックボックスにより追加される最大 10 項目）
3. **輪郭基本統計量カラム**（マッピングされた輪郭ごとに自動出力される 4 項目）
4. **動的線量評価指標 (DQP) カラム**（Tab 3 で登録した任意の DQP 指標）

#### 1. 基本計画情報カラム (Core Columns: 常時出力)

| 列ヘッダー名 | データ型 / 単位 | 概要・詳細仕様 | 例 / デフォルト値 |
| :--- | :--- | :--- | :--- |
| **`Patient ID`** | 文字列 (string) | 患者識別番号（Patient ID）。※Tab 4 で「Anonymize Patient ID」が有効な場合は、不可逆なソルト付き SHA-256 ハッシュ（64文字の16進数英数字）に置換されます。 | `12345678` または `e3b0c44298fc1c149afbf4...` |
| **`Course ID`** | 文字列 (string) | Eclipse 内のコース識別子（Course ID）。 | `C1`, `Course1` |
| **`Date of birth`** | 日付 (yyyy-MM-dd) | 患者の生年月日。匿名化有効時は `REDACTED` にマスク。未登録時は `N/A`。 | `1955-04-12` または `REDACTED` |
| **`Plan ID`** | 文字列 (string) | 治療計画 ID（`PlanSetup.Id` または `PlanSum.Id`）。 | `VMAT_Prostate`, `PlanSum1` |
| **`Target volume`** | 文字列 (string) | 計画に関連付けられた標的体積の輪郭 ID（`TargetVolumeId`）。未設定時は `N/A`。 | `PTV_78Gy`, `PTV` |
| **`DosePerFraction[Gy]`** | 浮動小数点 [Gy] | 1分割あたりの処方線量。単位は必ず Gray [Gy] に統一され、小数第2位まで出力されます。未計算時は `N/A`。 | `2.00` |
| **`NumberOfFractions`** | 整数 (int) | 処方分割回数（Fraction 数）。未計算時は `N/A`。 | `39`, `35` |
| **`TotalDose[Gy]`** | 浮動小数点 [Gy] | 処方総線量（`DosePerFraction * NumberOfFractions`）。単位は必ず Gray [Gy]（小数第2位）。未計算時は `N/A`。 | `78.00`, `70.00` |
| **`NumberOfBeams`** | 整数 (int) | 計画に含まれる通常照射ビームの総数（セットアップビームを除く）。合算計画 (`PlanSum`) の場合は `0`。 | `2`, `4` |
| **`ApprovalStatus`** | 文字列 (string) | Eclipse 内の承認ステータス。`TreatmentApproved`（治療承認）、`PlanApproved`（計画承認）、`Unapproved`（未承認）、合算計画時は `PlanSum`。 | `TreatmentApproved` |
| **`IsPlanSum`** | 真偽値 (bool) | 合算計画（PlanSum）か単一計画（PlanSetup）かの識別フラグ。 | `True` または `False` |

#### 2. オプション計画メタデータ・照射パラメータカラム (Optional Columns)

Tab 4 の「Plan Metadata」「Beam Parameters & Calculation Logs」「Plan Complexity Analysis」のチェックボックスに応じて動的に追加されます：

| 列ヘッダー名 | 有効化オプション | 概要・詳細仕様 | 例 / 書式 |
| :--- | :--- | :--- | :--- |
| **`PlanningApprover`** | Planning Approver | 計画を承認したスタッフ名（Eclipse ユーザー名）。匿名化有効時は `REDACTED` にマスク。未承認時は `N/A`。 | `physicist01` または `REDACTED` |
| **`PlanningApprovalDate`** | Planning Approval Date | 計画が承認された日時（`yyyy-MM-dd HH:mm:ss`）。未承認時は `N/A`。 | `2026-06-15 14:30:00` |
| **`MU`** | Beam MU | 各照射野の Monitor Unit (MU) をセミコロン (`;`) で連結出力。書式: `BeamID:MU値`。合算計画時は `N/A`。 | `B1:145.2;B2:138.6` |
| **`Machine/Energy/Tech/PlanType`** | Machine / Energy / Tech / PlanType | 各照射野のハードウェアおよび照射技術仕様をセミコロンで集約。書式: `BeamID(装置名/エネルギー/照射手法/MLC計画タイプ)`。 | `B1(TrueBeam/6X/ARC/VMAT);B2(TrueBeam/6X/ARC/VMAT)` |
| **`CalculationModel`** | Calculation Model | 線量計算アルゴリズム名。光子線モデルおよび電子線モデルをスラッシュ区切りで出力。 | `AAA_16.1.0/N/A` または `AcurosXB_16.1.0/N/A` |
| **`CalculationLog`** | Calculation Logs | 線量計算アルゴリズムの実行ログ。CSV 構造破壊防止のため、ログ内の改行コードはすべて半角スペースへ置換・サニタイズされます。ログ不在時は `N/A`。 | `Calculation completed in 12.4s; Grid: 2.5mm...` |
| **`PlanNormalizationMethod`** | Normalization Mode | 処方線量の正規化方法の説明文字列（例: 標的体積平均値、特定点など）。 | `100.0% in target PTV` |
| **`ClinicalProtocol`** | Clinical Protocol | 計画に紐付けられた臨床プロトコル名または治験テンプレート名。 | `JCOG1408_Lung`, `RTOG0534` |
| **`OptimizationObjectives`** | Optimization Objectives | 最適化計算で設定された全目的関数（Objective）をセミコロンで連結。書式: `輪郭名:目的関数種別:線量/体積値:優先度`。3D-CRT 等で設定がない場合は `N/A`。 | `PTV:Upper:70.0Gy:100;Rectum:Upper:50.0Gy:80` |
| **`PlanComplexity`** | Plan Complexity | 文献値準拠の幾何学的照射野複雑度解析指標（全ビーム集約値）をセミコロン区切りで出力。<br/>- **`MCS`**: Modulation Complexity Score (Masi 2013 [0〜1], 1に近いほど単純)<br/>- **`EdgeMetric`**: 周囲長・開口面積比 (Younge 2012 [mm⁻¹])<br/>- **`LeafTravel`**: 総リーフトラベル移動量 [mm]<br/>- **`ArcLength`**: ガントリー総回転角 [deg]<br/>- **`AAV`**: Area Aperture Variation (開口面積変動)<br/>- **`LSV`**: Leaf Sequence Variation (リーフシーケンス変動) | `MCS=0.342;EdgeMetric=0.087;LeafTravel=1420.5;ArcLength=358.0;AAV=0.512;LSV=0.668` |

#### 3. 輪郭基本統計量カラム (Automatic Baseline Structure Statistics)

Tab 2 でマッピング対象（`IsSelected = true`）となった各輪郭（Target Alias）について、**個別の DQP 指標登録を行わなくても必ず自動的に出力される 4 大基本統計量** です。

| 列ヘッダー名 | 単位 | 概要・詳細仕様 | 例 |
| :--- | :--- | :--- | :--- |
| **`<Alias>-Volume[cc]`** | cc (cm³) | 輪郭の幾何学的体積（小数第2位）。輪郭が存在しない計画では `N/A`。 | `42.50` |
| **`<Alias>-Max dose[Gy]`** | Gray [Gy] | 輪郭内の最大線量。Eclipse の単位が cGy の場合も自動的に Gy へ統一換算。 | `81.25` |
| **`<Alias>-Mean dose[Gy]`** | Gray [Gy] | 輪郭内の平均線量（小数第2位）。 | `78.40` |
| **`<Alias>-Min dose[Gy]`** | Gray [Gy] | 輪郭内の最小線量（小数第2位）。 | `71.10` |

※ 例: 輪郭エイリアスとして `PTV` および `Rectum` を定義している場合、`PTV-Volume[cc]`, `PTV-Max dose[Gy]`, `PTV-Mean dose[Gy]`, `PTV-Min dose[Gy]`, `Rectum-Volume[cc]`, `Rectum-Max dose[Gy]`, `Rectum-Mean dose[Gy]`, `Rectum-Min dose[Gy]` の計 8 列が自動生成されます。

#### 4. 動的線量評価指標 (DQP) カラム (Dynamic DQP Columns)

Tab 3 で登録した各線量体積指標について、指定された出力単位で 3D 線量グリッド（DVH）からリアルタイムサンプリングして出力されます。

| 列ヘッダー名の規則 | 指標種別 (DQP Type) | 計算ロジック・詳細 | 例 |
| :--- | :--- | :--- | :--- |
| **`<Alias>-D<Value>%[Gy]`** | Dose at Volume (D) | 指定体積割合（%）が受ける線量 [Gy]。 | `PTV-D95%[Gy]` -> `74.50` |
| **`<Alias>-D<Value>cc[Gy]`** | Dose at Volume (D) | 指定絶対体積（cc）が受ける線量 [Gy]。高線量小体積（ホットスポット）の評価に常用。 | `SpinalCord-D0.1cc[Gy]` -> `42.10` |
| **`<Alias>-D<Value>%[%]`** | Dose at Volume (D) | 指定体積が受ける相対線量 [%]（処方総線量に対する割合）。 | `PTV-D98%[%]` -> `96.50` |
| **`<Alias>-V<Value>Gy[%]`** | Volume at Dose (V) | 指定線量 [Gy] を受ける輪郭の体積割合 [%]。OAR 耐容線量評価の標準指標。 | `Rectum-V70Gy[%]` -> `12.30` |
| **`<Alias>-V<Value>%[%]`** | Volume at Dose (V) | 処方総線量（`TotalDose`）の指定割合（%）を受ける体積割合 [%]。 | `Bladder-V70%[%]` -> `25.40` |
| **`<Alias>-V<Value>Gy[cc]`** | Volume at Dose (V) | 指定線量 [Gy] を受ける絶対体積 [cc]。 | `Rectum-V50Gy[cc]` -> `15.20` |
| **`<Alias>-DC<Value>%[Gy]`** | Dose Complement (DC) | 補集合体積が受ける線量 [Gy]。 | `PTV-DC95%[Gy]` -> `72.80` |
| **`<Alias>-CV<Value>Gy[%]`** | Complement Volume (CV) | 指定線量未満に保たれた補集合体積の割合 [%]。 | `Body-CV10Gy[%]` -> `85.20` |

> [!NOTE]
> **合算計画（`PlanSum`）における相対線量（%）の計算仕様**
> - **入力が相対線量の場合（`InputUnit = Relative [%]`、例: `V70%[Gy]`, `V50%[%]`）**:
>   PlanSum には単一の処方総線量（`TotalDose`）が存在しないため、フォールバックとして **「該当輪郭の最大線量（`Max dose`）」** が 100% 基準線量として体積評価に用いられます（例: `V50%` は輪郭内最大線量の 50% を受ける体積）。
> - **出力が相対線量の場合（`OutputUnit = Relative [%]`、例: `D95%[%]`）**:
>   Eclipse 上で合算正規化基準線量が設定されていない場合、安全のため **`N/A`**（欠損値）が出力されます。
> - **推奨運用**:
>   PlanSum を含むコホートのデータマイニングでは、評価の一貫性・再現性を確保するため、線量指定を **絶対線量（`Gy`）**（例: `V50Gy[%]`, `D95%[Gy]`, `D0.1cc[Gy]`）で登録することを推奨します。

---

### 4.2 特殊文字・サニタイズおよび欠損値の仕様

- **欠損値 (`N/A`)**: 計画内に輪郭が存在しない、線量が未計算である、あるいは該当項目（PlanSum におけるビーム情報など）が存在しない場合、空欄ではなく明示的に `N/A` を出力します。
- **改行コードの置換**: 計算ログ等の文字列に改行コード（`\r\n`, `\n`）が含まれる場合、CSV の行崩れを防ぐためすべて半角スペースに自動置換されます。
- **カンマ・引用符のエスケープ**: 文字列内にカンマ (`,`) や二重引用符 (`"`) が含まれる場合、全体が `""` で囲まれ、内部の `"` は `""` にエスケープされます。
- **文字コード**: Microsoft Excel での直接ダブルクリック起動時にも文字化けが発生しないよう、**UTF-8 with BOM** で出力されます。

---

### 4.3 Python (Pandas) / JSONL でのデータ解析例

#### Python / Pandas での CSV 読み込み
```python
import pandas as pd

# CSV の読み込み (UTF-8)
df = pd.read_csv("DataMiningOutput.20260928120000.csv")
print(f"Loaded {len(df)} plans.")

# 欠損値 N/A は自動的に NaN として解釈されます
print(df[["Patient ID", "Plan ID", "TotalDose[Gy]", "PTV-D95%[Gy]"]].head())

# 線量指標の統計サマリー
print(df["PTV-D95%[Gy]"].describe())
```

#### JSON Lines (`.jsonl`) の読み込み例
```python
import json

plans = []
with open("DataMiningOutput.20260928120000.jsonl", "r", encoding="utf-8") as f:
    for line in f:
        plans.append(json.loads(line))

print(f"Loaded {len(plans)} plans.")
# 各ビームの MU や最適化パラメータに階層的にアクセス可能
first_plan_beams = plans[0]["Beams"]
```

---

## 5. トラブルシューティング

| 現象 | 原因 | 対処法 |
| :--- | :--- | :--- |
| **起動時にエラーが発生する** | 実行端末に Eclipse ESAPI ランタイムが存在しない | Eclipse がインストールされているワークステーション上で実行してください。 |
| **事前スキャンが遅い** | 患者数が極めて多い状態でフィルタを指定していない | Patient ID や承認状態などの検索条件を指定して、スキャン対象を絞り込んでください。 |
| **特定の輪郭が出力されない** | 輪郭名の大文字小文字やアンダースコアの表記揺れ | Tab 2 の「Pre-Scan」を実行し、実際の Structure ID を確認した上で Target Alias を紐付けてください。 |
| **PlanSum の DQP 相対線量が N/A または意図と異なる** | PlanSum に処方総線量（TotalDose）が存在しないため | 相対線量（%）ではなく、絶対線量（`Gy`、例: `D95%[Gy]`, `V50Gy[%]`）で指標を登録してください。 |
| **処理を中断したい** | 長時間処理を停止したい | 「Cancel」ボタンをクリックしてください。1秒以内に現在の患者処理を完了して安全に終了し、途中までのデータが保存されます。 |
