# スイカゲーム (Unity)

2D物理で動くフルーツ合成ゲームの最小実装です。シーン内のGameObjectは実行時に生成されるため、手動で配置する必要はありません。

## 遊び方
- マウス移動で落下位置を調整します。
- 左クリックまたはSpaceでフルーツを落とします。
- ゲームオーバー後はRでリスタートします。

## セットアップ
1. Unity (2022.3.16f1)でプロジェクトを開く。
2. 任意のシーンを作成または開く。
3. Playを押す。

`SceneBootstrap` がメインカメラと `GameManager` を自動生成します（存在しない場合のみ）。

## 調整項目
- `GameManager.fruitDefinitions` でサイズ・色・スコアを変更できます。
- `GameManager.spawnTypeCount` で序盤に出るフルーツ種類数を変更できます。
- プレイエリアは `GameManager` の "Playfield" と "Box Offset" の項目で調整できます。

## アセット
- `Assets/Resources` に `back_box` / `front_box` / `test` の名前でスプライトを置くと反映されます。
- スプライトがない場合は自動生成の図形で表示されます。
