using Grpc.Net.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace Uta.Server.Tests.Integration.Support;

public sealed class UtaServerFactory : WebApplicationFactory<Program>
{
    public UtaServerFactory(PostgresFixture postgres, DateTimeOffset? now = null)
    {
        // Program.cs が builder.Configuration から読むので、環境変数で渡す（ConfigureAppConfiguration は間に合わない）。
        // 環境変数は user-secrets より優先されるので、開発機のローカル DB には落ちない。コンストラクタで渡すので、フィクスチャの初期化前には作れない
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", postgres.ConnectionString);
        Time = new FakeTimeProvider(now ?? new DateTimeOffset(2026, 11, 1, 3, 0, 0, TimeSpan.Zero));
    }

    public FakeTimeProvider Time { get; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Time);
        });
    }

    public GrpcChannel CreateGrpcChannel()
    {
        var handler = new ResponseVersionHandler { InnerHandler = Server.CreateHandler() };
        return GrpcChannel.ForAddress(Server.BaseAddress, new GrpcChannelOptions { HttpHandler = handler });
    }

    // TestServer のレスポンスは HTTP/1.1 になるので、gRPC のクライアントが受け付けるよう、リクエストの版に合わせる
    private sealed class ResponseVersionHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);
            response.Version = request.Version;
            return response;
        }
    }
}
