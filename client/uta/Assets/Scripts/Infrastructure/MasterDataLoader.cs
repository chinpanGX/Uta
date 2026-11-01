using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MasterMemory.Addons;
using MessagePack;
using UnityEngine.Networking;
using Uta.MasterData;

namespace Uta.Client.Infrastructure
{
    /// <summary>StreamingAssets のマスタのバイナリ（masterdata.bytes）を読む。</summary>
    public static class MasterDataLoader
    {
        public const string RelativePath = "MasterData/masterdata.bytes";

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
            catch (MessagePackSerializationException exception)
            {
                throw new InvalidOperationException("マスタのバイナリを読めない（中身を解釈できない）", exception);
            }
        }

        public static async UniTask<MemoryDatabase> LoadFromStreamingAssetsAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = UnityEngine.Application.streamingAssetsPath + "/" + RelativePath;
            var uri = path.Contains("://") ? path : new Uri(path).AbsoluteUri;
            using (var request = UnityWebRequest.Get(uri))
            {
                await request.SendWebRequest().WithCancellation(cancellationToken);
                return Load(request.downloadHandler.data);
            }
        }
    }
}
