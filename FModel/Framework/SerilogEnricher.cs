using System;
using System.Diagnostics;
using System.Reflection;
using Serilog.Core;
using Serilog.Events;

namespace FModel.Framework;

public abstract class SerilogEnricher : ILogEventEnricher
{
    public abstract void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory);

    protected bool TryGetCaller(out MethodBase method)
    {
        method = null;

        var serilogAssembly = typeof(Serilog.Log).Assembly;
        var stack = new StackTrace(3);

        foreach (var frame in stack.GetFrames())
        {
            var m = frame.GetMethod();
            if (m?.DeclaringType is null) continue;

            if (m.DeclaringType.Assembly != serilogAssembly)
            {
                method = m;
                break;
            }
        }

        return method != null;
    }
}

public class SourceEnricher : SerilogEnricher
{
    public override void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var source = "N/A";
        if (logEvent.Properties.TryGetValue("SourceContext", out var sourceContext))
        {
            source = sourceContext.ToString()[1..^1];
        }
        else if (TryGetCaller(out var method))
        {
            source = method.DeclaringType?.Namespace;
        }

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("Enriched", source.Split('.')[0]));
    }
}

public class CallerEnricher : SerilogEnricher
{
    public override void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (TryGetCaller(out var method))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("Enriched", $"{method.DeclaringType?.FullName}.{method.Name}"));
        }
    }
}
