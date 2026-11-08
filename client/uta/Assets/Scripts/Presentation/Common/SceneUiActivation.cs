using System;
using VContainer.Unity;

namespace Uta.Client.Presentation.Common
{
    /// <summary>各シーンのスコープのエントリーポイント。SceneUiInstaller が登録する。</summary>
    public sealed class SceneUiActivation : IStartable, IDisposable
    {
        private readonly ActiveSceneUi active;
        private readonly UiRoot root;
        private readonly DialogOpener dialogs;

        public SceneUiActivation(ActiveSceneUi active, UiRoot root, DialogOpener dialogs)
        {
            this.active = active;
            this.root = root;
            this.dialogs = dialogs;
        }

        public void Start()
        {
            active.Attach(root, dialogs);
        }

        public void Dispose()
        {
            active.Detach(root);
        }
    }
}
