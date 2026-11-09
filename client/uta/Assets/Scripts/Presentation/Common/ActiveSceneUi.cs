using System;

namespace Uta.Client.Presentation.Common
{
    /// <summary>いまのシーンの UiRoot と DialogOpener の入れ物。ルートのスコープに 1 つ（BootstrapEntry が作って RegisterInstance）。</summary>
    public sealed class ActiveSceneUi
    {
        /// <summary>入っていなければ null。</summary>
        public UiRoot Root { get; private set; }

        public DialogOpener Dialogs { get; private set; }

        /// <summary>Root が null または破棄済み（Unity の == null）なら null、そうでなければ Root.Busy。</summary>
        public BusyIndicator Busy
        {
            get
            {
                if (ReferenceEquals(Root, null) || Root == null)
                {
                    return null;
                }

                return Root.Busy;
            }
        }

        /// <summary>uiRoot・dialogOpener のどちらかが null なら ArgumentNullException。前に入っていたものは置き換える。</summary>
        public void Attach(UiRoot uiRoot, DialogOpener dialogOpener)
        {
            if (ReferenceEquals(uiRoot, null))
            {
                throw new ArgumentNullException(nameof(uiRoot));
            }

            if (dialogOpener == null)
            {
                throw new ArgumentNullException(nameof(dialogOpener));
            }

            Root = uiRoot;
            Dialogs = dialogOpener;
        }

        /// <summary>ReferenceEquals(Root, uiRoot) のときだけ Root・Dialogs を null にする。違えば何もしない（新しいシーンが先に Attach した場合）。</summary>
        public void Detach(UiRoot uiRoot)
        {
            if (ReferenceEquals(Root, uiRoot))
            {
                Root = null;
                Dialogs = null;
            }
        }
    }
}
