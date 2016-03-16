using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitHarness.Tests
{
	public class TestBase : IDisposable
	{
		protected const string Host = "192.168.99.100";
		protected readonly string QueueName;

		private readonly IConnection _connection;
		private readonly IModel _channel;

		protected ConnectionFactory Factory { get; }

		public TestBase()
		{
			Factory = new ConnectionFactory { HostName = Host };

			_connection = Factory.CreateConnection();
			_channel = _connection.CreateModel();
			QueueName = "TestsQueue" + Guid.NewGuid();
		}

		
		protected void CreateResponder(Action<IBasicProperties> mangle)
		{

			_channel.QueueDeclare(QueueName, durable: false, exclusive: false, autoDelete: true, arguments: null);
			_channel.BasicQos(0, 1, false);

			var listener = new EventingBasicConsumer(_channel);

			listener.Received += (s, e) =>
			{
				var result = Encoding.UTF8.GetString(e.Body).Length.ToString();

				var props = _channel.CreateBasicProperties();
				props.CorrelationId = e.BasicProperties.CorrelationId;

				mangle(props);

				_channel.BasicPublish(
					exchange: "",
					routingKey: e.BasicProperties.ReplyTo,
					basicProperties: props,
					body: Encoding.UTF8.GetBytes(result));
				_channel.BasicAck(e.DeliveryTag, false);
			};

			_channel.BasicConsume(QueueName, false, listener);
		}

		public void Dispose()
		{
			_channel.QueueDelete(QueueName);
			_channel.Dispose();
			_connection.Dispose();
		}

	}
}
