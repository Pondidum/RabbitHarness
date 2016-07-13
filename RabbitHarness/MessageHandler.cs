using System;
using RabbitMQ.Client;

namespace RabbitHarness
{
	public class MessageHandler<TMessage>
	{
		public virtual void OnException(Exception ex)
		{
		}

		public virtual bool OnReceive(IBasicProperties props, TMessage message)
		{
			return false;
		}
	}
}
