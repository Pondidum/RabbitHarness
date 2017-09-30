using System;
using RabbitMQ.Client;

namespace RabbitHarness
{
	public static class Extensions
	{

		private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);

		public static DateTime ToDateTime(this AmqpTimestamp self)
		{
			return Epoch.AddSeconds(self.UnixTime).ToLocalTime();
		}

		public static AmqpTimestamp ToTimestamp(this DateTime self)
		{
			var delta = self.ToUniversalTime() - Epoch;
			var ticks = (long)delta.TotalSeconds;

			return new AmqpTimestamp(ticks);
		}
	}
}