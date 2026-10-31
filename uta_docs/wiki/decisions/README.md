---
title: decisions（ADR）の目次
status: draft
updated: 2026-10-31
---

# decisions（ADR）の目次

技術選定・方針の決定記録。**決定は ADR が正**（CLAUDE.md）。

## 決まり

- 新しい ADR は [0000-template.md](0000-template.md) を写して書く。番号は最後の番号の次。ファイル名は `NNNN-件名.md`
- status は proposed / accepted / superseded。accepted の ADR は書き換えない。方針を変えるときは、新しい ADR を起こし、古い ADR を superseded にして、置き換えた ADR へのリンクを足す
- 足したら、この一覧に 1 行足し、[log.md](../log.md) に 1 行足す。一覧に書いた status と本体の status を合わせる（Lint が点検する）

## 一覧

- [0000-template.md](0000-template.md) — ADRテンプレート
- [0001-ip-policy.md](0001-ip-policy.md) — IP方針：システムのみ模倣、素材・名称はオリジナル
- [0002-magiconion.md](0002-magiconion.md) — 通信方式：MagicOnion（gRPC）を採用
- [0003-defer-3d-live.md](0003-defer-3d-live.md) — 3DライブをMVPから外す（accepted）
- [0004-server-foundation.md](0004-server-foundation.md) — サーバーは .NET 10 と C# サーバー基盤を採用（accepted）
- [0005-foundation-submodule.md](0005-foundation-submodule.md) — 基盤と自作パッケージを `external/` に Git サブモジュールで取り込む（proposed。4 件登録済み）
- [0006-adopt-unity-7-early.md](0006-adopt-unity-7-early.md) — Unity 7 を早期に触ることを目的に加える。Unity のバージョンは Unity 7 で確定（accepted）
- [0007-masterdata-with-addons.md](0007-masterdata-with-addons.md) — マスタを MasterMemoryAddons でクライアント用・サーバー用に出し分ける。列の型は int/long/string/bool/enum のみ、masterdata/ は最上位、CSV が正本（accepted）
- [0008-shared-domain-and-onion.md](0008-shared-domain-and-onion.md) — ドメイン層を共有し、クライアントはオニオンアーキテクチャにする。クライアントは判定・予測表示まで、シードは持たない（accepted）
- [0009-client-stack.md](0009-client-stack.md) — クライアントは uGUI + UnityScreenNavigator・VContainer・UniTask・縦画面（accepted）
- [0010-database.md](0010-database.md) — DB は PostgreSQL + Npgsql + Dapper、セッションは状態を1列にまとめて保存（accepted）
- [0011-deploy-google-cloud.md](0011-deploy-google-cloud.md) — API サーバーをコンテナにして Google Cloud（Cloud Run + Cloud SQL）にデプロイする（accepted）
- [0012-migration-dbup.md](0012-migration-dbup.md) — マイグレーションは DbUp、API とは別の `migrate` コマンド（dev は Cloud Run のジョブ）で流す（accepted）
- [0013-commit-masterdata-binary.md](0013-commit-masterdata-binary.md) — マスタのバイナリもコミットする。ADR 0007 の「バイナリはコミットしない」を置き換え（accepted）
- [0014-design-heavy-sonnet-implementation.md](0014-design-heavy-sonnet-implementation.md) — 設計は厚く書き、実装は Sonnet のサブエージェントに任せる。仕様と実装の間に実装計画（`uta_docs/plans/`）を置く（superseded。ADR 0017 で置き換え）
- [0015-reference-card-values.md](0015-reference-card-values.md) — 初期カードセットの数値は参考作品の値を使う（superseded。ADR 0016 で置き換え）
- [0016-reference-game-values.md](0016-reference-game-values.md) — ゲームシステムの数値（カード・週構成・排出率・強化量など）は参考作品の値を使う。ADR 0015 を置き換え（accepted）
- [0017-spec-design-plan-stages.md](0017-spec-design-plan-stages.md) — ゲームの仕様・実装設計書・実装計画の 3 段に分け、レビューは実装設計書と計画で回す。ADR 0014 を置き換え（accepted）
