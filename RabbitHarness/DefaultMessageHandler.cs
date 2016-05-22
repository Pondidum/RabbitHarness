using System.Text;
using Newtonsoft.Json;

namespace RabbitHarness
{
	public class DefaultMessageHandler : IMessageHandler
	{
		private readonly JsonSerializerSettings _jsonSettings;

		public DefaultMessageHandler() 
			: this(new JsonSerializerSettings())
		{
		}

		public DefaultMessageHandler(JsonSerializerSettings jsonSettings)
		{
			_jsonSettings = jsonSettings;
		}

		public byte[] Serialize(object message)
		{
			var json = JsonConvert.SerializeObject(message, _jsonSettings);

			return Encoding.UTF8.GetBytes(json);
		}

		public TMessage Deserialize<TMessage>(byte[] bytes)
		{
			var json = Encoding.UTF8.GetString(bytes);

			return JsonConvert.DeserializeObject<TMessage>(json, _jsonSettings);
		}
	}
}
