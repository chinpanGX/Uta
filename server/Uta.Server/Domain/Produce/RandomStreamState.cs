using Uta.Shared.Domain.Rng;

namespace Uta.Server.Domain.Produce;

/// <summary>乱数ストリーム 1 本の保存用の状態（xoshiro256** の状態 4 つと、引いた回数）。</summary>
public sealed record RandomStreamState(RandomStreamKey Key, ulong S0, ulong S1, ulong S2, ulong S3, long DrawCount);
