namespace Uta.Server.Application.Support;

public sealed record LevelUpCost(int SupportPoints, int Gold);

public interface ISupportCatalog
{
    /// <summary>上限解放の回数ごとの上限レベル（SupportLevelCap）。カード ID・回数が無ければ InvalidOperationException。</summary>
    int GetMaxLevel(int cardId, int limitBreak);

    /// <summary>Lv fromLevel → toLevel の必要量の合計（SupportLevelCost の Level が fromLevel 以上 toLevel 未満の行の和）。toLevel &gt; fromLevel。行が足りなければ InvalidOperationException。</summary>
    LevelUpCost GetLevelUpCost(int cardId, int fromLevel, int toLevel);
}
