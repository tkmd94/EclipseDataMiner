# EclipseDataMiner 開発・コントリビューション規約 (Contributing Guide)

本ドキュメントは、**EclipseDataMiner** の開発に参加する開発者・医学物理士のためのガイドライン、開発環境の構築手順、コーディング規約、テスト方針、およびリリース手順をまとめたものです。

---

## 目次

1. [開発環境の要件](#1-開発環境の要件)
2. [リポジトリのセットアップとビルド](#2-リポジトリのセットアップとビルド)
3. [自動検証パイプライン (`test.bat`)](#3-自動検証パイプライン-testbat)
4. [アーキテクチャ設計規約](#4-アーキテクチャ設計規約)
5. [コーディング規約](#5-コーディング規約)
6. [コミットメッセージ規約 (Conventional Commits)](#6-コミットメッセージ規約-conventional-commits)
7. [プルリクエスト (PR) チェックリスト](#7-プルリクエスト-pr-チェックリスト)

---

## 1. 開発環境の要件

- **OS**: Windows 10 / 11 (64-bit)
- **IDE**: Visual Studio 2022 Community / Professional 以上
  - 必須ワークロード: **.NET デスクトップ開発**
  - .NET Framework 4.6.1 開発者パック (または Targeting Pack)
- **ターゲットプラットフォーム**: `.NET Framework 4.6.1` / `x64`
- **言語バージョン**: C# 10.0 (`<LangVersion>10.0</LangVersion>`)
- **外部 ESAPI ライブラリ**:
  - `VMS.TPS.Common.Model.API.dll` (v15.6 または v16.1)
  - `VMS.TPS.Common.Model.Types.dll` (v15.6 または v16.1)
  - ※ プロジェクトファイルは以下のパスを自動探索します：
    1. `C:\Program Files\Varian\RTM\16.1\esapi\API`
    2. リポジトリ親階層 (`..\..\`)
    3. ソリューション親階層 (`$(SolutionDir)..`)

---

## 2. リポジトリのセットアップとビルド

### NuGet パッケージの復元
本リポジトリは標準の **`PackageReference` 方式** を採用しており、ルートの `nuget.config` に基づいて依存ライブラリ（`CommunityToolkit.Mvvm`, `System.Text.Json`, `Costura.Fody`, `MSTest` 等）を `packages/` フォルダへ自動取得・復元します。

```powershell
# NuGet パッケージの復元
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" EclipseDataMiner.sln /t:Restore /verbosity:minimal
```

### ソリューションのビルド
ESAPI は 64-bit ネイティブライブラリであるため、ビルドプラットフォームは常に **`x64`** を指定してください（`AnyCPU` は実行時エラーとなります）。

```powershell
# x64 Release ビルド
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" EclipseDataMiner.sln /p:Configuration=Release /p:Platform=x64 /t:Build /verbosity:minimal
```

ビルドが完了すると、`EclipseDataMiner\bin\x64\Release\EclipseDataMiner.exe` に単一自己完結バイナリ（約 589 KB）が生成されます。

---

## 3. 自動検証パイプライン (`test.bat`)

リポジトリルートに配置されている `test.bat` を実行することで、**NuGet復元 → x64 Release ビルド → MSTest 全件実行 → `release/` ディレクトリへの成果物集約** をワンタッチで完全自動実行できます。

```cmd
# 自動検証パイプラインの実行
.\test.bat
```

> [!IMPORTANT]
> コードの変更やプルリクエストの作成前には、必ず `test.bat` を実行し、**100% (24/24 PASS)** することを確認してください。

---

## 4. アーキテクチャ設計規約

### 4.1 中間 DTO 疎結合アーキテクチャ
- **ESAPI 非依存のテスト容易性**:
  - ESAPI のオブジェクト（`PlanSetup`, `Structure`, `Beam` 等）から、独立した純粋な C# モデル（`ExtractionPlanRecord`, `BeamRecord`, `DvhMetricResult` 等）へ即座にマッピングします。
  - CSV / JSONL ストリーミング出力、サニタイズ処理、検索論理判定、エイリアスマッピングは、ESAPI データベースに接続しないオフライン環境で 100% 単体テスト可能でなければなりません。

### 4.2 STA ワーカースレッド ＆ メモリ安全原則
- **UI フリーズ防止**:
  - ESAPI へのアクセスは、UI スレッドをブロックしない専用バックグラウンド STA スレッド（`StaEsapiWorkerService`）で直列（シーケンシャル）に実行します。
- **メモリリークの根絶**:
  - 1 患者の処理ごとに `try-finally` で確実に `app.ClosePatient()` を呼び出します。
  - 200 患者走査ごとに明示的な `GC.Collect()` と `GC.WaitForPendingFinalizers()` を実行します。

### 4.3 Costura.Fody 依存性埋め込みルール
- 外部 NuGet 依存 DLL（`CommunityToolkit.Mvvm`, `System.Text.Json` 等）は、配布の利便性のため `EclipseDataMiner.exe` 内に自動内包します。
- **ESAPI アセンブリの内包禁止**:
  - `VMS.TPS.Common.Model.API` および `Types` は、実行環境の Eclipse バージョンに動的バインドさせるため、[FodyWeavers.xml](file:///g:/Source/Repos/tkmd94/EclipseDataMiner/EclipseDataMiner/FodyWeavers.xml) の `ExcludeAssemblies` に必ず含め、内包から除外してください。

---

## 5. コーディング規約

### 5.1 基本コーディング標準
- **C# 命名規約**:
  - クラス、メソッド、プロパティ: `PascalCase`
  - プライベートフィールド: `_camelCase`
  - メソッド引数、ローカル変数: `camelCase`
- **線量単位の正規化 (Gy統一原則)**:
  - ESAPI から取得した線量値には必ず `ToGy()` 拡張メソッドを経由させ、内部・出力ともに `Gy` 単位に統一してください（`cGy` の混在を厳禁とします）。
- **CSV エスケープ・サニタイズ原則**:
  - 文字列出力時には `StringSanitizer.EscapeCsv()` を使用し、改行コードのスペース置換およびカンマ・引用符のエスケープを徹底してください。
  - 欠損値や不在項目には `StringSanitizer.NotApplicable` (`N/A`) を出力してください。
- **相対線量の計算基準**:
  - 相対線量指標（V70% 等）の計算基準には、プラン情報の処方総線量（`PlanSetup.TotalDose`）を参照してください。

### 5.2 単体テストの記述規則
- テストフレームワーク: `MSTest`
- テストクラス命名: `<対象クラス名>Tests.cs`
- テストメソッド命名: `<メソッド名>_<条件>_<期待される結果>` (例: `ToGy_WhenUnitIsCGy_ShouldConvertToGy`)
- テスト構造: **Arrange-Act-Assert (AAA) パターン** を厳守。

---

## 6. コミットメッセージ規約 (Conventional Commits)

コミットメッセージは [Conventional Commits](https://www.conventionalcommits.org/) 規約に準拠して記述します。

### 書式
```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

### Type 一覧
- `feat`: 新機能の追加 (例: `feat(export): add jsonl streaming export`)
- `fix`: バグ修正 (例: `fix(dvh): correct relative dose reference to TotalDose`)
- `docs`: ドキュメントの追加・更新 (例: `docs: update COMMISSIONING.md and MANUAL.md`)
- `refactor`: バグ修正や機能追加を伴わないコード改善 (例: `refactor(mvvm): separate code-behind into MainViewModel`)
- `test`: 単体テストの追加・更新 (例: `test(dqp): add test for basic stats and dqp co-existence`)
- `chore`: ビルドツールや設定ファイルの変更 (例: `chore(build): update test.bat pipeline`)

---

## 7. プルリクエスト (PR) チェックリスト

変更をマージまたはプッシュする前に、以下の項目をセルフチェックしてください：

- [ ] `test.bat` を実行し、全自動テスト（24/24）が 100% PASS していること。
- [ ] x64 Release ビルドで警告（0 Warnings）およびエラー（0 Errors）がないこと。
- [ ] 機密情報（院内 IP アドレス、ローカル PC のユーザー名、個人プロファイルパス、患者個人情報等）が一切コードやドキュメントに含まれていないこと（`privacy-and-secret-scrubber` 原則の遵守）。
- [ ] `FodyWeavers.xml` で ESAPI DLL が除外されていること。
- [ ] 新機能・修正内容に対応する単体テストが追加されていること。
- [ ] [CHANGELOG.md](file:///g:/Source/Repos/tkmd94/EclipseDataMiner/CHANGELOG.md) に変更内容が追記されていること。
