---
title: 画面・遷移・クライアントの層（実装設計書 画面設計編）
status: draft
updated: 2026-11-03
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
4. サインアップ・サインイン [決定。2026-11-01。ユーザーの判断。`SyncAsync` を分けた]: 端末の登録をした直後（初回）は `IPlayerService.SignUpAsync`（プレイヤーの作成と初期付与）を呼び、そのあと `SignInAsync`（所持データの全件取得とバージョン違いのプロデュースの破棄。[api-design.md](api-design.md) 1章・5章）を呼ぶ。保存ありの起動は `SignInAsync` だけ。`SignInAsync` が `PLAYER_NOT_CREATED` を返したら、`SignUpAsync` を呼んでから `SignInAsync` をやり直す [決定。2026-11-01。ユーザーの判断]。プロデュースを破棄したと返ってきたら、ホームでダイアログを出して知らせる。受け取った所持データはクライアントがキャッシュする（`PlayerCache`）。実装の経路は [api-design.md](api-design.md) 1章「クライアントの実装」。診断用の IMGUI（`StartupView`・`ErrorDialogView`）に結果を出すところまでで、画面は順 7 [2026-11-03]
5. ホームへ遷移する

Addressables に移ったあとの、更新確認のタイミングは未決。

## 5. 通信とエラーの画面側の扱い [Uta案]

- VolTech-MagicOnion のクライアント（`ApiCore`: 順番待ち、再認証、進行中通知、エラーハンドラー）を使う（組み立ては `UtaApiClient`。進行中通知とナビゲーターは未接続で、エラーハンドラーは `DefaultCommonErrorHandler` と診断用の `ErrorDialogView`。画面の計画で作り直す。2026-11-03）。画面は `ErrorPolicy` に従って、再試行・ダイアログ・タイトルへ戻る、を出し分ける（VolTech-MagicOnion の設計書「クライアント側」）
- 通信中は入力を受け付けない表示（ブロッカー）を出す。進行中通知の受け口は VolTech-MagicOnion のものを使う
- 通信の失敗後の再送は、同じ `x-request-id` で行う。画面側で独自の重複防止はしない
- **日付の更新（12:00 JST）をまたいだとき** [決定。2026-10-31。ユーザーの判断]: VolTech-MagicOnion は日付が変わったあとの RPC に `DATE_CHANGED` を返し、既定の動作はタイトルへ戻る。MVP はこれを受け入れ、変えない。プロデュース中でも状態はサーバーに残っているので、起動の流れ（4章）からホームの「再開」で続ける（[api-design.md](api-design.md) 1章 `SignInAsync`）。ただし `DATE_CHANGED` を受けたあとのサインインは、全件を取り直す

## 6. レッスン・試験画面とドメインの関係 [決定]

- レッスン・試験の画面は、`Uta.Shared.Domain` の効果エンジンで**使用可否の判定と予測値の表示**をする（PRD-04、PRD-08）。確定はサーバーの結果（[ADR 0008](../decisions/0008-shared-domain-and-onion.md)）
- クライアントはシードを持たないので、ドローなど乱数が絡む結果は予測しない。操作後は、サーバーのレスポンスで画面を確定させる
- 画面（Presenter）は `Uta.Shared.Domain` を直接呼ばず、`Application` のユースケース越しに判定・予測の結果を受け取る
- **MVP のカードの使用可否はサーバーが判定して返す** [決定。2026-11-03。ユーザーの判断]: `LessonDto` に `UsableHand`（`bool[]`。`Hand` と同じ長さ・同じ順。`LessonEngine.CanUse` が Ok のとき true）を足す（[server/produce-server.md](server/produce-server.md) 6章）。`LessonDto` から `LessonState` を復元するには持続効果の定義（`EffectDef`）などが足りず、クライアントだけでは判定できないため。ADR 0008 の「クライアントが判定」からは外れるが、確定はサーバー、表示のための判定もサーバーの値を使う、という向きは同じ。クライアントの予測（効果の見込み）は MVP では出さない（[lesson-domain-types.md](lesson-domain-types.md)）

## 7. MVP の画面の実装方針 [決定。2026-11-03]

MVP の画面（[game-spec.md](../overview/game-spec.md) の「画面」）を作る実装計画（6 本。[game-spec.md](../overview/game-spec.md) の順 7）が共通して従う方針。型のシグネチャは各計画が持ち、このページは「なぜそうするか」と置き場を持つ。

### 7.1 UI はコードで組む [決定。ユーザーの判断]

- 画面・部品は C# で `GameObject` と uGUI（`UnityEngine.UI`）から組み立てる。`.prefab`・`.unity` は作らない（Sonnet が YAML で手書きするのは現実的でないため。見た目は仮でよい）
- `UnityScreenNavigator`（USN。自作のフォーク）の **Presenter 駆動のナビゲーション**（`Runtime/ChinpanGX`: `IScreenNavigator`・`IPresenter`・`[AssetAddress]`・`IScreenWithArgs<TArgs>`・`OverlayContainer`。遷移は待ち行列で直列になる）をそのまま使い、自前のナビゲーターは作らない [決定。2026-11-03。レビューの指摘: 同じ役割の実装が基盤にある]。ページ・モーダルの雛形は、`PreloadedAssetLoader` に「コンポーネントだけを付けた空の `GameObject`」として登録して渡す。雛形が `Instantiate` されたあと、`Page.Initialize()`（モーダルは `Modal.Initialize()`）の中で子の UI を組む（雛形には子を作らない。`Instantiate` は非シリアライズのフィールドを複製しないため）
- 文字は `UnityEngine.UI.Text`（legacy）と OS のフォント（`Font.CreateDynamicFontFromOSFont`）。TextMeshPro とフォントのアセットは使わない（日本語のフォントのアセットを置かないため。仮の見た目）
- 入力は Input System だけが有効（`ProjectSettings` の `activeInputHandler: 1`）なので、`EventSystem` には `InputSystemUIInputModule` を付ける

### 7.2 Presentation の部品と置き場 [Uta案]

| 要素 | 置き場 | 役割 |
|---|---|---|
| 共通部品 | `Presentation/Common/` | `UiTheme`・`UiFactory`（uGUI の部品を作る）・`SafeAreaFitter`・`UiRoot`（`Canvas`・`PageContainer`・`ModalContainer`・ブロッカー）・`PageBase`・`ModalBase`・`Presenter<TView>`・`ScreenNavigator`・`BusyIndicator` |
| 画面 | `Presentation/<機能>/` | `<名前>Page`（`PageBase`。View。ロジックを持たない）、`I<名前>View`（Presenter が見る View の IF）、`<名前>Presenter`（`Presenter<I<名前>View>`。EditMode のテストで偽の View を渡して確かめる） |
| ユースケース | `Application/<機能>/` | `<名前>UseCase`。通信は `I<機能>Port`（Application が持つ IF）越し。`Presentation` は `Infrastructure` に触れない |
| 組み立て | `Bootstrap/` | VContainer のスコープ。Presenter は `Transient`、ユースケース・ゲートウェイ・`PlayerCache` は `Singleton` |

- **Presenter の作り方**: `ScreenNavigator` が USN で画面を出し、VContainer から `TPresenter` を解決して `Bind(view)` を呼ぶ。画面が閉じられたら `Dispose()`。Presenter の入力（遷移先に渡す値）は、`ScreenNavigator` の `configure` コールバックで `Bind` の前に設定する
- **画面の ID**: 雛形のキーは Presenter の `[AssetAddress("<ページの型名>")]`。USN の `pageId` は指定せず自動
- **Presenter の形**: `IPresenter` を実装する普通のクラス。コンストラクターで、読み込まれた `Page`/`Modal` の具象型・`IScreenWithArgs<TArgs>` の引数・ユースケースを受ける（VContainer が子スコープで解決する）。EditMode のテストのため、`IXxxView`（インターフェース）を受ける 2 つ目のコンストラクターも持ち、画面の具象型を受けるほうに `[Inject]` を付ける
- **遷移**: ホームをスタックの底にする。ホームへ戻るときは、ホーム以外のページをすべて `Pop` する。ガチャ・所持一覧などはホームの上に `Push`。プロデュースの局面ごとの画面は、現在のプロデュースの画面を `Pop` して次の画面を `Push` する（`Replace` は作らない: USN の `stack: false` は「積むページ自身を履歴に残さない」意味で、前のページを消すものではないため。戻るボタンで前の局面に戻れてしまうのを避ける。実装は `ProduceFlow`）

### 7.3 通信・状態・マスタの扱い [Uta案]

- **所持データのキャッシュ**: `PlayerCache.Current`（サインインで取った `PlayerSnapshot`）。ガチャ・強化・プロデュースの終了のあとは、変わった量をクライアントで計算せず、**`RefreshPlayerUseCase`（`IPlayerPort.SignInAsync` を呼んで `PlayerCache` を差し替える）で取り直す**。サーバーの確定値と食い違わせないため。サインインはプロデュースの破棄（古いセッション）も行うが、実行中のクライアントのバージョンは変わらないので起きない
- **マスタ**: 画面が使うマスタの値（名前・レア度・プラン・ガチャの価格と重み・シナリオ・サポートカードのレベル表）は、`Application` の読み取り専用の IF `IGameMaster` と読み取り用の型（`IdolInfo` など）で受ける。実装は `Infrastructure` の `MasterGameData`（`MemoryDatabase` から詰め替える）。`Uta.MasterData` の型を `Application`・`Presentation` に出さない。スキルカードは `Uta.Shared.Domain` の `CardCatalog`（`ICardCatalog`）をそのまま使う
- **スキルカードの表示**（MVP）: 名前と説明文はマスタに足す [決定。2026-11-03。ユーザーの判断。`Card.Name`・`CardFace.Description`。[計画](../../plans/2026-11-03-masterdata-card-text.md)]。画面は名前・レア度・種類（`CardType`）・消費（`CostKind` と値）・強化済みの印（`+`）・説明文を出す。効果の予測値は出さない
- **ブロッカー**: VolTech-MagicOnion の `IApiActivityHandler`（進行中の呼び出しが 0→1 で開始、1→0 で終了。冪等性の順番待ち・再認証の再送を含む）を `ApiActivityBridge` で `BusyIndicator` につなぐ。進行中は全面の透明な `Image`（`raycastTarget`）で入力を止める。画面は個別に包まない
- **エラーの遷移先**: `IErrorNavigator` を `UtaErrorNavigator` で実装する。「タイトルへ」は起動の流れ（`StartupFlow`）のやり直し、「ホームへ」はサインインで取り直してホームに戻る、「ストアへ」は何もしない
- **エラー**: `ApiCore` の共通ハンドラーが出すダイアログ（ネットワークの失敗・`DATE_CHANGED` など。[5章](#5-通信とエラーの画面側の扱い-uta案)）に任せる。業務エラー（ガチャの通貨不足・`PRODUCE_STALE_OPERATION` など）は、ゲートウェイが `ApiResult` の失敗をユースケースの結果の型（成功／業務エラー／その他）に直し、Presenter が文言を選んで `DialogService` で出す。診断用だった IMGUI の `ErrorDialogView`・`StartupView` は uGUI に置き換える
- **起動の診断モード**: `-uta-quit-after-check` を付けたときだけ、いまの診断（`unity7-evaluation.md` 3.1。通信の疎通・再認証の再送・サーバーのエラーの受け取り）を画面なしで実行して終了コードを返す（`StartupDiagnostics`）。付けないときは通常の起動（画面）

### 7.4 プロデュースの画面の流れ [Uta案]

- 局面（`ProducePhase`）が画面を決める。`ProduceSession`（`Application`。最新の `ProduceStateView` と次の操作番号を持つ）が操作のたびに更新され、`ProduceFlow`（`Presentation`）が局面から画面への対応を持つ: `WeekSelect` → 週の行動選択（授業は選択肢を別のページ（授業のページ）に出す。screen-wireframes の共通の方針のとおり授業・支給はページ）、`CardOffer` → スキルカード獲得、`Lesson`・`Exam` → レッスン・試験、`ExamResult` → 試験結果、`Finished` → プロデュース結果
- 操作のレスポンスの `ExamResult` が入っている（再挑戦が残らず `ExamResult` の局面を飛ばした場合を含む）ときは、次の局面の前に試験結果の画面を出す（[produce-domain.md](produce-domain.md) 8章）
- `PRODUCE_STALE_OPERATION` を受けたら `GetStateAsync` で取り直して同じ流れに戻す。`PRODUCE_SESSION_DISCARDED`・`PRODUCE_NOT_IN_PROGRESS` を受けたらホームに戻る（サインインで取り直す）
- 授業・支給・休むの結果（上がった値）は、操作の前後の `ProduceStateView` の差でクライアントが出す（サーバーは差を返さない）

### 7.5 ウィンドウ・確かめ方 [Uta案]

- Windows: 初期 540×960、ウィンドウモード、縦向きのみ。比率を 9:16 に保つ補正を `WindowAspectKeeper`（Windows のスタンドアロンだけ）が行う
- 確かめ方は計画ごとに「EditMode のテスト（`unity` CLI または Test Runner）／ Play モード／ Windows の IL2CPP のビルド／手動」のどれで確かめるかを書く。Unity の Editor を開けない環境では、確かめられていないと計画・報告に書く（推測で「通った」と書かない）

## 未決事項

- 画面の名称・文言（Uta 独自のものにする。[glossary.md](../glossary.md) の仮称を置き換える）
- Addressables に移ったあとの、マスタの更新確認のタイミングと更新中の表示
- 予測値をどの画面要素に出すか（カードの詳細、スコアのプレビューなど）
- ガチャ結果の演出の範囲（MVP は演出なし。足すときに決める。3D ライブは保留。[ADR 0003](../decisions/0003-defer-3d-live.md)）
- 相談の画面の種類（相談を作るときに決める）

（2026-11-01 に解決: 基準解像度・セーフエリア・Windows のウィンドウ、スキルカード獲得・授業・支給・試験結果の画面の種類 → [screen-wireframes.md](../overview/screen-wireframes.md)「共通の方針」・2章の表）

## 実装メモ

（実装後に仕様とのズレをここに記録する）
