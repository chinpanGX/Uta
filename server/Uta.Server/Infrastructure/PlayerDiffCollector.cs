using Uta.Server.Application.Idol;
using Uta.Server.Application.Player;
using Uta.Server.Application.Support;
using Uta.Shared.Contracts.Common;
using Uta.Shared.Contracts.Player;
using VolTech.Server.Transaction;

namespace Uta.Server.Infrastructure;

/// <summary>
/// 1 つのトランザクションで触った通貨・アイドル・サポートカードを記録し、BuildAsync で DB から読み直して player_diff にする。
/// Infrastructure から Contracts の DTO を参照する唯一の例外（TxContext.GetCollector が具象型の完全一致を要求するため）。
/// </summary>
public sealed class PlayerDiffCollector(IPlayerRepository players, IIdolRepository idols, ISupportCardRepository supportCards)
    : ICommonResponseCollector<AppCommonDto>
{
    private Ulid? playerId;
    private bool balanceTouched;
    private readonly HashSet<int> touchedIdols = [];
    private readonly HashSet<Ulid> touchedSupportCards = [];
    private readonly HashSet<Ulid> removedSupportCards = [];

    public void TouchBalance(Ulid playerId)
    {
        Bind(playerId);
        balanceTouched = true;
    }

    public void TouchIdol(Ulid playerId, int idolId)
    {
        Bind(playerId);
        touchedIdols.Add(idolId);
    }

    public void TouchSupportCard(Ulid playerId, Ulid uid)
    {
        Bind(playerId);
        removedSupportCards.Remove(uid);
        touchedSupportCards.Add(uid);
    }

    /// <summary>upsert の記録からも外す。</summary>
    public void RemoveSupportCard(Ulid playerId, Ulid uid)
    {
        Bind(playerId);
        touchedSupportCards.Remove(uid);
        removedSupportCards.Add(uid);
    }

    public async ValueTask<AppCommonDto?> BuildAsync(TxContext tx)
    {
        if (playerId is not { } id)
        {
            return null;
        }

        var hasBalance = false;
        var level = 0;
        var exp = 0;
        var gold = 0;
        var supportPoints = 0;
        var gachaCurrency = 0;
        if (balanceTouched)
        {
            var snapshot = await players.FindAsync(tx, id)
                ?? throw new InvalidOperationException("player not found");
            hasBalance = true;
            level = snapshot.Level;
            exp = snapshot.Exp;
            gold = snapshot.Gold;
            supportPoints = snapshot.SupportPoints;
            gachaCurrency = snapshot.GachaCurrency;
        }

        var idolDtos = new List<OwnedIdolDto>();
        if (touchedIdols.Count > 0)
        {
            var owned = await idols.ListAsync(tx, id);
            foreach (var idolId in touchedIdols.Order())
            {
                var idol = owned.FirstOrDefault(x => x.MasterId == idolId)
                    ?? throw new InvalidOperationException($"touched idol not found: {idolId}");
                idolDtos.Add(new OwnedIdolDto(idol.MasterId, idol.Training, idol.Awakening, idol.Shards, idol.UniqueId.ToString()));
            }
        }

        var cardDtos = new List<OwnedSupportCardDto>();
        foreach (var uid in touchedSupportCards.Order())
        {
            var card = await supportCards.FindAsync(tx, id, uid)
                ?? throw new InvalidOperationException($"touched support card not found: {uid}");
            cardDtos.Add(new OwnedSupportCardDto(card.UniqueId.ToString(), card.MasterId, card.Level, card.LimitBreak));
        }

        var removed = removedSupportCards.Order().Select(x => x.ToString()).ToArray();

        if (!hasBalance && idolDtos.Count == 0 && cardDtos.Count == 0 && removed.Length == 0)
        {
            return null;
        }

        return new AppCommonDto(new PlayerDiffDto(hasBalance, level, exp, gold, supportPoints, gachaCurrency, idolDtos.ToArray(), cardDtos.ToArray(), removed));
    }

    private void Bind(Ulid id)
    {
        if (playerId is { } current && current != id)
        {
            throw new InvalidOperationException($"player_diff は 1 トランザクションで 1 人分だけ: {current} と {id}");
        }

        playerId = id;
    }
}
