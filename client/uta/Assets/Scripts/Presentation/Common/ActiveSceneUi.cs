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

        /// <summary>root・dialogs のどちらかが null なら ArgumentNullException。前に入っていたものは置き換える。</summary>
        public void Attach(UiRoot root, DialogOpener dialogs)
        {
            if (ReferenceEquals(root, null))
            {
                throw new ArgumentNullException(nameof(root));
            }

            if (dialogs == null)
            {
                throw new ArgumentNullException(nameof(dialogs));
            }

            Root = root;
            Dialogs = dialogs;
        }

        /// <summary>ReferenceEquals(Root, root) のときだけ Root・Dialogs を null にする。違えば何もしない（新しいシーンが先に Attach した場合）。</summary>
        public void Detach(UiRoot root)
        {
            if (ReferenceEquals(Root, root))
            {
                Root = null;
                Dialogs = null;
            }
        }
    }
}
