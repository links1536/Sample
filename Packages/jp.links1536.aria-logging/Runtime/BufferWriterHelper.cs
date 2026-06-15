using System;
using System.Buffers;
using System.Text;

namespace Aria.Logging
{
	static class BufferWriterHelper
	{
		public static void Write(IBufferWriter<byte> writer, string message1)
		{
			if (string.IsNullOrEmpty(message1))
				return;
			Span<byte> span = writer.GetSpan(Encoding.UTF8.GetMaxByteCount(message1.Length));
			int bytes = Encoding.UTF8.GetBytes(message1.AsSpan(), span);
			writer.Advance(bytes);
		}

		public static void Write(IBufferWriter<byte> writer, string message1, string message2)
		{
			Span<byte> span = writer.GetSpan(Encoding.UTF8.GetMaxByteCount(message1.Length + message2.Length));
			int bytes = Encoding.UTF8.GetBytes(message1.AsSpan(), span);
			int bytes2 = Encoding.UTF8.GetBytes(message2.AsSpan(), span.Slice(bytes));
			writer.Advance(bytes + bytes2);
		}

		public static void Write(IBufferWriter<byte> writer, string message1, string message2, string message3)
		{
			Span<byte> span = writer.GetSpan(Encoding.UTF8.GetMaxByteCount(message1.Length + message2.Length + message3.Length));
			int bytes = Encoding.UTF8.GetBytes(message1.AsSpan(), span);
			int bytes2 = Encoding.UTF8.GetBytes(message2.AsSpan(), span.Slice(bytes));
			int bytes3 = Encoding.UTF8.GetBytes(message3.AsSpan(), span.Slice(bytes + bytes2));
			writer.Advance(bytes + bytes2 + bytes3);
		}

		public static void Write(IBufferWriter<byte> writer, ReadOnlySpan<char> message1)
		{
			if (message1.IsEmpty)
				return;

			Span<byte> span = writer.GetSpan(message1.Length);
			int bytes = Encoding.UTF8.GetBytes(message1, span);
			writer.Advance(bytes);
		}

		public static void Write(IBufferWriter<byte> writer, ReadOnlySpan<char> message1, string message2)
		{
			Span<byte> span = writer.GetSpan(Encoding.UTF8.GetMaxByteCount(message1.Length + message2.Length));
			int bytes = Encoding.UTF8.GetBytes(message1, span);
			int bytes2 = Encoding.UTF8.GetBytes(message2.AsSpan(), span.Slice(bytes));
			writer.Advance(bytes + bytes2);
		}

		public static void Write(IBufferWriter<byte> writer, ReadOnlySpan<char> message1, string message2, string message3)
		{
			Span<byte> span = writer.GetSpan(Encoding.UTF8.GetMaxByteCount(message1.Length + message2.Length + message3.Length));
			int bytes = Encoding.UTF8.GetBytes(message1, span);
			int bytes2 = Encoding.UTF8.GetBytes(message2.AsSpan(), span.Slice(bytes));
			int bytes3 = Encoding.UTF8.GetBytes(message3.AsSpan(), span.Slice(bytes + bytes2));
			writer.Advance(bytes + bytes2 + bytes3);
		}

		public static void Write(IBufferWriter<byte> writer, ReadOnlySpan<byte> message1)
		{
			if (message1.IsEmpty)
				return;

			writer.Write(message1);
		}

		public static void Write(IBufferWriter<byte> writer, ReadOnlySpan<byte> message1, string message2)
		{
			writer.Write(message1);

			Span<byte> span = writer.GetSpan(Encoding.UTF8.GetMaxByteCount(message2.Length));
			int bytes2 = Encoding.UTF8.GetBytes(message2.AsSpan(), span);
			writer.Advance(bytes2);
		}

		public static void Write(IBufferWriter<byte> writer, ReadOnlySpan<byte> message1, string message2, string message3)
		{
			writer.Write(message1);

			Span<byte> span = writer.GetSpan(Encoding.UTF8.GetMaxByteCount(message2.Length + message3.Length));
			int bytes2 = Encoding.UTF8.GetBytes(message2.AsSpan(), span);
			int bytes3 = Encoding.UTF8.GetBytes(message3.AsSpan(), span.Slice(bytes2));
			writer.Advance(bytes2 + bytes3);
		}

	}
}
