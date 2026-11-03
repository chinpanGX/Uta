using MessagePack;

namespace Uta.Server.Infrastructure.Produce;

/// <summary>state 列の保存の形（produce-server.md 3章）。</summary>
[MessagePackObject]
public sealed class ProduceStateBlob
{
    [Key(0)]
    public ulong MasterSeed { get; set; }

    [Key(1)]
    public RandomStreamBlob[] Streams { get; set; } = [];

    [Key(2)]
    public ProduceSnapshotBlob Snapshot { get; set; } = new();
}

/// <summary>乱数ストリーム 1 本の保存の形。</summary>
[MessagePackObject]
public sealed class RandomStreamBlob
{
    [Key(0)]
    public int Kind { get; set; }

    [Key(1)]
    public int Week { get; set; }

    [Key(2)]
    public int Attempt { get; set; }

    [Key(3)]
    public ulong S0 { get; set; }

    [Key(4)]
    public ulong S1 { get; set; }

    [Key(5)]
    public ulong S2 { get; set; }

    [Key(6)]
    public ulong S3 { get; set; }

    [Key(7)]
    public long DrawCount { get; set; }
}

/// <summary>SupportCardSnapshot の保存の形。</summary>
[MessagePackObject]
public sealed class SupportCardSnapshotBlob
{
    [Key(0)]
    public int CardId { get; set; }

    [Key(1)]
    public int Level { get; set; }

    [Key(2)]
    public int LimitBreak { get; set; }
}

/// <summary>CardInstanceSnapshot の保存の形。</summary>
[MessagePackObject]
public sealed class CardInstanceSnapshotBlob
{
    [Key(0)]
    public int InstanceId { get; set; }

    [Key(1)]
    public int CardId { get; set; }

    [Key(2)]
    public bool Enhanced { get; set; }
}

/// <summary>ExamReportSnapshot の保存の形。</summary>
[MessagePackObject]
public sealed class ExamReportSnapshotBlob
{
    [Key(0)]
    public int Kind { get; set; }

    [Key(1)]
    public int Attempt { get; set; }

    [Key(2)]
    public int Score { get; set; }

    [Key(3)]
    public int Rank { get; set; }

    [Key(4)]
    public int[] NpcScores { get; set; } = [];

    [Key(5)]
    public bool Passed { get; set; }
}

/// <summary>ExamBackupSnapshot の保存の形。</summary>
[MessagePackObject]
public sealed class ExamBackupSnapshotBlob
{
    [Key(0)]
    public int[] Parameters { get; set; } = [];

    [Key(1)]
    public int Hp { get; set; }

    [Key(2)]
    public CardInstanceSnapshotBlob[] Deck { get; set; } = [];

    [Key(3)]
    public int NextInstanceId { get; set; }
}

/// <summary>ProduceResultSnapshot の保存の形。</summary>
[MessagePackObject]
public sealed class ProduceResultSnapshotBlob
{
    [Key(0)]
    public int Outcome { get; set; }

    [Key(1)]
    public int EvaluationValue { get; set; }

    [Key(2)]
    public int Rank { get; set; }

    [Key(3)]
    public int FinalScore { get; set; }

    [Key(4)]
    public int FinalRank { get; set; }
}

/// <summary>OperationSnapshot の保存の形。</summary>
[MessagePackObject]
public sealed class OperationSnapshotBlob
{
    [Key(0)]
    public int Number { get; set; }

    [Key(1)]
    public int Kind { get; set; }

    [Key(2)]
    public int Arg1 { get; set; }

    [Key(3)]
    public int Arg2 { get; set; }

    [Key(4)]
    public int Arg3 { get; set; }

    [Key(5)]
    public int Week { get; set; }

    [Key(6)]
    public int Attempt { get; set; }

    [Key(7)]
    public int Turn { get; set; }
}

/// <summary>ParamBonusSnapshot の保存の形。</summary>
[MessagePackObject]
public sealed class ParamBonusSnapshotBlob
{
    [Key(0)]
    public int Percent { get; set; }

    [Key(1)]
    public int RemainingTurns { get; set; }

    [Key(2)]
    public bool SkipNextDecrement { get; set; }
}

/// <summary>PersistentSnapshot の保存の形。</summary>
[MessagePackObject]
public sealed class PersistentSnapshotBlob
{
    [Key(0)]
    public int CardId { get; set; }

    [Key(1)]
    public bool Enhanced { get; set; }

    [Key(2)]
    public int EffectIndex { get; set; }

    [Key(3)]
    public int RemainingTurns { get; set; }

    [Key(4)]
    public int RemainingCount { get; set; }

    [Key(5)]
    public long GrantOrder { get; set; }

    [Key(6)]
    public bool SkipNextDecrement { get; set; }
}

/// <summary>LessonSnapshot の保存の形。</summary>
[MessagePackObject]
public sealed class LessonSnapshotBlob
{
    [Key(0)]
    public int Kind { get; set; }

    [Key(1)]
    public int TargetParam { get; set; }

    [Key(2)]
    public int MaxTurns { get; set; }

    [Key(3)]
    public int ClearTarget { get; set; }

    [Key(4)]
    public int PerfectTarget { get; set; }

    [Key(5)]
    public int[] ExamTurnParams { get; set; } = [];

    [Key(6)]
    public int[] ExamTurnScoreBonusPercents { get; set; } = [];

    [Key(7)]
    public int ExamEarlyEndScore { get; set; }

    [Key(8)]
    public CardInstanceSnapshotBlob[] Deck { get; set; } = [];

    [Key(9)]
    public int[] DrawPile { get; set; } = [];

    [Key(10)]
    public int[] Hand { get; set; } = [];

    [Key(11)]
    public int[] Discard { get; set; } = [];

    [Key(12)]
    public int[] Exiled { get; set; } = [];

    [Key(13)]
    public int[] StatusValues { get; set; } = [];

    [Key(14)]
    public bool[] StatusSkipNextDecrement { get; set; } = [];

    [Key(15)]
    public ParamBonusSnapshotBlob[] ParamBonuses { get; set; } = [];

    [Key(16)]
    public PersistentSnapshotBlob[] Persistents { get; set; } = [];

    [Key(17)]
    public int[] LessonEnhanced { get; set; } = [];

    [Key(18)]
    public int Turn { get; set; }

    [Key(19)]
    public int Phase { get; set; }

    [Key(20)]
    public int Hp { get; set; }

    [Key(21)]
    public int MaxHp { get; set; }

    [Key(22)]
    public int Shield { get; set; }

    [Key(23)]
    public int UsesLeft { get; set; }

    [Key(24)]
    public int ParamGained { get; set; }

    [Key(25)]
    public long NextGrantOrder { get; set; }
}

/// <summary>ProduceSnapshot の保存の形。</summary>
[MessagePackObject]
public sealed class ProduceSnapshotBlob
{
    [Key(0)]
    public int ScenarioId { get; set; }

    [Key(1)]
    public int IdolId { get; set; }

    [Key(2)]
    public int PlayerLevel { get; set; }

    [Key(3)]
    public SupportCardSnapshotBlob[] SupportCards { get; set; } = [];

    [Key(4)]
    public int Phase { get; set; }

    [Key(5)]
    public int Week { get; set; }

    [Key(6)]
    public int Attempt { get; set; }

    [Key(7)]
    public int NextOperationNumber { get; set; }

    [Key(8)]
    public int NextInstanceId { get; set; }

    [Key(9)]
    public int Hp { get; set; }

    [Key(10)]
    public int MaxHp { get; set; }

    [Key(11)]
    public int ExamRetryLeft { get; set; }

    [Key(12)]
    public int[] Parameters { get; set; } = [];

    [Key(13)]
    public int[] BonusPerThousand { get; set; } = [];

    [Key(14)]
    public CardInstanceSnapshotBlob[] Deck { get; set; } = [];

    [Key(15)]
    public int[] ClassOptionParams { get; set; } = [];

    [Key(16)]
    public int[] OfferCards { get; set; } = [];

    [Key(17)]
    public LessonSnapshotBlob? Lesson { get; set; }

    [Key(18)]
    public ExamBackupSnapshotBlob? ExamBackup { get; set; }

    [Key(19)]
    public ExamReportSnapshotBlob? LastExam { get; set; }

    [Key(20)]
    public ExamReportSnapshotBlob? MidExam { get; set; }

    [Key(21)]
    public ProduceResultSnapshotBlob? Result { get; set; }

    [Key(22)]
    public OperationSnapshotBlob[] Operations { get; set; } = [];
}
