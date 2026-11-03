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
    private readonly string? appVersion;

    private PlayerClient(GrpcChannel channel, string playerId, string token, string? appVersion)
    {
        this.channel = channel;
        PlayerId = playerId;
        this.token = token;
        this.appVersion = appVersion;
    }

    public string PlayerId { get; }

    public static async Task<PlayerClient> CreateAsync(GrpcChannel channel, string secret = "0123456789abcdef-secret", string? appVersion = "1")
    {
        var device = MagicOnionClient.Create<IDeviceService>(channel);
        var registered = await device.RegisterAsync(secret);
        var issued = await device.IssueTokenAsync(registered.DeviceId, secret);
        return new PlayerClient(channel, registered.PlayerId, issued.AccessToken, appVersion);
    }

    /// <summary>x-app-version を差し替えた同じプレイヤーのクライアント。null なら付けない。</summary>
    public PlayerClient WithAppVersion(string? version) => new PlayerClient(channel, PlayerId, token, version);

    /// <summary>別のチャンネル（再起動したサーバーなど）に同じプレイヤーのままつなぐ。</summary>
    public PlayerClient OnChannel(GrpcChannel other) => new PlayerClient(other, PlayerId, token, appVersion);

    /// <summary>同じチャンネルで IPlayerService を呼ぶ。requestId が null なら x-request-id を付けない。</summary>
    public Task<T> CallAsync<T>(Func<IPlayerService, UnaryResult<T>> call, string? requestId)
    {
        var headers = new Metadata { { ApiHeaders.Authorization, "Bearer " + token } };
        if (requestId is not null)
        {
            headers.Add(ApiHeaders.RequestId, requestId);
        }

        if (appVersion is not null)
        {
            headers.Add(ApiHeaders.AppVersion, appVersion);
        }

        var client = MagicOnionClient.Create<IPlayerService>(channel).WithHeaders(headers);
        return Invoke(call, client);
    }

    private static async Task<T> Invoke<T>(Func<IPlayerService, UnaryResult<T>> call, IPlayerService client)
    {
        return await call(client);
    }

    /// <summary>任意のサービスを呼ぶ汎用版。requestId が null なら x-request-id を付けない。</summary>
    public async Task<T> CallAsync<TService, T>(Func<TService, UnaryResult<T>> call, string? requestId)
        where TService : IService<TService>
    {
        var headers = new Metadata { { ApiHeaders.Authorization, "Bearer " + token } };
        if (requestId is not null)
        {
            headers.Add(ApiHeaders.RequestId, requestId);
        }

        if (appVersion is not null)
        {
            headers.Add(ApiHeaders.AppVersion, appVersion);
        }

        var client = MagicOnionClient.Create<TService>(channel).WithHeaders(headers);
        return await call(client);
    }
}
