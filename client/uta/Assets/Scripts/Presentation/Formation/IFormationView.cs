using System;

namespace Uta.Client.Presentation.Formation
{
    public interface IFormationView
    {
        /// <summary>育成アイドル・サポートカードの枠・シナリオ・開始ボタンの表示を入れる。</summary>
        void Set(FormationViewDto formationViewDto);

        event Action IdolClicked;
        event Action<int> SlotClicked;        // 選ぶ（枠の添字 0〜4）
        event Action<int> SlotClearClicked;   // 外す
        event Action ScenarioClicked;
        event Action StartClicked;
        event Action BackClicked;
    }
}
