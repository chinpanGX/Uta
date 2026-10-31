---
title: systems（ゲームの仕様）の目次
status: draft
updated: 2026-10-31
---

# systems（ゲームの仕様）の目次

システムごとのゲームの仕様。**実装設計書（[architecture/](../architecture/README.md)）の一次情報**（[ADR 0017](../decisions/0017-spec-design-plan-stages.md)）。

## 決まり

- **書くこと**: 何が起きるか。ルール、値（参考作品の値を初期値にする。[ADR 0016](../decisions/0016-reference-game-values.md)）、局面の遷移、実行要件（機能ID と優先度）、マスタの表の列（[masterdata-pipeline.md](../architecture/masterdata-pipeline.md) 9章の索引が、列の正をここにしている）
- **書かないこと**: SQL、DTO、型名、層の置き場、乱数の引き方、テストの作り方。これらは architecture/ に書き、ここからはリンクするだけにする
- **変えるのは、ゲームの仕様（遊ぶ人から見える挙動）が変わるときだけ**。実装の都合の変更は architecture/ と plans/ の中で直す
- 用語の定義は [glossary.md](../glossary.md) が正。ページにはリンクだけを書く
- 章立て: 目的・凡例・完了条件・決まっていること・番号つきの章・実行要件・未決事項・実装メモ。印は [確認]（参考作品で確かめた）・[決定]（Uta で決めた）・[Uta案]（提案）
- レビューは整合性の観点だけを、変えたときに 1 回（[requesting-design-review](../../../.claude/skills/requesting-design-review/SKILL.md)）

## 一覧

- [produce.md](produce.md) — プロデュース（育成）：週構成、行動、レッスン・試験の値、試験の結果、スコアボーナス、NPC、最終評価・報酬、プレイヤーLv、局面の遷移（10章）。実装設計は [produce-domain.md](../architecture/produce-domain.md)
- [lesson-card.md](lesson-card.md) — レッスン・試験のカードロジック：カード定義、コスト、条件、効果、計算式、ターン処理順、初期カードセット。実装の順番は [lesson-phases.md](../architecture/lesson-phases.md)
- [idol-support.md](idol-support.md) — 育成アイドル・サポートカード：ステータス、初期デッキ、編成、レベル上げ・上限解放、アビリティ（枠 1）、マスタの表
- [gacha.md](gacha.md) — ガチャ：排出テーブル、天井（交換pt）、重複時の扱い、無償通貨のみ。サーバーの処理は [gacha-support-server.md](../architecture/gacha-support-server.md)
- [live.md](live.md) — 3Dライブ：**保留（MVP対象外）**
