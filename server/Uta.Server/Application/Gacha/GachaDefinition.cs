using Uta.Server.Domain.Gacha;

namespace Uta.Server.Application.Gacha;

public sealed record GachaPoolItem(GrantedItemKind Kind, int ItemId, int Weight);

/// <summary>排出テーブル 1 つ（マスタの GachaTableEntry の集まり）。MasterMemory が生成する GachaTable と名前が重ならないよう Pool と呼ぶ。</summary>
public sealed class GachaPool
{
    public GachaPool(int gachaTableId, IReadOnlyList<GachaPoolItem> items)
    {
        GachaTableId = gachaTableId;
        Items = items;
        Weights = items.Select(x => x.Weight).ToList();
    }

    public int GachaTableId { get; }

    /// <summary>Order 昇順。</summary>
    public IReadOnlyList<GachaPoolItem> Items { get; }

    public IReadOnlyList<int> Weights { get; }
}

public sealed record CeilingItem(int Order, GrantedItemKind Kind, int ItemId);

public sealed record GachaDefinition(
    int GachaId, int SingleCost, int TenCost, int CeilingPoints,
    GachaPool Normal, GachaPool Guaranteed, IReadOnlyList<CeilingItem> CeilingItems);
