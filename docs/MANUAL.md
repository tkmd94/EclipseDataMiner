# EclipseDataMiner 操作マニュアル (v3.0)

本ドキュメントは、**EclipseDataMiner v3.0** の全機能、画面操作手順、および臨床データマイニングにおける活用方法を解説する公式マニュアルです。

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
2. **右ペイン: 事前スキャンの実行（🔍 Pre-Scan Structures）**:
   - 右ペイン上部の **「🔍 Pre-Scan Structures」** ボタンをクリックします。
   - **Plan Search 選択情報の自動反映**:
     - **Tab 1 の一覧テーブルで選択（`Extract` チェック）された計画のみをピンポイントで走査** します。
     - ツールバー右側のスコープバッジ（例: `🎯 Target: 3 / 3 Selected Plans`）に対象プラン数がリアルタイムに明示されます。
     - ※ Tab 1 でプラン検索を実行していない場合は、Tab 1 の検索条件に合致する全計画が走査対象となります（`🌐 Target: All Criteria Matching Plans`）。
     - スキャンを実行しても、**左ペインのロード済みルールは一切消去・上書きされません**。
   - スキャン完了後、検出された全輪郭 ID、出現頻度（Count）、現在のルールによる解決先（Resolved Alias）、判定ステータス（Status）が即座に一覧表示されます。
3. **左ペイン: マッピングルールの定義と編集**:
   - **Load Rules (JSON)**: 定義済みルールファイルを読み込みます（サンプル: `Templates/StructureMapping_Prostate.json`）。
   - **+ Add Rule / - Delete**: ルールの新規追加や削除（Delete キー連打での連続削除対応）。
   - **▲ Up / ▼ Down**: ルールの適用優先順位を上下に入れ替え。
   - **Target Alias / Match Mode**:
     - `Exact`（完全一致）、`Contains`（部分一致）、`Regex`（正規表現）を選択。
     - **特異度優先探索 (Exact > Contains > Regex)**: `Exact` が最優先され、次に `Contains`、最後に `Regex` が評価されるため、包括的な正規表現ルールと個別例外ルールが安全に両立します。
     - **リアルタイム構文バリデーション**: `Regex` 入力中に構文エラーがある場合はセル枠線が警告レッドに変化しツールチップで原因を表示。
     - **❓ Regex Hints ▾**: 臨床現場で頻出の正規表現スニペットをワンクリックで挿入可能。
   - **Extract チェックボックス**: 抽出不要な輪郭はチェックを外して除外。
   - **Save Rules (JSON)**: 編集したルール辞書を JSON ファイルに保存。
4. **プレビューの確認とルールの追加**:
   - 右ペインのクイックフィルタ（テキスト検索、ステータス別絞り込み `All` / `Unmapped Only` / `Mapped Only` / `Excluded Only`）で未マッピング輪郭を素早く特定できます。
   - 見つかった輪郭行を **ダブルクリック** するか、選択して **「+ Add to Rules」** を押すと、即座に左ペインのルール定義へ追加され、プレビューが自動再計算されます。
   - 同一の Target Alias に複数輪郭が集約される場合は `[統合: 2件]` のように集約数が明記されます。
5. **事前スキャンバイパス（スキップ）**:
   - 既存のルール辞書で即座に抽出を開始したい場合は、Tab 4 の **「Bypass Structure Pre-Scan」** をチェックすることで確認ダイアログをスキップして本番抽出へ進めます。

---

### ステップ 3: 線量品質パラメータ (DQP) の設定（Tab 3: 3- 📊 Dose Quality Parameters (DQP)）

![Tab 3: 線量品質パラメータ (DQP) 設定一覧 (Dose Quality Parameters)](img/UI_Tab3_DQP.png)

1. **Tab 3: 3- 📊 Dose Quality Parameters (DQP)** を開きます。
2. **自動出力指標の確認**:
   - **各輪郭の基本統計量（Volume, Max, Mean, Min dose）は登録不要で自動出力されます**。
   - 相対線量（%）の正規化基準は「処方総線量 (`TotalDose`)」です。
3. **評価指標の追加・編集・削除**:
   - **+ Add DQP**: 新しい評価指標行を追加。
   - **- Delete Selected**: 選択行の削除（Delete キー連打での連続削除対応）。
   - **Structure Name / Alias**: 評価対象の輪郭名、またはステップ 2 で定義した Target Alias。
   - **DQP Type**: 
     - `Dose` (D): 指定体積が受ける線量（例: D95%）
     - `Volume` (V): 指定線量を受ける体積（例: V20Gy, V70%）
     - `DoseComplement` (DC): 指定体積を除く部分の線量
     - `ComplementVolume` (CV): 指定線量未満の体積
   - **Value / Input Unit / Output Unit**: 指標値、入力単位（Relative [%] / Absolute [Gy / cc]）、出力単位を設定。
4. **Save / Load DQP (CSV)**: 疾患別テンプレートを CSV 形式で保存・再利用できます（ヘッダー: `structureName,DQPtype,DQPvalue,InputUnit,OutputUnit`）。

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

## 4. 出力ファイルの活用

### 4.1 Python / Pandas での読み込み例
```python
import pandas as pd

# CSV の読み込み
df = pd.read_csv("DataMiningOutput.20260925120000.csv")
print(df.head())

# 欠損値 N/A は自動的に NaN として解釈されます
print(df["TotalDose[Gy]"].describe())
```

### 4.2 JSON Lines (`.jsonl`) の読み込み例
```python
import json

plans = []
with open("DataMiningOutput.20260925120000.jsonl", "r", encoding="utf-8") as f:
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
| **処理を中断したい** | 長時間処理を停止したい | 「Cancel」ボタンをクリックしてください。1秒以内に現在の患者処理を完了して安全に終了し、途中までのデータが保存されます。 |
