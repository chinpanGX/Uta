---
title: 実装計画: client/ ホームの 3D キャラ表示（待機モーション）
status: draft
updated: 2026-11-08
spec: ../wiki/architecture/screen-design.md
---

# 実装計画: client/ ホームの 3D キャラ表示（待機モーション）

ホームのキャラ表示のステージ（[screen-design.md](../wiki/architecture/screen-design.md) 7.8）を、MVP の仮の枠（`PlaceholderCharacterStage`）から、育成アイドルの 3D モデルが待機モーションを再生する表示（`ModelCharacterStage`）に替える。**この計画は仮組**: 型・手順・失敗の扱いは決めて書くが、見た目の値（カメラの位置・画角・ライト・`RenderTexture` の大きさ）は「仮の値」で、実装のあとに目で見て直してよい（直した値は実装設計書に書き戻す）。

## この計画で決めたこと

ユーザーは、この節と次の「ユーザーの確認が要ること」だけを読めば判断できるように書く。

1. **MVP の後に入れる**（2026-11-08。ユーザーの判断）。順 7（client/ の画面。[索引](2026-11-07-client-screens-index.md)）の範囲外で、[game-spec.md](../wiki/overview/game-spec.md)「MVP に入れるもの・入れないもの」でも MVP に入れないものに書く。いつ入れるかは確認 3
2. **要件は待機モーションの再生だけ**（ユーザーの判断）。タップへの反応・表情・視線・台詞・音声・着せ替えは範囲外
3. **差し替えるのは `HomePage.CreateStage` だけ**（screen-design 7.8 の拡張点のとおり）。`ModelCharacterStage` は `ICharacterStage` の実装として足す。`ICharacterStage`・`HomePresenter`・`IHomeView`・`HomePage` の Prefab の階層・`PlaceholderCharacterStage` は変えない。破棄は `CreateStage` の中で、ページの破棄（`GetCancellationTokenOnDestroy`）に `Dispose` を結ぶ（`HomePage` にほかのメソッドを足さない）
4. **描き方**: 専用のカメラ（ホームの UI のカメラ・シーンのカメラとは別）が、3D 用のレイヤー `CharacterStage`（ユーザーのレイヤーの 8 番）だけを Culling Mask で映し、`RenderTexture` に描く。その `RenderTexture` を `Stage` の `RawImage.texture` に入れる。カメラ・ライト・モデルの置き場は、Prefab `CharacterStageRig`（3.4）で、ワールドの (1000, 0, 0) に置く（シーンのほかのカメラの視野に入らない位置）
5. **読み込みは Addressables**。グループは画面の `Screens`（screen-design 7.2）と別の **`Characters`**。アドレスは Prefab の名前（`CharacterStageRig`・`IdolModel_01` など）。ロードと解放は `AddressablesCharacterStageAssets`（3.3）だけが行い、`ModelCharacterStage` は IF `ICharacterStageAssets` 越しに使う（EditMode のテストで偽に替えるため）
6. **育成アイドル → モデルの引き当ては、仮の対応表 `ProvisionalCharacterModelCatalog`（C# の辞書。3.3）で始める**。マスタに列を足すのは、モデルの数と命名が決まってから（6章 未決事項 1。この計画では足さない）。最初の表は、育成アイドル 1〜5 の全員を `IdolModel_01`（最初に取り込む 1 体）に当てる。表に無い育成アイドルは仮の枠に落とす
7. **モデルの Prefab**: `Assets/Models/Idols/IdolModel_<2 桁の番号>/IdolModel_<番号>.prefab`。ルートに `Animator`（Humanoid の Avatar、AnimatorController は全モデル共通の `Assets/Models/Animations/HomeIdle.controller`、Apply Root Motion なし、Culling Mode `CullCompletely`）。`HomeIdle.controller` はステート `Idle` が 1 つだけ（既定のステート、モーションは待機のクリップ、Loop Time あり、遷移なし、パラメータなし）。Prefab は手で作らず、Editor のスクリプト（3.5）が作る（screen-design 7.1 と同じ）
8. **読めなかったときは仮の枠に落とす**: 対応表に無い・リグかモデルの読み込みが失敗した、のどれでも、`RawImage` を元の色つきの枠に戻し、`PlaceholderCharacterStage` で名前を出す。警告のログを 1 行出す。リグの読み込みの失敗は覚えておき、そのページの間は再び読まない
9. **非同期の読み込みの扱い**: `ShowIdol` は待たずに戻る（読み込みは裏で進む）。同じ育成アイドルの 2 回目（読み込み中を含む）は何もしない。読み込み中に別の `ShowIdol`・`Clear`・`Dispose` が来たら、古い読み込みの結果は届いた時点で解放して捨てる（Addressables の読み込みは途中で止められないので、届いてから捨てる）。新しいモデルが読めるまで、前のモデルを出したままにする
10. **モバイルの負荷（仮の値）**: `RenderTexture` は `Stage` の大きさ（キャンバスの単位）に合わせ、長い辺を 1024 以下に縮める（3.3 の `CharacterStageSize`）。形式は ARGB32・深度 24・MSAA なし・ミップなし。カメラは毎フレーム描く（間引かない。負荷を測ってから決める: 6章 未決事項 3）が、**`Stage` の `RawImage` が画面に出ていないとき（ホームの上にガチャなどのページを積んだとき・シーンを離れたとき）とモデルが無いときはカメラを止める**。モデルの `Animator` は `CullCompletely` なので、カメラが止まると再生も止まる。影・ポストプロセスは使わない
11. **最初のタスクはスパイク（止まる条件つき）**: Unity 7（`7000.0.0a12`）で、URP 17.7.0 のカメラ → `RenderTexture`（透明の背景）→ `RawImage`、Humanoid の `Animator` の待機のループ、Addressables のグループ `Characters` からの `InstantiateAsync`・`ReleaseInstance`、ホームの上にページを積んだときの `HomePage` の非表示、を Editor の Play と Windows の IL2CPP で確かめる。動かなければ止まって報告する（`Camera.Render` の手動呼び出し・Resources・Legacy の Animation などに勝手に変えない）
12. **アセットの見た目の扱い**: マテリアルは URP の `Universal Render Pipeline/Lit`（仮。トゥーンの表現は範囲外: 6章 未決事項 4）。取り込みで URP 以外のシェーダーになったマテリアルは、タスク 2 で `Lit` に替え、主のテクスチャを `_BaseMap` に入れる

## ユーザーの確認が要ること

計画を `ready` にする前に、次の 3 つ（とその中の小項目）の判断をもらう。推奨は各項目の「推奨」。

### 確認 1. アセット（モデル・モーション）の運用ルール・取り込み方法・ライセンスの記録

ユーザーは 2026-11-08 に「AI 生成のもの、または無料で使える MMD モデルを使ってよい」と許可した。これと [ADR 0001](../wiki/decisions/0001-ip-policy.md)（キャラクター・名称・世界観・UI 素材はすべてオリジナル、またはライセンスが明確な素材。既存作品の素材・固有の表現を入れない）の関係を、次のとおり正直に書く。**ADR 0001 は書き換えない。**

- **事実（ライセンスの傾向。筆者の理解で、個々の配布物は確かめていない）**: 無料で配られている MMD のモデルには、(a) 既存のゲーム・アニメ・VTuber などのキャラクターを模したもの（二次創作。原作の権利者の許諾が別に要る、または許されない）、(b) 規約で再配布・改変・商用・特定の用途（アプリへの同梱、ゲームでの利用）を禁じるもの、(c) クレジットの表示を条件にするもの、が多い。MMD 向けの規約は「MMD での動画・静止画の制作」を想定し、**ゲームのアプリに同梱して配ること（再配布にあたる）を想定していないものが多い**。モーションの配布物も同じ（既存の楽曲の振り付けを写したものは、振り付けの権利の問題もある）
- **事実（AI 生成）**: AI 生成のモデル・モーションは、生成サービスの利用規約が出力の利用（商用・再配布）をどう許すかがサービスごとに違う。既存のキャラクターに似せた出力は (a) と同じ問題になる。法的な扱いは筆者（Claude）には判断できない（ADR 0001「影響・リスク」と同じく、公開前に専門家への確認を推奨する）
- **運用ルールの案**（推奨: この案。採るなら、ADR 0001 を置き換えない**追加の ADR**（次の番号で「外部の 3D アセットの利用ルール」）として起こす。今回は計画にだけ書き、ADR はユーザーの判断のあとに作る）:
  1. 使ってよいのは「**オリジナルのキャラクター**（既存作品のキャラクター・その二次創作でない）で、**配布元のライセンス（規約）が、このゲームでの利用（ゲームのアプリへの同梱＝再配布を含む、改変（形式の変換・マテリアルの差し替え）を含む）を明示的に許すもの**」に限る。規約が曖昧・書かれていない・「MMD での利用に限る」ものは使わない
  2. 名前は Uta の仮称（`IdolModel_01` など）で扱い、配布物の名前・キャラクター名をコード・アドレス・画面に出さない（ADR 0001。台帳にだけ書く）
  3. 商用の可否も記録する（いまは個人の技術検証で配布しないが、公開の形を決めるとき（ADR 0001「見直す条件」）に見直せるように）
  4. クレジットの表示が条件のものは、台帳に表示の文言を写し、アプリ内のライセンス表示の画面（MVP では作らない: screen-design 7.3）を作るときに出す
  5. 台帳に書いてから取り込む。台帳に無いアセットを `client/uta/Assets/Models/` に入れない
- **台帳（アセットごとに、出どころ・ライセンス・確認日を記録する）の置き場の案**:
  - 推奨: **`uta_docs/wiki/assets/third-party-assets.md`**（wiki に `assets/` のフォルダと `README.md` を足し、`index.md` に登録する）。列: 仮称（`IdolModel_01`・`Idle_01`）・種類（モデル／モーション）・出どころ（配布元の URL・作者・生成サービスと生成日）・ライセンス（規約の名前と版、規約の URL、再配布・改変・商用・ゲームでの利用・クレジットの可否）・確認日・確認した人・リポジトリでの置き場。規約の本文は、規約が同梱を求めるときだけ、アセットの置き場に `LICENSE.txt` として写す
  - 代案: `client/uta/Assets/Models/LICENSES.md`（アセットの隣）。Unity のプロジェクトに Markdown が入る（`.meta` が付く）ので推奨しない
  - 代案: `uta_docs/raw/` に置く。raw は「変更しない一次資料」で、台帳は行を足していくので合わない
- **MMD のモデル（PMX）の Unity への取り込み方法**（未確定。推奨と代案）:
  - 推奨: **Blender で PMX を読み、FBX に書き出してから Unity に入れる**（Blender の MMD 取り込みのアドオン（例: mmd_tools。GPL のツールで、ツールのライセンスは出力のアセットに及ばないと筆者は理解しているが、推測）を使う）。Unity には FBX とテクスチャだけが入り、Unity のプロジェクトに変換のツールを入れない。Humanoid の Avatar は Unity の FBX の取り込みで作る（ボーンの名前の対応が自動で取れないときは、取り込みの設定で手で割り当てる）
  - 代案: Unity のエディター拡張で PMX を直接読む（MMD4Mecanim・UniMMD など）。Unity 7 a12 で動くか分からず、拡張そのものの再配布・ライセンスの確認も要るので推奨しない
  - 変換（形式の変換・ボーンの対応付け・マテリアルの差し替え）は「改変」にあたるので、運用ルール 1 のとおり規約が改変を許すことを確かめる
  - モーション（VMD）も同じく Blender で FBX（Humanoid のアニメーション）にする。AI 生成のモーション（FBX・BVH）は FBX に揃える
- **この計画の前提**: 確認 1 の判断と、最初の 1 体（`IdolModel_01`）と待機のモーション 1 本（`Idle_01`）の調達・台帳への記録・FBX への変換が、タスク 2 の前に済んでいること（調達と変換はユーザーが行う。Claude はアセットを探さない・落とさない）

### 確認 2. モデルの見え方（仮の値）

次の仮の値で作り、Play で目で見てから直す。推奨: この仮の値で始める。

| 項目 | 仮の値 | 理由 |
|---|---|---|
| 映す範囲 | **全身**（足先から頭の上まで。身長 1.6 m 前後を想定） | 待機モーションが全身で分かる。上半身にするならカメラの値だけ変える（下の行） |
| 画面の位置 | ホームの中央の `Stage`（P2 の構成のまま。ステータスと主ボタンの間の、残りの高さ全部） | Prefab を変えない（決めたこと 3） |
| 背景 | **透過**（カメラの背景を透明の黒 (0,0,0,0) で消し、ページの背景が透けて見える）。`RawImage` の色は白にして `RenderTexture` の色を変えない | 背景の素材を作らない |
| カメラ | 透視、画角（縦）30 度、リグの中で (0, 0.9, 3.2)、回転 (0, 180, 0)（モデルの正面 +Z を向く）、Near 0.1・Far 20 | 全身が縦 1.7 m ほどに収まる |
| ライト | 平行光源 1 つ、回転 (50, -30, 0)、強さ 1.0、色 白、影なし、Culling Mask は `CharacterStage` だけ | 影を使わない（決めたこと 10） |
| 上半身にするとき | カメラ (0, 1.25, 1.6)、画角 30 度 | 参考の値 |

### 確認 3. MVP の後のどの時点で入れるか

推奨: **順 7 の lesson（最後の計画）が `done` になり、game-spec の完了条件 6（Windows で通す）と 7（Android の実機）を満たした後**。理由: 3D の描画は Android の実機での負荷・IL2CPP の確認が要り、条件 7 の実機ビルドの環境ができてからのほうが一度で確かめられる。代案: 順 7 の foundation が `done` になった直後（`HomePage.CreateStage` があれば作れる）。この場合、後続の画面の計画と並行するので、ホームの手動確認が重なる

## 1. 目的と前提

ホームのキャラ表示のステージに、育成アイドルの 3D モデルを出し、待機モーションをループで再生する。どの育成アイドルを出すかは今までどおり `HomePresenter` が決める（screen-design 7.8「出す育成アイドルの選び方」）。この計画はその描き方だけを替える。

- 方針の正: [screen-design.md](../wiki/architecture/screen-design.md) 7.8（ステージと拡張点）、7.1（Prefab は Editor のスクリプトで生成）、7.2（Addressables）。ホームの並べ方は [screen-wireframes.md](../wiki/overview/screen-wireframes.md)「ホーム」
- 関係する ADR: [ADR 0001](../wiki/decisions/0001-ip-policy.md)（IP 方針。確認 1）、[ADR 0003](../wiki/decisions/0003-defer-3d-live.md)（3D ライブの保留。この計画はライブではなく、ホームのキャラ表示。ライブの保留は変えない）、[ADR 0006](../wiki/decisions/0006-adopt-unity-7-early.md)（Unity 7）、[ADR 0009](../wiki/decisions/0009-client-stack.md)（uGUI・UnityScreenNavigator・VContainer・UniTask）
- Unity 7 の確認の状況: [unity7-evaluation.md](../wiki/architecture/unity7-evaluation.md)。URP の `RenderTexture`・`Animator`・Addressables の 3D の Prefab は、Unity 7 a12 で未確認（タスク 1）
- **前提**:
  1. 順 7 の P2（[計画](2026-11-07-client-prefabs-screens-a.md)。`HomePage` の `Stage`（`RawImage`）と `StageLabel`）と foundation（[計画](2026-11-03-client-screens-foundation.md) 3.7。`ICharacterStage`・`PlaceholderCharacterStage`・`HomePage.CreateStage`）が `done`。P1 の Addressables の設定（`Assets/AddressableAssetsData/`、グループ `Screens`、`BuildCommands` の `BuildPlayerContent`）がある
  2. 確認 1〜3 の判断が済み、確認 1 のアセット（`IdolModel_01` の FBX とテクスチャ、待機のモーション `Idle_01` の FBX）が台帳に記録され、ユーザーから渡されていること
  3. Unity 7 の Editor が、その worktree の `client/uta` を開いていること（索引 6章と同じ。Editor が無ければタスクを実行せず、確認できていないと報告する）
- **この計画で決めないこと（範囲外）**: タップ・表情・視線・揺れもの（髪・スカートの物理）・音声、トゥーンのシェーダー、マスタの列（モデルのアドレス）、モデルのダウンロード（リモートのカタログ）、3D ライブ、ホーム以外の画面の 3D 表示

## 2. 触るファイル

パスは `client/uta/Assets/` から（wiki と台帳を除く）。

| タスク | 操作 | パス | 中身 |
|---|---|---|---|
| 1 | 作成→削除 | `Spike3D/`（`Uta.Spike3D.asmdef`・`CharacterStageSpikeProbe.cs`・`Spike3D.unity`・`Editor/Uta.Spike3D.Editor.asmdef`・`Editor/Spike3DCommands.cs`） | スパイク。タスク 1 の終わりに消す |
| 1 | 変更 | `uta_docs/wiki/architecture/unity7-evaluation.md` | スパイクの結果を 2章（確かめたもの）か 3章（失敗）に足す |
| 2 | 変更 | `client/uta/ProjectSettings/TagManager.asset` | レイヤー 8 に `CharacterStage`（Editor のスクリプトが書く。3.5） |
| 2 | 作成 | `Models/Idols/IdolModel_01/Source/`（FBX・テクスチャ。必要なら `LICENSE.txt`） | ユーザーが渡すアセット。取り込みの設定は 3.5 |
| 2 | 作成 | `Models/Animations/Idle_01/`（FBX）・`Models/Animations/HomeIdle.controller` | 待機のモーションと共通の AnimatorController |
| 2 | 生成 | `Models/Idols/IdolModel_01/IdolModel_01.prefab` | モデルの Prefab（3.5 のスクリプトが作る） |
| 2 | 生成 | `Prefabs/Characters/CharacterStageRig.prefab` | カメラ・ライト・モデルの置き場（3.4。3.5 のスクリプトが作る） |
| 2 | 生成 | `AddressableAssetsData/`（グループ `Characters` の追加） | 3.5 のスクリプトが作る |
| 2 | 作成 | `Scripts/Editor/Characters/CharacterStageLayer.cs`・`CharacterAssetsBuilder.cs` | レイヤー・`HomeIdle.controller`・モデルの Prefab・リグの Prefab・Addressables の登録（3.5） |
| 3 | 作成 | `Scripts/Presentation/Home/ModelCharacterStage.cs` | `ICharacterStage` の 3D の実装（3.2） |
| 3 | 作成 | `Scripts/Presentation/Home/ICharacterStageAssets.cs`・`ICharacterStageRig.cs`・`ICharacterModelCatalog.cs` | テストで偽に替える IF（3.1） |
| 3 | 作成 | `Scripts/Presentation/Home/AddressablesCharacterStageAssets.cs`・`CharacterStageRig.cs`・`ProvisionalCharacterModelCatalog.cs`・`CharacterStageSize.cs` | 実装（3.3・3.4） |
| 3 | 変更 | `Scripts/Presentation/Uta.Client.Presentation.asmdef` | 参照に `Unity.Addressables`・`Unity.ResourceManager`・`Unity.RenderPipelines.Universal.Runtime` を足す（無いものだけ） |
| 3 | 変更 | `Scripts/Editor/Uta.Client.Editor.asmdef` | 参照に `Unity.RenderPipelines.Universal.Runtime` を足す（無いものだけ。3.5 がカメラの URP の設定を書くため） |
| 3 | 作成 | `Tests/EditMode/ModelCharacterStageTests.cs`・`CharacterStageSizeTests.cs`・`ProvisionalCharacterModelCatalogTests.cs`・`CharacterAssetsContractTests.cs` | EditMode のテスト（5章） |
| 4 | 変更 | `Scripts/Presentation/Home/HomePage.cs` | `CreateStage` の中身だけ（3.6） |
| 4 | 変更 | `uta_docs/wiki/architecture/screen-design.md` | 7.8 の拡張点の段落を、実装した形（置き場・寿命・読み込み）に書き直す |
| 4 | 変更 | この計画 | `status: done`、仮の値を直したならその値 |

- 台帳（確認 1）は前提で、この計画では作らない（置き場が決まってから、ユーザーの判断のあとに作る）
- `HomePresenter.cs`・`IHomeView.cs`・`ICharacterStage.cs`・`PlaceholderCharacterStage.cs`・`HomePage.prefab`・`Home.unity`・`SceneBuilder.cs` は変えない

## 3. 型とシグネチャ

名前空間はどれも `Uta.Client.Presentation.Home`（Editor のスクリプトは `Uta.Client.Editor.Characters`）。C# 9 の範囲（`client/` の `.editorconfig`）。1 型 1 ファイル。

### 3.1 テストで偽に替える IF

```csharp
/// <summary>ステージのアセット（リグとモデル）の読み込みと解放。実装は AddressablesCharacterStageAssets。</summary>
public interface ICharacterStageAssets
{
    /// <summary>リグの Prefab（アドレス "CharacterStageRig"）を親なしで作る。失敗したら例外。ct は読み込みが届いたあとに見る（3.3）。</summary>
    UniTask<ICharacterStageRig> InstantiateRigAsync(CancellationToken ct);
    /// <summary>モデルの Prefab を parent の子に作る。失敗したら例外。</summary>
    UniTask<GameObject> InstantiateModelAsync(string address, Transform parent, CancellationToken ct);
    /// <summary>InstantiateRigAsync・InstantiateModelAsync で作ったものを解放する。null・破棄済みなら何もしない。</summary>
    void ReleaseInstance(GameObject instance);
}

/// <summary>専用のカメラ・ライト・モデルの置き場。実装は CharacterStageRig（MonoBehaviour）。</summary>
public interface ICharacterStageRig
{
    GameObject Root { get; }
    Transform ModelAnchor { get; }
    /// <summary>Initialize で作った RenderTexture。Initialize の前は null。</summary>
    Texture Target { get; }
    /// <summary>width×height の RenderTexture を作ってカメラに入れ、image が画面に出ているかを毎フレーム見るようにする。2 回目は何もしない。</summary>
    void Initialize(RawImage image, int width, int height);
    /// <summary>true のあいだ、image が画面に出ていればカメラが描く。false ならカメラを止める。</summary>
    bool Rendering { get; set; }
}

/// <summary>育成アイドル → モデルの Prefab のアドレス。</summary>
public interface ICharacterModelCatalog
{
    bool TryGetAddress(int idolId, out string address);
}
```

`using`: `System.Threading`・`Cysharp.Threading.Tasks`・`UnityEngine`・`UnityEngine.UI`。

### 3.2 `ModelCharacterStage`

```csharp
/// <summary>ホームのキャラ表示の 3D の実装（screen-design 7.8 の拡張点）。読めないときは fallback（PlaceholderCharacterStage）に落とす。</summary>
public sealed class ModelCharacterStage : ICharacterStage, IDisposable
{
    /// <summary>引数のどれかが null なら ArgumentNullException（引数名はそのまま）。image.color をこの時点で覚える（仮の枠の色に戻すため）。</summary>
    public ModelCharacterStage(RawImage image, TMP_Text label, ICharacterStage fallback,
        ICharacterStageAssets assets, ICharacterModelCatalog catalog, CancellationToken lifetime);

    public void ShowIdol(int idolId);   // ShowIdolAsync(idolId).Forget()
    public void Clear();
    public void Dispose();

    /// <summary>テストが await するための本体。例外は外に出さない（キャンセルは無視、それ以外は仮の枠に落とす）。</summary>
    public UniTask ShowIdolAsync(int idolId);
}
```

フィールド: `RawImage image`、`TMP_Text label`、`ICharacterStage fallback`、`ICharacterStageAssets assets`、`ICharacterModelCatalog catalog`、`CancellationToken lifetime`、`Color placeholderColor`、`int? requestedIdolId`、`int requestVersion`、`CancellationTokenSource loadCts`、`UniTask<ICharacterStageRig>? rigLoad`、`ICharacterStageRig rig`（読めたら入る）、`GameObject currentModel`、`bool disposed`。

**`ShowIdolAsync(int idolId)` の手順**（この順）:

1. `disposed` なら戻る。`requestedIdolId == idolId` なら戻る（同じ育成アイドル。読み込み中・仮の枠の表示中を含む）
2. `requestedIdolId = idolId; var version = ++requestVersion;`。`loadCts` があれば `Cancel()`・`Dispose()` し、`loadCts = CancellationTokenSource.CreateLinkedTokenSource(lifetime)`、`var ct = loadCts.Token;`
3. `catalog.TryGetAddress(idolId, out var address)` が false なら `ReleaseModel(); ShowFallback(idolId);` で戻る（ログは出さない: 表に無いのは想定どおり）
4. `try` の中で:
   - `var stageRig = await EnsureRigAsync().AttachExternalCancellation(ct);`
   - `var model = await assets.InstantiateModelAsync(address, stageRig.ModelAnchor, ct);`
   - `version != requestVersion || disposed` なら `assets.ReleaseInstance(model);` で戻る（古い要求の結果を捨てる）
   - `ReleaseModel(); currentModel = model; ShowModel(stageRig);`
5. `catch (OperationCanceledException)`: 何もしない（新しい要求・`Clear`・`Dispose` に置き換わった）
6. `catch (Exception e)`: `version != requestVersion || disposed` なら何もしない。そうでなければ `Debug.LogWarning("CharacterStage: モデルを読めなかったので仮の枠に落とす（idolId=" + idolId + "、address=" + address + "）: " + e.Message);`、`ReleaseModel(); ShowFallback(idolId);`

**補助**（private）:

- `UniTask<ICharacterStageRig> EnsureRigAsync()`: `rigLoad ??= LoadRigAsync().Preserve();` を返す（`Preserve` で何度でも await できる。失敗も覚えられ、2 回目以降も同じ例外が出る: 決めたこと 8）
- `async UniTask<ICharacterStageRig> LoadRigAsync()`: `var loaded = await assets.InstantiateRigAsync(lifetime);`。`disposed` なら `assets.ReleaseInstance(loaded.Root)` して `throw new OperationCanceledException()`。`Canvas.ForceUpdateCanvases(); var rect = image.rectTransform.rect; var size = CharacterStageSize.Compute(rect.width, rect.height); loaded.Initialize(image, size.x, size.y); rig = loaded; return loaded;`
- `void ShowModel(ICharacterStageRig stageRig)`: `image.texture = stageRig.Target; image.color = Color.white; label.gameObject.SetActive(false); stageRig.Rendering = true;`
- `void ShowFallback(int idolId)`: `ShowEmptyFrame(); fallback.ShowIdol(idolId);`（`fallback.ShowIdol` の例外は捕まえない: `PlaceholderCharacterStage` と同じ）
- `void ShowEmptyFrame()`: `if (rig != null) rig.Rendering = false; image.texture = null; image.color = placeholderColor; label.gameObject.SetActive(true);`
- `void ReleaseModel()`: `currentModel` が null でなければ `assets.ReleaseInstance(currentModel); currentModel = null;`
- `void CancelLoad()`: `++requestVersion;`、`loadCts` があれば `Cancel()`・`Dispose()`・`null`

**`Clear()`**: `disposed` なら戻る。`requestedIdolId = null; CancelLoad(); ReleaseModel(); ShowEmptyFrame(); fallback.Clear();`

**`Dispose()`**: 2 回目は何もしない。`disposed = true; requestedIdolId = null; CancelLoad(); ReleaseModel();`、`rig` が null でなければ `assets.ReleaseInstance(rig.Root); rig = null;`、`image` が破棄済みでなければ（`image != null`）`image.texture = null;`。`fallback` には何もしない

- `ShowIdol` の `Forget()` は UniTask の未処理の例外の報告に回るが、`ShowIdolAsync` は例外を外に出さないので、報告されるのは `fallback.ShowIdol` の `KeyNotFoundException` だけ（所持の `IdolId` はマスタにあるので起きない前提。foundation 3.7 と同じ）
- `using`: `System`・`System.Threading`・`Cysharp.Threading.Tasks`・`TMPro`・`UnityEngine`・`UnityEngine.UI`

### 3.3 実装の部品

```csharp
/// <summary>Addressables でリグとモデルを作る。グループ Characters（決めたこと 5）。</summary>
public sealed class AddressablesCharacterStageAssets : ICharacterStageAssets
{
    public const string RigAddress = "CharacterStageRig";
    public UniTask<ICharacterStageRig> InstantiateRigAsync(CancellationToken ct);
    public UniTask<GameObject> InstantiateModelAsync(string address, Transform parent, CancellationToken ct);
    public void ReleaseInstance(GameObject instance);
}
```

- `InstantiateModelAsync`: `var handle = Addressables.InstantiateAsync(address, parent);`、`while (!handle.IsDone) await UniTask.Yield();`（UniTask の Addressables 拡張の asmdef に頼らない）。`handle.Status != AsyncOperationStatus.Succeeded` なら `var error = handle.OperationException; Addressables.Release(handle); throw new InvalidOperationException("Addressables の読み込みに失敗: " + address, error);`。`ct.IsCancellationRequested` なら `Addressables.ReleaseInstance(handle.Result); throw new OperationCanceledException(ct);`。そうでなければ `handle.Result` を返す
- `InstantiateRigAsync`: 同じ手順で `RigAddress` を親なしで作り、`GetComponent<CharacterStageRig>()` を返す。null なら `ReleaseInstance` して `InvalidOperationException("CharacterStageRig が無い")`
- `ReleaseInstance(GameObject instance)`: `if (instance == null) return; Addressables.ReleaseInstance(instance);`（Unity の `==` で破棄済みも弾く。シーンを閉じたときに先に破棄された場合）
- `using`: `UnityEngine.AddressableAssets`・`UnityEngine.ResourceManagement.AsyncOperations`

```csharp
/// <summary>仮の対応表（決めたこと 6）。マスタの列ができたら置き換える。</summary>
public sealed class ProvisionalCharacterModelCatalog : ICharacterModelCatalog
{
    // private static readonly Dictionary<int, string> Addresses: 1〜5 → "IdolModel_01"
    public bool TryGetAddress(int idolId, out string address);   // 無ければ false、address は null
}

/// <summary>RenderTexture の大きさ（決めたこと 10）。</summary>
public static class CharacterStageSize
{
    public const int MaxSide = 1024;
    /// <summary>width・height（キャンバスの単位）のどちらかが 1 未満なら (MaxSide, MaxSide)。
    /// そうでなければ scale = Min(1, MaxSide / Max(width, height)) をかけ、Mathf.RoundToInt で丸め、1 未満は 1 にする。</summary>
    public static Vector2Int Compute(float width, float height);
}
```

### 3.4 `CharacterStageRig`（MonoBehaviour。リグの Prefab のルートに付く）

```csharp
public sealed class CharacterStageRig : MonoBehaviour, ICharacterStageRig
{
    [SerializeField] private Camera stageCamera;
    [SerializeField] private Transform modelAnchor;

    public GameObject Root => gameObject;
    public Transform ModelAnchor => modelAnchor;
    public Texture Target => target;   // private RenderTexture target
    public bool Rendering { get; set; }
    public void Initialize(RawImage image, int width, int height);
    // Awake: stageCamera.enabled = false;
    // Initialize: target が null でなければ戻る。target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
    //   { name = "CharacterStageTexture", antiAliasing = 1, useMipMap = false, autoGenerateMips = false }; target.Create();
    //   stageCamera.targetTexture = target; boundImage = image;
    // LateUpdate: stageCamera.enabled = Rendering && boundImage != null && boundImage.isActiveAndEnabled;
    // OnDestroy: stageCamera.targetTexture = null（stageCamera が残っていれば）; target があれば target.Release(); Destroy(target);
}
```

リグの Prefab `CharacterStageRig`（すべての子のレイヤーは `CharacterStage`）:

| 子 | 中身（仮の値は確認 2） |
|---|---|
| （ルート） | 位置 (1000, 0, 0)、回転 0、`CharacterStageRig`（参照 2 つ） |
| `StageCamera` | 位置 (0, 0.9, 3.2)、回転 (0, 180, 0)。`Camera`: 透視・fieldOfView 30・near 0.1・far 20・clearFlags `SolidColor`・backgroundColor (0,0,0,0)・cullingMask は `CharacterStage` だけ・depth 0・targetTexture なし・enabled false。`UniversalAdditionalCameraData`: renderType `Base`・renderPostProcessing false・antialiasing `None`・renderShadows false・requiresDepthTexture false・requiresColorTexture false。`AudioListener` は付けない |
| `KeyLight` | 回転 (50, -30, 0)。`Light`: Directional・強さ 1.0・色 白・shadows `None`・cullingMask は `CharacterStage` だけ |
| `ModelAnchor` | 位置 0・回転 0（モデルはここの子に作る。モデルの正面は +Z） |

### 3.5 Editor のスクリプト（`Scripts/Editor/Characters/`）

```csharp
public static class CharacterStageLayer
{
    public const int Index = 8;
    public const string Name = "CharacterStage";
    /// <summary>TagManager のレイヤー 8 が空なら Name を入れる。Name なら何もしない。ほかの名前なら InvalidOperationException（止まって報告する）。</summary>
    public static void Ensure();
}

public static class CharacterAssetsBuilder
{
    public const string ControllerPath = "Assets/Models/Animations/HomeIdle.controller";
    public const string IdleClipSourcePath = "Assets/Models/Animations/Idle_01/Idle_01.fbx";   // ユーザーが渡すファイル名に合わせて、この 1 行だけ直してよい
    public const string RigPrefabPath = "Assets/Prefabs/Characters/CharacterStageRig.prefab";
    public const string GroupName = "Characters";
    /// <summary>冪等。Ensure() → BuildController() → BuildRig() → BuildModel(各モデル) → RegisterAddressables() の順。</summary>
    public static void BuildAll();
    public static void BuildController();   // HomeIdle.controller を作り直す: レイヤー 1 つ、ステート "Idle"（既定）、Motion は IdleClipSourcePath の最初の AnimationClip、遷移・パラメータなし
    public static void BuildRig();          // 3.4 の表のとおりに組み、RigPrefabPath に保存
    /// <summary>modelId は "01" など。Assets/Models/Idols/IdolModel_{modelId}/Source/ の .fbx（1 つだけ。0 個・2 個以上なら InvalidOperationException）から IdolModel_{modelId}.prefab を作る。</summary>
    public static void BuildModel(string modelId);
    public static void RegisterAddressables();   // グループ Characters（無ければ Default Local Group のスキーマの写しで作る）に、リグと Assets/Models/Idols/*/IdolModel_*.prefab をアドレス = ファイル名（拡張子なし）・ラベルなしで入れる
}
```

- **取り込みの設定**（`BuildModel`・`BuildController` の前に、`ModelImporter` をスクリプトで設定して `SaveAndReimport`）: モデルの FBX は Rig = `Humanoid`（Avatar は Create From This Model）、Import Animation なし、Materials は Extract して `Source/Materials/` に置き、シェーダーが URP 以外なら `Universal Render Pipeline/Lit` に替えて主のテクスチャを `_BaseMap` に入れる（決めたこと 12）。モーションの FBX は Rig = `Humanoid`、クリップの Loop Time あり・Root Transform の Rotation／Position Y／XZ は Bake Into Pose
- **`BuildModel` の Prefab**: FBX を `PrefabUtility.InstantiatePrefab` で置き、ルートの `Animator` に `HomeIdle.controller`・applyRootMotion false・cullingMode `CullCompletely`、レイヤーを子まで `CharacterStage` にし、`PrefabUtility.SaveAsPrefabAsset` で保存する（FBX の Prefab のバリアントになる）。置いたものは `Object.DestroyImmediate`
- Humanoid の Avatar が作れない（`avatar.isValid` が false・ボーンの対応が取れない）ときは止まって報告する（手でボーンを割り当てる作業はユーザーに頼む。確認 1 の取り込み方法）
- 実行は `unity command eval 'Uta.Client.Editor.Characters.CharacterAssetsBuilder.BuildAll(); return "ok";'`（`client/uta` で）。asmdef は `Uta.Client.Editor`（3.1 の一般則のとおり、CS0012 が `Unity.RenderPipelines.Universal.Runtime` を名指ししたら足す）

### 3.6 `HomePage.CreateStage` の変更

```csharp
public ICharacterStage CreateStage(IGameMaster master)
{
    var placeholder = new PlaceholderCharacterStage(stageLabel, master);
    var destroyed = this.GetCancellationTokenOnDestroy();
    var stage = new ModelCharacterStage(stageImage, stageLabel, placeholder,
        new AddressablesCharacterStageAssets(), new ProvisionalCharacterModelCatalog(), destroyed);
    destroyed.Register(stage.Dispose);   // ページ（Home シーン）の破棄でリグ・モデル・RenderTexture を解放する
    return stage;
}
```

- `using Cysharp.Threading.Tasks;`（`GetCancellationTokenOnDestroy`）を足す。`HomePage` のほかのメンバー・フィールド・Prefab は変えない
- 3D に戻せない（仮の枠に戻す）ときは、この中身を foundation 3.7 の 1 行（`return new PlaceholderCharacterStage(stageLabel, master);`）に戻せばよい

## 4. タスク

各タスクの終わりに、`unity recompile --format json` がエラー 0 で、EditMode のテスト（`unity test --mode EditMode`。動かなければ Editor の Test Runner を手動で回してもらい、確認できていないと報告する: 索引 6章）が通る状態にする。

### タスク 1. スパイク（止まる条件つき）

- 触る: `Spike3D/` 一式（作って最後に消す）、`unity7-evaluation.md`
- 手順: 使い捨てのシーン `Assets/Spike3D/Spike3D.unity` に、Overlay の Canvas と `RawImage`、3.4 の値で手で組んだカメラ・ライト（レイヤーは仮にユーザーのレイヤー 8 を使う。タスク 1 では TagManager に名前を入れなくてよい）を置く。モデルは前提 2 の `IdolModel_01` の FBX を Humanoid で取り込み、待機のクリップ 1 本の AnimatorController で動かす。Addressables は一時のグループ `Spike3D` にモデルの Prefab を入れ、`CharacterStageSpikeProbe` が `InstantiateAsync` → 5 秒後に `ReleaseInstance` → もう一度 `InstantiateAsync` する
- 確かめること（Editor の Play と Windows の IL2CPP のプレイヤーの両方）:
  1. `RawImage` にモデルが映り、背景が透けている（アルファが効いている）
  2. 待機のモーションがループで再生される（1 周の後も止まらない）
  3. Addressables の作成・解放・再作成でエラーが出ない（`Addressables` のログに失敗が無い）
  4. カメラの `enabled = false` で `RawImage` の絵が止まり、`true` で戻る
  5. （Editor の Play のみ）順 7 の実装の `Home` シーンで、ホームの上にガチャのページを積んだとき、`HomePage` の GameObject が非アクティブになる（`activeInHierarchy` が false）。これで決めたこと 10 の「画面に出ていないときはカメラを止める」が `isActiveAndEnabled` で働く
- 止まる条件: 1〜4 のどれかが失敗したら止まって報告する（別の描き方・読み込み方に替えない）。5 で `HomePage` がアクティブのままなら止まって報告する（カメラを止める仕組みを計画で直す）
- 終わり: `Spike3D/` と一時のグループ `Spike3D` を消す。結果を `unity7-evaluation.md` の 2章（確かめたもの）の表に、失敗は 3章に、日付つきで足す

### タスク 2. アセットの取り込みと Prefab の生成

- 前提: 確認 1 の判断が済み、台帳に `IdolModel_01`・`Idle_01` の行がある（**無ければ止まる**。台帳に無いアセットを入れない）
- 触る: 2 の表のタスク 2 の行
- 手順: ユーザーから渡された FBX・テクスチャを `Models/Idols/IdolModel_01/Source/` と `Models/Animations/Idle_01/` に置く（ファイル名は渡されたままでよいが、配布物の名前・キャラクター名が入っていたら `IdolModel_01.fbx`・`Idle_01.fbx` に改名する: 確認 1 の運用ルール 2）。3.5 の `CharacterStageLayer`・`CharacterAssetsBuilder` を書き、`BuildAll()` を `unity command eval` で実行する。2 回目の実行で差分が出ないこと（冪等）を `git status` で確かめる
- 境界: Avatar が作れない・FBX が 0 個か 2 個以上・レイヤー 8 が別の名前、はどれも止まって報告する
- 確かめ方: 5章の `CharacterAssetsContractTests` が通る

### タスク 3. `ModelCharacterStage` と部品

- 触る: 2 の表のタスク 3 の行
- 手順: 3.1〜3.4 のとおりに書く。asmdef は 2 の表のとおり（無いものだけ足す）。`HomePage` はまだ変えない（ホームは仮の枠のまま動く）
- 境界: 3.2 の手順の 1〜6 と `Clear`・`Dispose`
- 確かめ方: 5章の `ModelCharacterStageTests`・`CharacterStageSizeTests`・`ProvisionalCharacterModelCatalogTests` が通る

### タスク 4. `HomePage.CreateStage` の切り替えと確認

- 触る: `HomePage.cs`、`screen-design.md` 7.8、この計画
- 手順: 3.6 のとおりに `CreateStage` を書き換える。プレイヤーのビルドの前の `BuildPlayerContent`（P1 の `BuildCommands`）が `Characters` も含めてビルドすることを確かめる（グループを足すだけで入る。入らなければ止まって報告する）。6章の手動確認を行う。screen-design 7.8 の拡張点の段落を、実装した形（リグの Prefab・グループ `Characters`・仮の対応表・`CreateStage` での破棄・仮の枠への落とし方）に書き直す。仮の値を直したら、確認 2 の表とこの計画の 3.4 を直す。計画を `status: done` にする
- 確かめ方: 既存の `HomePresenterTests`・`PlaceholderCharacterStageTests` が通る（`HomePresenter` は変えていない）、6章の手動確認

## 5. テスト

EditMode のテストは `Tests/EditMode/`（asmdef は `Uta.Client.Infrastructure.Tests.EditMode`。P1・foundation で `Uta.Client.Presentation`・`Unity.TextMeshPro`・`UnityEngine.UI`・`Unity.Addressables` を参照済み。足りなければ 3.1 の一般則で足す）。非同期は foundation 3.3 と同じく `[UnityTest] public IEnumerator Xxx() => UniTask.ToCoroutine(async () => { ... });`。

### 5.1 EditMode で確かめられるもの

**`ModelCharacterStageTests` の組み立て**: `[SetUp]` で `go = new GameObject("Stage", typeof(RectTransform)); image = go.AddComponent<RawImage>(); image.color = Color.gray; labelGo = new GameObject("Label"); label = labelGo.AddComponent<TextMeshProUGUI>();`、`log = new CallLog();`（foundation のテストの偽）。偽はテストのファイルの中の `private sealed class`:

- `FakeAssets : ICharacterStageAssets`: `RigGate`・モデルのアドレスごとの `UniTaskCompletionSource`（`Dictionary<string, UniTaskCompletionSource<GameObject>>`。登録が無いアドレスは、その場で `new GameObject(address)` を返す）。`RigFails`（true なら `InvalidOperationException`）。呼ばれたら `"Rig"`・`"Model:" + address`・`"Release:" + instance.name` を記録する。`ct` は見ない（Addressables と同じく、届いてから捨てるのは `ModelCharacterStage` の仕事）
- `FakeRig : ICharacterStageRig`: `Root = new GameObject("Rig")`、`ModelAnchor = null`、`Target = Texture2D.whiteTexture`（`Initialize` の後）、`Initialize` で `"Initialize:" + width + "x" + height`、`Rendering` の set で `"Rendering:" + value` を記録
- `FakeCatalog : ICharacterModelCatalog`: 1 → `"IdolModel_01"`、2 → `"IdolModel_02"`、ほかは false
- `FakeFallback : ICharacterStage`: `"Fallback:ShowIdol:" + idolId`・`"Fallback:Clear"` を記録
- `stage = new ModelCharacterStage(image, label, fallback, assets, catalog, CancellationToken.None)`。`[TearDown]` で `stage.Dispose()` と作った GameObject を `Object.DestroyImmediate`

| テスト | 確かめること |
|---|---|
| `ShowIdol_Found_LoadsRigThenModelAndShowsTexture` | `await stage.ShowIdolAsync(1)` → 記録が `"Rig"`・`"Initialize:100x100"`（新しい `RawImage` の既定の大きさ 100×100）・`"Model:IdolModel_01"`・`"Rendering:True"` の順。`image.texture == rig.Target`、`image.color == Color.white`、`label.gameObject.activeSelf == false` |
| `ShowIdol_SameIdolTwice_LoadsOnce` | 1 を 2 回 → `"Model:IdolModel_01"` が 1 回だけ |
| `ShowIdol_NotInCatalog_FallsBackWithoutLoading` | 9 → 記録は `"Fallback:ShowIdol:9"` だけ（`"Rig"` が無い）。`image.texture == null`、`image.color == Color.gray`、ラベルがアクティブ |
| `ShowIdol_ModelFails_FallsBackAndLogsWarning` | 1 のモデルの源を `TrySetException(new InvalidOperationException("x"))` → `LogAssert.Expect(LogType.Warning, new Regex("CharacterStage"))`、`"Fallback:ShowIdol:1"`、`image.texture == null` |
| `ShowIdol_RigFails_FallsBackAndDoesNotRetryRig` | `RigFails = true`。1 → 仮の枠、続けて 2 → 仮の枠。`"Rig"` は 1 回だけ。警告 2 回（`LogAssert.Expect` を 2 回） |
| `ShowIdol_SwitchWhileLoading_DiscardsOldResult` | 1 のモデルを止めておき、`var first = stage.ShowIdolAsync(1); await stage.ShowIdolAsync(2);`（2 は即時）→ 1 の源を `TrySetResult(new GameObject("IdolModel_01"))` → `await first` → `"Release:IdolModel_01"` が記録され、`"Rendering:True"` は 2 の分の 1 回だけ。最後の表示は 2 のモデル |
| `ShowIdol_SwitchAfterShown_ReleasesOldAfterNewLoaded` | 1 を表示 → 2 を表示 → 記録で `"Model:IdolModel_02"` が `"Release:IdolModel_01"` より前 |
| `Clear_WhileLoading_ReleasesLateModelAndShowsEmpty` | 1 を止めておき、`ShowIdolAsync(1)` → `Clear()` → 源を届ける → `"Release:IdolModel_01"`、`"Fallback:Clear"`、`image.texture == null`、ラベルがアクティブ |
| `Clear_AfterShown_ReleasesModelAndStopsRendering` | 1 を表示 → `Clear()` → `"Release:IdolModel_01"`・`"Rendering:False"`・`"Fallback:Clear"` |
| `Clear_ThenSameIdol_LoadsAgain` | 1 を表示 → `Clear()` → 1 → `"Model:IdolModel_01"` が 2 回 |
| `Dispose_ReleasesModelAndRig_AndIgnoresLaterCalls` | 1 を表示 → `Dispose()` → `"Release:IdolModel_01"`・`"Release:Rig"`、`image.texture == null`。そのあと `ShowIdolAsync(2)`・`Clear()` で記録が増えない。`Dispose()` の 2 回目で記録が増えない |
| `Constructor_NullArgument_Throws` | 各引数を null にして `ArgumentNullException`（引数名） |

**`CharacterStageSizeTests`**（同期の `[Test]`）: `Compute(1000, 900)` → (1000, 900)、`Compute(2000, 1000)` → (1024, 512)、`Compute(0, 500)` → (1024, 1024)、`Compute(1024, 1024)` → (1024, 1024)、`Compute(3000, 1)` → (1024, 1)（1 未満にしない）

**`ProvisionalCharacterModelCatalogTests`**: 1〜5 が `true` で `"IdolModel_01"`、0・6 が `false` で `address == null`

**`CharacterAssetsContractTests`**（Editor のアセットを読む。タスク 2 の後）: `CharacterStageRig.prefab` がある・`CharacterStageRig` の参照 2 つが null でない・子まで全部のレイヤーが 8・カメラの cullingMask が `1 << 8` だけ・ライトの cullingMask が `1 << 8` だけ。`IdolModel_01.prefab` のルートに `Animator`（controller が `HomeIdle.controller`、avatar が Humanoid で `isValid`、cullingMode `CullCompletely`）。`HomeIdle.controller` のステートが `Idle` の 1 つでクリップの `isLooping` が true。Addressables のグループ `Characters` に `CharacterStageRig`・`IdolModel_01` のアドレスがある。`ProvisionalCharacterModelCatalog` の全アドレスが `Characters` にある

### 5.2 Unity の Play・実機でしか確かめられないもの

次は EditMode のテストでは確かめられない。実行できなかったら「確認できていない」と書き、ユーザーに頼む（推測で通ったと書かない）。

- `RenderTexture` に描いた絵が `RawImage` に透過で出ること、待機モーションのループ、カメラを止めたときに描画と `Animator` が止まること（タスク 1・6章）
- `GetCancellationTokenOnDestroy` による破棄（`Home` → `Produce` のシーンの移動で、リグ・モデル・`RenderTexture` が解放され、警告・エラーが出ないこと）
- Windows の IL2CPP のプレイヤーでの表示と Addressables のビルド
- Android の実機（負荷・発熱・フレームレート）。iOS は未確認のまま残す

## 6. 手動確認（タスク 4）

| # | どこで | 確かめること |
|---|---|---|
| 1 | Editor の Play（`Bootstrap` から） | ホームのステージに `IdolModel_01` が全身で映り、待機モーションがループし、背景が透けている。「キャラ表示」の文字が出ていない |
| 2 | 同上 | ガチャのページを積むと、Frame Debugger か Profiler で `StageCamera` の描画が止まる。戻ると再び動く |
| 3 | 同上 | 育成アイドルの所持が 0（新規の端末）では、仮の枠と「キャラ表示」の文字。対応表から 1 を一時的に外すと、仮の枠に名前（`PlaceholderCharacterStage`）が出る（確かめたら戻す） |
| 4 | 同上 | プロデュースへ行って（`Produce` シーン）戻る。コンソールに Addressables・`RenderTexture` の警告・エラーが無い。Memory Profiler か `Resources.FindObjectsOfTypeAll<RenderTexture>()` で `CharacterStageTexture` が 1 つだけ（増え続けない） |
| 5 | Windows の IL2CPP のプレイヤー（バッチモードでビルド。Editor を閉じてから） | 1 と同じ表示。`-uta-quit-after-check` の診断が今までどおり終了コード 0 |
| 6 | Android の実機（完了条件 7 の環境ができていれば） | 1 と同じ表示。ホームで 1 分置いてフレームレートが落ちない（目で見る）。できなければ確認できていないと書く |

## 7. 未決事項

`ready` にする前に決めるもの（1〜3 は「ユーザーの確認が要ること」）:

- 確認 1（アセットの運用ルール・台帳の置き場・取り込み方法。外部のアセットの利用ルールの ADR を足すか）
- 確認 2（見え方の仮の値）
- 確認 3（入れる時点）

`ready` の後に回してよいもの（この計画の実装を止めない）:

1. **マスタにモデルの列を足すか**（`Idol` に `ModelAddress`（string）など）。モデルの数と命名が決まり、育成アイドルごとにモデルが揃ってから別の計画で決める。それまでは `ProvisionalCharacterModelCatalog`
2. 複数のモデルのとき、`Characters` グループを分けるか・リモートのカタログにするか（いまは同梱。マスタの配信の方針 [masterdata-pipeline.md](../wiki/architecture/master/masterdata-pipeline.md) 6章と合わせる）
3. カメラの描画の間引き（30 fps にするなど）。Android の実機で測ってから
4. トゥーンのシェーダー（URP 向けの自作か、ライセンスの明確な配布物か。後者なら確認 1 の運用ルールに入れる）
5. アプリ内のライセンス表示の画面（クレジットが条件のアセットを使うなら必要。screen-design 7.3 のフォントと同じく、ストアに出すときに作る）
