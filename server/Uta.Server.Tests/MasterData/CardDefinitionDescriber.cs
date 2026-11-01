using System.Globalization;
using Uta.Shared.Domain.Lesson;

namespace Uta.Server.Tests.MasterData;

/// <summary>カード定義を、比べやすい 1 行の文字列にする。UnlockLv は含めない（テストのカードは 1 固定でマスタは 1・5・10）。</summary>
internal static class CardDefinitionDescriber
{
    public static string Describe(CardDefinition card)
    {
        return string.Join(
            "|",
            "card=" + Int(card.Id),
            Int((int)card.Plan),
            Int((int)card.Type),
            Int((int)card.Rarity),
            Bool(card.OnceInLesson),
            Bool(card.Unique),
            Bool(card.StartInHand),
            "base:" + DescribeFace(card.Base),
            "enh:" + (card.Enhanced == null ? "none" : DescribeFace(card.Enhanced)));
    }

    private static string DescribeFace(CardFace face)
    {
        var costs = string.Join(";", face.Costs.Select(x => Int((int)x.Kind) + ":" + Int(x.Value)));
        return "costs[" + costs + "] conds[" + DescribeConditions(face.UseConditions) + "] effects["
            + string.Join(";", face.Effects.Select(DescribeEffect)) + "]";
    }

    private static string DescribeConditions(IReadOnlyList<ConditionDef> conditions)
    {
        return string.Join(
            ";",
            conditions.Select(x => Int((int)x.Subject) + ":" + Int((int)x.Comparator) + ":" + Int(x.Value)));
    }

    private static string DescribeEffect(EffectDef effect)
    {
        return "(" + string.Join(
            ",",
            Int((int)effect.Op),
            Int((int)effect.Status),
            Int((int)effect.Trigger),
            Int(effect.Value),
            Int(effect.Arg1),
            Int(effect.Arg2),
            Int(effect.Arg3),
            Int(effect.RefCardId),
            "c[" + DescribeConditions(effect.Conditions) + "]",
            "e[" + string.Join(";", effect.Children.Select(DescribeEffect)) + "]") + ")";
    }

    private static string Int(int value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string Bool(bool value)
    {
        return value ? "1" : "0";
    }
}
