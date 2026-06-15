using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aria.Common.IO
{
	public static class TrafficUtils
	{
		// 通信量は1000単位で統一
		public const long SizeUnit = 1000;

		public const long Kiro = SizeUnit;
		public const long Mega = SizeUnit * SizeUnit;
		public const long Giga = SizeUnit * SizeUnit * SizeUnit;
		public const long Tera = SizeUnit * SizeUnit * SizeUnit * SizeUnit;

		public static double ToKiro(long bytes)
			=> (double)bytes / Kiro;
		public static double ToMega(long bytes)
			=> (double)bytes / Mega;
		public static double ToGiga(long bytes)
			=> (double)bytes / Giga;
		public static double ToTera(long bytes)
			=> (double)bytes / Tera;

		public static string HumanReadable(long bytes, int digit)
		{
			long bits = bytes * 8;
			if (bits > Tera) return string.Format($"{{0:0.{string.Concat('#', digit)}}}Tbps", (double)bits / Tera);
			if (bits > Giga) return string.Format($"{{0:0.{string.Concat('#', digit)}}}Gbps", (double)bits / Giga);
			if (bits > Mega) return string.Format($"{{0:0.{string.Concat('#', digit)}}}Mbps", (double)bits / Mega);
			if (bits > Kiro) return string.Format($"{{0:0.{string.Concat('#', digit)}}}Kbps", (double)bits / Kiro);
			 return $"{bits}bps";
		}
	}
}
