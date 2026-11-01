---
title: レッスンの実装フェーズ
status: draft
updated: 2026-11-01
---

# レッスンの実装フェーズ

[lesson-card.md](../systems/lesson-card.md)（レッスンのゲームの仕様）を、どの順番で、どのコードを変えて作るかを書いた実装設計書。カード・効果・計算式・ターン処理順の正は lesson-card.md、実装の順番・フェーズごとに変えるコード・テストの置き場・完了条件の正はこのページ。2026-10-31 に lesson-card.md の 15章を移した（中身は変えていない）。型は [lesson-domain-types.md](lesson-domain-types.md)、enum は [lesson-enums.md](lesson-enums.md)。

仕組みは最初から全ステータス分を用意し、カードとして動かす範囲を段階的に広げる。順番: 好印象 → 元気 → 好調 → 集中 → 試験 → 育成アイテム・ドリンク。この章は範囲の定義だけを書き、進み具合は実装メモと `uta_docs/plans/` に書く(フェーズ 0〜4・6 は実装済み。フェーズ 5 は未着手。[2026-11-01-shared-lesson-phases.md](../../plans/2026-11-01-shared-lesson-phases.md))。

**先に入れるものと後で足すものの基準** [決定。2026-10-31]: enum の値・ステータスの入れ物・式の項は、参考作品の構造で形が決まっているものを先に全部入れる(集中強化・好印象強化・低下状態無効・パラメータ上昇量増加・発動予約を含む)。カードに置いて動かすのはフェーズごと。式の形が決まっていないもの(低下状態の追加の項。[lesson-card.md](../systems/lesson-card.md) 8章)と、細則の多いプリミティブ([lesson-card.md](../systems/lesson-card.md) 7.6)だけは、使うカードを採用するときに足す

**順番の前提**: フェーズ 2 に入る前に、enum の名前を変える実装計画([2026-10-31-lesson-enums-rename.md](../../plans/2026-10-31-lesson-enums-rename.md))を終える。1章 以降の表は新しい名前で書いている

## 1. フェーズ0: 共通基盤(フェーズ1と同時)

- カード定義の読み込み。全プリミティブ・コスト・条件をスキーマ上は定義しておく
- ゾーンと移動、ターン処理順の全ステップ
- [lesson-card.md](../systems/lesson-card.md) 8章の全ステータスの入れ物と、[lesson-card.md](../systems/lesson-card.md) 11章の数え方
- [lesson-card.md](../systems/lesson-card.md) 9.2 のパイプライン(未実装の項は「倍率1・加算0」で通す)
- 決定的な乱数、固定小数点の数値型

[決定] 未実装のプリミティブを使うカードは、マスタ読み込み時にエラーにする。

**MVP の 36 枚が使わない `Op` は、どのフェーズにも入れない** [決定。2026-11-01。ユーザーの判断]: `AddParamBonus`・`AddStatusPercent`・`ReplaceHand`・`EnhanceHand`・`CreateCard`・`ScheduleEffect`・`EveryNTurns`。enum には定義しておき([lesson-enums.md](lesson-enums.md) のフェーズの列は「採用時」)、そのカード・アイテムを採用するときに仕様を確かめて実装する。[lesson-card.md](../systems/lesson-card.md) 1章の「含める」(発動予約・レッスン中強化など)は仕様の範囲で、実装の時期は採用のときにするフェーズを進めるときは、`CardDefinitionValidator` の対応する `Op` の一覧に足す。

**フェーズごとに変えるコード** [決定。2026-10-31]: 仕組み(全コスト・全条件・全ステータスと [lesson-card.md](../systems/lesson-card.md) 11章の数え方、`GainParam` の連撃と倍率)はフェーズ 1 で先に入れた。以降のフェーズで変えるのは次だけ。

| フェーズ | 変えるコード |
|---|---|
| 2 | `EffectExecutor` に `GainParamByShield`・`GainParamByMotivation`・`ScaleShield`・`SetShield`・`AddFixedShield`・`RecoverHp` を足し、`CardDefinitionValidator` の対応する一覧に足す。`StatusSet.Add` に低下状態無効の打ち消しを足す(ルールは [lesson-card.md](../systems/lesson-card.md) 8章「低下状態」。付与しようとした量が 1 以上のときだけ見る)。`CardDefinitionValidator` に [lesson-card.md](../systems/lesson-card.md) 10章の L1 の検査を足す |
| 3・4 | カード定義とテストを足すだけ |
| 5 | 別の仕様(6章)を書いてから決める |
| 6 | 試験(7章) |

**テストのカードの置き場** [決定。2026-10-31]: テストのカード定義は `shared/Uta.Shared.Domain.Tests/Lesson/` に、フェーズごとに `Phase<N>Cards` を足す。カードを組み立てる補助(今の `Phase1Cards` の `private` な補助メソッド)は、共通の静的クラス `TestCards` に移し、開始時手札(`startInHand`)も渡せるようにする。全フェーズのカードをまとめて返す `ICardCatalog` の実装 `TestCardCatalog` を 1 つ作り、テストはそれを使う。カード ID は [lesson-card.md](../systems/lesson-card.md) 14章の番号から機械的に振る: マスタの `CardId`(`masterdata/csv/card.csv`)と同じ値にする [決定。2026-11-01。ユーザーの判断。それまでは L = 10 + 番号、S = 30 + 番号]: F は番号そのまま(F01 = 1)、S は 100 + 番号(S01 = 101)、L は 200 + 番号(L01 = 201)。テストだけで使う合成カードは 900 以上。(採番の規則としては定めない。[lesson-card.md](../systems/lesson-card.md) 3.2)。マスタができたら、テストのカードとマスタの値が一致することを確かめるテストを 1 本置く。テストとマスタの二重の定義は、マスタができるまでの暫定。`TestCards`・`TestCardCatalog` のシグネチャは [2026-11-01-shared-lesson-phases.md](../../plans/2026-11-01-shared-lesson-phases.md) 3.1 で決め、実装した。テストの ID は 2026-11-01 にマスタの値に合わせた(一致テストは別の計画で作る)

**カードのない op の確かめ方** [決定。2026-10-31]: 初期カードセットで使われない op・コスト(`AddFixedShield`・`RecoverHp`・`SetShield`、消費体力増加の付与、好調・絶好調・集中のコストなど)は、テストだけの合成カード(ID 900 以上)で op ごとに単体テストする。F08(不調カード)と `CreateCard` はどのフェーズにも入れない(後回し)。

## 2. フェーズ1: 好印象(理論)

| 区分 | 範囲 |
|---|---|
| ステータス | 好印象、元気(肩代わりのみ)、消費体力減少、消費体力削減、使用数追加 |
| プリミティブ | GainParam、GainParamByImpression、AddShield、AddStatus、DrawCards、AddCardUses、AddPersistent(トリガーは `EveryNTurns` を除く全部。カードで使うのは使用時) |
| コスト | 全種類(仕組みが同じなので先に入れた。カードで使うのは consume、hpDirect) |
| カード | F01〜F06、L01、L02、L04、L07、L10、L14、L15 |

## 3. フェーズ2: 元気(理論)

| 区分 | 追加する範囲 |
|---|---|
| ステータス | やる気、元気増加無効、消費体力増加 |
| プリミティブ | GainParamByShield、GainParamByMotivation、ScaleShield、SetShield、AddFixedShield、RecoverHp |
| カード | F07、L03、L05、L06、L08、L09、L11、L12、L13 |

## 4. フェーズ3: 好調(感性)

| 区分 | 追加する範囲 |
|---|---|
| ステータス | 好調、絶好調 |
| プリミティブ | GainParam の goodScale |
| コスト | goodConditionTurns、excellentTurns |
| カード | S01、S04、S06、S07、S11 |

## 5. フェーズ4: 集中(感性)

| 区分 | 追加する範囲 |
|---|---|
| ステータス | 集中 |
| プリミティブ | GainParam の hits、focusScale |
| コスト | focus |
| カード | S02、S03、S05、S08、S09、S10、S12、S13、S14 |

好調×集中の組み合わせで [lesson-card.md](../systems/lesson-card.md) 9.2 の式全体を検証して完了とする。

フェーズ 3・4 の「コスト」の行(好調・絶好調・集中のコスト)は、フェーズ 1 で実装済み。使うカードがないので、合成カードで確かめる(1章)。

## 6. フェーズ5: 育成アイテム・ドリンク

| 区分 | 範囲 |
|---|---|
| ドリンク | ルールは [produce.md](../systems/produce.md) 3.6 |
| 育成アイテム | ルールは [produce.md](../systems/produce.md) 3.7 |

- MVP では作らない([game-spec.md](../overview/game-spec.md))。ドリンク・育成アイテムのマスタ、状態の持ち方、処理順(P1〜P8 のどこを通るか、使用時・使用後のトリガーに反応するか)、「レッスン内N回」の数え方を別の節に書いてから、実装計画に入る

## 7. フェーズ6: 試験

| 区分 | 範囲 |
|---|---|
| 計算 | [lesson-card.md](../systems/lesson-card.md) 9.5 のスコア、[produce.md](../systems/produce.md) 5.2 の式は週の進行が計算し、`ExamTurn` の最終スコアボーナスで渡す(信頼度は 0 固定) |
| 入力 | `ExamSetup`([lesson-domain-types.md](lesson-domain-types.md) 2章)。審査ターンの列は外から渡されたものをそのまま使う。列を作る処理(先頭の判定・シャッフル)は週の進行の実装で行う([produce.md](../systems/produce.md) 5.1、[produce-domain.md](produce-domain.md) 4章) |
| 出力 | `LessonResult`([lesson-card.md](../systems/lesson-card.md) 10.1) |
| 終了 | 規定ターン数、中間試験の早期終了([lesson-card.md](../systems/lesson-card.md) 9.5) |

## 8. 完了条件 [決定。2026-10-31]

- そのフェーズの全カードについて、[lesson-card.md](../systems/lesson-card.md) 9章の式から出した期待値のユニットテストが通る。期待値は、初期状態 → 操作 → 期待値の具体例を実装計画に 3〜5 個書き、計算の途中も計画に書く(実装と同じ丸め方を実装担当が選ばないようにする)
- 同じシードと操作列で、再生した状態が一致する(`Uta.Shared.Domain` の単体テスト。比べ方は [produce-domain.md](produce-domain.md) 7章: 値を項目ごとに比べる。比べる関数は `Uta.Shared.Domain` の `LessonStateComparer` に置き、.NET のテストと IL2CPP の確認で同じものを使う。`LessonEngineTests` の `Summarize` はそれに置き換えた(2026-10-31 に「テストの共通クラス」から直した)。含める項目は `LessonState` の全部(`NextGrantOrder` などの内部の値を含む))
- IL2CPP との一致は、実機ビルドのときに 1 回確かめる: 固定のシードと操作列で再生した状態を .NET 側で求めてテストに埋め込み、**IL2CPP でビルドした実行物**(Android の実機。または Windows の IL2CPP スタンドアロン)で同じ再生を走らせて比べる。Unity Editor は Mono で動くので、IL2CPP の確認にならない。比べ方は [produce-domain.md](produce-domain.md) 7章(項目ごと。ハッシュは作らない)。入口は Unity Test Runner の実機実行。テストの置き場は `client/uta/Assets/Tests/SharedDomainReplay/`(asmdef `Uta.Client.Tests.SharedDomainReplay`。参照は `Uta.Shared.Domain` と Unity Test Framework だけ)。期待値は .NET の単体テストで出した値を定数で書く。CI では回さない(ADR 0008) [決定。2026-11-01。ユーザーの判断]。本番のクライアントはシードを持たない([ADR 0008](../decisions/0008-shared-domain-and-onion.md))。この確認は実機ビルドの手順に入れる([game-spec.md](../overview/game-spec.md) 完了条件 7)

---
