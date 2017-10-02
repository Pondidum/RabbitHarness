using System;
using RabbitMQ.Client;
using Xunit;

namespace RabbitHarness.Tests
{
	public class RequiresRabbitFactAttribute : FactAttribute
	{
		private static readonly Lazy<bool> IsAvailable = new Lazy<bool>(() =>
		{
			var factory = new ConnectionFactory
			{
				HostName = TestBase.Host,
				RequestedConnectionTimeout = 1000
			};

			try
			{
				using (var connection = factory.CreateConnection())
					return connection.IsOpen;
			}
			catch (Exception)
			{
				return false;
			}
		});

		public override string Skip
		{
			get { return IsAvailable.Value ? "" : "RabbitMQ is not available"; }
			set { /* nothing */ }
		}
	}
}
