# EclipseDataMiner 操作マニュアル (v3.0)

本ドキュメントは、**EclipseDataMiner v3.0** の全機能、画面操作手順、および臨床データマイニングにおける活用方法を解説する公式マニュアルです。

---

## 1. 概要と画面構成

EclipseDataMiner は、Varian Eclipse (ESAPI) データベースから指定条件に合致する治療計画を横断探索し、線量指標 (DQP)、照射パラメータ、プラン複雑度指標 (MCS / Edge Metric) などを高速・安全に抽出するスタンドアロンアプリケーションです。

メイン画面は **上部に番号付きの 4 つのタブ**、**中段にログ出力コンソール**、**最下部に進捗バーおよび実行コントロールバー** で構成されています：

```
+---------------------------------------------------------------------------------------------------------+
| [1- 📋 Plan Search]    [2- 📐 Structure Mapping]  [3- 📊 DQP]  [4- ⚙️ Extraction & Options]              |  <- 1. メイン 4 タブ
|                                                                                                         |
|  << Tab 1: 1- 📋 Plan Search 内部 >>                                                                    |
|  - CARD: 🔍 計画検索・絞り込み条件 (Plan Search & Filter)                                                |
|    - Patient / Course / Plan / Target Volume / Dose / Status / Advanced (Machine, Date Range)           |
|    - [ 💡 設定条件で治療計画を高速走査し、下部のプラン一覧に表示します。        [🔍 Search Plans] ]        |
|  - TABLE: 検索プラン一覧 (全検索項目・複数ビーム集約・全日付) - [Select All] [Unselect All] [Invert]        |
|                                                                                                         |
|  << Tab 4: 4- ⚙️ Extraction & Analysis Options 内部 >>                                                  |
|  - CARD: 📁 Export File Destination ( Output CSV Path ...................... [Browse...] [Open Folder] )|
|  - CARD: Plan Metadata / Complexity Analysis / Output & Privacy Formats                                 |
+---------------------------------------------------------------------------------------------------------+
|  CARD: 💻 Console Output   [Copy] [Clear]                                                               |  <- 2. ログコンソール
|  [ Dark Slate ターミナルコンソール (自動最下行追従スクロール, Consolas, #090D16) ]                         |
+---------------------------------------------------------------------------------------------------------+
| [▶ Run Extraction] [⏹ Cancel]  [======================= 42% =======================]                  |  <- 3. 実行 ＆ 進捗バー
| Status: Processing patient 42/100 (Extracted plans: 84)...                                             |
+---------------------------------------------------------------------------------------------------------+
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

### ステップ 1: プラン検索・絞り込みと選択（Tab 1: 1- 📋 Plan Search）

![Tab 1: 計画検索・絞り込みと合致プラン一覧 (Plan Search)](img/UI_Tab1_PlanSearch.png)

1. **検索＆プラン一覧の統合**:
   - Tab 1 に検索条件入力カードとプラン一覧テーブルが統合されており、画面上部で条件を指定して「🔍 Search Plans」を実行すると、直下の一覧テーブルに合致した計画が即座に表示されます。
2. **プラン事前検索（🔍 Search Plans）**:
   - 条件入力カードの最下部にある **「🔍 Search Plans」** ボタンをクリックします。
   - ESAPI データベースから合致する計画のメタデータを高速走査し、下部の一覧テーブルに表示されます。
   - **全検索条件を網羅した表表示**:
     - `Extract`（抽出対象選択チェック）
     - `Patient ID` / `Course ID` / `Plan ID` / `Target Volume`（計画ターゲット輪郭ID）
     - `Type`（PlanSetup / PlanSum） / `Approval Status`（承認ステータス）
     - `Dose/Fr` / `Fractions` / `Total Dose`
     - `Machine` / `Energy` / `Technique`（**複数ビームが存在する場合もすべてカンマ区切りで漏れなく集約表示**）
     - **全日付項目（3列すべて独立表示）**:
       - Treatment Approval Date（治療承認日）
       - Planning Approval Date（計画承認日）
       - Creation Date（計画作成日）
   - **選択制御ツールバー**:
     - テーブル上部の「Select All」「Unselect All」「Invert」ボタンで、抽出対象とする計画を一括制御できます。
3. **本番データ抽出（▶ Run Extraction）**:
   - **事前検索済みの場合**: 一覧テーブルでチェックが入っている選択計画のみをピンポイントで本番抽出（DVH・DQP・幾何指標計算）します。不要な計画の計算を省くことで、抽出所要時間を大幅に削減できます。
   - **事前検索をスキップした場合**: 一覧が空の状態で直接クリックした場合は、設定された検索条件に合致するすべての計画を直接走査して抽出します（従来のワンストップ抽出フローとの完全互換）。
4. **出力先の設定**:
   - **Tab 4 (4- ⚙️ Extraction & Analysis Options)** 内の **「Browse...」** ボタンをクリックし、出力先 CSV ファイルの保存場所とファイル名を指定します（デフォルトは実行ファイルと同じフォルダの `DataMiningOutput.<日時>.csv`）。
   - 出力フォルダを確認・開くには **「Open Folder」** ボタンをクリックします。
5. **安全・即時キャンセル機能**:
   - データ抽出中、プラン検索中、および輪郭事前スキャン中に画面最下部の **「⏹ Cancel」** ボタンが有効化されます。
   - キャンセルボタンを押すと、バックグラウンドの ESAPI 走査・DVH 計算ループへミリ秒単位で協調キャンセルが伝播し、部分書き出し済み CSV の破損を防ぎながら安全かつ速やかに処理を中止します。

---

### ステップ 2: 計画検索・絞り込み条件の指定（Tab 1: 🔍 計画検索・絞り込み条件 (Plan Search & Filter)）
1. **検索プリセット機能 (Presets) & 説明文の編集**:
   - カード最上段の **「🔖 Preset」** バーから、設定した検索条件セットをワンクリックで瞬時に保存・呼び出し・削除できます。
   - **画面上での Description（説明文）直接編集**:
     - プリセットバーの 2 行目に **「Description:」** 入力欄が常設されています。プリセットを選択するとその説明文が即座に表示され、画面上でそのまま臨床プロトコルの詳細や注意点を編集・追記できます。
     - **「💾 Save」** ボタンを押すことで、指定された検索条件とともに更新された Description が `.json` ファイルへ同時に保存・更新されます。
   - **保存場所（アプリ本体と同じフォルダの `Presets/` フォルダ）**:
     - プリセットは、**`EclipseDataMiner.exe` と同じ階層にある `Presets/` フォルダ** 配下に、プリセット名ごとの個別 JSON ファイル（例: `Presets/Prostate VMAT 78Gy.json`）として自動保存されます。
     - エクスプローラーでこのフォルダを開き、他端末や同僚が作成した `.json` ファイルを配置するだけで即座にプリセットとして認識されます。
   - **自由な保存と削除（組み込み制限なし）**:
     - 画面上で設定した検索条件は、ComboBox に新しい名前（例: `Breast HypoFr 40Gy`）を入力し、**「💾 Save」** ボタンを押すだけで保存されます。
     - 不要になったプリセットは **「🗑 Delete」** ボタンを押すことで、UI および `Presets/` フォルダ内のファイルから完全に削除できます（特定のプリセットに対する削除禁止保護はありません）。
   - **臨床サンプルプリセット**:
     - `Templates/Presets/` フォルダ内に、頻出の臨床プロトコルサンプル（前立腺 78Gy、肺 SBRT、頭頸部 70Gy、全承認計画）が同梱されています。必要に応じて `Presets/` フォルダへコピーして使用できます。

2. **Patient ID / Course ID / Plan ID / Target Volume**:
   - 複数キーワードをカンマ (`,`) で区切って入力します（例: `1001, 1002, 2005` または `VMAT, IMRT`）。
   - **NOT / Exclude 除外検索 (`!` または `-`)**:
     - 単語の先頭に `!` または `-` を付与すると、その文字列を含む計画を**除外**します。
     - **包含＋除外の混在**: `VMAT, IMRT, !QA, !Test`（VMAT または IMRT であり、かつ "QA" や "Test" を含まない計画のみを抽出）。
     - **除外単独指定**: `!QA, !Verify`（QA や Verify 以外の全計画を抽出）。
   - **一致モード選択（Contains / Exact / Regex）**:
     - 各入力欄の右側のドロップダウンで、項目ごとにマッチング方式を選択できます：
       - `Contains`（デフォルト）: **部分一致**。指定した文字列が含まれる計画を幅広く抽出（大文字小文字無視）。
       - `Exact`: **完全一致**。指定した名称と完全に一致する計画のみを抽出（例: Plan ID に `Exact` で `Plan` を指定すると、`Plan1` や `MyPlan` はヒットせず `Plan` のみが合致）。
       - `Regex`: **正規表現**。柔軟なパターン照合（例: `^Plan_\d+$` や `(VMAT|IMRT)_Prostate`）。
   - **計画検索用 正規表現ヒント (❓ Regex Hints ▾)**:
     - プリセットバー右端の **「❓ Regex Hints ▾」** ボタンをクリックすると、臨床計画検索に特化した正規表現チートシートがポップアップ表示されます。
     - 複数手法OR (`(VMAT|IMRT)`）、末尾指定 (`.*_Boost$`）、疾患接頭辞 (`^(HN|Lung)_.*`）、ターゲット＆線量パターン (`PTV.*60(Gy)?`）などの臨床頻出スニペットを、ワンクリックで **クリップボードへコピー (Copy)** または **Plan ID 欄へ直接挿入 (Insert)** できます。

3. **線量・分割数の範囲・不等号指定 (Dose/Fr, Fractions, Total Dose)**:
   - 単一値だけでなく、臨床で一般的な**範囲指定**および**不等号**によるフィルタリングに完全対応しています：
     - **範囲指定 (`-` または `~`)**: `70-80` や `70 ~ 80`（70Gy から 80Gy の範囲、両端を含む）。分割数なら `4-5` や `33-35`。
     - **不等号 (`>=`, `>`, `<=`, `<`)**: `>= 10`（10Gy 以上）、`< 30`（30Gy 未満）、`<= 5`（5分割以下）。
     - **単一値**: `2.0` や `78`（線量は ±0.05Gy の許容誤差、分割数は完全一致）。
   - **NaN / Infinity（線量未計算・未定義計画）の確実な除外**:
     - 最適化前や線量未計算の計画で発生する `NaN` や `Infinity` 値は、範囲指定や不等号条件を指定した際に**誤ってヒットすることがないよう厳密に除外（false判定）** されます。また、一覧画面上でも `"-"` として安全にサニタイズ表示されます。

4. **線量計算の有無フィルタ (Dose Presence: All / HasDose / NoDose)**:
   - 計画に線量計算（Total Dose / Dose per Fraction）が存在するかどうかに基づいて直接絞り込めます：
     - **`All`**（デフォルト）: 線量の有無を問わず、すべての計画を対象とします。
     - **`HasDose`（線量あり）**: **線量計算が完了している計画のみ** を抽出します（未計算計画や線量ゼロの計画を完全に除外）。
     - **`NoDose`（線量なし）**: **線量が未計算（または未定義・NaN）の計画のみ** をピンポイントで抽出します（「線量計算前の作成中プラン」や「QA用未計算プラン」の探索に便利です）。

5. **Logic 切替 (AND / OR セグメント)**:
   - ボタン形式のセグメントスイッチで直感的に切り替えられます。
   - `AND`: 指定した全検索条件（Patient ID, Course ID, Plan ID, Target Volume, Dose/Fr, Fractions, Total Dose）を **すべて満たす** 計画のみを抽出。
   - `OR`: 指定した条件の **いずれか 1 つでも満たす** 計画を抽出（Patient ID や Course ID も OR 条件に含まれ、線量条件やプラン名等に合致する他患者・コースのプランも漏れなく抽出されます）。

6. **Approval Status (承認状態)**:
   - `Unapproved`（未承認）、`Plan approved`（計画承認）、`TRT approved`（治療承認）の対象をチェックボックスで選択します。

7. **Include PlanSum**:
   - 合算計画（PlanSum）も対象に含める場合はチェックを入れます（視認性の高いアンバー調のバッジスタイルで表示されます）。

8. **高度フィルタ（照射パラメータ・日付範囲フィルタ）**:
   - 検索条件カード下部の **「▾ Advanced Filters (Machine, Energy, Technique, Date Range)」** をクリックすると展開されます。
   - **照射パラメータ (Machine ID, Energy, Technique)**:
     - 計画内の通常照射ビーム（セットアップビームを除く）を走査して判定します。
     - カンマ (`,`) 区切りでの複数 OR 指定、および `!` や `-` による除外指定に対応しています。
       - **Machine ID**: 治療装置名（例: `TrueBeam, Clinac_iX, !QA_Linac`）。
       - **Energy**: 照射エネルギー（例: `6X, 10X, !6FFF`）。
       - **Technique**: 照射手法・MLC技術（例: `ARC, STATIC, !SRS`）。
   - **日付範囲フィルタ (Date Target & Date Range)**:
     - **Date Target**: 判定基準となる日付種別を選択（`TreatmentApprovalDate` [治療承認日・デフォルト]、`PlanningApprovalDate` [計画承認日]、`CreationDate` [計画作成日]）。
     - **Date Range (From 〜 To)**: カレンダーピッカーから開始日・終了日を選択（片側のみの指定も可能）。
     - **✕ Clear ボタン**: ワンクリックで設定した日付範囲を素早く解除。
   - ※ 高度フィルタの入力値は、すべて「🔖 Preset」の保存・復元に完全連動します。

---

### ステップ 3: 輪郭マッピング（Tab 2: 2- 📐 Structure Mapping）
輪郭名の表記揺れ（例: `PTV_60`, `ptv60`, `PTV-60Gy`）を単一カラムに集約するための機能です。画面は左右を仕切る **ドラッグ可能なスプリッター（GridSplitter）** を備えた 2 ペイン構成となっており、スキャンを実行してもロード済みのルールが消去・上書きされることはありません。

![Tab 2: 輪郭マッピングルールと事前スキャンプレビュー (Structure Mapping)](img/UI_Tab2_StructureMapping.png)

1. **Tab 2: 2- 📐 Structure Mapping** を開きます。
2. **左ペイン（マッピングルール定義）**:
   - **ルール件数バッジ**: 登録されているルール数がヘッダー部にリアルタイム表示されます。
   - **Load Rules (JSON)**: 定義済みルールファイルを読み込みます（前立腺がん用サンプル: `Templates/StructureMapping_Prostate.json`）。
   - **+ Add Rule / - Delete**: ルールを新規追加または削除します（削除時は1つ下の行へ自動選択とキーボードフォーカスが追従し、キーボードの **Delete キー連打** や「- Delete」ボタン連打で快適に連続削除が可能）。
   - **▲ Up / ▼ Down**: ルールの適用優先順位を上下に入れ替えます（同一マッチモード内では上にある行が優先されます）。
   - **Target Alias / Match Mode**: 統合名およびマッチング方式（`Exact` 完全一致、`Contains` 部分一致、`Regex` 正規表現）を編集します。
     - **特異度優先探索 (Exact > Contains > Regex)**: `Exact`（完全一致）が常に最優先され、次に `Contains`（部分一致）、最後に `Regex`（正規表現）が評価されます。そのため、全体的な正規表現ルールと個別例外ルールが安全に両立します。
     - **リアルタイム構文バリデーション**: `Regex` モード選択時、入力中のパターンに構文エラー（閉じ括弧忘れ等）がある場合は、セルの枠線と文字色が警告レッドに変化し、ホバー時に具体的なエラー理由ツールチップを表示して入力ミスを防止します。
   - **❓ Regex Hints ▾（臨床正規表現チートシート）**:
     - ツールバーのヒントボタンから、臨床現場で頻出の正規表現スニペット（前方一致 `^PTV.*`、左右指定 `.*[_-](Rt|Lt)$`、OR結合 `(Bladder|Rectum)`、線量付き `.*_\d+Gy$` 等）を一覧参照できます。
     - 「Insert」ボタンを押すと、選択中ルールのパターンにワンクリックで挿入され、`MatchMode` が自動的に `Regex` に切り替わります（クリップボードへのコピーも可能）。
   - **Extract チェックボックス**: 抽出不要な輪郭はチェックを外してオプトアウト（除外）します。
   - **Save Rules (JSON)**: 編集したルール辞書を JSON ファイルに保存します。
3. **右ペイン（事前スキャン ＆ プレビュー）**:
   - **スキャン件数バッジ**: 検出された輪郭の総種類数が表示されます。
   - **🔍 Pre-Scan Structures**: 検索条件に合致する全計画の輪郭 ID を高速スキャンします。スキャンを実行しても**左ペインのルールは一切消去されず**、左ペインのルールが即座に適用された解決結果（`Resolved Alias`）と判定ステータス（`Status`）がプレビュー表示されます。
   - **統合件数の注記**: 同一の Target Alias（例: `PTV`）に複数の生輪郭が統合される場合、ステータスに `[統合: 2件]` のように集約数が明記され、意図しない巻き込みがないか一目で確認できます。
   - **+ Add to Rules / 行のダブルクリック**: スキャンで見つかった輪郭を選択して「+ Add to Rules」を押すか、あるいは **リスト上の行をダブルクリック** することで、即座に左ペインのルール定義へ追加され、プレビューが自動再計算されます（登録済みの場合は重複防止しつつ該当ルールを選択）。
   - **クイックフィルタバー（リアルタイム絞り込み）**:
     - **テキスト検索**: 入力キーワードで `Raw Structure ID` や `Resolved Alias` を即時部分一致絞り込み。
     - **ステータス別絞り込み**: `All` / `Unmapped Only`（未マッピングのみ）/ `Mapped Only` / `Excluded Only` により、未処理の輪郭を素早く特定。
     - **件数表示 & クリア**: `Showing: X/Y` バッジで絞り込み後の件数比率を表示し、`Clear` ボタンで瞬時に全件表示へ復帰。
   - **↻ Refresh Preview**: ルール編集後、最新のプレビュー状態を手動で再計算・更新します。
4. **バイパス（スキップ）**:
   - 事前スキャンを行わずに即時抽出したい場合は、Tab 4 の **「Bypass Structure Pre-Scan」** をチェックして本検索へ進むことができます。
   - チェックを ON にすると、事前の輪郭確認ダイアログをスキップし、DQP リストに指定した輪郭名（またはロード済みのマッピング設定）に基づいて即座に抽出を開始します。
   - チェックが無効の状態で事前スキャンやマッピングを行わずに「Run Extraction」を押した場合は、マッピング漏れを防ぐための確認ダイアログが表示されます。

---

### ステップ 4: 線量品質パラメータ (DQP) の設定（Tab 3: 3- 📊 Dose Quality Parameters (DQP)）

![Tab 3: 線量品質パラメータ (DQP) 設定一覧 (Dose Quality Parameters)](img/UI_Tab3_DQP.png)

1. **Tab 3: 3- 📊 Dose Quality Parameters (DQP)** を開きます。
2. **情報カード**:
   - タブ上部のアラートカードに、**Volume, Max, Mean, Min dose が各輪郭について自動的に出力される旨**、および **相対線量の正規化基準が「処方総線量 (`TotalDose`)」である旨** が明記されています。個別登録は不要です。
3. **指標の追加・編集・削除**:
   - **+ Add DQP**: 新しい評価指標行をワンクリックで追加します。
   - **- Delete Selected**: 選択した評価指標行を削除します（削除時は1つ下の行へ自動選択とキーボードフォーカスが追従し、キーボードの **Delete キー連打** や「- Delete」ボタン連打で快適に連続削除が可能）。
   - **Structure Name / Alias**: 評価対象の輪郭名またはステップ 3 で設定した Target Alias。
   - **DQP Type**: 
     - `Dose` (D): 指定体積が受ける線量（例: D95%）
     - `Volume` (V): 指定線量を受ける体積（例: V20Gy, V70%）
     - `DoseComplement` (DC): 指定体積を除く部分の線量
     - `ComplementVolume` (CV): 指定線量未満の体積
   - **Value**: 指標の数値（95, 20, 0.1 など）。
   - **Input Unit**: `Relative [%]` または `Absolute [Gy / cc]`。
   - **Output Unit**: 出力単位（`Absolute [Gy / cc]` または `Relative [%]`）。
4. **Save / Load DQP (CSV)**: 疾患別テンプレートを CSV ファイルとして管理・再利用できます（ヘッダー: `structureName,DQPtype,DQPvalue,InputUnit,OutputUnit`）。

---

### ステップ 5: 出力先設定と抽出オプション（Tab 4: 4- ⚙️ Extraction & Analysis Options）

![Tab 4: 出力先ファイル設定と抽出・解析オプション (Extraction & Analysis Options)](img/UI_Tab4_Options.png)

**Tab 4: 4- ⚙️ Extraction & Analysis Options** を開き、出力したい項目を有効化します：
- **出力先ファイル設定 (Export File Destination)**:
   - **Output CSV File**: 出力先 CSV ファイルのフルパス。
   - **Browse...**: 保存先フォルダとファイル名をダイアログで指定。
   - **Open Folder**: 指定した保存先フォルダを Windows エクスプローラーで開く。
- **カード 1: Plan Metadata & Beam Parameters**:
   - Planning Approver, Approval Date, Calculation Model, Normalization Mode, Clinical Protocol, Optimization Objectives。
   - Beam MU, Machine / Energy / Technique, Calculation Log（改行エスケープサニタイズ済み）。
- **カード 2: Complexity Analysis, Privacy & Output Formats**:
   - **Plan Complexity Analysis**: Modulation Complexity Score (MCS), Edge Metric, Leaf Travel Length, Arc Length を自動計算。
   - **Anonymize Patient ID & Redact Personal Info**: 院外研究用に患者 ID を SHA-256 でハッシュ化し、生年月日や承認者名を `REDACTED` にマスク（プライバシーバッジ付き）。
   - **Simultaneously Export JSON Lines (.jsonl)**: 機械学習・AI 解析用に `.jsonl` 形式を同時出力。
   - **Bypass Structure Pre-Scan**: 事前輪郭スキャンの確認をバイパス。

---

### ステップ 6: データマイニング実行と進捗監視（画面最下部 ＆ ログコンソール）
1. 画面最下部のコントロールバーにある **「▶ Run Extraction」** ボタン（鮮やかなオーシャンブルー）をクリックします。
2. 稼働ステータスバッジが `EXTRACTING` に切り替わり、進捗バーがリアルタイムに更新されます。
3. **ダークターミナルログコンソール**:
   - 完全な黒背景（`#090D16`）と視認性の高いオフホワイトテキスト（`#F1F5F9`）、等幅フォント（`Consolas`）を採用。長時間の監視でも目が疲れず高い視認性を維持。
   - 上部ミニツールバーの **「📋 Copy」** で全ログをクリップボードにコピー可能。
   - **「🗑 Clear」** でコンソールをクリア可能。
4. 中断したい場合は **「⏹ Cancel」** ボタン（赤色 Danger スタイル）を押します（中断時もそこまでに抽出されたデータはファイルに安全に保存されます）。
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
