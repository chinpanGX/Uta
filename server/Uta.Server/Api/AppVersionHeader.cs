using System.Globalization;
using MagicOnion.Server;
using Uta.Shared.Contracts.Produce;
using VolTech.Server;
using VolTech.Server.Abstractions;

namespace Uta.Server.Api;

/// <summary>x-app-version ヘッダーの読み取り。</summary>
public static class AppVersionHeader
{
    /// <summary>整数で読めなければ null。</summary>
    public static int? Read(ServiceContext context)
    {
        var value = context.CallContext.RequestHeaders.GetValue(ApiHeaders.AppVersion);
        return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var version) ? version : null;
    }

    public static int Require(ServiceContext context) =>
        Read(context) ?? throw new BusinessException(ProduceErrors.InvalidArgument, "x-app-version is required");
}
