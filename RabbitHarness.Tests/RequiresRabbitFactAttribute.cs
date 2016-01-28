using System;
using RabbitMQ.Client;
using Xunit;

namespace RabbitHarness.Tests
{
	public class RequiresRabbitFactAttribute : FactAttribute
	{
		private static Func<bool> _isAvailable; 

		public RequiresRabbitFactAttribute(string host)
		{
			_isAvailable = _isAvailable ?? (() =>
			{
				var factory = new ConnectionFactory
				{
					HostName = host,
					RequestedConnectionTimeout = 1000
				};

				try
				{
					using (var connection = factory.CreateConnection())
					{
						return connection.IsOpen;
					}
				}
				catch (Exception)
				{
					return false;
				}
			});

			if (_isAvailable() == false)
			{
				Skip = "RabbitMQ is not available";
			}
		}
	}
}
