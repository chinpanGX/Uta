using System;

namespace Uta.Client.Presentation.Home
{
    public interface IHomeView
    {
        void SetProfile(string text);
        void SetResume(bool visible, string primaryLabel);
        event Action ProduceClicked;   // 主ボタンとナビの「プロデュース」の両方
        event Action GachaClicked;
        event Action OwnedClicked;
    }
}
