---
title: 更新履歴
status: draft
updated: 2026-10-31
---

# 更新履歴

新しいものを上に追記する。1変更1行。形式: `YYYY-MM-DD | 種別 | 内容`
種別: ingest / spec / adr / impl / lint

- 2026-10-31 | lint | wiki の機械的な点検のスキル linting-wiki（lint_wiki.py）を追加。main で実行してエラー 0 件（警告はサブモジュール未初期化と未作成の masterdata/ など）
- 2026-10-31 | adr | ADR 0014（設計は厚く書き、実装は Sonnet のサブエージェントに任せる）を proposed で起票。実装計画の置き場 `uta_docs/plans/` と書式（plans/README.md）、設計書レビューのスキル requesting-design-review を追加。main へのマージで ADR 0012（DbUp）・0013 と番号が重なったため 0014 に改番
- 2026-10-31 | lint | claude/uta-design-continuation（10/28 の未決事項の決定、ADR 0012・0013）を main にマージ。同じ日に main で決めた内容と食い違う3点は main を正にした（テストは `shared/Uta.Domain.Tests`、乱数は用途・場面ごとのストリーム、操作の記録は状態の1列の中で `produce_actions` テーブルは作らない）。produce.md 10章に「1ユーザー1セッション」「アプリのバージョンが違えば破棄」を反映し、10.7 から外した。api-design.md 3.1 を produce.md 10章に合わせ、repository-layout.md・CLAUDE.md の `tests/` を削除
- 2026-10-31 | impl | external/VolTech-MagicOnion を a65980e に更新（RPC ごとの設定を、共有インターフェース（`ServiceBase<T>` の `T`）の属性だけから読む。`[SkipAuth]` はクラスに付けられず、冪等性の対象にもならない。予約エラーの既定の action を基盤の仕様に表で置く）
- 2026-10-29 | spec | overview/screen-wireframes.md（ホームと10画面のワイヤー叩き台）を新規作成。ADR 0008・0009、produce.md 10章、screen-design.md に合わせた。produce.md の未決事項と screen-design.md（ワイヤーへの参照、画面一覧に未掲載の4画面を [Uta案] で追加）を更新、index に登録
- 2026-10-28 | impl | `shared/Uta.Domain`（netstandard2.1・C# 9）にレッスンのフェーズ0・1（カード定義・状態・乱数・計算式・エンジン）を実装し、`shared/Uta.Domain.Tests`（xunit.v3 4.0.2、49件）を追加。architecture/lesson-domain-types.md を新規作成し index に登録。ルートに global.json、CLAUDE.md にビルド・テストのコマンドを追記、repository-layout.md 10.4 にテストの置き場を反映
- 2026-10-28 | spec | produce.md 10章の案を決定に変更。要求に「次の操作番号」を足す（api-design.md 1・5章）。乱数ストリームの作り方を repository-layout.md 10.3、新しい用語を glossary に反映
- 2026-10-28 | lint | 調査反映ページを点検して修正（lesson-card: スコアボーナスの参照先 5.2・最終スコアボーナス、中間試験の早期終了は初級・中級のみ、カード ID を int に、同タイミングの順にサポートアビリティを追加、シードの粒度を produce.md 10章に揃える。produce: 完了条件を ADR 0008 に合わせる、決定済みの未決事項を削除、API案を RPC の分割に合わせる、NPC スコアは切り捨て。gacha: DB を ADR 0010 に。updated を修正）。参考作品と同じ仮称を glossary に「置き換え対象」として記録
- 2026-10-28 | spec | produce.md 10章を書き換え、サーバー検証の土台の案（入力待ち単位の処理、用途・場面ごとの乱数ストリームと引く順番、操作の記録形式、途中再開で保存する状態）を [Uta案] で追加
- 2026-10-28 | adr | ADR 0013（マスタのバイナリもコミットする）を accepted で起票し、ADR 0007 に置き換えの注記を追加。masterdata-pipeline.md 5章（生成は CSV か定義を変えたときだけ）・8章（アドオンの改善2件: 中身が同じなら書き換えない、`--check` でバイナリの古さを検出）、repository-layout.md 7章、index を更新。バイナリを無視する `.gitignore` は足さない
- 2026-10-28 | adr | ADR 0012（マイグレーションは DbUp、API とは別の `migrate` コマンドで流す）を accepted で起票。ADR 0011 に 11.4 の決定を追記、ADR 0002 に「マスタ型の置き場は ADR 0007 で置き換え」の注記を追加。index・CLAUDE.md を更新
- 2026-10-28 | spec | 未決事項を推奨案で決定。api-design.md を更新（RPC 名の確定と `SyncAsync` の追加、エラーは domain `uta`＋機能の接頭辞の reason に変更（基盤の `ErrorCode` が数値を持たないため番号帯をやめた）、テーブル一覧、セッションの状態の中身・`state_version`・`produce_actions`、最終評価は `Uta.Domain`、中断・放棄・**アプリのバージョンが違えば破棄**）。repository-layout.md（ULID の持ち方、`tests/`、`noEngineReferences`、NetArchTest、11.4 のデプロイ構成、8章に要検証3件）、masterdata-pipeline.md（CSV を直接編集、`gen.ps1`）、produce.md（6.1 報酬）、gacha.md（8章 通貨の入手と変換）、game-spec.md（認証は匿名のみ）、screen-design.md（起動フローに同期）、glossary を更新
- 2026-10-28 | spec | 効果の `Op` 一覧を決定。効果・条件はグループ（`EffectGroup`/`Effect`、`ConditionGroup`/`Condition`）にまとめてカードから指す、対象のステータスと発動タイミングは型付きの enum 列（`Status`/`Trigger`）、倍率・回数の `Arg` は 0＝既定値。masterdata-pipeline.md 3章・3.1、lesson-card.md 6.1・7章、glossary に反映（ADR 0007 の「汎用列＋型付きの参照列」の範囲内なので ADR は変えない）
- 2026-10-28 | spec | CI は最初は手動で流し、手順が固まったら GitHub Actions で自動化する、に決定。repository-layout.md 11.2 に追記して 11.4 の未決から外し、ADR 0011 の「CI での自動化は未決」を更新
- 2026-10-27 | lint | `docs/` を `uta_docs/` にリネーム。CLAUDE.md・README・raw/README・project-instructions・ADR 0001/0006・repository-layout.md の参照を更新（ADR 0003 の記録と過去のログは当時の表記のまま）
- 2026-10-27 | adr | ADR 0011（API をコンテナにして Google Cloud の Cloud Run + Cloud SQL にデプロイ）を accepted で起票。repository-layout.md に11章「環境構成とデプロイ」と8章の要検証2件を追加。index・game-spec・CLAUDE.md・glossary を更新
- 2026-10-27 | spec | クライアントのローカル保存は Supplement の MessagePack 版（`Supplement.MessagePack`）に決定。repository-layout.md 10.6 と 8章（版の整合）に反映
- 2026-10-27 | spec | テストは xUnit v3（最新の安定版）、サーバーの結合テストは Testcontainers に決定。マスタ ID の採番規則は作らない、ローカル保存の対象は実装時に決める、を repository-layout.md 10章に反映
- 2026-10-27 | spec | マスタの日時を `string`（`yyyy-MM-dd HH:mm:ss`、JST として解釈）に決定。検証・スプレッドシートの注意・アドオンの改善案を repository-layout.md 10.2・masterdata-pipeline.md 3章・8章に反映
- 2026-10-27 | spec | 共通の規約を repository-layout.md 10章に追加（ID: マスタは int・サーバーは ULID、時刻: サーバーが正・JST 固定・日付更新は 12 時、乱数: xoshiro256**、テスト: 単体は専用 dll・サーバーは結合テスト・Unity 側は必須にしない、強制アップデート: アプリのバージョン、ローカル保存: Supplement）。lesson-card・api-design・masterdata-pipeline・ADR 0008・glossary・CLAUDE.md に反映
- 2026-10-27 | lint | 会話で決めたが未反映だった内容を反映。repository-layout.md（層・制約・固有の知識の置き場を [決定] に、古いクライアントとのずれの扱い、8章「実装前に確かめること」を追加）、masterdata-pipeline.md（8章「アドオンの改善バックログ」を追加）、game-spec.md（クライアントの役割・技術構成・DB を更新）、glossary.md（技術・設計の用語を追加）
- 2026-10-27 | adr | 未決13件を推奨案で決定。ADR 0007・0008 を accepted に変更し、ADR 0009（クライアント: uGUI + UnityScreenNavigator・VContainer・UniTask・縦画面）・ADR 0010（DB: PostgreSQL + Npgsql + Dapper、セッションの保存形式）を起票
- 2026-10-27 | spec | 13件の決定を設計書4編・CLAUDE.md・project-instructions.md・produce.md 10章・lesson-card.md に反映（ドメイン使用範囲、カード効果の表現、masterdata/ の配置、RPC の分割、エラーコードの番号帯、マッピング、生成物・配信・CSV の運用）
- 2026-10-27 | spec | repository-layout.md に「固有の知識の置き場」（6.1）を追加し、ADR 0008 に反映。サーバーの構成図に `Domain/` を追加
- 2026-10-27 | adr | ADR 0006 を accepted に変更。Unity のバージョンを Unity 7（現在 `7000.0.0a12`）で確定し、「Preview まで Unity 6 系」の記述を ADR 0006・CLAUDE.md・project-instructions.md・game-spec.md・index.md から更新。repository-layout.md の未決事項（バージョンの食い違い）を解消
- 2026-10-27 | adr | ADR 0007（マスタを MasterMemoryAddons で出し分ける。列の型は int/long/string/bool/enum のみ）・ADR 0008（ドメイン層の共有とクライアントのオニオン構成）を proposed で起票
- 2026-10-27 | spec | 実装設計書4編の叩き台を architecture/ に追加（repository-layout / masterdata-pipeline / api-design / screen-design）。index を更新、game-spec の未決事項にリンクを追加
- 2026-10-27 | spec | 参考作品の構造調査を反映。systems/produce.md を書き直し（旧叩き台の12ターン制・数値・画面ワイヤーを廃止し、週構成・レッスン・試験・評価・プレイヤーLvに置き換え）、systems/lesson-card.md・systems/idol-support.md を新規作成、systems/gacha.md を書き直し（課金なし・無償通貨のみ、レンタル枠はサーバーのマスタから）。glossary に仮称を追加、index・game-spec を更新
- 2026-10-27 | ingest | raw/2026-10-26-reference-structure-research.md を追加（名称・出典URLは残さない。ADR 0001）
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
