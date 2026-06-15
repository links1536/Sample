using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Aria.Diagnostics
{
	public struct StopwatchArea : IDisposable
	{
		ILogger m_Logger;
		Stopwatch m_Stopwatch;
		string m_Name;

		public StopwatchArea(ILogger logger, string name)
		{
			m_Name = name;
			m_Logger = logger;
			m_Logger.LogTrace($"[{m_Name} Start]");

			m_Stopwatch = new Stopwatch();
			m_Stopwatch.Start();
		}
		public void Dispose()
		{
			m_Stopwatch.Stop();
			m_Logger.LogTrace($"[{m_Name} End] {m_Stopwatch.Elapsed}");
		}
	}
}
