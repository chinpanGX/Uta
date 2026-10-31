---
title: ADR 0012 マイグレーションは DbUp で、API とは別の migrate コマンドで流す
status: accepted
updated: 2026-10-28
---

# ADR 0012: マイグレーションは DbUp で、API とは別の `migrate` コマンドで流す

## 状況

DB は PostgreSQL（[ADR 0010](0010-database.md)）、デプロイ先は Cloud Run + Cloud SQL（[ADR 0011](0011-deploy-google-cloud.md)）。スキーマを更新する仕組みと、それをいつ・どこで流すかを決める必要がある。サーバー基盤の設計書は DbUp を推奨している（[server-foundation.md](../architecture/server/server-foundation.md)）。ADR 0011 では、マイグレーションの実行方法を未決にしていた。

## 決定

- **マイグレーションのツールは DbUp**。SQL のファイルを `server/Uta.Server/Migrations/` に `0001_xxx.sql` の形で置き、埋め込みリソースにする。番号の順に1回だけ流す（流した記録は DbUp の管理テーブル）
- **API の起動時には流さない**。API と同じイメージに `migrate` コマンドを持たせる
  - ローカル: `dotnet run --project server/Uta.Server -- migrate`（コマンドの形は実装時に確定）
  - dev（Google Cloud）: 同じイメージを Cloud Run のジョブとして、`migrate` を引数にして流す。デプロイの工程の3番目（[repository-layout.md](../architecture/repository-layout.md) 11.2）
- 結合テスト（Testcontainers）は、テストの開始時に同じマイグレーションを流して DB を作る

## 理由

- DbUp は SQL をそのまま書くので、Dapper（SQL を直接書く）と考え方がそろう。基盤の推奨にも合う
- 起動時に流すと、Cloud Run がインスタンスを複数立てたときに同時に流れうる。0 台から立ち上がるたびに起動も遅くなる
- 同じイメージを使うので、API とマイグレーションの版が食い違わない

## 検討した代替案

- EF Core のマイグレーション: モデルから差分を作れるが、データアクセスは Dapper なので EF Core を入れる理由が弱い
- API の起動時に流す: 手順が1つ減るが、上の同時実行と起動の遅さの問題がある
- Flyway などの .NET 以外のツール: 別の実行環境が要る

## 影響・リスク

- デプロイの手順が1つ増える（ジョブを流してから、サービスを更新する）
- DbUp は「戻す」マイグレーションを持たない。戻すときは、戻す内容の SQL を新しい番号で足す
- Cloud Run のジョブから Cloud SQL への接続は、サービスと同じ設定にする（[repository-layout.md](../architecture/repository-layout.md) 11.4）

## 見直す条件

- DbUp が .NET 10 で使えない、またはメンテナンスが止まったとき
- マイグレーションの量が増え、SQL の手書きが負担になったとき
