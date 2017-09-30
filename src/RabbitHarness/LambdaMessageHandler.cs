using System;
using RabbitMQ.Client;

namespace RabbitHarness
{
	public class LambdaMessageHandler<TMessage> : MessageHandler<TMessage>
	{
		private readonly Func<IBasicProperties, TMessage, bool> _handler;
		private readonly Action<Exception> _exceptionHandler;

		public LambdaMessageHandler(Func<IBasicProperties, TMessage, bool> handler)
		{
			_handler = handler;
			_exceptionHandler = ex => { };
		}

		public LambdaMessageHandler(Func<IBasicProperties, TMessage, bool> handler, Action<Exception> exceptionHandler)
		{
			_handler = handler;
			_exceptionHandler = exceptionHandler;
		}

		public override bool OnReceive(IBasicProperties props, TMessage message)
		{
			return _handler(props, message);
		}

		public override void OnException(Exception ex)
		{
			_exceptionHandler(ex);
		}
	}
}
