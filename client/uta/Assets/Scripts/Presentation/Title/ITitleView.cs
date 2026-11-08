using System;

namespace Uta.Client.Presentation.Title
{
    public interface ITitleView
    {
        void SetStatus(string text);
        void SetStartInteractable(bool interactable);
        event Action StartClicked;
    }
}
