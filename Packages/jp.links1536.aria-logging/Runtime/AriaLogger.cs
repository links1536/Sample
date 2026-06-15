
using Aria.Logging;
using Microsoft.Extensions.Logging;

namespace Aria.Logging
{
	public static class AriaLogger<T>
	{
		static readonly ILogger m_Logger = LoggerHandler.CreateLogger<T>();

		public static ILogger Get()
			=> m_Logger;
	}
}
