namespace RabbitHarness
{
	public interface IMessageSerializer
	{
		byte[] Serialize(object message);
		TMessage Deserialize<TMessage>(byte[] bytes);
	}
}
