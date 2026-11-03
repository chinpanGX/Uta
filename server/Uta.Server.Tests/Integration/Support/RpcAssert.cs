using System.Text;
using Grpc.Core;
using VolTech.Server.Abstractions;

namespace Uta.Server.Tests.Integration.Support;

public static class RpcAssert
{
    /// <summary>gRPC のステータスと、grpc-status-details-bin に入った reason を確かめる。</summary>
    public static void Reason(RpcException exception, StatusCode code, string reason)
    {
        Assert.Equal(code, exception.StatusCode);
        foreach (var entry in exception.Trailers)
        {
            if (entry.Key == ApiHeaders.ErrorDetailsTrailer && entry.IsBinary)
            {
                Assert.Contains(reason, Encoding.UTF8.GetString(entry.ValueBytes));
                return;
            }
        }

        throw new InvalidOperationException("grpc-status-details-bin が trailers にない");
    }
}
