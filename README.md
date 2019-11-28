# EclipseDataMiner

VARIAN社製治療計画装置Eclipse上で利用できるデータマイニング用のスタンドアロン型スクリプトです。  

## 導入方法

本スクリプトはスタンドアロン型となるため、事前にビルドして実行ファイルを生成する必要があります。  
ビルドで生成された「EclipseDataMiner.exe」をEclipse端末上で実行します。  

動作検証：Eclipse version 15.6  

## 操作方法

- 各種フィルタ及び出力データオプションを設定し、「Run」ボタンを押します。
- 処理状況は進捗バーとログウィンドウに表示されます。
- 途中で中断したい場合は「Cancel」ボタンを押してください。
- 処理が中断しても、途中までのデータは出力されます。

## 出力データフォーマット

データはテキスト形式で保存されます。  

- １つのプランデータが1行で出力されます。  
- データはタブ区切りです。
- 小項目は「,」や「；」で区切りです。

## 各種設定

### データ出力先設定

- Set Folder: データ出力先フォルダおよびファイル名を変更できます。
- Open Folder: 出力先フォルダを開きます。
  - 初期フォルダは実行ファイルが存在するフォルダとなります。

### フィルタオプション

- 検索方法は部分一致です。
  - １つの項目に複数キーワードを定義したい場合は「,」カンマで区切ってください。
    - 例) 1-1,1-22500,2501
  - 項目内はOR検索
  - 項目間はAND検索
- Patient ID: 患者ID
- Course ID: コースID
- Plan ID: プランID
- Approval status：承認状態
  - Unapproved: 未承認
  - Plan approved: 計画承認
  - TRT approved: 治療承認
- Target Volume ID
- Total Dose
- Dose per fraction
- Number of fraction

## 出力データオプション

### プラン情報

- Planning Approver: 計画承認者情報の出力
- Planning Approval Date: 計画承認日付の出力
- MU: 各フィールドのMU値
- Machine/Energy/Tech./PlanType: 各フィールドの装置名/エネルギー/照射方法/MLC動作タイプ
- Calculation Model: 線量アルゴリズム名とバージョン
- Calculation Log: 各フィールドの線量計算ログ
- Normalization Mode: 線量正規化方法
- Clinical Protocol: クリニカルプロトコルパラメータ
- DVHdata(Only the structures in the DQP list): DQPリストにあるstructureのDVHデータを個別保存

### Analysis（解析）

- Plan Complexity: プランの複雑性評価指標
  - Modulation Complexity Score
  - Edge Metric
  - Leaf Travel Length
  - Arc Length

### Dose Quality Parameter

- 設定した輪郭名「Structure name」に完全一致する以下のDQPを出力します。
- Dxx%[Gy]
- Vyy%[cc]
- DC(Dose Complement)
- CV(Complement Volume)

  - 例)D95%[Gy]
    - DQP type: D
    - Value: 95
    - Input Unit: Relative[%]
    - output Unit: Absolute[Gy or cc]
- 設定したパラメータは「Save parameters」ボタンで保存できます。
- 保存したパラメータは「Load parameters」ボタンで読み込むことができます。

- 設定したDQP以外に下記項目も出力されます。
  - Volume
  - Maximum dose
  - Minimum dose
  - Mean dose

## UI画面

![Screen capture of planCompare UI](https://github.com/tkmd94/EclipseDataMiner/blob/master/img/UI.jpg)
