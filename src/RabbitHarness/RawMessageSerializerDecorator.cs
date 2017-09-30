using System;

namespace RabbitHarness
{
	public class RawMessageSerializerDecorator : IMessageSerializer
	{
		private readonly IMessageSerializer _inner;

		public RawMessageSerializerDecorator(IMessageSerializer inner)
		{
			_inner = inner;
		}

		public byte[] Serialize(object message)
		{
			var bytes = message as byte[];

			return bytes ?? _inner.Serialize(message);
		}

		public TMessage Deserialize<TMessage>(byte[] bytes)
		{
			if (typeof(TMessage) == typeof(byte[]))
				return (TMessage)Convert.ChangeType(bytes, typeof(TMessage));

			return _inner.Deserialize<TMessage>(bytes);
		}
	}
}
