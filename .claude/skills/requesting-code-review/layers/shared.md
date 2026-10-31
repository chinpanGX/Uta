# shared レイヤーのチェックリスト（`shared/`、`masterdata/`）

`shared/Uta.Domain`（純粋なゲームルール）と `shared/Uta.Shared`（サービスIF・DTO）。Unity（IL2CPP）と .NET 10 サーバーの**両方**でソースのまま動く。`masterdata/`（マスタの定義・CSV）もここで見る。
根拠: CLAUDE.md、[ADR 0007](../../../../uta_docs/wiki/decisions/0007-masterdata-with-addons.md)・[0008](../../../../uta_docs/wiki/decisions/0008-shared-domain-and-onion.md)、[repository-layout.md](../../../../uta_docs/wiki/architecture/repository-layout.md) 3〜6章・10章、[lesson-domain-types.md](../../../../uta_docs/wiki/architecture/lesson-domain-types.md)、[masterdata-pipeline.md](../../../../uta_docs/wiki/architecture/master/masterdata-pipeline.md)、[api-design.md](../../../../uta_docs/wiki/architecture/api-design.md)。

**このレイヤーの不具合は、クライアントとサーバーの両方に同時に出る。** 重さは両側への影響で付ける。

## 言語・ランタイムの制約

- [ ] **C# 9 の範囲か**。ファイルスコープ namespace、プライマリコンストラクタ、コレクション式（`[]`）、Index/Range（`^1`、`..`）、`required`、raw 文字列リテラル、`record struct` を使っていないか
- [ ] netstandard2.1 にない API（.NET 5 以降の API。`Random.Shared`、`Math.Clamp` の一部の型、`ArgumentNullException.ThrowIfNull` など）を使っていないか
- [ ] `record`・nullable の値型（`int?`）を使っていないか（マスタの生成ソースとそろえる。repository-layout.md 5章）
- [ ] `init` を使うなら `IsExternalInit` の置き方が決まっているか（要確認の項目。5章）
- [ ] リフレクションに依存していないか（iOS の AOT）

## Uta.Domain の制約（ADR 0008）

- [ ] **`float` / `double` を使っていないか**。小数は整数・固定小数点（万分率など）で持っているか。`decimal` も含め、割り算の丸め方が仕様どおりか
- [ ] 整数のオーバーフローが起こりうる計算で、範囲を押さえているか（`checked` か、上限の検証）
- [ ] **乱数は `IRandom` の注入か**。`System.Random`・`Guid.NewGuid()`・時刻を乱数に使っていないか。範囲内の整数は棄却法、シャッフルは Fisher–Yates か（10.3）
- [ ] 同じシードと操作列で同じ結果になるか（`Dictionary` / `HashSet` の列挙順、`GetHashCode` に結果を頼っていないか。並べ替えが安定か）
- [ ] UnityEngine・MasterMemory とその生成物・MagicOnion・DB に依存していないか（BCL のみ）
- [ ] **同期処理だけか**（`Task` / `UniTask` を使わない。ADR 0009）
- [ ] 時刻（`DateTime.Now`）・ファイル・環境変数など外部の状態に触れていないか
- [ ] `[ServerOnly]` のマスタの列に依存していないか。必要な値は引数で受け取っているか
- [ ] サーバー固有（抽選、天井、シード生成）・クライアント固有（表示の整形、演出）の知識が入り込んでいないか（6.1 の判断基準）
- [ ] 継承・`partial` で各側から広げる前提になっていないか（合成とインターフェースで差し替える）
- [ ] 型と状態遷移が lesson-domain-types.md や systems/ の仕様（ゾーンの移動、ターン処理順、計算式）どおりか

## Uta.Shared の制約（通信契約）

- [ ] DTO が enum とプリミティブ型で組まれ、マッピングが薄く保てる形か（ADR 0008）。Domain の型をそのまま DTO にしていないか
- [ ] MessagePack の属性（`[MessagePackObject]`、`[Key(n)]`）が付いているか。**既存の `Key` の番号を変えたり再利用したりしていないか**（古いクライアントと互換が壊れる）
- [ ] フィールドの追加・削除・型の変更が、古いアプリ・新しいサーバーの組み合わせで壊れないか。壊れるなら、アプリのバージョンを上げて手元のアプリを入れ直す前提が書かれているか（強制アップデートは実装しない。10.5）
- [ ] サービスIF に、RPC ごとの属性（`[SkipAuth]`・`[NoSideEffects]`）が仕様どおり付いているか
- [ ] エラーコード（reason）を消して使い回していないか
- [ ] 秘匿すべき値（シード、抽選の内部状態、ほかのプレイヤーの情報）を DTO に載せていないか
- [ ] ID（ULID）・時刻の持ち方が決まった形か（未決なら、勝手に決めていないか。10.1）
- [ ] VolTech-MagicOnion の `Abstractions` と役割が重なっていないか（未決事項。Uta 固有のものだけを置くのが素直）

## masterdata/（ADR 0007）

- [ ] マスタの列の型が `int` / `long` / `string` / `bool` / enum だけか。`float`・配列・nullable がないか
- [ ] マスタ型を `shared/` や Unity に直接置いていないか（定義は `masterdata/Definitions/`、各側へは生成する）
- [ ] `[ClientOnly]` / `[ServerOnly]` の付け方で、クライアントに渡してはいけない列（確率の内部値など）が出ていないか
- [ ] 他のテーブルを指す列に `[Exists]` が付いているか。`Op` ごとの引数の意味が一覧にあり、`IValidatable` で検証しているか
- [ ] 日時が `string`（`yyyy-MM-dd HH:mm:ss`、JST、オフセットなし）か（10.2）
- [ ] 生成したソースが更新されコミットされているか（`master-gen --check` で検出される状態か）。バイナリをコミットしていないか
- [ ] CSV がタブ名＝テーブル名の書き出しで、1行1レコードか
- [ ] IP方針: 名称・テキストに既存作品の固有名詞が入っていないか

## テスト

- [ ] `shared/Uta.Domain.Tests` に、計算式と「同じシードと操作列で同じ結果」のテストがあるか（10.4）
- [ ] 境界（0、上限、空の山札、ターンの最後）を押さえているか
- [ ] 実行できるなら `dotnet test --project shared/Uta.Domain.Tests` を回し、結果をそのまま書く。回していなければ「未確認」

## 書き方

- [ ] `shared/.editorconfig` どおりか（ブロック形式の namespace など）。命名はルートの `.editorconfig`
- [ ] `.cs` が BOM 付き UTF-8・LF か
- [ ] `shared/` のプロジェクトファイルがコミットされているか
- [ ] Unity にソースで入るので、Unity 側の `.meta` や asmdef の扱いが変更に追従しているか（取り込み方は要確認の項目。2章）
