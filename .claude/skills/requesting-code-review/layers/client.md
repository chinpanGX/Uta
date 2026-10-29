# client レイヤーのチェックリスト（`client/uta/`）

Unity 7（`7000.0.0a12`）のクライアント。対象は Android / iOS / Windows。
根拠: CLAUDE.md、[ADR 0006](../../../../uta_docs/wiki/decisions/0006-adopt-unity-7-early.md)・[0008](../../../../uta_docs/wiki/decisions/0008-shared-domain-and-onion.md)・[0009](../../../../uta_docs/wiki/decisions/0009-client-stack.md)、[repository-layout.md](../../../../uta_docs/wiki/architecture/repository-layout.md) 3〜4章・10章、[screen-design.md](../../../../uta_docs/wiki/architecture/screen-design.md)。

## 層と依存（オニオン）

- [ ] 依存が内側に向いているか。`Domain` → `Application` → `Infrastructure` / `Presentation` → `Bootstrap`
- [ ] asmdef の参照が repository-layout.md 3章の表どおりか（新しい asmdef・参照の追加は特に見る）
- [ ] **`Application` が UnityEngine の API を使っていないか**。asmdef では強制できない（UniTask のため `noEngineReferences` を使わない）ので、レビューで守る。`using UnityEngine`、`MonoBehaviour`、`Debug.Log`、`Time`、`PlayerPrefs` などを探す
- [ ] `Application` が `Infrastructure`・`Presentation`・`Uta.Shared` を参照していないか。外部とのやりとりはポート（インターフェース）で定義しているか
- [ ] `Presentation` が `Infrastructure`・`Uta.Shared`（通信 DTO）を参照していないか
- [ ] **DTO ↔ Domain のマッピングが `Infrastructure` に閉じているか**。DTO が Presenter や View まで漏れていないか
- [ ] マスタの生成物（`MemoryDatabase`）が `Infrastructure` に留まり、`Application` へはポート越しに必要な値だけを渡しているか
- [ ] DI の組み立て（VContainer の登録）が `Bootstrap` にあるか。ほかの層で `new` して組み立てていないか

## サーバー権威と共有ドメイン

- [ ] 結果（育成の計算結果、ガチャの抽選、所持品の更新）をクライアントで**確定**していないか。確定は必ずサーバーのレスポンスで行う
- [ ] `Uta.Domain` の使い方が「使用可否の判定と予測の表示」までか。**シードを持っていない**か。乱数が絡む結果（ドローなど）を予測していないか
- [ ] 予測とサーバーの結果がずれても壊れないか（古いクライアントの式がずれる前提。[repository-layout.md](../../../../uta_docs/wiki/architecture/repository-layout.md) 6章）
- [ ] 結果に影響するルールを `Application` / `Presentation` に独自に書いていないか（書いたら共通へ移すサイン。6.1）
- [ ] 時刻の判定にクライアントの時刻を使っていないか（サーバーが正。`x-server-time`・`x-game-date`）

## iOS（IL2CPP / AOT）とモバイル

- [ ] リフレクションに依存していないか（`Type.GetType`、`Activator.CreateInstance`、`GetMethod` など）。必要なら Source Generator / 事前生成にする
- [ ] ジェネリックの仮想メソッド・値型のジェネリックの組み合わせで、AOT のコード生成漏れが起きそうなところはないか
- [ ] MessagePack・MagicOnion のフォーマッターが事前生成に乗っているか（実行時の動的生成に頼っていないか）
- [ ] コードの削り（managed stripping）で消えて困る型に `[Preserve]` や `link.xml` が要らないか
- [ ] 毎フレームの処理で GC の割り当て（LINQ、ボクシング、文字列の連結、クロージャ）を出していないか
- [ ] プラットフォームの分岐（`#if UNITY_IOS` など）が3つの対象（Android / iOS / Windows）で抜けていないか

## 非同期（UniTask）

- [ ] `async void` を使っていないか（`UniTaskVoid` / `Forget()` を意図して使っているか）
- [ ] `CancellationToken` を受け取り、画面を閉じたら止まるか（`destroyCancellationToken` など）
- [ ] 例外が握りつぶされていないか。通信エラー（`UNAUTHENTICATED` の再送1回、`DATE_CHANGED`、メンテナンス・強制アップデート）の扱いが基盤の規約どおりか（[server-foundation.md](../../../../uta_docs/wiki/architecture/server-foundation.md)）

## 画面（uGUI + UnityScreenNavigator）

- [ ] 画面・遷移が screen-design.md の画面一覧と遷移に合っているか
- [ ] View は表示だけ、Presenter が Application を呼ぶ形になっているか
- [ ] 縦向き前提か（Windows は縦長のウィンドウ。ADR 0009）
- [ ] 画面のライフサイクル（表示・非表示・破棄）で購読や非同期処理を解放しているか

## ローカル保存

- [ ] ローカル保存が `Supplement.MessagePack` のセーブデータ（`SaveDataRepository`）を使っているか（repository-layout.md 10.6）。`PlayerPrefs` に直接書いていないか
- [ ] トークンなど秘密の値を平文で保存していないか

## 書き方・ファイル

- [ ] C# 9 の範囲か（ファイルスコープ namespace、プライマリコンストラクタ、コレクション式、Index/Range を使わない）
- [ ] 命名・書式が `.editorconfig` どおりか（Allman、`var`、private フィールドは接頭辞なしの camelCase）
- [ ] `.cs` が BOM 付き UTF-8・LF か
- [ ] 追加・移動したアセットの `.meta` がコミットされているか。`Library/` など自動生成物が入っていないか
- [ ] `Packages/manifest.json` の変更が意図したものか。MessagePack の版が MagicOnion・MasterMemory・Supplement とそろっているか
- [ ] IP方針: 既存作品の名称・素材・固有の表現が、コード・アセット・文言に入っていないか

## 確認できないこと

Unity のコンパイル・Play・実機ビルドは、レビューの中では実行できないことが多い。実行していなければ「未確認」と書き、ユーザーに実行を頼む項目として挙げる。
