---
title: 画面・遷移・クライアントの層（実装設計書 画面設計編）
status: draft
updated: 2026-10-27
---

# 画面・遷移・クライアントの層（実装設計書 画面設計編）

実装設計書（4編）の4編目。クライアントの Presentation 層にあたる、画面・遷移・起動フローを定める。前提は [repository-layout.md](repository-layout.md)。

- 各画面は、呼ぶ API（[api-design.md](api-design.md)）と、使うマスタ（[masterdata-pipeline.md](masterdata-pipeline.md)）に機能ID付きで対応づける
- 画面のワイヤー・見た目はこのページには書かない。[produce.md](../systems/produce.md) は旧叩き台のワイヤーを削除済みで、週構成・レッスン画面から描き直す予定。**画面名・文言は仮称**（[ADR 0001](../decisions/0001-ip-policy.md)）

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
- クライアントは `Uta.Domain` を、使用可否の判定と予測の表示まで使う。シードは持たない [決定。[ADR 0008](../decisions/0008-shared-domain-and-onion.md)]

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
| 起動・ロード | ページ | — | IssueToken、（マスタ取得） | マスタのバイナリ |
| ホーム | ページ | — | GetPlayer | — |
| ガチャ（一覧） | ページ | GAC-01 | GetGachas | ガチャ、排出率 |
| ガチャ（排出率の表示） | モーダル | GAC-02 | — | 排出率（マスタから生成） |
| ガチャ（結果） | ページ／モーダル | GAC-01、GAC-04 | Draw | 育成アイドル、サポートカード |
| 交換 | ページ | GAC-03 | Exchange | 交換pt の設定 |
| 所持一覧・詳細 | ページ | IDL-01、SUP-01 | GetPlayer | 育成アイドル、サポートカード |
| 育成アイドルの強化 | ページ | IDL-02 | Train、Awaken | トレーニング・覚醒の設定 |
| サポートカードの強化 | ページ | SUP-02 | LevelUp、LimitBreak | 同上 |
| 編成 | ページ | PRD-01、SUP-06 | GetRentalCandidates | アイドル、サポートカード |
| 週の行動選択 | ページ | PRD-02 | ChooseWeekAction、GetState | 週構成、行動 |
| レッスン | ページ | PRD-03〜08 | ExecuteLessonAction | スキルカード、レッスンルール |
| 試験 | ページ | PRD-06、PRD-07 | ExecuteLessonAction | 試験の設定、スコアボーナス |
| プロデュース結果 | ページ | PRD-13 | ExecuteLessonAction（最終試験の終了時）・GetState | 評価の設定 |
| 設定・お知らせ | ページ／モーダル | — | — | — |

- 「次点」「後回し」の機能（PRD-09〜12、PRD-14、PRD-15、GAC-05）の画面は、その機能を実装するときに足す
- 山札・捨札の確認（PRD-08）は、レッスン中に開くモーダルとして持つ案

## 3. 画面遷移 [Uta案]

```
起動・ロード ─▶ ホーム ─┬─▶ ガチャ ─▶ ガチャ結果 ─▶ ガチャ
                        │        └─▶ 交換
                        ├─▶ 所持一覧 ─▶ 詳細 ─▶ 強化
                        └─▶ 編成 ─▶ 週の行動選択 ─┬─▶ レッスン ─┐
                                      ▲           └─▶ 試験    ─┤
                                      └──────────────────────┘
                                      （最終試験の後）─▶ プロデュース結果 ─▶ ホーム
```

- 進行中のプロデュースがある状態でアプリを起動したら、ホームから**再開**できる（`GetState` で復元。PRD の「中断・再開」）
- 試験の再挑戦（PRD-06）は、試験画面から再び同じ試験へ

## 4. 起動フロー [Uta案・推測]

`Bootstrap` が行う。詳細は [server-foundation.md](server-foundation.md) と [masterdata-pipeline.md](masterdata-pipeline.md) 6章。

1. 認証: 端末の登録・トークンの発行（`IDeviceService`）。無効・期限切れは1回だけ取り直して再送する（基盤の仕様）
2. サーバーの状態: メンテナンス・強制アップデートの判定（基盤が返すエラー）。画面に告知を出す
3. マスタ: いまはアプリに同梱したバイナリ（StreamingAssets）を `MasterDataBinary.Read` で読む。あとで Addressables によるダウンロードに移る（[masterdata-pipeline.md](masterdata-pipeline.md) 6章）。`SchemaMismatch` はアプリの更新を促す
4. ホームへ遷移する

Addressables に移ったあとの、更新確認のタイミングは未決。

## 5. 通信とエラーの画面側の扱い [Uta案]

- 基盤のクライアント側（`ApiCore`: 順番待ち、再認証、進行中通知、エラーハンドラー）を使う。画面は `ErrorPolicy` に従って、再試行・ダイアログ・タイトルへ戻る、を出し分ける（基盤の設計書「クライアント側」）
- 通信中は入力を受け付けない表示（ブロッカー）を出す。進行中通知の受け口は基盤のものを使う
- 通信の失敗後の再送は、同じ `x-request-id` で行う。画面側で独自の重複防止はしない

## 6. レッスン・試験画面とドメインの関係 [決定]

- レッスン・試験の画面は、`Uta.Domain` の効果エンジンで**使用可否の判定と予測値の表示**をする（PRD-04、PRD-08）。確定はサーバーの結果（[ADR 0008](../decisions/0008-shared-domain-and-onion.md)）
- クライアントはシードを持たないので、ドローなど乱数が絡む結果は予測しない。操作後は、サーバーのレスポンスで画面を確定させる
- 画面（Presenter）は `Uta.Domain` を直接呼ばず、`Application` のユースケース越しに判定・予測の結果を受け取る

## 未決事項

- 縦向きでの基準解像度と、セーフエリア・Windows のウィンドウサイズの扱い
- 画面の名称・文言（Uta 独自のものにする。[glossary.md](../glossary.md) の仮称を置き換える）
- Addressables に移ったあとの、マスタの更新確認のタイミングと更新中の表示
- 予測値をどの画面要素に出すか（カードの詳細、スコアのプレビューなど）
- ガチャ結果の演出の範囲（3D ライブは保留。[ADR 0003](../decisions/0003-defer-3d-live.md)）
- 画面ワイヤーの作成（週構成・レッスン・試験から）

## 実装メモ

（実装後に仕様とのズレをここに記録する）
