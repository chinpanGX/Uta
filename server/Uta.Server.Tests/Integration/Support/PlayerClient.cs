using Grpc.Core;
using Grpc.Net.Client;
using MagicOnion;
using MagicOnion.Client;
using Uta.Shared.Contracts.Player;
using VolTech.Server.Abstractions;
using VolTech.Server.Auth;

namespace Uta.Server.Tests.Integration.Support;

/// <summary>端末を登録してトークンを発行し、認証つきで <see cref="IPlayerService" /> を呼ぶテスト支援。</summary>
public sealed class PlayerClient
{
    private readonly GrpcChannel channel;
    private readonly string token;

    private PlayerClient(GrpcChannel channel, string playerId, string token)
    {
        this.channel = channel;
        PlayerId = playerId;
        this.token = token;
    }

    public string PlayerId { get; }

    public static async Task<PlayerClient> CreateAsync(GrpcChannel channel, string secret = "0123456789abcdef-secret")
    {
        var device = MagicOnionClient.Create<IDeviceService>(channel);
        var registered = await device.RegisterAsync(secret);
        var issued = await device.IssueTokenAsync(registered.DeviceId, secret);
        return new PlayerClient(channel, registered.PlayerId, issued.AccessToken);
    }

    /// <summary>同じチャンネルで IPlayerService を呼ぶ。requestId が null なら x-request-id を付けない。</summary>
    public Task<T> CallAsync<T>(Func<IPlayerService, UnaryResult<T>> call, string? requestId)
    {
        var headers = new Metadata { { ApiHeaders.Authorization, "Bearer " + token } };
        if (requestId is not null)
        {
            headers.Add(ApiHeaders.RequestId, requestId);
        }

        var client = MagicOnionClient.Create<IPlayerService>(channel).WithHeaders(headers);
        return Invoke(call, client);
    }

    private static async Task<T> Invoke<T>(Func<IPlayerService, UnaryResult<T>> call, IPlayerService client)
    {
        return await call(client);
    }
}
