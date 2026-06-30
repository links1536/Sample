namespace Links.Licenses.Parser
{
	static class ParserUtils
	{
		static char[] Lines = new char[] { '\r', '\n' };

		public static string TrimLines(string text)
			=> text.TrimStart(Lines).TrimEnd(Lines);
	}
}
