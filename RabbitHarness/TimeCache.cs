using System;
using System.Diagnostics;
using RabbitMQ.Client;

namespace RabbitHarness
{
	public class TimeCache
	{
		private static readonly Lazy<DateTime> Start;
		private static readonly Stopwatch Stopwatch;

		static TimeCache()
		{
			Stopwatch = new Stopwatch();
			Start = new Lazy<DateTime>(() =>
			{
				var nt = new NetworkTime();
				Stopwatch.Start();
				return nt.GetNetworkTime();
			});
		}

		public static AmqpTimestamp Now()
		{
			var now = Start.Value.AddMilliseconds(Stopwatch.ElapsedMilliseconds);

			return new AmqpTimestamp((long)(now - NetworkTime.Epoch).TotalSeconds);
		}
	}
}
