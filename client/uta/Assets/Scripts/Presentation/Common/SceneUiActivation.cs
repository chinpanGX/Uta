using System;
using VContainer.Unity;

namespace Uta.Client.Presentation.Common
{
    /// <summary>各シーンのスコープのエントリーポイント。SceneUiInstaller が登録する。</summary>
    public sealed class SceneUiActivation : IStartable, IDisposable
    {
        private readonly ActiveSceneUi activeSceneUi;
        private readonly UiRoot uiRoot;
        private readonly DialogOpener dialogOpener;

        public SceneUiActivation(ActiveSceneUi activeSceneUi, UiRoot uiRoot, DialogOpener dialogOpener)
        {
            this.activeSceneUi = activeSceneUi;
            this.uiRoot = uiRoot;
            this.dialogOpener = dialogOpener;
        }

        public void Start()
        {
            activeSceneUi.Attach(uiRoot, dialogOpener);
        }

        public void Dispose()
        {
            activeSceneUi.Detach(uiRoot);
        }
    }
}
