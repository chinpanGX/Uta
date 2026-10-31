# server レイヤーのチェックリスト（`server/`）

ASP.NET Core + MagicOnion（Unary のみ）、.NET 10（C# 14）。DB は PostgreSQL（Npgsql + Dapper）、デプロイ先は Cloud Run + Cloud SQL。
根拠: CLAUDE.md、[ADR 0002](../../../../uta_docs/wiki/decisions/0002-magiconion.md)・[0004](../../../../uta_docs/wiki/decisions/0004-server-foundation.md)・[0008](../../../../uta_docs/wiki/decisions/0008-shared-domain-and-onion.md)・[0010](../../../../uta_docs/wiki/decisions/0010-database.md)・[0011](../../../../uta_docs/wiki/decisions/0011-deploy-google-cloud.md)、[server-foundation.md](../../../../uta_docs/wiki/architecture/server-foundation.md)、[api-design.md](../../../../uta_docs/wiki/architecture/api-design.md)、[repository-layout.md](../../../../uta_docs/wiki/architecture/repository-layout.md) 3〜4章・10〜11章。

## 層と依存

- [ ] `Uta.Server` の中の層（`Api` → `Application` → `Domain`、`Infrastructure`）の依存が内側に向いているか。フォルダと名前空間で分けているので、コンパイルでは守られない
- [ ] アーキテクチャテスト（NetArchTest など）があるなら、新しい依存がそれに引っかからないか。ないなら、違反を目で探す
- [ ] `Api`（MagicOnion のサービス）が薄いか。ルールを `Api` に書かず、`Application` / `Domain` に置いているか
- [ ] サーバー固有の知識（ガチャ抽選、交換pt・天井、シード生成、レンタル候補の決定、報酬付与）が `server/Uta.Server/Domain/` にあり、`shared/` に漏れていないか（6.1）
- [ ] Shared.Domain のルールを `Uta.Domain` から使わず、サーバーで別に書き直していないか（二重実装。ADR 0008）
- [ ] Shared.Domain の型は合成で包んでいるか（継承・`partial` で `Uta.Domain` の型を広げていないか）

## サーバー権威とゲームの正しさ

- [ ] 結果をサーバーで確定しているか。クライアントから送られた値（計算結果、所持数、時刻）を信じていないか
- [ ] リクエストの検証: 所持していないカード・アイテム、範囲外の値、状態と合わない操作を弾いているか
- [ ] 乱数が `xoshiro256**` で、シードはサーバーが生成しているか。`System.Random` を使っていないか。ストリームの作り方・引く順番が `uta_docs/wiki/architecture/produce-domain.md` 3・4章どおりか
- [ ] シードや抽選の内部状態をレスポンス・ログでクライアントに漏らしていないか
- [ ] 時刻はサーバーの時刻で、JST 固定、日付の更新は 12:00 JST か（repository-layout.md 10.2）。`DateTime.Now` を直接使っていないか（テストで差し替えられる時刻の抽象を使うのが望ましい）

## VolTech-MagicOnion の規約

- [ ] 業務エラーは `BusinessException(ErrorCode, …)` を投げるだけにしているか。独自に `RpcException` を作っていないか
- [ ] 消した reason を使い回していないか（古いアプリが `UNKNOWN_ERROR` として扱う）
- [ ] 状態を変える RPC（育成の操作、ガチャ）が**冪等性の対象**になっているか（`x-request-id` + `last_responses`）。仕様側で独自の重複防止を作っていないか
- [ ] 読み取りだけの RPC に `[NoSideEffects]`、認証不要の RPC にだけ `[SkipAuth]` が付いているか
- [ ] 本体が `ITransactions.RunAsync` の中で動き、保存と冪等性の書き込みが同じトランザクションか
- [ ] フィルターの順序を変えようとしていないか（`AddVoltechServer()` が固定する）
- [ ] 想定外の例外の詳細（スタックトレース、SQL）をクライアントに返していないか（`INTERNAL` で返し、詳細はログだけ）

## DB（PostgreSQL + Npgsql + Dapper）

- [ ] SQL がパラメーター化されているか（文字列の連結で値を埋めていないか）
- [ ] プロデュースのセッションが「検索に使う列 + 状態を MessagePack にした `bytea`」の形で、**バージョン列の楽観ロック**をかけているか（ADR 0010）。競合時の扱いがあるか
- [ ] N+1 になっていないか。1リクエストのクエリ数が妥当か
- [ ] スキーマの変更にマイグレーションがあるか。既存のデータを壊さないか（戻す必要があるか）
- [ ] セッションの状態の型を変えたとき、保存済みの MessagePack を読めるか（互換性）
- [ ] ID の発行が ULID か（マスタの ID は `int`）

## 非同期・性能

- [ ] `async` の途中で `.Result` / `.Wait()` で止めていないか
- [ ] `CancellationToken` を DB 呼び出しまで渡しているか
- [ ] ホットパスで不要な割り当て・LINQ がないか（Cloud Run は最小 0 台なので起動の重さも見る）

## 設定・秘密・デプロイ

- [ ] 接続文字列・鍵・パスワードがコード・`appsettings*.json` に入っていないか（CLAUDE.md。Secret Manager などは未決）
- [ ] コンテナ発行（`dotnet publish /t:PublishContainer`）で動く前提を壊していないか（ファイルのパス、ポート、HTTP/2 の h2c）
- [ ] サーバー用のマスタのバイナリをイメージに含める前提を壊していないか
- [ ] ログに個人の情報やトークンを出していないか

## テスト

- [ ] 結合テスト（Testcontainers で PostgreSQL を立てる）で、RPC から結果・保存まで確かめているか（repository-layout.md 10.4）
- [ ] フレームワークが xUnit v3 か。テストのメソッド名は `Method_Condition_Result` でもよい
- [ ] 冪等性（同じ `x-request-id` の再送）、楽観ロックの競合、業務エラーの経路をテストしているか

## 書き方

- [ ] .NET 10 / C# 14 の書き方を使ってよい（ファイルスコープ namespace、プライマリコンストラクタ、コレクション式）。`server/.editorconfig` どおりか
- [ ] 命名・書式がルートの `.editorconfig` どおりか。`.cs` は BOM 付き UTF-8・LF
- [ ] `server/` のプロジェクトファイル（`.csproj`）がコミットされているか

## 確認できないこと

Docker が要る結合テスト、Cloud Run へのデプロイ、実機からの接続は、実行していなければ「未確認」と書く。
