using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Uta.Client.Presentation.Home;
using Uta.Client.Presentation.Title;

namespace Uta.Client.Tests
{
    /// <summary>タイトルでスタート → ホームのページ。ローカルのサーバー（http://localhost:5000）が要る。</summary>
    public sealed class TitleToHomeTests
    {
        private const float HomeTimeoutSeconds = 30f;
        private const string ConnectFailedPrefix = "接続に失敗しました";

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return TestScenes.DestroyPersistentScopes();
        }

        [UnityTest]
        [Category("Server")]
        public IEnumerator Start_WithLocalServer_ReachesHomePage()
        {
            yield return TestScenes.BootToTitle();

            var titlePage = Object.FindAnyObjectByType<TitlePage>();
            TestScenes.StartButton(titlePage).onClick.Invoke();

            yield return TestWait.Until(
                IsHomeReady,
                HomeTimeoutSeconds,
                "Home のページ。ローカルのサーバー（http://localhost:5000）が起動しているか確かめる",
                () => ConnectFailed(titlePage));
        }

        /// <summary>アクティブなシーンが Home で、HomePage が activeInHierarchy。</summary>
        private static bool IsHomeReady()
        {
            if (SceneManager.GetActiveScene().name != "Home")
            {
                return false;
            }

            var page = Object.FindAnyObjectByType<HomePage>();
            return page != null && page.gameObject.activeInHierarchy;
        }

        /// <summary>タイトルのページが残っていて、文字が ConnectFailedPrefix で始まるなら、その文字を返す。そうでなければ null。</summary>
        private static string ConnectFailed(TitlePage titlePage)
        {
            if (titlePage == null)
            {
                return null;
            }

            var status = TestScenes.StatusText(titlePage);
            return status.StartsWith(ConnectFailedPrefix, System.StringComparison.Ordinal) ? "Title: " + status : null;
        }
    }
}
