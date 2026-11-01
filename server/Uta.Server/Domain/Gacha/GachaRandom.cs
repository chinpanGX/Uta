using System.Buffers.Binary;
using System.Security.Cryptography;
using Uta.Shared.Domain.Rng;

namespace Uta.Server.Domain.Gacha;

public static class GachaRandom
{
    /// <summary>テストと SecureRandomSource 用。SplitMix64 で 4 回引いて状態にする（Xoshiro256StarStar.FromSeed）。</summary>
    public static IRandom FromSeed(ulong seed) => Xoshiro256StarStar.FromSeed(seed);

    /// <summary>暗号用乱数（RandomNumberGenerator）で 64 ビットのシードを作る。保存しない。</summary>
    public static IRandom CreateSecure()
    {
        Span<byte> buffer = stackalloc byte[8];
        RandomNumberGenerator.Fill(buffer);
        return FromSeed(BinaryPrimitives.ReadUInt64LittleEndian(buffer));
    }
}
