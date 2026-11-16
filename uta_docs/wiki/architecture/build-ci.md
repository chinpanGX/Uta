---
title: ビルドと実機の確かめ方
status: draft
updated: 2026-11-16
---

# ビルドと実機の確かめ方

[repository-layout.md](repository-layout.md) 7章（ビルド・CI）の実体。いまは手で回すビルドと、実機での確かめ方だけを書く。CI は未実装（7章の「これから作るもの」）。確かめた結果は [unity7-evaluation.md](unity7-evaluation.md) に書き、このページには手順と決めごとを書く。

MVP の完了条件 7（[game-spec.md](../overview/game-spec.md)「完了条件」）の確かめ方はこのページが正。実装計画は [plans/2026-11-11-android-smoke.md](../../plans/2026-11-11-android-smoke.md)。

## 1. 共通

- Unity のバッチモードのビルドは、Editor が開いていない worktree で回す。同じプロジェクトを開いた Editor があると競合する（[unity7-evaluation.md](unity7-evaluation.md) 4章）。Editor は閉じない
- ビルド用の worktree は `client/uta/Assets/Packages/`（NuGetForUnity の復元先。`.gitignore` 済み）を、Editor で開いている worktree からコピーしてから使う。起動時の自動復元は、ほかにコンパイルエラーがあると走らないため（[unity7-evaluation.md](unity7-evaluation.md) 2章）
- ビルドの成果物は `client/uta/Build/`（`.gitignore` 済み）

## 2. Windows の IL2CPP

- 入口: `Uta.Client.Editor.BuildCommands.BuildWindowsIl2Cpp`（`client/uta/Build/Windows/Uta.exe`）。コマンドは [CLAUDE.md](../../../CLAUDE.md)「ビルド・テスト・実行」
- 置き場: `../Uta-wt/build-foundation`（ビルドターゲットは Windows のまま使う）
- スタンドアロンのスクリプトバックエンドは、プロジェクトの設定でも IL2CPP にする（2026-11-11。`ProjectSettings.asset` の `scriptingBackend` に `Standalone: 1`）。`BuildWindowsIl2Cpp` は前から IL2CPP を指定しているので結果は変わらない。Test Runner でスタンドアロンのプレイヤーを作るとき（5.3）に、設定どおりの IL2CPP で作らせるため。Editor の Play は Mono のまま

## 3. Android の IL2CPP [決定。2026-11-16。ユーザーの判断。置き場は「進めて」で推奨どおり]

| 項目 | 値 |
|---|---|
| 入口 | `Uta.Client.Editor.BuildCommands.BuildAndroidIl2Cpp`。成果物は `client/uta/Build/Android/Uta.apk` |
| 形式 | APK（AAB にしない。`adb install` で入れるため）。`EditorUserBuildSettings.buildAppBundle = false` |
| スクリプトバックエンド | IL2CPP（`ProjectSettings.asset` は既に `Android: 1`。入口でも指定する。Windows と同じ） |
| CPU | ARM64 だけ（`ProjectSettings.asset` の `AndroidTargetArchitectures: 2` は既に ARM64） |
| パッケージ名 | `com.chinpangx.uta`（`ProjectSettings.asset` の `applicationIdentifier` の `Android`。テンプレートの値を置き換える）。アカウントのファイルの鍵には使わない（[unity7-evaluation.md](unity7-evaluation.md) 4章） |
| 署名 | Unity の既定のデバッグ用の鍵（`~/.android/debug.keystore`）。キーストアはリポジトリに入れない |
| 最小・目標の API レベル | 最小 26（今の設定のまま）、目標は「入っている最新」（今の設定 `0` のまま） |
| SDK・NDK・JDK | Unity Hub で入れた `7000.0.0a12` の Android のモジュールに同梱のもの（`Editor/Data/PlaybackEngines/AndroidPlayer/` の `SDK`・`NDK`・`OpenJDK`。2026-11-11 に、SDK の `platforms` に `android-34`・`android-36`・`android-37.0` があることを確かめた）。別に入れない |
| Addressables | 入口が `AddressableAssetSettings.BuildPlayerContent` を先に実行する（Windows と同じ）。アクティブなビルドターゲットが Android でなければ止める |
| 置き場 | 専用の worktree `../Uta-wt/build-android`（ビルドターゲットは Android のまま使う。Windows と行き来すると Library の再インポートになるため） |

- `adb` は同梱の SDK の `platform-tools/adb.exe`（PATH には無い）。手順では `ADB="/c/Program Files/Unity/Hub/Editor/7000.0.0a12/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe"` と置いて呼ぶ
- 確かめていないもの（[計画](../../plans/2026-11-11-android-smoke.md)のタスク 1 のスパイクで確かめる）: Unity 7 Alpha の Android のビルド（Gradle・IL2CPP の arm64）、YetAnotherHttpHandler のネイティブ（`android-arm64` の `.so` はパッケージにある）、MasterMemory・VContainer・MessagePack の実機での動作、Managed Stripping の既定の値で落ちないか

## 4. 実機からローカルのサーバーへの接続 [決定。2026-11-16。ユーザーの判断。クラウドの dev 環境は使わない]

- USB でつなぎ、`adb reverse tcp:5000 tcp:5000` で、端末の `localhost:5000` を PC の `localhost:5000` に流す。クライアントの接続先は既定の `http://localhost:5000`（`ServerEndpoint`）のままで、コードは変えない。サーバーはローカルの手順（[CLAUDE.md](../../../CLAUDE.md)「ビルド・テスト・実行」）のまま起動する（外に公開しない・ファイアウォールを開けない）
- Android では `-uta-server-url` の起動引数を渡す手段を作らない。dev 環境（Cloud Run）につなぐ手段は、dev 環境を作るときに決める（[repository-layout.md](repository-layout.md) 11章）
- 平文の HTTP/2（h2c）: YetAnotherHttpHandler は Android の通信の仕組みを使わず自前のネイティブで通信するので、Android の平文の通信の制限（Network Security Config）にかからないと見込む（推測。スパイクで確かめる）
- `adb reverse` は USB を抜く・端末を再起動すると消える。スモークの前に毎回実行する
- アカウントのファイルは `persistentDataPath` の `account-http___localhost_5000.dat`。最初からやり直すときは、アプリのデータを消す（`adb shell pm clear com.chinpangx.uta`）

## 5. 完了条件 7 の確かめ方

### 5.1 スモーク（起動 → ログイン → ガチャ 1 回）

手動。端末を手で操作する。

1. ローカルのサーバーを起動する（PostgreSQL・`migrate`・`dotnet run`）
2. `adb install -r client/uta/Build/Android/Uta.apk`、`adb reverse tcp:5000 tcp:5000`
3. `adb logcat -c` のあと、別の端末で `adb logcat -s Unity` を流しておく
4. アプリのデータを消してから起動し、タイトル → ホーム（サインアップ → サインイン）→ ガチャ → 単発を 1 回 → 結果 → ガチャの画面に戻る
5. アプリを終了して起動し直し、ホームまで進む（保存したアカウントでサインインする経路）

通ったとみなすもの: 4・5 でエラーのダイアログが出ない、ガチャの結果が表示される、ホームの所持の表示（ガチャ通貨）が引いた分だけ減る、logcat に `[Uta] master: ok` が出て例外（`Exception`）の行が無い。

### 5.2 IL2CPP の計算の一致

比べ方の決定は [lesson-phases.md](lesson-phases.md) 8章と [produce-domain.md](produce-domain.md) 7章（項目ごと。ハッシュは作らない）。ここでは置き場と回し方を決める。

- **テストのカードの置き場** [決定。2026-11-16。ユーザーの「進めて」で推奨どおり]: テストのカード（`TestCards`・`Phase1Cards`〜`Phase4Cards`・`SyntheticCards`・`TestCardCatalog`）を、`Uta.Shared.Domain.Tests` から新しいプロジェクト `shared/Uta.Shared.Domain.Testing`（netstandard2.1・C# 9。Unity のパッケージ `com.uta.shared.domain.testing`）に移す。.NET のテストと Unity の実機のテストが、同じカードの定義・同じ再生の手順・同じ期待値を使うため（写すと、写しのずれと IL2CPP の不一致を見分けられない）
  - 名前空間は移す前の `Uta.Shared.Domain.Tests.Lesson` のまま（参照している 14 ファイルを変えないため。「ルート名前空間はアセンブリ名と同じ」の規則の例外）
  - Unity の asmdef `Uta.Shared.Domain.Testing` は `defineConstraints` に `UNITY_INCLUDE_TESTS` を持ち、テストを含むビルドでだけコンパイルされる（本番のプレイヤーに入れない）
- **再生のシナリオ**: `Uta.Shared.Domain.Testing` の `LessonReplayScenario.Run`。36 枚のデッキ（フェーズ 1〜4 のカードを 1 枚ずつ）で、使えるカードがあれば手札の先頭から使い、無ければスキップして、レッスンの終わりまで進める（今の `LessonReplayTests` と同じ手順）。通常のレッスンと中間試験の 2 つ
- **期待値**: `Uta.Shared.Domain.Testing` の `LessonReplayExpected` に、.NET で再生した最後の状態の `LessonStateComparer.Describe` の行を定数で書く。.NET の単体テスト（`LessonReplayExpectedTests`）が、期待値とシナリオの結果が同じであることを毎回確かめる（シナリオやルールを変えて期待値が古くなったら、.NET のテストが落ちる）
- **実機のテスト**: `client/uta/Assets/Tests/SharedDomainReplay/`（asmdef `Uta.Client.Tests.SharedDomainReplay`。PlayMode のテスト。参照は `Uta.Shared.Domain`・`Uta.Shared.Domain.Testing`・Unity Test Framework）。シナリオを再生し、期待値と項目ごとに比べる
- **回し方**: Editor の PlayMode（Mono。テストのコードの確認だけで、IL2CPP の確認にはならない）で通してから、`build-android` の worktree で Test Runner の実機実行をする（バッチモードの `-runTests -testPlatform Android`。Unity が APK を作って端末に入れ、結果を受け取る）。CI では回さない（[ADR 0008](../decisions/0008-shared-domain-and-onion.md)）

### 5.3 Android で通らないとき [決定。2026-10-31]

[game-spec.md](../overview/game-spec.md)「完了条件」の代え方のとおり、Windows の IL2CPP スタンドアロンで代え、Android は iOS と同じ別の項目に回す。代えるときの手順:

- スモーク: `Uta.exe` を引数なしで起動し、5.1 の 4・5 を手で行う
- 計算の一致: `build-foundation` の worktree で、Test Runner の `-testPlatform StandaloneWindows64`（2章のとおり、スタンドアロンは設定で IL2CPP）

代えるかどうかは、計画のスパイクの「止まる条件」に当たったときに、ユーザーが決める。

## 6. 未決事項

- iOS の実機ビルド（[ADR 0006](../decisions/0006-adopt-unity-7-early.md)。Unity 7 の対応状況しだいの別の項目）
- CI（[repository-layout.md](repository-layout.md) 7章）
- dev 環境（Cloud Run）に実機からつなぐときの接続先の渡し方（4章）
