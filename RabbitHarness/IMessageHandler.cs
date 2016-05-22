namespace RabbitHarness
{
	public interface IMessageHandler
	{
		byte[] Serialize(object message);
		TMessage Deserialize<TMessage>(byte[] bytes);
	}
}
