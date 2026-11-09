---
title: ADR 0023 クライアントの LINQ は ZLinq を使う
status: accepted
updated: 2026-11-09
---

# ADR 0023: クライアントの LINQ は ZLinq を使う

## 状況

- クライアント（Unity）で LINQ 形式の処理を書く場面が増えている
  - 例: 所持の育成アイドルからレア度が最も高いものを選ぶ `FeaturedIdolPicker`
  - 例: マスタからカードの定義を組み立てる `CardCatalogFactory`
- 標準の `System.Linq` は、列挙子・クロージャ・並べ替えの配列を、呼ぶたびにヒープに確保する
- モバイル（IL2CPP）では GC の停止が描画に響く
- ユーザーが、LINQ 形式の実装は ZLinq を使うと判断した（2026-11-09。PR #52 の `FeaturedIdolPicker` の書き方の相談から）

## 決定

- **クライアントの LINQ 形式の実装は ZLinq を使う**
  - [ZLinq](https://github.com/Cysharp/ZLinq)（Cysharp。構造体の列挙子で、確保を減らした LINQ）を使う
  - `ZLinq.DropInGenerator`（Source Generator）で、書き方は標準の LINQ のまま ZLinq に置き換える
- 入れ方: NuGetForUnity の `client/uta/Assets/packages.config` に、次の 2 つを足す（版は固定する）
  - `ZLinq` 1.6.1（`targetFramework="netstandard2.1"` を付ける。下の「影響・リスク」）
  - `ZLinq.DropInGenerator` 1.6.1
  - Unity の拡張 `ZLinq.Unity`（git URL のパッケージ。GameObject の走査・Native のコレクション）は入れない。要るようになったら足す
- **対象は client だけ**: Uta の手書きの asmdef 全部
  - 対象: `Uta.Client.Domain`・`Application`・`Infrastructure`・`Presentation`・`Bootstrap`・`Editor`・`Tests.EditMode`
  - 生成コードの `Uta.Client.MasterData` は除く
  - shared（`Uta.Shared.Domain`・`Contracts`）と server は、標準の `System.Linq` のまま。shared はサーバー（.NET 10）と同じソースを使うので、依存を増やさない（ユーザーの判断）
- **置き換える型は `DropInGenerateTypes.Everything`**（配列・Span・Memory・List・IEnumerable。ユーザーの判断）
  - 生成する名前空間はグローバル（`""`）にする。README の「Enumerable を置き換えるときは、名前空間の優先のためにグローバルにする」に従う
  - 各 asmdef に `AssemblyAttributes.cs` を 1 つ置く: `[assembly: ZLinq.ZLinqDropIn("", ZLinq.DropInGenerateTypes.Everything)]`
  - DropInGenerator は asmdef ごとに属性が要る（README）
- **LINQ を書くファイルは、`using System.Linq;` に加えて `using ZLinq;` を書く**
  - DropIn が置き換えるのは、元のコレクションに対する最初の呼び出し（`OrderByDescending`・`Select` など）だけ
  - そのあとに続ける `ThenBy`・`ToList`・`FirstOrDefault` などは、`ZLinq` 名前空間の拡張メソッドなので、`using ZLinq;` が無いと見えない
  - 見えないと、標準の LINQ の版が選ばれて型が合わず、コンパイルエラーになる（CS0411。2026-11-09 に確かめた）
- 結果の型は、ZLinq の `ValueEnumerable<…>`（構造体）になる
  - `IEnumerable<T>` を受け取る所に渡すときは、`ToArray()`・`ToList()` で確定させる
  - 返り値の型を `IEnumerable<T>` にしない

## 理由

- 書き方を変えずに、確保を減らせる
  - DropIn なので、`using System.Linq;` のままの標準の書き方が ZLinq に置き換わる
  - 配列と `List<T>` が元なら、確保はほぼ無くなる。README「zero alloc only works when the actual implementation of `IEnumerable<T>` is an array [] or `List<T>`」
- `Everything` にすると、リポジトリが返す `IReadOnlyList<T>` などを含む全部の LINQ が ZLinq を通る。「LINQ 形式の実装は ZLinq」の決まりを、書く人が意識しなくても守れる

## 検討した代替案

- `DropInGenerateTypes.Collection`（配列・Span・Memory・List）＋ `IReadOnlyList<T>` の追加:
  - README はこちらを勧めている
  - `IEnumerable<T>` では、書く側が `AsValueEnumerable()` を呼ぶ決まりになり、守れているかを人が見る必要がある
  - 採らなかった（ユーザーの判断）
- 標準の `System.Linq` のまま、確保が効く所だけ手で書く: 決まりが無く、場所ごとの判断になる
- shared にも入れる: サーバーにも依存が増える。shared の LINQ は 4 ファイルで、どれも組み立ての処理なので今は要らない

## 影響・リスク

- `Everything` では、対象の asmdef で標準の LINQ の拡張メソッドが使えなくなる（README「making it impossible to use normal LINQ methods」）
  - ZLinq が持たない演算子や、`IEnumerable<T>` を要る API に渡す所は、`ToArray()` などで確定させてから使う
- C# 9（Unity にソースで入るコードの上限。CLAUDE.md）と Unity 7（`7000.0.0a12`）で、DropIn の生成コードを含めてコンパイルが通り、EditMode 210 件が通った（2026-11-09。`unity test`）
  - DropInGenerator が求める Unity の版は 2022.3.12f1 以上（Incremental Source Generator のため）
- **NuGetForUnity 4.5.0 は、`targetFramework` を書かないと、Unity 7 の起動時の復元で `ZLinq.dll` を取り出さない**（2026-11-09）
  - ログ: 「Couldn't find a library folder with a supported target-framework for the package ZLinq.1.6.1」
  - .nupkg には `lib/netstandard2.1` があり、開いている Editor の上で NuGetForUnity の選ぶ関数を呼ぶと `netstandard2.1` を返した
  - 起動時の復元のときだけ API 互換レベルの読み取りがずれると見ている（推測。確かめていない）。ほかのパッケージが起動時にどう選ばれるかは見ていない
  - `packages.config` の行の `targetFramework` は、NuGetForUnity の選び方より優先される。付けると、起動時の復元で `lib/netstandard2.1/ZLinq.dll` が取り出された
- IL2CPP のプレイヤーで動くかは未確認。次に Windows の IL2CPP をビルドするときに確かめる
- NuGetForUnity は、ほかの asmdef にコンパイルエラーがあると、起動時の復元を走らせない（unity7-evaluation.md 2章。前からの動き）。client の asmdef は全部 ZLinq を参照するので、ZLinq が無い worktree では、復元の前にコンパイルエラーになる。そのときは `AssemblyAttributes.cs` を一時的に外して開き、復元させてから戻す
- Editor が `Assets/Packages/` を持つ worktree ごとに、復元が要る

## 見直す条件

- ZLinq の生成コードが C# 9 または Unity 7 でコンパイルできない
- IL2CPP で動かない
- 標準の LINQ との違いで、不具合が続く
- Unity 7 が C# 14 を使えるようになり、ZLinq の版を上げる必要が出た
