﻿using Microsoft.Extensions.DependencyInjection;

namespace Squidlr.Hosting.Telemetry;

public static class TelemetryHostBuilderExtensions
{
    public static IServiceCollection AddTelemetry(this IServiceCollection services, Action<TelemetryOptions> options)
    {
        services.AddApplicationInsightsTelemetry(options =>
        {
            options.EnableActiveTelemetryConfigurationSetup = true;
            options.EnableQuickPulseMetricStream = true;
            options.EnableAdaptiveSampling = false;
        });
        services.AddServiceProfiler();
        services.AddApplicationInsightsTelemetryProcessor<IgnoreProfilerDependencyTelemetryProcessor>();
        services.AddApplicationInsightsTelemetryProcessor<IgnorePathTelemetryProcessor>();
        services.Configure(options);

        return services;
    }
}
