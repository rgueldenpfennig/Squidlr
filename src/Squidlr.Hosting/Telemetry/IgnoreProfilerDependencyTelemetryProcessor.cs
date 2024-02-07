using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Squidlr.Hosting.Telemetry;

public sealed class IgnoreProfilerDependencyTelemetryProcessor : ITelemetryProcessor
{
    private readonly ITelemetryProcessor? _next;

    public IgnoreProfilerDependencyTelemetryProcessor(ITelemetryProcessor? next)
    {
        _next = next;
    }

    public void Process(ITelemetry item)
    {
        if (item is DependencyTelemetry dependencyTelemetry &&
            dependencyTelemetry.Target.Equals("profiler.monitor.azure.com", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _next?.Process(item);
    }
}
