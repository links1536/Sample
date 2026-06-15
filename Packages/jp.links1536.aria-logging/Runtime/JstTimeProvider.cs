using System;

namespace Aria.Logging
{
	public sealed class JstTimeProvider : TimeProvider
	{
		static readonly TimeZoneInfo JstTimeZone =
			TimeZoneInfo.CreateCustomTimeZone(
				"Asia/Tokyo",
				TimeSpan.FromHours(9),
				"Japan Standard Time",
				"Japan Standard Time"
			);

		public override DateTimeOffset GetUtcNow()
			=> DateTimeOffset.UtcNow;

		public override TimeZoneInfo LocalTimeZone
			=> JstTimeZone;
	}

}
