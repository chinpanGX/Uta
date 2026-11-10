using System;
using System.Collections.Generic;

namespace Uta.Client.Presentation.Owned
{
    public interface ISupportDetailView
    {
        void SetInfo(IReadOnlyList<string> lines);
        void SetAbilities(IReadOnlyList<string> lines);
        void SetResources(string text);
        void SetLevelButtons(string plus1, string plus5, string max, bool plus1Enabled, bool plus5Enabled, bool maxEnabled);
        void SetLimitBreak(string label, bool enabled);

        event Action Plus1Clicked;
        event Action Plus5Clicked;
        event Action MaxClicked;
        event Action LimitBreakClicked;
        event Action BackClicked;
    }
}
