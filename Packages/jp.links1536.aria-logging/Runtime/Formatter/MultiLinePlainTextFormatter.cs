using System;
using System.Buffers;
using System.Text;
using Microsoft.Extensions.Logging;
using Utf8StringInterpolation;
using ZLogger;
using ZLogger.Formatters;

namespace Aria.Logging.Formatter
{
	class MultiLinePlainTextFormatter : IZLoggerFormatter
	{
		static readonly byte[] LogLevelTrace	= EncodeLogLevelTag(LogLevel.Trace);
		static readonly byte[] LogLevelDebug	= EncodeLogLevelTag(LogLevel.Debug);
		static readonly byte[] LogLevelInfo		= EncodeLogLevelTag(LogLevel.Information);
		static readonly byte[] LogLevelWarning	= EncodeLogLevelTag(LogLevel.Warning);
		static readonly byte[] LogLevelError	= EncodeLogLevelTag(LogLevel.Error);
		static readonly byte[] LogLevelFatal	= EncodeLogLevelTag(LogLevel.Critical);
		static readonly byte[] LogLevelNone		= EncodeLogLevelTag(LogLevel.None);
		static readonly byte[] NewLine			= Encoding.UTF8.GetBytes(Environment.NewLine);

		static byte[] EncodeLogLevelTag(LogLevel logLevel)
		{
			string logLevelTag = (logLevel switch
			{
				LogLevel.Trace => "Trace",
				LogLevel.Debug => "Debug",
				LogLevel.Information => "Info",
				LogLevel.Warning => "Warning",
				LogLevel.Error => "Error",
				LogLevel.Critical => "Fatal",
				_ => string.Empty,
			});
			return Encoding.UTF8.GetBytes($"[ {logLevelTag, -7} ]");
		}

		static ReadOnlySpan<byte> GetLogLevelSpan(LogLevel logLevel)
			=> (logLevel switch
			{
				LogLevel.Trace => LogLevelTrace,
				LogLevel.Debug => LogLevelDebug,
				LogLevel.Information => LogLevelInfo,
				LogLevel.Warning => LogLevelWarning,
				LogLevel.Error => LogLevelError,
				LogLevel.Critical => LogLevelFatal,
				_ => LogLevelNone
			}).AsSpan();

		public bool WithLineBreak => true;

		public void FormatLogEntry(IBufferWriter<byte> writer, IZLoggerEntry entry)
		{
			// outにでるWriterに書き込む→WriterをFlushで返り値のUtf8StringBuilderに値が入る
			using var prefixBuffer = Utf8String.CreateWriter(out var prefixWriter);
			WritePrefix(prefixWriter, entry.LogInfo);

			// メッセージとして渡された本文
			using var bodyBuffer = Utf8String.CreateWriter(out var bodyWriter);
			entry.ToString(bodyWriter.GetBufferWriter());

			// ボディを確定させる
			bodyWriter.Flush();

			// ボディの下に例外ログを本文として書きだす
			var exception = entry.LogInfo.Exception;
			if (exception != null)
				WriteExceptionLog(bodyWriter, exception);

			// ボディを確定させる
			bodyWriter.Flush();

			// ボディに対して行ごとにプレフィックスを付ける
			var body = bodyBuffer.WrittenSpan;
			bool firstLine = true;
			int lineStart = 0;
			for (int i = 0; i < body.Length; i++)
			{
				byte b = body[i];

				// 次の改行コードまでの文字数を測る
				if (b != (byte)'\r' && b != (byte)'\n')
					continue;

				// 1行分出力
				int lineLength = i - lineStart;
				WriteLine(ref firstLine, writer, prefixBuffer.WrittenSpan, body.Slice(lineStart, lineLength));

				// CRLFの時はもう1文字だけずらしておく
				if (b == (byte)'\r' && i + 1 < body.Length && body[i + 1] == (byte)'\n')
					i++;

				// 次の行は次の文字からはじめる
				lineStart = i + 1;
			}

			// 最終行
			if (lineStart < body.Length)
			{
				WriteLine(ref firstLine, writer, prefixBuffer.WrittenSpan, body.Slice(lineStart));
			}
		}

		static void WritePrefix(Utf8StringWriter<ArrayBufferWriter<byte>> prefixWriter, in LogInfo logInfo)
		{
			var time = logInfo.Timestamp.Local;
			var logLevel = logInfo.LogLevel;
			var category = logInfo.Category;
			prefixWriter.AppendFormatted($"[ {time:yyyy/MM/dd HH:mm:ss} ]");
			prefixWriter.AppendUtf8(GetLogLevelSpan(logLevel));
			prefixWriter.AppendFormatted($"[ {category,-16} ]");
			prefixWriter.AppendWhitespace(1);
			prefixWriter.Flush();
		}

		static void WriteLine(ref bool firstLine, IBufferWriter<byte> writer, ReadOnlySpan<byte> prefix, ReadOnlySpan<byte> body)
		{
			if (!firstLine)
				writer.Write(NewLine);

			writer.Write(prefix);
			writer.Write(body);

			firstLine = false;
		}

		static void WriteExceptionLoggingCore(IBufferWriter<byte> writer, Exception exception)
		{
			string fullName = exception.GetType().FullName;
			string message = exception.Message;
			Exception innerException = exception.InnerException;
			string stackTrace = exception.StackTrace;

			BufferWriterHelper.Write(writer, fullName, ": ", message ?? "");
			if (innerException != null)
			{
				BufferWriterHelper.Write(writer, NewLine.AsSpan(), " ---> ");
				WriteExceptionLoggingCore(writer, innerException);
				BufferWriterHelper.Write(writer, NewLine.AsSpan(), "   --- End of inner exception stack trace ---");
			}

			if (stackTrace != null)
			{
				BufferWriterHelper.Write(writer, NewLine.AsSpan(), stackTrace);
			}
		}

		static void WriteExceptionLog(Utf8StringWriter<ArrayBufferWriter<byte>> writer, Exception exception)
		{
			writer.AppendLine();

			AppendException(writer, exception);

			if (exception is AggregateException aggregateExceptions)
			{
				var innerExceptions = aggregateExceptions.InnerExceptions;
				int count = innerExceptions.Count;
				for (int i = 0; i < count; i++)
				{
					writer.AppendLine($"InnerException[{i}]");
					AppendInnerException(writer, innerExceptions[i]);
				}
			}
			else
			{
				AppendInnerException(writer, exception.InnerException);
			}
		}

		static void AppendInnerException(Utf8StringWriter<ArrayBufferWriter<byte>> writer, Exception? innnerException)
		{
			while (innnerException != null)
			{
				writer.AppendLine(ILoggerExtension.HorizontalLine);
				writer.AppendLine("=> InnerException");
				AppendException(writer, innnerException);
				innnerException = innnerException.InnerException;
			}
		}

		static void AppendException(Utf8StringWriter<ArrayBufferWriter<byte>> writer, Exception? exception)
		{
			if (exception == null)
				return;
			writer.AppendLine(ILoggerExtension.HorizontalLine);
			writer.AppendLine(exception.GetType().Name);
			writer.AppendLine(exception.Message);
			writer.AppendLine(ILoggerExtension.HorizontalLine);

			if (exception.StackTrace != null)
				writer.AppendLine(exception.StackTrace);
		}

	}
}
