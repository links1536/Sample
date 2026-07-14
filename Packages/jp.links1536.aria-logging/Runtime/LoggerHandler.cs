using Aria.Logging.Formatter;
using Microsoft.Extensions.Logging;
using ZLogger;
using ZLogger.Unity;

namespace Aria.Logging
{
	static class TypeNameCaching<T>
	{
		static string m_TypeName;

		public static string TypeName
			=> m_TypeName;

		static TypeNameCaching()
		{
			m_TypeName = typeof(T).Name;
		}
	}

	public static class LoggerHandler
	{
		static ILoggerFactory m_LoggerFactory;
		static ILogger m_Logger;

		public static ILoggerFactory DefaultLoggerFactory
			=> m_LoggerFactory;

		public static ILogger DefaultLogger
			=> m_Logger;


		static string LogDirectory = "./";

		[UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.AfterAssembliesLoaded)]
#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
#endif
		static void Initialize()
		{
#if UNITY_EDITOR
			// エディタではカレントディレクトリ内につくる
			LogDirectory = System.Environment.CurrentDirectory;
#else
			// 実行フォルダと書き込めるフォルダが違う場合
			LogDirectory = UnityEngine.Application.persistentDataPath;
#endif

			m_LoggerFactory = LoggerFactory.Create(builder =>
			{
#if DEBUG
				var minLevel = LogLevel.Trace;
#else
				var minLevel = LogLevel.Information;
#endif
				builder
				.ClearProviders()
				.SetMinimumLevel(minLevel)
				.AddZLoggerUnityDebug(option =>
				{
					option.UseFormatter(() => new UnityLogPlainTextFormatter());
				})
				.AddZLoggerRollingFile(option =>
				{
					// 従来の形式のログをファイルに吐き出す設定
					option.UseFormatter(() => new MultiLinePlainTextFormatter());
					option.FilePathSelector = static (timestamp, sequenceNumber) => $"{LogDirectory}/Logs/{timestamp:yyyy-MM-dd}_{sequenceNumber}.txt";
					option.RollingInterval = ZLogger.Providers.RollingInterval.Month;
					option.TimeProvider = new JstTimeProvider();
				});
			});
			m_Logger = m_LoggerFactory.CreateLogger(string.Empty);
		}

		public static ILogger CreateLogger<T>()
			=> m_LoggerFactory.CreateLogger(TypeNameCaching<T>.TypeName);

		public static ILogger CreateLogger(string name)
			=> m_LoggerFactory.CreateLogger(name);
	}
}
