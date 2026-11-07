---
title: 画面・遷移・クライアントの層（実装設計書 画面設計編）
status: draft
updated: 2026-11-07
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
| ホーム | ページ | — | サインインで取った所持データ（キャッシュ）を表示。再開を押したら GetState（進行中かは起動時の `SignInAsync` で知る。[api-design.md](api-design.md) 1章） | — |
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
4. サインアップ・サインイン [決定。2026-11-01。ユーザーの判断。`SyncAsync` を分けた]: 端末の登録をした直後（初回）は `IPlayerService.SignUpAsync`（プレイヤーの作成と初期付与）を呼び、そのあと `SignInAsync`（所持データの全件取得とバージョン違いのプロデュースの破棄。[api-design.md](api-design.md) 1章・5章）を呼ぶ。保存ありの起動は `SignInAsync` だけ。`SignInAsync` が `PLAYER_NOT_CREATED` を返したら、`SignUpAsync` を呼んでから `SignInAsync` をやり直す [決定。2026-11-01。ユーザーの判断]。プロデュースを破棄したと返ってきたら、ホームでダイアログを出して知らせる。受け取った所持データはクライアントがキャッシュする（`PlayerCache`）。実装の経路は [api-design.md](api-design.md) 1章「クライアントの実装」。`Title` の最小の uGUI に状態を出し、成功したら `Home` へ進む（7章）。診断用の IMGUI（`ErrorDialogView`、マスタの失敗の `StartupView`）が残る。画面の作り込みは順 7 [2026-11-03]
5. ホームへ遷移する

シーンとの対応 [決定。2026-11-03。ユーザーの判断。7章]: 手順 1 は Bootstrap シーン、手順 2〜4 は **Title シーンでスタートをタップしたとき**、手順 5 は Home シーンの読み込み。マスタの読み込みはタップの前（Bootstrap）に済ませる。

Addressables に移ったあとの、更新確認のタイミングは未決。

## 5. 通信とエラーの画面側の扱い [Uta案]

- VolTech-MagicOnion のクライアント（`ApiCore`: 順番待ち、再認証、進行中通知、エラーハンドラー）を使う（組み立ては `UtaApiClient`。ナビゲーターは `ErrorNavigator`（7章）、進行中通知は未接続で、エラーハンドラーは `DefaultCommonErrorHandler` と診断用の `ErrorDialogView`。画面の計画で作り直す。2026-11-03）。画面は `ErrorPolicy` に従って、再試行・ダイアログ・タイトルへ戻る、を出し分ける（VolTech-MagicOnion の設計書「クライアント側」）
- 通信中は入力を受け付けない表示（ブロッカー）を出す。進行中通知の受け口は VolTech-MagicOnion のものを使う
- 通信の失敗後の再送は、同じ `x-request-id` で行う。画面側で独自の重複防止はしない
- **日付の更新（12:00 JST）をまたいだとき** [決定。2026-10-31。ユーザーの判断]: VolTech-MagicOnion は日付が変わったあとの RPC に `DATE_CHANGED` を返し、既定の動作はタイトルへ戻る。MVP はこれを受け入れ、変えない。プロデュース中でも状態はサーバーに残っているので、起動の流れ（4章）からホームの「再開」で続ける（[api-design.md](api-design.md) 1章 `SignInAsync`）。ただし `DATE_CHANGED` を受けたあとのサインインは、全件を取り直す

## 6. レッスン・試験画面とドメインの関係 [決定]

- レッスン・試験の画面は、`Uta.Shared.Domain` の効果エンジンで**使用可否の判定と予測値の表示**をする（PRD-04、PRD-08）。確定はサーバーの結果（[ADR 0008](../decisions/0008-shared-domain-and-onion.md)）
- クライアントはシードを持たないので、ドローなど乱数が絡む結果は予測しない。操作後は、サーバーのレスポンスで画面を確定させる
- 画面（Presenter）は `Uta.Shared.Domain` を直接呼ばず、`Application` のユースケース越しに判定・予測の結果を受け取る
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
- プレイヤーのビルドの前に `AddressableAssetSettings.BuildPlayerContent()` を実行する（`BuildCommands` に足す）
- **未確認**: Addressables 4.1.2 の Unity 7（`7000.0.0a12`）での動作（Editor の Play、Windows の IL2CPP、`BuildPlayerContent`）。計画の最初のタスクで確かめ、動かなければ止まって報告する（別の読み込み方法に勝手に変えない）

### 7.3 文字は TextMeshPro + 日本語のフォントアセット [決定。2026-11-07。ユーザーの判断]

- 文字は TextMeshPro。フォントは **Noto Sans JP（SIL Open Font License 1.1）**。フォントのアセットは**作成済み**（ブランチ `claude/noto-sans-jp-font`、コミット `a86f436`。2026-11-07 時点で main には未マージ。画面の Prefab の計画は積み PR で前提にする）: `client/uta/Assets/Fonts/NotoSansJP/` に静的な ttf（Regular・Bold）、`OFL.txt`（ライセンス文）、TMP の動的 SDF のフォントアセット `NotoSansJP-Regular SDF.asset`・`NotoSansJP-Bold SDF.asset`（Atlas Population Mode = Dynamic で全部の漢字を事前に焼かない。Multi Atlas、サンプリング 48、パディング 5、アトラス 1024、Clear Dynamic Data On Build 有効）。可変フォントは TMP の Dynamic が wght 軸を扱わないので使わない
- 文字は Regular のアセット、**太字は Bold のアセット**を使う（TMP の疑似太字 `FontStyles.Bold` は使わない）
- **フォントの読み込みはシリアライズ参照**: `TMP_Text.font` は生成スクリプトが Prefab に入れる参照で持つ。TMP の既定のフォントも Regular のアセット。フォントは Addressables に入れない（Addressables に入れるのは画面の Prefab だけ。フォントは Prefab・シーンから参照されてビルドに入る）
- Editor で文字を描くと動的アセットのアトラスに字が足され `.asset` が変わる。Prefab の生成の最後に動的データを空に戻し、残った差分はコミットしない（コミットするアセットはアトラスが空）
- [Uta案。確認待ち] Unity 7（uGUI 7.0.0）は TMP の Essential Resources の取り込みが無い（既定の設定・シェーダーがパッケージに同梱）。そのため取り込みはせず、プロジェクトの TMP Settings（`Assets/TextMesh Pro/Resources/TMP Settings.asset`）を Editor のスクリプトで作り（無ければ）、既定のフォントを Regular のアセットにする
- 確かめてあること（フォントの作業。Editor）: `com.unity.ugui` 7.0.0 に TMPro の型がある、`Assets/` に `TextMesh Pro` フォルダが無くても `Shader.Find("TextMeshPro/Distance Field")` が見つかる、アセットのシェーダーは `TextMeshPro/Mobile/Distance Field`、日本語 15 文字がアトラスに載る。**未確認**: 実際の `TMP_Text` の表示、TMP Settings の既定のフォントの設定状況、プレイヤーのビルド・実機。Prefab の計画（P1）の最初のタスクのスパイクで確かめる
- アプリ内のライセンス表示の画面は MVP では作らない（ストアに出すときに作る。ユーザーの確認待ち）

### 7.4 Presentation の部品と置き場 [Uta案]

| 要素 | 置き場 | 役割 |
|---|---|---|
| Prefab | `Assets/Prefabs/Parts/`（共通部品）、`Assets/Prefabs/Screens/<機能>/`（画面） | 7.1 のとおり。画面の Prefab の中に部品の Prefab をネストして置く |
| 生成スクリプト | `Scripts/Editor/Prefabs/` | 部品・画面ごとに 1 クラス。共通の補助（`PrefabBuildUtil`）を使う |
| View | `Presentation/<機能>/` | Prefab のルートに付く `PageBase`／`ModalBase` の派生。表示と入力の受け取りだけ。ロジックを持たない |
| Presenter | 同上 | View と `Application` のユースケースをつなぐ（基盤の `IPresenter`。コンストラクターで具象の View を受け、テスト用に `IXxxView` を受ける 2 つ目のコンストラクターを持つ） |
| ユースケース | `Application/<機能>/` | 通信は `I<機能>Port` 越し。`Presentation` は `Infrastructure` に触れない |
| 組み立て | 各シーンの `LifetimeScope`（8章） | シーンの `UiRoot` と、そのシーンの入口・補助を登録する。**Presenter はどのスコープにも登録しない**（Presenter の `[AssetAddress]` で、基盤の `IScreenNavigator` が Push のたびにシーンのスコープの子スコープで解決する）。ユースケース・ゲートウェイは Bootstrap のルートのスコープ |

- **遷移**: ホーム（またはシーンの最初のページ）を底にする。局面ごとの切り替えは、いまのページを `Pop` してから次を `Push`（`Replace` は作らない: USN の `stack: false` は「積むページ自身を履歴に残さない」意味で、前のページを消すものではない）。ホームへ戻るときは上のページ・モーダルを全部 `Pop`
- **Presenter の作法**: 購読は `InitializeAsync`、解除は `Dispose`。実行中フラグは `finally` で戻す。テストが await できるよう、処理は公開の `UniTask` メソッドにする
- **命名**: Prefab 名 = View の型名（`HomePage`）。子の GameObject は役割の PascalCase（`BackButton`、`TitleText`、`ListContent`）。部品は `Uta` 接頭辞（`UtaButton`・`UtaLabel`）

### 7.5 通信・状態・マスタの扱い [Uta案]

- **所持データのキャッシュ**: `PlayerCache.Current`。ガチャ・強化・プロデュースの終了のあとは、変わった量をクライアントで計算せず、サインインで取り直す（`RefreshPlayerUseCase`）。サーバーの確定値と食い違わせないため
- **マスタ**: 画面が使うマスタの値は、`Application` の読み取り専用の IF `IGameMaster` と読み取り用の型で受ける。`Uta.MasterData` の型を `Application`・`Presentation` に出さない
- **スキルカードの表示**: 名前と説明文はマスタに足す [決定。2026-11-03。ユーザーの判断。[計画](../../plans/2026-11-03-masterdata-card-text.md)]。画面は名前・レア度・種類・消費・強化済みの印・説明文を出す。効果の予測値は出さない
- **ブロッカー**: VolTech-MagicOnion の `IApiActivityHandler` を `BusyIndicator` につなぐ（進行中は全面の透明な Image で入力を止める）。画面は個別に包まない
- **エラー**: 業務エラーのコードは `UtaErrors` に `Stay` で登録し、画面が文言を出す。「タイトルへ」「ホームへ」は `IErrorNavigator`（8章の `ISceneNavigator` を呼ぶ）
- **カードの使用可否はサーバーが判定して返す**（6章の `UsableHand`）

### 7.6 プロデュースの画面の流れ [Uta案]

- 局面（`ProducePhase`）が画面を決める。`ProduceSession`（最新の `ProduceStateView` と次の操作番号）を操作のたびに更新し、`ProduceFlow` が局面から画面への対応を持つ: `WeekSelect` → 週の行動選択（授業は別ページ。screen-wireframes のとおり授業・支給はページ）、`CardOffer` → スキルカード獲得（モーダル）、`Lesson`・`Exam` → レッスン・試験、`ExamResult` → 試験結果、`Finished` → プロデュース結果
- 試験が終わった応答の `ExamResult` は、次の局面の前に試験結果の画面で見せる
- `PRODUCE_STALE_OPERATION` は `GetStateAsync` で取り直す。`PRODUCE_SESSION_DISCARDED`・`PRODUCE_NOT_IN_PROGRESS` はホームへ戻る
- 授業・休むの結果は、操作の前後の状態の差で出す。ターン終了時の効果の 1 件ずつの表示は MVP から外す（応答に効果の記録が無い）

### 7.7 ウィンドウ・確かめ方 [Uta案]

- Windows: 初期 540×960、ウィンドウモード、縦向きのみ。比率を 9:16 に保つ補正（`WindowAspectKeeper`）
- 確かめ方は計画ごとに「EditMode のテスト／ Play モード／ Windows の IL2CPP のビルド／手動」のどれかを書く。Editor が開けない環境では、確かめていないと書く

## 8. シーン構成 [決定。2026-11-03。ユーザーの判断。細部は Uta案]

Unity のシーン（`.unity`）は機能の単位で 4 つに分ける。画面（ページ・モーダル）は、シーンの中で `UnityScreenNavigator` が積む（順 7 の画面の計画で使う。この章の時点では各シーンに最小の uGUI を置くだけ）。

| シーン | 役割 | 遷移 |
|---|---|---|
| `Bootstrap` | 起動の起点（Build Settings の先頭）。マスタを読み、アプリ全体で共有するもの（ルートの DI スコープ）を作り、`Title` を読み込む。マスタの読み込みに失敗したら、エラーを表示して止まる（4章） | → `Title` |
| `Title` | 「スタートをタップ」を待つ。タップしたら端末の登録・トークンの発行・サインアップ・サインイン（4章 2〜4）を行い、成功したら `Home` へ。失敗したら状態を表示し、もう一度タップできる | → `Home` |
| `Home` | サインインで受け取った所持データを表示する（最小）。ここから `Produce` へ | → `Produce` |
| `Produce` | プロデュース。この章の時点では空（文字と `Home` へ戻るボタンだけ） | → `Home` |

- **ルートの DI スコープは `Bootstrap` が作る**（`DontDestroyOnLoad`）。通信（`UtaApiClient`）・`PlayerCache`・マスタ（`ICardCatalog`）・共通エラーハンドラー・**すべてのユースケース**・`ISceneNavigator`・`IErrorNavigator` を登録する。`Title`・`Home`・`Produce` は、それぞれのシーンに置いた `LifetimeScope`（Presentation の `TitleLifetimeScope` など）が、ルートを親にして、そのシーンの `UiRoot`・入口と、そのシーンだけの補助（診断など）だけを登録する。Presenter と View は登録しない（Presenter の `[AssetAddress]` で、`IScreenNavigator` が Push のたびにシーンのスコープの子スコープで解決する。7.4）。シーンの読み込みは `LifetimeScope.EnqueueParent(ルート)` の中で行い、子が親を見つけられるようにする（ルートの `LifetimeScope` は VContainer が自分自身を登録するので、`SceneNavigator` がコンストラクターで受け取る）
- **シーンの遷移は `ISceneNavigator`**（Application のポート: `GoToTitleAsync`・`GoToHomeAsync`・`GoToProduceAsync`）。実装は `Bootstrap` に置く（`SceneManager.LoadSceneAsync` を単独（Single）で呼ぶ）。Presenter は `ISceneNavigator` だけを知り、シーンの名前や `SceneManager` を知らない
- **通信エラーの後の遷移**: `ApiCore` に `IErrorNavigator`（VolTech-MagicOnion）を渡す。`GoToTitleAsync` は `Title` の読み込み、`GoToHomeAsync` は `Home` の読み込み、`OpenStoreAsync` は未実装（ログだけ。強制アップデートは実装しないため。[repository-layout.md](repository-layout.md) 10.5）。実装は `ISceneNavigator` を呼ぶだけの小さなクラスで、`Bootstrap` に置く。`ReturnToTitle` で `Title` が読み込み直されると、通信中だった古い Presenter の続きが破棄された View に触らないよう、Presenter は View が破棄済みなら何もしない。診断（下記）のときはナビゲーターを渡さない
- **Editor で Play したときも `Bootstrap` から始める**（`EditorSceneManager.playModeStartScene`）。どのシーンを開いていても、起動の流れが同じになる
- **Home に入ったとき `PlayerCache.Current` が無い**（サインインしていない）場合は、`Title` へ戻す
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

（実装後に仕様とのズレをここに記録する）
