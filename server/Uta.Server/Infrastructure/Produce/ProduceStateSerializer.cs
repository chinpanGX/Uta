using MessagePack;
using Uta.Server.Application.Produce;
using Uta.Server.Domain.Produce;
using Uta.Shared.Domain.Lesson;
using Uta.Shared.Domain.Produce;
using Uta.Shared.Domain.Rng;

namespace Uta.Server.Infrastructure.Produce;

/// <summary>ProduceSessionData と state 列の MessagePack の相互変換（produce-server.md 3章）。</summary>
public static class ProduceStateSerializer
{
    public static byte[] Serialize(ProduceSessionData data) =>
        MessagePackSerializer.Serialize(new ProduceStateBlob
        {
            MasterSeed = data.MasterSeed,
            Streams = data.Streams.Select(x => ToBlob(x)).ToArray(),
            Snapshot = ToBlob(data.Snapshot),
        });

    public static ProduceSessionData Deserialize(byte[] bytes)
    {
        var blob = MessagePackSerializer.Deserialize<ProduceStateBlob>(bytes);
        return new ProduceSessionData(blob.MasterSeed, blob.Streams.Select(x => FromBlob(x)).ToList(), FromBlob(blob.Snapshot));
    }

    private static RandomStreamBlob ToBlob(RandomStreamState x) => new()
    {
        Kind = (int)x.Key.Kind,
        Week = x.Key.Week,
        Attempt = x.Key.Attempt,
        S0 = x.S0,
        S1 = x.S1,
        S2 = x.S2,
        S3 = x.S3,
        DrawCount = x.DrawCount,
    };

    private static RandomStreamState FromBlob(RandomStreamBlob x) =>
        new(new RandomStreamKey((RandomStreamKind)x.Kind, x.Week, x.Attempt), x.S0, x.S1, x.S2, x.S3, x.DrawCount);

    private static SupportCardSnapshotBlob ToBlob(SupportCardSnapshot x) => new()
    {
        CardId = x.CardId,
        Level = x.Level,
        LimitBreak = x.LimitBreak,
    };

    private static SupportCardSnapshot FromBlob(SupportCardSnapshotBlob x) => new()
    {
        CardId = x.CardId,
        Level = x.Level,
        LimitBreak = x.LimitBreak,
    };

    private static CardInstanceSnapshotBlob ToBlob(CardInstanceSnapshot x) => new()
    {
        InstanceId = x.InstanceId,
        CardId = x.CardId,
        Enhanced = x.Enhanced,
    };

    private static CardInstanceSnapshot FromBlob(CardInstanceSnapshotBlob x) => new()
    {
        InstanceId = x.InstanceId,
        CardId = x.CardId,
        Enhanced = x.Enhanced,
    };

    private static ExamReportSnapshotBlob ToBlob(ExamReportSnapshot x) => new()
    {
        Kind = (int)x.Kind,
        Attempt = x.Attempt,
        Score = x.Score,
        Rank = x.Rank,
        NpcScores = x.NpcScores,
        Passed = x.Passed,
    };

    private static ExamReportSnapshot FromBlob(ExamReportSnapshotBlob x) => new()
    {
        Kind = (ExamKind)x.Kind,
        Attempt = x.Attempt,
        Score = x.Score,
        Rank = x.Rank,
        NpcScores = x.NpcScores,
        Passed = x.Passed,
    };

    private static ExamBackupSnapshotBlob ToBlob(ExamBackupSnapshot x) => new()
    {
        Parameters = x.Parameters,
        Hp = x.Hp,
        Deck = x.Deck.Select(v => ToBlob(v)).ToArray(),
        NextInstanceId = x.NextInstanceId,
    };

    private static ExamBackupSnapshot FromBlob(ExamBackupSnapshotBlob x) => new()
    {
        Parameters = x.Parameters,
        Hp = x.Hp,
        Deck = x.Deck.Select(v => FromBlob(v)).ToArray(),
        NextInstanceId = x.NextInstanceId,
    };

    private static ProduceResultSnapshotBlob ToBlob(ProduceResultSnapshot x) => new()
    {
        Outcome = (int)x.Outcome,
        EvaluationValue = x.EvaluationValue,
        Rank = (int)x.Rank,
        FinalScore = x.FinalScore,
        FinalRank = x.FinalRank,
    };

    private static ProduceResultSnapshot FromBlob(ProduceResultSnapshotBlob x) => new()
    {
        Outcome = (ExamOutcome)x.Outcome,
        EvaluationValue = x.EvaluationValue,
        Rank = (ProduceRank)x.Rank,
        FinalScore = x.FinalScore,
        FinalRank = x.FinalRank,
    };

    private static OperationSnapshotBlob ToBlob(OperationSnapshot x) => new()
    {
        Number = x.Number,
        Kind = (int)x.Kind,
        Arg1 = x.Arg1,
        Arg2 = x.Arg2,
        Arg3 = x.Arg3,
        Week = x.Week,
        Attempt = x.Attempt,
        Turn = x.Turn,
    };

    private static OperationSnapshot FromBlob(OperationSnapshotBlob x) => new()
    {
        Number = x.Number,
        Kind = (OperationKind)x.Kind,
        Arg1 = x.Arg1,
        Arg2 = x.Arg2,
        Arg3 = x.Arg3,
        Week = x.Week,
        Attempt = x.Attempt,
        Turn = x.Turn,
    };

    private static ParamBonusSnapshotBlob ToBlob(ParamBonusSnapshot x) => new()
    {
        Percent = x.Percent,
        RemainingTurns = x.RemainingTurns,
        SkipNextDecrement = x.SkipNextDecrement,
    };

    private static ParamBonusSnapshot FromBlob(ParamBonusSnapshotBlob x) => new()
    {
        Percent = x.Percent,
        RemainingTurns = x.RemainingTurns,
        SkipNextDecrement = x.SkipNextDecrement,
    };

    private static PersistentSnapshotBlob ToBlob(PersistentSnapshot x) => new()
    {
        CardId = x.CardId,
        Enhanced = x.Enhanced,
        EffectIndex = x.EffectIndex,
        RemainingTurns = x.RemainingTurns,
        RemainingCount = x.RemainingCount,
        GrantOrder = x.GrantOrder,
        SkipNextDecrement = x.SkipNextDecrement,
    };

    private static PersistentSnapshot FromBlob(PersistentSnapshotBlob x) => new()
    {
        CardId = x.CardId,
        Enhanced = x.Enhanced,
        EffectIndex = x.EffectIndex,
        RemainingTurns = x.RemainingTurns,
        RemainingCount = x.RemainingCount,
        GrantOrder = x.GrantOrder,
        SkipNextDecrement = x.SkipNextDecrement,
    };

    private static LessonSnapshotBlob ToBlob(LessonSnapshot x) => new()
    {
        Kind = (int)x.Kind,
        TargetParam = (int)x.TargetParam,
        MaxTurns = x.MaxTurns,
        ClearTarget = x.ClearTarget,
        PerfectTarget = x.PerfectTarget,
        ExamTurnParams = x.ExamTurnParams.Select(v => (int)v).ToArray(),
        ExamTurnScoreBonusPercents = x.ExamTurnScoreBonusPercents,
        ExamEarlyEndScore = x.ExamEarlyEndScore,
        Deck = x.Deck.Select(v => ToBlob(v)).ToArray(),
        DrawPile = x.DrawPile,
        Hand = x.Hand,
        Discard = x.Discard,
        Exiled = x.Exiled,
        StatusValues = x.StatusValues,
        StatusSkipNextDecrement = x.StatusSkipNextDecrement,
        ParamBonuses = x.ParamBonuses.Select(v => ToBlob(v)).ToArray(),
        Persistents = x.Persistents.Select(v => ToBlob(v)).ToArray(),
        LessonEnhanced = x.LessonEnhanced,
        Turn = x.Turn,
        Phase = (int)x.Phase,
        Hp = x.Hp,
        MaxHp = x.MaxHp,
        Shield = x.Shield,
        UsesLeft = x.UsesLeft,
        ParamGained = x.ParamGained,
        NextGrantOrder = x.NextGrantOrder,
    };

    private static LessonSnapshot FromBlob(LessonSnapshotBlob x) => new()
    {
        Kind = (LessonKind)x.Kind,
        TargetParam = (ParamKind)x.TargetParam,
        MaxTurns = x.MaxTurns,
        ClearTarget = x.ClearTarget,
        PerfectTarget = x.PerfectTarget,
        ExamTurnParams = x.ExamTurnParams.Select(v => (ParamKind)v).ToArray(),
        ExamTurnScoreBonusPercents = x.ExamTurnScoreBonusPercents,
        ExamEarlyEndScore = x.ExamEarlyEndScore,
        Deck = x.Deck.Select(v => FromBlob(v)).ToArray(),
        DrawPile = x.DrawPile,
        Hand = x.Hand,
        Discard = x.Discard,
        Exiled = x.Exiled,
        StatusValues = x.StatusValues,
        StatusSkipNextDecrement = x.StatusSkipNextDecrement,
        ParamBonuses = x.ParamBonuses.Select(v => FromBlob(v)).ToArray(),
        Persistents = x.Persistents.Select(v => FromBlob(v)).ToArray(),
        LessonEnhanced = x.LessonEnhanced,
        Turn = x.Turn,
        Phase = (LessonPhase)x.Phase,
        Hp = x.Hp,
        MaxHp = x.MaxHp,
        Shield = x.Shield,
        UsesLeft = x.UsesLeft,
        ParamGained = x.ParamGained,
        NextGrantOrder = x.NextGrantOrder,
    };

    private static ProduceSnapshotBlob ToBlob(ProduceSnapshot x) => new()
    {
        ScenarioId = x.ScenarioId,
        IdolId = x.IdolId,
        PlayerLevel = x.PlayerLevel,
        SupportCards = x.SupportCards.Select(v => ToBlob(v)).ToArray(),
        Phase = (int)x.Phase,
        Week = x.Week,
        Attempt = x.Attempt,
        NextOperationNumber = x.NextOperationNumber,
        NextInstanceId = x.NextInstanceId,
        Hp = x.Hp,
        MaxHp = x.MaxHp,
        ExamRetryLeft = x.ExamRetryLeft,
        Parameters = x.Parameters,
        BonusPerThousand = x.BonusPerThousand,
        Deck = x.Deck.Select(v => ToBlob(v)).ToArray(),
        ClassOptionParams = x.ClassOptionParams,
        OfferCards = x.OfferCards,
        Lesson = x.Lesson is null ? null : ToBlob(x.Lesson),
        ExamBackup = x.ExamBackup is null ? null : ToBlob(x.ExamBackup),
        LastExam = x.LastExam is null ? null : ToBlob(x.LastExam),
        MidExam = x.MidExam is null ? null : ToBlob(x.MidExam),
        Result = x.Result is null ? null : ToBlob(x.Result),
        Operations = x.Operations.Select(v => ToBlob(v)).ToArray(),
    };

    private static ProduceSnapshot FromBlob(ProduceSnapshotBlob x) => new()
    {
        ScenarioId = x.ScenarioId,
        IdolId = x.IdolId,
        PlayerLevel = x.PlayerLevel,
        SupportCards = x.SupportCards.Select(v => FromBlob(v)).ToArray(),
        Phase = (ProducePhase)x.Phase,
        Week = x.Week,
        Attempt = x.Attempt,
        NextOperationNumber = x.NextOperationNumber,
        NextInstanceId = x.NextInstanceId,
        Hp = x.Hp,
        MaxHp = x.MaxHp,
        ExamRetryLeft = x.ExamRetryLeft,
        Parameters = x.Parameters,
        BonusPerThousand = x.BonusPerThousand,
        Deck = x.Deck.Select(v => FromBlob(v)).ToArray(),
        ClassOptionParams = x.ClassOptionParams,
        OfferCards = x.OfferCards,
        Lesson = x.Lesson is null ? null : FromBlob(x.Lesson),
        ExamBackup = x.ExamBackup is null ? null : FromBlob(x.ExamBackup),
        LastExam = x.LastExam is null ? null : FromBlob(x.LastExam),
        MidExam = x.MidExam is null ? null : FromBlob(x.MidExam),
        Result = x.Result is null ? null : FromBlob(x.Result),
        Operations = x.Operations.Select(v => FromBlob(v)).ToArray(),
    };
}
