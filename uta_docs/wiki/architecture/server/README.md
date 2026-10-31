---
title: architecture/server（サーバーの実装設計）の目次
status: draft
updated: 2026-10-31
---

# architecture/server（サーバーの実装設計）の目次

サーバー（`server/Uta.Server`）の実装設計書。層と依存のルールは [repository-layout.md](../repository-layout.md) 3・4章、RPC・DTO・エラー・テーブルは [api-design.md](../api-design.md)。プロデュースの週の進行は Shared.Domain に置くので [produce-domain.md](../produce-domain.md)（architecture/ の直下）。

- [server-foundation.md](server-foundation.md) — サーバー基盤（VolTech-MagicOnion。.NET 10、MagicOnion + PostgreSQL）：構成、フィルター順序、エラー、ヘッダー、認証・冪等性、トランザクション
- [gacha-support-server.md](gacha-support-server.md) — ガチャ・サポートカードのサーバー処理：抽選の実装、ガチャ・強化の処理の手順（SQL・トランザクション）
