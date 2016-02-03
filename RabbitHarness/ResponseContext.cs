using RabbitMQ.Client;

namespace RabbitHarness
{
	public class ResponseContext<TMessage>
	{
		public TMessage Content { get; set; }
		public IBasicProperties Headers { get; set; }
	}
}
