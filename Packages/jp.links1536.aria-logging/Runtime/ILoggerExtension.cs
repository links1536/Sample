using Microsoft.Extensions.Logging;
using ZLogger;

namespace Aria.Logging
{
	public static class ILoggerExtension
	{
		internal static readonly string HorizontalLine = $"--------------------------------------------------------";

		//public static void ZLogLine(this ILogger logger, LogLevel level)
		//	=> logger.ZLog(level, $"--------------------------------------------------------");
		//public static void ZLogLine(this ILogger logger)
		//	=> logger.ZLogInformation($"--------------------------------------------------------");
	}
}
