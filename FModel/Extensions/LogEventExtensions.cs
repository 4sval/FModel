using Serilog.Events;

namespace FModel.Extensions;

public static class LogEventExtensions
{
    public static string GetContext(this LogEvent log, string propertyName)
    {
        return log.Properties.TryGetValue(propertyName, out var value) ? value.ToString().Trim('"') : string.Empty;
    }
}
