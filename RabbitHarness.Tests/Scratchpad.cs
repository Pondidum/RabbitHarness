using System;
using System.Text;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace RabbitHarness.Tests
{
	public class Scratchpad : IDisposable
	{
		private const string Host = "192.168.99.100";
		private const string QueueName = "some queue";

		private readonly ITestOutputHelper _output;

		private readonly IConnection _connection;
		private readonly IModel _channel;
		private readonly ConnectionFactory _factory;
		private readonly QueryContext _context;

		public Scratchpad(ITestOutputHelper output)
		{
			_output = output;

			_factory = new ConnectionFactory { HostName = Host };

			_connection = _factory.CreateConnection();
			_channel = _connection.CreateModel();

			_context = new QueryContext { QueueName = QueueName };
		}

		[RequiresRabbitFact(Host)]
		public void When_a_reply_is_sent()
		{
			CreateResponder();

			var reset = new AutoResetEvent(false);
			var message = new { Message = "message" };
			
			
			_factory.Query<int>(_context, message, (e, response) =>
			{
				response.ShouldBe(21);
				reset.Set();
			});

			reset.WaitOne(TimeSpan.FromSeconds(10));
		}

		[RequiresRabbitFact(Host)]
		public void When_nothing_is_listening()
		{
			var reset = new AutoResetEvent(false);
			var message = new { Message = "message" };	
			var received = false;

			_factory.Query<int>(_context, message, (e, response) =>
			{
				received = true;
				reset.Set();
			});

			reset.WaitOne(TimeSpan.FromSeconds(5));
			received.ShouldBe(false);
		}

		[RequiresRabbitFact(Host)]
		public void When_something_else_responds()
		{
			CreateResponder(p =>  p.CorrelationId = Guid.NewGuid().ToString());

			var reset = new AutoResetEvent(false);
			var message = new { Message = "message" };
			var received = false;

			_factory.Query<int>(_context, message, (e, response) =>
			{
				received = true;
				reset.Set();
			});

			reset.WaitOne(TimeSpan.FromSeconds(5));
			received.ShouldBe(false);
		}

		private void CreateResponder()
		{
			CreateResponder(p => { });
		}
		private void CreateResponder(Action<IBasicProperties> mangle)
		{

			_channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
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
