---
title: プロデュースの実装設計（週の進行・乱数・状態・API）
status: draft
updated: 2026-11-01
---

# プロデュースの実装設計（週の進行・乱数・状態・API）

[produce.md](../systems/produce.md)（ゲームの仕様）を、どう作るかを書いた実装設計書。ゲームのルール・値・局面の遷移の正は produce.md、乱数・操作の記録・保存する状態・再現・RPC と DTO の正はこのページ。2026-10-31 に produce.md の 10章と API案を移した（中身は変えていない）。

## 1. 前提 [決定]

- サーバー権威。結果の確定は常にサーバー。クライアントはシードと乱数を持たず、Shared.Domain で使用可否の判定と予測の表示までを行う。乱数が絡む結果は予測しない([ADR 0008](../decisions/0008-shared-domain-and-onion.md))
- 乱数は `xoshiro256**`。`System.Random` は使わない。シードはサーバーが生成する([repository-layout.md](repository-layout.md) 10.3)
- `float` / `double` を使わない([lesson-card.md](../systems/lesson-card.md) 9.6)
- セッションは検索用の列と、状態を丸ごと入れた1列(MessagePack の `bytea`)に分け、バージョン列で楽観ロックをかける([ADR 0010](../decisions/0010-database.md)、[api-design.md](api-design.md) 3章)
- 操作の RPC は局面ごとに分ける(8章)。再送は `x-request-id` + `last_responses` に任せる([api-design.md](api-design.md) 1章、[server-foundation](server/server-foundation.md))
- 同じシードと同じ操作列で、同じ結果を再現できる(完了条件)
- 1ユーザーにつき進行中のセッションは1つ [決定。[api-design.md](api-design.md) 3章]
- 中断(アプリの終了)は期限なしで再開できる。放棄は明示の操作で、報酬なし。**開始したときとアプリのバージョン(または状態の形式のバージョン)が違えば、進行中のセッションを破棄する**(報酬なし) [決定。2026-10-28。[api-design.md](api-design.md) 5章]
- 状態の中身・乱数・操作の記録はこのページが正。セッションの検索用の列と、中断・放棄・破棄の流れは [api-design.md](api-design.md) 3.1・5章が正

以下 2〜7章は [決定]。2026-10-28 に決めた。

## 2. 処理の単位: 入力待ちから入力待ちまで

- サーバーは1回の操作を受けると、**次にプレイヤーの入力が要る地点(入力待ち)まで**を1回で処理し、その状態を保存して返す。
  - 例: カードを使う → 効果の処理 → (使用数が尽きたら)ターン終了 → 次のターン開始・ドロー → 入力待ち
  - 例: 週の行動を選ぶ → サポートイベント → 行動の結果 → スキルカードの選択肢の提示 → 入力待ち
- 保存される状態は、常にどこかの入力待ち。途中再開は「最後の入力待ちから続ける」だけになり、処理の途中を保存する仕組みは要らない。
- 乱数で決まる提示内容(授業の上昇パラメータ、相談の品ぞろえ、カードの選択肢など)は、**提示する前に引いて状態に保存する**。再接続や `GetState` で引き直さない(リセマラの防止)。

## 3. 乱数ストリーム

セッションのシード(マスターシード)から、用途と場面ごとに独立したストリームを作る。

| ストリーム | キー | 使う場面 |
|---|---|---|
| `Scenario` | なし(1本) | 上級の週パターン、中級11週のイベント([produce.md](../systems/produce.md) 3.2・3.4) |
| `Week` | 週番号 | 特別レッスンの判定、授業の上昇パラメータと確率分岐、おでかけのイベント、相談の品ぞろえと割引、支給の育成pt、サポートイベントの発生、クリア報酬のドリンク |
| `Offer` | 週番号 | スキルカードの選択肢の抽選と再抽選([produce.md](../systems/produce.md) 3.5) |
| `Lesson` | 週番号 + 挑戦回数 | 山札のシャッフルと再シャッフル、不調カードの生成位置、ランダムな効果、スキルカードサポート、試験の審査ターン |

- マスターシード: 64 ビット。プロデュース開始時にサーバーが暗号用の乱数(`RandomNumberGenerator`)で生成する([repository-layout.md](repository-layout.md) 10.3)。
- ストリームの初期化: キーとマスターシードから `xoshiro256**` の状態を作る。式は [lesson-domain-types.md](lesson-domain-types.md) 4章が正(実装済み。テストベクタで守る)
- 挑戦回数は **0 始まり**(初回の挑戦が 0、再挑戦のたびに 1 増える)。レッスンは常に 0
- 1本のストリームにしない理由:
  - あるレッスンで引く回数が変わっても(ドロー効果のあるカードを足すなど)、後の週の結果がずれない。自動プレイでのバランス比較と、不具合の切り分けがしやすい
  - 試験の再挑戦は挑戦回数がキーに入るので、山札の並びが前回と変わる(並びを覚えて再挑戦する攻略を防ぐ)
  - 保存するのは、使用中のストリームの状態だけで済む
- `Offer` を `Week` から分けるのは、再抽選の回数(プレイヤーの操作で変わる)が `Week` の引く回数に混ざらないようにするため。今の順番では同じ週で `Offer` の後に `Week` を引く場面はないので、分けなくても結果は変わらないが、行動の効果を足したときに引く順番を考えなくてよいよう、分けたままにする(実装済み)

乱数の取り出し方(`Uta.Shared.Domain` に置き、同じコードで固定する):

| 操作 | 方法 |
|---|---|
| `NextInt(n)`(0 以上 n 未満) | 64 ビットの値を引き、`(2^64 − n) mod n` 未満なら引き直す(棄却法)。残りを `mod n`。偏りが出ず、64 ビットの演算だけで済む |
| 確率の判定 | 確率は万分率の整数。`NextInt(10000) < 率`。**判定する場面では、率が 0% や 100% でも1回引く**(マスタの値を変えても、引く回数が変わらないようにする) |
| 範囲 `[min, max]` | `min + NextInt(max − min + 1)` |
| 重み付きの抽選 | 重みの合計 `W` に対して `NextInt(W)`。候補はマスタの ID の昇順に並べて累積する |
| 候補から 1 つ | 候補を ID の昇順に並べ、`NextInt(候補の数)` 番目 |
| シャッフル | Fisher–Yates。末尾から、`i` 番目と `NextInt(i + 1)` 番目を入れ替える |

## 4. 乱数を引く順番

決まった順番で引く。どこで何回引いたかは、状態と操作列から一意に決まる。

**プロデュース開始時**(`Scenario`)

1. 上級なら週パターン(A / B / C)
2. 中級なら11週のイベント

**週の開始時(提示の前)**(`Week`。前の週の処理を終えた同じ要求の中で行う)

1. 特別レッスンの判定(対象のレッスン週だけ。MVP では作らない): 歌唱 → 律動 → 表現の順に、パラメータごとの率(基本発生率+アビリティの合計)で 1 回ずつ、計 3 回引く
2. 授業: 選択肢の順に、上昇するパラメータ
3. おでかけ: イベント
4. 相談: スキルカード4種、ドリンク4種、割引の対象

その週に出ない行動の分は引かない。

**行動を選んだ後**(`Week`)

1. サポートイベント(行動の前。MVP では作らない): 編成の順に、各カードの 1 つ目のイベント(SR 以上)の発生判定を 1 回ずつ
2. 行動の確率分岐(授業の「40%で110」など)、支給の育成pt
3. サポートイベント(行動の後。MVP では作らない): 編成の順に、各カードの解放済みの行動後のイベントを、イベントの順に 1 回ずつ判定
4. スキルカードの選択肢(`Offer`): 枠の順に、レアリティを重みで引き、そのレアリティの候補から1枚を引く(カード ID の昇順に並べて `NextInt(候補の数)`)。同じ選択肢の中での重複と、除去したカードと、デッキにすでにある重複不可(lesson-card.md 14章の「重」)のカードは候補から外す。再抽選は同じストリームの続きから引く

**レッスン・試験の開始時**(`Lesson`。[lesson-card.md](../systems/lesson-card.md) 10章の L1・L2)

1. 試験なら審査ターン: 先頭のターンの判定(どの型でも 1 回。[produce.md](../systems/produce.md) 5.1)→ 最後の3ターンと先頭を除いた残りのターンをシャッフル
2. L1 の「レッスン開始時」効果の中の乱数(効果の記載順)
3. L2: 開始時手札のカードが5枚を超えるなら、そのカードの組をインスタンス ID の昇順に並べてからシャッフルして、先頭5枚を手札へ
4. L2: 残りのカード(超えた分を含む)を、カードのインスタンス ID の昇順に並べてからシャッフルし、山札にする

**ターン中**(`Lesson`。lesson-card.md 10章の順)

- T3・T5・T7・P6・E2 などの効果の中の乱数は、効果を処理する順に引く
- T4 のドローや効果のドローで山札が空になったら、その時点で捨札をシャッフルする(捨札は入った順に並べてからシャッフル)
- 山札のランダムな位置への生成は `NextInt(山札の枚数 + 1)`(0 が一番上)
- スキルカードサポート(次点。MVP では作らない): T4 の直後に、編成の順で発生判定 → 発生したら対象の手札を `NextInt(対象の枚数)` で選ぶ。タイミングは仮

**レッスンの後**: クリア報酬のドリンク(`Week`)→ スキルカードの選択肢(`Offer`)

## 5. 操作の記録

受け付けた操作だけを、受け付けた順に記録する。拒否した操作は記録しない。

| 局面 | 操作 | 引数 | 担当の RPC |
|---|---|---|---|
| 週の選択 | 行動を選ぶ | 行動の枠番号、選択肢の番号(授業・おでかけ)、パラメータ(レッスン) | `ChooseWeekActionAsync` |
| 相談中 | 交換する / 強化する / 削除する / 終える | 品の番号 / カードのインスタンス ID | `ExecuteConsultActionAsync` |
| カードの選択肢 | 選ぶ / 再抽選 / 除去 / 受け取らない | 選択肢の番号 | `ResolveCardOfferAsync` |
| ドリンクの整理 | 捨てる | ドリンクの枠番号(0〜3。3 は受け取ろうとしたドリンク) | `DiscardDrinkAsync` |
| 終了以外の入力待ち | ドリンクを捨てる([produce.md](../systems/produce.md) 3.6 の「いつでも捨てられる」) | ドリンクの枠番号(0〜2) | `DiscardDrinkAsync` |
| 試験の結果 | 再挑戦する / しない | — | `DecideExamRetryAsync` |
| レッスン・試験 | カードを使う | カードのインスタンス ID | `ExecuteLessonActionAsync` |
| レッスン・試験 | ドリンクを使う | ドリンクの枠番号 | `ExecuteLessonActionAsync` |

- **操作の表し方は 1 つにする** [決定。2026-11-01。ユーザーの判断]: `Uta.Shared.Domain` の操作のコマンドの型(レッスンの `LessonCommand` と同じ形で、プロデュースの局面ごとの操作を表す型)を、そのまま記録の 1 件にする。記録だけの `OperationKind` は作らない。DTO(`Uta.Shared.Contracts`)からコマンドへの変換はサーバーの `Api` だけで行い、記録の保存の形(MessagePack)はサーバーの `Infrastructure` で作る
| レッスン・試験 | スキップ | — | `ExecuteLessonActionAsync` |
| 終了以外 | 放棄する | — | `AbandonAsync`(操作の記録には残さない。セッションが終わるため) |

- 局面は 6章の「進行」の局面。**試験の結果**は、再挑戦が残るときの入力待ち(合否にかかわらず。[produce.md](../systems/produce.md) 5.1.1)。**ドリンクの整理**は、ドリンクを3つ持っているときに4つ目を受け取ったときの入力待ちで、4つから1つ捨てるまで先へ進まない [決定。2026-10-31]
- **MVP で受ける操作**は、週の選択・カードの選択肢(選ぶ・受け取らないだけ。再抽選・除去は残り回数 0 なので常に拒否)・試験の結果・レッスン・試験(カードを使う・スキップだけ)・放棄。相談中・ドリンクの整理・ドリンクを使う / 捨てるは MVP の後(`OperationKind` の値は予約して残す)
- 要求の局面が今の局面と違えば拒否する(`PRODUCE_UNEXPECTED_PHASE`)。例外は「ドリンクを捨てる」で、終了以外のどの入力待ちでも受ける(MVP の後)

1件の記録: `操作番号(0 から連番)`、`操作の種類(enum)`、`週番号`、`挑戦回数`、`ターン番号`、`引数(int × 3)`。

操作の種類(enum `OperationKind`)と引数 [決定。2026-10-31]。使わない引数は 0(番号・パラメータを入れる引数で、無いときは -1):

| 値 | 名前 | 引数1 | 引数2 | 引数3 |
|---|---|---|---|---|
| 0 | `Nothing` | | | |
| 1 | `ChooseWeekAction` | 行動の枠番号 | 選択肢の番号(無いとき -1) | パラメータ(`ParamKind`。レッスン以外は、Request の `Param` を見ずに -1 を入れる) |
| 2 | `ConsultExchange` | 品の番号 | | |
| 3 | `ConsultEnhance` | カードのインスタンス ID | | |
| 4 | `ConsultRemove` | カードのインスタンス ID | | |
| 5 | `ConsultFinish` | | | |
| 6 | `OfferPick` | 選択肢の番号 | | |
| 7 | `OfferReroll` | | | |
| 8 | `OfferRemove` | 選択肢の番号 | | |
| 9 | `OfferDecline` | | | |
| 10 | `DiscardDrink` | ドリンクの枠番号 | | |
| 11 | `ExamRetry` | | | |
| 12 | `ExamNoRetry` | | | |
| 13 | `UseCard` | カードのインスタンス ID | | |
| 14 | `UseDrink` | ドリンクの枠番号 | | |
| 15 | `Skip` | | | |

- **カードはインスタンス ID で指す**。手札の位置では指さない(表示の並びに左右されない)。インスタンス ID はセッションの中だけの連番の `int`(初期デッキは [idol-support.md](../systems/idol-support.md) 1.3 の並びで 1 から、以降は入手順)。セッションの外には出ないので ULID にしない([repository-layout.md](repository-layout.md) 10.1 の「サーバーが発行する ID」には当たらない、という整理)
- **要求には次の操作番号を入れる**。サーバーの番号と違えば拒否し、クライアントは `GetState` で取り直す。同じターンにカードとドリンクを続けて使うと、ターン番号だけでは古い要求と新しい要求を区別できないため([api-design.md](api-design.md) 1章にも反映)
- **古い要求の拒否は操作番号だけで行う** [決定。2026-10-31]。要求は週・ターン・挑戦回数を持たない(サーバーの状態から分かる)。エラーは `PRODUCE_STALE_OPERATION` の 1 つ
- 記録は状態の1列(6章)の中に持つ。1回のプロデュースで数百件・数 KB の見込み(推測)で、ADR 0010 の「1ターンの読み書きが1行」を変えずに済む。調査で SQL から見たくなったら、別テーブルに追記する形に移す

## 6. 保存する状態(途中再開)

状態の1列に入れるもの。検索用の列(ユーザー、状態、週、ターン、開始時のアプリのバージョン、状態の形式のバージョン、楽観ロックのバージョン)は [api-design.md](api-design.md) 3.1 のとおり。

| 区分 | 内容 |
|---|---|
| 形式 | 状態の形式のバージョン |
| 開始時の入力 | セッション ID、ユーザー ID、シナリオ、**育成アイドルとサポートカード(0〜5枚)の開始時点の性能**(育成アイドル: ID・トレーニング・覚醒。サポートカード: カード ID・レベル・上限解放回数。所持IDは持たない)、開始時のプレイヤーLv(カードの `unlockLv` の絞り込みに使う。[produce.md](../systems/produce.md) 3.5)、マスターシード。開始時の体力は最大体力と同じ |
| 乱数 | 使用中のストリームごとに、種類・キーと `xoshiro256**` の状態(64 ビット × 4)、引いた回数(調査用) |
| 進行 | 局面(週の選択 / カードの選択肢 / レッスン / 試験 / 試験の結果 / 終了。入力待ちの種類のこと)、試験の開始直前の状態(再挑戦の戻り先。[produce.md](../systems/produce.md) 5.1.1)、週番号、**挑戦回数**(今の試験の中で 0 始まり。[produce.md](../systems/produce.md) 5.1.1)、次の操作番号、確定した週構成、その週に提示した内容(4章 で引いたもの) |
| 育成 | パラメータ3種、体力・最大体力、デッキ(インスタンス ID・カード ID・強化済みか)、次のインスタンス ID、試験の再挑戦の残り回数、中間試験の結果 |
| レッスン中だけ | `LessonState` の全部の値([lesson-domain-types.md](lesson-domain-types.md) 3章。山札の**並び順**、得たパラメータ(試験ならスコア)、ステータスと印、上昇量増加、発動予約、持続効果と残り回数、付与の順番を含む)と、試験なら審査ターンの列 |
| 記録 | 操作の記録(5章) |

- 上の表は **MVP で持つもの**。MVP の後に足すもの(形式のバージョンを上げて足す。[api-design.md](api-design.md) 3.1): 局面の「相談中」「ドリンクの整理」、相談の残り回数と費用、育成pt、ドリンク(最大3)、育成アイテム(残り回数)、再抽選・除去の残り回数、除去したカード、回数制限のあるアビリティの使用回数
- `Uta.Shared.Domain` が持つのは「開始時の入力」のうちプレイヤーLv と性能、「育成」「進行」「レッスン中だけ」の区分。「開始時の入力」のセッション ID・ユーザー ID(ULID)・マスターシードと「乱数」の区分は、サーバーが `Uta.Shared.Domain` の状態を包んで持つ([repository-layout.md](repository-layout.md) 6.1)
- **乱数の受け渡し** [決定。2026-10-31]: `Uta.Shared.Domain` は乱数をポート `IRandomStreamSource`(`IRandom Get(RandomStreamKey key)`。`Uta.Shared.Domain.Rng` に置く)から受け取る。ドメインは週番号・挑戦回数からキーを作って求め、サーバーがポートを実装する(マスターシードと保存済みのストリームの状態から `RandomStreams.Create` で作り、要求の終わりに状態を保存する)。ドメインはシードを持たない([ADR 0008](../decisions/0008-shared-domain-and-onion.md))。テストと再生では、マスターシードを持つテスト用の実装を渡す

- 開始時の性能を写して持つのは、プロデュース中にトレーニングなどをしても、進行中のセッションの結果を変えないため。
- `GetState` で返さないもの: マスターシード、ストリームの状態、**山札の並び順**(枚数と、インスタンス ID の昇順に並べ替えた中身だけを返す。PRD-08)、まだ提示していない乱数の結果。
- `Uta.Shared.Domain` は MessagePack に依存しない。保存の形はサーバーの `Infrastructure` で、共通の状態の型を包んで作る(ADR 0010 の「影響・リスク」、[repository-layout.md](repository-layout.md) 6.1)。
- **区分の持ち主** [決定。2026-11-01。ユーザーの判断]: 「記録」(操作の記録)と「開始時の入力」のシナリオ ID も `Uta.Shared.Domain` の状態に入れる(再生の入力になるため)。サーバーだけが持つのは「形式」(状態の形式のバージョン)、セッション ID・ユーザー ID・マスターシード、「乱数」の区分
- **状態の写しは平らなスナップショット 1 つ** [決定。2026-11-01。ユーザーの判断]: `Uta.Shared.Domain` に、状態を保存・復元するための平らな型(`ProduceSnapshot`。値と配列だけを持ち、参照を持たない)を 1 つ置き、状態からの作成と状態への復元をドメインに書く。サーバーの保存の形(MessagePack)と DTO は、このスナップショットから作る。手書きの変換の漏れは、サーバーの結合テストで「状態 → 保存 → 復元 → 状態」の往復が同じになることで見つける(比べ方は 7章)

## 7. 再現の確かめ方

- 「開始時の入力+操作の記録」を最初から再生すると、保存した状態と一致する。これを `Uta.Shared.Domain` の単体テストで確かめる([lesson-phases.md](lesson-phases.md) 8章、repository-layout.md 10.4)。
- 再生した状態の比べ方 [決定。2026-10-31]: `Uta.Shared.Domain` の単体テストでは、再生した状態と比べる状態が両方メモリにあるので、**値を項目ごとに比べる**(状態の型に、全部の値を比べる関数を置く)。**状態のハッシュは作らない**。IL2CPP との一致の確認([lesson-phases.md](lesson-phases.md) 8章)も、実機で再生した状態と、.NET で再生した状態の値を項目ごとに比べる(.NET の側の値はテストの期待値として持つ)。状態の比べ方の正はこの節
- **マスタの版・ルールの版は、アプリのバージョンと同じ**とする [決定。2026-10-31]。マスタ(サーバーの値を含む)やルールを変えたら、アプリのバージョンを上げる([repository-layout.md](repository-layout.md) 10.5)。再現を保証するのは、開始時と `app_version` が同じ場合だけ。違えばセッションは破棄される(1章)

## 8. API案 [決定。2026-10-31／型の名前は実装時に変えてよい]

RPC の分け方は [api-design.md](api-design.md) 1章が正。**局面ごとに RPC を1つ**にし、同じ局面の操作は Request の「操作の種類」(enum)で分ける。1つの汎用の「操作」RPC に全部の引数を詰めないのは、DTO の型で局面と引数の組み合わせを縛り、使わない引数が混ざらないようにするため。

```csharp
// shared/Uta.Shared.Contracts に置く。MVP で宣言するもの
public interface IProduceService : IService<IProduceService>
{
    UnaryResult<ProduceStateResponse> StartAsync(StartProduceRequest request);
    UnaryResult<ProduceStateResponse> ChooseWeekActionAsync(ChooseWeekActionRequest request);
    UnaryResult<ProduceStateResponse> ResolveCardOfferAsync(ResolveCardOfferRequest request);
    UnaryResult<ProduceStateResponse> DecideExamRetryAsync(DecideExamRetryRequest request);
    UnaryResult<ProduceStateResponse> ExecuteLessonActionAsync(ExecuteLessonActionRequest request);
    UnaryResult<ProduceStateResponse> GetStateAsync();      // 再開用。[NoSideEffects]
    UnaryResult<ProduceResultResponse> AbandonAsync();      // 進行中のセッションは1つなので引数なし
}
```

- MVP では宣言しない(作るときに足す): `GetRentalCandidatesAsync`、`ExecuteConsultActionAsync`、`DiscardDrinkAsync`。宣言するとサーバーで実装が要るため。RPC は後から足しても、ほかの RPC に影響しない(api-design.md 1章)

Request のフィールド。操作の RPC はすべて、先頭に `OperationNumber` を持つ:

| フィールド | 型 | 内容 |
|---|---|---|
| `OperationNumber` | `int` | 次の操作番号(5章)。サーバーと違えば拒否 |

| Request | フィールド(`OperationNumber` の後) |
|---|---|
| `StartProduceRequest` | `IdolId`(`int`)、`SupportCardUids`(`string[]`、所持ID 0〜5 個を枠の順に。[idol-support.md](../systems/idol-support.md) 2.1・2.2.1)、`RentalCandidateId`(`int`。MVP は 0＝なし)、`ScenarioId`(`int`。難易度はシナリオが持つ)。`OperationNumber` は持たない |
| `ChooseWeekActionRequest` | `SlotIndex`(`int`)、`OptionIndex`(`int`。無いとき -1)、`Param`(`ParamKind`。レッスン以外は無視) |
| `ExecuteConsultActionRequest`(MVP の後) | `Kind`(enum `ConsultActionKind`: `Exchange` / `Enhance` / `Remove` / `Finish`)、`ItemIndex`(`int`。`Exchange` のみ)、`CardInstanceId`(`int`。`Enhance` / `Remove` のみ) |
| `ResolveCardOfferRequest` | `Kind`(enum `CardOfferActionKind`: `Pick` / `Reroll` / `Remove` / `Decline`)、`OfferIndex`(`int`。`Pick` / `Remove` のみ) |
| `DiscardDrinkRequest`(MVP の後) | `SlotIndex`(`int`) |
| `DecideExamRetryRequest` | `Retry`(`bool`) |
| `ExecuteLessonActionRequest` | `Kind`(enum `LessonActionKind`: `UseCard` / `UseDrink` / `Skip`)、`CardInstanceId`(`int`。`UseCard` のみ)、`DrinkSlotIndex`(`int`。`UseDrink` のみ) |

- 使わないフィールドは 0(`OptionIndex` は -1)を送り、サーバーは見ない
- Response [決定。2026-10-31]: `ProduceResultResponse` は `produce_results` の行(終わり方・試験の結果・評価値・ランク・付与した報酬)。`ProduceStateResponse` のフィールド(6章 の「`GetState` で返さないもの」を除いた状態):

| フィールド | 型 | 内容・いつ入るか |
|---|---|---|
| `OperationNumber` | `int` | 次の操作番号 |
| `Phase` | enum `ProducePhase` | `Nothing` / `WeekSelect` / `CardOffer` / `Lesson` / `Exam` / `ExamResult` / `Finished` |
| `Week` | `int` | 週番号 |
| `Singing` / `Rhythm` / `Expression` | `int` | パラメータ |
| `Hp` / `MaxHp` | `int` | 体力(レッスン・試験の間は `LessonState` の値。[produce.md](../systems/produce.md) 4.3) |
| `Deck` | 配列 | インスタンス ID・カード ID・強化済みか |
| `ExamRetryLeft` | `int` | 再挑戦の残り回数 |
| `WeekActions` | 配列 | 週の選択のとき: 枠ごとの行動の種類。休むは最後の枠(選べないときは選べない印) |
| `ClassOptions` | 配列 | 授業の週のとき: 選択肢ごとの `HpCost`・上がるパラメータ・`ParamGain` |
| `CardOffers` | `int[]` | カードの選択肢のとき: カード ID(枠の順) |
| `Lesson` | 型 | レッスン・試験のとき: [lesson-domain-types.md](lesson-domain-types.md) 3章の `LessonState` から山札の並びを除いたもの(手札のインスタンス ID、山札・捨札・除外の中身は ID の昇順)と、試験なら審査ターンの列・スコア・NPC のスコア |
| `ExamResult` | 型 | 試験が終わった操作のレスポンスに入れる: スコア・順位・NPC のスコア・合否。再挑戦が残らず局面「試験の結果」を飛ばしたときも入れる(画面は結果を出してから次の局面に進む。入力は待たない) [2026-11-01] |
| `Result` | `ProduceResultResponse` | 終了の局面に入った操作のレスポンスだけ(ほかは空) |

- 進行中のセッションが無いとき(一度も始めていない・終わった・破棄した・`app_version` か `state_version` が違う)、`GetState` はエラーにせず、`Phase` が `Nothing` で、ほかの項目が空のレスポンスを返す。`PRODUCE_NOT_IN_PROGRESS` を返すのは操作の RPC と `AbandonAsync` だけ [決定。2026-10-31。ユーザーの判断]。結果は終了の局面に入った操作のレスポンスだけで返す。そのレスポンスを受け取る前にアプリが落ちたら、結果の画面は出さずにホームに戻る(報酬は付与済みで、ホームの所持品に反映されている)
- エラーの一覧は [api-design.md](api-design.md) 2章が正。このページの操作で返すのは `PRODUCE_STALE_OPERATION`(操作番号が違う)、`PRODUCE_UNEXPECTED_PHASE`(局面が違う)、`PRODUCE_INVALID_ARGUMENT`(範囲外の番号・ID、使えない操作、`RentalCandidateId` が 0 でない)、`PRODUCE_NOT_IN_PROGRESS`、`PRODUCE_INVALID_FORMATION`(idol-support.md 2.1)、`PRODUCE_ALREADY_IN_PROGRESS`、`PRODUCE_SESSION_DISCARDED`(api-design.md 5章)
- **楽観ロックに失敗したとき**(保存の `UPDATE ... WHERE version = @v` が 0 行) [決定。2026-11-01。ユーザーの判断]: `PRODUCE_STALE_OPERATION` を返す。同じプレイヤーの要求は基盤で直列になるので([api-design.md](api-design.md) 3章)ふつうは起きないが、起きたらクライアントは `GetState` で取り直す(操作番号の食い違いと同じ扱い)

## 未決事項

- スキルカードサポートの判定のタイミング(4章では T4 の直後を仮置き)

## 実装メモ

(実装後に設計とのズレをここに記録する)
