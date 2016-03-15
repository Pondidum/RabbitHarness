namespace RabbitHarness
{
	public class Route
	{
		internal string ExchangeName { get; set; }
		internal string QueueName { get; set; }

		public static Route Exchange(string exchange)
		{
			return Exchange(exchange, string.Empty);
		}

		public static Route Exchange(string exchange, string queue)
		{
			return new Route { ExchangeName = exchange, QueueName = queue };
		}

		public static Route Queue(string name)
		{
			return new Route { ExchangeName = string.Empty, QueueName = name };
		}
	}
}
