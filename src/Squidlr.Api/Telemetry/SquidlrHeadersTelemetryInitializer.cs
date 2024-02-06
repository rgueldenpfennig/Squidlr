using Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Squidlr.Shared;

namespace Squidlr.Api.Telemetry;

public sealed class SquidlrHeadersTelemetryInitializer : TelemetryInitializerBase
{

    public SquidlrHeadersTelemetryInitializer(IHttpContextAccessor httpContextAccessor)
             : base(httpContextAccessor)
    {
    }

    protected override void OnInitializeTelemetry(HttpContext platformContext, RequestTelemetry requestTelemetry, ITelemetry telemetry)
    {
        telemetry.Context.Session.Id = platformContext.Request.Headers[SquidlrHeaderNames.SessionId];
        telemetry.Context.User.Id = platformContext.Request.Headers[SquidlrHeaderNames.UserId];
    }
}
