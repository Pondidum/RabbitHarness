using System;
using System.Diagnostics;
using Microsoft.Win32;
using RabbitMQ.Client;

namespace RabbitHarness
{
	public class TimeCache
	{
		private static Lazy<DateTime> _start;
		private static readonly Stopwatch Stopwatch;

		static TimeCache()
		{
			Stopwatch = new Stopwatch();
			Reset();

			SystemEvents.TimeChanged += (s, e) =>
			{
				Reset();
			};
		}

		public static void Reset()
		{
			_start = new Lazy<DateTime>(() =>
			{
				var nt = new NetworkTime();

				Stopwatch.Start();

				try
				{
					return nt.GetNetworkTime();
				}
				catch (Exception)
				{
					return DateTime.UtcNow;
				}
			});
		}

		public static AmqpTimestamp Now()
		{
			var now = _start.Value.AddMilliseconds(Stopwatch.ElapsedMilliseconds);

			return new AmqpTimestamp((long)(now - NetworkTime.Epoch).TotalSeconds);
		}
	}
}
