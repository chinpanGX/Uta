using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Uta.Client.Presentation.Common.Parts;
using Uta.Client.Presentation.Title;
using VContainer.Unity;

namespace Uta.Client.Tests
{
    /// <summary>起動からタイトルまでの手順と、テストの後片付け。</summary>
    public static class TestScenes
    {
        public const float BootTimeoutSeconds = 30f;
        public const string TitleReadyStatus = "タップしてスタート";

        /// <summary>Bootstrap シーンを Single で読み、Title シーンのタイトルのページが操作できるまで待つ。</summary>
        public static IEnumerator BootToTitle()
        {
            SceneManager.LoadScene("Bootstrap", LoadSceneMode.Single);
            yield return TestWait.Until(IsTitleReady, BootTimeoutSeconds, "Title のページ（status=\"" + TitleReadyStatus + "\"、スタートが押せる）");
        }

        /// <summary>アクティブなシーンが Title で、TitlePage が activeInHierarchy、StatusText が TitleReadyStatus、StartButton が interactable。</summary>
        public static bool IsTitleReady()
        {
            if (SceneManager.GetActiveScene().name != "Title")
            {
                return false;
            }

            var page = Object.FindAnyObjectByType<TitlePage>();
            if (page == null || !page.gameObject.activeInHierarchy)
            {
                return false;
            }

            return StatusText(page) == TitleReadyStatus && StartButton(page).interactable;
        }

        /// <summary>TitlePage の子 Body/Main/StatusText の文字（TitlePrefabTests と同じパス）。</summary>
        public static string StatusText(TitlePage page)
        {
            return page.transform.Find("Body/Main/StatusText").GetComponent<TMP_Text>().text;
        }

        /// <summary>TitlePage の子 Body/Main/StartButton に付いた ButtonView の Button（TitlePage.startButton が指すもの）。</summary>
        public static Button StartButton(TitlePage page)
        {
            return page.transform.Find("Body/Main/StartButton").GetComponent<ButtonView>().Button;
        }

        /// <summary>DontDestroyOnLoad のシーンの root のうち、LifetimeScope を持つものを Destroy し、1 フレーム送る。ほかの root（Test Runner のもの）は触らない。</summary>
        public static IEnumerator DestroyPersistentScopes()
        {
            var probe = new GameObject("TestScenesProbe");
            Object.DontDestroyOnLoad(probe);
            var roots = probe.scene.GetRootGameObjects();
            Object.Destroy(probe);
            foreach (var root in roots)
            {
                if (root != probe && root.GetComponent<LifetimeScope>() != null)
                {
                    Object.Destroy(root);
                }
            }

            yield return null;
        }
    }
}
