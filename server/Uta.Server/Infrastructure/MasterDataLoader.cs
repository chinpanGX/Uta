using MasterMemory.Addons;
using Uta.MasterData;

namespace Uta.Server.Infrastructure;

/// <summary>埋め込みのマスタのバイナリ（masterdata.bytes）を読む。</summary>
public static class MasterDataLoader
{
    private const string ResourceName = "Uta.Server.MasterData.masterdata.bytes";

    public static MemoryDatabase Load()
    {
        using var stream = typeof(MasterDataLoader).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"埋め込みリソース {ResourceName} がない");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return Load(buffer.ToArray());
    }

    public static MemoryDatabase Load(byte[] bytes)
    {
        try
        {
            return new MemoryDatabase(MasterDataBinary.Read(bytes, MasterDataSchema.Hash));
        }
        catch (MasterDataFormatException exception)
        {
            // スキーマのハッシュ（暗号化の鍵）はメッセージに入れない（masterdata-pipeline.md 6章）
            throw new InvalidOperationException($"マスタのバイナリを読めない（{exception.Reason}）", exception);
        }
    }
}
