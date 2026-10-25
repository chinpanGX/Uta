---
title: 更新履歴
status: draft
updated: 2026-10-26
---

# 更新履歴

新しいものを上に追記する。1変更1行。形式: `YYYY-MM-DD | 種別 | 内容`
種別: ingest / spec / adr / impl / lint

- 2026-10-26 | adr | ADR 0005 を更新（対象を共通パッケージに拡大、配置先を `external/` に決定）。VolTech-MagicOnion・Supplement・UnityScreenNavigator・MasterMemoryAddons をサブモジュールとして登録。CLAUDE.md の Git 節と構成図、index を更新
- 2026-10-26 | adr | ADR 0006（Unity 7 を早期に触ることを目的に追加）を proposed で起票。CLAUDE.md・project-instructions.md・game-spec.md の目的欄を更新
- 2026-10-26 | adr | ADR 0005 の基盤開発ルールを修正（サブモジュール内でも別 clone でもよい。push 順序は基盤 → Uta）、worktree の注意を追記
- 2026-10-25 | adr | ADR 0005（基盤を Git サブモジュールで取り込む）を proposed で起票
- 2026-10-25 | adr | ADR 0004（サーバーは .NET 10 と C# サーバー基盤を採用）を起票。architecture/server-foundation.md を追加、server/.editorconfig を .NET 10 前提に更新
- 2026-10-25 | impl | .editorconfig を共通（ルート）と client/・shared/（C# 9）・server/（新しい C# 可）に分割
- 2026-10-25 | impl | .gitignore / .gitattributes / .editorconfig を追加、CLAUDE.md に読むファイル表・常に守ること・C#規約・Git規約を追加
- 2026-10-25 | adr | ADR 0003 を accepted に変更。CLAUDE.md・project-instructions.md の目的欄から3Dライブを除外
- 2026-10-25 | spec | glossary に ADR / MVP / 体力 / デッキ・手札 を追加
- 2026-10-25 | spec | systems/produce.md に叩き台（ターン・パラメータ・カード・計算式・乱数・画面ワイヤー）を追加
- 2026-10-25 | spec | live.md を保留扱いにし、game-spec から3Dライブを除外
- 2026-10-25 | adr | ADR 0003（3DライブをMVPから外す）を proposed で起票
- 2026-10-25 | spec | overview/game-spec.md（ゲーム概要仕様）を新規作成、index に登録
- 2026-10-25 | adr | ADR 0001（IP方針）、0002（MagicOnion採用）を起票
- 2026-10-25 | spec | produce / gacha / live の仕様ページ雛形を作成
- 2026-10-25 | ingest | wiki骨格（CLAUDE.md、index、log、glossary）を作成
