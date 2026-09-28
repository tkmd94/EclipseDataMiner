# EclipseDataMiner トラブルシューティング & FAQ ガイド

[English](TROUBLESHOOTING.en.md) | **日本語**

本ドキュメントは、**EclipseDataMiner (v3.0 / Eclipse v15.6 & v16.1 対応)** の起動、環境設定、UI 操作、検索フィルタリング、輪郭事前マッピング、DQP 指標計算、およびストリーミング CSV / JSONL 出力において発生しうるエラー・警告メッセージの原因と具体的な対処方法をまとめたトラブルシューティングガイドです。

---

## 目次

1. [起動・環境関連トラブル](#1-起動環境関連トラブル)
   - 1.1 [ESAPI DLL が見つからない (`FileNotFoundException`)](#11-esapi-dll-が見つからない-filenotfoundexception)
   - 1.2 [.NET Framework バージョン不一致で起動しない](#12-net-framework-バージョン不一致で起動しない)
   - 1.3 [スクリプト実行権限エラー / スタンドアロン実行権限](#13-スクリプト実行権限エラー--スタンドアロン実行権限)
   - 1.4 [FIPS 暗号化ポリシー例外 (`InvalidOperationException: This implementation is not part of the Windows Platform FIPS validated cryptographic algorithms`)](#14-fips-暗号化ポリシー例外-invalidoperationexception-this-implementation-is-not-part-of-the-windows-platform-fips-validated-cryptographic-algorithms)
2. [検索・抽出処理中のトラブル](#2-検索抽出処理中のトラブル)
   - 2.1 [処理中に UI がフリーズする / 応答なしになる](#21-処理中に-ui-がフリーズする--応答なしになる)
   - 2.2 [大量計画抽出時にメモリ不足 (`OutOfMemoryException`) で落ちる](#22-大量計画抽出時にメモリ不足-outofmemoryexception-で落ちる)
   - 2.3 [途中で処理を中止したい / キャンセル時のデータ保存](#23-途中で処理を中止したい--キャンセル時のデータ保存)
   - 2.4 [特定の患者・計画でエラーが出て抽出が中断する](#24-特定の患者計画でエラーが出て抽出が中断する)
3. [DQP・輪郭・ファイル出力トラブル](#3-dqp輪郭ファイル出力トラブル)
   - 3.1 [出力 CSV の DQP 列がすべて `N/A` になる](#31-出力-csv-の-dqp-列がすべて-na-になる)
   - 3.2 [輪郭事前マッピングで表記揺れが統合されない](#32-輪郭事前マッピングで表記揺れが統合されない)
   - 3.3 [CSV 保存時に「プロセスはファイルにアクセスできません」エラーが出る](#33-csv-保存時にプロセスはファイルにアクセスできませんエラーが出る)
   - 3.4 [Excel で CSV を開くと文字化けする](#34-excel-で-csv-を開くと文字化けする)
4. [よくある質問 (FAQ)](#4-よくある質問-faq)

---

## 1. 起動・環境関連トラブル

### 1.1 ESAPI DLL が見つからない (`FileNotFoundException`)
- **現象**: `EclipseDataMiner.exe` を起動しようとすると、`Could not load file or assembly 'VMS.TPS.Common.Model.API.dll'` というエラーメッセージが表示されて即座に終了する。
- **原因**: 
  - 本ツールはスタンドアロン型 ESAPI アプリケーションであるため、Eclipse がインストールされていない PC や、ESAPI のインストールパスが標準と異なる環境では DLL をロードできません。
  - `Costura.Fody` による依存内包では、Varian の規約およびライセンス保護のため ESAPI DLL は意図的に除外されています。
- **対処方法**:
  - **Eclipse 端末上で実行**: Varian Eclipse クライアントまたは TBox（開発・テスト端末）上で実行してください。
  - **DLL の手動配置**: 開発・検証環境で実行する場合は、`EclipseDataMiner.exe` と同じフォルダに `VMS.TPS.Common.Model.API.dll` および `VMS.TPS.Common.Model.Types.dll` をコピーして配置してください。

### 1.2 .NET Framework バージョン不一致で起動しない
- **現象**: 起動時に `.NET Framework Initialization Error` が表示される。
- **原因**: 端末に .NET Framework 4.6.1 以上がインストールされていません。
- **対処方法**:
  - Windows の機能の有効化または Microsoft 公式サイトより、.NET Framework 4.6.1 以上（推奨: 4.8）をインストールしてください。

### 1.3 スクリプト実行権限エラー / スタンドアロン実行権限
- **現象**: アプリケーション起動直後、または抽出実行時に `Access Denied` や `User has insufficient rights` が発生する。
- **原因**: ログイン中の Windows アカウントに、ESAPI スタンドアロン実行権限（臨床データベースまたは研究データベースへの読み取り権限）が付与されていません。
- **対処方法**:
  - Eclipse のユーザー管理者（System Administrator）に依頼し、該当アカウントの ESAPI 権限（Scripting / Read Rights）を有効化してください。

### 1.4 FIPS 暗号化ポリシー例外 (`InvalidOperationException: This implementation is not part of the Windows Platform FIPS validated cryptographic algorithms`)
- **現象**: 
  - 研究用・検証用端末では正常に動作するが、実環境（臨床用 Eclipse / ARIA 端末）で実行した際に以下のエラーが発生して起動または処理が停止する。
  ```text
  Error occured while processing request
  System.InvalidOperationException: This implementation is not part of the Windows Platform FIPS validated cryptographic algorithms.
  ```
- **原因**: 
  - 病院の臨床環境や医療情報システム端末では、セキュリティ強化のため Windows のグループポリシー（GPO）またはローカルセキュリティポリシーにおいて、**「システム暗号化: 暗号化、ハッシュ、および署名のための FIPS 準拠アルゴリズムを使用する」 (FipsAlgorithmPolicy)** が「有効」に設定されています。
  - .NET Framework ではこのポリシーが有効な場合、標準の暗号化プロバイダ（`SHA256Managed` 等）のインスタンス生成が OS レベルでブロックされ、上記の例外がスローされます。これは Varian ESAPI 内部（WCF/Gateway 接続・認証トークン処理）や患者匿名化ハッシュ生成時にも影響します。
- **対処方法**:
  1. **構成ファイル（`EclipseDataMiner.exe.config`）の同封確認（推奨）**:
     - `EclipseDataMiner.exe` を実環境のフォルダに配置する際は、**必ず同一フォルダに `EclipseDataMiner.exe.config` もセットで配置** してください。
     - 本構成ファイル内の `<enforceFIPSPolicy enabled="false"/>` ディレクティブにより、OS の FIPS 制限下でもアプリケーションおよび ESAPI 内部の暗号通信が安全に動作するようバイパス制御されます。
     - ※ v3.0 以降では、アプリケーション内部のハッシュ処理（`StringSanitizer.AnonymizePatientId`）も Windows プラットフォーム認定済みの FIPS 準拠クラス（`SHA256CryptoServiceProvider` / `SHA256Cng`）に最適化されています。
  2. **端末側での FIPS ポリシー変更（管理者権限がある場合）**:
     - 「ファイル名を指定して実行」（`Win + R`）で `secpol.msc` を起動します。
     - [セキュリティの設定] → [ローカル ポリシー] → [セキュリティ オプション] を開きます。
     - **「システム暗号化: 暗号化、ハッシュ、および署名のための FIPS 準拠アルゴリズムを使用する」** をダブルクリックし、「**無効**」に変更して適用後、PC を再起動（または `gpupdate /force`）します。
     - *※ 病院のドメインポリシー（GPO）で一括管理されている場合は、情報システム部またはセキュリティ管理者の指示に従ってください。*

---

## 2. 検索・抽出処理中のトラブル

### 2.1 処理中に UI がフリーズする / 応答なしになる
- **現象**: 検索や抽出の実行中、ウィンドウのタイトルバーに「応答なし」が表示され、操作を受け付けなくなる。
- **原因**: v1.0 以前の旧実装では UI スレッド上で直接 ESAPI ループを実行していたためブロックが発生していました。
- **対処方法**:
  - **v3.0 へのアップデート**: 最新の v3.0 では専用の STA バックグラウンドワーカースレッド（`StaEsapiWorkerService`）に分離されているため、処理中も UI は完全に軽快に動作し、進捗バーやキャンセルボタンがリアルタイムに応答します。

### 2.2 大量計画抽出時にメモリ不足 (`OutOfMemoryException`) で落ちる
- **現象**: 1,000 件〜5,000 件以上の計画を走査している途中でメモリ使用量が 4GB を超え、`OutOfMemoryException` が発生して強制終了する。
- **原因**:
  - ESAPI はアンマネージド C++ コアで構成されており、`ClosePatient()` の呼び出し漏れや GC（ガベージコレクション）の遅延により内部メモリが蓄積します。
  - メモリ上に全抽出データを蓄積してから最後にファイル出力する設計の場合に発生します。
- **対処方法**:
  - **v3.0 のストリーミングパイプラインの活用**:
    - v3.0 では 1 プラン抽出完了ごとにディスクへ即時ストリーミングフラッシュされます。
    - 200 患者走査ごとに明示的な `GC.Collect()` および `GC.WaitForPendingFinalizers()` が自動実行され、メモリ消費量は常に数十 MB〜100MB 程度で平坦に保たれます。

### 2.3 途中で処理を中止したい / キャンセル時のデータ保存
- **現象**: 処理に時間がかかりすぎるため中断したいが、強制終了（タスクマネージャー等）すると抽出済みデータが消えてしまう。
- **対処方法**:
  - 画面上部アクションバーの **「⏹ Cancel」** ボタンを押下してください。
  - 協調的キャンセル（`CancellationToken`）により、現在処理中の患者を安全に閉じた上で処理が停止します。
  - **ストリーミング出力により、キャンセル直前までに抽出されたすべての行は CSV / JSONL ファイルに安全に保存されています。**

### 2.4 特定の患者・計画でエラーが出て抽出が中断する
- **現象**: 線量未計算のプランや最適化設定がないプランに遭遇した際、例外が発生してツール全体が停止する。
- **対処方法**:
  - v3.0 では多層フェイルセーフ機構が組み込まれており、輪郭欠落、線量未計算、PlanSum 不在項目などは自動的に検知され、該当列に `N/A` を出力して安全に次の計画へスキップ・継続します。

---

## 3. DQP・輪郭・ファイル出力トラブル

### 3.1 出力 CSV の DQP 列がすべて `N/A` になる
- **現象**: 抽出結果の CSV で、`<Structure>-D95%[Gy]` などの DQP 列の値がすべて `N/A` になる。
- **主な原因**:
  1. DQP 設定の **`structureName`** と、実際の患者の輪郭名（または Target Alias）が一致していない。
  2. 輪郭事前スキャン（Pre-Scan）で該当輪郭の **「Extract」チェックボックスが外れている（オプトアウト）**。
  3. プランに線量が計算されていない（`plan.Dose == null`）。
- **対処方法**:
  - **Pre-Scan の活用**: 本検索の前に「Pre-Scan Structures」を実行し、実際の輪郭名を確認してエイリアス（Target Alias）を DQP 設定と一致させてください。
  - **大文字・小文字の確認**: 照合はインセンシティブ（大文字小文字無視）ですが、全角半角やスペース（`PTV_60` と `PTV 60`）の不一致に注意してください。

### 3.2 輪郭事前マッピングで表記揺れが統合されない
- **現象**: `PTV_60` と `ptv60` が別々の列に出力されてしまう。
- **原因**: マッチングルール（Match Mode）が `Exact`（完全一致）のままになっている。
- **対処方法**:
  - Tab 1 で対象ルールの **Match Mode** を `Contains`（部分一致）または `Regex`（正規表現、例: `PTV.*|ptv.*`）に変更し、**Target Alias** を共通の名称（例: `PTV`）に設定してください。

### 3.3 CSV 保存時に「プロセスはファイルにアクセスできません」エラーが出る
- **現象**: 抽出開始時に `The process cannot access the file because it is being used by another process` エラーが発生する。
- **原因**: 出力先に指定した CSV ファイルが **Microsoft Excel 等の別アプリケーションで開かれたまま** になっています。Excel は開いているファイルに対して排他ロックをかけるため、外部ツールからの書き込みが拒否されます。
- **対処方法**:
  - Excel 等で開いている該当 CSV ファイルを閉じてから、再度「▶ Run Extraction」をクリックしてください。

### 3.4 Excel で CSV を開くと文字化けする
- **現象**: 出力された CSV を Excel でダブルクリックして開くと、日本語部分（患者名、ログ等）が文字化けする。
- **原因**: Excel はデフォルトで Shift-JIS として CSV を開くため、UTF-8 エンコードの CSV で文字化けが発生します。
- **対処方法**:
  - **BOM 付き UTF-8 出力**: 本ツールの CSV 出力は Excel 互換の UTF-8 (BOM付き) で書き出されるため、通常はそのまま開けます。
  - **Excel の「データ」タブから読み込み**: Excel の「データ」→「テキストまたは CSV から」を選択し、文字コードに「65001 : Unicode (UTF-8)」を指定してインポートしてください。

---

## 4. よくある質問 (FAQ)

### Q1. PlanSum（合算計画）で抽出できない項目があるのはなぜですか？
**A**: ESAPI の仕様上、`PlanSum` には単一のビーム照射野（Beam）、MLC、MU、および最適化目標（Optimization Objectives）が存在しません。本ツールでは、PlanSum に存在しない項目に対して安全に `N/A` を出力し、DVH 統計量や DQP 指標のみを正確に抽出します。

### Q2. 相対線量指標（例: V70%）の 100% 基準線量は何ですか？
**A**: プラン情報の **「処方総線量 (`PlanSetup.TotalDose`)」** を 100% の基準として計算します。処方線量が未定義の例外的なケースでは、安全フォールバックとして計画内最大線量（`dvh.MaxDose`）が参照されます。

### Q3. Dmean, Dmax, Dmin は DQP リストに登録する必要がありますか？
**A**: **登録不要です。** 対象の全輪郭について、個別の DQP 登録なしに `<Structure>-Volume[cc]`, `<Structure>-Max dose[Gy]`, `<Structure>-Mean dose[Gy]`, `<Structure>-Min dose[Gy]` が自動的に必ず出力されます。DQP リストには、可変パラメータを伴う指標（D95%, V20Gy, D0.1cc 等）のみを登録してください。

### Q4. Python (Pandas / PyTorch) でデータを読み込む最適な方法は？
**A**:
- 表形式の統計解析には **CSV 出力**（`pd.read_csv("output.csv")`）をご利用ください。
- ビーム詳細や最適化パラメータなど階層構造を含む機械学習用途には、オプションの **JSON Lines (`.jsonl`) 同時出力** を有効化し、`json.loads()` で読み込むのが最適です（詳細は [docs/MANUAL.md](docs/MANUAL.md) を参照）。
