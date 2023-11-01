using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Options;

namespace Squidlr.Hosting.Telemetry
{
    /// <summary>
    /// Prevents certain paths to be sent to Application Insights.
    /// </summary>
    /// <remarks>https://learn.microsoft.com/en-us/azure/azure-monitor/app/api-filtering-sampling</remarks>
    public sealed class IgnorePathTelemetryProcessor : ITelemetryProcessor
    {
        private readonly ITelemetryProcessor? _next;
        private readonly TelemetryOptions _options;

        public IgnorePathTelemetryProcessor(ITelemetryProcessor? next, IOptions<TelemetryOptions> options)
        {
            _next = next;
            _options = options.Value;
        }

        public void Process(ITelemetry item)
        {
            if (_options.IgnoreAbsolutePaths.Length != 0 &&
                item is RequestTelemetry requestTelemetry)
            {
                for (var i = 0; i < _options.IgnoreAbsolutePaths.Length; i++)
                {
                    if (requestTelemetry.Url.AbsolutePath.Contains(_options.IgnoreAbsolutePaths[i]))
                    {
                        return;
                    }
                }
            }

            _next?.Process(item);
        }
    }
}
