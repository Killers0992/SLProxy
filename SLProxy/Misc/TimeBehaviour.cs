using System;

namespace SLProxy.Misc
{
	public static class TimeBehaviour
	{
		/// <summary>
		/// Current UTC timestamp (.NET Ticks)
		/// </summary>
		/// <returns>UTC.Now in ticks</returns>
		public static long CurrentTimestamp() => DateTime.UtcNow.Ticks;

		/// <summary>
		/// Current unix timestamp
		/// </summary>
		public static long CurrentUnixTimestamp => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

		public static long GetBanExpirationTime(uint seconds) => DateTime.UtcNow.AddSeconds(seconds).Ticks;

		public static bool ValidateTimestamp(long timestampentry, long timestampexit, long limit) => (timestampexit - timestampentry < limit);
	}
}
