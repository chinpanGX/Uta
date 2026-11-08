---
title: 画面・遷移・クライアントの層（実装設計書 画面設計編）
status: draft
updated: 2026-11-08
---

# 画面・遷移・クライアントの層（実装設計書 画面設計編）

実装設計書（4編）の4編目。クライアントの Presentation 層にあたる、画面・遷移・起動フローを定める。前提は [repository-layout.md](repository-layout.md)。

- 各画面は、呼ぶ API（[api-design.md](api-design.md)）と、使うマスタ（[masterdata-pipeline.md](master/masterdata-pipeline.md)）に機能ID付きで対応づける
- 画面のワイヤー・見た目はこのページには書かない。[overview/screen-wireframes.md](../overview/screen-wireframes.md)（叩き台）に置く。**画面名・文言は仮称**（[ADR 0001](../decisions/0001-ip-policy.md)）

## 凡例

- **[決定]**: CLAUDE.md・accepted の ADR で決まっていること
- **[Uta案]**: この叩き台の提案
- 推測は「推測」と明記する

## 決まっていること

- 対象は Android / iOS / Windows [決定]
- クライアントはサーバー権威のもとで、表示と入力を担う [決定。[CLAUDE.md](../../../CLAUDE.md)]
- **画面は uGUI + `UnityScreenNavigator`** で作る [決定。[ADR 0009](../decisions/0009-client-stack.md)]
- **DI は VContainer、非同期処理は UniTask** [決定。ADR 0009]
- **画面は縦向き**。Windows は縦長のウィンドウで動かす [決定。ADR 0009]
- **シーンは Bootstrap / Title / Home / Produce の 4 つ**。Bootstrap が起動の起点。Title でスタートをタップすると、サインアップ・サインインの通信をする [決定。2026-11-03。ユーザーの判断。7章]
- クライアントは `Uta.Shared.Domain` を、使用可否の判定と予測の表示まで使う。シードは持たない [決定。[ADR 0008](../decisions/0008-shared-domain-and-onion.md)]

## 1. Presentation 層の構成 [Uta案]

[repository-layout.md](repository-layout.md) 4章の依存ルールに従う。`Presentation` は `Application` と `Domain` だけを参照し、通信の DTO や `Infrastructure` に触れない。

| 要素 | 役割 | 置き場 |
|---|---|---|
| View | 表示と入力の受け取りだけ。ロジックを持たない | `Presentation/<機能>/` |
| Presenter | View と `Application` のユースケースをつなぐ。状態の表示用への変換 | 同上 |
| 画面の種類 | `UnityScreenNavigator` の概念に合わせる: ページ（全画面）、モーダル、シート | — |
| 組み立て | DI（VContainer）のスコープは `Bootstrap` で組む | `Bootstrap/` |

- 機能ごとのフォルダは機能ID・機能名で切る（例: `Presentation/Gacha/`）
- ユースケースが返す型（表示用の結果）は `Application` が持つ。`Presentation` が DTO を直接受けない

## 2. 画面の一覧 [Uta案]

仮の一覧。名前・分け方はレビューで決める。

| 画面 | 種類 | 機能ID | 呼ぶ API（仮） | 主なマスタ |
|---|---|---|---|---|
| 起動・ロード | ページ | — | RegisterAsync（初回）、IssueTokenAsync、SignUpAsync（初回）、SignInAsync（4章）。マスタは同梱を読む（RPC にしない） | マスタのバイナリ |
| ホーム | ページ | — | サインインで取った所持データ（キャッシュ）を表示。「プロデュース」から再開を選んだら GetState（進行中かは起動時の `SignInAsync` で知る。[api-design.md](api-design.md) 1章） | — |
| ガチャ（一覧） | ページ | GAC-01 | GetGachas | ガチャ、排出率 |
| ガチャ（排出率の表示） | モーダル | GAC-02 | — | 排出率（マスタから生成） |
| ガチャ（結果） | ページ／モーダル | GAC-01、GAC-04 | Draw | 育成アイドル、サポートカード |
| 交換 | ページ | GAC-03 | Exchange | 交換pt の設定（MVP では作らない。天井は結合テストで確かめる） |
| 所持一覧・詳細 | ページ | IDL-01、SUP-01 | サインインで取った所持データ（キャッシュ） | 育成アイドル、サポートカード |
| 育成アイドルの強化 | ページ | IDL-02 | Train、Awaken | トレーニング・覚醒の設定（MVP では作らない） |
| サポートカードの強化 | ページ | SUP-02 | LevelUp、LimitBreak | サポートカードのレベル・上限解放の設定（`SupportLevelCost`・`SupportLevelCap`）。MVP で作る（[game-spec.md](../overview/game-spec.md)。2026-11-01） |
| 編成 | ページ | PRD-01 | Start | アイドル、サポートカード、シナリオ（シナリオを選ぶ。レンタル枠の `GetRentalCandidates` は MVP では作らない） |
| 週の行動選択 | ページ | PRD-02 | ChooseWeekAction、GetState | 週構成、行動 |
| レッスン | ページ | PRD-03〜08 | ExecuteLessonAction | スキルカード、レッスンルール |
| 試験 | ページ | PRD-06、PRD-07 | ExecuteLessonAction | 試験の設定、スコアボーナス |
| プロデュース結果 | ページ | PRD-13 | 終了の局面に入った操作のレスポンス（`ExecuteLessonAction` または `DecideExamRetry`）。終わった後の `GetState` は結果を返さない（[produce-domain.md](produce-domain.md) 8章） | 評価の設定 |
| 設定・お知らせ | ページ／モーダル | — | — | — |
| スキルカード獲得 | モーダル | PRD-02 | ResolveCardOffer | スキルカード |
| 授業・おでかけ・支給 | ページ | PRD-02 | ChooseWeekAction | 行動、イベント（MVP は授業・支給だけ） |
| 相談 [Uta案・追加] | ページ（案） | PRD-09 | ChooseWeekAction、ExecuteConsultAction | スキルカード、ドリンク（MVP では作らない） |
| 試験結果 | ページ | PRD-06 | DecideExamRetry（再挑戦が残るときだけ。残らないときは結果を出して「次へ」。試験が終わった操作のレスポンスの `ExamResult` を出す） | 試験の設定 |

- MVP で作る画面は [game-spec.md](../overview/game-spec.md) の「画面」が正。「次点」「後回し」の機能（PRD-09〜12、PRD-14〜17、IDL-02、SUP-04〜06、GAC-05〜07）の画面は、その機能を実装するときに足す
- 山札・捨札の確認（PRD-08）は、レッスン中に開くモーダルとして持つ案

## 3. 画面遷移 [Uta案]

```
起動・ロード ─▶ ホーム ─┬─▶ ガチャ ─▶ ガチャ結果 ─▶ ガチャ
                        │        └─▶ 交換
                        ├─▶ 所持一覧 ─▶ 詳細 ─▶ 強化
                        └─▶ 編成 ─▶ 週の行動選択 ─┬─▶ レッスン ─┐
                                      ▲           └─▶ 試験    ─┤
                                      └──────────────────────┘
                                      （最終試験の後、または不合格で終わったとき）─▶ プロデュース結果 ─▶ ホーム
```

- 進行中のプロデュースがある状態でアプリを起動したら、ホームから**再開**できる（`GetStateAsync` で復元。PRD の「中断・再開」）。アプリのバージョンが変わっていたら、そのプロデュースは破棄されている（[api-design.md](api-design.md) 5章）
- プロデュース中の画面から、確認のダイアログを経て**放棄**できる（`AbandonAsync`）
- 試験の再挑戦（PRD-06）は、試験結果の画面で選び、再び同じ試験へ（合否にかかわらず選べる。[produce.md](../systems/produce.md) 5.1.1）

## 4. 起動フロー [Uta案・推測]

`Bootstrap` が行う。詳細は [server-foundation.md](server/server-foundation.md) と [masterdata-pipeline.md](master/masterdata-pipeline.md) 6章。

1. マスタ [決定。2026-11-01。ユーザーの判断: [計画](../../plans/2026-11-01-masterdata-client-use.md) 7章]: 通信が要らないので**認証の前**に、アプリに同梱したバイナリ（StreamingAssets）を `MasterDataBinary.Read` で読む。あとで Addressables によるダウンロードに移る（[masterdata-pipeline.md](master/masterdata-pipeline.md) 6章）。読めなければ（`SchemaMismatch` を含む）リトライせず、エラーを表示して止まり、認証には進まない（同梱のバイナリは再試行しても同じ結果になる）。`SchemaMismatch` でアプリの更新を促すのは、Addressables に移ったあと
2. 認証: 端末に保存が無ければ端末の登録（`RegisterAsync`）、そのあとトークンの発行（`IssueTokenAsync`）（どちらも `IDeviceService`）。無効・期限切れは1回だけ取り直して再送する（VolTech-MagicOnion の仕様）
3. （サーバーの状態の判定は無い）強制アップデートとメンテナンスは実装しない（[repository-layout.md](repository-layout.md) 10.5）。VolTech-MagicOnion のクライアントは `UPDATE_REQUIRED`・`MAINTENANCE` の受け口を持つが、サーバーは返さないので、Uta の画面は作らない
4. サインアップ・サインイン [決定。2026-11-01。ユーザーの判断。`SyncAsync` を分けた]: 端末の登録をした直後（初回）は `IPlayerService.SignUpAsync`（プレイヤーの作成と初期付与）を呼び、そのあと `SignInAsync`（所持データの全件取得とバージョン違いのプロデュースの破棄。[api-design.md](api-design.md) 1章・5章）を呼ぶ。保存ありの起動は `SignInAsync` だけ。`SignInAsync` が `PLAYER_NOT_CREATED` を返したら、`SignUpAsync` を呼んでから `SignInAsync` をやり直す [決定。2026-11-01。ユーザーの判断]。プロデュースを破棄したと返ってきたら、ホームでダイアログを出して知らせる。受け取った所持データはクライアントがキャッシュする（Domain のリポジトリ `IPlayerRepository`・`IIdolRepository`・`ISupportCardRepository`。2026-11-08。[ADR 0021](../decisions/0021-domain-entities-and-repositories.md)）。実装の経路は [api-design.md](api-design.md) 1章「クライアントの実装」。`Title` の最小の uGUI に状態を出し、成功したら `Home` へ進む（7章）。診断用の IMGUI（`ErrorDialogView`、マスタの失敗の `StartupView`）が残る。画面の作り込みは順 7 [2026-11-03]
5. ホームへ遷移する

シーンとの対応 [決定。2026-11-03。ユーザーの判断。7章]: 手順 1 は Bootstrap シーン、手順 2〜4 は **Title シーンでスタートをタップしたとき**、手順 5 は Home シーンの読み込み。マスタの読み込みはタップの前（Bootstrap）に済ませる。

Addressables に移ったあとの、更新確認のタイミングは未決。

## 5. 通信とエラーの画面側の扱い [Uta案]

- VolTech-MagicOnion のクライアント（`ApiCore`: 順番待ち、再認証、進行中通知、エラーハンドラー）を使う（組み立ては `ApiClient`。ナビゲーターは `ErrorNavigator`（7章）、進行中通知は未接続で、エラーハンドラーは `DefaultCommonErrorHandler` と診断用の `ErrorDialogView`。画面の計画で作り直す。2026-11-03）。画面は `ErrorPolicy` に従って、再試行・ダイアログ・タイトルへ戻る、を出し分ける（VolTech-MagicOnion の設計書「クライアント側」）
- 通信中は入力を受け付けない表示（ブロッカー）を出す。進行中通知の受け口は VolTech-MagicOnion のものを使う
- 通信の失敗後の再送は、同じ `x-request-id` で行う。画面側で独自の重複防止はしない
- **日付の更新（12:00 JST）をまたいだとき** [決定。2026-10-31。ユーザーの判断]: VolTech-MagicOnion は日付が変わったあとの RPC に `DATE_CHANGED` を返し、既定の動作はタイトルへ戻る。MVP はこれを受け入れ、変えない。プロデュース中でも状態はサーバーに残っているので、起動の流れ（4章）からホームの「再開」で続ける（[api-design.md](api-design.md) 1章 `SignInAsync`）。ただし `DATE_CHANGED` を受けたあとのサインインは、全件を取り直す

## 6. レッスン・試験画面とドメインの関係 [決定]

- レッスン・試験の画面は、`Uta.Shared.Domain` の効果エンジンで**使用可否の判定と予測値の表示**をする（PRD-04、PRD-08）。確定はサーバーの結果（[ADR 0008](../decisions/0008-shared-domain-and-onion.md)）
- クライアントはシードを持たないので、ドローなど乱数が絡む結果は予測しない。操作後は、サーバーのレスポンスで画面を確定させる
- 画面（Presenter）は `Uta.Shared.Domain` を直接呼ばず、`Application` の UseCase の IF（`I<機能>UseCase`。[ADR 0022](../decisions/0022-client-external-concretes-in-infrastructure.md)）越しに判定・予測の結果を受け取る
- **MVP のカードの使用可否はサーバーが判定して返す** [決定。2026-11-03。ユーザーの判断]: `LessonDto` に `UsableHand`（`bool[]`。`Hand` と同じ長さ・同じ順。`LessonEngine.CanUse` が Ok のとき true）を足す（[server/produce-server.md](server/produce-server.md) 6章）。`LessonDto` から `LessonState` を復元するには持続効果の定義（`EffectDef`）などが足りず、クライアントだけでは判定できないため。ADR 0008 の「クライアントが判定」からは外れるが、確定はサーバー、表示のための判定もサーバーの値を使う、という向きは同じ。クライアントの予測（効果の見込み）は MVP では出さない（[lesson-domain-types.md](lesson-domain-types.md)）

## 7. MVP の画面の実装方針 [決定。2026-11-07]

MVP の画面（[game-spec.md](../overview/game-spec.md) の「画面」）を作る実装計画（順 7。[索引](../../plans/2026-11-07-client-screens-index.md)）が共通して従う方針。型のシグネチャと Prefab の構造は各計画が持ち、このページは「なぜそうするか」と置き場を持つ。シーンの分け方は 8章。

### 7.1 画面は Prefab で作る。Prefab は Editor のスクリプトで生成する [決定。2026-11-07。ユーザーの判断]

- 画面（ページ・モーダル）と共通部品は **Prefab**。Prefab は Sonnet が YAML で手書きせず、**Unity Editor を unity-cli で動かして、C# の Editor スクリプト（`Scripts/Editor/Prefabs/`）で生成し、保存する**。生成された `.prefab`・`.meta` をコミットする。スクリプトは冪等（再実行すると同じ Prefab ができる）にする。Editor で手作業した修正は、生成スクリプトに戻す（戻さないと次の再生成で消える）
- C# は GameObject を組み立てない。View は Prefab のルートに付く MonoBehaviour（`PageBase`／`ModalBase` を継承）で、子の参照を `[SerializeField]` で持つ。参照は生成スクリプトが `SerializedObject` で代入する。参照が欠けていないことは EditMode のテスト（`PrefabContractTests`: 全 Prefab を読み、必須の参照が `null` でないこと）で確かめる
- Unity の Editor が開いていない環境では、Prefab の生成・確認はできない。計画のタスクは「確認できていない」と書き、ユーザーに実行を頼む（推測で「通った」と書かない）

### 7.2 Prefab の読み込みは Addressables [決定。2026-11-07。ユーザーの判断]

- 画面の Prefab は Addressables のグループ `Screens` に入れる。アドレスは Prefab の名前（例 `HomePage`）。登録は Editor のスクリプト（`AddressableAssetSettings` の API）が行う。`PageContainer`／`ModalContainer` の `AssetLoader` は USN の `AddressableAssetLoader`
- **アドレス・グループ・ラベル・ロードと解放** [Uta案]: グループは `Screens` の 1 つ（MVP。画面数が増えたら機能ごとに分ける）。アドレスは Prefab の名前そのまま（`HomePage`、`GachaRatesModal`）で、Presenter の `[AssetAddress(nameof(HomePage))]` と同じ文字列。ラベルは付けない。ロードと解放は USN のコンテナー（`AddressableAssetLoader`）が Push／Pop で行い、Presenter・View は `Addressables` の API を呼ばない。リスト行などの共通部品の Prefab は Addressables に入れず、View の `[SerializeField]` で直接参照する（画面の Prefab と一緒にビルドされる）。Editor のスクリプトが、`Assets/Prefabs/Screens/` の Prefab を `Screens` グループにアドレス付きで登録する
- 基盤の `IScreenNavigator`（Presenter 駆動。`[AssetAddress("HomePage")]` のキーがアドレスになる）をそのまま使う
- プレイヤーのビルドの前に `AddressableAssetSettings.BuildPlayerContent()` を実行する（`BuildCommands.BuildWindowsIl2Cpp` の先頭に足した。ビルドターゲットが `StandaloneWindows64` でなければ止める。Addressables の設定の `BuildAddressablesWithPlayerBuild` は `DoNotBuildWithPlayer`）
- **確かめたこと**（2026-11-08。[P1 の計画](../../plans/2026-11-07-client-prefabs-common.md) のタスク 1・6。[unity7-evaluation.md](unity7-evaluation.md) 2章）: Addressables 4.1.2 で、`Screens` グループの作成と Prefab の登録（`AddressablesSetup`）、Editor の Play での `AddressableAssetLoader` 経由の `PageContainer.Push`、`BuildPlayerContent`（`result.Error` が空）、Windows の IL2CPP のプレイヤーでの `Push` が通った。Play とプレイヤーは使い捨てのスパイク（専用シーン。プレイヤーは `-batchmode -nographics` で終了コード 0、コンソールのログで判定）。`BuildCommands` に足した `BuildPlayerContent` は、Editor の `eval` で単体を実行して `result.Error` が空だった
- **未確認**: `BuildPlayerContent` を足したあとの `Uta.exe` の IL2CPP のビルドと `-uta-quit-after-check`（Prefab を足すだけの計画ではビルドを確かめない。ユーザーの判断。2026-11-08）。画面の Prefab を基盤の `IScreenNavigator` から読む本番の経路（C# の計画で確かめる）

### 7.3 文字は TextMeshPro + 日本語のフォントアセット [決定。2026-11-07。ユーザーの判断]

- 文字は TextMeshPro。フォントは **Noto Sans JP（SIL Open Font License 1.1）**。フォントのアセットは**作成済み**（ブランチ `claude/noto-sans-jp-font`、コミット `a86f436`。PR #29 で main に入った）: `client/uta/Assets/Fonts/NotoSansJP/` に静的な ttf（Regular・Bold）、`OFL.txt`（ライセンス文）、TMP の動的 SDF のフォントアセット `NotoSansJP-Regular SDF.asset`・`NotoSansJP-Bold SDF.asset`（Atlas Population Mode = Dynamic で全部の漢字を事前に焼かない。Multi Atlas、サンプリング 48、パディング 5、アトラス 1024、Clear Dynamic Data On Build 有効）。可変フォントは TMP の Dynamic が wght 軸を扱わないので使わない
- 文字は Regular のアセット、**太字は Bold のアセット**を使う（TMP の疑似太字 `FontStyles.Bold` は使わない）
- **フォントの読み込みはシリアライズ参照**: `TMP_Text.font` は生成スクリプトが Prefab に入れる参照で持つ。TMP の既定のフォントも Regular のアセット。フォントは Addressables に入れない（Addressables に入れるのは画面の Prefab だけ。フォントは Prefab・シーンから参照されてビルドに入る）
- Editor で文字を描くと動的アセットのアトラスに字が足され `.asset` が変わる。Prefab の生成の最後に動的データを空に戻し、残った差分はコミットしない（コミットするアセットはアトラスが空）
- Unity 7（uGUI 7.0.0）は TMP の Essential Resources の取り込みが無い（既定の設定・シェーダーがパッケージに同梱）。そのため取り込みはせず、プロジェクトの TMP Settings（`Assets/TextMesh Pro/Resources/TMP Settings.asset`）を Editor のスクリプトで作り（無ければ）、既定のフォントを Regular のアセットにする
- 確かめてあること（フォントの作業。Editor）: `com.unity.ugui` 7.0.0 に TMPro の型がある、`Assets/` に `TextMesh Pro` フォルダが無くても `Shader.Find("TextMeshPro/Distance Field")` が見つかる、アセットのシェーダーは `TextMeshPro/Mobile/Distance Field`、日本語 15 文字がアトラスに載る
- **確かめたこと**（2026-11-08。[P1 の計画](../../plans/2026-11-07-client-prefabs-common.md) のタスク 1 のスパイク。[unity7-evaluation.md](unity7-evaluation.md) 2章）: 手を入れる前はプロジェクトの TMP Settings が無く、既定のフォントはパッケージ同梱の `DefaultFont` だった。`TmpSettingsBuilder` が同梱の設定を複製して作り、既定のフォントを `NotoSansJP-Regular SDF` にできた。Editor の Play と Windows の IL2CPP のプレイヤー（`-batchmode -nographics`）の両方で、`TMP_Text` の日本語について `font.HasCharacters` が Regular・Bold とも true だった（コンソールのログで判定）
- **未確認**: 画面で豆腐にならないかを目で見る確認（Game ビュー、プレイヤーを引数なしで起動。手動で、まだ行っていない）。Android・iOS の実機
- アプリ内のライセンス表示の画面は MVP では作らない（ストアに出すときに作る。ユーザーの確認待ち）

### 7.4 Presentation の部品と置き場 [Uta案]

| 要素 | 置き場 | 役割 |
|---|---|---|
| Prefab | `Assets/Prefabs/Parts/`（共通部品）、`Assets/Prefabs/Screens/<機能>/`（画面） | 7.1 のとおり。画面の Prefab の中に部品の Prefab をネストして置く。共通部品は 10 個: `Label`・`Button`・`Panel`・`ListRow`・`Header`・`Tab`・`TabBar`・`Dialog`・`BusyBlocker`・`UiRoot`（[P1 の計画](../../plans/2026-11-07-client-prefabs-common.md) 3.4）。画面の Prefab だけを Addressables の `Screens` に登録する。部品は画面の Prefab にネストし、`UiRoot` はシーンに置く（どちらも登録しない）。画面の Prefab: `Title/TitlePage`・`Home/HomePage`・`Gacha/GachaPage`・`GachaRatesModal`・`GachaResultModal`・`Owned/OwnedPage`・`SupportDetailPage`（[P2 の計画](../../plans/2026-11-07-client-prefabs-screens-a.md) 3.2。フィールドと子の対応は同 3.3）。行の Prefab（`Parts/`。登録しない）: `GachaRateHeadingRow`・`GachaRateRow`・`GachaResultRow` |
| 生成スクリプト | `Scripts/Editor/Prefabs/` | 部品・画面ごとに 1 クラス。共通の補助（`PrefabBuildUtil`）を使う。入口は `PrefabBuildCommands`（メニュー `Uta/Prefabs/Build All`、`unity command eval` から呼ぶ `BuildAll()`）。生成クラスは `IPrefabBuilder` を実装すれば Build All に入る（`TypeCache` で集め、`Order` の順に作る） |
| View | `Presentation/<機能>/` | Prefab のルートに付く `PageBase`／`ModalBase` の派生。表示と入力の受け取りだけ。ロジックを持たない |
| Presenter | 同上 | View と `Application` の UseCase の IF をつなぐ（基盤の `IPresenter`。コンストラクターで具象の View を受け、テスト用に `IXxxView` を受ける 2 つ目のコンストラクターを持つ） |
| UseCase | IF は `Application/<機能>/`、具象は `Infrastructure/<機能>/` | Application に UseCase の IF `I<機能>UseCase` とその出力の型を置き、外部に触れる具象 `<機能>UseCase` は Infrastructure に置く（`I<機能>Service` を直接呼び、DTO を static の `<名詞>Mapper` で詰め替え、リポジトリに書く。通信のポート・ゲートウェイは作らない。[ADR 0022](../decisions/0022-client-external-concretes-in-infrastructure.md)。例: `IPlayerUseCase`・`PlayerUseCase`）。`Presentation` は `Infrastructure` に触れない |
| 組み立て | 各シーンの `LifetimeScope`（8章） | シーンの `UiRoot` と、そのシーンの入口・補助を登録する。**Presenter はどのスコープにも登録しない**（Presenter の `[AssetAddress]` で、基盤の `IScreenNavigator` が Push のたびにシーンのスコープの子スコープで解決する）。UseCase の具象（IF で登録）・リポジトリ（書き込みと読み取り専用の 2 つの IF で同じインスタンス）は Bootstrap のルートのスコープ |

- **遷移**: ホーム（またはシーンの最初のページ）を底にする。局面ごとの切り替えは、いまのページを `Pop` してから次を `Push`（`Replace` は作らない: USN の `stack: false` は「積むページ自身を履歴に残さない」意味で、前のページを消すものではない）。ホームへ戻るときは上のページ・モーダルを全部 `Pop`
- **Presenter の作法**: 購読は `InitializeAsync`、解除は `Dispose`。実行中フラグは `finally` で戻す。テストが await できるよう、処理は公開の `UniTask` メソッドにする
- **命名**: Prefab 名 = View の型名（`HomePage`）。子の GameObject は役割の PascalCase（`BackButton`、`TitleText`、`ListContent`）。部品の Prefab も接頭辞を付けず、役割の名前にする（`Button`・`Label`・`Panel`。View の型は Prefab 名 + `View`: `ButtonView`）。部品に `Uta` 接頭辞は付けない [決定。2026-11-07。ユーザーの判断]

#### 7.4.1 テストの偽物と詰め替えの規約 [[ADR 0022](../decisions/0022-client-external-concretes-in-infrastructure.md) の「影響・リスク」。2026-11-08]

UseCase の具象を Infrastructure に置くと、手順のテストが MagicOnion の `IService<T>` のメンバー・基盤のエラーの表し方・DTO の形の 3 つに結合する。重さを抑えるため、次の形にそろえる。

- **偽のサービスは機能ごとに 1 つ（`Fake<機能>Service`）**。テストのアセンブリ（`Uta.Client.Tests.EditMode`）の `Tests/EditMode/Fakes/` に public で置いて使い回す。返す値の Queue と、呼び出しの記録を持つ。`IService<T>` の `With…` は `this` を返す（`WithHandledCodes` の実体が `WithHeaders` を呼ぶため）。`WithHost` だけ `NotSupportedException`。失敗は基盤の `FakeRpcErrors.ApiFailureResult<T>`（VolTech-MagicOnion の Unity パッケージの `Runtime/Client/Testing`）で作った `UnaryResult<T>` を返し、偽物のメソッドから同期で例外を投げない。実例: `FakePlayerService`（`SignUpResults`・`SignInResults` の Queue、`Calls`、積み方の補助 `Ok<T>`・`Fail<T>`）
- **Presenter のテストの偽物は `Fake<機能>UseCase`**。同じくテストのアセンブリに置く。返す値の Queue の要素は `Func<UniTask<T>>` にし、本物の具象が行う副作用（状態の型・リポジトリへの書き込み）と例外は、テストが積む `Func` の中で再現する。偽そのものは状態の型・リポジトリに書かない。例外: 状態の型に書くだけのメソッド（`IProduceUseCase.MarkExamResultShown()` など）は、偽が受けた状態の型に書いてよい
- **DTO → Application・Domain の型の詰め替えは、static の純粋な関数（`<名詞>Mapper`）に分け**、詰め替えのテストを手順のテストと別に持つ。実例: `PlayerMapper` と `PlayerMapperTests`、手順は `PlayerUseCaseTests`（リポジトリは本物のメモリの実装を使う）
- 非同期の手順のテストは `[UnityTest]` と `UniTask.ToCoroutine` で書く（`PlayerUseCaseTests`）。回すときは `runSynchronously` を付けない（付けると `[UnityTest]` が走らない。[unity7-evaluation.md](unity7-evaluation.md) 4章）
- 偽物は `ApiCore` を通らないので、`WithHandledCodes` に載せたコードを `ApiCore` が共通ハンドラーに回さないことは、手順のテストでは確かめない（基盤のテストと、実機相当の確認で見る）

### 7.5 通信・状態・マスタの扱い [Uta案]

- **所持データのキャッシュ**: Domain のリポジトリ（`IPlayerRepository`・`IIdolRepository`・`ISupportCardRepository`。読むだけの使い手には `IReadOnly<名詞>Repository` を渡す。[ADR 0021](../decisions/0021-domain-entities-and-repositories.md)）。サインインの応答で `IPlayerUseCase` の具象が 3 つとも置き換える（プレイヤーは `Upsert`、アイドル・サポートカードは `Clear()` のあと `Upsert(IReadOnlyList<T>)`）。エンティティは、サーバーが採番する ULID を `UniqueId`（`string`）、マスタの ID を `MasterId`（`int`）として持つ（`PlayerEntity` は `UniqueId` だけ、`IdolEntity`・`SupportCardEntity` は両方）。1 件を引く・置き換える・消すのは `UniqueId` で行い（`IReadOnlyIdolRepository.Find(string uniqueId)`、`IIdolRepository.Upsert` は `UniqueId` が同じものを置き換える）、`FindAll` は `MasterId` の昇順 [決定。2026-11-08。ユーザーの判断。[naming.md](naming.md) 2章]。ガチャ・強化・プロデュースの終了のあとは、変わった量をクライアントで計算せず、サインインで取り直す（`IPlayerUseCase.RefreshAsync`）。サーバーの確定値と食い違わせないため。`Common` を返す RPC では、`Common` の player_diff が先にリポジトリへ反映される（基盤が呼び出し側に返す前に `PlayerCommonApplier` を呼ぶ。[server-foundation.md](server/server-foundation.md) 共通レスポンス項目）。`RefreshAsync` は安全網
- **マスタ**: 画面が使うマスタの値は、`MemoryDatabase`（`Uta.MasterData`。ルートのスコープにシングルトンで登録してある）を直接読む。Presentation でマスタを読むのは表示用の値を作る DtoFactory で、マスタの値でゲームのルールを計算しない（[ADR 0020](../decisions/0020-masterdata-referenced-outside-shared-domain.md)。`IGameMaster` は作らない）
- **スキルカードの表示**: 名前と説明文はマスタに足す [決定。2026-11-03。ユーザーの判断。[計画](../../plans/2026-11-03-masterdata-card-text.md)]。画面は名前・レア度・種類・消費・強化済みの印・説明文を出す。効果の予測値は出さない
- **ブロッカー**: VolTech-MagicOnion の `IApiActivityHandler` を `BusyIndicator` につなぐ（進行中は全面の透明な Image で入力を止める）。画面は個別に包まない
- **エラー**: 業務エラーのコードは `ErrorDefinitions` に `Stay` で登録し、画面が文言を出す。「タイトルへ」「ホームへ」は `IErrorNavigator`（8章の `ISceneNavigator` を呼ぶ）
- **カードの使用可否はサーバーが判定して返す**（6章の `UsableHand`）

### 7.6 プロデュースの画面の流れ [Uta案]

- 局面（`ProducePhase`）が画面を決める。`ProduceSession`（最新の `ProduceStateView` と次の操作番号）を操作のたびに更新し、`ProduceFlow` が局面から画面への対応を持つ: `WeekSelect` → 週の行動選択（授業は別ページ。screen-wireframes のとおり授業・支給はページ）、`CardOffer` → スキルカード獲得（モーダル）、`Lesson`・`Exam` → レッスン・試験、`ExamResult` → 試験結果、`Finished` → プロデュース結果
- 試験が終わった応答の `ExamResult` は、次の局面の前に試験結果の画面で見せる
- `PRODUCE_STALE_OPERATION` は `GetStateAsync` で取り直す。`PRODUCE_SESSION_DISCARDED`・`PRODUCE_NOT_IN_PROGRESS` はホームへ戻る
- 授業・休むの結果は、操作の前後の状態の差で出す。ターン終了時の効果の 1 件ずつの表示は MVP から外す（応答に効果の記録が無い）

### 7.7 ウィンドウ・確かめ方 [Uta案]

- Windows: 初期 540×960、ウィンドウモード、縦向きのみ。比率を 9:16 に保つ補正（`WindowAspectKeeper`）
- 確かめ方は計画ごとに「EditMode のテスト／ Play モード／ Windows の IL2CPP のビルド／手動」のどれかを書く。Editor が開けない環境では、確かめていないと書く

### 7.8 ホームの構成とキャラ表示のステージ（`ICharacterStage`）。MVP は仮の枠、3D は拡張点 [決定。2026-11-07。ユーザーの判断]

- **ホームの構成**: 上部のステータス（プレイヤーLv・ガチャ通貨）、中央のキャラ表示のステージ、大きな主ボタン「プロデュース」（進行中のプロデュースがあるときは「再開」の表示）、下部のナビ（「プロデュース」「ガチャ」「アイドル」）。主ボタンとナビの「プロデュース」は同じ動作（進行中なら再開・放棄・やめるを選ぶ）。ホーム自体はナビのタブにしない。並べ方と作らないものは [screen-wireframes.md](../overview/screen-wireframes.md)「ホーム」が正。他のゲームのホーム画面の構造だけを参考にし、見た目の素材・文言は使わない（[ADR 0001](../decisions/0001-ip-policy.md)）
- **ステージの Prefab**: ホームのページの子 `Stage` に `RawImage` を置き、その子に枠の文字 `StageLabel`（TMP）を置く。MVP の `RawImage` はテクスチャなしの色つきの枠（`UiTheme.Panel`）で、文字は「キャラ表示」。値は P2 の計画（[2026-11-07-client-prefabs-screens-a.md](../../plans/2026-11-07-client-prefabs-screens-a.md) 3.2）
- **C# は `ICharacterStage` で隠す**（`Uta.Client.Presentation.Home`）: `void ShowIdol(int idolId)`（その育成アイドルを出す）と `void Clear()`（何も出さない）だけ。`HomePresenter` は `ICharacterStage` だけを知り、どう描くかを知らない
- **MVP の実装は `PlaceholderCharacterStage`**: 枠の文字に育成アイドルの名前（`IGameMaster.GetIdol(idolId).Name`）を出すだけ。`Clear` は文字を「キャラ表示」に戻す。`RawImage` には触らない
- **出す育成アイドルの選び方**（`HomePresenter` が決める）: 所持の育成アイドルのうち、レア度（`Rarity` の数値）が最も高いもの、同率なら `IdolEntity.MasterId`（アイドルのマスタの ID）が小さいもの。所持が 0 なら `Clear`。ホームを表示するたびに（最初の表示と、上のページから戻ったとき）選び直す
- **拡張点（MVP 外）: `ModelCharacterStage`**: 専用のカメラが育成アイドルの 3D モデルを `RenderTexture` に描き、その `RenderTexture` を `Stage` の `RawImage.texture` に割り当てる実装。`ICharacterStage` を実装し、`HomePage` がステージを作るところ（C# の計画 [foundation](../../plans/2026-11-03-client-screens-foundation.md) 3.7 の `HomePage.CreateStage`）だけを差し替える。`HomePresenter`・Prefab の階層は変えない。モデル・カメラ・`RenderTexture` の置き場と寿命、読み込み（Addressables か）は作るときに決める。3D ライブの保留（[ADR 0003](../decisions/0003-defer-3d-live.md)）とは別の、ホームのキャラ表示の拡張点

## 8. シーン構成 [決定。2026-11-03。ユーザーの判断。細部は Uta案]

Unity のシーン（`.unity`）は機能の単位で 4 つに分ける。画面（ページ・モーダル）は、シーンの中で `UnityScreenNavigator` が積む（順 7 の画面の計画で使う。この章の時点では各シーンに最小の uGUI を置くだけ）。

| シーン | 役割 | 遷移 |
|---|---|---|
| `Bootstrap` | 起動の起点（Build Settings の先頭）。マスタを読み、アプリ全体で共有するもの（ルートの DI スコープ）を作り、`Title` を読み込む。マスタの読み込みに失敗したら、エラーを表示して止まる（4章） | → `Title` |
| `Title` | 「スタートをタップ」を待つ。タップしたら端末の登録・トークンの発行・サインアップ・サインイン（4章 2〜4）を行い、成功したら `Home` へ。失敗したら状態を表示し、もう一度タップできる | → `Home` |
| `Home` | サインインで受け取った所持データを表示する（最小）。ここから `Produce` へ。ホームの構成とキャラ表示のステージは 7.8 | → `Produce` |
| `Produce` | プロデュース。この章の時点では空（文字と `Home` へ戻るボタンだけ） | → `Home` |

- **ルートの DI スコープは `Bootstrap` が作る**（`DontDestroyOnLoad`）。通信（`ApiClient`・`IPlayerService`）・所持データのリポジトリ（3 つ）・マスタ（`MemoryDatabase`・`ICardCatalog`）・共通エラーハンドラー・**すべての UseCase の具象**（IF で登録）・`ISceneNavigator`・`IErrorNavigator` を登録する。`Title`・`Home`・`Produce` は、それぞれのシーンに置いた `LifetimeScope`（Presentation の `TitleLifetimeScope` など）が、ルートを親にして、そのシーンの `UiRoot`・入口と、そのシーンだけの補助（診断など）だけを登録する。Presenter と View は登録しない（Presenter の `[AssetAddress]` で、`IScreenNavigator` が Push のたびにシーンのスコープの子スコープで解決する。7.4）。シーンの読み込みは `LifetimeScope.EnqueueParent(ルート)` の中で行い、子が親を見つけられるようにする（ルートの `LifetimeScope` は VContainer が自分自身を登録するので、`SceneNavigator` がコンストラクターで受け取る）
- **シーンの遷移は `ISceneNavigator`**（Application の IF: `GoToTitleAsync`・`GoToHomeAsync`・`GoToProduceAsync`）。実装は `Bootstrap` に置く（`SceneManager.LoadSceneAsync` を単独（Single）で呼ぶ）。Presenter は `ISceneNavigator` だけを知り、シーンの名前や `SceneManager` を知らない
- **通信エラーの後の遷移**: `ApiCore` に `IErrorNavigator`（VolTech-MagicOnion）を渡す。`GoToTitleAsync` は `Title` の読み込み、`GoToHomeAsync` は `Home` の読み込み、`OpenStoreAsync` は未実装（ログだけ。強制アップデートは実装しないため。[repository-layout.md](repository-layout.md) 10.5）。実装は `ISceneNavigator` を呼ぶだけの小さなクラスで、`Bootstrap` に置く。`ReturnToTitle` で `Title` が読み込み直されると、通信中だった古い Presenter の続きが破棄された View に触らないよう、Presenter は View が破棄済みなら何もしない。診断（下記）のときはナビゲーターを渡さない
- **Editor で Play したときも `Bootstrap` から始める**（`EditorSceneManager.playModeStartScene`）。どのシーンを開いていても、起動の流れが同じになる
- **Home に入ったとき所持データが無い**（`IReadOnlyPlayerRepository.Find()` が null。サインインしていない）場合は、`Title` へ戻す。2026-11-08 から、#24 の仮の `HomePresenter` はこの確かめをしない（固定の文を出すだけ。画面の計画の `HomePresenter` で作り直す）
- 診断: `-uta-quit-after-check`（バッチモードの疎通の確認）のときは、`Title` がタップを待たずに開始し、サインインのあと診断（トークンの取り直し・再認証の再送・サーバーのエラーの受け取り）を行って、`Home` に進まずに終了コードで終わる。通常の起動では行わない。画面の計画で、診断を別の入口に分ける [Uta案]
- 最小の uGUI は、Editor のスクリプト（`Uta/Create Scenes`）で作り、`.unity` をコミットする（手で組み立てを再現できるように）。順 7 の画面の計画で、各シーンの最小の uGUI を 7章の Prefab に置き換える（日本語の文字は TextMeshPro + フォントアセット）
- 置き場: ビュー・Presenter・各シーンの `LifetimeScope` は `Presentation/<シーン名>/`。`Presentation` は `UnityEngine.UI` を参照してよい（`VolTech` のクライアントは参照しない）

## 未決事項

- 画面の名称・文言（Uta 独自のものにする。[glossary.md](../glossary.md) の仮称を置き換える）
- Addressables に移ったあとの、マスタの更新確認のタイミングと更新中の表示
- 予測値をどの画面要素に出すか（カードの詳細、スコアのプレビューなど）
- ガチャ結果の演出の範囲（MVP は演出なし。足すときに決める。3D ライブは保留。[ADR 0003](../decisions/0003-defer-3d-live.md)）
- 相談の画面の種類（相談を作るときに決める）

（2026-11-01 に解決: 基準解像度・セーフエリア・Windows のウィンドウ、スキルカード獲得・授業・支給・試験結果の画面の種類 → [screen-wireframes.md](../overview/screen-wireframes.md)「共通の方針」・2章の表）

## 実装メモ

### Prefab 1/3（共通部品・Addressables・TMP。2026-11-08。[P1 の計画](../../plans/2026-11-07-client-prefabs-common.md)）

- **確かめた範囲**: `BuildAll()` が `built 10`。もう一度回しても `Prefabs/Parts/` に差分は出なかった。Prefab の中身は `unity command eval` で計画 3.4 の表と突き合わせた（スクリーンショットでの見た目の確認はしていない）。`PrefabContractTests`（7 本）を含む EditMode のテストは、Editor を開いたまま `TestRunnerApi` で 43/43 が通り、`Header` の `titleText` を外すと `AllPrefabs_SerializedReferences_AreAssigned` だけが失敗した。バッチの入口 `BuildAllBatch` は終了コード 0 を 1 回確かめた
- **計画とのズレ・気づいたこと**:
  - `Pages`・`Modals`・`Overlays` に `RectMask2D` が自動で付く（USN の `PageContainer`・`ModalContainer` の `RequireComponent`）
  - `EventSystem` の `InputSystemUIInputModule.AssignDefaultActions` の結果は Prefab に保存されない（`m_ActionsAsset` は空）。実行時は `OnEnable` が、アクションが無いときに既定のアクションを入れるはず（Play では未確認）
  - `UiRoot` のルートの `RectTransform` は scale 0・anchor 0 で保存される（ルートの `Canvas` が実行時に決めるため。推測）
  - **`BuildAll` のたびに `NotoSansJP-Regular SDF.asset` のマテリアルの `_ScaleRatioA`・`_ScaleRatioC` が変わり、`FontAssets.ClearDynamicData()` では戻らない**。毎回 `git restore client/uta/Assets/Fonts/NotoSansJP` で戻し、コミットに含めない（CLAUDE.md「ビルド・テスト・実行」）
  - `PrefabContract.FindPrefabPaths` は、フォルダ（`Assets/Prefabs/Screens` など）が無いと空を返す（画面の Prefab が 0 個のときに通るため）
  - テストの結果は `Temp/uta-test-results.xml` に出すので、`.gitignore` に `test-results.xml` を足していない
  - プレイヤーのビルド（`BuildPlayerContent` を足したあとの IL2CPP）と、フォントの二重の取り込み（画面の Prefab のバンドルとプレイヤーの両方にフォントが入るか。推測）は、ビルドを計画から外したので確かめていない。ビルドするときに `Build/` と `StreamingAssets/aa` の大きさを見る

### Prefab 2/3（Title・Home・ガチャ・所持一覧。2026-11-08。[P2 の計画](../../plans/2026-11-07-client-prefabs-screens-a.md)）

- **確かめた範囲**: `BuildAll()` が `built 20`（部品 10 + 行 3 + 画面 7）。EditMode のテストは、Editor を開いたまま `TestRunnerApi` で 55/55 が通った。見た目は、画面の Prefab を一時的なプレビューのシーンで 1080×1920 の Canvas（`CanvasScaler` は P1 の `UiRoot` と同じ値）に置いて `RenderTexture` に描き、PNG で目で見た。あわせて各子の矩形の大きさを計画 3.2 の計算と突き合わせた（Home の `Stage` 1216、`GachaRatesModal`・`GachaResultModal` の `Panel` 1548・1448、`OwnedPage` の `List` 1296。どれも計画どおり）。Prefab モードで開いて見る手動の確認と、Safe Area の縮みは行っていない
- **叩き台（[screen-wireframes.md](../overview/screen-wireframes.md)）との差**（P2 の計画 決めたこと 10）: ガチャの結果は、絵のグリッドではなく 1 枠 1 行の文字にした。ガチャ名の行・画像・「交換」ボタンは置かない（gacha の C# の計画の範囲）。ホームの「プロデュース中」の帯には、週を出さない（foundation の C# の計画どおり）
- **再生成の差分**（P2 の計画 タスク 5）: もう一度 `BuildAll()` を回すと、`OwnedPage.prefab`（と、別の回では `HomePage.prefab`）に差分が出ることがある。中身は、ネストした部品のインスタンス（`PrefabInstance`）の `stripped` のオブジェクトの fileID の付け直しと、ブロックの並び替えだけだった。値（`m_Modifications` の `propertyPath`・`value`）は同じ。`Screens.asset` のエントリは重複しない。`Prefabs/Parts/Gacha*` には差分が出なかった
- **壊したときの確かめ**: `HomePage` の `Body/Main/Nav/NavGachaButton` を改名して保存すると、`HomePrefabTests` に絞ったテスト（`groupNames`。2 件だけが走った）で `HomePage_FieldsPointToChildren` だけが失敗し、失敗の文に `navGachaButton` と `Body/Main/Nav/NavGachaButton` が入った。改名は `HomePage.prefab` の中の `m_Name` のオーバーライドとして保存され、`Button.prefab` は変わらなかった。`BuildAll()` で戻すと全件が通った
- **計画とのズレ**:
  - **伸ばす H が true の HLG は、`flexibleHeight` を指定しないと 1 を返す**。そのため、行（`Nav`・`DrawRow` など）と部品 `Header`・`TabBar` が、親の VLG の余りを `Stage`・`Main` などと分け合って高くなった（例: `GachaPage` の `Header` が 720）。`ScreenLayout.AddRow` と、部品の `HeaderBuilder`・`TabBarBuilder` で `flexibleHeight: 0` を明示した（部品の変更は P1 の範囲の外）。P3 の計画で、高さを固定する HLG の行を作るときは同じようにする

### Prefab 3/3（編成・プロデュース。2026-11-08。[P3 の計画](../../plans/2026-11-07-client-prefabs-screens-b.md)）

- **作ったもの**: 画面 10（`FormationPage`（`Screens/Formation/`）、`ProduceBackdropPage`・`WeekPage`・`ClassPage`・`SupplyPage`・`CardOfferModal`・`LessonPage`・`DeckListModal`・`ExamResultPage`・`ProduceResultPage`（`Screens/Produce/`）。Addressables の `Screens` に登録）と行 5（`FormationSlotRow`・`SkillCardRow`・`DeckSectionHeaderRow`・`DeckCardRow`・`ExamRankingRow`（`Parts/`。登録しない））。View はフィールドだけ（メソッドと `IXxxView` の実装は formation・week・lesson の C# の計画）。生成の補助は P2 の `ScreenLayout` を使い、P3 だけが使うもの（戻るの無いヘッダー・メニューのボタン・実行時に並べる一覧の入れ物）を `ProduceLayout` に置いた
- **プロデュースのメニュー（[≡]）は Prefab を作らない**。P1 の `Dialog`（`DialogService.ChooseAsync`）で出す。[≡] のボタンだけを `WeekPage`・`LessonPage` のヘッダーの `RightSlot` に置いた（両画面はヘッダーの `BackButton` を非アクティブにし、`TitleText` を左寄せにした）
- **手札とスキルカード獲得の選択肢は、行 `SkillCardRow` を共有する**（どちらも名前・消費・説明文・タップで同じ形）
- **確かめた範囲**: `BuildAll()` が `built 35`（P2 の 20 + 行 5 + 画面 10）。`Screens` のエントリは 7 → 17。EditMode のテストは、Editor を開いたまま `TestRunnerApi` で 63/63（`ProducePrefabTests` 8/8）が通った。壊す確かめとして、`WeekPage` のヘッダーを戻るありにし `menuButton` の参照を外して生成すると、`PrefabContractTests`（参照の null）と `ProducePrefabTests`（`BackButton` の `activeSelf`）の 2 件が落ちた
- **見た目**: P2 と同じく、プレビューのシーンで 1080×1920 の Canvas に置いて PNG に描いて見た（画面 10 と行 `FormationSlotRow`・`SkillCardRow`）。配置は計画の表どおりで、日本語と `≡` は豆腐にならなかった。ただし **`CardOfferModal`・`DeckListModal` の `TitleText` と、`SkillCardRow` の `TitleText`・`CostsText` が PNG に描かれなかった**。どれも、それまでの画面に無かった字（獲得・名・消費・「・」など）を含む。動的フォントのアトラスに字を足した直後に描いたためと推測する（確かめていない）。描画の途中で Editor が落ち（クラッシュのフォルダは空で、原因は分からない）、残りの行 3 個の PNG と矩形の大きさの出力は取れなかった。`◀`・`＜` の字形と、Prefab モードで開いて見る手動の確認（計画 タスク 5 の手順 4）はしていない
- **再生成の差分**（計画 タスク 5）: 2 回目の `BuildAll()` で、`FormationSlotRow`・`FormationPage`・`OwnedPage` に、ネストした部品のインスタンスの fileID の付け直しと並び替えだけの差分が出た（fileID を伏せて並べ替えると同じ）。`FormationPage` が参照する `FormationSlotRow` の fileID は、付け直しの前後で同じだった。`Screens.asset` に差分は出なかった。`NotoSansJP-Regular SDF.asset` には、P1 から続く `_ScaleRatioA`・`_ScaleRatioC` の差分が出た（計画は「差分が無いこと」としていたが、既知の差分として戻した）
- **計画とのズレ**:
  - 行の View（`FormationSlotRowView` など 5 つ）に、P2 の `GachaLineRow` にならって `[DisallowMultipleComponent]` を付けた（画面の View には付けない。P2 と同じ）
  - `unity command` は、同じ PC で別のプロジェクトの Editor が開いていると、`--project-path` を付けないとそちらに送られることがあった（1 回、テストが別のプロジェクトで走った）。`unity command --project-path <client/uta の絶対パス> ...` で送る
