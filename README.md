# EclipseDataMiner (v3.0.0)

[English](README.en.md) | **日本語**

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen.svg)]()
[![Platform](https://img.shields.io/badge/.NET%20Framework-4.6.1-blue.svg)]()
[![Target](https://img.shields.io/badge/Architecture-x64-orange.svg)]()
[![Eclipse](https://img.shields.io/badge/Eclipse-v15.6%20%7C%20v16.1-purple.svg)]()
[![Tests](https://img.shields.io/badge/MSTest-116%2F116%20PASS-success.svg)]()
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

Varian 社製放射線治療計画装置 Eclipse (ESAPI) 上で動作する、**10,000 件規模の治療計画データマイニング・スタンドアロンアプリケーション** です。  
患者データベースから指定条件に合致する計画（PlanSetup / PlanSum）を横断探索し、線量品質指標 (DQP)、照射パラメータ、臨床プロトコル、最適化条件、および照射野複雑度 (Modulation Complexity Score, Edge Metric 等) を高速・安全に抽出して、正規化された CSV および機械学習用途の JSON Lines (`.jsonl`) 形式でストリーミング出力します。

---

## 🌟 主な特徴と新機能 (v3.0.0)

- 🌐 **標準英語 UI ＆ 日英バイリンガルドキュメント**:
  - ESAPI 臨床スクリプトの国際標準に準拠した洗練された英語 UI を採用。
  - 操作マニュアルおよび各種技術ドキュメントは日本語版と英語版を両方配備。
- 🚀 **10,000 件規模のストリーミング出力パイプライン**:
  - 全データをメモリに溜め込まず、1 患者・1 プランごとにディスクへ即時フラッシュ。長時間実行時もメモリ消費を数十 MB 程度で平坦に維持。
- 🔍 **輪郭事前マッピング（Pre-Scan）＆ エイリアス統合**:
  - 本検索の前に輪郭 ID を高速スキャンし、表記揺れ（`PTV_60`, `ptv60`, `PTV-60Gy` 等）を正規表現や部分一致で単一カラムに集約。
  - マッピング設定は JSON ファイルとして永続化・再利用可能。
- 🔀 **階層的 AND / OR フィルタリング ＆ 線量有無判定**:
  - カンマ (`,`) 区切り入力によるフィールド内 OR 検索に加え、全条件を満たす「AND」と、いずれかを満たす「OR」をワンタッチで切り替え。
  - 線量計算の有無（`All` / `HasDose` [線量あり] / `NoDose` [線量なし]）フィルタにより、未計算プランの除外や作成中プランの絞り込みに対応。
  - 範囲指定（`70-80`）や不等号（`>=10`）による線量・分割数検索、照射パラメータ（Machine/Energy/Technique）および日付範囲フィルタ。
  - 合算計画 (`PlanSum`) の抽出オプション対応。
- 🤖 **機械学習・AI 解析向け JSON Lines (`.jsonl`) 同時出力**:
  - 階層構造（ビーム、最適化パラメータ、DVH 指標）を保持したまま、Python (Pandas / PyTorch 等) で即座に読み込めるフォーマットを出力。
- 🔒 **プライバシー保護・患者データ匿名化**:
  - 院外研究・共同研究用途向けに、Patient ID の SHA-256 ハッシュ化および個人情報（生年月日、承認者名等）の `REDACTED` マスキングに対応。
- 📏 **線量単位の自動正規化 (Gy 統一)**:
  - ESAPI の `cGy` / `Gy` 混在を自動検出し、すべて `Gy` 単位に統一換算。
- 🛡️ **堅牢な STA ワーカースレッド ＆ メモリ安全機構**:
  - ESAPI の STA 制約を遵守した専用バックグラウンドワーカーにより UI フリーズを完全防止。
  - 200 件ごとの定期 `GC.Collect()` と `try-finally` による確実な患者解放でメモリリークを根絶。
- 📦 **単一自己完結型バイナリ (Costura.Fody)**:
  - 外部 NuGet 依存 DLL を `EclipseDataMiner.exe` 内に統合。Eclipse 端末への単一ファイル配布・即実行が可能。

---

## 🖥️ 動作環境

- **OS**: Windows 10 / 11 / Windows Server (64-bit)
- **治療計画装置**: Varian Eclipse v15.6 / v16.1
- **ランタイム**: .NET Framework 4.6.1 以上
- **実行権限**: ESAPI スクリプト実行権限（Eclipse 端末または TBox 上で実行）

---

## 🚀 クイックスタート・導入方法

本ツールはスタンドアロン型実行ファイルです。

1. **ビルドまたは配布パッケージの取得**:
   - `release\EclipseDataMiner_v3.0.0.exe` を取得します（または `EclipseDataMiner\bin\x64\Release\EclipseDataMiner.exe`）。
2. **Eclipse 端末への配置**:
   - 取得した `EclipseDataMiner_v3.0.0.exe` を Eclipse 端末上の任意のフォルダに配置します。
3. **起動**:
   - `EclipseDataMiner_v3.0.0.exe` をダブルクリックして起動します。

---

## 🖥️ 画面構成

![EclipseDataMiner UI メイン画面](img/UI.png)

---

## 📖 基本操作フロー

メイン画面は番号付きの 4 つのタブで構成され、左から右へ直感的に設定を進めることができます：

```
[1- Plan Search]       ->  [2- Structure Mapping]  ->  [3- DQP 設定]
 検索条件指定・プリセット   輪郭事前スキャン            線量評価指標 (D95%等)
 プラン検索・一覧で選択     エイリアスマッピング
       ↓
[4- Options & 出力先]  ->  [最下部: 実行コントロール]
 出力先 CSV/JSONL 指定     [▶ Run Extraction] ワンクリック抽出
 複雑度・匿名化設定        リアルタイム進捗監視・中断対応
```

1. **Tab 1: 1- 📋 Plan Search**:
   - 上部の「🔍 計画検索・絞り込み条件 (Plan Search & Filter)」で患者・計画・線量・承認状態・装置・日付範囲を指定（またはプリセット呼び出し）。
   - 「🔍 Search Plans」ボタンで合致プランを高速走査し、下部の一覧表で抽出対象プランを選択。
2. **Tab 2: 2- 📐 Structure Mapping**:
   - 「🔍 Pre-Scan Structures」で輪郭名をスキャンし、表記揺れを単一のカラム（Target Alias）へ統合。
3. **Tab 3: 3- 📊 Dose Quality Parameters (DQP)**:
   - D95%, V20Gy などの線量・体積指標を追加・設定（テンプレート CSV の読込・保存対応）。
4. **Tab 4: 4- ⚙️ Extraction & Analysis Options**:
   - 出力先 CSV ファイルパスの指定（Browse... / Open Folder）。
   - 照射パラメータ、プラン複雑度 (MCS/Edge Metric)、匿名化 (SHA-256)、JSONL 同時出力などのオプションを設定。
5. **抽出実行（画面最下部）**:
   - 画面最下部のコントロールバーにある **「▶ Run Extraction」** をクリックして実行。同バー内の進捗バーおよびコンソールログでリアルタイムに進捗を監視できます。
---

## 📊 出力データ仕様

### 1. CSV 出力 (フラット形式)
- 1 プラン = 1 行（カンマ区切り、UTF-8 with BOM）。
- 基本計画情報 (11列) ＋ オプション情報 (最大10列) ＋ 輪郭基本統計量 (4列/輪郭) ＋ 動的 DQP 列で構成。全カラムの詳細解説は [マニュアル第4章 (docs/MANUAL.md#4)](docs/MANUAL.md#4) を参照してください。
- 改行コードはスペース置換、カンマや引用符は適切にサニタイズエスケープされます。
- 欠損値や該当しない項目には `N/A` が出力されます。

| 列名（主要項目例） | 内容 | 例 |
| :--- | :--- | :--- |
| `Patient ID` | 患者 ID（匿名化時は SHA-256 ハッシュ） | `PT_1001` または `e3b0c442...` |
| `Course ID` / `Plan ID` | コース名 / 計画名 | `C1`, `VMAT_Prostate` |
| `DosePerFraction[Gy]` | 1回処方線量 (Gy) | `2.00` |
| `TotalDose[Gy]` | 総処方線量 (Gy) | `60.00` |
| `ApprovalStatus` | 承認状態 | `TreatmentApproved` |
| `MU` | 各ビームの MU 値 (セミコロン連結) | `B1:145.2;B2:138.6` |
| `Machine/Energy/Tech/PlanType` | 照射パラメータまとめ | `TrueBeam:6X:ARC:VMAT` |
| `PlanComplexity` | 複雑度指標 (Beam:MCS,EM,LeafTravel,ArcLength) | `(B1:0.28,0.12,1850.4,360.0)` |
| `<Structure>-Volume[cc]` | 輪郭体積（自動出力） | `42.50` |
| `<Structure>-Max dose[Gy]` | 輪郭最大線量（自動出力） | `64.20` |
| `<Structure>-Mean dose[Gy]` | 輪郭平均線量（自動出力） | `61.80` |
| `<Structure>-Min dose[Gy]` | 輪郭最小線量（自動出力） | `58.10` |
| `<Structure>-D95%[Gy]` | DQP 算出線量 | `59.80` |
| `<Structure>-V70%[%]` | DQP 算出体積割合（処方総線量基準） | `18.50` |

### 2. JSON Lines (`.jsonl`) 出力
- 1 プラン = 1 行の JSON オブジェクト。
- Python での読み込み：
  ```python
  import json
  with open("DataMiningOutput.csv.jsonl", "r", encoding="utf-8") as f:
      plans = [json.loads(line) for line in f]
  print(f"Loaded {len(plans)} plans.")
  ```

---

## 📚 関連ドキュメント一式

本リポジトリは、医療現場における品質保証および安全管理基準に準拠した包括的なドキュメント体系を配備しています：

| ドキュメント | 概要・対象読者 |
| :--- | :--- |
| 📖 **詳細操作マニュアル** | [日本語 (docs/MANUAL.md)](docs/MANUAL.md) \| [English (docs/MANUAL.en.md)](docs/MANUAL.en.md) — 画面構成、操作手順、事前マッピング、DQP 設定、Python 連携例 |
| 📐 **アーキテクチャ設計書** | [日本語 (docs/ARCHITECTURE.md)](docs/ARCHITECTURE.md) \| [English (docs/ARCHITECTURE.en.md)](docs/ARCHITECTURE.en.md) — レイヤー構造、STAスレッドモデル、メモリ安全管理、シーケンス図 |
| 🏥 **臨床コミッショニング手順書** | [日本語 (docs/COMMISSIONING.md)](docs/COMMISSIONING.md) \| [English (docs/COMMISSIONING.en.md)](docs/COMMISSIONING.en.md) — TG-275 準拠の受入試験手順書、検証チェックリスト、臨床承認記録票 |
| 🔧 **トラブルシューティング & FAQ** | [日本語 (docs/TROUBLESHOOTING.md)](docs/TROUBLESHOOTING.md) \| [English (docs/TROUBLESHOOTING.en.md)](docs/TROUBLESHOOTING.en.md) — ESAPI 接続、メモリ不足、ファイルロック等のエラー対処法と FAQ |
| 🤝 **開発・コントリビューション規約** | [日本語 (docs/CONTRIBUTING.md)](docs/CONTRIBUTING.md) \| [English (docs/CONTRIBUTING.en.md)](docs/CONTRIBUTING.en.md) — 開発環境構築、ビルド・テスト手順、コーディング規約、コミット規約 |
| 📋 **詳細設計仕様書** | [日本語 (docs/DESIGN_SPECIFICATION.md)](docs/DESIGN_SPECIFICATION.md) \| [English (docs/DESIGN_SPECIFICATION.en.md)](docs/DESIGN_SPECIFICATION.en.md) — 要件定義、データ抽出仕様、ESAPI 制御詳細仕様 (v3.0.0) |
| 🛡️ **標準開発計画仕様書** | [日本語 (docs/STANDARD_DEVELOPMENT_PLAN.md)](docs/STANDARD_DEVELOPMENT_PLAN.md) \| [English (docs/STANDARD_DEVELOPMENT_PLAN.en.md)](docs/STANDARD_DEVELOPMENT_PLAN.en.md) — 7大品質原則、4層 DoD ゲート、自己完結再現性規約 |
| 📝 **更新履歴** | [日本語 (docs/CHANGELOG.md)](docs/CHANGELOG.md) \| [English (docs/CHANGELOG.en.md)](docs/CHANGELOG.en.md) — バージョン別変更履歴 (Keep a Changelog 準拠) |
| 📄 **サードパーティライセンス通知** | [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) — 同梱・利用しているオープンソースライブラリの一覧およびライセンス条文 |
| 📖 **総合技術マニュアル PDF** | [日本語 (release/EclipseDataMiner_v3.0.0_Manual.pdf)](release/EclipseDataMiner_v3.0.0_Manual.pdf) \| [English (release/EclipseDataMiner_v3.0.0_Manual.en.pdf)](release/EclipseDataMiner_v3.0.0_Manual.en.pdf) — 全ドキュメントを結合・出版品質でレイアウトした A4 印刷対応 PDF |

---

## 🧪 テスト・品質保証

本プロジェクトは `EclipseDataMiner.Tests` (MSTest) による多層自動テストを配備しており、ESAPI データベース非接続環境でもコアロジックを 100% 検証可能です。

- **テスト件数**: 116 件 (100% PASS)
- **テストカバレッジ**: 線量正規化 (`ToGy()`)、文字列サニタイズ・エスケープ、患者匿名化、CSV/JSONL ストリーミング出力、階層的 AND/OR 検索フィルタ＆UIトグル連動、線量有無判定フィルタ (HasDose/NoDose)、不等号・範囲数値フィルタ、日付・照射野パラメータフィルタ、プリセット管理＆説明文永続化、2ペイン輪郭マッピング＆リアルタイムプレビュー、特異度優先 (Exact > Contains > Regex) ＆ 上下順序制御、JSON 永続化、照射野複雑度解析アルゴリズム（MCS, Edge Metric, Leaf Travel Length, Arc Length 文献値完全一致検証）、XAMLリソース整合性検証

---

## 📄 ライセンス

本ソフトウェアは [MIT License](LICENSE) の下で公開されています。
同梱・利用しているサードパーティ製オープンソースソフトウェアの著作権情報およびライセンス条文（MIT License, BSD 3-Clause, Apache 2.0）については、[サードパーティライセンス通知 (THIRD-PARTY-NOTICES.md)](THIRD-PARTY-NOTICES.md) をご参照ください。
