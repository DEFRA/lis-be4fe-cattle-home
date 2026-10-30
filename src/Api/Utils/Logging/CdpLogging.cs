// <copyright file="CdpLogging.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Utils.Logging;

using System.Diagnostics.CodeAnalysis;
using Defra.Lis.Be4Fe.Api.Utils.Auditing;
using Elastic.Serilog.Enrichers.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

public static class CdpLogging
{
    [ExcludeFromCodeCoverage]
    public static void Configuration(HostBuilderContext ctx, LoggerConfiguration config)
    {
        var httpAccessor = ctx.Configuration.Get<HttpContextAccessor>();
        var traceIdHeader = ctx.Configuration.GetValue<string>("TraceHeader");

        var mainLogger = new LoggerConfiguration()
            .ReadFrom.Configuration(ctx.Configuration)
            .Enrich.WithEcsHttpContext(httpAccessor!)
            .Enrich.FromLogContext()
            .Filter.With<AuditLogger.Filters.ExcludeAuditEvents>()
            .CreateLogger();

        // Serilog applies MinimumLevel overrides where an event is created, never in a sub-logger, so the
        // Serilog:MinimumLevel settings must be read onto this logger too or the overrides do nothing.
        config.ReadFrom.Configuration(MinimumLevelSettings(ctx.Configuration));

        if (traceIdHeader != null)
        {
            config.Enrich.WithCorrelationId(traceIdHeader);
        }

        var auditLogger = AuditLogger.CreateAuditLogger();

        config
            .WriteTo.Logger(mainLogger)
            .WriteTo.Logger(auditLogger);
    }

    private static IConfiguration MinimumLevelSettings(IConfiguration configuration) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(configuration.GetSection("Serilog:MinimumLevel").AsEnumerable())
            .Build();
}
