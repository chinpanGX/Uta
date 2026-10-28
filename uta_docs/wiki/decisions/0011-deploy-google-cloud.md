---
title: ADR 0011 API サーバーをコンテナにして Google Cloud（Cloud Run + Cloud SQL）にデプロイする
status: accepted
updated: 2026-10-28
---

# ADR 0011: API サーバーをコンテナにして Google Cloud（Cloud Run + Cloud SQL）にデプロイする

## 状況

ローカルでテストし、問題がなければデプロイして実機から確かめる、という工程を回したい。クラウドへのデプロイは経験としても価値がある。サーバーは MagicOnion（gRPC、HTTP/2）、DB は PostgreSQL（[ADR 0010](0010-database.md)）。商用公開は非目標（[game-spec.md](../overview/game-spec.md)）なので、使わない時間の費用は抑えたい。

## 決定

- **API サーバーはコンテナにする**。イメージは .NET SDK のコンテナ発行（`dotnet publish /t:PublishContainer`）で作り、Dockerfile は書かない（必要になったら見直す）
- **デプロイ先は Google Cloud**: API は **Cloud Run**、DB は **Cloud SQL for PostgreSQL**、イメージは **Artifact Registry**
- **環境はローカルとデプロイ先（dev）の2つ**。本番環境は分けない
- 開発中は、PostgreSQL だけを Docker Compose で動かし、API は `dotnet run` や IDE から直接起動する

工程の詳細は [architecture/repository-layout.md](../architecture/repository-layout.md) 11章。

## 理由

- Cloud Run は gRPC（HTTP/2）に対応し、使わないときは0台まで縮むので、API の費用を抑えやすい
- コンテナ、マネージド DB、レジストリ、デプロイという、よくある構成を一通り経験できる
- コンテナにすることで、ローカル・テスト（Testcontainers）・デプロイ先で実行環境をそろえられる

## 検討した代替案

- AWS（ECS on Fargate + ALB + RDS）: 業務で最も使われている構成の1つで、インフラの基礎を広く経験できる。ただし 0 台まで縮めにくく（ALB・RDS が常に動く）、ネットワーク・権限の構築の手間も多い
- Azure Container Apps + Azure Database for PostgreSQL: .NET との相性がよく gRPC にも対応するが、構成と費用は Cloud Run と同程度で、設定項目が多め
- VPS + Docker Compose: ローカルと同じ構成で月額も固定だが、TLS・OS の更新・監視を自分で持つことになり、マネージドサービスの経験にならない

## 影響・リスク

- **Cloud SQL は最小構成でも常に費用がかかる**。使わない期間はインスタンスを停止する。料金は作る前に公式の料金表で確認する
- Cloud Run で gRPC を通すには、HTTP/2 で end-to-end に受ける設定と、Kestrel を HTTP/2（h2c）で待ち受ける設定が要る（推測・要確認）
- Unity（実機）から Cloud Run へ HTTPS + HTTP/2 で MagicOnion をつなげるかを、早期に確かめる必要がある（[repository-layout.md](../architecture/repository-layout.md) 8章）
- 接続情報などの秘密の値の置き場（Secret Manager など）と、マイグレーションの実行方法は未決
- CI は、最初は手動で流し、手順が固まったら GitHub Actions で自動化する（2026-10-28 追記。[repository-layout.md](../architecture/repository-layout.md) 11.2）

## 見直す条件

- 費用が想定を超えたとき
- Cloud Run で gRPC の要件（接続の持続、ヘッダーなど）を満たせないと分かったとき
